using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using LogWriter;
using UnityEngine;
using Phigros_Fanmade;
using Unity.VisualScripting;
using Event = Phigros_Fanmade.Event;
using LogType = LogWriter.LogType;

public class Play_JudgeLine : MonoBehaviour
{
    //获取初始参数
    public JudgeLine judgeLine;
    public double playStartTime;

    public int whoami = 0;

    //NoteGameObject
    public GameObject TapNote;
    public GameObject HoldNote;
    public GameObject DragNote;
    public GameObject FlickNote;

    //self
    public RectTransform rectTransform;

    //SPEvent
    public Event.SpeedEvent lastSpeedEvent = new()
    {
        startTime = 0,
        endTime = 0,
        startValue = 0,
        endValue = 0
    };
    
    // Start is called before the first frame update
    void Start()
    {
        
        for (int i = 0; i < judgeLine.noteList.Count; i++)
        {
            var note = judgeLine.noteList[i];
            GameObject noteGameObject;
            switch (note.type)
            {
                case Note.NoteType.Tap:
                    noteGameObject = Instantiate(TapNote);
                    break;
                case Note.NoteType.Hold:
                    noteGameObject = Instantiate(HoldNote);
                    break;
                case Note.NoteType.Drag:
                    noteGameObject = Instantiate(DragNote);
                    break;
                case Note.NoteType.Flick:
                    noteGameObject = Instantiate(FlickNote);
                    break;
                default:
                    Log.Write($"Unknown note types in{i}", LogType.Error);
                    noteGameObject = Instantiate(TapNote);
                    break;
            }

            noteGameObject.GetComponent<Play_Note>().fatherJudgeLine = GetComponent<GameObject>();
            noteGameObject.GetComponent<Play_Note>().note = note;
            noteGameObject.GetComponent<Play_Note>().playStartUnixTime = playStartTime;
            noteGameObject.transform.SetParent(rectTransform);
        }
        
        if (ChartCache.Instance.moveMode == ChartCache.MoveMode.WhatTheFuck)
        {
            StartCoroutine(XMoveEventReader());
            StartCoroutine(YMoveEventReader());
            StartCoroutine(AlphaEventReader());
            StartCoroutine(RotateEventReader());
            StartCoroutine(SP_SpeedEventReadr());
        }
        
    }

    #region EventReadr

    /// <summary>
    /// X移动事件读取器
    /// </summary>
    IEnumerator XMoveEventReader()
    {
        int i = 0;
        var xMoveList = judgeLine.xMoveList;
        while (true)
        {
            var now = DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds -
                            playStartTime;
            if (xMoveList[i].startTime <= now)
            {
                var xEvent = xMoveList[i];
                StartCoroutine(MoveXOverTime(xEvent.startValue, xEvent.endValue,
                    (float)(xEvent.endTime - xEvent.startTime) / 1000));
                i++;
            }
            yield return null;
        }
    }

    /// <summary>
    /// Y移动事件读取器
    /// </summary>
    IEnumerator YMoveEventReader()
    {
        int i = 0;
        var yMoveList = judgeLine.yMoveList;
        while (true)
        {
            var now = DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds -
                      playStartTime;
            if (yMoveList[i].startTime <= now)
            {
                var yEvent = yMoveList[i];
                StartCoroutine(MoveYOverTime(yEvent.startValue, yEvent.endValue,
                        (float)(yEvent.endTime - yEvent.startTime) / 1000));
                i++;
                yield return null;
            }
        }
    }

    IEnumerator RotateEventReader()
    {
        int i = 0;
        var angleChangeList = judgeLine.angleChangeList;
        while (true)
        {
            var now = DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds -
                                playStartTime;
            if (angleChangeList[i].startTime <= now)
            { 
                var angleEvent = angleChangeList[i]; 
                StartCoroutine(RotateOverTime(angleEvent.startValue, angleEvent.endValue, 
                    (float)(angleEvent.endTime - angleEvent.startTime) / 1000));
                i++; 
                yield return null;
            }
        }
    }
    
    IEnumerator AlphaEventReader()
    {
        int i = 0;
        var alphaChangeList = judgeLine.alphaChangeList;
        while (true)
        {
            var now = DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds -
                      playStartTime;
            if (alphaChangeList[i].startTime <= now)
            {
                var alphaEvent = alphaChangeList[i];
                StartCoroutine(FadeOverTime(alphaEvent.startValue, alphaEvent.endValue,
                                (float)(alphaEvent.endTime - alphaEvent.startTime) / 1000));
                i++;
                yield return null;
            }
        }
    }
    
    IEnumerator SP_SpeedEventReadr()
    {
        int i = 0;
        var speedEventList = judgeLine.speedChangeList;
        while (true)
        {
            var now = DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds -
                            playStartTime;
            if (speedEventList[i].startTime <= now)
            {
                lastSpeedEvent = speedEventList[i];
            }
        }
    }
    
    

    #endregion


    #region judgeLineMover

    /// <summary>
    /// 移动X轴
    /// </summary>
    /// <param name="startXValue">开始时X位置</param>
    /// <param name="endXValue">结束时X位置</param>
    /// <param name="duration">指定经过时间（单位为妙）</param>
    IEnumerator MoveXOverTime(float startXValue, float endXValue, float duration)
    {
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            rectTransform.anchoredPosition = Vector3.Lerp(new Vector3(startXValue, rectTransform.anchoredPosition.y),
                new Vector3(endXValue, rectTransform.anchoredPosition.y), (Time.time - startTime) / duration);
            yield return null;
        }

        rectTransform.anchoredPosition = new Vector3(endXValue, rectTransform.transform.position.y,
            rectTransform.transform.position.z);
    }

    /// <summary>
    /// 移动Y轴
    /// </summary>
    /// <param name="startYValue">开始时Y的位置</param>
    /// <param name="endYValue">结束时Y的位置</param>
    /// <param name="duration">指定经过时间（单位为妙）</param>
    IEnumerator MoveYOverTime(float startYValue, float endYValue, float duration)
    {
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            rectTransform.anchoredPosition = Vector3.Lerp(new Vector3(rectTransform.anchoredPosition.x, startYValue),
                new Vector3(rectTransform.anchoredPosition.x, endYValue), (Time.time - startTime) / duration);
            yield return null;
        }

        rectTransform.anchoredPosition = new Vector3(rectTransform.anchoredPosition.x, endYValue,
            rectTransform.transform.position.z);
    }

    /// <summary>
    /// 旋转判定线
    /// </summary>
    /// <param name="startRotate">开始时角度</param>
    /// <param name="endRotate">结束时角度</param>
    /// <param name="duration">指定经过时间（单位为妙）</param>
    IEnumerator RotateOverTime(float startRotate, float endRotate, float duration)
    {
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            rectTransform.rotation = Quaternion.Lerp(Quaternion.Euler(0, 0, startRotate),
                Quaternion.Euler(0, 0, endRotate), (Time.time - startTime) / duration);
            yield return null;
        }

        rectTransform.transform.rotation = Quaternion.Euler(0, 0, endRotate);
    }

    /// <summary>
    /// 透明度渐变
    /// </summary>
    /// <param name="startAlpha">开始时不透明度</param>
    /// <param name="endAlpha">结束时不透明度</param>
    /// <param name="duration">指定经过时间（单位为妙）</param>
    IEnumerator FadeOverTime(float startAlpha, float endAlpha, float duration)
    {
        // 计算总时间
        float time = 0;
        while (time < duration)
        {
            // 更新时间
            time += Time.deltaTime;
            // 计算新的透明度
            float newOpacity = Mathf.Lerp(startAlpha, endAlpha, (float)(time / duration));
            // 设置新的透明度
            Color color = GetComponent<Renderer>().material.color;
            color.a = newOpacity;
            GetComponent<Renderer>().material.color = color;
            yield return null;
        }
    }
    
    #endregion
}