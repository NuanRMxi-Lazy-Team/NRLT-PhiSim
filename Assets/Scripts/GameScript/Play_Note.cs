using RePhiEdit;
using UnityEngine;
using UnityEngine.Serialization;

// ReSharper disable once CheckNamespace
// ReSharper disable once InconsistentNaming
public class Play_Note : MonoBehaviour
{
    //获取初始参数
    [HideInInspector]
    public RpeClass.Note Note;
    public RectTransform noteRectTransform;
    [HideInInspector]
    public JudgeLineScript fatherJudgeLine;
    [HideInInspector]
    public Play_GameManager gameManager;
    [FormerlySerializedAs("HitFx")] public HitFx hitFx;

    private Renderer _noteRenderer;
    private GameObject _canvas;

    // Start is called before the first frame update
    private void Start()
    {
        _noteRenderer = gameObject.GetComponent<Renderer>();
        noteRectTransform.transform.rotation = fatherJudgeLine.rectTransform.rotation;
        _canvas = GameObject.Find("Play Canvas");
        if (Note.Above != 1)
        {
            //翻转自身贴图
            _noteRenderer.transform.Rotate(0, 0, 180);
        }
        // 遮罩交互调试
        bool debugMode = PlayerPrefs.GetInt("debugMode") == 1;
        gameObject.GetComponent<SpriteRenderer>().maskInteraction =
            debugMode ? SpriteMaskInteraction.None : SpriteMaskInteraction.VisibleInsideMask;
    }

    // Update is called once per frame
    private void Update()
    {
        //实际speed = speed * speedMultiplier，单位为每一个速度单位648像素每秒，根据此公式实时演算相对于判定线的高度（y坐标）
        var yPos = CalculateYPosition(Note.StartTime.CurTime(), gameManager.curTick);
        noteRectTransform.anchoredPosition = new Vector2(Note.PositionX, yPos);

        _noteRenderer.enabled = (!((yPos < 0f && Note.Above == 1) || (yPos > 0f && Note.Above != 1)) ||
                                fatherJudgeLine.judgeLine.IsCover == 0) && fatherJudgeLine.alpha >= 0f;
    }


    /// <summary>
    /// 计算垂直位置
    /// </summary>
    /// <param name="targetTime">目标时间</param>
    /// <param name="lastTime">当前时间</param>
    /// <returns>垂直位置</returns>
    private float CalculateYPosition(double targetTime, float lastTime)
    {
        if (lastTime >= targetTime)
        {
            if (Note.IsFake == 0)
            {
                //生成hitFx，恒定不旋转
                var fxPos = fatherJudgeLine.CalcPositionXY(Note.StartTime.CurTime(), Note.PositionX);
                var hitFx = Instantiate(this.hitFx, new Vector3(), Quaternion.identity);
                hitFx.gameManager = gameManager;
                hitFx.hitType = Note.Type;
                //设置父对象为Canvas
                hitFx.transform.SetParent(_canvas.transform);
                hitFx.rectTransform.anchoredPosition = new Vector2(fxPos.Item1, fxPos.Item2);
            }

            //摧毁
            Destroy(gameObject);
            return 0;
        }

        // 根据速度（像素/秒）计算y坐标
        // float yPosition = (float)(speed * (note.clickStartTime / 1000 - elapsedTime) * 648 * note.speedMultiplier); // 这里加入了速度单位648像素/秒，648是1080 * 0.6
        // 弃用原直接计算，使用floorPos进行计算。
        float newYPosition =
        (
            fatherJudgeLine.floorPosition -
            Note.FloorPosition
        ) * Note.SpeedMultiplier;

        newYPosition = Note.Above == 1 ? -newYPosition : newYPosition;

        if (lastTime > Note.EndTime.CurTime() || lastTime < targetTime)
        {
            return newYPosition;
        }

        newYPosition = -1200f;
        if (Note.Above == 1)
        {
            //翻转y坐标
            newYPosition = 1200f;
        }

        return newYPosition;
    }
}