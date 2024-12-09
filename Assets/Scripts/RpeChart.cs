using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RpeEasing;
using UnityEngine;

namespace RePhiEdit
{
    public class RpeChart
    {
        [JsonProperty("BPMList")] public List<RpeClass.RpeBpm> BpmList = new();
        [JsonProperty("META")] public RpeClass.Meta Meta = new();
        [JsonProperty("judgeLineList")] public List<RpeClass.JudgeLine> JudgeLineList = new();
        
        // 模拟器私有部分
        public Sprite Illustration;
        public AudioClip Music;
        
    }

    public static class RpeClass
    {
        public const float RpeSpeedToOfficial = 4.5f; // RPE速度转换为官谱速度的比例

        [JsonConverter(typeof(BeatJsonConverter))]
        public class Beat
        {
            private readonly int[] _time;

            public Beat(int[] timeArray = null)
            {
                _time = timeArray ?? new[] { 0, 0, 0 };
            }

            // 存储单个拍的时间，格式为 [0]:[1]/[2]，所以需要声明索引器，并确保不会越界，超过2抛出异常
            public int this[int index]
            {
                get
                {
                    if (index > 2)
                    {
                        throw new System.IndexOutOfRangeException();
                    }

                    return _time[index];
                }
                set
                {
                    if (index > 2)
                    {
                        throw new System.IndexOutOfRangeException();
                    }

                    _time[index] = value;
                }
            }

            public float CurBeat => (float)this[1] / this[2] + this[0];

            public float CurTime(List<RpeBpm> bpmList)
            {
                float sec = 0.0f;
                float t = CurBeat;
                foreach (var e in bpmList)
                {
                    float bpmv = e.Bpm;
                    if (e != bpmList.Last())
                    {
                        float etBeat = e.StartTime.CurBeat - e.StartTime.CurBeat;
                        if (t >= etBeat)
                        {
                            sec += etBeat * (60 / bpmv);
                            t -= etBeat;
                        }
                        else
                        {
                            sec += t * (60 / bpmv);
                            break;
                        }
                    }
                    else
                    {
                        sec += t * (60 / bpmv);
                    }
                }

                return sec * 1000;
            }
        }

        /// <summary>
        /// BPM
        /// </summary>
        public class RpeBpm
        {
            [JsonProperty("bpm")] public float Bpm;
            [JsonProperty("startTime")] public Beat StartTime;
        }

        /// <summary>
        /// 元数据
        /// </summary>
        public class Meta
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
            [JsonProperty("extended")] public Extend Extended = new(); // 扩展事件层
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
    
                EventLayers.ForEach(eventLayer =>
                {
                    // X轴坐标转换
                    eventLayer.MoveXEvents.ForEach(e =>
                    {
                        // 直接按比例缩放，保持原点在中心
                        e.Start = e.Start * (1920f / (675f * 2));
                        e.End = e.End * (1920f / (675f * 2));
                    });

                    // Y轴坐标转换
                    eventLayer.MoveYEvents.ForEach(e =>
                    {
                        // 直接按比例缩放，保持原点在中心
                        e.Start = e.Start * (1080f / (450f * 2));
                        e.End = e.End * (1080f / (450f * 2));
                    });

                    // Alpha值转换（0~255 到 0~1）保持不变
                    eventLayer.AlphaEvents.ForEach(e =>
                    {
                        e.Start /= 255f;
                        e.End /= 255f;
                    });
                    // 取反旋转角度
                    eventLayer.RotateEvents.ForEach(e =>
                    {
                        e.Start = -e.Start;
                        e.End = -e.End;
                    });
                });

                // Notes的X坐标转换
                Notes.ForEach(note =>
                {
                    note.PositionX = note.PositionX * (1920f / (675f * 2));
                    note.YOffset = note.YOffset * (1080f / (450f * 2));
                });
            }
        }

        public class Extend
        {
            [JsonProperty("colorEvents")] public List<ColorEvent> ColorEvents; // 颜色事件
            [JsonProperty("scaleXEvents")] public EventList ScaleXEvents; // X轴缩放事件
            [JsonProperty("scaleYEvents")] public EventList ScaleYEvents; // Y轴缩放事件
            [JsonProperty("textEvents")] public List<TextEvent> TextEvents; // 文本事件
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
            public float GetXAtTime(float t, List<RpeBpm> bpmList) =>
                this.Sum(eventLayer => eventLayer.MoveXEvents.GetValueAtTime(t, bpmList));

            public float GetYAtTime(float t, List<RpeBpm> bpmList) =>
                this.Sum(eventLayer => eventLayer.MoveYEvents.GetValueAtTime(t, bpmList));

            public float GetAngleAtTime(float t, List<RpeBpm> bpmList) =>
                this.Sum(eventLayer => eventLayer.RotateEvents.GetValueAtTime(t, bpmList));

            public float GetAlphaAtTime(float t, List<RpeBpm> bpmList) =>
                this.Sum(eventLayer => eventLayer.AlphaEvents.GetValueAtTime(t, bpmList));

            public float GetCurFloorPosition(float t, List<RpeBpm> bpmList) =>
                this.Sum(eventLayer => eventLayer.SpeedEvents.GetCurTimeSu(t, bpmList));
        }


        /// <summary>
        /// 普通事件
        /// </summary>
        public class Event
        {
            [JsonProperty("bezier")] public int Bezier; // 是否为贝塞尔曲线
            [JsonProperty("bezierPoints")] public float[] BezierPoints = { 0.0f, 0.0f, 0.0f, 0.0f }; // 贝塞尔曲线点
            [JsonProperty("easingLeft")] public float EasingLeft; // 缓动开始
            [JsonProperty("easingRight")] public float EasingRight = 1.0f; // 缓动结束
            [JsonProperty("easingType")] public int EasingType = 1; // 缓动类型
            [JsonProperty("start")] public float Start; // 开始值
            [JsonProperty("end")] public float End; // 结束值
            [JsonProperty("startTime")] public Beat StartTime = new(); // 开始时间
            [JsonProperty("endTime")] public Beat EndTime = new(); // 结束时间

            public float GetValueAtTime(float time, List<RpeBpm> bpmList)
            {
                float startTime = StartTime.CurTime(bpmList);
                float endTime = EndTime.CurTime(bpmList);
                //获得这个拍在这个事件的时间轴上的位置
                float t = (time - startTime) / (endTime - startTime);
                //获得当前拍的值
                float easedBeat = Easing.Evaluate(EasingType, EasingLeft, EasingRight, t);
                //插值
                return Mathf.Lerp(Start, End, easedBeat);
            }
        }

        public class EventList : List<Event>
        {
            public float GetValueAtTime(float t, List<RpeBpm> bpmList)
            {
                Event previousEvent = null;

                for (int i = 0; i < Count; i++)
                {
                    var theEvent = this[i];
                    if (t >= theEvent.StartTime.CurTime(bpmList) && t <= theEvent.EndTime.CurTime(bpmList))
                    {
                        return theEvent.GetValueAtTime(t, bpmList);
                    }

                    if (t <= theEvent.StartTime.CurTime(bpmList))
                    {
                        break;
                    }

                    previousEvent = theEvent;
                }

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

                    float lastStartTime = lastEvent.StartTime.CurTime(bpmList);
                    float lastEndTime = lastEvent.EndTime.CurTime(bpmList);

                    float curStartTime = curEvent.StartTime.CurTime(bpmList);


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
            /// <param name="bpmList">BPM列表</param>
            /// <returns>从谱面开始到当前时间的总路程</returns>
            public float GetCurTimeSu(float time, List<RpeBpm> bpmList)
            {
                var floorPosition = 0.0f;
                foreach (SpeedEvent speedEvent in this)
                {
                    var startTime = speedEvent.StartTime.CurTime(bpmList);
                    var endTime = speedEvent.EndTime.CurTime(bpmList);

                    var i = IndexOf(speedEvent);
                    if (Mathf.Abs(time - speedEvent.StartTime.CurTime(bpmList)) < 1e-5)
                    {
                        floorPosition += speedEvent.FloorPosition;
                        break;
                    }

                    if (time <= speedEvent.EndTime.CurTime(bpmList))
                    {
                        floorPosition += speedEvent.FloorPosition +
                                         (speedEvent.Start + (speedEvent.End - speedEvent.Start) *
                                          (time - startTime) / (endTime - startTime) +
                                          speedEvent.Start) * (time - startTime) / 2;
                        break;
                    }

                    if (Count - 1 != i && !(time <= this[i + 1].StartTime.CurTime(bpmList))) continue;
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
            public new float[] GetValueAtBeat(float beat)
            {
                float startBeat = StartTime.CurBeat;
                float endBeat = EndTime.CurBeat;
                //获得这个拍在这个事件的时间轴上的位置
                float t = (beat - startBeat) / (endBeat - startBeat);
                //获得当前拍的值
                float easedBeat = Easing.Evaluate(EasingType, EasingLeft, EasingRight, t);
                //插值，RGB三个颜色值
                return new[]
                {
                    Mathf.Lerp(Start[0], End[0], easedBeat),
                    Mathf.Lerp(Start[1], End[1], easedBeat),
                    Mathf.Lerp(Start[2], End[2], easedBeat)
                };
            }
        }

        public class TextEvent : Event
        {
            [JsonProperty("start")] public new string Start = ""; // 开始文本

            [JsonProperty("end")] public new string End = ""; // 结束文本

            // 覆写GetValue方法，返回抛出异常
            public new string GetValueAtBeat(float t)
            {
                throw new System.NotImplementedException();
            }
        }

        public class Note
        {
            [JsonProperty("above")] public int Above = 1; // 是否在判定线上方（1为上方，2为下方）
            [JsonProperty("alpha")] public int Alpha = 255; // 透明度，255为不透明，0为透明
            [JsonProperty("startTime")] public Beat StartTime = new(); // 开始时间
            [JsonProperty("endTime")] public Beat EndTime = new(); // 结束时间
            [JsonProperty("isFake")] public int IsFake; // 是否为假note（1为假note，0为真note）
            [JsonProperty("positionX")] public float PositionX; // X坐标
            [JsonProperty("size")] public float Size = 1.0f; // 宽度倍率
            [JsonProperty("speed")] public float SpeedMultiplier = 1.0f; // 速度倍率
            [JsonProperty("type")] public int Type = 1; // 类型（1 为 Tap、2 为 Hold、3 为 Flick、4 为 Drag）
            [JsonProperty("visibleTime")] public float VisibleTime = 999999.0000f; // 可见时间（单位为秒）
            [JsonProperty("yOffset")] public float YOffset; // Y偏移
            [JsonProperty("hitsound")] [CanBeNull] public string HitSound; // 音效
            public float FloorPosition = 0.0f;
        }
    }
    
    public class BeatJsonConverter : JsonConverter<RpeClass.Beat>
    {
        public override RpeClass.Beat ReadJson(JsonReader reader, Type objectType, RpeClass.Beat existingValue, bool hasExistingValue, JsonSerializer serializer)
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