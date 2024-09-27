using TMPro;
using UnityEngine;
using LogWriter;
using LogType = LogWriter.LogType;

public class MessageBox_Scripts : MonoBehaviour
{
    public TextMeshProUGUI text;

    public string showText = "Null";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text.text = showText;
        //Log.Write("MessageBox_Scripts Start", LogType.Debug);
    }

    public void MessageBoxClose()
    {
        //摧毁自己
        Destroy(gameObject);
    }
}
