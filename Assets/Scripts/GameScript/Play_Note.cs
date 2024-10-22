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
    public Play_GameManager GameManager;
    public HitFx HitFx;
    
    
    [FormerlySerializedAs("tapHitClip")] 
    public AudioClip HitClip;
    private AudioSource HitAudioSource;
    private bool hited;
    private bool hitEnd;
    private Renderer noteRenderer;

    // Start is called before the first frame update
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

    // Update is called once per frame
    private void Update()
    {
        if (hitEnd) return;
        //实际speed = speed * speedMultiplier，单位为每一个速度单位648像素每秒，根据此公式实时演算相对于判定线的高度（y坐标）
        float yPos = CalculateYPosition(
            note.clickStartTime,
            GameManager.curTick);
        noteRectTransform.anchoredPosition = new Vector2(note.x,
            yPos);
    }


    /// <summary>
    /// 计算垂直位置
    /// </summary>
    /// <param name="targetTime">目标时间</param>
    /// <param name="lastTime">当前时间</param>
    /// <returns>垂直位置</returns>
    private float CalculateYPosition(double targetTime, double lastTime)
    {
        if (lastTime >= targetTime)
        {
            //直接摧毁自己
            if (!hited)
            {
                HitAudioSource.Play();
                hited = true;
            }

            //摧毁渲染器
            Destroy(noteRenderer);
            //生成hitFx，恒定不旋转
            var hitFx = Instantiate(HitFx, transform.position, Quaternion.identity);
            hitFx.GameManager = GameManager;
            //设置父对象为Canvas
            hitFx.transform.SetParent(GameObject.Find("Play Canvas").transform);
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
            fatherJudgeLine.judgeLine.speedChangeList.GetCurTimeSu(lastTime) -
            note.floorPosition
        ) * note.speedMultiplier;
        if (note.above)
        {
            //翻转y坐标
            newYPosition = -newYPosition;
        }

        if (lastTime <= note.clickEndTime  && lastTime >= targetTime)
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