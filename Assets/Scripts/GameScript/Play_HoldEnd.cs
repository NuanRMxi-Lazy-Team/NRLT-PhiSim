using RePhiEdit;
using UnityEngine;
using UnityEngine.Serialization;

public class Play_HoldEnd : MonoBehaviour
{
    public RpeClass.Note note;
    public RectTransform noteRectTransform;
    public JudgeLineScript fatherJudgeLine;
    [FormerlySerializedAs("GameManager")] public Play_GameManager gameManager;
    private Renderer noteRenderer;

    private void Start()
    {
        noteRenderer = gameObject.GetComponent<Renderer>();

        noteRectTransform.transform.rotation = fatherJudgeLine.rectTransform.rotation;
        if (note.Above == 2)
        {
            //翻转自身贴图
            noteRenderer.transform.Rotate(0, 0, 180);
        }
    }

    private void Update()
    {
        //计算Y位置
        float yPos = CalcYPos();
        noteRectTransform.anchoredPosition = new Vector2(note.PositionX, yPos);
        if (gameManager.curTick >= note.EndTime.CurTime())
        {
            Destroy(gameObject);
        }
    }


    private float CalcYPos()
    {
        var newYPosition =
        (
            fatherJudgeLine.judgeLine.EventLayers.GetCurFloorPosition(gameManager.curTick) -
            fatherJudgeLine.judgeLine.EventLayers.GetCurFloorPosition(note.EndTime.CurTime())
        );

        return note.Above == 1 ? -newYPosition : newYPosition;
    }
}