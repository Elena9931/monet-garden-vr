# Monet Garden · PICO 独立工程

该目录来自作者指定的 MonetGarden_PICO，区别于仓库根目录的111 Windows工程。作者于本次整理中确认已经完成PICO适配和真机验证。当前APK为0.1.2-fieldfix，versionCode3，包名com.monetgarden.pico4，ARM64/Vulkan。APK与保存的10月11日构建和真机循环证据对应。

直接体验请从仓库Releases下载 `MonetGarden_PICO4_FieldFix_20261011.apk`。本次未重新编译APK，分发的是经哈希核对的原始成品；没有把它称作10月25日新构建。

## 头显操作与流程

1. 在PICO上安装APK，启动MonetGarden PICO。地球入口使用左摇杆旋转；射线指向吉维尼红点，扳机选择，再按独立Enter按钮进入。
2. A/X可重新居中入口。进入花园后，射线扳机操作三页世界空间引导，第三页Confirm后出现观察任务。
3. 左摇杆沿头部朝向移动，右摇杆水平每次转向30°；头部追踪持续保留。
4. A/X可开关花园引导，关闭引导后移动至观察点、面对日本桥，保持约3秒自动触发。展开引导会暂停观察检测。
5. 约7秒转化，完整原作展示约10秒，再自动回到入口。序列期间禁用摇杆移动/转向，保留真实头部追踪。

PICO版使用262144点的移动端SH0高斯副本；Windows111使用完整桌面数据，视觉密度和性能不能相互代替。真实头部转动可看向世界空间画作之外，不能套用Windows锁定镜头的说明。

## 真机证据与边界

保存的FieldFix设备日志记录：PICO Display running=True、双眼passes=2、PICO Input有效、262144移动高斯、进入/引导确认/观察触发/转化/原作/返回一次完整循环。原作停留10.0449秒。本次重新核对了APK哈希、源码配置和该设备事件日志；未把编辑器模拟截图当作头显实拍。作者另行确认适配和真机验证已完成。

本次没有重新测量头显FPS、CPU/GPU帧时间或持续运行舒适度，不据此填写未经记录的性能数字。历史材料提及PICO4/4Ultra；本次未重新读取具体设备型号和系统版本。

见 [设备事件摘要](Documentation/DeviceEvents_FieldFix.txt)、[文件身份](Documentation/ArtifactIdentity.json) 以及根目录 [PICO状态说明](../Documentation/PICO_READINESS.md)。主README的16张新截图来自Windows111的Unity Play；PICO工程历史Game图不作为本次新截图。

## 打开与构建源码

Unity2022.3.62f3c1；入口Assets/Scenes/MonetGarden_Prototype.unity。将PICO目录单独放到不含中文的路径，例如C:/UnityProjects/MonetGarden_PICO。

公开源文件排除受许可限制的3DGS-VFX插件源码，以及PICO Integration3.4.0的内置SDK源码目录。需从有许可的本地备份恢复Assets/gsplat-unity-vfx及原meta/GUID、Packages/com.unity.xr.picoxr，并恢复SDK生成的Resources调试prefab；Unity包管理器恢复XRI2.6.4、InputSystem1.14.0、XRManagement4.4.0及CoreUtils2.5.2。SDK恢复后保留原XR加载器与PXR设置引用。

本次发布副本的PICO构建脚本改为使用Unity External Tools的JDK/SDK/NDK设置，避免强制使用原作者本机绝对路径。菜单Tools > Monet Garden > PICO > Build APK输出Builds/PICO4/MonetGarden_PICO4.apk。原PICO工程未修改；本次未在排除外部依赖的公开副本上重编译。

历史构建报告为0错误、51警告，不能沿用Windows新构建的0警告结论。具体警告包括Shader编译提示、SDK元数据/插件配置；本次未用重新构建去掩盖这些记录。
