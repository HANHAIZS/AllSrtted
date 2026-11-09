# Figma To Unity UI Workflow

## 双层资产

框架不包含 Figma 下载或同步工具。开发者按需自行接入外部工具并验收；基础接入只提供 UI 规范、Profile 与预制体规范化能力，AI 不得因为要整理 UI 而擅自安装插件。

Figma 插件导入的内容作为原始设计层，生产 Prefab 作为运行层。原始层允许再次同步，运行层必须满足项目命名、层级、组件绑定和性能约束。不要直接在运行时代码里长期兼容插件生成的随机层级。

## 标准流程

1. 如需下载或同步，确认开发者已接入外部设计工具，并已配置本机凭据；AI 不自动安装。已有导入资产无需重新下载。
2. 确认 Figma 文件、页面、Frame、节点 ID 和导入目录。
3. 通过项目选择的外部工具拉取到临时源层，按稳定 File/Frame/Node ID 同步明确允许的字段；具体能力以实际工具为准。
4. 区分 Popup、Page、Hud、Overlay、ListItem 或 Component，并按 `ResourceLoading.md` 确定加载方式和生产路径：路径加载的预制体在 `Assets/Resources/Prefabs/UI`，弹窗在 `Assets/Resources/Prefabs/UI/Popups`，直接引用的预制体在 `Assets/Prefabs/UI`。设计源文件在 `Assets/Art/DesignImports`，导入 Prefab 草稿在 `Assets/Prefabs/DesignImports`，提取的 Sprite 图片在 `Assets/Sprites/UI`；普通贴图和材质在 `Assets/Art`。
5. 保存一份稳定的语义命名映射；不确定含义的节点保持原名并报告。
6. 调用 `FigmaPrefabNormalizer.Preview` 查看将创建、移动和重命名的节点。
7. 通过 Unity Editor API 调用 `FigmaPrefabNormalizer.Apply`，不得文本编辑 Prefab YAML。
8. 运行 `UiPrefabValidator.Validate`，解决错误后再绑定脚本。
9. 检查动画路径、Prefab Variant、组件引用、布局、Safe Area 和多分辨率截图。
10. 若资产有外部同步绑定，验证其稳定 ID 与资源引用仍可回读。

## 默认 Popup 规范

```text
ExamplePopup
  DimBackground
  PopupContent
    Header
    Body
    Actions
```

`DimBackground` 必须全屏拉伸、黑色、Alpha `0.97`，并保留射线遮挡。主体与脚本绑定节点放在 `PopupContent`，不得用 `Container` 替代。

项目可以在自己的 `UiPrefabConventionProfile` 中声明额外根节点。文件名、根节点名和类型后缀必须一致，例如 `SettingsPopup.prefab`、`HomePage.prefab`、`GameHud.prefab`、`LoadingOverlay.prefab`、`RewardItem.prefab`。

## 同步安全

导入后的视觉保护遵循 [UiAuthoring.md](UiAuthoring.md)：按钮 Preserve Aspect 和 Image Type 均为 authored 配置，未勾选 Preserve Aspect 不是错误。规范化仅修改拥有的结构字段，不能统一重写按钮文字、贴合方式或场景覆盖。挂载与排序遵循 [UiLayers.md](UiLayers.md)；Prefab 的 Overlay/Hud 等类型不是新的排序层。

- 保留外部同步组件中的 File/Frame/Node ID；显示名不是同步主键。框架不要求特定的插件组件类型。
- 默认只同步 Sprite 与颜色。文本内容、尺寸、坐标和激活状态需按页面显式开启。
- 本地化文本不应被 Figma 文案覆盖。
- 重命名前检查 AnimationClip 路径、序列化对象引用和代码中的字符串查找。
- 原始层可以重导入；生产层的结构改动必须可预览、可校验并进入版本控制。
- Figma Token 只允许保存在 EditorPrefs 或本机环境变量，不进入 ScriptableObject、Prefab、日志或版本库。
