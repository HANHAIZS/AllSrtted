# SDK 初始化与数数 / Adjust 回传规范

本文提取 Turngrid 的初始化依赖，供同时使用 ThinkingData（数数 / TE）与 Adjust 的新项目复用。厂商适配、账号策略和隐私策略属于项目层；框架不会自动安装这两个 SDK，也不替项目配置后台。

核对日期：2026-10-23。来源项目解析到的版本为 ThinkingData 3.4.6、Adjust 5.6.0。新项目应记录自己的实际版本并验证 API，不能只按 SDK 名称假定兼容。本文是接入合同与验收规则，不代表旧项目已被修复，也不是新增的运行时依赖调度器。

## 1. 两条链路必须分开验收

```text
数数初始化 → 取得真实 TE 标识 → 写入 Adjust 回传参数 → Adjust 初始化
                                                    ├─ 客户端归因回调 → 客户端诊断事件
                                                    └─ Adjust 服务端 → 数数回传入口 → 用户关联 / 入库
```

客户端拿到归因、Adjust session success、数数收到客户端主动上报的事件，均不单独证明 Adjust 服务端回传已入库。初始化顺序解决的是标识准备与首次请求的竞态，不能保证后台配置、网络和服务端处理一定成功。

## 2. 从 Turngrid 提取的实际顺序

来源文件均位于 Turngrid 的 `Assets/Scripts/Game`，这里只保留可复用逻辑，不复制项目凭据、业务事件协议或关卡分流规则。

| 阶段 | 实际动作 | 来源 |
| --- | --- | --- |
| 注册 | 缓存依赖，先订阅归因等回调 | `Systems/Infrastructure/TurngridSdkSystem.cs` 的 `OnInit` |
| 数数 | 设置启动标识，执行 `TDAnalytics.Init`，开启自动采集 | `TurngridSdkSystem.Initialize`、`Utilities/Infrastructure/Sdk/TurngridAnalyticsUtility.cs` |
| 用户标识 | 读取 `GetDistinctId`、`GetAccountId`、`GetDeviceId` | `TurngridAnalyticsUtility` 的三个 Current 方法 |
| Adjust 配置 | 选择环境，提前绑定归因、session 成功/失败委托并保留引用 | `Utilities/Infrastructure/Sdk/TurngridAttributionUtility.cs` 的 `Initialize` |
| 回传关联 | 写入 `ta_distinct_id`、可用时的 `ta_account_id` 和 `ta_device_id` | `ConfigureThinkingDataCallbackParameters` |
| Adjust 启动 | 最后调用 `Adjust.InitSdk`，随后请求当前归因 | `TurngridAttributionUtility.Initialize` |
| 后续 SDK | 社交 SDK、按项目策略初始化广告、内购 | `TurngridSdkSystem.Initialize`；这些业务选择不纳入框架默认功能 |
| 后续刷新 | 首次启动、session 成功、ATT 结果、回到前台时尝试获取归因 | `RequestCurrentAttribution` 及调用方 |

这是主初始化链，不表示之前没有其他副作用：原项目在主链前还有广告策略和 App Open 记录。归因更新后写数数 super properties、上报 `adjust_attribution_update` 是客户端诊断，不是服务端回传收据。

原项目的 ATT 等待 30 秒、归因查询超时 5 秒及归因锁定条件都是项目配置，不应照搬为所有游戏的固定规则。不要复制业务埋点、关卡、广告门槛、IAP 或存档格式。

## 3. 新项目必须落实的依赖顺序

1. 确认配置、环境、隐私条件和唯一初始化入口。检查厂商组件、场景脚本、原生启动代码是否还会自动提前初始化。
2. 初始化数数；如果产品要求首包就关联已登录账号，先恢复合法可用的账号状态。
3. 从数数 SDK 读取实际标识。本接入合同要求 `distinct_id` 非空后才能进入依赖数数的 Adjust 分支；游客可以没有 `account_id`。
4. 构造 Adjust 配置、绑定回调，再写入全局回传参数。
5. 只有前置条件全部满足后才调用 `Adjust.InitSdk`。无依赖的其他 SDK 可按各自规则启动。
6. 异步接收归因并做有界补查；不要让游戏无限等待归因，也不要把 pending、超时或查询失败直接当作 organic。

数数官方要求初始化数数并同步标识后再初始化 Adjust；账号或访客标识变化后也要重新同步。参见[数数 Adjust 回传接入](https://docs.thinkingdata.cn/ta-manual/latest/user_guide/data/thirdparty/thirdparty_adjust/thirdparty_adjust_callbacks.html)。

| Adjust 参数 | 来源与规则 |
| --- | --- |
| `ta_distinct_id` | `TDAnalytics.GetDistinctId()` 的实际值；本合同的启动前置条件 |
| `ta_account_id` | `TDAnalytics.GetAccountId()`；游客可空，退出账号后移除旧值 |
| `ta_device_id` | 如项目需要，可同步数数设备标识用于诊断；不能替代 distinct/account 关联 |

不要用 Unity `deviceUniqueIdentifier`、IDFA、IDFV、Adjust adid 或 launch_id 冒充数数 distinct_id。仅使用账号关联的特殊方案必须另外明确约定和验收，不能悄悄绕开本合同。

Adjust v5 使用 `AddGlobalCallbackParameter` / `RemoveGlobalCallbackParameter`；旧示例中的 session 参数 API 需要按版本迁移。Callback 参数不是 partner 参数，事件级同名参数也可能覆盖全局值。参见 [Adjust 回传参数](https://dev.adjust.com/en/sdk/unity/features/session-parameters/)与 [v5 迁移说明](https://dev.adjust.com/en/sdk/migration/unity/v4-to-v5)。

下面仅展示关键先后关系，不是可直接替换项目启动器的完整实现；调用方须先完成数数初始化、隐私检查、单次初始化保护和 Adjust 回调配置：

```csharp
var distinctId = TDAnalytics.GetDistinctId();
if (string.IsNullOrWhiteSpace(distinctId))
    throw new InvalidOperationException("Analytics identity is not ready.");

Adjust.AddGlobalCallbackParameter("ta_distinct_id", distinctId);
var accountId = TDAnalytics.GetAccountId();
if (string.IsNullOrWhiteSpace(accountId))
    Adjust.RemoveGlobalCallbackParameter("ta_account_id");
else
    Adjust.AddGlobalCallbackParameter("ta_account_id", accountId);

// adjustConfig 已提前完成平台配置和回调绑定。
Adjust.InitSdk(adjustConfig);
```

只设一个标识同步责任方。登录、登出、切换账号或 Identify 后：先更新数数，再读取实际值并同步 Adjust，随后才放行新身份的相关事件。登出只移除自己管理的旧账号参数，不清空其他模块的全部参数。若选择厂商自动共享方案，应按其版本验证并替代手动同步，不要让两套写入机制竞争。

## 4. 失败处理与框架边界

**原项目存在不能原样复制的缺口：** `TryRunSdkStep` 捕获数数异常后仍继续执行后续步骤；Adjust 参数配置会跳过空值，但不会因此阻止 `InitSdk`。所以“调用顺序正确”不等于“依赖已经就绪”。本次提取不修改 Turngrid 的运行代码。

新项目实现必须满足：

- 数数被禁用、未获适用的数据处理同意、初始化失败或标识为空时，阻止依赖它的 Adjust 分支，记录原因；游戏和无关服务可按降级策略继续。
- 若业务确实允许不关联数数的独立 Adjust 模式，要明确列为另一种模式，不能将其报告为数数回传已就绪。
- 等待实际完成信号，不用固定 sleep 猜测就绪；设置超时、取消和有界重试，超时后的晚到回调不得绕过门禁再次启动。
- 防止重复初始化、重复订阅和多处并发改写身份。使用事件驱动，不逐帧查询用户标识或全量刷新。
- 记录阶段、错误、版本和脱敏关联证据；不打印完整用户标识、token、密钥或带凭据的回传地址。前置事件如需缓存必须真实实现有界队列；仅打印 buffered 日志不算缓存。

Foundation 的 `SdkHost` 提供 Order 排序、串行初始化、超时与生命周期转发，但**没有数数到 Adjust 的依赖图或标识映射**：

- `ContinueAfterFailure` 默认允许后续适配器继续；即使关闭它，因未授权而跳过的适配器也不会自动阻断后继依赖。
- 可把“数数初始化 → 身份门禁 → 参数同步 → Adjust 初始化”封装在同一个项目级 Bridge 内，任一前置失败就停止这条链；其他独立适配器单独注册。
- 若拆成多个适配器，必须显式实现就绪依赖和失败传播，不能仅依靠 `Order`。被依赖阻断时不得成功返回、让宿主误记为 Ready。
- `SdkInitializationContext.UserId` 不是自动生成的数数 distinct/account/device 三元组；项目需自行完成映射和后续身份变更同步。
- 使用现有单一启动入口和项目 QFramework 架构，不为此额外新建全局 Manager。厂商代码与凭据留在项目层。

## 5. ATT 与后台回传

ATT 和数据处理同意不是同一条件。由项目隐私策略决定各 SDK 可以何时启动、可以采集什么；不要统一把 consent 设置为 true。ATT 展示时机、拒绝/受限状态、前后台恢复与归因补查应分别验证。ATT 未授权不应被直接解释为所有归因能力必然失败。

后台需检查数数项目/环境、Adjust 环境、回传入口、事件类型及用户字段映射。默认关联字段是 `ta_account_id` / `ta_distinct_id`；两者均空可能导致记录丢弃。必须以实际服务端回传和数数用户/事件记录核对关联结果，不能用客户端补报代替。参见[数数回传配置与排查说明](https://docs.thinkingdata.cn/ta-manual/latest/user_guide/data/thirdparty/thirdparty_adjust/thirdparty_adjust_callbacks.html)。

参数晚补不能作为已发出首包一定会被自动修复的保证；重试、延迟和历史补发能力需按实际厂商服务验证。“实时”应记录实际延迟及项目验收标准，不承诺固定秒数。

## 6. AI 实施与验收入口

新项目按需填写 `Docs/Integrations/SdkInitializationChecklist.md`，并在对应 Feature Spec 记录初始化顺序、标识来源、依赖门禁、失败/恢复策略和后台验收负责人。不使用这组 SDK 的项目可以说明不适用，不因此被要求安装厂商 SDK。

最小验证覆盖正常顺序、前置失败/无同意/空标识阻断、取消及晚到回调、游客到登录/登出/切号、重复启动、离线恢复、ATT/前后台，以及真机服务端回传入库。客户端测试可以使用替身，但服务端回传项不能靠替身或静态检查标为通过。

项目规范校验器检查文档和工程约定，不会证明任意厂商代码的实际调用时序或远端入库。报告必须区分：文档已生成、代码已实现、本地测试通过、真机通过、后台链路通过。

接入工具默认保留项目已有文档和 Skills。升级既有项目时应对照新模板合并本文件、清单与 SDK 规则，不能假设仅更新包引用就更新了全部项目规则，也不要覆盖项目已经填写的验收证据。
