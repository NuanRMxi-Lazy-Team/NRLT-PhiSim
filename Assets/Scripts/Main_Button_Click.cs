using UnityEngine;
using LogWriter;
using LogType = LogWriter.LogType;
using UnityEngine.Localization.Settings;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class Main_Button_Click : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        ChartCache.Instance.debugMode = true;
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
#endif
        //设置移动模式
        ChartCache.Instance.moveMode = ChartCache.MoveMode.WhatTheFuck;
    }


    public void LoadChart()
    {
        //测试文本输出
        Log.Write(LocalizationSettings.StringDatabase.GetLocalizedString("Languages", "Android_Permission_Prompt"));
    }

#if UNITY_ANDROID
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