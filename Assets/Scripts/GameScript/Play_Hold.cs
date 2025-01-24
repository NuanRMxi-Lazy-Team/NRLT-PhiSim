using RePhiEdit;
using UnityEngine;

public class Play_Hold : MonoBehaviour
{
    // 获得Head，Body，End的GameObject
    public GameObject head;
    public GameObject body;
    public GameObject end;
    
    // 初始化的时候应该有的一些东西
    public RpeClass.Note Note;
    [HideInInspector]
    public Play_GameManager gameManager;
    public RectTransform noteRectTransform;
    public RectTransform headRectTransform;
    public RectTransform bodyRectTransform;
    public RectTransform endRectTransform;
    [HideInInspector]
    public JudgeLineScript fatherJudgeLine;
    
    public HitFx HitFx;
    private GameObject _canvas;
    
    private bool _isHolding;
    
    private Renderer _headRenderer;
    private Renderer _bodyRenderer;
    private Renderer _endRenderer;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 使得noteRectTransform的旋转和fatherJudgeLine的旋转一致
        noteRectTransform.transform.rotation = fatherJudgeLine.rectTransform.rotation;
        if (Note.Above == 2)
        {
            //翻转三个GameObject的贴图
            head.transform.Rotate(0, 0, 180);
            body.transform.Rotate(0, 0, 180);
            end.transform.Rotate(0, 0, 180);
        }
        _canvas = GameObject.Find("Play Canvas");
        
        bool debugMode = PlayerPrefs.GetInt("debugMode") == 1;
        head.GetComponent<SpriteRenderer>().maskInteraction =
            debugMode ? SpriteMaskInteraction.None : SpriteMaskInteraction.VisibleInsideMask;
        body.GetComponent<SpriteRenderer>().maskInteraction =
            debugMode ? SpriteMaskInteraction.None : SpriteMaskInteraction.VisibleInsideMask;
        end.GetComponent<SpriteRenderer>().maskInteraction =
            debugMode ? SpriteMaskInteraction.None : SpriteMaskInteraction.VisibleInsideMask;
        _headRenderer = head.GetComponent<Renderer>();
        _bodyRenderer = body.GetComponent<Renderer>();
        _endRenderer = end.GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        // 计算位置和高度
        float yPos = CalculateYPosition(Note.StartTime.CurTime(), gameManager.curTick);
        float height = -CalcHeight();

        
        // 修改Body的高度
        body.GetComponent<SpriteRenderer>().size = new Vector2(body.GetComponent<SpriteRenderer>().size.x, height - 12f);
        if (!_isHolding)
        {
            // 设置note整体位置
            noteRectTransform.anchoredPosition = new Vector2(Note.PositionX,yPos + height / 2f);
        }
        else
        {
            noteRectTransform.anchoredPosition = new Vector2(Note.PositionX,yPos - 6f + height / 2f );
        }
        // 设置note尾位置
        endRectTransform.anchoredPosition = new Vector2(0, height / 2f);
        // 如果没有在被打击，那么更新note头位置信息，防止Null
        if (!_isHolding) headRectTransform.anchoredPosition = new Vector2(0, -height / 2f);
        
        bool isEnabled = (yPos < 0f && Note.Above != 2) || (yPos > 0f && Note.Above == 2) || (height >= 0f) || fatherJudgeLine.judgeLine.IsCover == 0;
        if(!_isHolding) _headRenderer.enabled = isEnabled;
        _bodyRenderer.enabled = isEnabled;
        _endRenderer.enabled = isEnabled;

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
            if (Note.IsFake == 0 && !_isHolding)
            {
                //生成hitFx，恒定不旋转
                var fxPos = fatherJudgeLine.CalcPositionXY(Note.StartTime.CurTime(), Note.PositionX);
                var hitFx = Instantiate(HitFx, new Vector3(), Quaternion.identity);
                hitFx.gameManager = gameManager;
                hitFx.hitType = Note.Type;
                //设置父对象为Canvas
                hitFx.transform.SetParent(_canvas.transform);
                hitFx.rectTransform.anchoredPosition = new Vector2(fxPos.Item1, fxPos.Item2);
            }
            _isHolding = true;
            Destroy(head);
        }

        if (lastTime >= Note.EndTime.CurTime())
        {
            //摧毁
            Destroy(gameObject);
            return 0;    
        }

        // 根据速度（像素/秒）计算y坐标
        // float yPosition = (float)(speed * (note.clickStartTime / 1000 - elapsedTime) * 648 * note.speedMultiplier); // 速度单位648像素/秒，648是1080 * 0.6
        // 弃用原直接计算，使用floorPos进行计算。
        // 如果音符已经被击打，为了确保仍然在线上，使用Note的FP
        var startPosition = _isHolding
            ? Note.FloorPosition
            : fatherJudgeLine.judgeLine.EventLayers.GetCurFloorPosition(lastTime);
        
        float newYPosition =
        (
            startPosition - Note.FloorPosition
        ) * Note.SpeedMultiplier;
        

        newYPosition = Note.Above == 1 ? -newYPosition : newYPosition;

        return newYPosition;
    }
    
    private float CalcHeight()
    {
        var curTick = gameManager.curTick;

        // 如果音符已经被击打，使用当前时间作为开始位置
        var startPosition = _isHolding
            ? fatherJudgeLine.judgeLine.EventLayers.GetCurFloorPosition(curTick)
            : Note.FloorPosition;

        var clickStartFloorPosition =
        (
            fatherJudgeLine.judgeLine.EventLayers.GetCurFloorPosition(curTick) -
            startPosition
        );

        var clickEndFloorPosition =
        (
            fatherJudgeLine.judgeLine.EventLayers.GetCurFloorPosition(curTick) -
            fatherJudgeLine.judgeLine.EventLayers.GetCurFloorPosition(Note.EndTime.CurTime())
        );

        return clickEndFloorPosition - clickStartFloorPosition;
    }
}
