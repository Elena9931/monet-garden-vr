> 最新交付（2026-10-09）：请先读取 CODEX_FINAL_HANDOFF.md 与 Documentation/FINAL_REPORT.md。最新设计已移除人物，采用位置与角度保持3秒自动触发、指定原作独立10秒与自动返回地球；下文为历史交接，不应恢复旧人物或E键流程。最终Play回归55项通过，最新Windows构建0错误/0警告，PICO未实现与测试。

# Monet Garden VR — Codex 项目交接说明

> 本文是此前 ChatGPT 多轮指导的明确交接摘要。以磁盘上的 Unity 工程为技术事实来源；聊天记录仅作为辅助参考。Codex 不应假设能自动继承过往聊天记忆。

## 1. 项目愿景 / Design brief

项目主题：**From Viewing a Painting to Entering the Painting（从观看画作到进入画作）**。

目标体验：观众佩戴 PICO VR，从全球地球空间选择一幅印象派作品，进入画家真实创作地点的**研究驱动的推测性重构**，观察会运动的光、水、树木、植物、人物与声音，自由探索并寻找画家的大致观察位置。当玩家移动到恰当的位置和视角时，真实景观依次经历光线、色彩、细节与笔触变化，最后与选定的原作画面衔接，从而理解画家的观看方式和创作过程。当前可运行原型以**莫奈法国吉维尼水花园／日本桥／睡莲池**为唯一已实现空间。

关键设计原则：让用户理解画家如何观察，而非单纯展示3D风景。场景从*照片写实*逐渐转入*原作画风*；不得把未经考证的AI生成环境声称为精确历史复原。具体画作版本、年代、授权图像、构图对照仍需要用户确认与提供。

## 2. 已有项目技术事实（以实际文件检查为准）

- Windows，本地 Unity **2022.3.62f3c1**，项目为 URP Empty / Universal Render Pipeline。当前场景主要为 `SampleScene`。
- 已安装并能工作的插件目录 `Assets/gsplat-unity-vfx`，用于读取和显示 Gaussian splats。还有 `Assets/GaussianEffects` 等已有组件。**不要盲目更换高斯渲染包或更新 Unity**。
- 图片工作流：写实莫奈花园AI参考图 → World Labs **Marble**（Image-to-World） → **Marble Studio**（Import World）→ 导出 `monet-garden.ply` → Unity 导入为 `Monet-garden (Gsplat Asset)` → 场景 `GSplat` 的 `Gsplat Renderer` 绑定此资源。
- Unity 的 PLY 导入后 Inspector 显示约 **1,824,584 splats**；花园成功显示。GSplat 的历史变换为 Position `(0,0,0)`、Rotation `(0,0,180)`、Scale `(1,-1,1)`，这是目前正确显示的组合，不可无依据改动。
- Hierarchy 中存在 `player`（小写） → `Main Camera`（子对象），以及 `GSplat`、`Global Volume`、`GSVolumeLights`、`Gsplat Shape View`、`Dolly Track`、`Virtual Camera`、`CameraSequence` 等样例对象。样例组件可能存在遗留引用，修改前先检查。
- `player` 已附加 Unity 内置 `Character Controller`，现有检查器历史参数：Height 2、Radius 0.5、Center (0,0,0)、Step Offset 0.3、Slope Limit 45。**这些尚未校准到现实步行尺度与地面高度。**
- `player` 已附加自制 `GardenWalkController.cs`，位于 `Assets/Scripts/`；`Main Camera` 有相机和 Audio Listener，移动脚本已从相机移到玩家根对象。
- 已测试成功：通过 `CharacterController.Move` 实现 **WASD 移动、Shift 加速、鼠标转头、Esc 释放鼠标**；当前没有重力、可行走地面代理碰撞体、桥梁碰撞和水域阻挡。虽然 Character Controller 组件已添加，**尚未证明地形物理碰撞有效**。
- 现有主摄像机已在莫奈花园中对准日本桥。保留初始构图，未经用户批准不要重置相机、删除 `SampleScene` 或覆盖现有 GSplat。
- 当前阶段前尚未完整实现：动态自然环境、人物动画、画家视点触发、印象派视觉变化、PICO XR 真机适配，以及全球地球式作品选择主页。

## 3. 需要 Codex 完成的内容（按最短可演示闭环排序）

### P0 — 项目检查与备份（必须先完成）

1. 确认 Unity 根目录（存在 `Assets/`, `Packages/`, `ProjectSettings/`）、读取 `ProjectSettings/ProjectVersion.txt`、Unity 包列表、当前场景和实际脚本，识别 URP 渲染设置及 GSplat 组件。检查 Console / 编译错误及缺失脚本，不要删除无关素材。
2. 先备份场景或建立版本控制提交；新增功能优先在 `MonetGarden_Prototype.unity` 等场景副本中实现。避免覆盖用户测试成功的原场景，明确变更日志。
3. 评估 Gaussian splat 是否能在当前 URP、Windows DX12 工作；在启用 XR/PICO 前先验证桌面版本。

### P1 — 可行走区域与角色碰撞

1. 保留 `player → Main Camera`，重构 `GardenWalkController`，使水平移动、鼠标水平/俯仰旋转和 Character Controller 的碰撞逻辑分工清楚；先确认原功能不退化。
2. 校正角色胶囊高度、中心与摄像机眼高。当前根节点与相机位置并非标准脚底原点，不能直接套用默认重力参数。
3. 在岸边或小路等确实可行走位置布置不可见 **proxy collider**（Box Collider / 简化 Mesh Collider）；阻挡水面、不能通行的植物与场景边界。桥需要用单独代理网格/桥梁模型构建碰撞（视模型质量决定）；不要把整个池塘设为地面。
4. 启用符合碰撞体设置的重力、落地检测；若缺少可靠地面碰撞，保留可回退的无重力演示模式，不能导致用户一按 Play 就跌落。

### P2 — 动态自然环境及人物

1. 高斯泼溅为**静态视觉环境**，不能直接给 splat 中单个柳树、花叶或人物挂 Animator。用少量独立 Unity 3D 对象/粒子/材质模拟柳枝摆动、局部睡莲轻晃、浅水波纹、飞鸟/蝴蝶、自然声音，并提供可开关或调整的演示设置。
2. 检查 splat 与新水面、植被重复渲染造成的 z-fighting / 遮挡问题；应局部遮盖、掩膜或精心布置，绝不可假定一层完全透明水面贴片就能覆盖原水纹。
3. 加一个可动的人物（适合历史年代的中性人物或合适授权模型），包含 Idle / Walk / Pause 动画状态；若缺少角色模型，先实现可替换的角色控制脚本与路径，并列出用户需要提供的资产，不得虚构模型已可用。
4. 优先做一处观察点附近的局部动态环境，不要求1:1让场景每个物体都独立动。

### P3 — 画家视点识别 + 现实转画作（核心体验）

1. 在编辑器中用空对象如 `MonetViewpoint` 表示**推测性**观察点，提供Scene Gizmo；检测玩家位置距离、水平朝向角（可选垂直俯仰）及维持时长，例如3秒，所有阈值 Inspector 可调。
2. 玩家靠近时给出轻量导航与视角对准提示，成功后锁定/缓动相机或提供用户确认，不要突然强制瞬移。
3. 在 URP 中制作视觉过渡（光照/色彩→画面柔化与色块→笔触或画面化→与原作图像对齐淡入）；优先可靠可播放的实现，复杂全屏shader失败则有可回退的简化版本。不能只用任意一张“莫奈风”图片替代明确作品的原作。
4. 构图匹配依赖**用户提供的具体原作文件**（例如选定《日本桥》某一幅的图像、题名、年代、来源）和在场景中手动校准的相机视点/FOV。若没有原作，请先做占位并明确报告阻塞，不要编造画家历史站位。
5. 必须可重置体验、再次探索，支持用户演示“现实 / 转化中 / 完成作品”的三个状态。

### P4 — 全球作品入口 + PICO VR（时间允许时）

1. 以最小可用的地球／地图选画入口开始，只需要法国吉维尼一个有效入口，后续可扩充其他画家与作品。不要生成不存在的其他场景作为已完成项。
2. 调研**与 Unity 2022.3、URP 和用户具体 PICO 机型兼容**的 XR SDK / OpenXR / PICO 配置。先做桌面功能闭环，再基于设备情况适配头显定位、手柄移动/指向、Android构建。
3. 注意 Gaussian splat 在移动VR设备上的透明度排序、渲染兼容性和性能限制；应主动测试真机FPS和内存。没有设备或SDK时不得声称完成真机验证，输出清晰的接入清单和备用演示方案。

## 4. 工作方式与完成标准

- 使用**实际存在的工程**，先阅读再修改，不要另起全新项目替代用户已有工作，也不要为了演示而删除 GSplat。任何涉及插件更换、丢数据或原场景覆盖都要先征求批准。
- 优先交付能在 Windows Unity Editor 中打开、按 Play 可演示的**最小可行版本**（P0→P3）；PICO 硬件是独立验收，不可因暂时缺设备而阻断桌面闭环。
- 代码使用 C#，考虑 Unity 2022.3、URP；所有参数尽量 Inspector 可编辑。新增目录归类到 `Assets/Scripts/`, `Assets/Scenes/`, `Assets/Materials/`, `Assets/Prefabs/` 或项目现有规范。
- **每一阶段实际验证**编译、Console报错、Play运行和回归测试，报告通过/未通过/未执行，不能声称没运行的测试通过。若工具无法启动 Unity GUI 或截屏，明确请用户手动完成。
- 必需人工确认项最少化：具体莫奈原画图片与来源、可用人物模型与授权、PICO具体型号、对真实性的细节校准。
- 每次任务结束列出：改动文件、实现功能、测试结果、需要用户操作的最少几步、接下来计划。

## 5. 作品集截图 / Evidence（只保留5个重点阶段）

请创建 `Documentation/ProcessShots/` 和 `Documentation/PROCESS_LOG.md`。只记录关键节点，不要每项小操作都截图；截图必须来自真实程序界面，不能伪造成功画面。如果无法亲自操作Unity GUI截图，则在 `PROCESS_LOG.md` 给用户精确的手动截图指引。

1. **S01 — Spatial Reconstruction & Import**：已有 Unity GSplat 渲染画面 + Inspector中 `monet-garden` 资产 / splat count；工具 Marble Studio, Unity; 技术 Gaussian Splatting / PLY Import / Real-time rendering（这一步可复用用户此前截图，无需重做）。
2. **S02 — Player Navigation & Collision**：Scene 中显示碰撞代理 Gizmos、Hierarchy的 player 和 Character Controller；工具 Unity + C#；技术 First-person navigation / Character Controller / Proxy Collision.
3. **S03 — Dynamic Environment & Characters**：Game 中动态柳枝/水面/人物的真实运行截图，外加短视频比静态截图更能证明动画；工具 Unity Animator / Particle System / Shader（以实际使用为准）。
4. **S04 — Artist Viewpoint Detection**：Scene Gizmo 的 Viewpoint 位置与运行中的触发UI；技术 Spatial Trigger / Position & Orientation Detection.
5. **S05 — Painterly Transformation**：同一构图三帧并排：Real World → Transition → Original Painting；技术 URP post-processing / Shader / Image Blending（以实际实现为准）。

可选 S06：若PICO真机确实完成编译/测试，记录设备与运行结果。每张截图在 PROCESS_LOG 中记录日期、实际截图文件名、工具、技术、操作、功能结果、中文说明和英文Caption。示例Caption：`Viewpoint-triggered transition from a photorealistic reconstruction to the selected Impressionist painting.`

## 6. 用户提供文件清单（Codex 如找不到应明确询问）

- Unity 项目完整根文件夹（`Assets`, `Packages`, `ProjectSettings`）；当前本地磁盘路径由用户选取。
- `monet-garden.ply` / 导入生成的 GSplat 相关文件及 Unity meta 文件（目前似乎已在项目中）。
- AI生成莫奈花园图与**选定的原作图片**，后者不能用前者替代。
- 如使用Codex上下文辅助：可选 ChatGPT 共享链接，注意链接是快照，可能缺少上传内容，也不继承本地文件或个人记忆。

## 7. 立即执行的第一条操作

先**只读检查**当前Unity目录与场景，生成 `Documentation/PROJECT_STATUS.md`（核对现状、发现的阻塞和方案）和 `Documentation/PROCESS_LOG.md`，给出三阶段最短交付计划。随后在已备份的场景副本中实现P1→P3，并在每个关键里程碑测试、记录截图需求。不要只给理论方案，应实际编辑工程文件并提交可复现的成果。允许任务持续多轮，在当前工作空间持续更新文档与代码。
