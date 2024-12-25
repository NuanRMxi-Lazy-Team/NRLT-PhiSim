using RePhiEdit;
using UnityEngine;


public class Play_HoldHead : MonoBehaviour
{
    [HideInInspector]
    public RpeClass.Note Note;
    public RectTransform noteRectTransform;
    public JudgeLineScript fatherJudgeLine;
    public Play_GameManager gameManager;
    public HitFx hitFx;
    
    private Renderer _noteRenderer;
    private GameObject _canvas;

    private void Start()
    {
        _noteRenderer = gameObject.GetComponent<Renderer>();
        noteRectTransform.transform.rotation = fatherJudgeLine.rectTransform.rotation;

        _canvas = GameObject.Find("Play Canvas");
        if (Note.Above == 2)
        {
            //翻转自身贴图
            _noteRenderer.transform.Rotate(0, 0, 180);
        }
    }

    private void Update()
    {

        //实际speed = speed * speedMultiplier，单位为每一个速度单位648像素每秒，根据此公式实时演算相对于判定线的高度（y坐标）
        float yPos = CalculateYPosition(
            Note.StartTime.CurTime(gameManager.BpmList),
            gameManager.curTick);
        noteRectTransform.anchoredPosition = new Vector2(Note.PositionX,
            yPos);
    }

    /// <summary>
    /// 计算垂直位置
    /// </summary>
    /// <param name="targetTime">目标时间</param>
    /// <param name="lastTime">当前时间</param>
    /// <returns>垂直位置</returns>
    private float CalculateYPosition(double targetTime, float lastTime)
    {
        if (targetTime <= lastTime)
        {
            if (Note.IsFake == 0)
            {
                //生成hitFx，恒定不旋转
                var fxPos = fatherJudgeLine.CalcPositionXY(Note.StartTime.CurTime(gameManager.BpmList),Note.PositionX);
                var hitFx = Instantiate(this.hitFx, new Vector3(fxPos.Item1,fxPos.Item2), Quaternion.identity);
                hitFx.hitType = Note.Type;
                hitFx.gameManager = gameManager;
                //设置父对象为Canvas
                hitFx.transform.SetParent(_canvas.transform);
                hitFx.rectTransform.anchoredPosition = new Vector2(fxPos.Item1, fxPos.Item2);
            }
            //摧毁
            Destroy(gameObject);
            return 0;
        }

        // 根据速度（像素/秒）计算y坐标
        //float yPosition = (float)(speed * (note.clickStartTime / 1000 - elapsedTime) * 648 * note.speedMultiplier); // 这里加入了速度单位648像素/秒，648是1080 * 0.6
        //弃用原直接计算，使用floorPos进行计算。
        float newYPosition =
        (
            fatherJudgeLine.judgeLine.EventLayers.GetCurFloorPosition(lastTime,gameManager.BpmList) -
            Note.FloorPosition
        );//* note.speedMultiplier;
        float spriteHeight = _noteRenderer.bounds.size.y;
        if (Note.Above == 1)
        {
            //翻转y坐标
            //获得自己的sprite高度
            return -newYPosition + spriteHeight / 2;
        }
        return newYPosition - spriteHeight / 2;
    }
}