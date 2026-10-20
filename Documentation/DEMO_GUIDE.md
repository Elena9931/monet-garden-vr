# Monet Garden — final desktop demo

1. Unity 打开 Assets/Scenes/MonetGarden_Prototype.unity，按 Play。无需创建新项目或重新导入 Gaussian 数据。
2. 拖拽完整地球，松开后惯性减速；点击法国发光红色标记。地球转向吉维尼并放大，纸色信息卡片出现。其他红点仅为未来扩展。
3. 再次点击信息卡片，约 2.4 秒过渡进入现有花园。WASD 行走、Shift 加速；点击 Game 捕获鼠标，Esc 释放。
4. 沿岸边探索，再回到出生区域附近的柔和光点。面对日本桥，缓慢调整观看角度；构图镜头会逐渐靠近。位置、朝向、俯仰同时正确并维持 3 秒，自动开始转化，无需 E。
5. 溶解和颜料重组约 9 秒。用户原图完整出现后单独停留 10 秒，期间无移动/转头/提示。自动柔和返回地球，可重复选择。
6. R 重置、M 返回地图、G 开关动态层只在探索阶段有效。转化与原作阶段不接受这些跳过操作。关闭独立程序使用 Alt+F4。

## Inspector adjustment

- MonetGardenExperience：transitionSeconds、entrySeconds、returnSeconds、compositionFieldOfView、explorationDistance、ambientVolume。
- Approximate Artist Viewpoint：radius、verticalTolerance、yawTolerance、pitchTolerance、holdSeconds。
- ImpressionistGlobeInterface：dragSensitivity、inertiaFriction、focusSeconds。
- PigmentReassemblyInterface：particleCount、dispersion。5600 个粒子为当前桌面默认；不代表移动 VR 性能结论。
- GardenProxyColliders_PROVISIONAL：出生区、岸边、碎石弯道代理与池塘/植物边界。
- Assets/MonetGarden/Artwork/Giverny.asset：用户原图与来源记录。原图文件在 References，不要替换成风格生成图。

## Test / Build

Tools / Monet Garden / Run Play Validation：在真实 Play world 运行最终回归并采集 Game PNG；约需一分半以上，包含两次 10 秒停留。
Tools / Monet Garden / Audit Saved Scene：缺失脚本与 5 个 Shader 审计。
Tools / Monet Garden / Build Windows Demo：只构建原型场景，不改变原 Scenes In Build 列表；当前输出位置记录在 Validation/windows_build.txt。
运行 Windows exe 时保留完整同目录 Data、MonoBleedingEdge 和 DLL，不可只复制 exe。

PICO 未适配/未真机测试，另见 PICO_READINESS.md。
