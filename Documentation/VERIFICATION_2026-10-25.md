# 2026-10-25 本次运行验证

## 运行范围

在Windows原机通过Computer Use操作Unity2022.3.62f3c1，打开D盘的111；真实按Play，通过鼠标选点/卡片、引导按钮、WASD和Shift完成两个编辑器主循环。未调用会移动玩家的历史EvidenceProbe来代替玩家路线。临时Editor观察器只调用ScreenCapture并记录状态；没有设定玩家坐标、触发条件或游戏速度。

首次编辑器循环原作停留10.0017秒，第二次10.0099秒，均返回地球。第二次正式截图记录从地球到返回地球的完整状态序列，详见Screenshots111/provenance.json。截图记录点后由Unity完成帧末捕获，因此CSV状态数值与最终像素可能有一帧时差。

从同一111用BuildPipeline显式指定MonetGarden_Prototype生成StandaloneWindows64：2026-10-24T16:31:22.6616480Z（北京时间10月25日00:31），Succeeded，0错误、0警告，232352927字节。随后实际启动本次EXE，按正常路线经过三页引导、探索和观察触发，回到地球。Player日志记录MONET_ORIGINAL_FULL_FRAME、MONET_HOLD_COMPLETE:10.0018s;loop=1。

测试设备：RTX4070Laptop，Direct3D12；没有独立性能基准或其他硬件覆盖。

## 检查结果

| 检查 | 本次结果 |
|---|---|
| 有效Unity结构/版本/入口 | Assets、Packages、ProjectSettings存在，原型场景正常打开 |
| Console编译/Play | 第二轮结束0错误0警告；新Windows构建0错误0警告 |
| 地球拖动/惯性/吉维尼卡片/进入 | 编辑器实际操作通过；EXE实际选点进入通过 |
| 三页引导/第一页滚动/Confirm | 编辑器实测；EXE翻页与Confirm通过 |
| WASD/Shift/Tab/Esc | 编辑器实测，UI模式移动停止；EXE实际横移探索通过 |
| R重置 | 编辑器实际测试回出生区域并重开第一页引导 |
| 观察位置/方向/3秒停留 | 两次编辑器与一次EXE路线自动触发，未强制跳阶段 |
| 转化/完整原作/自动返回 | 编辑器完整帧序列，EXE日志确认原作10.0018秒和完成循环 |
| 模型/材质/交互阻塞 | 本次路线未见缺失模型、粉色材质或卡住；不等于全部地形可达性测试 |
| M/G/Guide关闭重开/其他目的地 | 补测EXE：关闭与收起Guide后可经Esc及Guide重开；G关闭/恢复附加动态层；M返回地球；另点一个非吉维尼红点未进入新场景。未穷举全部占位点 |
| 声音 | 合成鸟/水/风资源与播放配置存在；工具未提供听觉核验，未声称已听音质 |
| VR/PICO | 作者确认独立MonetGarden_PICO已完成适配与真机验证；本次核对FieldFix APK与保存日志，未把它计作本次新头显运行 |

## 证据与限制

- 新正式截图仅使用本次16张，均1920×1080。原文件名、UTC时间、帧号、状态与SHA256在provenance.json中。
- 本次完整CSV、构建报告与Standalone日志摘要保存在Verification111目录。
- 没有录制全程视频；图像序列加状态日志作为本次核对证据。
- 旧Validation内各轮自动测试的通过数是开发历史，不是本次重新执行。
- 本次未修改原始111的玩法/场景/视觉。发布副本仅做构建路径可移植化、真实入口Build Settings与文档/证据整理；插件源码不公开。
- 原作具体标题/年份未独立确证，观察点为近似构图。近距离高斯重建可见糊化和附加叶片几何，本次照实保留画面。
- 补测时未确认任务即关闭Guide，也没有跳过Confirm进入转化；关闭/收起引导不会自动完成观察任务。
- 原始111复核：临时Editor观察器及meta已移除，三个URP资产和两个Burst设置的构建自动序列化改动已恢复。446个备份文件中，仅本次运行追加的editor_messages.log有变化，其他文件SHA256与备份一致。

PICO补充：已核对APK SHA256与当前默认包相同；保存的设备日志覆盖一次完整循环。详见PICO_READINESS.md和PICO/Documentation，Windows测试结果及截图范围不变。
