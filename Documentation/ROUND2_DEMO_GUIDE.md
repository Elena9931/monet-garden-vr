# Second-round demo guide / 操作说明
Open Assets/Scenes/MonetGarden_Prototype.unity in Unity 2022.3.62f3c1 and press Play. Do not run the old Prototype/Final upgrade actions again: the saved scene already contains Round 2.

1. Drag the globe with the left mouse button; release to decelerate. Only the illuminated Giverny pin is selectable. Click it, wait for the focus animation, then click its card to enter.
2. Read the upper-right guide. Scroll Page 01 as needed. Continue →, Continue →, Confirm. The original English text is preserved.
3. Confirm collapses the panel, captures the mouse and reveals the tiny ground marker. WASD moves, Shift accelerates. Tab toggles between looking and UI interaction; Esc releases the cursor. Click Guide to reopen. − collapses; × closes; both keep a reopen icon.
4. Explore first, then approach the gold marker beside the existing bridge approach. This is an Approximate Artist Viewpoint, not a historically verified coordinate. Look toward the Japanese bridge; position, yaw, pitch and a continuous three-second hold are required. If interrupted, the hold restarts.
5. Navigation and look lock automatically. The seven-second point sequence ends in the supplied original, fitted without cropping. Its separate ten-second clock starts only after the complete image has rendered.
6. The experience returns to the globe automatically; the guide, cue, source splat parameters and player position reset. Select Giverny again for another cycle.

中文：拖拽地球 → 点击吉维尼 → 再点卡片进入 → 阅读三页并 Confirm → WASD 探索、朝金色小标记靠近 → 对准日本桥并保持三秒 → 七秒转换 → 完整原作独立十秒 → 自动返回。Tab / Esc 用于释放鼠标点击导览。

## Inspector controls
MonetGardenExperience: transitionSeconds=7; GardenGuide widthFraction=.24 (range .20–.25); GardenGaussianTransition pointSize=.004, spread=1.35, assemblyDensity=.12; GardenReassembly particleCount=18000 (GPU billboards).
Approximate Artist Viewpoint: radius=.36, vertical tolerance=.20, yaw=16°, pitch=8°, hold=3s.
QuietViewpointLights: diameter=.11, clearance=.13, float amplitude=.035, period=2s.
The imported scene is calibrated at eyeHeight=.82; these are scene units, not unverified metre measurements. Keep existing proxy collision geometry and splat transform.
Do not force-enable Uniform Sphere Points in the saved scene: the sequence controller enables it temporarily and restores the original settings.
