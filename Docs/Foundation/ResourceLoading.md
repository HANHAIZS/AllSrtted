# Resource Loading Convention

新项目默认使用 Unity `Resources` 按路径加载资源，不默认改用 YooAsset 或 Addressables。以下规范适用于新资源和明确授权的迁移；升级不会自动移动旧资源、改写存档或替换现有加载行为。

## 按类型与加载方式选择目录

| 资源 | 默认位置 | 加载方式 |
| --- | --- | --- |
| 弹窗 Prefab | `Assets/Resources/Prefabs/UI/Popups` | 首次打开时 Resources 加载，按宿主生命周期缓存复用 |
| 其他按路径加载的 UI Prefab | `Assets/Resources/Prefabs/UI` | Resources |
| 按路径加载的 Sprite | `Assets/Resources/Sprites/UI` | Resources |
| 直接引用的 Prefab（HUD、组件、角色、环境等） | `Assets/Prefabs` | Inspector / 场景 / Prefab 引用 |
| 模型、Mesh、材质、Shader、普通/法线贴图、字体及许可证 | `Assets/Art` | 直接引用或编辑器工具输入 |
| 精灵图片与原生序列化 Sprite | `Assets/Sprites` | 直接引用 |
| 原始设计源文件 | `Assets/Art/DesignImports` | 编辑器源数据 |
| 导入的 Prefab 草稿 | `Assets/Prefabs/DesignImports` | 编辑器规范化输入 |
| 从设计中提取的 Sprite 图片 | `Assets/Sprites/UI` | 直接引用 |

`Sprites` 只放真正的 Sprite。图片扩展名不代表 Sprite：以 Unity 导入类型为准；普通贴图和法线贴图仍归 Art。原生序列化 Sprite（Unity class ID 213）也可放 Sprites。不要为了通过目录校验而修改 importer。不要在 Sprites 下创建 Prefabs、Materials、Models、Fonts 或 Resources 目录。

Resources 中现有配置、音频、字体等保留原路径和键；新增按路径加载的其他类型使用其类型子目录。脚本、场景和 Editor 设置继续按职责存放。目录选择不替代对实际引用和加载方式的检查。

Resources Prefab 可以直接引用 Art / Sprites 中的依赖，无需复制或移动整棵依赖树。直接引用的资产会随其引用方加载；仅推迟 Instantiate 并不能消除 Prefab 的硬引用加载成本。Resources 中的内容会进入构建，但并不因为位于 Resources 就全部在启动时加载；共享纹理、图集或其他引用方仍可能让依赖提前驻留。

## 弹窗按需加载与生命周期

- 弹窗默认放在 `Assets/Resources/Prefabs/UI/Popups`。宿主保存可追踪、可配置的 Resources 键，不在启动场景或 HUD 上直接引用所有弹窗 Prefab，也不在启动时批量 Load 或实例化。
- 加载键相对 Resources，使用 `/`，不带扩展名或 `Assets/Resources/` 前缀，例如 `Prefabs/UI/Popups/SettingsPopup`。由项目配置或目录表统一维护，不在点击回调中随意拼接。
- 普通首次加载用 `Resources.Load<T>`；确有预加载需求时可用 `Resources.LoadAsync<T>`，并明确时机和内存预算。异步完成时检查宿主是否已销毁或请求是否过期。
- 首次打开加载、实例化并初始化一次；关闭后默认隐藏并复用。重复打开不重复加载、订阅或创建实例。宿主销毁时清理实例和引用；若使用跨场景宿主或对象池，明确其所有者和释放边界。
- 缺失资源、组件不匹配或初始化失败时记录加载键，并使用项目现有提示机制；清理失败实例，保留当前页面、遮罩及业务状态，不显示空白弹窗。恢复资源后应可重试。
- 加载资源与创建实例是不同步骤。不要每次关窗调用全局 `Resources.UnloadUnusedAssets`，不要卸载其他视图仍使用的共享依赖。
- 文件名、Prefab 根名与类型后缀保持一致。禁止重复 Resources 键，包括大小写或扩展名不同以及多个 Resources 目录中的冲突。

## 创建、迁移与删除

1. Feature Spec 记录最终路径、Resources 键或直接引用方、加载/缓存/释放责任，以及缺失与恢复行为。
2. Figma 导入保持源层与生产层分离；导入工具由开发者自行管理。Popup 结构遵循 `DimBackground` + `PopupContent` 和项目 Profile。
3. 经 Unity Editor API 创建、移动或删除资源；迁移保留 `.meta` GUID、Figma 稳定 ID、引用及 Inspector 配置。不得文本修改 Prefab / Scene / Asset，不得未经任务授权批量整理旧资源。
4. 删除模型前检查反向依赖、场景、Prefab、配置、Resources 字符串加载，以及 Editor 烘焙/生成工具的源路径。未显示、当前关闭或仅供编辑器使用，不代表未使用。
5. 对确认无引用且任务授权删除的资产，保留可恢复版本或在 Assets 之外备份文件与 meta。模型迁移/清理不应改变玩法、存档格式、加载键或 UI 行为。
6. 验证真实 Resources 加载、实例化、关闭重开、宿主销毁及失败恢复。AssetDatabase 能找到文件不能替代运行验证。

## 验证与升级边界

- Python 与 Unity 校验器都检查 Sprite 类型、重复 Resources 键和错误嵌套。静态检查不能证明是否有动态使用；仍需依赖审计和运行证据。
- 测量启动实例数、首次加载和复用成本、纹理/网格驻留及 GC。区分 Editor 与设备、资产原生内存与进程总内存，不把资源文件大小直接当作内存节省。
- YooAsset 只管理明确选择的热更内容，`Assets/GameContent` 不是默认 UI 根目录。不要自动采集 Resources 或将同一资产复制到两套加载体系。
- v0.6.1 起新项目使用本规范。旧版将非 Sprite 放入 Sprites 的项目会收到新的目录错误；在明确授权的迁移中修复，不能通过改变 importer 或忽略校验掩盖问题。
- Setup 只创建缺失目录，保留既有文件与 AGENTS 的项目自有段落。旧项目应审阅并合并更新的文档和 Skills，不能用全量覆盖代替合并。

Unity 依据：[Resources.Load](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Resources.Load.html)、[Loading Resources at Runtime](https://docs.unity3d.com/2022.3/Documentation/Manual/LoadingResourcesatRuntime.html)。
