# Monet Garden VR — Process Log / 作品集过程记录

日期：2026-10-08（Asia/Shanghai）。只管理五个关键阶段。图片必须来自实际 Unity 编辑器或 Game 输出；测试图案不得当作原作、开发图或作品集结果。

## S01 — Spatial Reconstruction & Import / 空间构建与导入

- 工具：用户之前使用的 World Labs Marble / Marble Studio；本次使用 Unity 2022.3、gsplat-unity-vfx、PLY 只读采样。
- 技术：Gaussian Splatting / PLY Import / Real-time Rendering。
- 操作：核对已有 monet-garden.ply、1,824,584 splats 和原变换；从未保存的真实编辑器场景另存原型。没有重新生成 Marble 世界。
- 验证：真实 Game view 已观察到花园，初始有 WalkableGround_Test 遮挡；后续在副本停用。
- 截图：已保存真实 Unity Game + GSPlat 绑定 Inspector：`S01_GaussianSplat_Unity.png`。点数由 PLY header 与 Unity GsplatAsset 交叉核验；此截图未同时显示 point count。
- 中文：将 Marble 导出的高斯泼溅场景导入 Unity，建立可实时观看的莫奈花园推测性重构。
- English: The Marble-exported Gaussian splat environment is rendered in Unity as a speculative reconstruction of Monet's garden.

## S02 — Player Navigation & Collision / 玩家移动与碰撞

- 工具：Unity、C#、Character Controller。
- 技术：First-person Navigation / Gravity / Proxy Collision / Fall Recovery。
- 操作：原脚本保留；新增 GardenDesktopController。校正副本胶囊、保持相机世界视线、创建狭窄 approach/bank 代理及池塘/边界阻挡，支持无地面时暂停重力。
- 限制：道路与米制尺度为待校准原型；不可声称整个花园/日本桥碰撞已精确完成。
- 测试：实际 Play 中重力落地、北侧水域、南侧池塘、西侧边界与复位通过，详见 `Validation/play_validation.txt`。岸边路径与桥面仍需逐段人工校准。
- 截图：`S02_Collision_Unity.png`，Scene 中选 GardenProxyColliders_PROVISIONAL，开启 Gizmos，并显示 player 的 Character Controller。
- 中文：通过简化碰撞代理与角色控制器，为静态高斯场景建立可行走区域与水域边界。
- English: Simplified proxy colliders and a Character Controller define a walkable prototype route and water boundaries within the static splat environment.

## S03 — Dynamic Environment & Characters / 动态环境与人物

- 工具：Unity、C#、ShaderLab/HLSL、Animator、AnimationClip。
- 技术：Vertex Sway / Local Water Ripple Overlay / Procedural Bird Flight / Animator State Machine / Waypoint Motion。
- 操作：独立柳叶带、局部水纹、睡莲轻晃、鸟翼/轨迹；程序化人物占位以 Idle/Walk/Pause 动画沿路径移动。G 开关动态层。
- 限制：不能直接使原 splat 中的每株植物或人物运动。水纹贴片使用叠加折中，人物不是正式授权模型。
- 测试：Play 中 Animator 存在且人物实际沿路径位移通过；截图已保存真实运行与 Animator Inspector。尚未录视频，仍建议录 10 秒验证摆动和遮挡。
- 截图：`S03_Dynamics_Unity.png`；可补 10 秒 Game view 视频，不使用伪造动态图。
- 中文：用独立的轻量动态对象，为静态高斯场景增加局部自然运动，并以可替换人物占位验证动画流程。
- English: Lightweight animated objects add local natural motion to the static splat environment; a replaceable procedural character validates the animation workflow.

## S04 — Artist Viewpoint Detection / 画家视角识别

- 工具：Unity、C#、Scene Gizmos。
- 技术：Position & Orientation Detection / Dwell Time / Explicit Confirmation。
- 操作：MonetViewpoint_PROPOSED 保留初始相机构图；检测水平距离、高度差、水平朝向和俯仰；持续 3 秒后 E 确认。不瞬移或强制转动镜头。
- 限制：这是构图观察点，未经历史站位考证与原作构图配准。
- 截图：`S04_Viewpoint_Unity.png`；Scene 选视点显示黄色 Gizmo，并在 Game view 展示距离/角度/hold 提示。
- 中文：在接近预设构图的位置并保持正确观看角度后，系统允许观众启动画风转化。
- English: Position, orientation and sustained gaze identify a proposed composition viewpoint before the visitor confirms the painterly transition.

## S05 — Painterly Transformation / 现实到印象派的视觉转化

- 工具：Unity URP 14、ScriptableRendererFeature、Blitter、HLSL。
- 技术：Colour Grading / Multi-tap Softening / Kuwahara-style Edge-preserving Filter / Colour Quantization / Procedural Stroke Texture / Aspect-preserving Image Blending。
- 操作：复制原 URP pipeline/renderer，保留 GSplat 排序 feature；仅运行原型时切换到副本管线。转化分阶段增强色彩、软化细节和笔触感，并提供原作图片淡入接口。
- 测试：实际 GPU 帧验证见 Validation。若使用纯色纹理诊断图片接口，明确为测试输入，不能作为原作截图。
- 截图：`S05_RealWorld_Game.png` → `S05_Transition_Game.png` → `S05_PainterlyStudy_Game.png`（最后一帧目前是画风研究画面，非原作）。原作接入后再补 `S05_OriginalPainting_Game.png`。
- 中文：在同一观看构图上逐渐改变色彩、细节与笔触感；选定原作图片提供后，再完成与原作的图像衔接。
- English: Colour, detail and brush-like marks gradually transform the same view. The final blend into a specific original painting remains dependent on the supplied artwork image.

## 手动补拍要求

若自动截图缺少 Inspector：打开原型；Game view 设为 16:9、缩放 1；S01 选 GSPlat，S02 选 player 并用 Scene 的 Gizmos 显示代理，S03 选 Gardener 并录 10 秒动画，S04 选 MonetViewpoint_PROPOSED 与运行提示同框。S05 用同一视线分别保存现实/转化/完成画面。每张保留原始截图，在本日志补实际文件名及日期。

## 本次实际采集文件

2026-10-08，Asia/Shanghai。S01/S02/S03/S04 的 Unity 截图来自 Windows Computer Use 的原始窗口捕获；S04/S05 的 Game PNG 来自 Unity ScreenCapture。没有合成或修饰成功界面。

|阶段|实际文件|可证明的内容|
|---|---|---|
|S01|S01_GaussianSplat_Unity.png|已有 GSPlat 的 monet-garden 绑定、保留的变换和真实花园渲染|
|S02|S02_Collision_Unity.png|真实 Scene 代理 Gizmos 与 Character Controller 参数|
|S03|S03_Dynamics_Unity.png|实际运行的局部环境、人物占位、Animator 与路径组件|
|S04|S04_Viewpoint_Unity.png / S04_Viewpoint_Game.png|实际位置/角度/3 秒阈值配置和正常检测完成提示|
|S05|S05_RealWorld_Game.png / S05_Transition_Game.png / S05_PainterlyStudy_Game.png|相同视线的现实→转化→画风研究画面；最后一帧不是原作|

首轮截图曾因异步保存时机带入测试图案标签，已移出 ProcessShots 放到 Validation，并在修复后重拍。该首轮图片不得进入作品集。
