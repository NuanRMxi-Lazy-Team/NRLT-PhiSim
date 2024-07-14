using System;
using System.IO;
using UnityEngine;

namespace LogWriter
{
    public enum LogType
    {
        Debug,
        Info,
        Warning,
        Error,
        Fatal
    }

    public static class Log
    {
        public static void Write(string message, LogType logtype = LogType.Info)
        {
#if UNITY_EDITOR_WIN
            string logPath = "N";
#elif UNITY_ANDROID
            string logPath = Application.persistentDataPath + "\\log.txt";
#elif UNITY_STANDALONE_WIN
            string logPath = Application.dataPath + "\\log.txt";
#elif UNITY_WEBGL
            string logPath = "N";
#endif
            //检查log文件是否存在，如不存在则创建
            if (logPath != "N")
            {
                if (!File.Exists(logPath))
                {
                    File.Create(logPath);
                }

                StreamWriter sw = new StreamWriter(logPath, true);
                sw.WriteLine(message);
                sw.Close();
                sw.Dispose();
            }

            string now = "";
            if (logPath != "N")
            {
                now = File.ReadAllText(logPath);
            }

            //获取当前时间，yyyy,mm,dd HH,mm,ss
            string nowTime = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]";

            switch (logtype)
            {
                case LogType.Debug:
                    Debug.Log(message);
                    if (ChartCache.Instance.debugMode == true)
                    {
                        now = now + nowTime + " Debug:" + message + "\n";
                    }

                    break;
                case LogType.Info:
                    Debug.Log(message);
                    now = now + nowTime + " Info:" + message + "\n";
                    break;
                case LogType.Warning:
                    Debug.LogWarning(message);
                    now = now + nowTime + " Warn:" + message + "\n";
                    break;
                case LogType.Error:
                    Debug.LogError(message);
                    now = now + nowTime + " Error:" + message + "\n";
                    break;
                case LogType.Fatal:
                    Debug.LogException(new Exception(message));
                    now = now + nowTime + " Fatal:" + message + "\n";
                    break;
            }

            if (logPath != "N")
            {
                File.WriteAllText(logPath, now);
            }
        }
    }
}