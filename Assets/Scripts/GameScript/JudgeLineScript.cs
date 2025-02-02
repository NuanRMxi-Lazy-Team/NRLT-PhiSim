using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using RePhiEdit;
using TMPro;

public class JudgeLineScript : MonoBehaviour
{
    //获取初始参数
    [HideInInspector]
    public RpeClass.JudgeLine judgeLine;
    [HideInInspector]
    public Play_GameManager GameManager;


    //self
    [HideInInspector] public int whoami = 0;
    public RectTransform rectTransform;
    private Renderer _lineRenderer;
    public SpriteRenderer spriteRenderer;
    public TMP_Text lineID;
    public TMP_Text lineText;
    [HideInInspector]
    public float alpha;

    private readonly ConcurrentQueue<Action> ExecutionQueue = new();

    private void Enqueue(Action action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));
        ExecutionQueue.Enqueue(action);
    }

    // Start is called before the first frame update
    private void Start()
    {
        _lineRenderer = GetComponent<Renderer>();
        lineID.text = whoami.ToString();
        bool debugMode = PlayerPrefs.GetInt("debugMode") == 1;
        gameObject.GetComponent<SpriteRenderer>().maskInteraction =
            debugMode ? SpriteMaskInteraction.None : SpriteMaskInteraction.VisibleInsideMask;
        
        lineID.enabled = debugMode;
        lineID.alpha = 1;
        if (judgeLine.Extended.TextEvents is not null && judgeLine.Extended.TextEvents.Count > 0)
        {
            spriteRenderer.enabled = false;
            lineText.enabled = true;
            StartCoroutine(TextEventReader());
        }
        if (judgeLine.Extended.ScaleYEvents is not null && judgeLine.Extended.ScaleYEvents.Count > 0)
            StartCoroutine(ScaleYEventReader());
        
        if (judgeLine.Extended.ScaleXEvents is not null && judgeLine.Extended.ScaleXEvents.Count > 0)
            StartCoroutine(ScaleXEventReader());
        if (judgeLine.Extended.ColorEvents is not null && judgeLine.Extended.ColorEvents.Count > 0)
            StartCoroutine(ColorEventReader());
        
        if (ChartCache.Instance.moveMode == ChartCache.MoveMode.Beta)
        {
            StartCoroutine(EventReader());
        }

        if (judgeLine.Texture != "line.png")
        {
            spriteRenderer.sprite = JudgeLineSprites.SpritePool[judgeLine.Texture];
            //spriteRenderer.size = new Vector2(spriteRenderer.sprite.rect.size.x * 0.5f,spriteRenderer.sprite.rect.size.y * 0.5f);//spriteRenderer.sprite.rect.size;
            spriteRenderer.size = spriteRenderer.sprite.rect.size;
            spriteRenderer.color = Color.white;
        }

        spriteRenderer.sortingOrder = judgeLine.ZOrder;
    }

    #region Beta

    private IEnumerator EventReader()
    {
        while (true)
        {
            float curTick = GameManager.curTick;
            var xy = GameManager.Chart.JudgeLineList.GetLinePosition(whoami, curTick);
            float x = xy.Item1;
            float y = xy.Item2;
            float theta = judgeLine.EventLayers.GetAngleAtTime(curTick);
            alpha = judgeLine.EventLayers.GetAlphaAtTime(curTick);
            rectTransform.anchoredPosition = new Vector2(x, y);
            rectTransform.rotation = Quaternion.Euler(0, 0, theta);
            Color color = _lineRenderer.material.color;
            color.a = alpha;
            _lineRenderer.material.color = color;
            yield return null;
        }
    }

    #endregion

    
    #region Extended Event

    private IEnumerator TextEventReader()
    {
        while (true)
        {
            var curTick = GameManager.curTick;
            var text = judgeLine.Extended.TextEvents.GetValueAtTime(curTick);
            lineText.text = text;
            lineText.alpha = alpha;
            yield return null;
        }
    }

    private IEnumerator ScaleXEventReader()
    {
        while (true)
        {
            var curTick = GameManager.curTick;
            var x = judgeLine.Extended.ScaleXEvents.GetValueAtTime(curTick);
            rectTransform.localScale = new Vector3(x, rectTransform.localScale.y, 1);
            yield return null;
        }
    }

    private IEnumerator ScaleYEventReader()
    {
        while (true)
        {
            var curTick = GameManager.curTick;
            var y = judgeLine.Extended.ScaleYEvents.GetValueAtTime(curTick);
            rectTransform.localScale = new Vector3(rectTransform.localScale.x, y, 1);
            yield return null;
        }
    }
    
    private IEnumerator ColorEventReader()
    {
        while (true)
        {
            var curTick = GameManager.curTick;
            var color = judgeLine.Extended.ColorEvents.GetValueAtTime(curTick);
            var spriteColor = new Color(color[0] / 255f, color[1] / 255f, color[2] / 255f);
            spriteRenderer.color = spriteColor;
            yield return null;
        }
    }

    #endregion

    #region Pos

    public Tuple<float, float> CalcPositionXY(float t, float x)
    {
        var xy = GameManager.Chart.JudgeLineList.GetLinePosition(whoami, t);
        // 获取在时间 t 时刻判定线的 x 位置
        float xPos = xy.Item1;
        // 获取在时间 t 时刻判定线的 y 位置
        float yPos = xy.Item2;
        // 获取在时间 t 时刻判定线的旋转角度（theta）
        float theta = judgeLine.EventLayers.GetAngleAtTime(t);

        // 将角度 theta 转换为弧度
        double radians = theta * Math.PI / 180.0;

        // 点相对于判定线中心的纵向偏移为 0
        float y = 0f;

        // 计算旋转后的坐标
        float rotatedX = (float)(x * Math.Cos(radians) - y * Math.Sin(radians));
        float rotatedY = (float)(x * Math.Sin(radians) + y * Math.Cos(radians));

        // 将旋转后的坐标和平移坐标相加，得到最终的世界坐标
        float newX = rotatedX + xPos;
        float newY = rotatedY + yPos;
        return Tuple.Create(newX, newY);
    }

    #endregion
}