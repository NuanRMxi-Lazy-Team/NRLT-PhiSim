using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.IO.Compression;
using SimpleJSON;
using System;
using System.Linq;
using JetBrains.Annotations;
using LogWriter;
using LogType = LogWriter.LogType;

namespace Phigros_Fanmade
{
    public class Chart
    {
        /// <summary>
        /// 官谱时间转换
        /// </summary>
        /// <param name="T"></param>
        /// <param name="bpm"></param>
        /// <returns>此时间对应的毫秒</returns>
        private static double OfficialV3_TimeConverter(double T, float bpm)
        {
            double originalTime = (T / bpm) * 1.875; //结果为秒
            originalTime *= 1000; //转换为毫秒
            return originalTime; //返回
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

            // 插画预处理
            RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height);
            Graphics.Blit(texture, rt, new Material(Shader.Find("Custom/HighQualityGaussianBlurWithBrightness")));
            RenderTexture.active = rt;
            Texture2D blurredTexture = new(texture.width, texture.height);
            blurredTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            blurredTexture.Apply();
            RenderTexture.ReleaseTemporary(rt);

            return Sprite.Create(blurredTexture, new Rect(0, 0, blurredTexture.width, blurredTexture.height),
                new Vector2(0.5f, 0.5f));
        }

        #endregion

        //Type
        public ChartType chartType { get; set; }

        //List
        public List<JudgeLine> judgeLineList { get; set; } = new();

        //Data
        public AudioClip music { get; set; }
        public Sprite Illustration { get; set; }
        public string rawChart { get; set; }

        #region 谱面转换区块

        [CanBeNull]
        public static Chart ChartConverter(byte[] fileData, string cacheFileDirectory, string FileExtension)
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
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }

                //检查文件扩展名是否为.zip
                if (Path.GetExtension(FileExtension) != ".zip")
                {
                    Log.Write("The selected file format is not zip");
                    throw new Exception("file format error.");
                }

                //将文件解压
                File.WriteAllBytes(cacheFileDirectory + "/ChartFileCache.zip", fileData);
                ZipFile.ExtractToDirectory(cacheFileDirectory + "/ChartFileCache.zip", cacheFileDirectory);

                //检查目录下是否含有config.json，如果有，读取到内存，否则返回null
                JSONNode jsonConfig;
                if (!File.Exists(cacheFileDirectory + "/config.json"))
                {
                    Log.Write("The selected file cannot be parsed into the config. json file");
                    throw new Exception("config file error.");
                }
                else
                {
                    jsonConfig = JSON.Parse(File.ReadAllText(cacheFileDirectory + "/config.json"));
                    //检查是否含有三个必要字段，music，illustration和chart
                    if (jsonConfig["music"] == null || jsonConfig["illustration"] == null ||
                        jsonConfig["chart"] == null)
                    {
                        Log.Write("Unable to find illustrations, music, or chart files in the selected file");
                        throw new Exception("Unable to find illustrations, music, or chart files in the selected file");
                    }
                }

                //检查参数中的文件都是否存在，若其中一个不存在，报错并返回null
                if (!File.Exists(cacheFileDirectory + "/" + jsonConfig["music"]))
                {
                    Log.Write("Load music is Failed");
                    throw new Exception("Load music is Failed");
                }

                if (!File.Exists(cacheFileDirectory + "/" + jsonConfig["illustration"]))
                {
                    Log.Write("Load illustration is Failed");
                    throw new Exception("Load illustration is Failed");
                }

                if (!File.Exists(cacheFileDirectory + "/" + jsonConfig["chart"]))
                {
                    Log.Write("Load chart is Failed");
                    throw new Exception("Load chart is Failed");
                }


                //读取谱面
                Chart chart = new()
                {
                    rawChart = File.ReadAllText(cacheFileDirectory + "/" + jsonConfig["chart"])
                };
                var jsonChart = JSON.Parse(chart.rawChart);

                //载入音频和插图
                chart.music = WavToAudioClip(File.ReadAllBytes(cacheFileDirectory + "/" + jsonConfig["music"]));
                chart.Illustration =
                    BytesToSprite(File.ReadAllBytes(cacheFileDirectory + "/" + jsonConfig["illustration"]));
                //获取音频时长，单位毫秒
                double musicLength = chart.music.length * 1000;
                
                
                //谱面类型识别
                if (jsonChart["formatVersion"] == 3)
                {
                    //第三代Phigros官谱
                    Log.Write("Chart Version is Official_V3");
                    chart.chartType = ChartType.Official_V3;

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

                            var eventEndTime = judgeLineMoveEventList[j]["endTime"] >= 1000000000.0
                                ? musicLength //超界
                                : OfficialV3_TimeConverter(judgeLineMoveEventList[j]["endTime"], judgeLineBpm);

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
                            judgeLine.xMoveList.Add(new Event.XMove
                            {
                                startTime = eventStartTime,
                                endTime = eventEndTime,
                                startValue = eventXStartValue,
                                endValue = eventXEndValue
                            });
                            judgeLine.yMoveList.Add(new Event.YMove
                            {
                                startTime = eventStartTime,
                                endTime = eventEndTime,
                                startValue = eventYStartValue,
                                endValue = eventYEndValue
                            });
                        }

                        judgeLine.angleChangeList = new List<Event.AngleChange>(); //角度变更事件初始化
                        //转换角度变更事件
                        for (int j = 0; j < judgeLineAngleChangeEventList.Count; j++)
                        {
                            //时间转换
                            var eventStartTime = judgeLineAngleChangeEventList[j]["startTime"] <= 0.0
                                ? 0 //超界，按0处理
                                : OfficialV3_TimeConverter(judgeLineAngleChangeEventList[j]["startTime"],
                                    judgeLineBpm); //转换T为毫秒

                            var eventEndTime = judgeLineAngleChangeEventList[j]["endTime"] >= 1000000000.0
                                ? musicLength //超界
                                : OfficialV3_TimeConverter(judgeLineAngleChangeEventList[j]["endTime"],
                                    judgeLineBpm); //转换T为毫秒

                            //添加数值到列表
                            judgeLine.angleChangeList.Add(new Event.AngleChange
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

                            var eventEndTime = judgeLineAlphaChangeEventList[j]["endTime"] >= 1000000000.0
                                ? musicLength //超界
                                : OfficialV3_TimeConverter(judgeLineAlphaChangeEventList[j]["endTime"],
                                    judgeLineBpm); //转换T为毫秒 

                            //添加数值到列表
                            judgeLine.alphaChangeList.Add(new Event.AlphaChange
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

                            double eventEndTime = judgeLineSpeedChangeEventList[j]["endTime"] >= 1000000000.0
                                ? musicLength //超界
                                : OfficialV3_TimeConverter(judgeLineSpeedChangeEventList[j]["endTime"],
                                    judgeLineBpm); //转换T为毫秒 

                            //添加到列表
                            judgeLine.speedChangeList.Add(new Event.SpeedEvent
                            {
                                startTime = eventStartTime,
                                endTime = eventEndTime,
                                startValue = judgeLineSpeedChangeEventList[j]["value"],
                                endValue = judgeLineSpeedChangeEventList[j]["value"] //官谱速度无任何缓动，只有关键帧
                            });
                        }


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
                                type = noteType,
                                clickStartTime = noteClickStartTime,
                                clickEndTime = noteClickEndTime,
                                //x = CoordinateTransformer.TransformX(noteList[j]["positionX"]),
                                x = (float)noteList[j]["positionX"] * 108f,
                                speedMultiplier = noteList[j]["speed"],
                                above = setAbove,
                                floorPosition = judgeLine.speedChangeList.GetCurTimeSu(noteClickStartTime) //这是临时修改。
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
                        chart.judgeLineList.Add(judgeLine);
                    }

                    return chart;
                }
                else if (jsonChart["formatVersion"] == 1)
                {
                    //第一代Phigros官谱
                    Log.Write("Chart Version is Official_V1");
                    chart.chartType = ChartType.Official_V1;
                    return chart; //暂不支持
                }
                else if (jsonChart["META"] != "" || jsonChart["META"] != null)
                {
                    //第4.0代RPE谱面
                    Log.Write("Chart Version is RePhiEdit_V400, but this chart is not supported.");
                    chart.chartType = ChartType.RePhiEdit_V400;
                    return chart; //暂不支持
                }
                else
                {
                    //未知的或不支持的文件
                    Log.Write(
                        " The format of this chart may be PhiEdit_V0, but it is not supported and will not be supported in the future");
                    chart.chartType = ChartType.PhiEdit_V0;
                    return chart; //永不支持，滚出去
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex.Message, LogType.Error);
                return null;
            }
        }

        #endregion
    }


    /// <summary>
    /// 谱面类型
    /// </summary>
    public enum ChartType
    {
        Official_V3,
        Official_V1,
        RePhiEdit_V400,
        PhiEdit_V0,
        SPCNRLT
    }

    /// <summary>
    /// 事件
    /// </summary>
    public static class Event
    {
        /// <summary>
        /// 事件模板
        /// </summary>
        public class EventTemplate
        {
            //Value
            public float startValue { get; set; }
            public float endValue { get; set; }

            //Time
            public double startTime { get; set; }
            public double endTime { get; set; }
        }
        /// <summary>
        /// X移动事件
        /// </summary>
        public class XMove : EventTemplate
        { }

        /// <summary>
        /// Y移动事件
        /// </summary>
        public class YMove : EventTemplate
        { }

        /// <summary>
        /// 透明度变化事件
        /// </summary>
        public class AlphaChange : EventTemplate
        { }

        /// <summary>
        /// 角度变化事件
        /// </summary>
        public class AngleChange : EventTemplate
        { }

        /// <summary>
        /// 流速变化事件
        /// </summary>
        public class SpeedEvent : EventTemplate
        {
            //Special
            public double floorPosition { get; set; } = 0.0;
        }
    }

    public static class EventList
    {
        public class XMoveList : List<Event.XMove>
        {
            
        }
        public class YMoveList : List<Event.YMove>
        {
            
        }
        public class AlphaChangeList : List<Event.AlphaChange>
        {
            
        }
        public class AngleChangeList : List<Event.AngleChange>
        {
            
        }
        public class SpeedEventList : List<Event.SpeedEvent>
        {
            /// <summary>
            /// 计算 SpeedEvent 的 FloorPosition
            /// </summary>
            /// <param name="speedEvent">速度事件</param>
            /// <returns>经过更新的流速变更列表</returns>
            public new void Add(Event.SpeedEvent speedEvent)
            {
                base.Add(speedEvent);

                // 遍历每个速度事件，计算 floorPosition
                for (int i = 0; i < Count - 1; i++)
                {
                    var lastEvent = this[i];       // 上一个事件
                    var curEvent = this[i + 1];    // 当前事件

                    // 获取上一个事件的时间段
                    double lastStartTime = lastEvent.startTime;
                    double lastEndTime = lastEvent.endTime;

                    // 当前事件的开始时间
                    double curStartTime = curEvent.startTime;

                    // 计算当前事件的 floorPosition
                    curEvent.floorPosition = lastEvent.floorPosition +
                                             // 梯形积分：上一个事件的速度积分
                                             (lastEvent.endValue + lastEvent.startValue) * (lastEndTime - lastStartTime) / 2 +
                                             // 区间外线性距离：上一个事件结束到当前事件开始
                                             lastEvent.endValue * (curStartTime - lastEndTime);
                }
            }
            
            /// <summary>
            /// 获取当前时间的速度积分，计算Note和当前时间的floorPosition的主要方法
            /// </summary>
            /// <param name="time">当前时间，单位毫秒</param>
            /// <returns>从谱面开始到当前时间的总路程</returns>
            public double GetCurTimeSu(double time)
            {
                double floorPosition = 0.0;
                foreach (var speedEvent in this)
                {
                    double startTime = speedEvent.startTime;
                    double endTime = speedEvent.endTime;

                    if (time <= startTime)
                    {
                        break;
                    }

                    if (time <= endTime)
                    {
                        floorPosition += (
                            speedEvent.startValue + (speedEvent.endValue - speedEvent.startValue) * 
                            (time - startTime) / (endTime - startTime)
                            ) * (time - startTime) / 2;
                        break;
                    }

                    floorPosition += (speedEvent.startValue + speedEvent.endValue) * (endTime - startTime) / 2;
                }

                return floorPosition;
            }
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

        public NoteType type { get; set; }

        //Value
        public float x { get; set; }
        public float speedMultiplier { get; set; }

        //Special
        public bool above { get; set; }

        //Time
        public double clickStartTime { get; set; }
        public double clickEndTime { get; set; }

        //SP
        public double floorPosition { get; set; }

    }

    /// <summary>
    /// 判定线
    /// </summary>
    public class JudgeLine
    {
        //Event List
        public List<Event.XMove> xMoveList { get; set; } = new();
        public List<Event.YMove> yMoveList { get; set; } = new();
        public List<Event.AlphaChange> alphaChangeList { get; set; } = new();
        public List<Event.AngleChange> angleChangeList { get; set; } = new();
        public EventList.SpeedEventList speedChangeList { get; set; } = new();

        //Note List
        public List<Note> noteList { get; set; } = new();
    }
}