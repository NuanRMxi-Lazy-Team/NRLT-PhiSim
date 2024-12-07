using System.Collections.Generic;
using Newtonsoft.Json;

namespace PhigrosOfficial
{
    public class V3
    {
        [JsonProperty("judgeLineList")] public List<JudgeLine> JudgeLineList; //判定线列表
        
        

        public class JudgeLine
        {
            [JsonProperty("bpm")] public float Bpm;
            [JsonProperty("offset")] public float Offset;                                 // 单位为秒
            [JsonProperty("judgeLineMoveEvents")] public List<MoveEvent> MoveEvents;      // 判定线移动事件
            [JsonProperty("judgeLineRotateEvents")] public List<Event> RotateEvents;      // 判定线旋转事件
            [JsonProperty("judgeLineDisappearEvents")] public List<Event> AlphaEvents;    // 判定线不透明度事件
            
            /// <summary>
            /// 判定线移动事件（XY绑定）
            /// </summary>
            public class MoveEvent
            {
                [JsonProperty("startTime")] public float StartTime;
                [JsonProperty("endTime")] public float EndTime;
                [JsonProperty("start")] public float XPositionStart;
                [JsonProperty("end")] public float XPositionEnd;
                [JsonProperty("start2")] public float YPositionStart;
                [JsonProperty("end2")] public float YPositionEnd;
            }
            

            /// <summary>
            /// 普通事件
            /// </summary>
            public class Event
            {
                [JsonProperty("startTime")] public float StartTime;
                [JsonProperty("endTime")] public float EndTime;
                [JsonProperty("start")] public float StartValue;
                [JsonProperty("end")] public float EndValue;
            }

            public class SpeedEventList : List<SpeedEvent>
            {
                public new void Add(SpeedEvent e)
                {
                    base.Add(e);
                }
            }
            /// <summary>
            /// 判定线速度事件
            /// </summary>
            public class SpeedEvent : Event
            {
                public float? FloorPosition;
            }
        }
        
    }
    
}