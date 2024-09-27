using System.Collections;
using System;
using Phigros_Fanmade;
using UnityEngine;
using UnityEngine.Serialization;

public class Play_Note : MonoBehaviour
{
    //获取初始参数
    public Note note;
    public RectTransform noteRectTransform;
    public Play_JudgeLine fatherJudgeLine;
    public double playStartUnixTime;
    [FormerlySerializedAs("tapHitClip")] public AudioClip HitClip;
    private AudioSource HitAudioSource;
    private bool hited = false;
    private bool hitEnd = false;
    private Renderer noteRenderer;

    // Start is called before the first frame update
    void Start()
    {
        noteRenderer = gameObject.GetComponent<Renderer>();
        noteRectTransform.transform.rotation = fatherJudgeLine.rectTransform.rotation;
        HitAudioSource = gameObject.AddComponent<AudioSource>();
        HitAudioSource.clip = HitClip;
        HitAudioSource.loop = false;
        if (!note.above)
        {
            //翻转自身贴图
            noteRenderer.transform.Rotate(0, 0, 180);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (hitEnd) return;
        //实际speed = speed * speedMultiplier，单位为每一个速度单位648像素每秒，根据此公式实时演算相对于判定线的高度（y坐标）
        float yPos = CalculateYPosition(
            note.clickStartTime,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - playStartUnixTime);
        noteRectTransform.anchoredPosition = new Vector2(note.x,
            yPos);
    }


    /// <summary>
    /// 计算Y位置
    /// </summary>
    /// <param name="targetTime"></param>
    /// <param name="speed"></param>
    /// <param name="currentTime"></param>
    /// <returns>Y Position</returns>
    private float CalculateYPosition(double targetTime, double currentTime)
    {
        //检查这是不是hold
        bool isHold = note.clickStartTime != note.clickEndTime;

        // 计算已经过去的时间（单位：秒）
        double elapsedTime = currentTime / 1000;

        // 计算目标时间（单位：秒）
        double targetTimeInSeconds = targetTime / 1000;

        // 如果已经过去的时间大于目标时间，那么摧毁渲染器
        if (elapsedTime >= note.clickEndTime / 1000 && isHold)
        {
            //摧毁渲染器
            Destroy(noteRenderer);
            //进入协程，等待音效结束
            StartCoroutine(WaitForDestroy());
            hitEnd = true;
            return 1200;
        }
        else if (elapsedTime >= targetTimeInSeconds && !isHold)
        {
            //直接摧毁自己
            if (!hited)
            {
                HitAudioSource.Play();
                hited = true;
            }
            //摧毁渲染器
            Destroy(noteRenderer);
            //进入协程，等待音效结束
            StartCoroutine(WaitForDestroy());
            hitEnd = true;
            return 0;
        }

        // 根据速度（像素/秒）计算y坐标
        //float yPosition = (float)(speed * (note.clickStartTime / 1000 - elapsedTime) * 648 * note.speedMultiplier); // 这里加入了速度单位648像素/秒，648是1080 * 0.6
        //弃用原直接计算，使用floorPos进行计算。
        float newYPosition = (float)
            (
                fatherJudgeLine.judgeLine.speedChangeList.GetCurTimeSu(currentTime) -
                note.floorPosition
                ) * note.speedMultiplier;
        if (isHold)
        {
            newYPosition -= 1200f;
        }
        if (note.above)
        {
            //翻转y坐标
            newYPosition = -newYPosition;
        }

        if (elapsedTime <= note.clickEndTime / 1000 && elapsedTime >= targetTimeInSeconds && isHold)
        {
            if (!hited)
            {
                HitAudioSource.Play();
                hited = true;
            }
            newYPosition = -1200f;
            if (note.above)
            {
                //翻转y坐标
                newYPosition = 1200f;
            }
        }
        
        return newYPosition;
    }
    
    IEnumerator WaitForDestroy()
    {
        //记录当前时间
        double now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        //加上音效时长，获得音效结束时间
        var endTime = now + HitClip.length * 1000;
        //等待音效结束
        while (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < endTime)
        {
            yield return null;
        }
        Destroy(gameObject);
    }
}