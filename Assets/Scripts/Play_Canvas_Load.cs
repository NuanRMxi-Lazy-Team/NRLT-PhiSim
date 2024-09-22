using System.Collections;
using LogWriter;
using UnityEngine;
using UnityEngine.SceneManagement;
using LogType = LogWriter.LogType;
using Phigros_Fanmade;
using System.IO;
#if UNITY_ANDROID
using E7.Native;
#endif


public class Play_Canvas_Load : MonoBehaviour
{
    //获取基本游戏对象
    public GameObject JudgeLine;
    public GameObject TapNote;
    public GameObject HoldNote;
    public GameObject DragNote;
    public GameObject FlickNote;
    
    //背景插画
    public GameObject Illistration;
    //public Camera camera;

    // Start is called before the first frame update
    void Start()
    {
        // 应用插画
        Illistration.GetComponent<SpriteRenderer>().sprite = ChartCache.Instance.chart.Illustration;
        
        
        // 从背景插画中获得材质，应用高斯模糊
        Material material = Illistration.GetComponent<SpriteRenderer>().material;
        Texture texture = Illistration.GetComponent<SpriteRenderer>().sprite.texture;
        material.SetVector("_MainTex_TexelSize", new Vector4(1.0f / texture.width, 1.0f / texture.height, 0, 0));

        // 增加高斯模糊的强度
        material.SetFloat("_BlurSize", 5.0f); // 模糊强度设置，往似里写
        material.SetFloat("_Brightness", 0.6f); //图片亮度，适当调整
        
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
        }
        else
        {
            Log.Write("没谱面你加载个集贸(E ON Canvas)", LogType.Error);
            SceneManager.LoadScene(0);
        }
    }

    #region 音频播放部分

#if UNITY_EDITOR_WIN
    IEnumerator MusicPlay(AudioClip music, double time)
    {
        bool isPlay = false;
        AudioSource musicAudioSource = gameObject.AddComponent<AudioSource>();
        musicAudioSource.clip = music;
        musicAudioSource.loop = false; //禁用循环播放
        while (true)
        {
            if (time <= System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1, 0, 0, 0, 0))
                    .TotalMilliseconds && !isPlay)
            {
                isPlay = true;
                musicAudioSource.Play();
            }

            yield return null;
        }
    }
#elif UNITY_STANDALONE_WIN
    IEnumerator MusicPlay(AudioClip music, double time)
    {
        bool MusicisPlay = false;
        AudioSource musicAudioSource = gameObject.AddComponent<AudioSource>();
        musicAudioSource.clip = music;
        musicAudioSource.loop = false;//禁用循环播放
        while (true)
        {
            if (time <= System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds && !MusicisPlay)
            {
                MusicisPlay = true;
                musicAudioSource.Play();
            }
            yield return null;
        }
    }
#elif UNITY_ANDROID
    IEnumerator MusicPlay(AudioClip music, double time)
    {
        bool MusicisPlay = false;
        //预加载音乐
        AudioClip musicAudioClip = music;
        NativeAudioPointer audioPointer;
        audioPointer = NativeAudio.Load(musicAudioClip);
        NativeSource nS = new NativeSource();
        while (true)
        {
            if (time <= System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds && !MusicisPlay)
            {
                MusicisPlay = true;
                nS.Play(audioPointer);
            }
            yield return null;
        }
    }
#elif UNITY_WEBGL
    IEnumerator MusicPlay(AudioClip music, double time)
    {
        bool MusicisPlay = false;
        AudioSource musicAudioSource = gameObject.AddComponent<AudioSource>();
        musicAudioSource.clip = music;
        musicAudioSource.loop = false;//禁用循环播放
        while (true)
        {
            if (time <= System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds && !MusicisPlay)
            {
                MusicisPlay = true;
                musicAudioSource.Play();
            }
            yield return null;
        }
    }
#endif
    
    #endregion

    public void DrawScene()
    {
        //预加载打击音频
        AudioClip tapAudioClip = Resources.Load<AudioClip>("Audio/tapHit");
        
        
        var chart = ChartCache.Instance.chart;
        double unixTime = System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1, 0, 0, 0, 0))
            .TotalMilliseconds + 5000;
        for (int i = 0; i < chart.judgeLineList.Count; i++)
        {
            // 设置父对象为画布
            GameObject parent = GameObject.Find("Play Canvas");
            // 生成判定线实例
            GameObject instance = Instantiate(JudgeLine, parent.transform);
            
            // 设置判定线位置到画布正中间
            instance.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            // 获取预制件的脚本组件
            var script = instance.GetComponent<Play_JudgeLine>();
            // 设置脚本中的公共变量
            script.playStartTime = unixTime;
            script.judgeLine = chart.judgeLineList[i];
            script.whoami = i;
            
            //生成note实例
            for (int j = 0; j < chart.judgeLineList[i].noteList.Count; j++)
            {
                var note = chart.judgeLineList[i].noteList[j];
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

                noteGameObject.GetComponent<Play_Note>().fatherJudgeLine = instance;
                noteGameObject.GetComponent<Play_Note>().note = note;
                noteGameObject.GetComponent<Play_Note>().playStartUnixTime = unixTime;
                noteGameObject.GetComponent<Play_Note>().tapHitClip = tapAudioClip;
                noteGameObject.transform.SetParent(instance.GetComponent<RectTransform>());
            }
        }

#if UNITY_EDITOR_WIN
        StartCoroutine(MusicPlay(chart.music, unixTime));
#elif UNITY_ANDROID
        NativeAudio.Initialize();
        StartCoroutine(MusicPlay(chart.music, unixTime));
#elif UNITY_STANDALONE_WIN
        StartCoroutine(MusicPlay(chart.music, unixTime));
#elif UNITY_WEBGL
        StartCoroutine(MusicPlay(chart.music, unixTime));
#endif
    }
}