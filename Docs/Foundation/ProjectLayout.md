# Project Layout

本规范面向消费项目；以 `.gamefoundation/project.json` 中的 codeRoot、架构文件和 Controller 为准。默认代码根为 `Assets/Scripts/Game`。已有项目保留其唯一架构、命名空间、序列化类型和公开 API；不要为了匹配生成器名称再建一个架构。

## 代码职责

| 代码根下的位置 | 内容 |
| --- | --- |
| Architecture | 唯一 QFramework Architecture 与 Controller 基类 |
| Commands | AbstractCommand 与命令专属参数 |
| Models | Model、模型拥有的状态、值对象和序列化数据 |
| Systems | ISystem / AbstractSystem 与实际系统实现 |
| Events | 一次性通知；默认不可变 readonly struct |
| ViewControllers | Unity 展示、输入转发、绑定、渲染辅助与展示缓存 |
| Utilities | 无业务状态所有权的计算、格式化和适配辅助 |
| Editor | 项目编辑器构建、烘焙和验证工具 |
| Runtime（已有项目按需保留） | 可读运行诊断状态；不引入第二套状态所有权或启动器 |

在角色目录内按职责细分，例如 Models/Progress、Systems/Board、Utilities/Levels、ViewControllers/UI。不要创建项目名嵌套层、业务代码兄弟根目录，或模块内另一套 Models/Systems/Commands。

类由自身责任决定位置，不由调用方决定。算法 helper 不因 System 使用它就进入 Systems；直接创建 Unity 展示对象或维护展示缓存的 helper 属于 ViewControllers；模型状态不放 Utilities。

## 依赖与变更边界

- 依赖只在唯一 Architecture.Init 注册；已有场景入口初始化该架构，不另建隐式 Manager、单例或 Bootstrap。
- Model/System 在 OnInit 缓存依赖，Controller 在 Awake/OnInit/Bind 等初始化阶段缓存。Command 执行开始先解析为局部变量再调用。
- 运行回调、点击、刷新、动画与埋点方法不重复 GetModel/GetSystem/GetUtility。修改后搜索并逐项核对调用位置。
- Controller 展示数据和发送 Command；规则和状态修改由 Commands/Systems/Models 承担。可观察状态使用 BindableProperty，一次性通知使用事件。
- 大型场景绑定 Controller 可以按职责拆成 partial 文件，保留组件和 GUID；不要仅为拆文件添加多个 MonoBehaviour 或改动场景结构。
- 避免热路径全量扫描、重复 LINQ/查找/订阅和临时分配，优先缓存、局部更新、事件和有明确生命周期的对象池。
- 重构保留存档键/字段、序列化类型、Inspector、Prefab overrides 和现有行为；以最小相关编译及运行检查验证。

## 资源目录

遵循 [ResourceLoading.md](ResourceLoading.md)：直接引用 Prefab 放 Assets/Prefabs；模型、材质、普通贴图、字体等放 Assets/Art；Assets/Sprites 只放 Sprite。弹窗默认 Assets/Resources/Prefabs/UI/Popups，首次打开加载并复用。既有 Resources 键不得因规范升级而自动改变。
