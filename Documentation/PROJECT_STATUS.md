> 最新状态：2026-10-09 已实施最终设计。人物已移除、用户原作已绑定、3秒自动触发与原作独立10秒循环已实现；详见 FINAL_DESIGN_NOTES.md 与最新 final_play_validation.txt。下文保留此前历史记录。

# Monet Garden VR — 实际工程状态

日期：2026-10-08（Asia/Shanghai）。工程：`D:\新建文件夹\111`。

## 已确认的起点

- 已读取根目录 AGENTS.md 和 CODEX_HANDOFF.md。共享链接读取失败；未用其替代工程事实。
- Unity 2022.3.62f3c1；URP 14.0.12；旧 Input Manager；编辑器以 DX12 运行。
- 保存到磁盘的 SampleScene 是 2024 年默认模板，没有花园。真实花园在带星号的未保存 Unity 场景内。
- 已通过 Unity 的“另存为”保存为 `Assets/Scenes/MonetGarden_Prototype.unity`。没有覆盖 SampleScene。
- 已有 GSPlat 引用 monet-garden.ply；PLY 顶点数 1,824,584。GSPlat 变换仍为 position(0,0,0)、rotation(0,0,180)、scale(1,-1,1)。
- 已有 player/Main Camera 和 GardenWalkController。原脚本保留不改，新控制器只在副本上启用。
- 原 Main Camera 世界位置约 (0.600022, 0.813882, 0.200028)，已保存到副本及修改前快照。
- 已有 WalkableGround_Test 是 1×1×1 的可见方块，挡住 Game view 右侧。副本中停用，不删除原对象。
- 存在遗留样例的 Missing Script；仅在原型副本移除缺失组件，原始场景/快照保留。
- 未找到原作纹理、正式人物模型、自然音频或 XR/PICO 包。

## 备份

源文件完整备份（Assets，包含全部 PLY；Packages；ProjectSettings；UserSettings；交接文件）：
`C:\Users\25221\Documents\Codex\2026-10-08\https-chatgpt-com-share-6ac7af07-58c0\work\ProjectBackup_20261008`。
约 294 MB；Library/Logs/Temp 缓存不纳入完整源备份。
另存了 Temp 自动恢复快照，以及真正花园另存后、修改前的场景。
项目内 `Documentation/Backups/RecoveredGarden_BeforeChanges.unity` 是修改前花园快照，不作为可打开资产导入。

## 三阶段交付

1. 副本中的碰撞步行：保持相机视线，规范胶囊、重力、防坠落与狭窄道路代理，验证水域边界。
2. 局部动态环境：少量柳叶、水纹、睡莲、飞鸟，及可替换的程序化人物 Animator / 路径。
3. 核心闭环：地图入口→探索→距离、朝向与持续注视→E 确认→URP 画风转化→重置/地图。

## 明确的验收限制

- 点云没有可靠的碰撞表面。代理道路依据点云采样与初始视点布置，仍需 Scene/Game 视觉校准，不能声称桥和全部岸线已精确贴合。
- 模型的米制尺度没有参考物校准。为保留初始构图，本原型眼高使用 0.82 模型单位，胶囊高 0.9；不等于已验证的人体真实尺度。
- 水纹是小范围独立叠加对象，ZTest Always 配合高于 splat 的渲染队列；这是明确的演示折中，可能穿过前景遮挡，需要继续校准。
- 人物是明确命名的程序化占位，非用户授权人物资产。Idle/Walk/Pause 为实际生成的 AnimationClip/AnimatorController。
- 原作未提供时，最终画面为 painterly study，不能标注“莫奈原作”。纹理、题名、年份、来源齐全后才启用原作淡入。
- 没有音频资产，未宣称自然声已经实现。
- 桌面编译、Play、shader 与截图验证结果见 `Validation/`；未执行项不算通过。
- PICO SDK/设备型号/真机均未确认；没有 Android/XR 构建与帧率结论。

## 实施与验收更新

- 新增 GardenDesktopController、GardenViewpoint、GardenExperience、GardenArtwork、GardenGlobe、GardenAmbientMotion、GardenActorPath、GardenProxyGizmo 与 GardenPainterlyFeature。
- 新增三个 shader：PainterlyTransition（Kuwahara-style 边缘保留处理）、LocalWaterRipples、LocalFoliageSway。
- Unity 实际生成副本 URP pipeline/renderer（保留原 GSplat feature）、材质、叶片/鸟翼网格、Gardener AnimatorController 与 Idle/Walk/Pause clips、Giverny catalogue asset，并将全部引用保存到 Prototype。
- 初轮 Play 发现渲染结果未进入 final frame、人物起点重复停顿。将 pass 移至 BeforeRenderingPostProcessing，避开 URP 14 后处理换缓冲；修复路径起点。修复后多轮真实 Play 验证为 0 failures。
- 最终 Play 结果包含重力、三个阻挡边界、真实探索距离、错误方向/距离拒绝、注视中断、持续注视、E 确认入口、转化过程、原图临时诊断采样、重置、人物路径及地图返回。
- 临时纯色诊断图未保存为 artwork。原图接口最终帧采样约 (0.996,0.016,0.996)，撤回后恢复缺图状态。
- 三个 shader 在 RTX 4070 Laptop GPU / DX12 上支持；未观察到 C# 编译错误或 shader error。Unity 在 domain reload / Play 切换时记录了 `Caller must complete domain reload` Assert，保留在 editor_messages.log；不将 Console 声称为绝对零消息。
- 五阶段真实截图已保存到 ProcessShots；S05 末帧为画风研究而非原作。已记录中英文说明。
- Windows 独立构建结果另见 Validation/windows_build.txt；在结果生成前不得视为成功。键鼠完整人工回归、全路线贴合与 PICO 真机仍未验收。
