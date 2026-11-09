# Game Foundation Architecture

## 目标

Game Foundation 是一组可独立复制或发布为 UPM 包的 Unity 2022.3 基础设施。它让新项目复用工程能力，同时由项目自己拥有玩法与产品逻辑。

```text
Project Architecture<T>
        |
        +-- com.nn555k.gamefoundation.core
        |       +-- clock
        |       +-- logging
        |
        +-- com.nn555k.gamefoundation.save
        +-- com.nn555k.gamefoundation.hotupdate
        |       +-- com.nn555k.gamefoundation.hotupdate.yooasset  # optional provider
        +-- com.nn555k.gamefoundation.sdk
        +-- com.nn555k.gamefoundation.ui
        +-- com.nn555k.gamefoundation.setup  # Editor only

Project business layer
        +-- Models / Systems / Commands / Events / ViewControllers
        +-- gameplay / level / economy / rank / IAP
```

依赖只允许从能力包指向 `com.nn555k.gamefoundation.core`。能力包互不依赖，项目可以按需安装。

## 包职责

| 包 | 包含 | 不包含 |
| --- | --- | --- |
| Core | QFramework 注册入口、时钟、日志契约 | 项目 App、全局单例、业务状态 |
| Save | 版本信封、校验、迁移、主备存储、可替换序列化 | 具体玩家数据结构、云存档供应商 |
| HotUpdate | 检查、下载、校验、提交、回滚的状态编排 | 具体供应商实现 |
| YooAsset Backend | YooAsset 初始化、非激活预下载、下载校验和清单提交 | CDN 地址、包名、加密策略、HybridCLR |
| SDK | 同意状态、顺序初始化、超时、隔离失败、生命周期转发 | Adjust、分析、广告或 IAP 的实现 |
| UI | 页面/弹窗导航、Safe Area、Prefab 规则与设计导入规范化 | 具体页面、玩法交互、业务状态变更 |
| Setup | 新项目目录、App、Controller、asmdef、UI Profile、Feature Spec、AI Skills 与 CI 治理脚手架 | 运行时代码、业务实现、自动覆盖项目自有指令 |

## 架构所有权

每个游戏创建并拥有一个 `Architecture<T>`。Foundation 不创建第二个架构，也不持有场景级 Manager。项目在唯一架构入口注册基础模块，再注册自己的 Utility、Model 和 System。

UI Controller 只负责展示、输入转发和状态绑定。业务写操作继续通过项目的 QFramework Command；Foundation UI Router 不绕过这一规则。

## 扩展边界

默认项目资源加载与目录约定见 `Docs/Foundation/ResourceLoading.md`（框架仓库内为 `Docs/ResourceLoading.md`）：按路径加载使用 Resources，直接引用的 Prefab 位于 `Assets/Prefabs`，非精灵美术资源位于 `Assets/Art`，`Assets/Sprites` 只放 Sprite；弹窗默认在 `Assets/Resources/Prefabs/UI/Popups` 首次加载并复用。代码职责见 `ProjectLayout.md`。YooAsset 是独立的热更能力，不自动接管 UI 加载。

出现跨项目都需要、与玩法无关且有稳定替换边界的能力时，才进入 Foundation。只有单个项目需要的功能先留在项目层；验证至少两个项目确实复用后，再抽取公共接口。

通用 UI 规则见 [UiLayers.md](UiLayers.md) 和 [UiAuthoring.md](UiAuthoring.md)：统一七层语义、排序所有权、模态栈和输入门控，保护项目 Inspector 配置。Setup 分发规范与项目接入模板，不生成生产 UI 根，不声明现有 Router 已实现 Stack，也不迁入项目相机、分辨率或暂停策略。

供应商 SDK、云存档和项目专属策略放在项目适配层。YooAsset 以可选 Provider 包提供，仍通过稳定接口与主包隔离。Figma 下载与同步工具不随框架分发，由开发者自行接入；UI 包只保留供应商无关的预制体规范化与校验能力。

## 版本与兼容

- `0.x` 阶段允许快速演进，但公共接口变化必须同步文档和测试。
- 存档版本独立于包版本，通过迁移链逐版升级，禁止跳过未知版本。
- Prefab 规范由 `UiPrefabConventionProfile` 资产定义，公共默认值与项目覆盖分离。
- 发布前必须运行静态边界检查、Unity 编译和 Foundation EditMode 测试。
