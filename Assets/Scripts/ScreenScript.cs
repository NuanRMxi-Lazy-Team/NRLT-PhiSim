using UnityEngine;
using UnityEngine.SceneManagement;

public class ScreenScript : MonoBehaviour
{
    private int _lastSceneIndex;
    private void Start()
    {
        // 先获取当前已存储的场景编号
        _lastSceneIndex = ChartCache.Instance.LastSceneIndex;
        
        // 获取当前场景编号
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        // 赋值给Cache中的_lastSceneIndex
        ChartCache.Instance.LastSceneIndex = sceneIndex;
        // 是否为null
        if (_lastSceneIndex == -1)
        {
            _lastSceneIndex = sceneIndex;
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //如果场景编号等于当前场景，什么也不做，否则返回上一个场景
            if (_lastSceneIndex != SceneManager.GetActiveScene().buildIndex)
            {
                SceneManager.LoadScene(_lastSceneIndex);
            }
        }
    }
}
