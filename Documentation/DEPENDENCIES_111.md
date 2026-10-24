# 依赖恢复与构建

Unity 2022.3.62f3c1；Packages/manifest.json 与 packages-lock.json 保留111配置，URP14.0.12。PLY通过Git LFS取得。

公开源码排除了 `Assets/gsplat-unity-vfx` 及其meta。原始111使用3DGS-VFX2.0.0（Dreamcore XR Labs，package ID dreamcore.xr.3dgsvfx），许可证允许个人/教育项目的编译版本分发，对插件源文件再分发作限制。须从自己有许可的原始包恢复该目录及原meta/GUID；普通同名文件夹不能替代正确的脚本、材质和Shader引用。本仓库不为其他资源追加开源许可。

111实际使用的插件 Shader 已有本项目集成修改。恢复一份未经集成的插件，不足以复现7秒转化。需在有许可的本地插件 `Runtime/Shaders/Gsplat.shader` 集成以下本项目文件/调用（这里只列本项目接口，不附插件源码）：

- Shader包含 `../../../GaussianEffects/Shaders/GaussianEffects.hlsl`（迁移的效果接口，旧版本已有）。
- Shader包含 `../../../GaussianEffects/Shaders/MonetParticleSequence.hlsl`（111重组序列）。
- 在完成原局部效果、局部Reveal及Relighting之后、计算修改后的Splat中心之前调用 `ApplyMonetParticleSequence(localCenter, localScales, localColor, originalCenter, originalScales, originalColor);`。
- 修改后中心初始化成功后，协方差计算之前保留近裁剪淡出：`if(_MonetSplatActive>.5)localColor.a*=smoothstep(.25,.65,-modifiedCenter.view.z);`。
- 原有迁移效果、效果体积和局部Reveal接口也必须保留；不能只粘贴最后两行并删除原有逻辑。与项目原始已集成包逐项核对。

缺少该依赖时源码可能编译失败或高斯点/画作转化缺失。本次验证使用原始111的完整本地依赖，未在公开源码的干净克隆上重建。教师直接体验应下载完整Windows Release。

入口：Assets/Scenes/MonetGarden_Prototype.unity。发布副本已将它选入Build Settings。菜单 Tools > Monet Garden > Build Windows Demo 明确只构建此场景，输出 Builds/MonetGarden111/MonetGardenVR.exe；不应直接构建SampleScene。工程Library、Temp、UserSettings及旧绝对构建路径不是发布依赖。
