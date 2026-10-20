# Final design revision / 最终设计修改

日期：2026-10-09，Asia/Shanghai。基于原工程与 MonetGarden_Prototype。

## 素材与保留

参考地球：Assets/MonetGarden/References/Reference02_ImpressionistEarth.png。
终点原图：Assets/MonetGarden/References/Reference03_MonetOriginal.png（原文件 2400×2293；未编辑；SHA256 6d3711cb09a4ff71814ac6b046219e5b52a4bea001195b7a282c31bf2016be47）。
原图题名、年代与来源机构未确认，不填虚构年份。此用户附件是唯一视觉终点。

修改前完整源备份：当前 Codex 工作目录 work/FinalRevisionBackup_20261009，包含 Assets / Packages / ProjectSettings / Documentation。此前备份也保留。
保留 monet-garden PLY、原 GSplat 变换、插件版本、Unity 与 Packages，原 SampleScene / GardenWalkController。

## 最终实施

- GardenGlobe：地理 UV 的真实球体网格、可旋转 Transform、鼠标拖拽与惯性、随球体运动的红色标记；仅吉维尼保留可选择碰撞体，背面标记不能穿球点击。第二次点击纸色信息卡片进入花园。
- 地球纹理使用程序化海陆轮廓、蓝/金/绿颜料色块与多层笔触，依据用户参考的视觉风格；不是参考图固定背景，也不是卫星地球。地理轮廓为示意，不作为 GIS 数据。
- 独立离屏 URP renderer（无 GSplat 排序 feature）分别渲染球体与 pigment ParticleSystem，主花园 renderer 保留 GSplat feature。没有替换插件。
- 花园移除所有 Gardener 人物及路径对象；旧人物资源作为历史文件保留，但不绑定最终场景/构建。
- 风、水、鸟声音为程序化合成的本地 WAV，不是吉维尼现场录音。局部植物使用不规则风变化；水纹贴片缩小幅度、取消 ZTest Always；它们不能使静态 splat 中每株植物运动。
- 保留已测试的出生区，校正岸边代理并接入点云可辨识的碎石弯道；池塘与低质量远景不开放。该代理仍需要人工逐段视觉复核，不声称整个花园精确碰撞。
- Approximate Artist Viewpoint 使用位置/高度/yaw/pitch/3 秒条件，探索后自动触发，无需 E。靠近时渐变构图镜头，保留玩家自主转向。光点引导克制，不用箭头。
- GardenPainterlyFeature 捕获最终相机 GPU 帧；屏幕空间 noise dissolve 分解冻结现实；GardenReassembly 的真实 ParticleSystem 使用原图色彩与目标图像坐标收集颜料。粗色关系→桥周边→细节与原图局部 reveal。没有重建原作高斯场景，也不是逐个 splat 的几何重组。
- 最终原图由 OnGUI 等比例完整绘制，绕开花园后处理，隐藏全部产品提示。玩家根节点与相机在转化/原图/返回中保持锁定，重力也暂停。
- 原作首次完全呈现的帧结束后，用 realtime double clock 独立计时至少 10 秒；下一帧开始柔和返回。GPU/显示器按帧刷新，状态切换误差限于一帧；不会提前结束。R/M/G 只在 Exploration 生效，不能提前打断原作停留。

## 构图解释

原作呈近正方形画幅，桥横贯中部、占宽较大，柳枝与睡莲围合。生成花园原始镜头为宽视野、桥更远更小。用观察朝向与约 24°构图视野提高对应关系；生成桥的形状、植物位置与原作仍不同。这是推测性构图观察点，不是历史作画坐标或精准摄影测量配准。

## 验收证据

最新实际 Play 记录见 Validation/final_play_validation.txt；包括两次完整循环、真实 ParticleSystem、原图帧采样、位置和朝向共同检测、碰撞与独立 10 秒计时。
Windows 构建见 Validation/windows_build.txt；未生成结果前不算通过。编辑器、键鼠独立程序测试与 PICO 真机结果必须分别报告。
既有 Unity domain reload Assert 日志保留，不将 Console 声称为绝对无消息。


## 最终操作回归补记 / Interaction regression

已实际使用 Windows Computer Use 操作 Unity：鼠标拖拽产生 214.1 px 的事件位移、地球真实旋转；点击吉维尼进入 focus/card；再次点击卡片进入花园。逐帧 Input 曾漏接快速拖拽，改为 Unity MouseDown/MouseDrag/MouseUp 事件；惯性限制为100°/s并指数衰减。选中视图的红点与信息卡分开放置，避免被卡片遮挡。
首轮单像素颜色检验失败属于纹理降采样对油画细节的采样差异；保留失败日志。独立图像比对在24×24区域均值上的误差约0.58/255，最终改用9个图像区域检验，实际均值通道误差约0.0018。没有用纯色图案替换原作。
环境声已生成并接入AudioSource，音量可在 ambientVolume 中调节；合成音频的现场听感仍需要用户用自己的扬声器/耳机确认。

最终画面复核已移除旧版纯色圆盘睡莲占位模型；保留原高斯中的精细睡莲与局部水纹，避免明显的风格冲突。
