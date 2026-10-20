# Process Log — final design / 最终设计过程

日期：2026-10-09（Asia/Shanghai）。此前五阶段记录保留在 History/PROCESS_LOG_20261008.md。

只使用真实 Unity 编辑器/Windows 独立程序窗口截图与 ScreenCapture 输出；尚未生成的文件必须标记待采集。生成的地球纹理是运行资源，不作为开发截图。


## 01 — Globe Interface Development

- Screenshot: F01_Globe_Game.png / F01_Globe_Unity.png / F01_GlobeRotated_Windows.png

- Tools: Unity / C# / HLSL

- Technology: 3D Globe Rendering / Geographic Texture Mapping / Object Rotation Interaction / URP

- Operation: 建立球体与地理UV、绘画纹理、鼠标拖动与惯性。

- Chinese Description: 完整油画色彩地球作为艺术作品的空间入口。

- English Description: A rotating painted globe provides a spatial gateway to Impressionist artworks.


## 02 — Landmark Selection & Scene Transition

- Screenshot: F02_LandmarkCard_Game.png / F02_LandmarkCard_Unity.png / F02_LandmarkCard_Windows.png

- Tools: Unity / C# / OnGUI / Camera

- Technology: Physics Raycasting / Landmark Selection / Camera Animation / Timed Transition

- Operation: 吉维尼红点跟随球体；选择后自动转向放大并显示纸色信息卡，第二次点击进入。

- Chinese Description: 通过两阶段地标交互，从地球过渡到吉维尼花园。

- English Description: A landmark focus and paper-like artwork card connect the globe to the garden.


## 03 — Dynamic Environment & Navigation

- Screenshot: F03_NoPeople_Garden_Game.png / F03_DynamicEnvironment_Unity.png / F03_ProxyCollision_Unity.png

- Tools: Unity / Gaussian Splat / C# / HLSL / AudioSource

- Technology: Gaussian Splat Rendering / Irregular Wind Motion / Local Ripples / Character Controller / Proxy Collision

- Operation: 移除人物；加入局部自然运动和合成声；配置岸边与碎石弯道代理。

- Chinese Description: 局部动态对象与限定步行路线，为静态高斯环境增加自然感。

- English Description: Local motion and constrained navigation bring the static splat garden to life without characters.


## 04 — Viewpoint Detection

- Screenshot: F04_Viewpoint_Game.png / F04_ViewpointInspector_Unity.png

- Tools: Unity / C# / Particle System

- Technology: Spatial Detection / Position & Orientation Alignment / Dwell Time

- Operation: 通过位置、俯仰、朝向和3秒注视，自动启动转化；观察点明确为推测。

- Chinese Description: 观众通过位置与观看角度发现推测性的艺术家构图视点。

- English Description: Position and sustained orientation reveal an approximate artist viewpoint.


## 05 — Dissolve & Reassembly Transformation

- Screenshot: F05_AlignedReal_Game.png / F05_DissolveReassembly_Game.png / F05_FinalMonet_Game.png

- Tools: Unity / URP HLSL / Particle System / C#

- Technology: Noise Dissolve / Pigment Particle Reassembly / Original Image Reveal / Camera Lock / Independent 10-second Hold

- Operation: 冻结相机帧，经噪声溶解与原图色彩粒子聚合，完整显示用户原作10秒，自动返回地球。

- Chinese Description: 现实景观分解为颜料颗粒，再重组为选定原作，构成可重复的体验循环。

- English Description: The garden dissolves into pigment particles and resolves into the selected painting before returning to the globe.


## 采集与限制

Game PNG 由最终 Play 回归的真实运行状态采集；Unity 窗口 PNG 补充 Inspector / Hierarchy / Gizmos。截图本身不能证明所有键鼠操作或自然动画连续性。音频是合成，不是现场录音；观察点与碰撞代理是推测和人工校准。

若 GUI 捕获不可用：Play 中截完整球体；选吉维尼截信息卡；Scene 开启 Gizmos选 player/碰撞体；运行中选 Approximate Artist Viewpoint 截阈值；同一轮转化分别截真实、粒子中间态与原图。最终原图无 HUD。

## 已生成的真实证据文件

文件名 Unity 为真实编辑器窗口捕获，Windows 为真实独立程序窗口捕获，Game 为实际 Unity ScreenCapture 输出。

- [F01_Globe_Game.png](ProcessShots/F01_Globe_Game.png) — 已生成，262,617 bytes。

- [F01_Globe_Unity.png](ProcessShots/F01_Globe_Unity.png) — 已生成，154,685 bytes。

- [F01_Globe_Windows.png](ProcessShots/F01_Globe_Windows.png) — 已生成，105,421 bytes。

- [F01_GlobeRotated_Unity.png](ProcessShots/F01_GlobeRotated_Unity.png) — 已生成，155,050 bytes。

- [F01_GlobeRotated_Windows.png](ProcessShots/F01_GlobeRotated_Windows.png) — 已生成，106,296 bytes。

- [F02_LandmarkCard_Game.png](ProcessShots/F02_LandmarkCard_Game.png) — 已生成，482,655 bytes。

- [F02_LandmarkCard_Unity.png](ProcessShots/F02_LandmarkCard_Unity.png) — 已生成，177,318 bytes。

- [F02_LandmarkCard_Windows.png](ProcessShots/F02_LandmarkCard_Windows.png) — 已生成，170,814 bytes。

- [F03_DynamicEnvironment_Unity.png](ProcessShots/F03_DynamicEnvironment_Unity.png) — 已生成，268,215 bytes。

- [F03_NoPeople_Garden_Game.png](ProcessShots/F03_NoPeople_Garden_Game.png) — 已生成，1,027,754 bytes。

- [F03_NoPeople_Garden_Windows.png](ProcessShots/F03_NoPeople_Garden_Windows.png) — 已生成，383,555 bytes。

- [F03_ProxyCollision_Unity.png](ProcessShots/F03_ProxyCollision_Unity.png) — 已生成，287,490 bytes。

- [F04_Viewpoint_Game.png](ProcessShots/F04_Viewpoint_Game.png) — 已生成，954,449 bytes。

- [F04_ViewpointInspector_Unity.png](ProcessShots/F04_ViewpointInspector_Unity.png) — 已生成，266,808 bytes。

- [F05_AlignedReal_Game.png](ProcessShots/F05_AlignedReal_Game.png) — 已生成，951,803 bytes。

- [F05_DissolveReassembly_Game.png](ProcessShots/F05_DissolveReassembly_Game.png) — 已生成，746,988 bytes。

- [F05_FinalMonet_Game.png](ProcessShots/F05_FinalMonet_Game.png) — 已生成，731,974 bytes。

没有制作替代原作、伪造 Inspector 或生成开发截图。HLSL 为手写 URP Shader，未使用 Shader Graph 节点；Particle System 为真实 Unity 粒子系统。