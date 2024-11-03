# NRLT-PhiSim
NuanR_Mxi Lazy Team Phigros Simulator，Unity Phigros Sim的重写版本  
~~哎呀重写的和史一样~~  
还是一样，先特别鸣谢：  
他是我跌：不会特效の点缀星空  
感谢他为计算FloorPosition提供了思路  
他也是我跌： ChickenPige0n   
感谢他的PhiDot项目，参考了FloorPosition的计算方式   
## 现版本使用方式  
由于当前项目仍然处在开发阶段，但是它是跨平台的，你可以直接使用Unity6导入本项目并编译任何平台的版本（WebGL未经测试，我们不建议您使用它）。  
在主屏幕，您需要导入一个谱面，然后点击 `开始游玩` 就好了，当前对谱面支持如下：

|        谱面格式         | 支持状态 |
|:-------------------:|:----:|
| Phigros_OfficialV3  |  ✅   |
| Phigros_OfficialV1  |  ❌   |
| RePhiEdit_V100~400  |  ❌   |
|     PhiEdit_V0      |  ❌   |

您导入的谱面中必须含有 `config.json`，否则程序无法识别到您提供的谱面  

|      参数      |              说明              |
|:------------:|:----------------------------:|
|    music     |    谱面对应歌曲，当前只能使用 `wav` 格式    |
| illustration |             谱面曲绘             |
|    chart     | 谱面文件，我们会自动识别谱面类型，只要他是合法的json |

如果导入的谱面有误，我们，，，我们不会怎么样，因为我没有做提示。  
祝你好运。

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
软件源码除插件外，使用MIT License协议开源，插件协议请直接查看插件的协议，如您因为没有遵守插件协议导致的任何麻烦均与本项目无关。  
贡献请在Github上提交PR，有问题发issue，若之issue会被直接关闭。


---
__「Writ deep into NuanR_▇▇ is a name you do not know」__