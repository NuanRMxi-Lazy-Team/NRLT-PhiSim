using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.IO.Compression;
using SimpleJSON;
using System;
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
            originalTime = originalTime * 1000; //转换为毫秒
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
                int samples = (wavBytes.Length - 44) / 2; // 16-bit stereo
                AudioClip clip = AudioClip.Create("MySound", samples, 2, 44100, false);
                float[] data = new float[samples];

                int offset = 44; // WAV头部
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
                Log.Write("WavToAudioClip error:" + ex.ToString(), LogType.Error);
                return null;
            }
        }
        #endregion

        //Type
        public ChartType chartType { get; set; }

        //List
        public List<JudgeLine> judgeLineList { get; set; } = new();

        //Data
        public AudioClip music { get; set; }
        public Image Illustration { get; set; }
        public string rawChart { get; set; }

        #region 谱面转换区块
        [CanBeNull]
        public static Chart ChartConverter(byte[] fileData, string cacheFileDirectory,string FileExtension)
        {
            try
            {
                //在缓存文件夹下创建一个新的叫"ChartFileCache"的文件夹
                if (Directory.Exists(cacheFileDirectory))
                {
                    if (!Directory.Exists(cacheFileDirectory+ "\\ChartFileCache"))
                    {
                        Directory.CreateDirectory(cacheFileDirectory + "\\ChartFileCache");
                    }
                }
                cacheFileDirectory += "\\ChartFileCache";
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
                    return null;
                }

                //将文件解压
                File.WriteAllBytes(cacheFileDirectory + "\\ChartFileCache.zip",fileData);
                ZipFile.ExtractToDirectory(cacheFileDirectory + "\\ChartFileCache.zip", cacheFileDirectory);

                //检查目录下是否含有config.json，如果有，读取到内存，否则返回null
                JSONNode jsonConfig;
                if (!File.Exists(cacheFileDirectory + "/config.json"))
                {
                    Log.Write("The selected file cannot be parsed into the config. json file");
                    return null;
                }
                else
                {
                    jsonConfig = JSON.Parse(File.ReadAllText(cacheFileDirectory + "\\config.json"));
                    //检查是否含有三个必要字段，music，illustration和chart
                    if (jsonConfig["music"] == null || jsonConfig["illustration"] == null ||
                        jsonConfig["chart"] == null)
                    {
                        Log.Write("Unable to find illustrations, music, or chart files in the selected file");
                        return null;
                    }
                }

                //检查参数中的文件都是否存在，若其中一个不存在，报错并返回null
                if (!File.Exists(cacheFileDirectory + "/" + jsonConfig["music"]))
                {
                    Log.Write("Load music is Failed");
                    return null;
                }

                if (!File.Exists(cacheFileDirectory + "/" + jsonConfig["illustration"]))
                {
                    Log.Write("Load illustration is Failed");
                    return null;
                }

                if (!File.Exists(cacheFileDirectory + "/" + jsonConfig["chart"]))
                {
                    Log.Write("Load chart is Failed");
                    return null;
                }


                //读取谱面
                Chart chart = new()
                {
                    rawChart = File.ReadAllText(cacheFileDirectory + "\\" + jsonConfig["chart"])
                };
                var jsonChart = JSON.Parse(chart.rawChart);

                //载入音频和插图
                chart.music = WavToAudioClip(File.ReadAllBytes(cacheFileDirectory + "/" + jsonConfig["music"]));
                //chart.Illustration = Resources.Load<Image>(jsonConfig["illustration"]); 暂不支持

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

                            var eventEndTime = judgeLineMoveEventList[j]["endTime"] >= 99999.0
                                ? eventStartTime
                                : OfficialV3_TimeConverter(judgeLineMoveEventList[j]["endTime"], judgeLineBpm);

                            //转换与添加坐标系
                            var eventXStartValue =
                                CoordinateTransformer.TransformX(judgeLineMoveEventList[j]["start"]);
                            var eventXEndValue =
                                CoordinateTransformer.TransformX(judgeLineList["end"]);
                            var eventYStartValue =
                                CoordinateTransformer.TransformY(judgeLineMoveEventList[j]["start2"]);
                            var eventYEndValue =
                                CoordinateTransformer.TransformY(judgeLineList["end2"]);

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
                            var eventStartTime = judgeLineAngleChangeEventList[j]["statTime"] <= 0.0
                                ? 0 //超界，按0处理
                                : OfficialV3_TimeConverter(judgeLineAngleChangeEventList[j]["statTime"],
                                    judgeLineBpm); //转换T为毫秒

                            var eventEndTime = judgeLineAngleChangeEventList[j]["endTime"] >= 999999.0
                                ? eventStartTime //超界，与事件的开始时间相同
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

                            var eventEndTime = judgeLineAlphaChangeEventList[j]["endTime"] >= 99999.0
                                ? eventStartTime
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

                            double eventEndTime = judgeLineSpeedChangeEventList[j]["endTime"] >= 99999.0
                                ? eventStartTime
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
                            judgeLine.speedChangeList = Event.SpeedEvent.CalcFloorPosition(judgeLine.speedChangeList);

                        }
                        

                        bool setAbove = true;
                        setNote:

                        JSONNode noteList = setAbove
                            ? judgeLineList[i]["notesAbove"]
                            : judgeLineList[i]["notesBelow"];

                        
                        //下落Note遍历
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
                                    return null;
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
                                x = CoordinateTransformer.TransformX(noteList[j]["positionX"]),
                                speedMultiplier = noteList[j]["speed"],
                                above = setAbove
                            });
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
                return null; //未知问题结束运行
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
        SPCNRLT_V1
    }

    /// <summary>
    /// 事件
    /// </summary>
    public static class Event
    {
        /// <summary>
        /// X移动事件
        /// </summary>
        public class XMove
        {
            //Value
            public float startValue { get; set; }
            public float endValue { get; set; }

            //Time
            public double startTime { get; set; }
            public double endTime { get; set; }
        }

        /// <summary>
        /// Y移动事件
        /// </summary>
        public class YMove
        {
            //Value
            public float startValue { get; set; }
            public float endValue { get; set; }

            //Time
            public double startTime { get; set; }
            public double endTime { get; set; }
        }

        /// <summary>
        /// 透明度变化事件
        /// </summary>
        public class AlphaChange
        {
            //Value
            public float startValue { get; set; }
            public float endValue { get; set; }

            //Time
            public double startTime { get; set; }
            public double endTime { get; set; }
        }

        /// <summary>
        /// 角度变化事件
        /// </summary>
        public class AngleChange
        {
            //Value
            public float startValue { get; set; }
            public float endValue { get; set; }

            //Time
            public double startTime { get; set; }
            public double endTime { get; set; }
        }

        /// <summary>
        /// 流速变化事件
        /// </summary>
        public class SpeedEvent
        {
            //Value
            public float startValue { get; set; }
            public float endValue { get; set; }

            //Time
            public double startTime { get; set; }
            public double endTime { get; set; }

            //Special
            public double floorPosition { get; set; } = 0.0;

            /// <summary>
            /// 计算NoteFloorPosition主要方法
            /// </summary>
            /// <param name="originSpeedList"></param>
            /// <returns>经过更新的流速变更列表</returns>
            public static List<SpeedEvent> CalcFloorPosition(List<SpeedEvent> originSpeedList)
            {
                //克隆列表
                var speedList = new List<SpeedEvent>(originSpeedList);
                //抄的。感谢PhiDot项目
                foreach (SpeedEvent lastEvent in speedList)
                {
                    int i = speedList.IndexOf(lastEvent);
                    if (i == speedList.Count - 1) break;
                    var curEvent = speedList[i + 1];

                    double lastStartTime = lastEvent.startTime;
                    double lastEndTime = lastEvent.endTime;

                    double curStartTime = curEvent.startTime;


                    curEvent.floorPosition +=
                    lastEvent.floorPosition + (lastEvent.endValue + lastEvent.startValue) * (lastEndTime - lastStartTime) / 2 +
                    lastEvent.endValue * (curStartTime - lastEndTime) / 1;
                }
                return speedList;
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
    }

    /// <summary>
    /// 判定线
    /// </summary>
    public class JudgeLine
    {
        //Event List
        public List<Event.XMove> xMoveList 
        { get; set; } = new();
        public List<Event.YMove> yMoveList 
        { get; set; } = new();
        public List<Event.AlphaChange> alphaChangeList 
        { get; set; } = new();
        public List<Event.AngleChange> angleChangeList 
        { get; set; } = new();
        public List<Event.SpeedEvent> speedChangeList 
        { get; set; } = new();

        //Note List
        public List<Note> noteList 
        { get; set; } = new();
    }
}