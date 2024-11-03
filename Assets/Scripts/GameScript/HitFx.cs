using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LogWriter;
using UnityEngine;
using System;
using UnityEngine.Serialization;

public class HitFx : MonoBehaviour
{
    private List<Sprite> hitFxSprites;

    //[FormerlySerializedAs("GameManager")] 
    [HideInInspector]
    public Play_GameManager gameManager;
    private SpriteRenderer spriteRenderer;
    
    private void Start()
    {
        //从ChartCache中获取打击特效
        hitFxSprites = ChartCache.Instance.HitFxs;
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        StartCoroutine(FxUpd());
    }

    private IEnumerator FxUpd()
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
            if (index >= hitFxSprites.Count)
            {
                Destroy(gameObject);
                break;
            }
            spriteRenderer.sprite = hitFxSprites[index];
            //hitFxSprites[index].rect.size是sprite的大小，显示时需要放大40.57%（基于1920*1080的分辨率）
            spriteRenderer.size = new Vector2(hitFxSprites[index].rect.size.x * 1.4057f, hitFxSprites[index].rect.size.y * 1.4057f);
            yield return null;
        }
    }
}