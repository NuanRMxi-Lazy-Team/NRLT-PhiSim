using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Phigros_Fanmade;
using TMPro;
using Event = Phigros_Fanmade.Event;
using LogType = LogWriter.LogType;

public class Play_JudgeLine : MonoBehaviour
{
    //获取初始参数
    public JudgeLine judgeLine;
    public Play_GameManager GameManager;
    
    
    //self
    [HideInInspector]
    public int whoami = 0;
    public RectTransform rectTransform;
    private Renderer lineRenderer;
    public TMP_Text lineID;
    
    // Start is called before the first frame update
    void Start()
    {
        lineRenderer = GetComponent<Renderer>();
        lineID.text = whoami.ToString();
        if (!ChartCache.Instance.debugMode)
        {
            lineRenderer.material.color = new Color
            {
                a = 0f
            };
        }
        if (ChartCache.Instance.moveMode == ChartCache.MoveMode.WhatTheFuck)
        {
            StartCoroutine(XMoveEventReader());
            StartCoroutine(YMoveEventReader());
            StartCoroutine(AlphaEventReader());
            StartCoroutine(RotateEventReader());
        }
    }
    

    #region EventReadr

    /// <summary>
    /// X移动事件读取器
    /// </summary>
    private IEnumerator XMoveEventReader()
    {
        int i = 0;
        var xMoveList = judgeLine.xMoveList;
        while (true)
        {
            if (!(i <= xMoveList.Count - 1))
            {
                break;
            }
            if (GameManager.curTick  >= xMoveList[i].startTime)
            {
                StartCoroutine(MoveXOverTime
                    (
                        xMoveList[i].startValue, xMoveList[i].endValue, 
                        (float)(xMoveList[i].endTime - xMoveList[i].startTime) / 1000
                        ));
                i++;
            }
            yield return null;
        }
    }

    /// <summary>
    /// Y移动事件读取器
    /// </summary>
    private IEnumerator YMoveEventReader()
    {
        int i = 0;
        var yMoveList = judgeLine.yMoveList;
        while (true)
        {
            if (!(i <= yMoveList.Count - 1))
            {
                break;
            }
            if (yMoveList[i].startTime <= GameManager.curTick )
            {
                var yEvent = yMoveList[i];
                StartCoroutine(MoveYOverTime(
                    yEvent.startValue, 
                    yEvent.endValue,
                    (float)(yEvent.endTime - yEvent.startTime) / 1000
                    ));
                i++;
            }
            yield return null;
        }
    }

    /// <summary>
    /// 旋转事件读取器
    /// </summary>
    private IEnumerator RotateEventReader()
    {
        int i = 0;
        var angleChangeList = judgeLine.angleChangeList;
        while (true)
        {
            if (!(i <= angleChangeList.Count - 1))
            {
                break;
            }
            if (angleChangeList[i].startTime <= GameManager.curTick )
            { 
                var angleEvent = angleChangeList[i]; 
                StartCoroutine(RotateOverTime(
                    angleEvent.startValue, 
                    angleEvent.endValue, 
                    (float)(angleEvent.endTime - angleEvent.startTime) / 1000
                    ));
                i++; 
            }
            yield return null;
        }
    }
    
    private IEnumerator AlphaEventReader()
    {
        int i = 0;
        var alphaChangeList = judgeLine.alphaChangeList;
        while (true)
        {
            if (!(i <= alphaChangeList.Count - 1))
            {
                break;
            }
            if (alphaChangeList[i].startTime <= GameManager.curTick )
            {
                var alphaEvent = alphaChangeList[i];
                StartCoroutine(FadeOverTime(
                    alphaEvent.startValue, 
                    alphaEvent.endValue,
                    (float)(alphaEvent.endTime - alphaEvent.startTime) / 1000
                    ));
                i++;
            }
            yield return null;
        }
    }
    #endregion

    #region judgeLineMover

    /// <summary>
    /// 移动X轴
    /// </summary>
    /// <param name="startXValue">开始时X位置</param>
    /// <param name="endXValue">结束时X位置</param>
    /// <param name="duration">指定经过时间（单位为秒）</param>
    IEnumerator MoveXOverTime(float startXValue, float endXValue, float duration)
    {
        double startTime = GameManager.curTick ;
        while ((GameManager.curTick  - startTime) / 1000 < duration)
        {
            var t = (GameManager.curTick  - startTime) / 1000 / duration;
            rectTransform.anchoredPosition = Vector2.Lerp(
                new Vector2(startXValue, rectTransform.anchoredPosition.y),
                new Vector2(endXValue, rectTransform.anchoredPosition.y),
                (float)t);
            yield return null;
        }
        rectTransform.anchoredPosition = new Vector2(endXValue, rectTransform.anchoredPosition.y);
    }

    /// <summary>
    /// 移动Y轴
    /// </summary>
    /// <param name="startYValue">开始时Y的位置</param>
    /// <param name="endYValue">结束时Y的位置</param>
    /// <param name="duration">指定经过时间（单位为秒）</param>
    IEnumerator MoveYOverTime(float startYValue, float endYValue, float duration)
    {
        double startTime = GameManager.curTick ;
        while ((GameManager.curTick  - startTime) / 1000 < duration)
        {
            var t = (GameManager.curTick  - startTime) / 1000 / duration;
            rectTransform.anchoredPosition = Vector2.Lerp(
                new Vector2(rectTransform.anchoredPosition.x, startYValue),
                new Vector2(rectTransform.anchoredPosition.x, endYValue),
                (float)t);
            yield return null;
        }
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, endYValue);
    }

    /// <summary>
    /// 旋转判定线
    /// </summary>
    /// <param name="startRotate">开始时角度</param>
    /// <param name="endRotate">结束时角度</param>
    /// <param name="duration">指定经过时间（单位为秒）</param>
    IEnumerator RotateOverTime(float startRotate, float endRotate, float duration)
    {
        double startTime = GameManager.curTick ;
        while ((GameManager.curTick  - startTime) / 1000 < duration)
        {
            var t = (GameManager.curTick  - startTime) / 1000 / duration;
            rectTransform.rotation = Quaternion.Euler(
                0,
                0,
                Mathf.Lerp(startRotate, endRotate, (float)t));
            yield return null;
        }
        rectTransform.rotation = Quaternion.Euler(0, 0, endRotate);
    }

    /// <summary>
    /// 透明度渐变
    /// </summary>
    /// <param name="startAlpha">开始时不透明度</param>
    /// <param name="endAlpha">结束时不透明度</param>
    /// <param name="duration">指定经过时间（单位为秒）</param>
    IEnumerator FadeOverTime(float startAlpha, float endAlpha, float duration)
    {
        double startTime = GameManager.curTick ;
        while ((GameManager.curTick  - startTime) / 1000 < duration)
        {
            var t = (GameManager.curTick  - startTime) / 1000 / duration;
            float newOpacity = Mathf.Lerp(startAlpha, endAlpha, (float)t);
            Color color = lineRenderer.material.color;
            color.a = newOpacity;
            lineRenderer.material.color = color;
            yield return null;
        }
    }
    
    #endregion
    
    #region Pos
    public Tuple<float, float> CalcPositionXY(double t, float x)
    {
        // Calculate the x position at time t
        float xPos = CalculateValueAtTime(judgeLine.xMoveList.Cast<Event.EventTemplate>().ToList(), t);
        // Calculate the y position at time t
        float yPos = CalculateValueAtTime(judgeLine.yMoveList.Cast<Event.EventTemplate>().ToList(), t);
        // Calculate the angle (theta) at time t
        float theta = CalculateValueAtTime(judgeLine.angleChangeList.Cast<Event.EventTemplate>().ToList(), t);

        // Convert theta to radians
        double radians = theta * Math.PI / 180.0;

        // Calculate the new position of the point (x, y) at time t
        float newX = (float)((x + xPos) * Math.Cos(radians) - yPos * Math.Sin(radians));
        float newY = (float)((x + xPos) * Math.Sin(radians) + yPos * Math.Cos(radians));

        return Tuple.Create(newX, newY);
    }

    private static float CalculateValueAtTime(List<Event.EventTemplate> changes, double t)
    {
        Event.EventTemplate previousChange = null;

        foreach (var change in changes)
        {
            if (t >= change.startTime && t <= change.endTime)
            {
                float normalizedTime = (float)((t - change.startTime) / (change.endTime - change.startTime));
                return Mathf.Lerp(change.startValue, change.endValue, normalizedTime);
            }
            previousChange = change;
        }

        // 如果时间点不在任何变化区间内，使用上一个变化区间的endValue
        if (previousChange != null)
        {
            return previousChange.endValue;
        }

        throw new ArgumentException("时间点不在任何变化区间内，且没有上一个变化区间");
    }
    #endregion
}