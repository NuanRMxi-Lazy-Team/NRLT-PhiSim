using System;
using UnityEngine;
using LogWriter;
using LogType = LogWriter.LogType;
using UnityEngine.Localization.Settings;
using Phigros_Fanmade;
using UnityEngine.SceneManagement;
using Keiwando.NFSO;

#if UNITY_ANDROID
using UnityEngine.Android;
using System.IO;
#endif

public class Main_Button_Click : MonoBehaviour
{
    public GameObject messageBox;
    // Start is called before the first frame update
    void Start()
    {
        ChartCache.Instance.debugMode = true;
        
        #region 权限相关
#if UNITY_EDITOR_WIN
        Log.Write("Start On UNITY_EDITOR.", LogType.Debug);
#elif UNITY_ANDROID
        Log.Write("Start On UNITY_ANDROID.", LogType.Debug);
        //获取权限
        Apply_for_read_permission:
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            Log.Write("读权限不足，申请External Storage Read权限", LogType.Debug);
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
            goto Apply_for_read_permission;
        }
        Log.Write("已获取读权限", LogType.Debug);
        Apply_for_write_permission:
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Log.Write("写权限不足，申请External Storage Write权限", LogType.Debug);
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
            goto Apply_for_write_permission;
        }
        Log.Write("已获取写权限", LogType.Debug);
        ShowToast(LocalizationSettings.StringDatabase.GetLocalizedString("Languages", "Android_Permission_Prompt"));

#elif UNITY_STANDALONE_WIN
        Log.Write("Start On UNITY_STANDALONE_WIN.", LogType.Debug);
#elif UNITY_WEBGL
        Log.Write("Start On UNITY_WEBGL.", LogType.Debug);
        //弹出MessageBox
        GameObject parent = GameObject.Find("Main_Panel");
        GameObject instance = Instantiate(messageBox, parent.transform);
        instance.GetComponent<MessageBox_Scripts>().showText = "Unsupperted WEBGL...";
        //抛出未实现的功能异常
        throw new NotImplementedException("Unsupperted WEBGL...");
#endif
        

        #endregion

        #region 屏幕适配相关
        
        // 获取屏幕的Safe Area
        Rect safeArea = Screen.safeArea;

        // 将Safe Area转换为相对坐标
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // 获取RectTransform组件
        RectTransform rectTransform = GetComponent<RectTransform>();

        // 设置锚点以适应Safe Area
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;

        // 输出Safe Area信息
        Log.Write("Safe Area: " + safeArea,LogType.Debug);
        

        #endregion
        //设置移动模式
        ChartCache.Instance.moveMode = ChartCache.MoveMode.WhatTheFuck;
    }
    
    public void LoadChart()
    {
        try
        {
            
            //提示选取与加载谱面
            NativeFileSO.shared.OpenFile(SupportedFilePreferences.supportedFileTypes, (isOpen, file) =>
            {
                if (isOpen)
                {
                    Log.Write(file.Name);
#if UNITY_EDITOR
                    ChartCache.Instance.chart = Chart.ChartConverter(file.Data, "D:\\PhiOfaChart",file.Extension);
#else
                    ChartCache.Instance.chart = Chart.ChartConverter(file.Data, Path.GetTempPath(),file.Extension);
#endif
                    //弹出MessageBox
                    GameObject parent = GameObject.Find("Main_Panel");
                    GameObject instance = Instantiate(messageBox, parent.transform);
                    instance.GetComponent<MessageBox_Scripts>().showText = "Chart is Loaded";
                }
            });
        }
        catch (Exception e)
        {
            Log.Write("Unknown errors in: " + e.Message, LogType.Error);
            //弹出MessageBox
            GameObject parent = GameObject.Find("Main_Panel");
            GameObject instance = Instantiate(messageBox, parent.transform);
            instance.GetComponent<MessageBox_Scripts>().showText = e.Message;
        }
    }

    public void Play()
    {
        if (ChartCache.Instance.chart != null)
        {
            //进入测试播放屏幕
            SceneManager.LoadScene(1);
        }
        else
        {
            Log.Write("没谱面你播放个集贸（E:Main Not Load Chart)", LogType.Error);
            //弹出MessageBox
            GameObject parent = GameObject.Find("Main_Panel");
            GameObject instance = Instantiate(messageBox, parent.transform);
            instance.GetComponent<MessageBox_Scripts>().showText = "unknown Chart...";
        }
    }
#if UNITY_ANDROID
    /// <summary>
    /// Android Toast Show
    /// </summary>
    /// <param name="toastString"></param>
    public void ShowToast(string toastString)
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            AndroidJavaClass UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                AndroidJavaObject toast = new AndroidJavaClass("android.widget.Toast");
                AndroidJavaObject javaString = new AndroidJavaObject("java.lang.String", toastString);
                AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
                AndroidJavaObject toastObject = toast.CallStatic<AndroidJavaObject>("makeText", context, javaString,
                    toast.GetStatic<int>("LENGTH_SHORT"));
                toastObject.Call("show");
            }));
        }
    }
#endif
}