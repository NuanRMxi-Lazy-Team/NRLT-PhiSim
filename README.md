# NRLT-PhiSim

NuanR_Mxi Lazy Team Phigros Simulator，Unity Phigros Sim的重写版本  
~~哎呀重写的和史一样~~  
还是一样，先特别鸣谢：  
他是我跌：不会特效の点缀星空  
感谢他为计算FloorPosition提供了思路  
他也是我跌： ChickenPige0n   
感谢他的PhiDot项目，参考了FloorPosition的计算方式

## 现版本使用方式

由于当前项目仍然处在开发阶段，但是它是跨平台的，你可以直接使用Unity6
26f1c1导入本项目并编译任何平台的版本（WebGL用不了，哈哈）。  
在主屏幕，您需要导入一个谱面，然后点击 `开始游玩` 就好了，当前对谱面支持如下：

|        谱面格式        | 支持状态 |
|:------------------:|:----:|
| Phigros_OfficialV3 |  ✅   |
| Phigros_OfficialV1 |  ❌   |
|     RePhiEdit      |  ??  |
|     PhiEdit_V0     |  ❌   |

您导入的谱面无需有配置文件，我们会帮你解决

~~但是我仍然做了多语言兼容，现在它支持简体中文，繁体中文和英文~~

## 资源补充

- 你需要对部分资源进行补充，包含音符的材质，判定线材质，打击效果，打击音效，默认曲绘等，结构如下：

```
---
Assets
 | Resources
  -| HitFx
  --| *.png
  -| Audio
  -| dragHit.wav
  -| flickHit.wav
  -| tapHit.wav
 | Texture
 -| Default_illustration.png
 -| DragNote.png
 -| TapNote.png
 -| FlickNote.png
 -| HLDragNote.png
 -| HLFlickNote.png
 -| HLTapNote.png
 -| Hold2.png
 -| Hold2HL.png
 -| Hold_Body.png
 -| Hold_End.png
 -| Hold_Head.png
 -| JudgeLine.png
```

- 对于HitFx，你需要自行更改代码中的部分内容

## 声明

本项目与南京鸽游无关，如有侵权，请及时将修改通知发送到以下邮箱：  
nrlt@nuanr-mxi.com  
软件源码除插件外，使用GPL v3.0协议开源，插件协议请自行查看，如您因为没有遵守插件协议造成的任何麻烦均与本项目无关。  
贡献请在Github上提交PR，有问题发issue，若之issue会被直接关闭。

## 开源项目引用

### GPL v3
FFmpeg：https://github.com/FFmpeg/FFmpeg  
FFmpeg-Kit：https://github.com/arthenica/ffmpeg-kit  
PhiDot：https://github.com/ChickenPige0n/PhiDot  
phi-chart-render：https://github.com/MisaLiu/phi-chart-render

### MPL-2.0
PhiZone Player：https://github.com/PhiZone/player

---
__「Writ deep into NuanR_▇▇ is a name you do not know」__
