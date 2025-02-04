using UnityEngine;
using System.IO;
using System.IO.Compression;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using LogWriter;
using LogType = LogWriter.LogType;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RePhiEdit;

// ReSharper disable once CheckNamespace
namespace PhigrosFanmade
{
    public class Chart
    {
        private const float RpeSpeedToOfficial = 4.5f; // RPE速度转换为官谱速度的比例
        
        /// <summary>
        /// 转换官谱时间为RPE时间
        /// </summary>
        /// <param name="beat128">128分音符时间，对应官谱时间</param>
        /// <returns>RPE时间数组</returns>
        private static int[] ConvertToRpeTime(int beat128)
        {
            int[] result = new int[3];
            result[0] = beat128 / 32;
            int remainder = beat128 % 32;
            result[1] = remainder;
            result[2] = 32;
            return result;
        }

        /// <summary>
        /// 官谱坐标转换
        /// </summary>
        private static class CoordinateTransformer
        {
            //以1920x1080分辨率为基准，后续此代码块将调整
            private const float XMin = -960f;
            private const float XMax = 960f;
            private const float YMin = -540f;
            private const float YMax = 540f;


            /// <summary>
            /// 提供官谱X坐标，返回以输入分辨率为基准的X坐标
            /// </summary>
            /// <param name="x"></param>
            /// <returns>以输入分辨率为基准的X坐标</returns>
            public static float TransformX(float x)
            {
                //return (x - 0) / (1 - 0) * (675 - -675) + -675;
                return x * (XMax - XMin) + XMin;
            }

            /// <summary>
            /// 提供官谱Y坐标，返回以输入分辨率为基准的Y坐标
            /// </summary>
            /// <param name="y"></param>
            /// <returns>以输入分辨率为基准的Y坐标</returns>
            public static float TransformY(float y)
            {
                return y * (YMax - YMin) + YMin;
            }
        }

        #region 音频部分
        // 这就是AI写的，我不演
        private static AudioClip ConvertWavToAudioClip(byte[] wavData)
        {
            if (wavData == null || wavData.Length < 44)
            {
                Log.Write("未知的或损坏的WAV数据：少于44字节", LogType.Error);
                return null;
            }

            string riffHeader = System.Text.Encoding.UTF8.GetString(wavData, 0, 4);
            string waveHeader = System.Text.Encoding.UTF8.GetString(wavData, 8, 4);
            if (riffHeader != "RIFF" || waveHeader != "WAVE")
            {
                Log.Write("无效的WAV文件：缺少RIFF/WAVE头", LogType.Error);
                return null;
            }
            
            // 通道数在偏移22，采样率在24，每个样本的位数在34
            short channelCount = BitConverter.ToInt16(wavData, 22);
            int sampleRate = BitConverter.ToInt32(wavData, 24);
            short bitsPerSample = BitConverter.ToInt16(wavData, 34);
            
            // 找到“data”块。通常，它从偏移36或更晚开始。
            // 此函数通过查找“data”标记来找到数据块的开头。
            int dataOffset = FindDataChunkOffset(wavData);
            if (dataOffset < 0)
            {
                Log.Write("无法在WAV文件中找到'data'块", LogType.Error);
                return null;
            }
            
            // 数据大小存储在“data”后的4个字节中
            int dataSize = BitConverter.ToInt32(wavData, dataOffset + 4);
            if (dataOffset + 8 + dataSize > wavData.Length)
            {
                Log.Write("WAV数据大小无效或文件被截断", LogType.Error);
                return null;
            }
            
            // 计算总样本数 = dataSize / bytesPerSample（包括所有通道）
            int bytesPerSample = bitsPerSample / 8;
            int totalSampleCount = dataSize / bytesPerSample;
            int sampleCountPerChannel = totalSampleCount / channelCount;

            float[] audioData = new float[totalSampleCount];
            
            // 转换原始PCM数据为浮点样本
            int sampleDataIndex = dataOffset + 8; // 移动到“data”（4字节）+ dataSize（4字节）
            for (int i = 0; i < sampleCountPerChannel; i++)
            {
                for (int c = 0; c < channelCount; c++)
                {
                    // 确定不会超出缓冲区：
                    if (sampleDataIndex + 1 >= wavData.Length)
                    {
                        Log.Write("尝试去读超出WAV缓冲区。停止转换。", LogType.Error);
                        break;
                    }

                    short sample = BitConverter.ToInt16(wavData, sampleDataIndex);
                    sampleDataIndex += bytesPerSample;
                    // 将16位样本归一化到范围[-1，1]
                    audioData[i * channelCount + c] = sample / 32768f;
                }
            }
            
            // 新建一个AudioClip，长度、通道数和采样率都是正确的
            AudioClip audioClip =
                AudioClip.Create("ConvertedAudio", sampleCountPerChannel, channelCount, sampleRate, false);
            audioClip.SetData(audioData, 0);
            return audioClip;
        }

        /// <summary>
        /// 查找 WAV 文件中 "data" 块的起始索引。
        /// 在字节数组中查找 ASCII 字符串 "data"。
        /// 如果未找到，则返回 -1。
        /// </summary>
        private static int FindDataChunkOffset(byte[] wavData)
        {
            for (int i = 12; i < wavData.Length - 4; i++)
            {
                // Compare 4 bytes to "data"
                if (wavData[i] == 'd' && wavData[i + 1] == 'a'
                                      && wavData[i + 2] == 't' && wavData[i + 3] == 'a')
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 音频文件转AudioClip
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>此文件对应的AudioClip</returns>
        [CanBeNull]
        private static AudioClip LoadAudioClip(string filePath)
        {
            var outputFilePath = Path.GetDirectoryName(filePath) + "/output.wav";

#if !UNITY_EDITOR_WIN && UNITY_ANDROID
            outputFilePath = Path.Combine(Application.persistentDataPath, "output.wav");
            string arguments = $"-i \"{filePath}\" -y -nostdin \"{outputFilePath}\""; // 输入文件和输出文件路径，确保路径中包含空格时使用引号

            AndroidJavaClass configClass = new AndroidJavaClass("com.arthenica.ffmpegkit.FFmpegKitConfig");
            AndroidJavaObject paramVal =
                new AndroidJavaClass("com.arthenica.ffmpegkit.Signal").GetStatic<AndroidJavaObject>("SIGXCPU");
            configClass.CallStatic("ignoreSignal", new object[] { paramVal });

            AndroidJavaClass javaClass = new AndroidJavaClass("com.arthenica.ffmpegkit.FFmpegKit");
            AndroidJavaObject session = javaClass.CallStatic<AndroidJavaObject>("execute", new object[] { arguments });

            AndroidJavaObject returnCode = session.Call<AndroidJavaObject>("getReturnCode", new object[] { });
            int rc = returnCode.Call<int>("getValue", new object[] { });
            // 检查返回值
            if (rc != 0)
            {
                Log.Write("FFmpeg error: " + rc, LogType.Error);
                return null;
            }
#else
            // 获取内置的 ffmpeg.exe 路径
            string ffmpegPath = Application.dataPath + "/Plugins/Windows/ffmpeg.exe"; // 使用 Assets/Plugins/Windows 目录

            // 构建 FFmpeg 命令行参数
            string arguments = $"-i \"{filePath}\" -y -nostdin \"{outputFilePath}\""; // 输入文件和输出文件路径，确保路径中包含空格时使用引号

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath, // FFmpeg 可执行文件路径
                Arguments = arguments, // 参数
                CreateNoWindow = true, // 不显示窗口
                UseShellExecute = false, // 不使用系统shell执行
            };

            var process = new Process
            {
                StartInfo = startInfo
            };


            // 启动进程
            process.Start();

            // 等待进程结束
            process.WaitForExit();
#endif
            var wavBytes = File.ReadAllBytes(outputFilePath);
            return ConvertWavToAudioClip(wavBytes);
        }

        #endregion

        #region 曲绘部分

        private static Sprite BytesToSprite(byte[] bytes)
        {
            Texture2D texture = new(1, 1);
            texture.LoadImage(bytes);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
        }
        
        private static Sprite IllustrationBlur(Sprite sprite)
        {
            Texture2D texture = sprite.texture;
            int width = texture.width;
            int height = texture.height;
            Texture2D newTexture = new Texture2D(width, height);
            Color[] colors = texture.GetPixels();
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Color color = colors[j * width + i];
                    newTexture.SetPixel(i, j, new Color(color.r, color.g, color.b, 0.5f));
                }
            }

            newTexture.Apply();
            return Sprite.Create(newTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        #endregion

        //Data
        private static AudioClip _musicTemp;
        private static Sprite _illustrationTemp;

        #region 谱面转换区块

        [CanBeNull]
        public static async Task<RpeChart> ChartConverter(byte[] fileData, string cacheFileDir, string fileExtension)
        {
            //var table = LocalizationSettings.StringDatabase.GetTable("Languages");
            var chart = await Task.Run(() =>
            {
                try
                {
                    //在缓存文件夹下创建一个新的叫"ChartFileCache"的文件夹
                    if (Directory.Exists(cacheFileDir))
                    {
                        if (!Directory.Exists(cacheFileDir + "/ChartFileCache"))
                        {
                            Directory.CreateDirectory(cacheFileDir + "/ChartFileCache");
                        }
                    }

                    cacheFileDir += "/ChartFileCache";
                    //清空缓存文件夹
                    DirectoryInfo directory = new DirectoryInfo(cacheFileDir);

                    // 删除所有文件
                    foreach (FileInfo file in directory.GetFiles())
                    {
                        file.Delete();
                    }

                    // 删除所有子目录
                    foreach (DirectoryInfo subDirectory in directory.GetDirectories())
                    {
                        subDirectory.Delete(true);
                    }

                    //检查文件扩展名是否为.zip
                    if (Path.GetExtension(fileExtension) != ".zip" && Path.GetExtension(fileExtension) != ".pez")
                    {
                        Log.Write("The selected file format is not support", LogType.Error);
                        throw new Exception(); //(table.GetEntry("Chart_Format_Err").GetLocalizedString());
                    }

                    //将文件解压
                    File.WriteAllBytes(cacheFileDir + "/ChartFileCache.zip", fileData);
                    ZipFile.ExtractToDirectory(cacheFileDir + "/ChartFileCache.zip", cacheFileDir);

                    // 查找目录中有多少个.json文件
                    var jsonFiles = Directory.GetFiles(cacheFileDir, "*.json");
                    // 如果啥也没有，抛出异常，因为既没有谱面也没有配置
                    if (jsonFiles.Length == 0)
                    {
                        Log.Write("The selected file cannot be parsed into the config. json file", LogType.Error);
                        throw new Exception(); //(table.GetEntry("Chart_Config_Err").GetLocalizedString());
                    }

                    string rawChart = "";
                    // 如果大于一个json文件，选择名称不是config.json的文件直接作为谱面
                    if (jsonFiles.Length > 1)
                    {
                        // 选取第一个名称不是config.json的文件
                        foreach (var jsonFile in jsonFiles)
                        {
                            var fileName = Path.GetFileName(jsonFile);
                            if (fileName != "config.json" && fileName != "extra.json")
                            {
                                rawChart = File.ReadAllText(jsonFile);
                            }
                        }
                    }
                    else
                    {
                        // 如果只有一个json文件，直接读取
                        rawChart = File.ReadAllText(jsonFiles[0]);
                    }

                    // 寻找音乐文件，后缀为所有音频格式，如果找到多个，选择与谱面同名（不包含后缀）的音频文件
                    var musicFile = Directory.GetFiles(cacheFileDir, "*.*", SearchOption.AllDirectories)
                        .Where(s => s.EndsWith(".wav") || s.EndsWith(".mp3") || s.EndsWith(".ogg") ||
                                    s.EndsWith(".flac"))
                        .ToArray().First();
                    // 寻找插图文件，后缀为所有图片格式，如果找到多个，选择与谱面同名（不包含后缀）的图片文件
                    var illustrationFile = Directory.GetFiles(cacheFileDir, "*.*", SearchOption.AllDirectories)
                        .Where(s => s.EndsWith(".png") || s.EndsWith(".jpg") || s.EndsWith(".jpeg") ||
                                    s.EndsWith(".bmp"))
                        .ToArray().First();

                    var rpeChart = new RpeChart(true);

                    //检查是否含有三个必要字段，music，illustration和chart
                    if (musicFile == null || illustrationFile == null ||
                        rawChart == null)
                    {
                        Log.Write("Unable to find illustrations, music, or chart files in the selected file",
                            LogType.Error);
                        throw new Exception(); //(table.GetEntry("Chart_Config_Err").GetLocalizedString());
                    }

                    //string missingFile = table.GetEntry("Chart_Format_Err").GetLocalizedString();
                    //检查参数中的文件都是否存在，若其中一个不存在，报错并返回null
                    if (!File.Exists(musicFile))
                    {
                        Log.Write("Load music is Failed");
                        throw new Exception(); //missingFile + "music");
                    }

                    if (!File.Exists(illustrationFile))
                    {
                        Log.Write("Load illustration is Failed");
                        throw new Exception(); //missingFile + "illustration");
                    }

                    // 读取谱面
                    var jsonChart = JSON.Parse(rawChart);
                    // 清空缓存
                    _musicTemp = null;
                    _illustrationTemp = null;
                    //载入音频和插图
                    Main_Button_Click.Enqueue(() =>
                    {
                        _musicTemp = LoadAudioClip(musicFile);
                        _illustrationTemp =IllustrationBlur(BytesToSprite(File.ReadAllBytes(illustrationFile)));
                    });


                    //谱面类型识别
                    if (jsonChart["formatVersion"] == 3)
                    {
                        //第三代Phigros官谱
                        Log.Write("Chart Version is Official_V3");

                        //读取出所有判定线
                        var judgeLineList = jsonChart["judgeLineList"];

                        //遍历所有判定线   
                        for (int i = 0; i < judgeLineList.Count; i++)
                        {
                            //读取当前线的BPM
                            float judgeLineBpm = judgeLineList[i]["bpm"];
                            //RPE
                            if (RpeChart.BpmList.Count == 0)
                            {
                                RpeChart.BpmList.Add(new RpeClass.RpeBpm
                                {
                                    Bpm = judgeLineBpm,
                                    StartTime = new RpeClass.Beat()
                                });
                            }

                            var rpeJudgeLine = new RpeClass.JudgeLine();
                            var rpeEventLayer = new RpeClass.EventLayer();
                            //读取出所有事件
                            var judgeLineMoveEventList =
                                judgeLineList[i]["judgeLineMoveEvents"]; //移动事件，官谱中，XY移动是绑定的

                            var judgeLineAngleChangeEventList =
                                judgeLineList[i]["judgeLineRotateEvents"]; //旋转事件，代码中重命名为角度变更事件

                            var judgeLineAlphaChangeEventList =
                                judgeLineList[i]["judgeLineDisappearEvents"]; //判定线消失事件，在代码中重命名为透明度事件

                            var judgeLineSpeedChangeEventList =
                                judgeLineList[i]["speedEvents"]; //Note流速变更事件

                            //转换XY Move事件
                            for (int j = 0; j < judgeLineMoveEventList.Count; j++)
                            {
                                //时间转换
                                var rpeStartBeat =
                                    ConvertToRpeTime(judgeLineMoveEventList[j]["startTime"]);
                                var rpeEndBeat = ConvertToRpeTime(judgeLineMoveEventList[j]["endTime"]);

                                //转换与添加坐标系
                                var eventXStartValue =
                                    CoordinateTransformer.TransformX(judgeLineMoveEventList[j]["start"]);
                                var eventXEndValue =
                                    CoordinateTransformer.TransformX(judgeLineMoveEventList[j]["end"]);
                                var eventYStartValue =
                                    CoordinateTransformer.TransformY(judgeLineMoveEventList[j]["start2"]);
                                var eventYEndValue =
                                    CoordinateTransformer.TransformY(judgeLineMoveEventList[j]["end2"]);

                                // RPE
                                rpeEventLayer.MoveXEvents.Add(new RpeClass.Event
                                {
                                    StartTime = new RpeClass.Beat(rpeStartBeat),
                                    EndTime = new RpeClass.Beat(rpeEndBeat),
                                    Start = eventXStartValue,
                                    End = eventXEndValue
                                });
                                // RPE
                                rpeEventLayer.MoveYEvents.Add(new RpeClass.Event
                                {
                                    StartTime = new RpeClass.Beat(rpeStartBeat),
                                    EndTime = new RpeClass.Beat(rpeEndBeat),
                                    Start = eventYStartValue,
                                    End = eventYEndValue
                                });
                            }

                            //转换角度变更事件
                            for (int j = 0; j < judgeLineAngleChangeEventList.Count; j++)
                            {
                                //时间转换
                                // RPE
                                var rpeStartBeat = ConvertToRpeTime(judgeLineAngleChangeEventList[j]["startTime"]);

                                // RPE
                                var rpeEndBeat = ConvertToRpeTime(judgeLineAngleChangeEventList[j]["endTime"]);

                                //添加数值到列表

                                // RPE
                                rpeEventLayer.RotateEvents.Add(new RpeClass.Event
                                {
                                    StartTime = new RpeClass.Beat(rpeStartBeat),
                                    EndTime = new RpeClass.Beat(rpeEndBeat),
                                    Start = judgeLineAngleChangeEventList[j]["start"],
                                    End = judgeLineAngleChangeEventList[j]["end"]
                                });
                            }

                            //转换透明度变更事件
                            for (int j = 0; j < judgeLineAlphaChangeEventList.Count; j++)
                            {
                                // RPE
                                var rpeStartBeat = ConvertToRpeTime(judgeLineAlphaChangeEventList[j]["startTime"]);

                                // RPE
                                var rpeEndBeat = ConvertToRpeTime(judgeLineAlphaChangeEventList[j]["endTime"]);

                                //添加数值到列表

                                // RPE，官谱中1为完全不透明，0为完全透明，在RPE中，255为完全不透明，0为完全透明，确保转换并丢弃小数点
                                rpeEventLayer.AlphaEvents.Add(new RpeClass.Event
                                {
                                    StartTime = new RpeClass.Beat(rpeStartBeat),
                                    EndTime = new RpeClass.Beat(rpeEndBeat),
                                    Start = judgeLineAlphaChangeEventList[j]["start"],
                                    End = judgeLineAlphaChangeEventList[j]["end"]
                                });
                            }

                            //转换速度变更事件
                            for (int j = 0; j < judgeLineSpeedChangeEventList.Count; j++)
                            {
                                // RPE
                                var rpeStartBeat = ConvertToRpeTime(judgeLineSpeedChangeEventList[j]["startTime"]);

                                // RPE
                                var rpeEndBeat = ConvertToRpeTime(judgeLineSpeedChangeEventList[j]["endTime"]);

                                // RPE
                                rpeEventLayer.SpeedEvents.Add(new RpeClass.SpeedEvent
                                {
                                    StartTime = new RpeClass.Beat(rpeStartBeat),
                                    EndTime = new RpeClass.Beat(rpeEndBeat),
                                    Start = judgeLineSpeedChangeEventList[j]["value"] * RpeSpeedToOfficial,
                                    End = judgeLineSpeedChangeEventList[j]["value"] * RpeSpeedToOfficial
                                });
                            }

                            rpeEventLayer.SpeedEvents.CalcFloorPosition(RpeChart.BpmList);
                            rpeJudgeLine.EventLayers.Add(rpeEventLayer);

                            bool setAbove = true;
                            setNote:

                            JSONNode noteList = setAbove
                                ? judgeLineList[i]["notesAbove"]
                                : judgeLineList[i]["notesBelow"];


                            //Note遍历
                            for (int j = 0; j < noteList.Count; j++)
                            {
                                //Note类型识别
                                int noteType = (int)noteList[j]["type"];

                                // RPE
                                var rpeClickBeat = ConvertToRpeTime(noteList[j]["time"]);
                                // RPE
                                var rpeEndBeat =
                                    ConvertToRpeTime((int)noteList[j]["time"] + (int)noteList[j]["holdTime"]);

                                //添加Note

                                // RPE，与官谱不同，note类型，1为Tap、2为Hold、3为Flick、4为Drag
                                // 官谱为：1为Tap、2为Drag、3为Hold、4为Flick
                                int rpeNoteType = noteType switch
                                {
                                    1 => 1,
                                    2 => 4,
                                    3 => 2,
                                    4 => 3,
                                    _ => 1
                                };
                                rpeJudgeLine.Notes.Add(new RpeClass.Note
                                {
                                    Type = rpeNoteType,
                                    StartTime = new RpeClass.Beat(rpeClickBeat),
                                    EndTime = new RpeClass.Beat(rpeEndBeat),
                                    PositionX = (float)noteList[j]["positionX"] * 108f,
                                    SpeedMultiplier = noteList[j]["speed"],
                                    Above = setAbove ? 1 : 2,
                                    FloorPosition = rpeJudgeLine.EventLayers.GetCurFloorPosition(
                                        new RpeClass.Beat(rpeClickBeat).CurTime())
                                });
                            }

                            if (setAbove)
                            {
                                setAbove = false;
                                goto setNote;
                            }

                            //添加判定线
                            rpeChart.JudgeLineList.Add(rpeJudgeLine);
                        }

                        //检查是否被赋值，没有就等待到被赋值
                        while (_musicTemp == null || _illustrationTemp == null)
                        {
                        }

                        rpeChart.Music = _musicTemp;
                        rpeChart.Illustration = _illustrationTemp;
                        float offsetSec = jsonChart["offset"];
                        rpeChart.Meta.Offset = (int)(offsetSec * 1000f);
                        return rpeChart;
                    }
                    if (jsonChart["formatVersion"] == 1)
                    {
                        //第一代Phigros官谱
                        Log.Write("Chart Version is OfficialV1");
                        throw new NotSupportedException("this version is not supported");
                    }
                    if (jsonChart["META"] != "" || jsonChart["META"] != null)
                    {
                        Main_Button_Click.Enqueue(() => Log.Write("序列化谱面..."));
                        //RPE谱面
                        rpeChart = JsonConvert.DeserializeObject<RpeChart>(rawChart);
                        Main_Button_Click.Enqueue(() => Log.Write("序列化成功..."));
                        JudgeLineSprites.SpritePool = new Dictionary<string, Sprite>();
                        foreach (var judgeLine in rpeChart.JudgeLineList)
                        {
                            judgeLine.CoordinateTransformer();
                            foreach (var eventLayer in judgeLine.EventLayers)
                            {
                                eventLayer.SpeedEvents?.CalcFloorPosition(RpeChart.BpmList);
                            }

                            for (int i = 0; i < judgeLine.Notes.Count; i++)
                            {
                                var note = judgeLine.Notes[i];
                                note.FloorPosition =
                                    judgeLine.EventLayers.GetCurFloorPosition(note.StartTime.CurTime());
                                judgeLine.Notes[i] = note;
                            }

                            if (judgeLine.Texture != "line.png")
                            {
                                // 将判定线贴图加载到Byte[]中
                                var judgeLineSpriteBytes =
                                    File.ReadAllBytes(Path.Combine(cacheFileDir, judgeLine.Texture));
                                Main_Button_Click.Enqueue(() =>
                                {
                                    // 转换为Sprite
                                    var judgeLineSprite = BytesToSprite(judgeLineSpriteBytes);
                                    judgeLineSprite.name = judgeLine.Texture;
                                    // 存储，方便后续调用
                                    JudgeLineSprites.SpritePool.TryAdd(judgeLine.Texture, judgeLineSprite);
                                    Log.Write(judgeLine.Texture + "已加载");
                                });
                            }
                        }

                        Main_Button_Click.Enqueue(() => Log.Write("数据整理完成..."));

                        bool musicDone = false;
                        bool illustrationDone = false;
                        //检查是否被赋值，没有就等待到被赋值
                        while (_musicTemp is null || _illustrationTemp is null)
                        {
                            if (_musicTemp is not null && !musicDone)
                            {
                                musicDone = true;
                                Main_Button_Click.Enqueue(() => Log.Write("音乐成功加载"));
                            }
                            if (_illustrationTemp is not null && !illustrationDone)
                            {
                                illustrationDone = true;
                                Main_Button_Click.Enqueue(() => Log.Write("曲绘成功加载"));
                            }
                        }

                        rpeChart.Music = _musicTemp;
                        rpeChart.Illustration = _illustrationTemp;
                        Main_Button_Click.Enqueue(() => Log.Write("返回结果！"));
                        return rpeChart;
                    }
                    //未知的或不支持的文件
                    Log.Write(
                        " The format of this chart may be PhiEdit, but it is not supported and will not be supported in the future");
                    throw new NotSupportedException("this version is not supported");
                }
                catch (Exception ex)
                {
                    Log.Write(ex.ToString(), LogType.Error);
                    throw;
                }
            });
            return chart;
        }

        #endregion
    }
}