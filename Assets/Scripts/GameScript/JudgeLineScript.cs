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
        {
            StartCoroutine(ScaleYEventReader());
        }
        if (judgeLine.Extended.ScaleXEvents is not null && judgeLine.Extended.ScaleXEvents.Count > 0)
        {
            StartCoroutine(ScaleXEventReader());
        }
        if (ChartCache.Instance.moveMode == ChartCache.MoveMode.Beta)
        {
            StartCoroutine(EventReader());
        }
        else if (ChartCache.Instance.moveMode == ChartCache.MoveMode.WhatTheFuck)
        {
            StartCoroutine(ThreadStarter());
            StartCoroutine(FrameUpdate());
        }
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

    #region WTF

    private void ThreadUpdate()
    {
        float curTick = GameManager.curTick;

        var xy = GameManager.Chart.JudgeLineList.GetLinePosition(whoami, curTick);
        float posX = xy.Item1;
        float posY = xy.Item2;
        float posTheta = judgeLine.EventLayers.GetAngleAtTime(curTick);
        alpha = judgeLine.EventLayers.GetAlphaAtTime(curTick);
        Enqueue(() =>
        {
            rectTransform.anchoredPosition = new Vector2(posX, posY);
            rectTransform.rotation = Quaternion.Euler(0, 0, posTheta);
            Color color = _lineRenderer.material.color;
            color.a = alpha;
            _lineRenderer.material.color = color;
        });
    }

    // 帧刷新
    private IEnumerator FrameUpdate()
    {
        while (true)
        {
            // 执行队列中的操作
            while (ExecutionQueue.TryDequeue(out var action))
            {
                action?.Invoke();
            }

            yield return null;
        }

        /*
        while (true)
        {
            rectTransform.anchoredPosition = new Vector2(_posX, _posY);
            rectTransform.rotation = Quaternion.Euler(0, 0, _posTheta);
            Color color = _lineRenderer.material.color;
            color.a = _alpha;
            _lineRenderer.material.color = color;
            yield return null;
        }
        */
    }

    private IEnumerator ThreadStarter()
    {
        while (true)
        {
            Thread thread = new Thread(ThreadUpdate);
            thread.Start();
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