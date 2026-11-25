# OTTO · 杀戮尖塔 2 角色 Mod

[Steam Workshop 创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3816929315) · [下载发行包](https://github.com/DarkboyRyan/SLS2-OTTO-mod/releases) · [反馈问题](https://github.com/DarkboyRyan/SLS2-OTTO-mod/issues) · [从源码构建](#从源码构建)

为《Slay the Spire 2》添加可选角色 **OTTO**、87 张专属设计卡，以及古神变身、动物园伙伴、监狱和易拉罐连招等机制。包含自定义卡面、角色动画、语音和中英文文本。

作者：**sakana雫**。欢迎在工坊评论或 GitHub Issues 提交反馈。

## 版本与兼容性

| 版本 | 内容 | 构建产物 |
| --- | --- | --- |
| `v1.0.4-universal` | 正式版 / beta 通用包 | `dist-universal/` |
| `v1.0.5-wheelchair-test.1` | 轮椅动画测试包，包含坐姿待机、下砸攻击、推轮防御与睡姿死亡动画 | `dist-test/` |

上述目录由构建生成，不提交到源码仓库。玩家请从 Releases 下载对应 ZIP；GitHub 的 **Code → Download ZIP** 下载的是源码，不是可安装的 Mod。

已验证的游戏版本为正式版 `v0.107.1` 与 public-beta `v0.113.0`，通用包需要 **BaseLib 3.4.8 及其 Compatibility Helper**。加载器按游戏接口选择实现，切换分支后需重启游戏。这些是已有验证记录，不代表对后续游戏版本的兼容保证。测试环境为 macOS，Windows 尚未实机验证。

## 安装

### Steam Workshop

1. 订阅并启用 [BaseLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3737335127)。
2. 前往 [OTTO 创意工坊页面](https://steamcommunity.com/sharedfiles/filedetails/?id=3816929315) 订阅本 Mod。
3. 等待 Steam 下载完成，在游戏中启用所需 Mod 并重启。

工坊页面的实际发布状态以 Steam 为准；GitHub 中的新代码或测试包不一定已在工坊上线。

### 手动安装

1. 从 [Releases](https://github.com/DarkboyRyan/SLS2-OTTO-mod/releases) 下载需要的发行包，并准备匹配的 BaseLib。
2. 解压后，将整个 `OTTO` 文件夹放入游戏的 `mods` 目录。Windows / Linux 通常位于游戏安装目录下；macOS 位于 `SlayTheSpire2.app/Contents/MacOS/mods/`。
3. 保留所有文件及子目录，启用后重启游戏。同一个环境不要同时加载手动安装和工坊下载的两份 OTTO。

通用包的完整结构如下，**不要只复制根目录的三个文件**：

```text
OTTO/
├── OTTO.json
├── OTTO.dll
├── OTTO.pck
└── assemblies/
    ├── stable/OTTO.Content.dll
    └── beta/OTTO.Content.dll
```

回退时，退出游戏后用上一版完整发行包替换手动安装目录。保留存档备份；跨版本存档兼容性需另行验证。

## 从源码构建

### 准备环境

- .NET SDK：支持 `net9.0`；项目使用 `Godot.NET.Sdk/4.5.1`。
- MegaDot：项目已有验证环境为 `4.5.1.m.14`。不要直接假设其他 Godot 版本兼容。
- 合法安装的游戏及其编译引用；通用包需同时准备上表两版游戏 API。
- BaseLib 编译引用。原单分支构建通过 NuGet 引用 `3.4.7`；beta 实现读取本机 BaseLib `3.4.8` DLL。
- Bash 与 Python 3。当前构建脚本按 macOS 开发环境编写。

克隆仓库并配置编辑器路径：

```bash
git clone https://github.com/DarkboyRyan/SLS2-OTTO-mod.git
cd SLS2-OTTO-mod
cp Directory.Build.local.props.example Directory.Build.local.props
```

修改 `Directory.Build.local.props` 中的 `GodotPath` 为本机 MegaDot 可执行文件路径。该文件被 Git 忽略。也可以通过环境变量 `OTTO_GODOT_PATH` 配置，不创建本机配置文件。

### 通用版与轮椅测试版

按本机位置指定编译引用；下列路径是占位示例：

```bash
export OTTO_STABLE_DATA_DIR="/path/to/stable/game-data"
export OTTO_BETA_DATA_DIR="/path/to/beta/game-data"
export OTTO_BETA_BASELIB_PATH="/path/to/BaseLib.dll"

# 正式版 / beta 通用包
bash build-universal.sh

# 轮椅动画测试包
bash build-wheelchair-test.sh
```

游戏数据目录中需有 `sts2.dll`、`0Harmony.dll` 等游戏依赖。未设置这些变量时，脚本会尝试开发机默认位置；其中正式版默认引用位于仓库外的 `.work/engine`，新克隆仓库后需要自行准备并设置路径。

输出分别为：

- `dist-universal/OTTO-v1.0.4-universal.zip`
- `dist-test/OTTO-v1.0.5-wheelchair-test.1.zip`

给脚本添加 `--install` 会将产物安装到脚本检测到的本机游戏目录。仅构建不带该参数。`build.sh` 与 `build-beta.sh` 保留为旧单分支构建入口，并不生成通用包。

### 源码结构

```text
Code/                         卡牌、角色、遗物、能力与战斗机制
Loader/                       通用包加载器，选择 stable / beta 实现
OTTO/                         图片、音频、场景与中英文文本
OTTO*.json                    各构建类型的 Mod 清单
*.csproj / *.sln / *.props     C# 工程与路径配置
project.godot                 Godot 项目入口
export_presets.cfg            PCK 导出预设
tools/                        场景生成、素材处理与验证工具
art/                          素材记录、设计说明与历史验证记录
build*.sh                     构建、打包及可选安装脚本
```

游戏本体 DLL、BaseLib 二进制、Godot 缓存、编译产物、本机路径配置及发行 ZIP 不随源码提交。加载器源码包含在本仓库中，无需额外获取同级项目。

## 发布与反馈

发行 ZIP 放在 [GitHub Releases](https://github.com/DarkboyRyan/SLS2-OTTO-mod/releases)，源码通过 Git 管理。更新工坊时，向上传工具的 `content/` 复制完整发行目录（包括 `assemblies/`），保留原有 `mod_id.txt`，使用原条目 ID `3816929315`。

提交问题时请说明游戏版本、操作系统、BaseLib / OTTO 版本、复现步骤，并附上相关错误日志；分享前请移除个人敏感信息。

## 素材与致谢

本项目为个人非商业 Mod。部分素材来自网络截图或 AI 生成；如涉及侵权，请通过 Issues 联系作者处理。仓库公开不代表第三方图片、音频等素材已获得任意再分发或商业使用授权。

感谢 [Alchyr](https://github.com/Alchyr) 开发的 [BaseLib](https://github.com/Alchyr/BaseLib-StS2) 及角色 Mod 模板。

## 验证记录

- [正式版 / beta 通用兼容方案与验证](art/universal-compatibility-2026-11-13/README.md)
- [轮椅动画测试版与验证](art/wheelchair-test-2026-11-24/README.md)

历史记录中的版本、数量与路径以记录生成时为准。当前代码可能包含尚未在工坊发布的修改。
