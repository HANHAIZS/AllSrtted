---
id: home-wallet
title: 主界面与商城金币、体力和离线恢复
status: Verified
owner: Project
created: 2026-10-27
updated: 2026-10-27
---

# 主界面与商城金币、体力和离线恢复

## Goal

使用指定金币和体力美术，在 Home/商城最顶部左右显示共享资源，提供免费增加按钮、每15分钟体力恢复、本地保存及退出/重开/失败扣体力。保留现有三页导航、关卡、道具和暂停布局。

## User-visible behavior

- Home 和 Shop 顶部中央并排显示体力（中线左侧）与金币（中线右侧），整体与开始游戏按钮中线对齐；可覆盖 Shop 文字上方的空白标牌区域，不能遮住 Shop 文字。Settings 不显示这两个入口。避开安全区，切页动画期间不接收资源按钮操作，Home/Shop 间位置与资源保持一致。
- 首次无存档金币1600、体力5；金币圆标右下角的绿色“+”每次增加800，不扣费、不接支付。红心图标右下角的绿色“+”每次加1，上限5；满额仍可反馈点击但不超过5。
- 数字为动态 Unity 文字。体力满额显示心内“5”和右侧“Full”；不足时心内显示当前额度，右侧显示下一点恢复倒计时 MM:SS。未满时每15分钟+1，上限5。满额不积攒恢复时间；从满额扣除后开始新的15分钟，未满时手动加减保留原计时，补满则清零。
- 金币、体力与下一次恢复时间本地保存，退出应用和跨场景不重置，离线时间按UTC结算。暂停和时间缩放不影响恢复。系统时钟回拨不会额外增加额度。
- 开始关卡不扣体力，但0点拒绝开始。退出、重开或失败扣1，同一局最多一次；失败后退出或重开不再扣本局。成功通关/下一关不扣；自动暂停、关闭暂停窗、切页、后台切换不扣。
- 用户确认：仅剩1点且本局未扣时，允许扣到0并立即重开；这局可以正常游玩，此后的开始/重开在0点时拒绝。0点仍可退出。
- 失败已扣到0时，保留失败页面并提供 HOME 回到补充入口；暂停窗的 Quit 仍可回 Home。0点时的重开拒绝不会清掉棋盘、原弹窗或道具；本关所用道具始终不返还。
- 关卡退出目标不存在或加载异常时不扣体力；重复/隐藏弹窗回调无效。资源按钮隐藏、过渡或离场回调不改变余额。

## Out of scope

商城商品、金币消费、付费/广告、云存档、服务器时间/防改时钟认证、关卡进度及其他偏好持久化、新关卡、关卡HUD资源栏。现有编辑器直接选关/低层内容初始化入口仍可用于开发，不作为玩家0体力绕过入口。

## Ownership and architecture

| Responsibility | Owner | Path | Notes |
| --- | --- | --- | --- |
| 资源与恢复事务 | Model/domain state | Models/Wallet/SortWalletLedger.cs、SortWalletModel.cs | Ledger 是唯一资源状态；Model 提供只读值与修订观察，不复制余额 |
| 资源入口与发布 | System | Systems/Wallet/SortWalletSystem.cs | 初始化缓存唯一 Model、时钟、存储；委托账本并发布修订 |
| 写操作 | Command | Commands/Wallet/SortWalletCommands.cs、现有 SortCommands.cs/Navigation Commands | 增加、结算、扣除、重开/开始门控 |
| 棋盘与失败转换 | 现有 System | Systems/Sorting/SortGameSystem.cs | 每次真实新盘建立新局标识；失败转换扣一次；胜利不扣 |
| 页面展示 | 现有 Controller focused partial | ViewControllers/Home/SortHomeViewController.Resources.cs | 原组件拥有HUD绑定、监听和展示更新；没有新常驻管理器 |
| 作者化 | Editor | Editor/Home/HomeWalletArtwork.cs | 通过Unity API定点添加HUD，不重建旧场景 |
| 通用存储与时间 | Foundation existing + project Utility | VersionedSaveService / ISaveStore / IFoundationClock；Utilities/Wallet/SortResourceSaveStore.cs | 文件读异常由项目边界抛出，避免误判为首次安装；写入仍委托Foundation原子替换，不修改包 |

## State and persistence

- Ledger 保存 coins:int、energy:int、nextRecoveryUtcTicks:long、lastSeenUtcTicks:long，v1，slot=all-sorted-wallet，使用 Foundation 的校验信封及文件主备恢复。缺失为首次安装默认；旧项目没有此slot，无迁移。新于当前版本/两个副本损坏时不覆盖，进入只读并显示实际错误；正常备份可恢复主文件。
- 金币使用非负int，超出int上限时饱和，不回绕。体力0..5；下次恢复UTC必须有效，满额时为0。未知/非法数据拒绝覆盖。保存写入失败时不发布增加/扣除；允许重试。恢复只有额度变化或生命周期保存时写盘，不每秒写倒计时。
- 局标识与是否已扣属于运行时局状态，不跨启动保存：未完成棋盘本身不恢复，强制关闭应用不是用户请求中的点击退出。失败后重复扣除由本局标记拒绝，新的棋盘才重置标记；旧局标识回调无效。
- UI -> Command -> System -> Ledger/Model；依赖仅 AllSortedApp.Init 注册。测试使用独立内存存储和可控时钟，不修改玩家存档。

## UI, prefab, scene, and Inspector impact

- 新Sprite在 Assets/Sprites/UI/Home/Wallet/Coins.png、Energy.png，由 HomeScene 原导航控制器的HUD Image直接引用，无Resources key；源自用户两张PNG。imagegen 仅去烘焙棋盘格、1240、5/Full，保留原币/心/底板；精确提示词与产物清单交付。
- Panels 层的 ResourceHud 共享安全区，两个装饰背景与绿色角标按钮；Home/Shop复用，Settings淡出。装饰不拦截输入；44单位角标点击区，图形可较小。数字使用现有 Lilita One。
- 原Scene中的新增子树与控制器新资源字段允许改变；相机/7层/UI排序、底部导航与分隔线、关卡按钮、Shop顶部、设置页和原Inspector值受保护。GameScene/PausePopup/ResultPopup文件不重写。
- 仅0体力终局在已有ResultPopup运行时显示HOME次按钮以返回补充，不新增Prefab/Resources/模态路由；其他弹窗生命周期仍首次加载、缓存复用、Replace。
- HUD随HomeScene卸载销毁，监听/按钮回调在原控制器销毁时移除。缺HUD绑定不破坏原三页，但报告定位错误；缺场景保留当前界面。

## Integrations and failure behavior

离线可用，无SDK/网络/同意变更。存储失败不虚报增加/扣除，不破坏原唯一副本。恢复使用已缓存时钟；重进前台和打开页面结算跨后台时间。0体力拒绝是正常交互提示，不记录为编译/运行错误。保存主备恢复、读写失败、时钟回拨、超长离线、重复回调纳入测试。

## Performance considerations

HUD按修订及整数秒更新，动画只改展示alpha；无每帧资源查找/纹理生成/磁盘写入。保存是小JSON事务，金币点击、体力变化及退出/后台时提交。三张静态Sprite（金币、体力、用户指定角标），不新建Canvas或常驻Update管理器。

## Acceptance criteria

- [x] 默认1600/5及两种“+”操作正确，满额不超过5。
- [x] 每15分钟恢复、部分计时、跨后台/离线、满额清零和回拨正确。
- [x] 退出/重开/失败每局最多扣一次，最后一点重开规则与0点门控正确。
- [x] 存档跨启动有效，缺失/备份/腐坏/未来版本/写入失败处理正确。
- [x] Home/Shop真实指针可用，Settings及隐藏回调无效；原三页和道具行为保持。
- [x] 手机/iPad比例HUD与Shop标题不重叠，0点可回Home补充。

## Verification matrix

| Boundary | Evidence required | Result |
| --- | --- | --- |
| 资源核心与保存 | 可控时钟、内存/文件回读、恢复与失败测试 | 115/115 EditMode；独立临时目录文件回读；不可读现有路径拒绝覆盖 |
| 扣除与场景入口 | 失败/重开/退出/胜利/0点/重复回调及道具不变 | 41/41 PlayMode，包含本次5项真实资源HUD/场景流程及现有36项回归 |
| HUD | 真实指针、切页、禁用恢复、默认/低体力/0/Full/余额更新 | 实际指针+800、+1封顶；双页共享，Settings隐藏；两栏宽高与缩放相同 |
| 保护范围 | 原组件/场景/Prefab哈希比较；7层与绑定读回 | 314个原native对象，仅Panels增加一个新子节点、原Home控制器增加9个新绑定；其余旧属性与引用不变。16项旧Scene/Prefab/PNG逐字节一致 |
| 编译规范 | Unity错误检查、validate_game_foundation_project.py | 编译成功；普通Play启动后Console 0错误/0警告；规范校验0警告 |
| 设备/性能 | 手机iPad编辑器截图、缓存与写入频率检查；真机未连接 | 720×1280与768×1024截图已检查；真实设备与GPU分析尚未执行 |

## Definition of Done

- [x] 状态与QFramework所有权一致，源图/产物/资源路径明确。
- [x] 接受规则有测试与运行证据，失败路径可恢复。
- [x] 编译、规范及必要现有回归通过，独立审查的实际问题已处理。
- [x] 截图、存档位置、性能边界与设备未测项交付，最终才标记Verified。

## Decision log

- 2026-10-27 用户确认退出/重开/失败扣1；跨启动保存及离线恢复。
- 2026-10-27 用户确认0点禁止开始/重开；同一局只扣一次。
- 2026-10-27 用户确认剩1点可扣到0并立即重开，0点限制之后操作。

- 2026-10-27 用户运行中补充布局：两栏向中间靠齐，允许覆盖Shop文字上方空白空间，Home与商城采用同一位置，整体位于开始游戏按钮正上方的顶部。只移动位置，不改本次已确认的功能规则。

- 2026-10-27 用户随后要求尺寸更大：资源栏整体放大25%，数字与绿色增加角标同步放大，继续顶部居中并保留Shop文字可见。

- 2026-10-27 用户指定绿色角标为 /Users/zhangshuai/Documents/欧卡风格/各界面之角标.png；仅去棋盘格底，保留原绿色边框和白色加号。角标附着在红心/金币圆标右下角（非整个计数底板右下角）。

- 2026-10-27 用户要求两栏同等大小：以金币栏为基准，体力栏使用相同宽高，Home与Shop同步，角标位置保持在各自图标右下角。

## Verified delivery — 2026-10-27

- Evidence: `/Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/all-sorted-prototype/home-wallet/`。最终结果为 `full-edit-final.json`（115/115，job e29567ad4b454911a87fd97c22f248c8）、`full-play-final.json`（41/41，job 5275f2ac0d9f45318b9e2567371dc915）。六张 `wallet-{home,shop,low}-{phone,ipad}.png` 来自实际Unity相机；Shop截图3200为测试中两次+800后的预期值。
- 原场景保护证据 `native-asset-comparison-final.json`；普通Play数据/纹理回读为 `ordinary-play-readback.json`。当前原生运行读取已存在的4800金币、5体力并核对信封校验通过；没有重置玩家已有余额。首次新存档1600/5由独立存储测试验证。
- Save path: `/Users/zhangshuai/Library/Application Support/DefaultCompany/All Sorted!/GameFoundation/Saves/all-sorted-wallet.json`，相邻 `.backup` 由Foundation保留。Schema v1，实际运行与文件值一致。
- 两栏使用金币栏宽高：当前安全区720×1280时各156×59.65，整体缩放1.25；Home/Shop位置相同。Energy Image只在本次新HUD内取消Preserve Aspect，使其填充同尺寸矩形，其他旧Image选择不变。
- Sprite import：Coins/Energy各512×512 RGBA32，PlusBadge 256×256 RGBA32，1级mip、readable=false。基础GPU像素约2.25 MiB；Editor Profiler运行内存合计4,720,608 bytes（约4.50 MiB，含编辑器计数），不等同于真机实测。没有新Canvas/常驻管理组件；每秒最多一次恢复检查，数字修订/整数秒更新，小JSON只在状态变化/生命周期/待保存事务重试时写入。
- 项目改动：新增Wallet Model/Ledger、System、Commands、Utilities/Wallet/SortResourceSaveStore、Home资源focused partial与HomeWalletArtwork作者工具；现有Architecture、GameSystem、入口Commands、GameViewController相关partial和HomeSceneBuilder定点接入。HomeScene仅新增Panels/ResourceSafeArea/ResourceHud子树与9字段绑定；GameScene/PausePopup/ResultPopup文件未改。具体新增/修改源文件清单见证据目录 `changed-sources.json`。
- 最终停在已保存的HomeScene Edit Mode。Inspector无需额外绑定；iPhone/iPad真机、安全区异形屏与设备性能尚未验证。

## Review resolutions

- 一次独立审查发现退出保存失败后遗失扣除、失败扣除不重试、文件读取异常被误判Missing三个问题。`review-regressions-red.json`保留修正前失败证据。
- 退出保留现有Single场景加载方式：加载调用成功后登记扣除；保存失败的事务保留在唯一Ledger并阻止下一次进入，Home显示错误并按秒重试，恢复后先扣旧局再放行。这样不需要另建场景路由或同时激活两个UI根；不把失败操作当作已付。存储持续不可写时留在Home等待恢复，进程被强制结束且磁盘始终不可写时无法承诺保存。
- 失败扣除仅登记一次；待保存状态由Ledger保存，Game的一秒刷新重试，成功后保持本局已扣，不会每帧写入或重复扣除。测试覆盖失败期间写入次数、恢复后扣除、随后重开不再扣旧局。
- 项目存储边界直接读取文件，仅FileNotFound/DirectoryNotFound作为缺失，其余I/O/权限异常阻止覆盖；写入/恢复/主备仍使用原Foundation FileSaveStore。
- 运行回归中3项旧测试最初失败原因是普通SetUp晚于UnitySetUp，重置掉已创建棋盘的局标识。PlayMode资源隔离改为基类UnitySetUp，在派生类加载场景前完成，原断言完整保留；最终全部41项通过。

## Follow-up — 2026-10-28

Wallet storage is now schema2, with four booster counts atomically saved alongside coins/energy. See booster-purchase.md for validated v1 migration and purchase persistence. Existing energy and coin UI/rules remain.

## 状态视觉修正 — 2026-10-28

满体力5时隐藏EnergyPlus，0–4显示；初始化、共享Home/Shop、补充达到上限、UTC恢复、重入均一致。隐藏回调不补充，金币角标不变。 验证完成：原生回读确认旧遮罩154×154、按钮200×210；修正后遮罩同父按钮尺寸和Sprite，禁用射线阻挡。新增两项回归修正前均失败（f49d189acf524d1eb8bc7ef500da6ee7），修正后相关HomeNavigation/Wallet PlayMode15/15通过（80139a7216e749b3ae4b2b1bb1cc8dac），覆盖补满、UTC恢复、Home/Shop共享、场景重载和真实点击。手机/平板关闭状态及满/未满体力截图位于 `/Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/home-state-visuals/`。Foundation规范校验0警告，编译无错；HomeScene仅三个遮罩Image及RectTransform变化。无新组件/纹理/存储格式；运行时仅既有刷新入口调整角标显隐。真机未测。

## 关前道具补充 — 2026-10-28

[prelevel-boosters](prelevel-boosters.md) implements Home Start → selected-booster popup → PLAY. Wallet schema3 preserves original resources and four booster counts while adding independent EntryCharges[2] default3. Closing does not spend; red numeric badge when stocked, green+at0; selected effects apply once per Home entry. Existing retry/next do not reuse effects.

## 金币图片替换 — 2026-10-28

[coin-art-refresh](coin-art-refresh.md) replaces supplied hexagon-coin art only. Existing dynamic balances, prices, quantities, grants/spending, saves and modal motion remain. Purchase button coin stays on LEFT per user correction.
