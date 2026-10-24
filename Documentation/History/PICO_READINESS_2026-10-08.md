# PICO readiness / 后续真机适配

调研日期：2026-10-08。当前交付是 Unity 2022.3.62f3c1 / URP 14.0.12 / Windows DX12 桌面原型。尚无 PICO 型号、系统版本、可连接设备和已安装 XR SDK，未构建 APK，未验证头显帧率。

## 官方参考与选择依据

- [PICO Unity OpenXR SDK 官方仓库](https://github.com/Pico-Developer/PICO-Unity-OpenXR-SDK)：仓库 README 明确列出它所记录版本的设备系统要求。该 README 的 1.4.0（2025-04-07）要求系统 5.13.0+；这不是对未知设备或当前最新版兼容性的承诺。
- [PICO Project Validation](https://developer.picoxr.com/document/unity/project-validation/)：官方提供安装后检查 Unity/SDK、图形 API 与相关配置的流程。查询摘要可访问，页面本次抓取超时；具体规则需以安装版本的验证窗口为准。
- [PICO 官方项目模板](https://github.com/Pico-Developer/PICO-Unity-Project-Templates)：区分 PICO XR 与 Unity OpenXR 集成方式。不能直接套入模板覆盖已有 Gaussian 工程。

## 项目实际阻塞

1. 保留现有插件。其 shader 使用 compute/DXC，当前未确认 Android GPU/API 与双目渲染兼容；Windows DX12 运行不构成 Android 验证。
2. 1,824,584 splats 的排序、透明混合和带宽，需要在目标头显测量；目前未声称达到任何 FPS。
3. 桌面 OnGUI、旧 Input Manager 鼠标控制和 0.82 模型单位眼高不能直接作为 XR 体验。必须改为 XR Origin、真实头部跟踪、世界空间 UI 和手柄输入。
4. 新 URP 全屏 pass 是桌面版本；双目纹理采样、每眼构图、原作呈现应单独验证。转化中不能冻结真实头部跟踪。
5. 本次没有更改 Packages、原 URP Renderer、GraphicsSettings、QualitySettings、PlayerSettings 或原 Build Settings；原型管线只在 Play 中启用并在结束时恢复。

## 收到设备信息后的实际接入顺序

1. 记录机型、PICO OS、USB/串流模式；在独立场景副本中按该版本官方兼容表选 SDK。
2. 确认已安装 Unity Android Build Support/SDK/NDK/JDK；启用一个 XR provider，运行官方 Project Validation。
3. 先在极简双目场景验证头部跟踪、手柄移动与世界空间 UI，再接入同一 GSplat（从更低点数开始测量）。
4. 分别验证目标图形 API、左右眼透明度排序、近裁剪、SSAO/MSAA、full-screen shader，记录截图与设备性能。
5. 调整现实尺度和地面高度，改用舒适移动/瞬移，保留用户自主转头；把核心闭环的视点检测复用于 XR camera，而非改写主题逻辑。
6. 实测 CPU/GPU frame time、内存、温度/持续运行，再决定压缩/降采样、网格替代或 PCVR 串流演示路线。

本文件是接入清单，不是完成 PICO 适配的证明。
