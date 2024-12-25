using System;
using System.Collections;
using UnityEngine;
using RePhiEdit;
using TMPro;

public class JudgeLineScript : MonoBehaviour
{
    //获取初始参数
    public RpeClass.JudgeLine judgeLine;
    public Play_GameManager GameManager;


    //self
    [HideInInspector] public int whoami = 0;
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
        lineID.alpha = 1;
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
            var xy = GameManager.Chart.JudgeLineList.GetLinePosition(whoami, curTick);
            float x = xy.Item1;
            float y = xy.Item2;
            float theta = judgeLine.EventLayers.GetAngleAtTime(curTick);
            float alpha = judgeLine.EventLayers.GetAlphaAtTime(curTick);
            rectTransform.anchoredPosition = new Vector2(x, y);
            rectTransform.rotation = Quaternion.Euler(0, 0, theta);
            Color color = _lineRenderer.material.color;
            color.a = alpha;
            _lineRenderer.material.color = color;
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