using System.Collections;
using System.IO;
using LogWriter;
using UnityEngine;
using UnityEngine.SceneManagement;
using LogType = LogWriter.LogType;
using Phigros_Fanmade;
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

    // Start is called before the first frame update
    void Start()
    {
        ChartCache.Instance.chart = Chart.ChartConverter(File.ReadAllBytes("D:\\PhiOfaChart\\SMS.zip"), "D:\\PhiOfaChart",".zip");
        
        //检查缓存中是否存在谱面
        if (ChartCache.Instance.chart != null)
        {
            //加载谱面

            StartCoroutine(DrawScene());
            
            //DrawScene();
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
#endif
    
    #endregion
    


    IEnumerator DrawScene()
    {
        var chart = ChartCache.Instance.chart;
        double unixTime = System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1, 0, 0, 0, 0))
            .TotalMilliseconds + 10000;
        for (int i = 0; i < chart.judgeLineList.Count; i++)
        {
            Log.Write("F", LogType.Debug);
            // 设置父对象为画布
            GameObject parent = GameObject.Find("Play Canvas");
            Log.Write("I", LogType.Debug);
            // 生成判定线实例
            GameObject instance = Instantiate(JudgeLine, parent.transform);
            
            Log.Write("GTR", LogType.Debug);
            // 设置判定线位置到画布正中间
            instance.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            // 获取预制件的脚本组件
            Log.Write("GTPJ", LogType.Debug);
            var script = instance.GetComponent<Play_JudgeLine>();
            // 设置脚本中的公共变量
            Log.Write("STpST jL wai", LogType.Debug);
            script.playStartTime = unixTime;
            script.judgeLine = chart.judgeLineList[i];
            script.whoami = i;
            Log.Write("i", LogType.Debug);
        }
        
#if UNITY_EDITOR_WIN
        StartCoroutine(MusicPlay(chart.music, unixTime));
#elif UNITY_ANDROID
        NativeAudio.Initialize();
        StartCoroutine(MusicPlay(chart.music, unixTime));
#endif
        yield return null;
    }
}