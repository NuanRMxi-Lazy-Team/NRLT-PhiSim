using System;
using PhigrosFanmade;
using RePhiEdit;
using UnityEngine;
using UnityEngine.Serialization;

public class Play_HoldBody : MonoBehaviour
{
    [HideInInspector] public RpeClass.Note Note;
    [HideInInspector] public JudgeLineScript fatherJudgeLine;
    [HideInInspector] public Play_GameManager gameManager;

    public RectTransform noteRectTransform;
    private Renderer _noteRenderer;
    private SpriteRenderer _spriteRenderer;
    private bool _isHolding;

    private void Start()
    {
        _noteRenderer = gameObject.GetComponent<Renderer>();
        _spriteRenderer = gameObject.GetComponent<SpriteRenderer>();

        noteRectTransform.transform.rotation = fatherJudgeLine.rectTransform.rotation;
        if (Note.Above == 2)
        {
            //翻转自身贴图
            _noteRenderer.transform.Rotate(0, 0, 180);
        }
    }

    private void Update()
    {
        //实际speed = speed * speedMultiplier，单位为每一个速度单位648像素每秒，根据此公式实时演算相对于判定线的高度（y坐标）
        float height = CalcHeight();
        if (gameManager.curTick >= Note.StartTime.CurTime())
        {
            _isHolding = true;
        }

        //使用sprite renderer修改sprite绘制模式中sprite的size中的高度
        _spriteRenderer.size = new Vector2(_spriteRenderer.size.x, height);

        //计算Y位置
        float yPos = CalcYPos(height);
        noteRectTransform.anchoredPosition = new Vector2(Note.PositionX, yPos);
    }

    private float CalcHeight()
    {
        if (gameManager.curTick >= Note.EndTime.CurTime())
        {
            Destroy(gameObject);
            return 0;
        }

        float startPosition;
        // 如果音符已经被击打，使用当前时间作为开始位置
        startPosition = _isHolding
            ? fatherJudgeLine.judgeLine.EventLayers.GetCurFloorPosition(gameManager.curTick)
            : Note.FloorPosition;

        var clickStartFloorPosition =
        (
            fatherJudgeLine.judgeLine.EventLayers.GetCurFloorPosition(gameManager.curTick) -
            startPosition
        );

        var clickEndFloorPosition =
        (
            fatherJudgeLine.judgeLine.EventLayers.GetCurFloorPosition(gameManager.curTick) -
            fatherJudgeLine.judgeLine.EventLayers.GetCurFloorPosition(Note.EndTime.CurTime())
        );

        return clickEndFloorPosition - clickStartFloorPosition;
    }

    private float CalcYPos(float spriteHeight)
    {
        //计算自己的相对位置，因为Sprite的原点在中心，所以要除以2，并加上hold头的高度再加上hold头的位置。
        if (gameManager.curTick >= Note.StartTime.CurTime())
        {
            return Note.Above == 1 ? -spriteHeight / 2 : spriteHeight / 2;
        }

        var fp = fatherJudgeLine.judgeLine.EventLayers.GetCurFloorPosition(gameManager.curTick) - Note.FloorPosition;
        var newYPosition = fp - 12 + spriteHeight / 2;
        return Note.Above == 1 ? (float)-newYPosition : (float)newYPosition;
    }
}