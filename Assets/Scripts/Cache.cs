using System.Collections.Generic;
using Phigros_Fanmade;
using UnityEngine;

public class ChartCache : MonoBehaviour
{
    // 静态私有实例
    private static ChartCache _instance;

    // 静态公共访问点
    public static ChartCache Instance
    {
        get
        {
            if (_instance == null)
            {
                // 如果实例不存在，则查找场景中是否有这个类的实例
                _instance = FindFirstObjectByType<ChartCache>();

                // 如果场景中没有找到，则创建一个新的实例
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject();
                    _instance = singletonObject.AddComponent<ChartCache>();
                    singletonObject.name = typeof(ChartCache) + " (Singleton)";

                    // 标记为DontDestroyOnLoad，这样在场景切换时不会销毁这个对象
                    DontDestroyOnLoad(singletonObject);
                }
            }
            return _instance;
        }
    }

    // 私有字段用于存储值
    private bool _debugMode;
    private MoveMode _moveMode;
    private Chart _chart;
    private List<Sprite> _hitFxs;
    private int _lastSceneIndex = -1;
    
    // 公共属性用于访问和设置值
    public bool debugMode
    {
        get => _debugMode; 
        set => _debugMode = value; 
    }
    
    public MoveMode moveMode
    {
        get =>_moveMode; 
        set => _moveMode = value;
    }
    
    public Chart chart
    {
        get  => _chart; 
        set  => _chart = value; 
    }
    
    public List<Sprite> HitFxs
    {
        get => _hitFxs; 
        set => _hitFxs = value; 
    }
    
    public int LastSceneIndex
    {
        get => _lastSceneIndex; 
        set => _lastSceneIndex = value; 
    }
    
    // 私有构造函数，防止外部实例化
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    public enum MoveMode
    {
        WhatTheFuck,
        Beta//后续支持
    }
}