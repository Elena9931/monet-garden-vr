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

# Second round — 2026-10-09 (eight evidence groups)
The Game PNGs below are real Unity ScreenCapture images from the final Play regression. Unity JPGs are real editor window captures. The Uniform Sphere Inspector image is an actual paused sequence for parameter inspection; use Game frames to judge the live composited effect. No AI-generated development screenshots. Earlier first-round evidence remains historical.

## 01 — Earth Material Optimization
Screenshot: [Before, first round](ProcessShots/F01_Globe_Game.png), [After](ProcessShots/R2_F01_Globe_Game.png), [Material Inspector](ProcessShots/R2_01_EarthMaterial_Unity.jpg), [Native drag rotation](ProcessShots/R2_01_EarthRotated_Unity.jpg).
Tools: Unity / Python Pillow / C# / HLSL.
Technology: Equirectangular Texture Mapping, Procedural Impasto Synthesis, URP Lighting, Atmospheric Rim Rendering.
Operation: Processed public-domain Natural Earth raster and land polygons into a 4096×2048 seamless texture; applied the new material to the existing sphere; retained independent Giverny pins and drag inertia.
Chinese Description: 保留真实大陆位置，以程序化厚涂笔触重绘蓝色海洋与金绿色陆地，并增加柔和金色日出及大气辉光。
English Description: A geographically grounded Earth texture translates the globe into an impressionist palette, while soft atmospheric light preserves its three-dimensional form.

## 02 — Glassmorphism UI
Screenshot: [Expanded](ProcessShots/R2_02_GuideExpanded_Game.png), [Collapsed](ProcessShots/R2_02_GuideCollapsed_Game.png), [Canvas Inspector](ProcessShots/R2_02_GuideCanvas_Unity.jpg).
Tools: Unity UGUI / C# / HLSL.
Technology: Screen-Space Canvas, Live Background Sampling, Nine-Tap Blur, Rounded Mask, ScrollRect, Cursor Mode Switching.
Operation: Added a responsive upper-right guide to the existing experience controller; kept all three English passages verbatim; connected paging, scroll, collapse, close, reopen and Tab.
Chinese Description: 磨砂玻璃导览窗以三页内容引导探索，阅读模式与第一人称观察模式可切换。
English Description: A compact frosted guide introduces the garden and leads visitors into exploration through a three-page reading sequence.

## 03 — Animated Viewpoint Marker
Screenshot: [Gold ground cue](ProcessShots/R2_03_GoldMarker_Game.png), [Marker parameters Inspector](ProcessShots/R2_03_MarkerParameters_Unity.jpg).
Tools: Unity / C# / Particle System / Procedural Mesh.
Technology: Breathing Emission, Low-Amplitude Float, Quest-Gated Visibility.
Operation: Reused QuietViewpointLights; added a tiny gold octahedral cue near the existing viewpoint with slow rotation, breathing colour and a two-second float cycle. Confirm on page three enables it; triggering hides it immediately.
Chinese Description: 小型金色菱形与微弱光点标记近地面观察位置，只有完成导览确认才出现。
English Description: A subtle floating gold cue invites discovery of the approximate viewpoint without dominating the garden.

## 04 — Viewpoint Detection
Screenshot: [Aligned garden](ProcessShots/R2_F04_Viewpoint_Game.png), [Detection Inspector](ProcessShots/R2_04_ViewpointInspector_Unity.jpg).
Tools: Unity / C# / Character Controller.
Technology: Spatial Proximity, Yaw/Pitch Alignment, Continuous Dwell Detection.
Operation: Reused the calibrated viewpoint and bank proxies. Added guide confirmation gating; retained simultaneous distance, vertical, yaw and pitch checks with a three-second uninterrupted hold. UI mode pauses the hold.
Chinese Description: 位置与朝向必须同时满足，且持续观察三秒才触发；此位置为构图推测，不是史料确认坐标。
English Description: Position and viewing direction are evaluated together to reveal an approximate artist viewpoint after a sustained gaze.

## 05 — Gaussian Particle Dissolution
Screenshot: [Reality](ProcessShots/R2_F05_AlignedReal_Game.png), [Dissolution](ProcessShots/R2_05_SphereDissolution_Game.png), [Uniform Sphere Points Inspector](ProcessShots/R2_05_UniformSphereInspector_Unity.jpg).
Tools: Unity / GSplat Renderer / C# / HLSL.
Technology: Uniform Sphere Points, GPU Vertex Choreography, Scale Interpolation, Camera Lock.
Operation: The live renderer temporarily switches to its existing Uniform Sphere Points effect; optional Shader parameters interpolate source shapes into sphere points during 0–1.5s. Original buffers are not modified.
Chinese Description: 真实花园逐渐变为球形高斯点，镜头锁定，保留初始空间轮廓。
English Description: The garden dissolves into spherical Gaussian points while the visitor's viewpoint remains fixed.

## 06 — Particle Dispersion
Screenshot: [Live dispersion](ProcessShots/R2_06_Dispersion_Game.png).
Tools: Unity / GSplat / HLSL.
Technology: Deterministic GPU Offsets, Swirl Motion, Depth-Layered Dispersion, Alpha Thinning.
Operation: During 1.5–3.5s source-coloured points spread at varied speeds with a gentle swirl. Near-camera alpha softening reduces intrusive splats; no per-frame CPU iteration over the source points.
Chinese Description: 花园色彩向空间扩散，速度与深度变化由GPU计算；透明排序仍沿用插件原排序，因此这是演出近似。
English Description: Coloured Gaussian points disperse through depth, transforming the garden into a moving field of light and pigment.

## 07 — Particle Reconstruction
Screenshot: [Live reconstruction](ProcessShots/R2_07_Reconstruction_Game.png).
Tools: Unity / C# / HLSL / Static Billboard Mesh.
Technology: GPU Particle Convergence, Painting Colour Sampling, Spatial Reveal Mask.
Operation: During 3.5–5.5s retained splats converge onto a camera-aligned painting plane and sample the supplied image colours. An 18,000-quad static GPU mesh supports the composition; a continuous noise reveal resolves painting structure.
Chinese Description: 粒子向画作平面收束，逐渐采用原作色彩，并通过空间遮罩显现桥与植物构图。
English Description: Dispersed colour gathers into a painting plane as the original composition emerges through a spatial reveal.

## 08 — Impressionist Artwork Reveal
Screenshot: [Complete original](ProcessShots/R2_F05_FinalMonet_Game.png).
Tools: Unity / URP Renderer Feature / C#.
Technology: Aspect-Fit Image Presentation, Seven-Second Sequence, Independent Ten-Second Hold, Resettable State Machine.
Operation: During 5.5–7s particles fade and the exact supplied original resolves without cropping or stretching. A separate clock begins after its first complete frame; measured hold 10.0051s, then automatic return and a second complete cycle.
Chinese Description: 七秒转换后完整显示用户原作，独立停留十秒，自动返回地球并重置流程。
English Description: The supplied original painting resolves in full, remains still for ten seconds, and gently returns visitors to the globe.

## Round 2 verification limits
70 real Play checks passed across two loops. Native editor mouse drag, Giverny selection, card entry, guide scroll, paging, Confirm and reopen were exercised. These checks do not prove every human walking route, artistic quality or mobile VR performance. Screenshots are still images, not evidence of temporal smoothness. Actual standalone build/startup results are recorded in ROUND2_REPORT.md. Final Windows native guide evidence: [Expanded](ProcessShots/R2_02_Guide_Windows.jpg), [Collapsed](ProcessShots/R2_02_Collapsed_Windows.jpg), [Reopened](ProcessShots/R2_02_Reopened_Windows.jpg). The beforeHeaderFix image documents the earlier spacing issue and is not the final UI.
