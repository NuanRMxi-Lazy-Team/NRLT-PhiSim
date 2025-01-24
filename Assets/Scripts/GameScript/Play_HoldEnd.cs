using RePhiEdit;
using UnityEngine;
using UnityEngine.Serialization;

public class Play_HoldEnd : MonoBehaviour
{
    public RpeClass.Note note;
    public RectTransform noteRectTransform;
    public JudgeLineScript fatherJudgeLine;
    public Play_GameManager gameManager;
    private Renderer _noteRenderer;

    private void Start()
    {
        _noteRenderer = gameObject.GetComponent<Renderer>();

        noteRectTransform.transform.rotation = fatherJudgeLine.rectTransform.rotation;
        if (note.Above == 2)
        {
            //翻转自身贴图
            _noteRenderer.transform.Rotate(0, 0, 180);
        }
    }

    private void Update()
    {
        //计算Y位置
        var yPos = CalcYPos();
        noteRectTransform.anchoredPosition = new Vector2(note.PositionX, yPos);
        if (gameManager.curTick >= note.EndTime.CurTime())
        {
            Destroy(gameObject);
        }
        /*
        if (yPos < 0f && note.Above != 2)
        {
            _noteRenderer.enabled = fatherJudgeLine.judgeLine.IsCover == 0;
        }
        else if (yPos > 0f && note.Above == 2)
        {
            _noteRenderer.enabled = fatherJudgeLine.judgeLine.IsCover == 0;
        }
        else
        {
            _noteRenderer.enabled = true;
        }
        */
        
        _noteRenderer.enabled = fatherJudgeLine.judgeLine.IsCover == 0 || (yPos >= 0f && note.Above != 2) || (yPos <= 0f && note.Above == 2);
    }


    private float CalcYPos()
    {
        var newYPosition =
        (
            fatherJudgeLine.judgeLine.EventLayers.GetCurFloorPosition(gameManager.curTick) -
            fatherJudgeLine.judgeLine.EventLayers.GetCurFloorPosition(note.EndTime.CurTime())
        ) - 6f;

        return note.Above == 1 ? -newYPosition : newYPosition;
    }
}