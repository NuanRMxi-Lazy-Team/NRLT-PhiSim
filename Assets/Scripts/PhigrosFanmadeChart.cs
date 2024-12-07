using System.Collections.Generic;

public class PhigrosFanmadeChart
{
    class JudgeLine
    {
        public float Bpm;
        public float Offset;
        public List<MoveEvent> MoveEvents;
        public List<Event> RotateEvents;
        public List<Event> AlphaEvents;
    }
    
    class MoveEvent
    {
        public const float MinXPosition = 0;
        public const float MaxXPosition = 1;
        public const float MinYPosition = 0;
        public const float MaxYPosition = 1;
        public float StartTime;
        public float EndTime;
        public float XPositionStart;
        public float XPositionEnd;
        public float YPositionStart;
        public float YPositionEnd;
        //用于双向转换坐标系方法，提供锚点位置（枚举，左下，屏幕中心）
        public enum AnchorPosition
        {
            LeftBottom,
            Center
        }
    }
    
    class Event
    {
        public float StartTime;
        public float EndTime;
        public float StartValue;
        public float EndValue;
    }
    
    class SpeedEvent : Event
    {
        public float? FloorPosition;
    }

    class SpeedEventList : List<SpeedEvent>
    {

    }
}
