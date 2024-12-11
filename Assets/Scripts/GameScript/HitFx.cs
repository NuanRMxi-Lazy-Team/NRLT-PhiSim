using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR_WIN
using E7.Native;
#endif


public class HitFx : MonoBehaviour
{
    private List<Sprite> _hitFxSprites;

    //[FormerlySerializedAs("GameManager")] 
    [HideInInspector]
    public Play_GameManager gameManager;
    public RectTransform rectTransform;
    public AudioSource audioSource;

    private SpriteRenderer _spriteRenderer;

    // 打击音效类别
    public int hitType;

    private void Start()
    {
        //1为Tap、2为Hold、3为Flick、4为Drag，从GameManager中获取对应的音效
        AudioClip hitAudioClip = null;
        hitAudioClip = hitType switch
        {
            1 => gameManager.tapAudioClip,
            2 => gameManager.tapAudioClip,
            3 => gameManager.flickAudioClip,
            4 => gameManager.dragAudioClip,
            _ => gameManager.tapAudioClip
        };
        // 立即加载打击音效
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        audioSource.clip = hitAudioClip;
        audioSource.loop = false;
        audioSource.Play();
#elif UNITY_ANDROID || UNITY_IOS
        NativeAudioPointer adp = NativeAudio.Load(hitAudioClip);
        NativeSource mAS = new NativeSource();
        mAS.Play(adp);
#endif
        //从ChartCache中获取打击特效
        _hitFxSprites = ChartCache.Instance.HitFxs;
        
        _spriteRenderer = GetComponent<SpriteRenderer>();
        StartCoroutine(FxUpdate());
    }

    private IEnumerator FxUpdate()
    {
        float startTime = gameManager.curTick;
        int index = 0;
        //顺序播放打击特效，每帧播放一张（60fps）
        while (true)
        {
            if (gameManager.curTick - startTime >= 16.67f)
            {
                index++;
                startTime += 16.67f;
            }
            //如果特效播放完毕，销毁自己
            if (index >= _hitFxSprites.Count)
            {
                Destroy(gameObject);
                break;
            }
            _spriteRenderer.sprite = _hitFxSprites[index];
            //hitFxSprites[index].rect.size是sprite的大小，显示时需要放大1.375倍
            _spriteRenderer.size = new Vector2(_hitFxSprites[index].rect.size.x * 1.375f, _hitFxSprites[index].rect.size.y * 1.375f);
            yield return null;
        }
    }
}