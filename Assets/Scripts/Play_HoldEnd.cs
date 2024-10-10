using System;
using Phigros_Fanmade;
using UnityEngine;

public class Play_HoldEnd : MonoBehaviour
{
    public Note note;
    public RectTransform noteRectTransform;
    public Play_JudgeLine fatherJudgeLine;
    [HideInInspector]
    public double playOffset;
    public Play_GameManager GameManager;
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
            GameManager.curTick);
        noteRectTransform.anchoredPosition = new Vector2(note.x,
            yPos);
    }

    /// <summary>
    /// 计算垂直位置
    /// </summary>
    /// <param name="targetTime">目标时间</param>
    /// <param name="lastTime">当前时间</param>
    /// <returns>垂直位置</returns>
    private float CalculateYPosition(double targetTime, double lastTime)
    {
        double clickTime = note.clickEndTime - note.clickStartTime;
        if (lastTime >= targetTime)
        {
            //摧毁自己
            Destroy(gameObject);
        }

        // 根据速度（像素/秒）计算y坐标
        //float yPosition = (float)(speed * (note.clickStartTime / 1000 - elapsedTime) * 648 * note.speedMultiplier); // 这里加入了速度单位648像素/秒，648是1080 * 0.6
        //弃用原直接计算，使用floorPos进行计算。
        float newYPosition = (float)
        (
            fatherJudgeLine.judgeLine.speedChangeList.GetCurTimeSu(lastTime - clickTime) -
            note.holdEndFloorPosition
        ) * note.speedMultiplier;
        if (note.above)
        {
            //翻转y坐标
            return -newYPosition - 12;
        }

        return newYPosition + 12;
    }
}