using System;
using System.Collections;
using LogWriter;
using UnityEngine;
using UnityEngine.SceneManagement;
using LogType = LogWriter.LogType;
using Phigros_Fanmade;
using TMPro;
#if UNITY_ANDROID
using E7.Native;
#endif

public class Play_GameManager : MonoBehaviour
{
    //获取基本游戏对象
    public GameObject JudgeLine;
    public GameObject TapNote;
    public GameObject HoldNote;
    public GameObject DragNote;
    public GameObject FlickNote;

    public TMP_Text Time;
    //时间
    public double lastTimeUnix;
    private double startTime;
    
    //背景插画
    public GameObject Illistration;

    // Start is called before the first frame update
    void Start()
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
        if (ChartCache.Instance.chart != null)
        {
            //加载谱面
            DrawScene();
            // 应用插画
            Illistration.GetComponent<SpriteRenderer>().sprite = ChartCache.Instance.chart.Illustration;
        }
        else
        {
            Log.Write("没谱面你加载个集贸(E ON Canvas)", LogType.Error);
            SceneManager.LoadScene(0);
        }
    }

    void FixedUpdate()
    {
        //date time update
        lastTimeUnix = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Time.text = (lastTimeUnix - startTime).ToString();
    }

    #region 音频播放部分

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    IEnumerator MusicPlay(AudioClip music, double time)
    {
        AudioSource musicAudioSource = gameObject.AddComponent<AudioSource>();
        musicAudioSource.clip = music;
        musicAudioSource.loop = false; //禁用循环播放
        while (true)
        {
            if (time <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                musicAudioSource.Play();
                break;
            }
            yield return null;
        }
    }
#elif UNITY_ANDROID
    IEnumerator MusicPlay(AudioClip music, double time)
    {
        NativeAudio.Initialize();
        bool MusicisPlay = false;
        //预加载音乐
        NativeAudioPointer audioPointer = NativeAudio.Load(music);
        NativeSource nS = new NativeSource();
        while (true)
        {
            if (time <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() && !MusicisPlay)
            {
                MusicisPlay = true;
                nS.Play(audioPointer);
                break;
            }
            yield return null;
        }
    }
#endif
    
    #endregion

    public void DrawScene()
    {
        //预加载打击音频
        var tapAudioClip = Resources.Load<AudioClip>("Audio/tapHit");
        var dragAudioClip = Resources.Load<AudioClip>("Audio/dragHit");
        var flickAudioClip = Resources.Load<AudioClip>("Audio/flickHit");
        
        
        var chart = ChartCache.Instance.chart;
        double unixTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 5000;
        startTime = unixTime;
        
        GameObject parent = GameObject.Find("Play Canvas");
        for (int i = 0; i < chart.judgeLineList.Count; i++)
        {
            // 生成判定线实例，设置父对象为画布
            GameObject instance = Instantiate(JudgeLine, parent.transform);
            
            // 设置判定线位置到画布顶端且为不可视区域
            instance.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 1000);
            // 获取预制件的脚本组件
            var script = instance.GetComponent<Play_JudgeLine>();
            // 设置脚本中的公共变量
            script.playStartTime = unixTime;
            script.judgeLine = chart.judgeLineList[i];
            script.whoami = i;
            script.GameManager = this;
            
            //生成note实例

            foreach (var note in chart.judgeLineList[i].noteList)
            {
                GameObject noteGameObject;
                switch (note.type)
                {
                    case Note.NoteType.Tap:
                        noteGameObject = Instantiate(TapNote);
                        noteGameObject.GetComponent<Play_Note>().HitClip = tapAudioClip;
                        break;
                    case Note.NoteType.Hold:
                        noteGameObject = Instantiate(HoldNote);
                        noteGameObject.GetComponent<Play_Note>().HitClip = tapAudioClip;
                        break;
                    case Note.NoteType.Drag:
                        noteGameObject = Instantiate(DragNote);
                        noteGameObject.GetComponent<Play_Note>().HitClip = dragAudioClip;
                        break;
                    case Note.NoteType.Flick:
                        noteGameObject = Instantiate(FlickNote);
                        noteGameObject.GetComponent<Play_Note>().HitClip = flickAudioClip;
                        break;
                    default:
                        Log.Write($"Unknown note types in{i}", LogType.Error);
                        noteGameObject = Instantiate(TapNote);
                        break;
                }
                //设置基本参数
                var playNote = noteGameObject.GetComponent<Play_Note>();
                playNote.fatherJudgeLine = script;
                playNote.note = note;
                playNote.playStartUnixTime = unixTime;
                
                //设置父对象
                noteGameObject.transform.SetParent(instance.GetComponent<RectTransform>());
            }
        }
        StartCoroutine(MusicPlay(chart.music, unixTime));
    }
}