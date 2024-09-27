using System.Collections;
using System;
using Phigros_Fanmade;
using UnityEngine;
using UnityEngine.Serialization;

public class Play_HoldHead : MonoBehaviour
{
    public Note note;
    public RectTransform noteRectTransform;
    public Play_JudgeLine fatherJudgeLine;
    public double playStartUnixTime;
    public AudioClip HitClip;
    private AudioSource HitAudioSource;
    private bool hited = false;
    private bool hitEnd = false;
    private Renderer noteRenderer;

    private void Start()
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

    private void Update()
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
    /// <param name="currentTime"></param>
    /// <returns>Y Position</returns>
    private float CalculateYPosition(double targetTime, double currentTime)
    {
        if (targetTime <= currentTime)
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
        float spriteHeight = noteRenderer.bounds.size.y;
        if (note.above)
        {
            //翻转y坐标
            //获得自己的sprite高度
            
            return -newYPosition + spriteHeight / 2;
        }
        else
        {
            return newYPosition - spriteHeight / 2;
        }
        
    }
    
    private IEnumerator WaitForDestroy()
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