using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.IO.Compression;
using SimpleJSON;
using System;
using JetBrains.Annotations;
using LogWriter;
using LogType = LogWriter.LogType;
using UnityEngine.Localization.Settings;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
        /// <param name="wavBytes"></param>
        /// <returns>此文件对应的AudioClip</returns>
        [CanBeNull]
        private static AudioClip WavToAudioClip(byte[] wavBytes)
        {
            try
            {
                // WAV文件的头部是44字节
                int headerOffset = 44;
                int sampleRate = BitConverter.ToInt32(wavBytes, 24);
                int channels = BitConverter.ToInt16(wavBytes, 22);
                int samples = (wavBytes.Length - headerOffset) / 2; // 16-bit stereo

                AudioClip clip = AudioClip.Create("MySound", samples / channels, channels, sampleRate, false);
                float[] data = new float[samples];

                int offset = headerOffset; // WAV头部
                for (int i = 0; i < samples; i++)
                {
                    data[i] = (short)(wavBytes[offset] | wavBytes[offset + 1] << 8) / 32768.0F;
                    offset += 2;
                }

                clip.SetData(data, 0);
                return clip;
            }
            catch (Exception ex)
            {
                Log.Write("WavToAudioClip error:" + ex, LogType.Error);
                return null;
            }
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

        //Type
        public ChartType ChartType;

        //List
        public List<JudgeLine> JudgeLineList = new();

        //Data
        public AudioClip Music;
        public Sprite Illustration;
        public string RawChart;

        #region 谱面转换区块

        [CanBeNull]
        public static async Task<Chart> ChartConverter(byte[] fileData, string cacheFileDirectory, string FileExtension)
        {
            //var table = LocalizationSettings.StringDatabase.GetTable("Languages");
            var chart = await Task.Run(() =>
            {
                try
                {
                    //在缓存文件夹下创建一个新的叫"ChartFileCache"的文件夹
                    if (Directory.Exists(cacheFileDirectory))
                    {
                        if (!Directory.Exists(cacheFileDirectory + "/ChartFileCache"))
                        {
                            Directory.CreateDirectory(cacheFileDirectory + "/ChartFileCache");
                        }
                    }

                    cacheFileDirectory += "/ChartFileCache";
                    //清空缓存文件夹
                    DirectoryInfo di = new(cacheFileDirectory);
                    foreach (var file in di.GetFiles())
                    {
                        file.Delete();
                    }

                    //检查文件扩展名是否为.zip
                    if (Path.GetExtension(FileExtension) != ".zip" && Path.GetExtension(FileExtension) != ".pez")
                    {
                        Log.Write("The selected file format is not support", LogType.Error);
                        throw new Exception(); //(table.GetEntry("Chart_Format_Err").GetLocalizedString());
                    }

                    //将文件解压
                    File.WriteAllBytes(cacheFileDirectory + "/ChartFileCache.zip", fileData);
                    ZipFile.ExtractToDirectory(cacheFileDirectory + "/ChartFileCache.zip", cacheFileDirectory);

                    //检查目录下是否含有config.json，如果有，读取到内存，否则返回null
                    if (!File.Exists(cacheFileDirectory + "/config.json"))
                    {
                        Log.Write("The selected file cannot be parsed into the config. json file", LogType.Error);
                        throw new Exception(); //(table.GetEntry("Chart_Config_Err").GetLocalizedString());
                    }

                    JSONNode jsonConfig = JSON.Parse(File.ReadAllText(cacheFileDirectory + "/config.json"));
                    //检查是否含有三个必要字段，music，illustration和chart
                    if (jsonConfig["music"] == null || jsonConfig["illustration"] == null ||
                        jsonConfig["chart"] == null)
                    {
                        Log.Write("Unable to find illustrations, music, or chart files in the selected file",
                            LogType.Error);
                        throw new Exception(); //(table.GetEntry("Chart_Config_Err").GetLocalizedString());
                    }

                    //string missingFile = table.GetEntry("Chart_Format_Err").GetLocalizedString();
                    //检查参数中的文件都是否存在，若其中一个不存在，报错并返回null
                    if (!File.Exists(cacheFileDirectory + "/" + jsonConfig["music"]))
                    {
                        Log.Write("Load music is Failed");
                        throw new Exception(); //missingFile + "music");
                    }

                    if (!File.Exists(cacheFileDirectory + "/" + jsonConfig["illustration"]))
                    {
                        Log.Write("Load illustration is Failed");
                        throw new Exception(); //missingFile + "illustration");
                    }

                    if (!File.Exists(cacheFileDirectory + "/" + jsonConfig["chart"]))
                    {
                        Log.Write("Load chart is Failed");
                        throw new Exception(); //missingFile + "chart");
                    }


                    //读取谱面
                    Chart chart = new()
                    {
                        RawChart = File.ReadAllText(cacheFileDirectory + "/" + jsonConfig["chart"])
                    };
                    var jsonChart = JSON.Parse(chart.RawChart);

                    //载入音频和插图
                    Main_Button_Click.Enqueue(() =>
                    {
                        chart.Music = WavToAudioClip(File.ReadAllBytes(cacheFileDirectory + "/" + jsonConfig["music"]));
                        chart.Illustration =
                            BytesToSprite(File.ReadAllBytes(cacheFileDirectory + "/" + jsonConfig["illustration"]));

                        //检查音频是否载入成功
                        try
                        {
                            double musicLength = chart.Music.length;
                        }
                        catch (NullReferenceException)
                        {
                            Log.Write("Load music is Failed", LogType.Error);
                            throw;
                        }
                    });


                    //谱面类型识别
                    if (jsonChart["formatVersion"] == 3)
                    {
                        //第三代Phigros官谱
                        Log.Write("Chart Version is Official_V3");
                        chart.ChartType = ChartType.OfficialV3;

                        //读取出所有判定线
                        var judgeLineList = jsonChart["judgeLineList"];

                        //遍历所有判定线   
                        for (int i = 0; i < judgeLineList.Count; i++)
                        {
                            //读取当前线的BPM
                            float judgeLineBpm = judgeLineList[i]["bpm"];
                            //读取出所有事件
                            var judgeLineMoveEventList =
                                judgeLineList[i]["judgeLineMoveEvents"]; //移动事件，官谱中，XY移动是绑定的

                            var judgeLineAngleChangeEventList =
                                judgeLineList[i]["judgeLineRotateEvents"]; //旋转事件，代码中重命名为角度变更事件

                            var judgeLineAlphaChangeEventList =
                                judgeLineList[i]["judgeLineDisappearEvents"]; //判定线消失事件，在代码中重命名为透明度事件

                            var judgeLineSpeedChangeEventList =
                                judgeLineList[i]["speedEvents"]; //Note流速变更事件

                            //判定线初始化
                            JudgeLine judgeLine = new();
                            //转换XY Move事件
                            for (int j = 0; j < judgeLineMoveEventList.Count; j++)
                            {
                                //时间转换
                                var eventStartTime = judgeLineMoveEventList[j]["startTime"] <= 0.0
                                    ? 0 //超界，按0处理
                                    : OfficialV3_TimeConverter(judgeLineMoveEventList[j]["startTime"],
                                        judgeLineBpm); //转换T为毫秒

                                var eventEndTime = OfficialV3_TimeConverter(judgeLineMoveEventList[j]["endTime"],
                                    judgeLineBpm);

                                //转换与添加坐标系
                                var eventXStartValue =
                                    CoordinateTransformer.TransformX(judgeLineMoveEventList[j]["start"]);
                                var eventXEndValue =
                                    CoordinateTransformer.TransformX(judgeLineMoveEventList[j]["end"]);
                                var eventYStartValue =
                                    CoordinateTransformer.TransformY(judgeLineMoveEventList[j]["start2"]);
                                var eventYEndValue =
                                    CoordinateTransformer.TransformY(judgeLineMoveEventList[j]["end2"]);

                                //添加数值到列表
                                judgeLine.xMoveList.Add(new Events.Event
                                {
                                    startTime = eventStartTime,
                                    endTime = eventEndTime,
                                    startValue = eventXStartValue,
                                    endValue = eventXEndValue
                                });
                                judgeLine.yMoveList.Add(new Events.Event
                                {
                                    startTime = eventStartTime,
                                    endTime = eventEndTime,
                                    startValue = eventYStartValue,
                                    endValue = eventYEndValue
                                });
                            }

                            judgeLine.angleChangeList = new EventList(); //角度变更事件初始化
                            //转换角度变更事件
                            for (int j = 0; j < judgeLineAngleChangeEventList.Count; j++)
                            {
                                //时间转换
                                var eventStartTime = judgeLineAngleChangeEventList[j]["startTime"] <= 0.0
                                    ? 0 //超界，按0处理
                                    : OfficialV3_TimeConverter(judgeLineAngleChangeEventList[j]["startTime"],
                                        judgeLineBpm); //转换T为毫秒

                                var eventEndTime = OfficialV3_TimeConverter(judgeLineAngleChangeEventList[j]["endTime"],
                                    judgeLineBpm); //转换T为毫秒

                                //添加数值到列表
                                judgeLine.angleChangeList.Add(new Events.Event
                                {
                                    startTime = eventStartTime,
                                    endTime = eventEndTime,
                                    startValue = judgeLineAngleChangeEventList[j]["start"],
                                    endValue = judgeLineAngleChangeEventList[j]["end"]
                                });
                            }

                            //转换透明度变更事件
                            for (int j = 0; j < judgeLineAlphaChangeEventList.Count; j++)
                            {
                                //时间转换
                                var eventStartTime = judgeLineAlphaChangeEventList[j]["startTime"] <= 0.0
                                    ? 0 //超界，按0处理
                                    : OfficialV3_TimeConverter(judgeLineAlphaChangeEventList[j]["startTime"],
                                        judgeLineBpm); //转换T为毫秒

                                var eventEndTime = OfficialV3_TimeConverter(judgeLineAlphaChangeEventList[j]["endTime"],
                                    judgeLineBpm); //转换T为毫秒 

                                //添加数值到列表
                                judgeLine.alphaChangeList.Add(new Events.Event
                                {
                                    startTime = eventStartTime,
                                    endTime = eventEndTime,
                                    startValue = judgeLineAlphaChangeEventList[j]["start"],
                                    endValue = judgeLineAlphaChangeEventList[j]["end"]
                                });
                            }

                            //转换速度变更事件
                            for (int j = 0; j < judgeLineSpeedChangeEventList.Count; j++)
                            {
                                //时间转换
                                double eventStartTime = judgeLineSpeedChangeEventList[j]["startTime"] <= 0.0
                                    ? 0 //超界，按0处理
                                    : OfficialV3_TimeConverter(judgeLineSpeedChangeEventList[j]["startTime"],
                                        judgeLineBpm); //转换T为毫秒 

                                double eventEndTime = OfficialV3_TimeConverter(
                                    judgeLineSpeedChangeEventList[j]["endTime"],
                                    judgeLineBpm); //转换T为毫秒 

                                //添加到列表
                                judgeLine.speedChangeList.Add(new Events.SpeedEvent
                                {
                                    startTime = eventStartTime,
                                    endTime = eventEndTime,
                                    startValue = judgeLineSpeedChangeEventList[j]["value"] / 1.5f,
                                    endValue = judgeLineSpeedChangeEventList[j]["value"] / 1.5f //官谱速度无任何缓动，只有关键帧
                                });
                            }

                            judgeLine.speedChangeList.CalcFloorPosition();


                            bool setAbove = true;
                            setNote:

                            JSONNode noteList = setAbove
                                ? judgeLineList[i]["notesAbove"]
                                : judgeLineList[i]["notesBelow"];


                            //Note遍历
                            for (int j = 0; j < noteList.Count; j++)
                            {
                                //Note类型识别
                                Note.NoteType noteType;
                                switch ((int)noteList[j]["type"])
                                {
                                    case 1:
                                        noteType = Note.NoteType.Tap;
                                        break;
                                    case 2:
                                        noteType = Note.NoteType.Drag;
                                        break;
                                    case 3:
                                        noteType = Note.NoteType.Hold;
                                        break;
                                    case 4:
                                        noteType = Note.NoteType.Flick;
                                        break;
                                    default:
                                        Log.Write(
                                            $"Unknown note types in {noteList[j]}\nThe chart may be damaged",
                                            LogType.Error);
                                        throw new Exception("Unknown note types, Chart may be damaged");
                                }

                                //打击时刻转换
                                double noteClickStartTime =
                                    OfficialV3_TimeConverter(noteList[j]["time"], judgeLineBpm);
                                double noteClickEndTime = noteType == Note.NoteType.Hold
                                    ? OfficialV3_TimeConverter(noteList[j]["holdTime"], judgeLineBpm) +
                                      noteClickStartTime
                                    : noteClickStartTime;

                                //添加Note
                                judgeLine.noteList.Add(new Note
                                {
                                    Type = noteType,
                                    clickStartTime = noteClickStartTime,
                                    clickEndTime = noteClickEndTime,
                                    //x = CoordinateTransformer.TransformX(noteList[j]["positionX"]),
                                    X = (float)noteList[j]["positionX"] * 108f,
                                    SpeedMultiplier = noteList[j]["speed"],
                                    Above = setAbove,
                                    FloorPosition =
                                        judgeLine.speedChangeList.GetCurTimeSu(noteClickStartTime), //这是临时修改。
                                    //floorPosition = float.Parse(noteList[j]["floorPosition"].ToString()) * 100f
                                });
                                //Log.Write("this note FP:" + Note.GetCurTimeSu(noteClickStartTime, judgeLine.speedChangeList)+"\norigin FP:"
                                //    + noteList[j]["floorPosition"],LogType.Debug);
                            }

                            if (setAbove)
                            {
                                setAbove = false;
                                goto setNote;
                            }

                            //添加判定线
                            chart.JudgeLineList.Add(judgeLine);
                        }

                        return chart;
                    }
                    else if (jsonChart["formatVersion"] == 1)
                    {
                        //第一代Phigros官谱
                        Log.Write("Chart Version is OfficialV1");
                        chart.ChartType = ChartType.OfficialV1;
                        throw new NotSupportedException("this version is not supported");
                    }
                    else if (jsonChart["META"] != "" || jsonChart["META"] != null)
                    {
                        //RPE谱面
                        Log.Write("Chart Version is RePhiEdi, but this chart is not supported.");
                        chart.ChartType = ChartType.RePhiEdit;
                        throw new NotSupportedException("this version is not supported");
                    }
                    else
                    {
                        //未知的或不支持的文件
                        Log.Write(
                            " The format of this chart may be PhiEdit, but it is not supported and will not be supported in the future");
                        chart.ChartType = ChartType.PhiEdit;
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


    /// <summary>
    /// 谱面类型
    /// </summary>
    public enum ChartType
    {
        OfficialV3,
        OfficialV1,
        RePhiEdit,
        PhiEdit,
        Nrlt
    }

    /// <summary>
    /// 事件
    /// </summary>
    public static class Events
    {
        /// <summary>
        /// 事件模板
        /// </summary>
        public class Event
        {
            //Value
            public float startValue { get; set; }
            public float endValue { get; set; }

            //Time
            public double startTime { get; set; }
            public double endTime { get; set; }
        }
        
        public class SpeedEvent : Event
        {
            //Special
            public double FloorPosition { get; set; }
        }
    }

    public class EventList : List<Events.Event>
    {
        public float GetCurValue(double t)
        {
            Events.Event previousEvent = null;

            foreach (var change in this)
            {
                if (t >= change.startTime && t <= change.endTime)
                {
                    float normalizedTime = (float)((t - change.startTime) / (change.endTime - change.startTime));
                    return Mathf.Lerp(change.startValue, change.endValue, normalizedTime);
                }
                previousEvent = change;
            }

            // 如果时间点不在任何变化区间内，使用上一个变化区间的 endValue
            if (previousEvent != null)
            {
                return previousEvent.endValue;
            }

            throw new ArgumentException("时间点不在任何变化区间内，且没有上一个变化区间");
        }
    }

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

    public class EventLayer
    {
        //Event List
        public EventList XMoveList = new();
        public EventList YMoveList  = new();
        public EventList AlphaChangeList = new();
        public EventList AngleChangeList = new();
        public SpeedEventList SpeedChangeList = new();
    }

    /// <summary>
    /// RPE特性：事件层级
    /// </summary>
    public class EventLayers : List<EventLayer>
    {
        public new void Add(EventLayer eventLayer)
        {
            if (Count >= 5) throw new InvalidOperationException("事件层级不能超过5层");
            base.Add(eventLayer);
        }
    }

    /// <summary>
    /// 音符
    /// </summary>
    public class Note
    {
        //Type
        public enum NoteType
        {
            Tap = 1,
            Drag = 2,
            Hold = 3,
            Flick = 4
        }

        public NoteType Type { get; set; }

        //Value
        public float X { get; set; }
        public float SpeedMultiplier { get; set; }

        //Special
        public bool Above { get; set; }

        //Time
        public double clickStartTime { get; set; }
        public double clickEndTime { get; set; }

        //SP
        public double FloorPosition { get; set; }
    }

    /// <summary>
    /// 判定线
    /// </summary>
    public class JudgeLine
    {
        //Event List
        public EventList xMoveList { get; set; } = new();
        public EventList yMoveList { get; set; } = new();
        public EventList alphaChangeList { get; set; } = new();
        public EventList angleChangeList { get; set; } = new();

        public SpeedEventList speedChangeList { get; set; } = new();

        //事件层级
        public List<EventLayer> eventLayers { get; set; } = new();

        //Note List
        public List<Note> noteList { get; set; } = new();
    }
}