using TMPro;
using UnityEngine;

public class MessageBox_Scripts : MonoBehaviour
{
    public TextMeshProUGUI text;

    public string showText = "Null";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text.text = showText;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    


    public void MessageBoxClose()
    {
        //摧毁自己
        Destroy(gameObject);
    }
}
