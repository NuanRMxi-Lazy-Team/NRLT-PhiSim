using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Phigros_Fanmade;
using RePhiEdit;
using TMPro;
using LogType = LogWriter.LogType;

public class JudgeLineScript : MonoBehaviour
{
    //获取初始参数
    public RpeClass.JudgeLine judgeLine;
    public Play_GameManager GameManager;
    
    
    //self
    [HideInInspector]
    public int whoami = 0;
    public RectTransform rectTransform;
    private Renderer _lineRenderer;
    public TMP_Text lineID;
    
    // Start is called before the first frame update
    void Start()
    {
        _lineRenderer = GetComponent<Renderer>();
        lineID.text = whoami.ToString();
        bool debugMode = PlayerPrefs.GetInt("debugMode") == 1;
        lineID.enabled = debugMode;
        if (ChartCache.Instance.moveMode == ChartCache.MoveMode.Beta)
        {
            StartCoroutine(EventReader());
        }
    }

    #region Beta

    private IEnumerator EventReader()
    {
        while (true)
        {
            float curTick = GameManager.curTick;
            float x = judgeLine.EventLayers.GetXAtTime(curTick,GameManager.BpmList);
            float y = judgeLine.EventLayers.GetYAtTime(curTick,GameManager.BpmList);
            float theta = judgeLine.EventLayers.GetAngleAtTime(curTick,GameManager.BpmList);
            float alpha = judgeLine.EventLayers.GetAlphaAtTime(curTick,GameManager.BpmList);
            rectTransform.anchoredPosition = new Vector2(x, y);
            rectTransform.rotation = Quaternion.Euler(0, 0, theta);
            Color color = _lineRenderer.material.color;
            color.a = alpha;
            _lineRenderer.material.color = color;
            lineID.alpha = alpha;
            yield return null;
        }
    }

    #endregion
    
    #region Pos
    
    
    
    public Tuple<float, float> CalcPositionXY(float t, float x)
    {
        // 获取在时间 t 时刻判定线的 x 位置
        float xPos = judgeLine.EventLayers.GetXAtTime(t,GameManager.BpmList);
        // 获取在时间 t 时刻判定线的 y 位置
        float yPos = judgeLine.EventLayers.GetYAtTime(t,GameManager.BpmList);
        // 获取在时间 t 时刻判定线的旋转角度（theta）
        float theta = judgeLine.EventLayers.GetAngleAtTime(t,GameManager.BpmList);

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