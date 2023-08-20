## 集成ChatGPT和微软Azure语音功能的虚拟导游
## 目录
- <a href="## 介绍">介绍</a>
- <a href="## 安装和包">安装和包</a>
- <a href="## 使用方法">使用方法</a>
- <a href="## 展示">展示</a>
## 介绍
该项目是我在格拉斯哥大学为完成msc project做的选题。该项目通过AR虚拟人像（实际上并没有,只能算伪AR）+ ChatGPT3.5+Azure语音功能，打造了一个能与游客沟通的虚拟导游，亨特博士，亨特博物馆展品的前主人。Demo运行的设备是Meta quest 2， 但由于时间有限，目前进展到需要通过Quest2自带的Oculus link功能与PC实现串流连接，在Unity editor工作状态下才能看到一个较完整的功能。下文介绍必要的包以及使用方法。
## 安装和包
该部分介绍如何安装必要的开发工具以及包
### Unity
由于后续用到的实现ChatGPT功能的OpenAI-DotNet包需要Unity 2021.3以以上，该demo使用的版本是2022.3.4f1。且后续demo被移植到Meta quest2进行测试，在安装项目时， 勾选安卓SDK，如图。
<img src="https://github.com/midnightc160/Hunterian/assets/122289736/2fc984d6-11b0-4529-8838-d2d3e771bc7c" width="1000" height="200">
### Oculus
具体的配置Unity环境以兼容Oculus设备的步骤见如下文档：

[Set Up Oculus and Unity for Oculus Quest](https://github.com/midnightc160/Hunterian/files/12388502/Set.Up.Oculus.and.Unity.for.Oculus.Quest.pdf)

1. PC端下载Meta development hub, 使用脸书账号或直接注册Meta账号，按照文档(P5)进行项目设置。开发者模式下设置项目需要身份认证(银行卡、手机号、邮箱)。
2. 移动端下载Oculus 应用，连接设备后，按照文档指示，开启开发者模式。
3. PC下载[Oculus ADB Driver](https://developer.oculus.com/downloads/package/oculus-adb-drivers/?locale=en_GB)以及SideQuest应用，用于后续将生成的.apk打包发送给设备。

### [OpenAI-DotNet](https://github.com/RageAgainstThePixel/OpenAI-DotNet)
ChatGPT功能的实现依赖该非官方包，因此你需要在[OpenAI官网](https://platform.openai.com/)注册账号，并在“我的账号”->“管理账号”中设置项目，并获取API Key。计费标准以及使用情况均可以在界面中确认。

包安装方式：在Unity项目中，“package manager”->"add package from URL"，复制OpenAI-DotNet github链接即可。

目前该包似乎仅可在桌面端应用，这或许是为什么我的demo目前只能在设备开启Oculus link功能与PC实现串流连接后在unity工作状态下才能正常工作的原因(目前做进一步跟进，仅仅是推测)。当应用被实际打包成apk格式导入到Quest2后，可以正常看到虚拟人像，语音识别功能正常，但调用OpenAI API后，一直卡在加载状态。等待该包后续更新或使用其他包。

### Azure语音功能
语音识别(Speech to Text)以及文本转语音(Text to Speech)需要用到[Azure SDK](https://learn.microsoft.com/zh-cn/azure/ai-services/speech-service/quickstarts/setup-platform?pivots=programming-language-csharp&tabs=windows%2Cubuntu%2Cdotnetcli%2Cunity%2Cjre%2Cmaven%2Cnodejs%2Cmac%2Cpypi),点击下载Unity版本，并将其拖入项目中。

同样地，在[Azure服务](https://portal.azure.com/#home)界面搜索“Speech Service”，点击“创建”进入创建页面,如图。

<img src="https://github.com/midnightc160/Hunterian/assets/122289736/59c873dc-34b4-4f21-b770-6377b0fcca11" width="600" height="300">

“资源组”“名称”两栏填写根据个人需求填写项目名称，“定价层”填写“standard s0”，点击“下一步”直到点击“创建审阅”，等待创建成功。点击“转到资源”，在新生成的项目中保存私人的Key以及Region，在后续步骤中使用。

文本转语音中独特的区域口音参考了[Azure language Library]([https://learn.microsoft.com/zh-cn/azure/ai-services/speech-service/speech-synthesis-markup-voice](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-support?tabs=stt)https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-support?tabs=stt)，本demo支持英/西/汉三语。

### Vroid Studio & Vroid插件包
该demo中，亨特博士的人物模型产自[Vroid Studio](https://vroid.com/en/studio)，整体风格偏卡通化，但模型设计易于上手；Vroid插件包用于调控人物面部的微表情，在[此处](https://github.com/vrm-c/UniVRM/releases/tag/v0.110.0)下载，并直接导入Unity项目中。

### [线程插件](https://github.com/PimDeWitte/UnityMainThreadDispatcherhttps://github.com/PimDeWitte/UnityMainThreadDispatcher)
用于解决Unity输入栏的BUG

## 使用方法
该部分主要介绍用户需要自行修改的部分，以及如何运行该程序。

### 脚本参数修改
双击打开“Assets”->"AIdialogue assets"->"Scenes"中的“AIdialogue”场景，找到左侧层级中的“ChatManager”(主要的脚本文件都在这边)。

在"Chat"脚本的inspector面板中，如图所示，用户需要输入自己的API Key，配置AI role等相关参数。当获得鼠标悬停在参数名称上时，可以获得具体的说明，例如“temperature”参数用以控制AI返回文本的随机性，取值区间为0~1，值越大返回的文本内容越随机。

<img src="https://github.com/midnightc160/Hunterian/assets/122289736/15cfbf4f-b3a5-49ee-a2d0-60695f507aba" width="600" height="800">



在Chat脚本内部需要为ChatGPT提供一些基本信息。
在Prompt区域，使用者需要根据自己的需求，补全你希望ChatGPT扮演角色的相关参数，包括需求设定/扮演角色背景设定/语气设定。

<img src="https://github.com/midnightc160/Hunterian/assets/122289736/c96ae736-332d-4b23-8137-81ee40c5a48c" width="1000" height="200">

<img src="https://github.com/midnightc160/Hunterian/assets/122289736/7d7e0b3e-25ff-4e05-b43e-17e69bb4611f" width="1000" height="200">

<img src="https://github.com/midnightc160/Hunterian/assets/122289736/cb1c086d-03bf-46ee-894e-eb6a2183e8b4" width="1000" height="200">

在“ChatManager”的"Speech to Text", "Text to Speech"脚本的inspector面板中填写你的Azure Key以及Region。
<img src="https://github.com/midnightc160/Hunterian/assets/122289736/3a62b1f8-961f-41a0-b0a5-8926a7ed8983" width="500" height="600">

在"Text to Speech"脚本剩下的参数用于控制人物模型围观动作的幅度。










### 指令修改

## 展示 


