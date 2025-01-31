using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using LogWriter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RpeEasing;
using UnityEngine;
using LogType = LogWriter.LogType;

namespace RePhiEdit
{
    public struct RpeChart
    {
        // 构造
        public RpeChart(bool init = true)
        {
            BpmList = new List<RpeClass.RpeBpm>();
            Meta = new RpeClass.Meta();
            JudgeLineList = new RpeClass.JudgeLineList();
            Illustration = null;
            Music = null;
        }

        [JsonProperty("BPMList")] public static List<RpeClass.RpeBpm> BpmList;
        [JsonProperty("META")] public RpeClass.Meta Meta;
        [JsonProperty("judgeLineList")] public RpeClass.JudgeLineList JudgeLineList;

        // 模拟器私有部分
        [CanBeNull] public Sprite Illustration;
        [CanBeNull] public AudioClip Music;
    }

    public static class RpeClass
    {
        private const float RpeSpeedToOfficial = 4.5f; // RPE速度转换为官谱速度的比例

        [JsonConverter(typeof(BeatJsonConverter))]
        public struct Beat
        {
            private readonly int[] _beat;
            private float? _time;

            public Beat(int[] timeArray = null)
            {
                _beat = timeArray ?? new[] { 0, 0, 1 };
                _time = null;
            }

            // 存储单个拍的时间，格式为 [0]:[1]/[2]
            public int this[int index]
            {
                get
                {
                    if (index > 2)
                    {
                        throw new IndexOutOfRangeException();
                    }
                    return _beat[index];
                }
                set
                {
                    if (index > 2)
                    {
                        throw new IndexOutOfRangeException();
                    }
                    _beat[index] = value;
                }
            }

            public float CurBeat => (float)this[1] / this[2] + this[0];

            public float CurTime()
            {
                var bpmList = RpeChart.BpmList;
                if (_time.HasValue) return _time.Value;
                
                float totalTime = 0;
                float currentBeat = 0;
                for (int i = 0; i < bpmList.Count; i++)
                {
                    var currentBpm = bpmList[i];
                    float msPerBeat = 60000f / currentBpm.Bpm; // 每拍的毫秒数

                    // 计算到下一个BPM变化点或目标拍数的拍数
                    float endBeat = i< bpmList.Count - 1 ? Math.Min(bpmList[i + 1].StartTime.CurBeat, CurBeat) : CurBeat;

                    // 计算这段BPM下经过的拍数
                    float beatInterval = endBeat - currentBeat;

                    // 累加时间
                    totalTime += beatInterval * msPerBeat;

                    // 更新当前拍位置
                    currentBeat = endBeat;

                    // 如果已经达到目标拍数，退出循环
                    if (currentBeat >= CurBeat)
                        break;
                }

                _time = totalTime;
                return totalTime;
            }
        }

        /// <summary>
        /// BPM
        /// </summary>
        public struct RpeBpm
        {
            [JsonProperty("bpm")] public float Bpm;
            [JsonProperty("startTime")] public Beat StartTime;
        }

        /// <summary>
        /// 元数据
        /// </summary>
        public struct Meta
        {
            [JsonProperty("RPEVersion")] public int RpeVersion; // RPE版本
            [JsonProperty("background")] public string Background; // 曲绘
            [JsonProperty("charter")] public string Charter; // 谱师
            [JsonProperty("composer")] public string Composer; // 曲师
            [JsonProperty("illustration")] public string Illustration; // 曲绘画师
            [JsonProperty("level")] public string Level; // 难度
            [JsonProperty("name")] public string Name; // 曲名
            [JsonProperty("offset")] public int Offset; // 音乐偏移
            [JsonProperty("song")] public string Song; // 音乐
        }

        public class JudgeLine
        {
            public string Texture = "line.png"; // 判定线材质
            [JsonProperty("anchor")] public float[] Anchor = { 0.5f, 0.5f }; // 判定线材质锚点
            [JsonProperty("eventLayers")] public EventLayers EventLayers = new(); // 事件层
            [JsonProperty("extended")] public Extend Extended; // 扩展事件层
            [JsonProperty("father")] public int Father = -1; // 父级
            [JsonProperty("isCover")] public int IsCover = 1; // 是否遮罩（1为遮罩，0为不遮罩）
            [JsonProperty("notes")] public List<Note> Notes = new(); // note列表
            [JsonProperty("zOrder")] public int ZOrder; // Z轴顺序
            [JsonProperty("attachUI")] [CanBeNull] public string AttachUi; // 绑定UI名，当不绑定时为null
            [JsonProperty("isGif")] public bool IsGif; // 材质是否为GIF

            // 单独的方法，用于转换原始坐标系 X轴范围为 -675 ~ 675，Y轴范围为 -450 ~ 450到 1920x1080坐标系
            public void CoordinateTransformer()
            {
                // X轴：从 -675~675 直接映射到 -960~960
                // Y轴：从 -450~450 直接映射到 -540~540
                // 克隆EventLayers，避免直接修改原始数据
                
                rmd:
                for (int i = 0; i < EventLayers.Count; i++)
                {
                    if (EventLayers[i] is null)
                    {
                        EventLayers.RemoveAt(i);
                        // 防止越界，删除直至没有null
                        goto rmd;
                    }
                }
                

                EventLayers?.ForEach(eventLayer =>
                {
                    // X轴坐标转换
                    eventLayer.MoveXEvents?.ForEach(e =>
                    {
                        // 直接按比例缩放，保持原点在中心
                        e.Start *= (1920f / (675f * 2));
                        e.End *= (1920f / (675f * 2));
                    });

                    // Y轴坐标转换
                    eventLayer.MoveYEvents?.ForEach(e =>
                    {
                        // 直接按比例缩放，保持原点在中心
                        e.Start *= (1080f / (450f * 2));
                        e.End *= (1080f / (450f * 2));
                    });

                    // Alpha值转换（0~255 到 0~1）保持不变
                    eventLayer.AlphaEvents?.ForEach(e =>
                    {
                        e.Start /= 255f;
                        e.End /= 255f;
                    });
                    // 取反旋转角度
                    eventLayer.RotateEvents?.ForEach(e =>
                    {
                        e.Start = -e.Start;
                        e.End = -e.End;
                    });
                });

                // Notes的X坐标转换
                for (int i = 0; i < Notes.Count; i++)
                {
                    var note = Notes[i];
                    note.PositionX *= (1920f / (675f * 2));
                    note.YOffset *= (1080f / (450f * 2));
                    Notes[i] = note;
                }
            }
        }
        
        public class JudgeLineList : List<JudgeLine>
        {
            public (float, float) GetLinePosition(int index, float time)
            {
                // 在没有父线的情况下直接返回
                if (this[index].Father == -1) return (this[index].EventLayers.GetXAtTime(time), this[index].EventLayers.GetYAtTime(time));
                
                int fatherIndex = this[index].Father;
                // 获取父线位置
                var (fatherX, fatherY) = GetLinePosition(fatherIndex, time);
        
                // 获取当前线相对于父线的偏移量
                float offsetX = this[index].EventLayers.GetXAtTime(time);
                float offsetY = this[index].EventLayers.GetYAtTime(time);
        
                // 获取父线的角度并转换为弧度
                float angleDegrees = this[fatherIndex].EventLayers.GetAngleAtTime(time);
                float angleRadians = (angleDegrees % 360 + 360) % 360 * Mathf.PI / 180f;
        
                // 对偏移量进行旋转
                float rotatedOffsetX = (float)(offsetX * Math.Cos(angleRadians) - offsetY * Math.Sin(angleRadians));
                float rotatedOffsetY = (float)(offsetX * Math.Sin(angleRadians) + offsetY * Math.Cos(angleRadians));
        
                // 最后加上父线的位置得到最终位置
                return (fatherX + rotatedOffsetX, fatherY + rotatedOffsetY);
            }

        }

        public struct Extend
        {
            // 在构造中初始化，避免空引用
            public Extend(bool init = true)
            {
                ColorEvents = new List<ColorEvent>();
                ScaleXEvents = new EventList();
                ScaleYEvents = new EventList();
                TextEvents = new TextEventList();
            }

            [JsonProperty("colorEvents")] public List<ColorEvent> ColorEvents; // 颜色事件
            [JsonProperty("scaleXEvents")] public EventList ScaleXEvents; // X轴缩放事件
            [JsonProperty("scaleYEvents")] public EventList ScaleYEvents; // Y轴缩放事件
            [JsonProperty("textEvents")] public TextEventList TextEvents; // 文本事件
        }

        /// <summary>
        /// 单个事件层
        /// </summary>
        public class EventLayer
        {
            [JsonProperty("moveXEvents")] public EventList MoveXEvents = new(); // 移动事件
            [JsonProperty("moveYEvents")] public EventList MoveYEvents = new(); // 移动事件
            [JsonProperty("rotateEvents")] public EventList RotateEvents = new(); // 旋转事件
            [JsonProperty("alphaEvents")] public EventList AlphaEvents = new(); // 透明度事件
            [JsonProperty("speedEvents")] public SpeedEventList SpeedEvents = new(); // 速度事件
        }

        /// <summary>
        /// 事件层列表
        /// </summary>
        public class EventLayers : List<EventLayer>
        {
            public float GetXAtTime(float t) =>
                this.Sum(eventLayer => eventLayer.MoveXEvents.GetValueAtTime(t));

            public float GetYAtTime(float t) =>
                this.Sum(eventLayer => eventLayer.MoveYEvents.GetValueAtTime(t));

            public float GetAngleAtTime(float t) =>
                this.Sum(eventLayer => eventLayer.RotateEvents.GetValueAtTime(t));

            public float GetAlphaAtTime(float t) =>
                this.Sum(eventLayer => eventLayer.AlphaEvents.GetValueAtTime(t));

            public float GetCurFloorPosition(float t) =>
                this.Sum(eventLayer => eventLayer.SpeedEvents.GetCurTimeSu(t));
        }


        /// <summary>
        /// 普通事件
        /// </summary>
        public class Event
        {
            [JsonProperty("bezier")] public int Bezier;                                 // 是否为贝塞尔曲线
            [JsonProperty("bezierPoints")] public float[] BezierPoints = new float[4];  // 贝塞尔曲线点
            [JsonProperty("easingLeft")] public float EasingLeft;                       // 缓动开始
            [JsonProperty("easingRight")] public float EasingRight = 1.0f;              // 缓动结束
            [JsonProperty("easingType")] public int EasingType = 1;                     // 缓动类型
            [JsonProperty("start")] public float Start;                                 // 开始值
            [JsonProperty("end")] public float End;                                     // 结束值
            [JsonProperty("startTime")] public Beat StartTime;                          // 开始时间
            [JsonProperty("endTime")] public Beat EndTime;                              // 结束时间

            public float GetValueAtTime(float time)
            {
                float startTime = StartTime.CurTime();
                float endTime = EndTime.CurTime();
                //获得这个拍在这个事件的时间轴上的位置
                float t = (time - startTime) / (endTime - startTime);
                //获得当前拍的值
                float easedBeat = Easing.Evaluate(EasingType, EasingLeft, EasingRight, t);
                //插值
                return Mathf.LerpUnclamped(Start, End, easedBeat);
            }
        }

        public class EventList : List<Event>
        {
            private int _lastIndex;

            public float GetValueAtTime(float t)
            {
                for (int i = _lastIndex; i < Count; i++)
                {
                    var e = this[i];
                    if (t >= e.StartTime.CurTime() && t <= e.EndTime.CurTime())
                    {
                        _lastIndex = i;
                        return e.GetValueAtTime(t);
                    }

                    if (t < e.StartTime.CurTime())
                    {
                        break;
                    }
                }

                var previousEvent = FindLast(e => t > e.EndTime.CurTime());
                return previousEvent?.End ?? 0;
            }
        }

        public class SpeedEventList : List<SpeedEvent>
        {
            public void CalcFloorPosition(List<RpeBpm> bpmList)
            {
                // 将所有速度事件的值除以RpeSpeedToOfficial
                foreach (SpeedEvent speedEvent in this)
                {
                    speedEvent.Start /= RpeSpeedToOfficial;
                    speedEvent.End /= RpeSpeedToOfficial;
                    //再除以1.5f
                    speedEvent.Start /= 1.5f;
                    speedEvent.End /= 1.5f;
                }

                foreach (SpeedEvent lastEvent in this)
                {
                    int i = IndexOf(lastEvent);
                    if (i == Count - 1) break;
                    var curEvent = this[i + 1];

                    float lastStartTime = lastEvent.StartTime.CurTime();
                    float lastEndTime = lastEvent.EndTime.CurTime();

                    float curStartTime = curEvent.StartTime.CurTime();


                    curEvent.FloorPosition +=
                        lastEvent.FloorPosition +
                        (lastEvent.End + lastEvent.Start) * (lastEndTime - lastStartTime) / 2 +
                        lastEvent.End * (curStartTime - lastEndTime) / 1;
                }
            }

            /// <summary>
            /// 获取当前时间的速度积分，计算Note和当前时间的floorPosition的主要方法
            /// </summary>
            /// <param name="time">当前时间，单位毫秒</param>
            /// <returns>从谱面开始到当前时间的总路程</returns>
            public float GetCurTimeSu(float time)
            {
                var floorPosition = 0.0f;
                foreach (SpeedEvent speedEvent in this)
                {
                    var startTime = speedEvent.StartTime.CurTime();
                    var endTime = speedEvent.EndTime.CurTime();

                    var i = IndexOf(speedEvent);
                    if (Mathf.Abs(time - speedEvent.StartTime.CurTime()) < 1e-5)
                    {
                        floorPosition += speedEvent.FloorPosition;
                        break;
                    }

                    if (time <= speedEvent.EndTime.CurTime())
                    {
                        floorPosition += speedEvent.FloorPosition +
                                         (speedEvent.Start + (speedEvent.End - speedEvent.Start) *
                                          (time - startTime) / (endTime - startTime) +
                                          speedEvent.Start) * (time - startTime) / 2;
                        break;
                    }

                    if (Count - 1 != i && !(time <= this[i + 1].StartTime.CurTime())) continue;
                    floorPosition += speedEvent.FloorPosition +
                                     (speedEvent.End + speedEvent.Start) * (endTime - startTime) / 2 +
                                     speedEvent.End * (time - endTime) / 1;
                    break;
                }

                return floorPosition;
            }
        }

        public class SpeedEvent : Event
        {
            public float FloorPosition;
        }

        public class ColorEvent : Event
        {
            [JsonProperty("start")] public new float[] Start = { 0.0f, 0.0f, 0.0f }; // 开始颜色(RGB)

            [JsonProperty("end")] public new float[] End = { 0.0f, 0.0f, 0.0f }; // 结束颜色(RGB)

            // 覆写GetValue方法，返回三个颜色值
            public new float[] GetValueAtTime(float time)
            {
                float startBeat = StartTime.CurBeat;
                float endBeat = EndTime.CurBeat;
                //获得这个拍在这个事件的时间轴上的位置
                float t = (time - startBeat) / (endBeat - startBeat);
                //获得当前拍的值
                float easedBeat = Easing.Evaluate(EasingType, EasingLeft, EasingRight, t);
                //插值，RGB三个颜色值
                return new[]
                {
                    Mathf.LerpUnclamped(Start[0], End[0], easedBeat),
                    Mathf.LerpUnclamped(Start[1], End[1], easedBeat),
                    Mathf.LerpUnclamped(Start[2], End[2], easedBeat)
                };
            }
        }

        public class TextEventList : List<TextEvent>
        {
            private int _lastIndex;

            public string GetValueAtTime(float t)
            {
                for (int i = _lastIndex; i < Count; i++)
                {
                    var e = this[i];
                    if (t >= e.StartTime.CurTime() && t <= e.EndTime.CurTime())
                    {
                        _lastIndex = i;
                        return e.GetValueAtTime(t);
                    }

                    if (t < e.StartTime.CurTime())
                    {
                        break;
                    }
                }

                var previousEvent = FindLast(e => t > e.EndTime.CurTime());
                return previousEvent?.End ?? "";
            }
        }

        public class TextEvent : Event
        {
            [JsonProperty("start")] public new string Start = ""; // 开始文本

            [JsonProperty("end")] public new string End = ""; // 结束文本

            // 覆写GetValue方法，返回抛出异常
            public new string GetValueAtTime(float t)
            {
                return End;
                // TODO: 文字事件插值
                // throw new NotImplementedException();
            }
        }

        public struct Note
        {
            // 结构体初始化，避免空引用，以下原本是class的属性
            public Note(bool init = true)
            {
                StartTime = new();
                EndTime = new();
                Alpha = 255;
                Above = 1;
                IsFake = 0;
                PositionX = 0.0f;
                Size = 1.0f;
                SpeedMultiplier = 1.0f;
                Type = 1;
                VisibleTime = 999999.0000f;
                YOffset = 0.0f;
                FloorPosition = 0.0f;
                HitSound = null;
            }

            [JsonProperty("above")] public int Above; // 是否在判定线上方（1为上方，2为下方）
            [JsonProperty("alpha")] public int Alpha; // 透明度，255为不透明，0为透明
            [JsonProperty("startTime")] public Beat StartTime; // 开始时间
            [JsonProperty("endTime")] public Beat EndTime; // 结束时间
            [JsonProperty("isFake")] public int IsFake; // 是否为假note（1为假note，0为真note）
            [JsonProperty("positionX")] public float PositionX; // X坐标
            [JsonProperty("size")] public float Size; // 宽度倍率
            [JsonProperty("speed")] public float SpeedMultiplier; // 速度倍率
            [JsonProperty("type")] public int Type; // 类型（1 为 Tap、2 为 Hold、3 为 Flick、4 为 Drag）
            [JsonProperty("visibleTime")] public float VisibleTime; // 可见时间（单位为秒）
            [JsonProperty("yOffset")] public float YOffset; // Y偏移
            [JsonProperty("hitsound")] [CanBeNull] public string HitSound; // 音效
            public float FloorPosition;
        }
    }

    public class BeatJsonConverter : JsonConverter<RpeClass.Beat>
    {
        public override RpeClass.Beat ReadJson(JsonReader reader, Type objectType, RpeClass.Beat existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            JArray array = JArray.Load(reader);
            return new RpeClass.Beat(array.ToObject<int[]>());
        }

        public override void WriteJson(JsonWriter writer, RpeClass.Beat value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            for (int i = 0; i < 3; i++)
            {
                writer.WriteValue(value[i]);
            }

            writer.WriteEndArray();
        }
    }
}