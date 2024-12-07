using System;
using Phigros_Fanmade;
using UnityEngine;
using UnityEngine.Serialization;

public class Play_HoldBody : MonoBehaviour
{
    [HideInInspector] public Note Note;
    [HideInInspector] public JudgeLineScript fatherJudgeLine;
    [HideInInspector] public Play_GameManager gameManager;
    
    public RectTransform noteRectTransform;
    private Renderer _noteRenderer;
    private SpriteRenderer _spriteRenderer;
    private void Start()
    {
        _noteRenderer = gameObject.GetComponent<Renderer>();
        _spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        
        noteRectTransform.transform.rotation = fatherJudgeLine.rectTransform.rotation;
        if (!Note.Above)
        {
            //翻转自身贴图
            _noteRenderer.transform.Rotate(0, 0, 180);
        }
    }

    private void Update()
    {
        //实际speed = speed * speedMultiplier，单位为每一个速度单位648像素每秒，根据此公式实时演算相对于判定线的高度（y坐标）
        float height = CalcHeight();
        
        //使用sprite renderer修改sprite绘制模式中sprite的size中的高度
        _spriteRenderer.size = new Vector2(_spriteRenderer.size.x, height);
        
        //计算Y位置
        float yPos = CalcYPos(height);
        noteRectTransform.anchoredPosition = new Vector2(Note.X, yPos);
    }
    
    /// <summary>
    /// 计算高度
    /// </summary>
    /// <returns>Sprite Height</returns>
    private float CalcHeight()
    {
        if (gameManager.curTick >= Note.clickEndTime)
        {
            //摧毁自己
            Destroy(gameObject);
        }
        // 根据速度（像素/秒）计算y坐标
        //float yPosition = (float)(speed * (note.clickStartTime / 1000 - elapsedTime) * 648 * note.speedMultiplier); // 这里加入了速度单位648像素/秒，648是1080 * 0.6
        //弃用原直接计算，使用floorPos进行计算。
        var clickStartFloorPosition = (float)
        (
            fatherJudgeLine.judgeLine.speedChangeList.GetCurTimeSu(gameManager.curTick) -
            Note.FloorPosition
        );//* note.speedMultiplier;
        var clickEndFloorPosition = (float)
        (
            fatherJudgeLine.judgeLine.speedChangeList.GetCurTimeSu(Note.clickEndTime) -
            Note.FloorPosition
        );//* note.speedMultiplier;
        
        //获得自己的sprite高度
        return -clickEndFloorPosition - -clickStartFloorPosition;
    }
    
    private float CalcYPos(float spriteHeight)
    {
        //计算自己的相对位置，因为Sprite的原点在中心，所以要除以2，并加上hold头的高度再加上hold头的位置。
        if (gameManager.curTick >= Note.clickStartTime)
        {
            return Note.Above ? -spriteHeight / 2 : spriteHeight / 2;
        }
        var fp = fatherJudgeLine.judgeLine.speedChangeList.GetCurTimeSu(gameManager.curTick) - Note.FloorPosition;
        var newYPosition = fp - 12 + spriteHeight / 2;
        return Note.Above ? (float)-newYPosition : (float)newYPosition;
    }
}