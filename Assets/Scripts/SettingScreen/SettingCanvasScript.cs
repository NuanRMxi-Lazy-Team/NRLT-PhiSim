using UnityEngine;
using UnityEngine.UI;

public class SettingCanvasScript : MonoBehaviour
{
    // Check Box
    public Toggle debugModeToggle;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 初始化设置，从设置中读取设置
        debugModeToggle.isOn = PlayerPrefs.GetInt("debugMode") == 1;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    // 保存事件
    public void Save()
    {
        // 保存设置
        PlayerPrefs.SetInt("debugMode", debugModeToggle.isOn ? 1 : 0);
    }
}
