using System.Collections;
using System;
using Phigros_Fanmade;
using UnityEngine;
using UnityEngine.Serialization;

public class Play_HoldEnd : MonoBehaviour
{
    public Note note;
    public RectTransform noteRectTransform;
    public Play_JudgeLine fatherJudgeLine;
    public double playStartUnixTime;
    private bool hitEnd = false;
    private Renderer noteRenderer;

    private void Start()
    {
        noteRenderer = gameObject.GetComponent<Renderer>();
        noteRectTransform.transform.rotation = fatherJudgeLine.rectTransform.rotation;
        if (!note.above)
        {
            //翻转自身贴图
            noteRenderer.transform.Rotate(0, 0, 180);
        }
    }

    private void Update()
    {
        if (hitEnd) return;
        //实际speed = speed * speedMultiplier，单位为每一个速度单位648像素每秒，根据此公式实时演算相对于判定线的高度（y坐标）
        float yPos = CalculateYPosition(
            note.clickEndTime,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - playStartUnixTime);
        noteRectTransform.anchoredPosition = new Vector2(note.x,
            yPos);
    }
    
    /// <summary>
    /// 计算Y位置
    /// </summary>
    /// <param name="targetTime"></param>
    /// <param name="currentTime"></param>
    /// <returns>Y Position</returns>
    private float CalculateYPosition(double targetTime, double currentTime)
    {
        double clickTime = note.clickEndTime - note.clickStartTime;
        if (currentTime >= targetTime)
        {
            //摧毁自己
            Destroy(gameObject);
        }
        // 根据速度（像素/秒）计算y坐标
        //float yPosition = (float)(speed * (note.clickStartTime / 1000 - elapsedTime) * 648 * note.speedMultiplier); // 这里加入了速度单位648像素/秒，648是1080 * 0.6
        //弃用原直接计算，使用floorPos进行计算。
        float newYPosition = (float)
            (
                fatherJudgeLine.judgeLine.speedChangeList.GetCurTimeSu(currentTime - clickTime) -
                note.floorPosition
                ) * note.speedMultiplier;
        if (note.above)
        {
            //翻转y坐标
            //获得自己的sprite高度
            
            return -newYPosition - 12;
        }
        else
        {
            return newYPosition + 12;
        }
    }
}