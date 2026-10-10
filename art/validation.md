# v0.1.2 验收记录

- 海报与用户提供原图字节完全相同，347×576。
- 战斗图片直接复制内置 image_gen 的输出，保留原始 RGBA 和透明通道，没有二次抠图或图像重采样。
- 1024×1536 图片中完全透明像素为 1,145,027；主体 alpha 大于等于 128 的范围为 (271, 25)–(779, 1517)。
- PNG 在美术预览中按主体约 304 px 高度显示，人物完整、轮廓清楚，没有海报背景或生成画面中的暗色光晕。
- C# 编译：0 警告、0 错误。
- 独立 MegaDot 工程加载最终 PCK：全部动画及背景布局检查通过，日志标记 `OTTO_PACK_ASSETS_OK`。
- 游戏 v0.107.1 + BaseLib 3.4.7：真实角色创建入口成功返回 NCreatureVisuals；Bounds、CenterPos 解析成功；idle、attack、cast、hurt、die、revive 通过 BaseLib 正常播放。
- 游戏日志标记：`OTTO_RUNTIME_VISUALS_OK: model path, NCreatureVisuals conversion, bounds, BaseLib animation dispatch`。
- 安装文件 DLL、PCK、JSON 与分发目录逐字节一致，压缩包只包含这三个正式文件。
- 临时验证插件已移除，没有进入压缩包。
- 尚未完成：真实选人界面截图、完整战斗出牌流程。当前视觉验收来自美术尺寸预览，动作验收来自引擎及游戏无界面检查。
