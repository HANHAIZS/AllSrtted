# New Project Setup

## AI 默认接入入口

从框架仓库运行一次下列命令。目标目录可以是空目录或尚未建立自有架构的 Unity 2022.3 工程；先关闭该工程的 Unity Editor：

```bash
python3 Scripts/onboard_project.py \
  --project /absolute/path/MyGame \
  --project-name MyGame \
  --namespace Studio.MyGame
```

默认完成：QFramework 程序集检查/首次安装、基础推荐 UPM 包安装（不含 Figma）、项目唯一 App/Controller、角色目录、UI Profile、AGENTS/Skills、Feature Spec 模板、CI、YooAsset 默认资源配置及工具可用性验收。最后自动重新启动一次 BatchMode，使新生成代码经过真实编译后再写入 `.gamefoundation/onboarding-report.json`。安装脚本只写包引用；AI 应使用上面的完整入口交付新项目。

框架不分发 Figma 下载与同步工具，也不提供它们的安装入口或就绪检查。开发者按需自行接入并验证；AI 初始化不得自动安装、替换或升级外部工具，已有安装引用原样保留。UI Profile、Prefab 命名/结构规则和规范化工作流仍自动提供，不依赖 Figma 下载插件。

初始化后的项目再次运行只需 `--project`。名称、命名空间、代码目录从 `.gamefoundation/project.json` 读取。不同项目身份或重复 Architecture 会在 Setup 写入前被拒绝；已有项目代码、Prefab 和采集规则不被覆盖。旧版本误生成多个 App 时，需要先明确保留哪个架构并迁移，再继续接入。

已有自有架构的工程在 `.gamefoundation/project.json` 中使用 `integrationMode: existing-architecture`，明确 `architectureFile`、`architectureType`、`controllerFile`、`controllerType`、`codeRoot` 和项目身份后，使用相同接入入口。Setup 验证唯一架构，保留已有 Architecture、Controller、asmdef 和配置原文，重编译后按配置类型检查，不生成或重写第二套入口；即使开启 overwrite 也不覆盖接入的代码。已有入口继续负责注册项目选择的 Foundation 模块。旧配置没有类型名时按命名空间与已记录源文件名推导；文件名与类型不同时必须明确填写类型。

示例（这些文件必须已经存在，类名与路径由项目填写）：

```json
{
  "schemaVersion": 1,
  "projectName": "Example",
  "rootNamespace": "Studio.Example",
  "codeRoot": "Assets/Scripts/Game",
  "integrationMode": "existing-architecture",
  "architectureFile": "Assets/Scripts/Game/Architecture/SessionArchitecture.cs",
  "architectureType": "Studio.Example.SessionArchitecture",
  "controllerFile": "Assets/Scripts/Game/Architecture/SceneController.cs",
  "controllerType": "Studio.Example.SceneController",
  "uiConventionProfile": "Assets/Settings/GameFoundation/ExampleUiPrefabConvention.asset",
  "contentPackageName": "DefaultPackage",
  "contentRoot": "Assets/GameContent"
}
```

直接调用 Editor Setup API 时也可设置对应 `IntegrationMode`、`ArchitectureFile/Type`、`ControllerFile/Type` 与 `UiConventionProfilePath`。已有工程的修复从配置读取这些值；BatchMode 首次接入可用 `GAME_FOUNDATION_INTEGRATION_MODE`、`GAME_FOUNDATION_ARCHITECTURE_FILE/TYPE`、`GAME_FOUNDATION_CONTROLLER_FILE/TYPE`。只安装包不代表完成接入。

默认资源结构：

```text
Assets/Settings/GameFoundation/AssetBundleCollectorSetting.asset
  Show Packages = true
  DefaultPackage
    Base
      Collector -> Assets/GameContent

Assets/GameContent                     # 仅预留给明确选择的热更内容
Assets/Resources/Prefabs/UI             # 默认动态加载的 UI / Prefab
Assets/Resources/Prefabs/UI/Popups      # 弹窗首次打开加载，随后复用
Assets/Resources/Sprites/UI             # 默认动态加载的 Sprite
Assets/Sprites/UI                       # 仅 Sprite 图片/资产
Assets/Prefabs/UI                       # 直接引用的 UI / Prefab
Assets/Prefabs/DesignImports            # 导入 Prefab 草稿
Assets/Art/Models                       # 模型与 Mesh
Assets/Art/Materials                    # 材质与 Shader
Assets/Art/Textures                     # 普通/法线贴图
Assets/Art/Fonts                        # 字体及许可证
Assets/Art/DesignImports                # 原始设计源文件
```

首次可通过 `--content-package` 和 `--content-root` 调整热更目录。已有 Collector 配置时保留其资源包、分组和规则，只打开 Package 列表并校验。YooAsset 默认按资源路径寻址，但不接管默认 UI 加载；不会把整个 Assets 或 Resources 加入采集。

默认资源加载方式为 `Resources.Load<T>` / `Resources.LoadAsync<T>`。制作 UI 或预制体前必须阅读 `Docs/Foundation/ResourceLoading.md`，按是否需要路径加载选择目录；不需要独立加载的图片可以被 Resources Prefab 直接引用。Setup 只创建目录，不搬迁旧资源。

生产页面不在接入阶段导入，外部设计工具由开发者单独验收。YooAsset 验收真实 Collector/Builder UXML 和采集配置。基础必需项缺失、已安装 Provider 校验失败、配置错误或编译失败会使完整接入命令失败；没有设计导入工具不属于失败。

完成报告区分本地就绪与外部待配置项：Figma Token/设计链接、CDN/Host 模式、SDK 密钥/隐私策略、实际热更资源与设备验证。运行时继续安全 NoOp，项目选择供应商后在自己的 Architecture 注入；不会假定一个示例 CDN 已可用于生产。没有资源的空目录不宣称已完成资源构建。

Unity 菜单 `Project Setup` 会读取并锁定已初始化的项目身份，按钮变为 `Repair / Complete Onboarding`；不再用默认 `NewGame` 覆盖项目配置。已经安装的可选工具安装项禁用。补装工具完成后，已初始化项目会自动补齐本地默认配置。

## 1. 安装

将需要的 `com.nn555k.gamefoundation.*` 目录复制到新项目 `Packages`，或发布到私有 UPM Registry 后在 `Packages/manifest.json` 中引用。项目必须已有 QFramework，并提供名为 `QFramework` 的程序集引用。

最小组合是 `Core`。存档、热更、SDK、UI 都是可选包。YooAsset Backend 是依赖 HotUpdate 的可选工具包。

默认安装脚本包含 YooAsset，不包含设计导入工具；需要最小安装时显式传入 `--include core,save,ui`。已有 Setup 的项目可以通过 `Game Foundation/Install Optional Tools/Recommended All` 补装 YooAsset。`--include figma` 已不再支持；外部设计工具应通过开发者选择的独立来源接入。

若希望用界面初始化完整项目，安装 `com.nn555k.gamefoundation.setup`，然后打开 `Game Foundation/Project Setup`。除新工程的 App、Controller、角色目录、asmdef 与 UI Profile 外，工具默认还会安装治理配置、`AGENTS.md` 托管区块、AI Skills、Feature Spec 模板、项目校验脚本和 GitHub CI。既有 UI Profile 保留 GUID 与 authored 配置；修复不会重建它。托管区块通过安装摘要判断是否可自动更新；存在项目编辑或未知旧版本时保留原文并输出 `.gamefoundation/AGENTS.managed.proposed.md`，人工合并后再修复。项目自有规则放在托管标记之外，详情见 `FeatureGovernance.md`。

建议保留 Setup 包，以便使用 `Game Foundation/Feature Workflow/New Feature Specification` 和 `Game Foundation/Validate Project Conventions`。详细流程见 `Docs/Foundation/FeatureGovernance.md`。

## 2. 创建项目架构

项目继续拥有自己的 App 与 Controller，不复制其他游戏的业务代码：

```csharp
using GameFoundation.Core;
using GameFoundation.Save;
using GameFoundation.UI;
using QFramework;

public sealed class ExampleApp : Architecture<ExampleApp>
{
    protected override void Init()
    {
        FoundationCoreModule.Register(this);
        FoundationSaveModule.Register(this);
        FoundationUiModule.Register(this);

        // 在这里继续注册本项目的 Utility、Model 和 System。
    }
}
```

需要热更或 SDK 时，再注册 `FoundationHotUpdateModule` 与 `FoundationSdkModule`。YooAsset 项目可直接创建 `YooAssetContentUpdateBackend`；CDN 地址、包名、标签和解密策略仍由项目配置。SDK 默认实现不会联网，生产环境继续注入自己的 `ISdkAdapter`。

SDK 初始化、身份同步及数数 / Adjust 回传接入见 [SDK 初始化规范](SdkInitialization.md)。Setup 会生成 `Docs/Integrations/SdkInitializationChecklist.md`，由项目按实际厂商填写；本地接入就绪不代表厂商 SDK、真机或服务端回传已验收。既有文档和 Skills 默认保留，升级时需对照新模板合并。

## 3. 建立项目目录

建议直接在项目代码根目录集中维护 QFramework 角色目录：

```text
Assets/Scripts/Game/
  Architecture/
  Commands/
  Models/
  Systems/
  Events/
  ViewControllers/
  Utilities/
```

具体功能按职责放到这些目录的子目录，不要再创建一套模块内 `Models/Systems/Commands`。

## 4. 配置 UI 规范

生产 UI 使用 `UiLayers.md` 的七层契约，在 `UiLayers.Project.md` 中记录项目宿主、接口、输入、相机/缩放和生命周期；按钮等 Inspector 配置遵循 `UiAuthoring.md`。Setup 仅分发文档与模板，不建立生产页面，也不声明默认 Router 已支持 Stack。

Setup Wizard 会创建一个 `UiPrefabConventionProfile` 资产，用于定义项目的 Popup、Page、Hud、Overlay、Item 和 Component 规则，也可以通过 `Create > Game Foundation > UI Prefab Convention` 手动创建。

Figma 导入后先在 Project 窗口选中 Prefab，再使用：

- `Game Foundation/UI/Preview Normalize Selected Prefab`
- `Game Foundation/UI/Normalize Selected Prefab`
- `Game Foundation/UI/Validate Selected Prefab`

设计页面的下载与同步通过项目自行接入的工具完成。令牌只保存在本机，不写入 Assets、Packages 或 Prefab。规范化时保留外部工具的稳定节点 ID 和资源引用；没有导入工具也能直接对现有 uGUI Prefab 使用上述规范化与校验功能。

从旧私有版本升级时，如果项目引用了旧的 Figma 工具包，请先决定保留私有来源还是迁移到其他独立工具。公开仓库不再提供该包或旧版本标签；不要将其引用盲目改为公开版本。升级不会自动删除消费项目现有插件和生成资源。迁移说明见框架仓库的 `Docs/PublicRelease.md`。

## 5. 验收

新项目至少应验证：

- Unity 无编译错误。
- App 只有一个架构入口，模块只在入口注册一次。
- 存档可往返读取，损坏主档可回退备份，旧版本可迁移。
- 热更在离线、取消、校验失败时有确定结果。
- 单个 SDK 失败或超时不影响独立适配器，但必须阻断依赖它的适配器；厂商身份同步和实际回传另行验收。
- UI Prefab 通过项目 Profile 校验，安全区与横竖屏符合目标设备。
- `python3 Scripts/validate_game_foundation_project.py --project .` 通过，每个行为改动关联 Feature Spec。
