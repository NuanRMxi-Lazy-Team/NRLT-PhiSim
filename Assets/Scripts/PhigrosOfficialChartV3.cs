using System.Collections.Generic;
using Newtonsoft.Json;

namespace PhigrosOfficial
{
    public class V3
    {
        public List<JudgeLine> judgeLineList { get; set; } //判定线列表
        







        public class JudgeLine
        {
            public float bpm { get; set; }
            public float offset { get; set; }//单位为秒
            [JsonProperty("judgeLineMoveEvents")]
            public List<MoveEvent> moveEvents { get; set; }
            [JsonProperty("judgeLineRotateEvents")]
            public List<RotateEvent> rotateEvents { get; set; }
            [JsonProperty("judgeLineDisappearEvents")]
            public List<AlphaEvent> alphaEvents { get; set; }
            
            /// <summary>
            /// 判定线移动事件（XY绑定）
            /// </summary>
            public class MoveEvent
            {
                public float startTime { get; set; }
                public float endTime { get; set; }
                [JsonProperty("start")]
                public float xPositionStart { get; set; }
                [JsonProperty("end")]
                public float xPositionEnd { get; set; }
                [JsonProperty("start2")]
                public float yPositionStart { get; set; }
                [JsonProperty("end2")]
                public float yPositionEnd { get; set; }
            }
            
            /// <summary>
            /// 判定线旋转事件
            /// </summary>
            public class RotateEvent
            { 
                public float startTime { get; set; }
                public float endTime { get; set; }
                [JsonProperty("start")]
                public float startValue { get; set; }
                [JsonProperty("end")]
                public float endValue { get; set; }
            }

            /// <summary>
            /// 判定线不透明度事件
            /// </summary>
            public class AlphaEvent
            {
                public float startTime { get; set; }
                public float endTime { get; set; }
                [JsonProperty("start")]
                public float startValue { get; set; }
                [JsonProperty("end")]
                public float endValue { get; set; }
            }
            
            public class SpeedEvent
            {
                public float startTime { get; set; }
                public float endTime { get; set; }
                [JsonProperty("start")]
                public float startValue { get; set; }
                [JsonProperty("end")]
                public float endValue { get; set; }
            }
        }
        
    }
    
}