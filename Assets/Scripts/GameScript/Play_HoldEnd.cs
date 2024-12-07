using System;
using Phigros_Fanmade;
using UnityEngine;

public class Play_HoldEnd : MonoBehaviour
{
    public Note note;
    public RectTransform noteRectTransform;
    public JudgeLineScript fatherJudgeLine;
    [HideInInspector]
    public double playOffset;
    public Play_GameManager GameManager;
    private bool hitEnd = false;
    private Renderer noteRenderer;
    private void Start()
    {
        noteRenderer = gameObject.GetComponent<Renderer>();
        
        noteRectTransform.transform.rotation = fatherJudgeLine.rectTransform.rotation;
        if (!note.Above)
        {
            //翻转自身贴图
            noteRenderer.transform.Rotate(0, 0, 180);
        }
    }

    private void Update()
    {
        if (hitEnd) return;
        //实际speed = speed * speedMultiplier，单位为每一个速度单位648像素每秒，根据此公式实时演算相对于判定线的高度（y坐标）
        float height = CalcHeight(
            note.clickStartTime,
            note.clickEndTime,
            GameManager.curTick);
        
        //计算Y位置
        float yPos = CalcYPos(height,GameManager.curTick);
        noteRectTransform.anchoredPosition = new Vector2(note.X, yPos);
    }
    
    /// <summary>
    /// 计算高度
    /// </summary>
    /// <param name="clickStartTime">打击开始时间</param>
    /// <param name="clickEndTime">打击结束时间</param>
    /// <param name="currentTime">当前时间</param>
    /// <returns>Sprite Height</returns>
    private float CalcHeight(double clickStartTime,double clickEndTime, double currentTime)
    {
        
        if (currentTime >= clickEndTime) 
            Destroy(gameObject);
        
        // 根据速度（像素/秒）计算y坐标
        //float yPosition = (float)(speed * (note.clickStartTime / 1000 - elapsedTime) * 648 * note.speedMultiplier); // 这里加入了速度单位648像素/秒，648是1080 * 0.6
        //弃用原直接计算，使用floorPos进行计算。
        float clickStartFloorPosition = (float)
        (
            fatherJudgeLine.judgeLine.speedChangeList.GetCurTimeSu(currentTime) -
            note.FloorPosition
        );//* note.speedMultiplier;
        float clickEndFloorPosition = (float)
        (
            fatherJudgeLine.judgeLine.speedChangeList.GetCurTimeSu(clickEndTime) -
            note.FloorPosition
        );//* note.speedMultiplier;
        float spriteHeight = -clickEndFloorPosition - -clickStartFloorPosition;
        
        //获得自己的sprite高度
        return spriteHeight;

    }
    
    private float CalcYPos(float spriteHeight,double lastTime)
    {
        //double clickTime = note.clickEndTime - note.clickStartTime;
        //计算自己的相对位置，因为Sprite的原点在中心，所以要除以2，并加上hold头的高度再加上hold头的位置。
        if (lastTime >= note.clickStartTime)
        {
            return note.Above ? -spriteHeight : spriteHeight;
        } 
        
        double fp = fatherJudgeLine.judgeLine.speedChangeList.GetCurTimeSu(lastTime) - note.FloorPosition;
        //double newYPosition = fp * note.speedMultiplier - 12 + spriteHeight / 2;//- spriteHeight/2;
        double newYPosition = fp - 12 + spriteHeight;
        
        return note.Above ? (float)-newYPosition : (float)newYPosition;
    }
}