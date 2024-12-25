using System.Collections;
using System;
using PhigrosFanmade;
using RePhiEdit;
using UnityEngine;
using UnityEngine.Serialization;

public class Play_Note : MonoBehaviour
{
    //获取初始参数
    public RpeClass.Note note;
    public RectTransform noteRectTransform;
    public JudgeLineScript fatherJudgeLine;
    public Play_GameManager GameManager;
    public HitFx HitFx;
    
    private Renderer noteRenderer;
    private GameObject _canvas;

    // Start is called before the first frame update
    private void Start()
    {
        noteRenderer = gameObject.GetComponent<Renderer>();
        noteRectTransform.transform.rotation = fatherJudgeLine.rectTransform.rotation;
        _canvas = GameObject.Find("Play Canvas");
        if (note.Above == 2)
        {
            //翻转自身贴图
            noteRenderer.transform.Rotate(0, 0, 180);
        }
    }

    // Update is called once per frame
    private void Update()
    {
        //实际speed = speed * speedMultiplier，单位为每一个速度单位648像素每秒，根据此公式实时演算相对于判定线的高度（y坐标）
        float yPos = CalculateYPosition(
            note.StartTime.CurTime(GameManager.BpmList),
            GameManager.curTick);
        noteRectTransform.anchoredPosition = new Vector2(note.PositionX,
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
        if (lastTime >= targetTime)
        {
            if (note.IsFake == 0)
            {
                //生成hitFx，恒定不旋转
                var fxPos = fatherJudgeLine.CalcPositionXY(note.StartTime.CurTime(GameManager.BpmList), note.PositionX);
                var hitFx = Instantiate(HitFx, new Vector3(), Quaternion.identity);
                hitFx.gameManager = GameManager;
                hitFx.hitType = note.Type;
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
            fatherJudgeLine.judgeLine.EventLayers.GetCurFloorPosition(lastTime,GameManager.BpmList) -
            note.FloorPosition
        ) * note.SpeedMultiplier;
        
        newYPosition = note.Above == 1 ? -newYPosition : newYPosition;

        if (lastTime <= note.EndTime.CurTime(GameManager.BpmList)  && lastTime >= targetTime)
        {
            newYPosition = -1200f;
            if (note.Above == 1)
            {
                //翻转y坐标
                newYPosition = 1200f;
            }
        }
        return newYPosition;
    }
}