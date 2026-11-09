# 项目适配层接入

Foundation 稳定的是生命周期和错误边界，不持有厂商 SDK、CDN 地址、应用密钥或项目存档数据。消费项目在自己的 `Utilities` 或明确的基础设施目录中实现接口，再在项目 Architecture 初始化时注入。

## SDK

从 Package Manager 导入 `Game Foundation SDK` 的 `SDK Adapter Template`，在项目程序集里实现 `IProjectSdkBridge`，并创建 `ProjectSdkAdapter`。`SdkHost` 根据项目提供的授权状态执行授权门、顺序、超时、状态查询和 Unity 生命周期转发；具体隐私策略、厂商身份同步和 SDK 间就绪依赖仍由项目负责。

同时使用数数与 Adjust 时，按 [SDK 初始化与回传规范](SdkInitialization.md) 实现“数数就绪 → 实际身份 → Adjust 回传参数 → Adjust 初始化”，并填写 `Docs/Integrations/SdkInitializationChecklist.md`。只设置 `Order` 不会自动阻断前置失败或未授权后的依赖 SDK。

接入时至少验证：

- 未授权时不会初始化需要授权的 Adapter。
- 单个 Adapter 失败或超时不会污染独立 Adapter；依赖它的 Adapter 必须按项目门禁阻断。
- 用户 ID、焦点和暂停事件只发给已经 Ready 的 Adapter。
- 密钥来自项目配置、CI Secret 或平台配置，不提交到 Foundation 仓库。

## 内容热更

默认 UI / Prefab / 图片仍按 `ResourceLoading.md` 使用 Resources 或直接引用；安装热更适配器不会替换它们。只对项目明确选择的热更内容启用下面的后端。

YooAsset 项目直接安装 `com.nn555k.gamefoundation.hotupdate.yooasset`；它使用 `PreDownloadContentAsync` 在提交前保持旧清单有效，并将下载器句柄封装在 `ContentUpdatePlan.BackendContext`。其他供应商仍可导入 `Content Backend Template` 实现 `IProjectContentBridge`。

必须保持以下语义：

1. `CheckAsync` 只生成确定的更新计划。
2. `DownloadAsync` 写入临时或未提交区域并上报字节进度。
3. `VerifyAsync` 校验完整性和目标版本。
4. `CommitAsync` 只激活已经验证的内容。
5. 任一步骤失败时 `RollbackAsync` 可重复执行，且不会破坏当前可用内容。
6. `HasUsableLocalContent` 只在确有可启动版本时返回 true。

## 存档

从 Package Manager 导入 `Game Foundation Save` 的 `Versioned Save` 示例。项目为每种存档定义 DTO、当前版本和连续迁移链；Foundation 处理信封、校验、主备恢复和迁移写回。

存档版本发布前应覆盖：旧版本逐级迁移、主存档损坏回退、备份同样损坏、未来版本拒绝读取，以及删除/重置的显式用户流程。云存档应实现 `ISaveStore`，不要把账号或云厂商逻辑写进通用包。

## 项目注册位置

项目只在自己的 `Architecture.Init` 注册 Foundation 模块和具体适配器。UI、Command、Model 或业务方法不应直接查找厂商单例；它们依赖 Foundation 接口或项目 System，由初始化阶段缓存依赖。

真实厂商联调仍必须在目标平台完成，包括隐私授权、断网、超时、恢复、切前后台、冷启动和商店构建。通用仓库中的 NoOp 实现与模板只用于结构验证，不代表厂商验收完成。
