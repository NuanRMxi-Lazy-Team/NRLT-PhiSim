using System;
using Phigros_Fanmade;
using UnityEngine;

public class Play_HoldBody : MonoBehaviour
{
    public Note note;
    public RectTransform noteRectTransform;
    public Play_JudgeLine fatherJudgeLine;
    public double playStartUnixTime;
    private bool hitEnd = false;
    private Renderer noteRenderer;
    private SpriteRenderer spriteRenderer;
    private void Start()
    {
        noteRenderer = gameObject.GetComponent<Renderer>();
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        
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
        double lastTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - playStartUnixTime;
        //实际speed = speed * speedMultiplier，单位为每一个速度单位648像素每秒，根据此公式实时演算相对于判定线的高度（y坐标）
        float height = CalcHeight(
            note.clickStartTime,
            note.clickEndTime,
            lastTime);
        
        //使用sprite renderer修改sprite绘制模式中sprite的size中的高度
        spriteRenderer.size = new Vector2(spriteRenderer.size.x, height);
        
        //计算Y位置
        float yPos = CalcYPos(height,lastTime);
        noteRectTransform.anchoredPosition = new Vector2(note.x, yPos);
    }
    
    /// <summary>
    /// 计算Y位置
    /// </summary>
    /// <param name="clickStartTime"></param>
    /// <param name="clickEndTime"></param>
    /// <param name="currentTime"></param>
    /// <returns>Sprite Height Position</returns>
    private float CalcHeight(double clickStartTime,double clickEndTime, double currentTime)
    {
        
        if (currentTime >= clickEndTime)
        {
            //摧毁自己
            Destroy(gameObject);
        }
        // 根据速度（像素/秒）计算y坐标
        //float yPosition = (float)(speed * (note.clickStartTime / 1000 - elapsedTime) * 648 * note.speedMultiplier); // 这里加入了速度单位648像素/秒，648是1080 * 0.6
        //弃用原直接计算，使用floorPos进行计算。
        float clickStartFloorPosition = (float)
            (
                fatherJudgeLine.judgeLine.speedChangeList.GetCurTimeSu(currentTime) -
                note.floorPosition
                ) * note.speedMultiplier;
        float clickEndFloorPosition = (float)
        (
            fatherJudgeLine.judgeLine.speedChangeList.GetCurTimeSu(clickEndTime) -
            note.floorPosition
        ) * note.speedMultiplier;
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
            if (note.above)
            {
                //翻转y坐标
                return -spriteHeight / 2;
            }
            else
            {
                return spriteHeight / 2;
            }
        }
        double fp = fatherJudgeLine.judgeLine.speedChangeList.GetCurTimeSu(lastTime) - note.floorPosition;
        double newYPosition = fp * note.speedMultiplier - 12 + spriteHeight / 2;//- spriteHeight/2;
        if (note.above)
        {
            //翻转y坐标
            return (float)-newYPosition;
        }
        else
        {
            return (float)newYPosition;
        }
    }
}