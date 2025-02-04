using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LogWriter;
using UnityEngine;
using UnityEngine.SceneManagement;
using LogType = LogWriter.LogType;
using RePhiEdit;
using TMPro;
#if UNITY_ANDROID && !UNITY_EDITOR_WIN
using E7.Native;
#endif

public class Play_GameManager : MonoBehaviour
{
    //获取基本游戏对象
    public GameObject JudgeLine;
    public GameObject TapNote;
    public GameObject DragNote;
    public GameObject FlickNote;
    public GameObject HoldNote;
    
    public GameObject HoldHead;
    public GameObject HoldBody;
    public GameObject HoldEnd;
    
    // AudioClips
    public AudioClip tapAudioClip;
    public AudioClip dragAudioClip;
    public AudioClip flickAudioClip;
    
    // Tick
    [HideInInspector]
    public float curTick = 0f;

    public TMP_Text Time;
    public RpeChart Chart;
    
    //背景插画
    public GameObject Illistration;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    private AudioSource _musicAudioSource;
#elif UNITY_ANDROID || UNITY_IOS
    private NativeSource _musicAudioSource;
#endif
    

    // Start is called before the first frame update
    private void Start()
    {
        //ChartCache.Instance.chart = Chart.ChartConverter(File.ReadAllBytes("D:\\PhiOfaChart\\SMS.zip"), "D:\\PhiOfaChart",".zip");
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        //计算宽高比
        float aspectRatio = screenWidth / screenHeight;

        //设置目标宽度为屏幕宽度
        float targetWidth = Screen.width;

        //计算正交大小
        float orthographicSize = targetWidth / (2 * aspectRatio);

        //设置摄像机的正交大小
        Camera.main.orthographicSize = orthographicSize;

        //输出结果到DEBUGLOG
        Log.Write("Calculated Orthographic Size: " + orthographicSize, LogType.Debug);
        //检查缓存中是否存在谱面
        if (ChartCache.Instance.chart.Music is not null)
        {
            //加载谱面
            DrawScene();
            // 应用插画
            Illistration.GetComponent<SpriteRenderer>().sprite = ChartCache.Instance.chart.Illustration;
            
            // 插画遮罩调试
            bool debugMode = PlayerPrefs.GetInt("debugMode") == 1;
            Illistration.GetComponent<SpriteMask>().enabled = !debugMode;
        }
        else
        {
            Log.Write("没谱面你加载个集贸(E ON Canvas)", LogType.Error);
            SceneManager.LoadScene(0);
        }
    }

    private void Update()
    {
        try
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _musicAudioSource.Stop();
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                PauseButton();
            }
        
            //Tick
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (_musicAudioSource is not null && !_musicAudioSource.isPlaying) return;
#endif
        
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            curTick = _musicAudioSource.time * 1000 - Chart.Meta.Offset;
#else 
            curTick = _musicAudioSource.GetPlaybackTime() * 1000 - Chart.Meta.Offset;
#endif
            Time.text = curTick.ToString();
        }
        catch (Exception e)
        {
            Log.Write(e.ToString(),LogType.Error);
            throw;
        }
        
    }

    #region 音频播放部分

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    IEnumerator MusicPlay(AudioClip music, double time)
    {
        _musicAudioSource = gameObject.AddComponent<AudioSource>();
        _musicAudioSource.clip = music;
        _musicAudioSource.loop = false; //禁用循环播放
        while (true)
        {
            if (time <= curTick)
            {
                _musicAudioSource.Play();
                break;
            }
            yield return null;
        }
    }
#elif UNITY_ANDROID || UNITY_IOS
    IEnumerator MusicPlay(AudioClip music, double time)
    {
        //预加载音乐
        NativeAudioPointer audioPointer = NativeAudio.Load(music);
        _musicAudioSource = new NativeSource();
        while (true)
        {
            if (time <= curTick)
            {
                _musicAudioSource.Play(audioPointer);
                break;
            }
            yield return null;
        }
    }
#endif
    
    #endregion

    private void DrawScene()
    {
        //预加载打击音频
        tapAudioClip = Resources.Load<AudioClip>("Audio/tapHit");
        dragAudioClip = Resources.Load<AudioClip>("Audio/dragHit");
        flickAudioClip = Resources.Load<AudioClip>("Audio/flickHit");
        
        //预加载打击特效
        var hitFxs = Resources.LoadAll<Sprite>("HitFx").ToList();
        //取sprite名称中两个下划线之间的数字，按照数字大小排序
        hitFxs.Sort((a, b) =>
        {
            int aIndex = int.Parse(a.name.Split('_')[1]);
            int bIndex = int.Parse(b.name.Split('_')[1]);
            return aIndex.CompareTo(bIndex);
        });
        ChartCache.Instance.HitFxs = hitFxs; 
        
        Chart = ChartCache.Instance.chart;
        // 试图同时播放三个audio在Native Audio上
        PlayHitSound(2);
        PlayHitSound(3);
        PlayHitSound(4);
        StartCoroutine(MusicPlay(Chart.Music, 0));
        
        GameObject canvas = GameObject.Find("Play Canvas");
        for (int i = 0; i < Chart.JudgeLineList.Count; i++)
        {
            // 生成判定线实例，设置父对象为画布
            GameObject instance = Instantiate(JudgeLine, canvas.transform);
            // 设置判定线位置到画布顶端且为不可视区域
            instance.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 1000);
            // 获取预制件的脚本组件
            var script = instance.GetComponent<JudgeLineScript>();
            // 设置脚本中的公共变量
            script.judgeLine = Chart.JudgeLineList[i];
            script.whoami = i;
            script.GameManager = this;
            
            //生成note实例
            List<RpeClass.Note> holdList = new();
            foreach (var note in Chart.JudgeLineList[i].Notes)
            {
                GameObject noteGameObject = note.Type switch
                {
                    1 => Instantiate(TapNote),
                    3 => Instantiate(FlickNote),
                    4 => Instantiate(DragNote),
                    _ => null
                };
                if (noteGameObject is null)
                {
                    holdList.Add(note);
                    goto next;
                }
                //设置基本参数
                var playNote = noteGameObject.GetComponent<Play_Note>();
                playNote.fatherJudgeLine = script;
                playNote.Note = note;
                playNote.gameManager = this;
                
                //设置父对象
                noteGameObject.transform.SetParent(instance.GetComponent<RectTransform>());
                next: ;
            }
            
            //生成Hold
            foreach (var hold in holdList)
            {
                var holdNote = Instantiate(HoldNote,instance.GetComponent<RectTransform>());
                holdNote.GetComponent<Play_Hold>().fatherJudgeLine = script;
                holdNote.GetComponent<Play_Hold>().Note = hold;
                holdNote.GetComponent<Play_Hold>().gameManager = this;
                
                /*
                var head = Instantiate(HoldHead,instance.GetComponent<RectTransform>());
                head.GetComponent<Play_HoldHead>().fatherJudgeLine = script;
                head.GetComponent<Play_HoldHead>().Note = hold;
                head.GetComponent<Play_HoldHead>().gameManager = this;
                
                var body = Instantiate(HoldBody,instance.GetComponent<RectTransform>());
                body.GetComponent<Play_HoldBody>().fatherJudgeLine = script;
                body.GetComponent<Play_HoldBody>().Note = hold;
                body.GetComponent<Play_HoldBody>().gameManager = this;
                
                var end = Instantiate(HoldEnd, instance.GetComponent<RectTransform>());
                end.GetComponent<Play_HoldEnd>().fatherJudgeLine = script;
                end.GetComponent<Play_HoldEnd>().note = hold;
                end.GetComponent<Play_HoldEnd>().gameManager = this;
                */
            }
            
        }
    }
    
    //Tick操作
    public void Pause()
    {
        //暂停音乐
        _musicAudioSource.Pause();
    }

    public void Resume()
    {
        //继续播放
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        _musicAudioSource.Play();
#elif UNITY_ANDROID || UNITY_IOS
        _musicAudioSource.SetPlaybackTime(curTick / 1000);
#endif
    }

    public void JumpToTick(float tick)
    {
        float curTick = (tick + Chart.Meta.Offset) / 1000 ;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        _musicAudioSource.time = curTick;
#elif UNITY_ANDROID || UNITY_IOS
        _musicAudioSource.SetPlaybackTime(curTick);
#endif
    }
    
    private bool _isPaused = false;
    public void PauseButton()
    {
        if (_isPaused)
        {
            Resume();
            _isPaused = false;
            return;
        }
        Pause();
        _isPaused = true;
    }

    public void PlayHitSound(int type)
    {
        AudioSource hitAudioSource = gameObject.AddComponent<AudioSource>();
        AudioClip hitAudioClip = type switch
        {
            1 => tapAudioClip,
            2 => tapAudioClip,
            3 => flickAudioClip,
            4 => dragAudioClip,
            _ => tapAudioClip
        };
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        hitAudioSource.clip = hitAudioClip;
        hitAudioSource.loop = false;
        hitAudioSource.Play();
        Destroy(hitAudioSource, hitAudioClip.length);
#elif UNITY_ANDROID || UNITY_IOS
        NativeAudioPointer adp = NativeAudio.Load(hitAudioClip);
        NativeSource mAS = new NativeSource();
        mAS.Play(adp);
#endif
    }
    public void PlayHitSound(string path)
    {
        
    }
}