# PICO 适配与真机验证状态

作者已确认MonetGarden_PICO完成PICO适配和真机验证。本次重新核对项目、APK与已有设备事件证据，旧的“未接入/未构建APK”清单已经过时。

两个版本分别保留：仓库根目录为111 Windows工程；PICO/为独立MonetGarden_PICO工程。Windows EXE使用键鼠，PICO APK使用头部追踪、控制器与世界空间UI。它们不是同一个可执行文件。

- PICO成品：MonetGarden_PICO4_FieldFix_20261011.apk，0.1.2-fieldfix / versionCode3 / com.monetgarden.pico4 / ARM64。
- SHA256：542abb095870275e21d9b977229476e7b4a4015a7ac95f42e17037bb7c8ab790。与当前默认MonetGarden_PICO4.apk逐字节相同。
- 已有真机日志明确覆盖双眼渲染、手柄追踪、入园、引导确认、观察触发、7秒转化、原作10.0449秒与返回地球一次循环。它与该FieldFix版本和构建记录对应。
- 本次由作者确认适配和真机验证已完成；没有声称本次Windows截图是头显实拍，也没有重新编造头显FPS或覆盖所有设备型号。
- 历史APK构建为0错误、51警告；本次Windows构建的0错误0警告仅属于Windows。

下载、操作、源码恢复与证据见 [PICO版说明](../PICO/README.md)。旧的适配前清单保存在 [历史文件](History/PICO_READINESS_2026-10-08.md)，不代表当前完成状态。
