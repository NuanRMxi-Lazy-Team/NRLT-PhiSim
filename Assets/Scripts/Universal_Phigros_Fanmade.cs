using UnityEngine;
using System.IO;
using System.IO.Compression;
using SimpleJSON;
using System;
using System.Linq;
using JetBrains.Annotations;
using LogWriter;
using LogType = LogWriter.LogType;
using UnityEngine.Localization.Settings;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RePhiEdit;
using UnityEngine.Networking;

namespace Phigros_Fanmade
{
    public class Chart
    {
        public const float HeightRatio = 1.83175f; // 神秘常量，爱来自Mivik
        public const float RpeSpeedToOfficial = 4.5f; // RPE速度转换为官谱速度的比例

        /// <summary>
        /// 官谱时间转换
        /// </summary>
        /// <param name="T"></param>
        /// <param name="bpm"></param>
        /// <returns>此时间对应的毫秒</returns>
        private static double OfficialV3_TimeConverter(double T, float bpm)
        {
            var originalTime = T / bpm * 1.875; //结果为秒
            return originalTime * 1000; //返回毫秒
        }
        
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

        /// <summary>
        /// wav音频文件转AudioClip
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>此文件对应的AudioClip</returns>
        [CanBeNull]
        private static AudioClip LoadAudioClip(string filePath)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.WAV))
            {
                www.SendWebRequest();

                while (!www.isDone)
                {
                }

                if (www.result == UnityWebRequest.Result.Success)
                {
                    return DownloadHandlerAudioClip.GetContent(www);
                }
            }

            return null;
        }

        #endregion

        #region 曲绘部分

        private static Sprite BytesToSprite(byte[] bytes)
        {
            Texture2D texture = new(1, 1);
            texture.LoadImage(bytes);
            try
            {
                // 插画预处理
                RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height);
                Graphics.Blit(texture, rt, new Material(Shader.Find("Custom/GaussianBlurWithBrightness")));
                RenderTexture.active = rt;
                Texture2D blurredTexture = new(texture.width, texture.height);
                blurredTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                blurredTexture.Apply();
                RenderTexture.ReleaseTemporary(rt);

                return Sprite.Create(blurredTexture, new Rect(0, 0, blurredTexture.width, blurredTexture.height),
                    new Vector2(0.5f, 0.5f));
            }
            catch (Exception e)
            {
                Log.Write(e.ToString(), LogType.Error);
                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
            }
        }

        #endregion

        //Data
        public string RawChart;
        private static AudioClip musicTemp;
        private static Sprite illustrationTemp;

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
                    DirectoryInfo di = new(cacheFileDir);
                    foreach (var file in di.GetFiles())
                    {
                        file.Delete();
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
                            if (Path.GetFileName(jsonFile) != "config.json")
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

                    var rpeChart = new RpeChart();
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

                    //读取谱面
                    var jsonChart = JSON.Parse(rawChart);

                    //载入音频和插图
                    Main_Button_Click.Enqueue(() =>
                    {
                        musicTemp = LoadAudioClip(musicFile);
                        illustrationTemp =
                            BytesToSprite(File.ReadAllBytes(illustrationFile));
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
                            if (rpeChart.BpmList.Count == 0)
                            {
                                rpeChart.BpmList.Add(new RpeClass.RpeBpm
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
                            rpeEventLayer.SpeedEvents.CalcFloorPosition(rpeChart.BpmList);
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
                                int noteType;
                                noteType = (int)noteList[j]["type"];

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
                                        new RpeClass.Beat(rpeClickBeat).CurTime(rpeChart.BpmList), rpeChart.BpmList)
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
                        while (musicTemp == null || illustrationTemp == null)
                        {
                        }

                        rpeChart.Music = musicTemp;
                        rpeChart.Illustration = illustrationTemp;
                        return rpeChart;
                    }
                    else if (jsonChart["formatVersion"] == 1)
                    {
                        //第一代Phigros官谱
                        Log.Write("Chart Version is OfficialV1");
                        throw new NotSupportedException("this version is not supported");
                    }
                    else if (jsonChart["META"] != "" || jsonChart["META"] != null)
                    {
                        //RPE谱面
                        rpeChart = JsonConvert.DeserializeObject<RpeChart>(rawChart);
                        foreach (var judgeLine in rpeChart.JudgeLineList)
                        {
                            judgeLine.CoordinateTransformer();
                            foreach (var eventLayer in judgeLine.EventLayers)
                            {
                                eventLayer.SpeedEvents.CalcFloorPosition(rpeChart.BpmList);
                            }

                            foreach (var note in judgeLine.Notes)
                            {
                                note.FloorPosition =
                                    judgeLine.EventLayers.GetCurFloorPosition(note.StartTime.CurTime(rpeChart.BpmList),
                                        rpeChart.BpmList);
                            }
                        }

                        //检查是否被赋值，没有就等待到被赋值
                        while (musicTemp == null || illustrationTemp == null)
                        {
                        }

                        rpeChart.Music = musicTemp;
                        rpeChart.Illustration = illustrationTemp;
                        return rpeChart;
                    }
                    else
                    {
                        //未知的或不支持的文件
                        Log.Write(
                            " The format of this chart may be PhiEdit, but it is not supported and will not be supported in the future");
                        throw new NotSupportedException("this version is not supported");
                    }
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


    /*
    public class SpeedEventList : List<Events.SpeedEvent>
    {
        public void CalcFloorPosition()
        {
            foreach (Events.SpeedEvent lastEvent in this)
            {
                int i = IndexOf(lastEvent);
                if (i == Count - 1) break;
                var curEvent = this[i + 1];

                double lastStartTime = lastEvent.startTime;
                double lastEndTime = lastEvent.endTime;

                double curStartTime = curEvent.startTime;


                curEvent.FloorPosition +=
                    lastEvent.FloorPosition +
                    (lastEvent.endValue + lastEvent.startValue) * (lastEndTime - lastStartTime) / 2 +
                    lastEvent.endValue * (curStartTime - lastEndTime) / 1;
            }
        }



        /// <summary>
        /// 获取当前时间的速度积分，计算Note和当前时间的floorPosition的主要方法
        /// </summary>
        /// <param name="time">当前时间，单位毫秒</param>
        /// <returns>从谱面开始到当前时间的总路程</returns>
        public double GetCurTimeSu(double time)
        {
            var floorPosition = 0.0d;
            foreach (Events.SpeedEvent speedEvent in this)
            {
                var startTime = speedEvent.startTime;
                var endTime = speedEvent.endTime;

                var i = IndexOf(speedEvent);
                if (Math.Abs(time - speedEvent.startTime) < 1e-5)
                {
                    floorPosition += speedEvent.FloorPosition;
                    break;
                }

                if (time <= speedEvent.endTime)
                {
                    floorPosition += speedEvent.FloorPosition +
                                     (speedEvent.startValue + (speedEvent.endValue - speedEvent.startValue) *
                                      (time - startTime) / (endTime - startTime) +
                                      speedEvent.startValue) * (time - startTime) / 2;
                    break;
                }

                if (Count - 1 != i && !(time <= this[i + 1].startTime)) continue;
                floorPosition += speedEvent.FloorPosition +
                                 (speedEvent.endValue + speedEvent.startValue) * (endTime - startTime) / 2 +
                                 speedEvent.endValue * (time - endTime) / 1;
                break;
            }

            return floorPosition;
        }
    }
    */
}