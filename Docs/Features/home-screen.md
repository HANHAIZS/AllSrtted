---
id: home-screen
title: 主界面、商城与设置三页导航
status: Verified
owner: Project
created: 2026-10-27
updated: 2026-10-27
---

# 主界面、商城与设置三页导航

## Goal

使用本次提供的工厂背景、紫金导航栏、商城/Home/设置图标和绿色开始按钮风格，制作竖屏主界面及商城、设置页面。按整体参考图组织布局，按参考视频制作可连续切换的导航动效。开始按钮显示实际待游玩的关卡，点击进入现有三关之一。沿用唯一 AllSortedApp 架构及现有玩法、道具与设置状态。

## User-visible behavior

- 正常启动先显示 HomeScene。保留 GameScene，Unity 编辑器仍可直接打开它测试，现有 1/2/3 关菜单继续工作。
- 工厂图片铺满屏幕，等比覆盖，不拉伸。画面顶部不添加本次未要求的标题、金币、体力或其他系统。
- 底部导航栏横向铺满，三个等宽点击区：左商城、中 Home、右设置。首次进入默认 Home；从关卡 Quit 返回也落在 Home。两条竖向区分线固定在宽度的 1/3、2/3 处，从平直栏顶部延伸到底部，不随选中件移动；平直背景使用不含分隔线的 Sprite 区域，禁止把原图里的线随整图拉伸到错误位置。选中件覆盖相邻边界时由自身边框自然衔接。
- 三个入口都能切换到实际页面。Home 显示工厂背景和开始按钮；商城显示本次提供的深紫色商城背景及紫金 Shop 顶部栏，暂不添加未提供的商品、价格或购买流程；设置使用本次提供的 Setting 页面图片，并将 Music/Sound/Haptic 区域做成真正的独立开关。
- 导航动效遵循参考视频：紫色凸起选中底板及黄色上边框作为一体，连续横移到目标入口；选中图标放大并上移，下方标签淡入；原选中图标缩小下移、标签淡出。仅当前选中项显示 Shop / Home / Setting 标签。底栏主体和三个点击区位置保持稳定。左右跨栏选择直接滑到目标，不闪现中间页。
- 初始参数：导航过渡 0.25 秒、平滑加减速，使用非缩放时间。选中图标相对未选中约放大 35%，上移约 44 个设计单位；最终通过实机画面观感在 0.20～0.30 秒区间校正，不制造明显过冲。视频帧率只有 15fps，因此 0.25 秒是初始实现参数，不宣称是原始工程的精确数值。
- 页面主体采用约 0.12 秒短淡入淡出；这是对视频未完整展示部分的设计选择。过渡开始后立即禁用各页面主体输入，完成后只开放最终目标页面，避免离场按钮误触。导航栏保持可点击；快速连续点击以最后一次为准，从当前实际位置/缩放/透明度接续，不归零、不排队等完整动画；点击当前目标不重启动画。
- 设置页三个开关复用 SortSettingsModel 和 ToggleSortSettingCommand，默认 ON。OFF 使用与暂停界面一致的变暗绿色按钮及红色左上到右下斜线；开关状态在切页、进关、Quit 和重开后保留，主界面与暂停窗互相同步。当前工程无实际音频/震动播放，本期实现真实设置状态与可见反馈。
- 绿色开始按钮只在 Home 页显示并可用，位于导航栏正上方、水平居中，白色粗体关卡文字配深绿色描边，动态显示 Level 1 / Level 2 / Level 3。不能保留素材中写死的 Level 18，也不能把文字烘焙为三张关卡图片。
- 首次启动显示 Level 1。本次运行中从关卡暂停界面退出后，主界面显示退出时的关卡；再次点击开始重新开始该关，而非恢复离开时的残局、倒计时或冻结效果。
- 暂停弹窗 Quit 从原先临时无操作改为返回主界面。Close X 仍继续当前对局，Restart 仍重开本关。退出和重新进入都不返还已使用的道具额度，已增加的额度也保留。音乐/音效/震动开关仍保持本次运行的状态。
- 现有胜利后的 NEXT LEVEL 与第三关后的 START OVER 行为保留；不新增关卡选择、解锁或奖励。编辑器直接加载 GameScene 的默认第 1 关策略保留，只有主界面的明确一次性入场请求可覆盖它。
- 手机与 iPad 竖屏适配，交互内容避开底部安全区，背景及底栏底色可以延伸至屏幕边缘。所有新按钮复用现有点击感。

## Out of scope

- 商城商品列表、价格、支付、广告、货币、体力、登录、关卡解锁与奖励；设置页除提供的三个开关之外的功能。
- 进度磁盘存档或云存档：本次只保留运行期间的当前关卡，重新启动游戏从第 1 关开始。以后需要跨启动保存时另行接入 Foundation Save。
- 第 4 关及新玩法；修改三关内容、商品消除/补货、四种道具规则或额度重置策略。
- 主界面中播放音乐、音效或调用震动，以及真机性能认证。

## Ownership and architecture

| Responsibility | Owner | Planned type/path | Notes |
| --- | --- | --- | --- |
| 页面选择、一次性进关请求 | Model | Models/Navigation/SortHomeNavigationModel.cs | 拥有 SortHomeTab 选择及待消费关卡号，不复制棋盘、额度或设置状态 |
| 选页、请求、消费、失败清理 | Command | Commands/Navigation/SortHomeNavigationCommands.cs | 验证页面/关卡；重复选页不重复发布；入场请求消费后清零 |
| 设置状态与写操作 | 现有 Model/Command | SortSettingsModel / ToggleSortSettingCommand | 主界面与暂停窗共用，禁止复制第二套偏好 |
| 当前关卡与玩法 | 现有 Model/System | SortBoardModel / SortGameSystem | Home 从已缓存的 LevelNumber 读取当前关卡，重开仍走现有 Command |
| 三页展示、输入与场景加载 | ViewController | ViewControllers/Home/SortHomeViewController.cs | 缓存依赖/页面引用，转发输入，维护一次加载和页面输入门控 |
| 导航及页面过渡展示 | 同一 ViewController 的 focused partial | ViewControllers/Home/SortHomeViewController.Transition.cs | 只存展示状态；一个动画所有者，不用多个协程争写同一 RectTransform |
| 入场与退出展示接线 | 现有 ViewController | SortGameViewController*.cs | 一次性主界面请求优先于默认入场；编辑器显式预览优先；Quit 仅在可见顶层暂停窗生效 |
| UI/场景作者化 | Editor | Editor/Home/HomeSceneBuilder.cs | 使用 Unity Editor API/MCP 创建，记录并读回精确值 |
| 通用能力 | 现有 Foundation | UI layers / safe area / popup router | 不修改 Foundation 包，不建立第二个 Architecture 或全局启动 Manager |

## State and persistence

- 当前关卡：继续由 SortBoardModel.LevelNumber 拥有；主界面显示它，不另建最高关卡或解锁进度。
- 选中页：SortHomeTab 为 Shop=0 / Home=1 / Settings=2，默认 Home，通过 SelectSortHomeTabCommand 变更。页面目标属于 Model；过渡的实际位置、比例、透明度属于 ViewController 展示缓存，不逐帧写业务模型。非法枚举拒绝，重复目标不增加修订或重播动画。
- 一次性入场请求：合法范围 1..PrototypeLevel.Count，默认无请求。主界面在确认目标场景可用后通过 Command 写入，GameScene 通过 Command 消费一次；直接重复加载 GameScene 仍从第 1 关开始。
- 状态路径：ViewController -> Command -> Model / 现有 System。按钮回调不直接写 Model。
- Save schema/version/migration：无。保留现有每次启动各 3 次道具额度、设置默认开启的策略，不新增持久化数据。
- 重复开始点击只接受一次场景切换；离场时清理旧视图及监听，不复制常驻 EventSystem、相机或 UI 根。

## UI, prefab, scene, and Inspector impact

- 新增 Assets/Scenes/HomeScene.unity，设为第一个启用的 Build Settings 场景；GameScene 继续启用，SampleScene 保持禁用。通过 Editor API 修改，不文本编辑 Unity 序列化资产。
- HomeScene 场景直接拥有主界面相机、EventSystem、三页和七层 UI 根。复用项目既有 SortUiLayers 的集中排序与门控约定；页面内容位于 Pages 的安全区子树，共用导航栏位于 Panels，排序高于页面，底色与背景铺满。三页在一个场景内缓存复用，不为切 Tab 重载场景或新建 Canvas。此范围不增加弹窗或修改现有 Resources 路径。
- 主界面 Sprite 位于 Assets/Sprites/UI/Home/，由场景中的 Image 直接引用；无新增 Resources key，无启动时预加载暂停或结算弹窗。
- 来源按文件名判定，避免用户文字中的图片序号错位：工厂 Background、NavBackgroundLeft/Right、NavRailLeft/Right 使用 Sort素材目录对应文件；Settings 来自 设置参考.png；Shop 来自 商城.png；Home 来自 HOMe_副本.png；绿色开始风格来自 主界面字体样式参考.png。
- 设置页来自 /Users/zhangshuai/Documents/Sort素材/主界面之设置界面.png（1088x1920）；商城页来自 /Users/zhangshuai/Documents/Sort素材/主界面之商城界面背景图.png（134x426）。商城背景按有意拉伸的背景处理，不把它放大解释成商品卡片；记录该小图的清晰度边界。
- 设置页的静态标题/背景使用原图；其中已烘焙的三个绿色按钮通过 Sprite 元数据分区成为独立控件，保留位置、文字及图标风格，避免在原有图标之上简单叠加导致按压时出现重影。页面图片整体等比铺满并顶部对齐，宽屏 iPad 裁切底部空背景，不能裁掉顶部 Setting 与开关；控件坐标与同一图像变换绑定，不使用另一套屏幕绝对坐标。
- 导航栏原图提供左右凸起样式，采用原 Sprite 的分区/组合构造一个可移动的凸起选中件及左右平直部分，避免完整背景交叉覆盖；黄色栏杆顶部与凸起一起移动，平直边在其两侧连续拼接，不在凸起内部留一条水平线。选中件装饰 Image 均不拦截输入，点击区独立且稳定。保留原文件及来源记录。
- 商城、Home 的白色底需要透明化；开始按钮需要移除写死文字，保留绿色底板与金边，再叠加动态文字。仅对这些必须处理的图片使用图像编辑，并记录源图、提示词、产物和肉眼检查结果；其余透明素材原像素导入。正文使用实际 Unity 字体与描边接近参考风格，不假称已获得参考图对应字体文件。
- 参考布局目标（以 720x1280 为基准）：底栏约 150 高，选中凸起约 175 高；选中入口图标大于其他两项，文字位于其下；开始按钮约 370x143、底边距导航栏上缘约 90。最终以手机/iPad 相机截图校正重叠、遮挡与点击区域，允许仅调整新主界面的尺寸间距。
- 允许改动 GameScene 控制器的启动/退出接线及已有暂停 Quit 监听；保护 GameScene 的货架、镜头、关卡 HUD、道具按钮、弹窗美术与布局。PausePopup/ResultPopup 原 GUID、加载键及首次加载复用不变。
- 不新增主界面 Prefab 路径加载器；场景卸载负责新主界面实例清理。保留 PausePopup 的 DimBackground/PopupContent、Replace 模态策略和原点击感组件。
- Inspector 确认：三页 CanvasGroup/背景、三入口的固定点击区与动画子物体、选中件、三个设置按钮/OFF 标记、开始按钮、关卡文字、层宿主与相机引用完整；场景名真实可加载；Build Settings 顺序正确；原组件引用未丢失。
- Model deletion dependency audit：无模型删除。

## Integrations and failure behavior

- 离线可用，无新 SDK、联网、同意或身份流程；不改变热更包或内容收集。
- 缺失 GameScene 或绑定不完整时，主界面保留可见，不留下入场请求/输入遮罩，不显示空白场景；输出清楚错误及可见短提示，修复后允许重试。
- 缺失 HomeScene 时，暂停 Quit 留在原暂停界面，棋盘仍暂停、额度不变，Close 和 Restart 继续可用。加载异常不能先清掉原弹窗后把用户困住。
- 主界面不会 TickSortCommand；保留的旧棋盘不计时。正常重新进关通过现有 StartSortLevelCommand 重建棋盘、计时及临时效果。
- 缺失某个目标页面或其必要绑定时拒绝切换，保留当前有效页面和选中项，输出可定位的错误及可见短提示；不能把用户切到空白内容。离场/禁用时停止过渡并解绑观察者；重新进入对齐当前目标状态，不复用半途动画的旧坐标。
- 切页过渡期间页面主体不能操作，但三个导航入口始终接收新选择；离开 Settings 后隐藏按钮的陈旧回调不得修改偏好，离开 Home 后开始按钮不得触发进关。旋转/尺寸变化时依据当前页面与最新可用宽度重算动画目标，不能仍向旧尺寸坐标移动。
- 隐藏、销毁或非顶层暂停窗的陈旧 Quit 回调无效；重复点击不会多次加载或泄漏监听。切换场景时原路由缓存和输入门控由原宿主清理。

## Performance considerations

- 三页为有限数量直接引用的 Sprite 和按钮，没有逐帧查找、纹理生成或 Model/System 服务定位；文字与开关在初始化/修订时刷新。导航仅动画期间更新已缓存的 RectTransform/CanvasGroup，静止时不运行动画循环。
- 背景维持清晰度并按资源实际大小控制导入上限，图标透明边界与显示尺寸匹配，不把文件压缩大小当显存占用。最终记录纹理尺寸与导入设置。
- 三个主菜单页随 HomeScene 生命周期复用，只在开始/退出时加载本地场景；不额外常驻 GameScene、弹窗或第二份架构。点击及切页动画使用非缩放时间，禁止切 Tab 时反复加载资源、创建页面或分配多个争用的协程。

## Acceptance criteria

- [x] 启动显示工厂主界面；底部三个入口、中央凸起、黄色上边框和开始按钮符合整体参考，无白底或 Level 18 残留。
- [x] 三入口切换到对应实际页面，商城背景正确，Setting 页面三个开关可独立操作并与暂停窗同步。
- [x] 凸起及黄色边框平滑横移，图标放大/上移及标签显示同步；Home<->Shop、Home<->Setting、Shop<->Setting 六个方向均无跳变、裂缝或残留标签。
- [x] 快速连点以最后目标为准，动画不归零、不排队；重复点击同目标不重播。只开放最终页面输入，隐藏开始/设置按钮及离场回调均不生效。
- [x] 开始按钮显示并进入同一个当前关卡；退出第 2/3 关后再入仍是第 2/3 关的初始盘面与 180 秒。
- [x] 暂停 Quit 返回主界面，重复/陈旧回调无效；已使用和添加的道具额度及三个偏好保留，Close/Restart 行为不变。
- [x] 一次性入场请求不会泄漏到后续直接 GameScene 加载；编辑器 1/2/3 关菜单继续工作。
- [x] 场景/绑定缺失时保留原界面并可恢复，无空白页面或卡死门控。
- [x] 手机/iPad 竖屏、安全区、实际指针命中和多次往返检查通过；现有游戏布局与规则受保护。

## Verification matrix

| Boundary | Evidence required | Result |
| --- | --- | --- |
| Model/Command | 页面选择合法性/重复请求、入场合法性、一次性消费、清理、无关状态未变 | 5 项 HomeNavigationTests（含资源身份持久性）；完整 EditMode 85/85 通过 |
| Main UI assets | 来源清单、透明图肉眼检查、三页 Sprite/按钮引用、凸起/边框拼接、动态关卡文字、设置分区位置 | 8 张原始 PNG 哈希一致；3 张 imagegen 产物及精确提示词有清单；Sprite 稳定 ID 与字体/引用已读回 |
| Tab animation | 六方向、快速往返/跨栏、重复目标、动画中 retarget、分辨率改变、标签/图标终态及隐藏回调门控 | 六方向、跨栏、快速 retarget、重复请求、尺寸变化、禁用恢复测试通过；83 帧普通 Play 录屏肉眼检查 |
| Shared settings | 设置页切换、进入关卡暂停窗后同步、反向修改后退出同步、各按钮实际指针命中 | 真实指针按压、独立开关、隐藏回调、设置页/暂停窗同步及额度保留均通过 |
| Navigation | Home->Game->Pause Quit->Home->Game 的 1/2/3 关往返、额度/偏好保留、重复点击拒绝 | 前三关逐一 Home->Game->Quit->Home 往返、新盘面、一次性入场及编辑器菜单通过 |
| Failure/recovery | 缺失场景、无效绑定、陈旧回调、清理后重新尝试 | 缺失场景与非目标页面均复现/恢复验证；独立审查 P2 已以红绿回归修复 |
| Existing behavior | 三关、道具、补货、编辑器选关、Close/Restart；更新旧 Quit 无操作测试为明确的新行为 | 最终完整 PlayMode 36/36 通过，涵盖原三关/补货/道具/暂停/编辑器入口 |
| UI layers/authoring | 单场景相机/EventSystem、Pages/导航 Panels 排序、安全区、非目标页输入禁用、模态路由清理、受保护资产对比 | 0..600 七层排序、Pages/Panels 输入、分隔线固定位置已读回；旧场景/Prefab SHA256 未变 |
| Compilation/conventions | Unity 无新编译错误；validate_game_foundation_project.py 零错误 | Unity 编译/普通启动无新错误；Python validator 0 warnings |
| Performance/platform | 缓存/订阅/资源检查；720x1280 与 768x1024 截图；实际 iPhone/iPad 未连接则明确未测 | 缓存与事件驱动检查完成；720x1280/768x1024 六张截图检查；实际 iPhone/iPad 未测试 |

## Definition of Done

- [x] 设计与实施计划完成审核后再修改产品代码/资产。
- [x] 所有权、场景、入口及资源路径与实现一致。
- [x] 必要的图片处理、三页动画、共享设置、动态文字、交互与失败恢复已实现并有证据。
- [x] 规范验证、Unity 编译、相关 EditMode/PlayMode 和现有必要回归通过。
- [x] 真实运行的三页手机/iPad 比例截图与切换录屏完成肉眼检查，确认点击区域与图标一致，动作符合视频可见部分。
- [x] 性能边界、未完成的设备检查和证据路径写入交付记录。
- [x] 验收完成后才标记 Verified。

## Decision log

- 2026-10-27：用户要求使用指定图片制作主界面及关卡开始入口，整体参考最后一张图；素材对应关系按文件名确定。
- 2026-10-27：用户明确选择商城、设置本次都只做入口和点击反馈，不制作完整页面。
- 2026-10-27：用户随后明确扩大并替代上一条范围：提供设置页、商城页背景及导航视频，要求三页真实切换且动效顺滑。因此上一条点击反馈范围不再适用；商城暂用提供的背景，设置实现已有三个偏好开关，不推导付费商品系统。
- 2026-10-27：参考视频 /Users/zhangshuai/Downloads/参考视频.mp4/参考视频.mp4，实际 1300x230、15fps、6.47 秒，只展示底部导航及极少页面下沿。抽取 97 帧，0.067～0.333 秒可见 Home->Shop 连续移动，其他时间可见双向与跨栏选择；选中件、图标及标签同步变化。0.25 秒导航与 0.12 秒页面淡入淡出是据此选定的实现参数，不能声称从视频验证了完整页面滑入/滑出。
- 2026-10-27：动效参考视频使用蓝色与 Shop/Home/Journey；只参考运动与交互，项目保留用户提供的紫金素材、商城/Home/设置三个入口及其对应图标。
- 2026-10-27：推荐独立 HomeScene，保留 GameScene 的关卡 HUD 与编辑器直接测试入口；将主界面叠加进 GameScene 会增加隐藏玩法计时/输入门控的责任，因此未选。
- 2026-10-27：现有工程没有磁盘进度存档；本期按当前运行中的关卡号显示和重新入场，不主动引入永久进度。此假设在设计审核时明确展示。
- 2026-10-27：该目录不是 Git 仓库，不初始化 Git 或伪称已提交；设计文档按 Foundation 规定放入 Docs/Features。

- 2026-10-27：用户在三页设计文档后明确要求开始执行；已补齐 Docs/superpowers/plans/2026-10-27-home-screen.md，连续在当前工程实现，无需重复确认。
- 2026-10-27：执行期间用户补充紫金底栏静态参考 codex-clipboard-a329505a-5d65-4e21-8a25-893012b0909b.png；校准凸起上沿与两侧金边连成一体、选中图标上移、白色紫影标签；仍使用之前指定的商城/蜂巢房屋/设置素材，静态参考不替换图标。

- 2026-10-27：用户追加商城/设置选中态静态参考；据三张状态图将选中比例校准至 1.35、选中上移 44，Shop/Home 原始透明边通过 Sprite 元数据收紧，未修改源 PNG。动态文字采用 Google Fonts 官方 Lilita One，OFL 许可随字体保存在 Assets/Art/Fonts/LilitaOne。
- 2026-10-27：主界面 Play Mode 中也应允许原有 1/2/3 关菜单直接进入选定关卡；补充该既有入口的主菜单适配与运行回归。

- 2026-10-27：普通 Play 启动检查发现新切片的 legacy SpriteMetaData 导入生成重复 Sprite GUID 和零 local file ID；新增 MenuSpriteSlicesHaveUniqueStableNonzeroIdentifiers 首先复现 NavDivider 的 ID=0。HomeSceneBuilder 通过 Unity SerializedObject API 给本功能的切片及名称映射写入稳定非零 ID 与唯一 GUID，然后重建 HomeScene；不文本改写 meta，也不改旧玩法/暂停资源。此修复需同时验证资源持久性与普通 Play 启动，不能仅依赖同会话截图。

## Final evidence — 2026-10-27

- 完整 EditMode：dd41ff9367ea452eadf496e83a32b436，85/85；完整 PlayMode：9256f48ff28743fba240fe148b2331b2，36/36。规范 0 warnings。普通 Play 进出后 Console 0 错误/警告。
- 新增 HomeScene 已设为首个启用场景，GameScene 第二，SampleScene 保持禁用；编辑器当前停在 HomeScene 的 Edit Mode。原 GameScene、PausePopup、ResultPopup 文件哈希未变，现有玩法 Models/Systems 文件未变。
- 独立审查仅发现非目标缺页时过渡读空引用，已先复现再修复并通过全套回归。后续普通启动录屏进一步发现切片 ID 问题，也已先添加失败测试再修复；完整中间结果保留，不把先前同会话截图当作冷启动验证。
- 产物目录：/Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/all-sorted-prototype/home-screen/。包含六张手机/iPad 比例 PNG、navigation.mp4、verification.json、asset-readback.json、original-art-manifest.json、imagegen-manifest.json、font-manifest.json、baseline-changes.json 和实现说明.md。
- 图片处理使用内置 imagegen：商城/Home 去白底，StartBase 去除写死 Level 18；产物实际路径为 Assets/Sprites/UI/Home/ShopIcon.png、HomeIcon.png、StartBase.png。其余八张原图字节保持。Lilita One 字体及 OFL 原文位于 Assets/Art/Fonts/LilitaOne，来源清单记录 Google Fonts 官方仓库链接与 SHA256。
- 实现为一个导航 Model、四个 Commands、一个 Home 控制器及其过渡 partial、两个轻量展示辅助组件、一个 Editor 作者化工具。现有游戏控制器只扩展入场/退出接线；PrototypeLevelMenu 支持从运行中的 Home 直接选关。无 Foundation 包变动、第二个架构、支付或存档系统。
- 动画仅活动时更新缓存的 Transform/CanvasGroup；尺寸变化检查及原图像比例检查为轻量逐帧比较。无每帧模型服务查找、纹理生成、资源重载、页面实例化或偏好扫描。真机性能、音频播放和震动未验证；偏好状态已实现，共享设置开关不会凭空增加音频或震动内容。


## Shop header addition — 2026-10-27

- 用户范围：仅增加商城页顶部栏，按 /Users/zhangshuai/Documents/欧卡参考风格/商店界面之顶部栏.png 的位置关系组织，不采用参考图写死的 Setting 文字。横向紫金背景铺满页宽，Shop 标牌居中，顶部可见边与屏幕顶部对齐，底部略低于横向金线。
- 资源：/Users/zhangshuai/Documents/欧卡风格/商店界面之顶部栏背景图.png 原样导入 Assets/Sprites/UI/Home/ShopHeader/Background.png。/Users/zhangshuai/Documents/欧卡风格/商店界面之顶部栏.png 外部棋盘格为烘焙底色，使用内置 imagegen 仅去背景，保留 Shop 及金边紫色标牌，产物存入同目录 Title.png。
- 所有权与生命周期：HomeScene 的 Pages/ShopPage/ShopHeader 及其 Title 子对象直接引用两个 Sprite；随原商城页淡入淡出、场景卸载销毁。装饰 Image 不拦截输入；不新增 Canvas、运行时控制器、Model、Command、Resources key、商品或支付流程。
- 作者化：新增 Editor/Home/ShopHeaderArtwork.cs，只通过 Unity Editor API 给现有商城页添加两张 Image 和等比布局。HomeSceneBuilder 未来显式重建时也调用它；本次不重建整场景。保留 Home、Settings、底栏分隔线和所有旧 Inspector 引用。GameScene/PausePopup/ResultPopup 不改变。
- 适配：背景保持 840:240，宽随页宽；标牌等比，按可见主体约占页宽 48% 对齐，抵消素材上部透明留白。手机与 iPad 竖屏检查金边、Shop 文字、相对位置及切页隐藏。
- 失败：缺素材或非 Edit Mode、脏场景时作者化入口拒绝执行并给出定位错误，防止覆盖未保存工作。运行时无需新增资源加载或状态路径。
- 验收完成：两图来源与 alpha/导入设置已读回；原 231 个组件序列化属性未变，仅增加两个商城装饰对象、8 个组件。720x1280 与 768x1024 商城截图已肉眼检查；相关 PlayMode 3/3 通过（da8a70db5e55458086e1ee7daa2bbf21），Unity 编译无新错误，规范验证 0 warnings；普通 Play 启动显示两个真实 Sprite，切至 Shop 后页面 alpha=1 且切换结束，Play/Stop 后 Console 0 错误/警告。真机尚未验收。
- 性能边界：新增两个静态 Sprite Image 和两个 AspectRatioFitter，不新增逐帧业务逻辑、网络、支付或持久化。图片真实导入尺寸和资源成本在最终说明记录。


### Shop header final evidence

- HomeScene 通过 Editor API 定点修改，没有运行整场景重建。原组件属性前后比较为空；原 GameScene、PausePopup 和 ResultPopup SHA256 不变。Home/Settings 的手机与 iPad 共四张截图和前次交付 PNG 字节一致；导航分隔线、原 Inspector 引用及功能保持。
- ShopHeader 背景为原文件逐字节副本，840x240；Title 为 imagegen 真 alpha 产物，原始 1448x1086，Unity 导入 1024x768。两者均 Sprite Single、RGBA32、Uncompressed、不开 Mipmap、CPU 不可读、raycastTarget=false，local file ID=21300000，独立有效 GUID。两张纹理 RGBA32 基础像素合计 3,952,128 字节（约 3.77 MiB，不代表进程总内存）。
- 原生比例：背景 3.5，Title 4/3；Title 全 PNG 宽占页面 .52575，透明区抵消 top anchor=1.17410，实际可见主体宽约 48%。两个 AspectRatioFitter 控制等比布局，继承原商城 CanvasGroup，无新增运行时脚本或 Canvas。
- 新增脚本 Assets/Scripts/Game/Editor/Home/ShopHeaderArtwork.cs；HomeSceneBuilder 仅为 Shop 分支增加调用，未来重建保留本次顶部栏。资源位于 Assets/Sprites/UI/Home/ShopHeader/，场景直接引用，无 Resources key。
- 证据目录 /Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/all-sorted-prototype/shop-header/：手机/iPad/普通启动截图、component-comparison.json、asset-readback.json、art-manifest.json、verification.json、final-hashes.json 与实现说明.md。
- 当前停在已保存的 HomeScene Edit Mode；运行后点击左侧商城即可查看。iPhone/iPad 真机和设备性能没有执行。


## Home/Shop icon replacement — 2026-10-27

- 用户范围：仅将底部导航栏 HOME 图标换为 /Users/zhangshuai/Documents/Sort素材/主界面之HOME图标.png（粉顶房屋），SHOP 图标换为 /Users/zhangshuai/Documents/Sort素材/主界面之商城图标.png（橙色购物车）。其他内容不变。
- 两张输入均 RGB 白底，使用内置 imagegen 仅去外部白底/地面阴影，保留完整图案和内部奶油白色表面。最终覆盖 Assets/Sprites/UI/Home/HomeIcon.png、ShopIcon.png，旧文件及 meta 在工作区备份。通过 Unity TextureImporter API 更新这两个 Sprite 的有效区域，保留原 GUID、Sprite 名与稳定 local file ID。
- 只更新 HomeSceneBuilder 对这两张图的区域坐标，以便未来显式重建使用新素材；本次不重建或重存场景，不改变 Icon RectTransform、尺寸、锚点、位置、Preserve Aspect、颜色、动画参数、点击区、标签、分隔线、设置图标或三页功能。
- 所有权/生命周期仍为 HomeScene Tab_Shop/Tab_Home 下原 Image 直接引用、原导航展示控制器管理。无新增对象、脚本、组件、Resources key、模型、命令、状态或资源加载策略。
- 验收完成：两图真 alpha 与完整内容已检查；原 Sprite GUID/local ID 保持，原 239 个组件序列化属性无变化；HomeScene/GameScene/PausePopup/ResultPopup 及其他受保护文件哈希一致；三页手机/iPad 比例与选中/未选中态检查通过；普通 Play 图标引用有效、默认 Home、控制台无错误/警告；现有相关 EditMode 1/1、PlayMode 3/3 和规范 0 warnings。真实设备未接入。


### Home/Shop icon replacement final evidence

- 仅更改 HomeIcon.png、ShopIcon.png、两者的 Unity importer 元数据、HomeSceneBuilder 中这两个区域的坐标及本说明；未保存或重建任何场景，未改变运行时脚本。原 239 个组件属性、Sprite 引用身份、导航尺寸/位置/动效/标签/点击区/分隔线、设置图标、三页内容及关卡功能保持。
- 图标区域通过 Unity TextureImporter API 更新；继续由原稳定身份方法写入相同 local ID。Shop GUID=5f45b4970462947cdb7dafd55bfdd765，local ID=883261686043671753；Home GUID=fb43c982af91e4d0297d62e9bed253be，local ID=4343239025328530780。不文本修改 meta/scene。
- 两张 imagegen 产物均 1254x1254 RGBA，Unity 导入仍各 1024x1024，关闭 Mipmap、不压缩，保留 Preserve Aspect=true。Sprite 区域仅去外围透明留白，不改变 PNG 像素、现有 RectTransform 或动画尺寸。纹理尺寸与此前相同，无新增运行时实例、组件或资源加载逻辑。
- 测试：EditMode be2d1e627d384183934e5b8f9e1cb263，1/1；PlayMode 9746eccf1c10472eb628c00f03f34330，3/3，涵盖初始页、六方向/快速切换及设置交互门控。Python 规范验证 0 warnings。普通 Play/Stop 后 Console 0 错误/警告。
- 证据目录 /Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/all-sorted-prototype/home-shop-icons/，包含六张三页手机/iPad PNG、普通启动图、源图/产物/提示词清单、GUID/组件/文件前后比较、像素差异记录、verification.json 与实现说明.md。与替换前相比，三页截图差异仅位于底部 HOME/SHOP 图标附近，右侧设置入口和上方页面像素一致。
- 当前 HomeScene 已保存且处于 Edit Mode；真实 iPhone/iPad 及设备性能尚未测试。

## 后续资源栏范围补充 — 2026-10-27

顶部金币、体力及其本地保存已由 [home-wallet](home-wallet.md) 接管，覆盖本文原先对应的排除项。关卡进度保存仍不在本次范围内。现有导航、图标、分隔线、设置页与Shop顶部保持。

## 设置按钮素材替换 — 2026-10-28

用户要求仅替换Home设置页的Music/Sound/Haptic三组按钮，其他不变。源图`暂停界面之设置按钮嵌入.png`与已有`Assets/Sprites/UI/PauseSettings/SettingsRow.png`逐字节相同。使用内置imagegen分别提取三个透明底按钮，排除源图的实心紫底；以原有按钮区域像素作为局部底图保留透明边角处的原背景。最终仅三张按钮Sprite与三张对应局部底图Image的sprite/color有变化。原Rect、Image拟合/类型、按钮回调、开关状态、关闭斜线、标题标签、背景和导航保持。验证已完成。

- 素材落点：Assets/Sprites/UI/Home/SettingsButtons/{Music,Sound,Haptic}.png；对应提示词、原图和生成路径见 `/Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/home-settings-buttons/art-manifest.json`（内置imagegen）。未修改暂停界面源图或其Importer。
- 新Editor/Home/HomeSettingsButtonArtwork.cs定点导入并保存三组Image；HomeSceneBuilder仅对应按钮/底图生成路径同步，未运行全场景重建。无运行时代码、状态或事件绑定变更。废弃的临时网格Sprite资产已通过Unity AssetDatabase移除。
- 原生比较：HomeScene对象数量不变，仅六个Image组件变化；所有Rect、按钮回调、状态标记、标签、页面背景、导航、相机及其他组件与修改前一致。证据 native-comparison.json。
- 现有真实指针开关/隐藏回调/切页共享状态检查通过（1/1，job339c0b06a34748418fed10ef1c179d9d）。编译无错，规范校验0警告；手机与iPad截图为 settings-phone.png /settings-ipad.png。截图中Sound关闭为测试验证状态，未重置玩家原开关设置。
- 三个512上限静态RGBA32 Sprite，无mipmap/CPU可读副本，约3MiB基础纹理像素上限；无新增Canvas、运行时组件或更新循环。HomeScene已保存，Edit Mode，无额外Inspector绑定；真机未测。

## 金币礼包商城补充 — 2026-10-28

六档金币模拟购买由 [shop-coin-packs](shop-coin-packs.md) 定义：三列两行、对应原图与价格、点击无收费发放并保存。原Shop标题、资源栏、导航及设置页保留。商城商品/货币的旧排除项仅在这次明确的模拟金币礼包范围内被覆盖，真实支付仍排除。

## 状态视觉修正 — 2026-10-28

修正Home设置页三个关闭遮罩：原154×154通用方块小于200×210按钮；改为与父按钮同Rect、同Sprite轮廓，黑色40%透明度，斜线尺寸和开关逻辑不变。暂停弹窗不变。 验证完成：原生回读确认旧遮罩154×154、按钮200×210；修正后遮罩同父按钮尺寸和Sprite，禁用射线阻挡。新增两项回归修正前均失败（f49d189acf524d1eb8bc7ef500da6ee7），修正后相关HomeNavigation/Wallet PlayMode15/15通过（80139a7216e749b3ae4b2b1bb1cc8dac），覆盖补满、UTC恢复、Home/Shop共享、场景重载和真实点击。手机/平板关闭状态及满/未满体力截图位于 `/Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/home-state-visuals/`。Foundation规范校验0警告，编译无错；HomeScene仅三个遮罩Image及RectTransform变化。无新组件/纹理/存储格式；运行时仅既有刷新入口调整角标显隐。真机未测。

## 关前道具补充 — 2026-10-28

[prelevel-boosters](prelevel-boosters.md) implements Home Start → selected-booster popup → PLAY. Wallet schema3 preserves original resources and four booster counts while adding independent EntryCharges[2] default3. Closing does not spend; red numeric badge when stocked, green+at0; selected effects apply once per Home entry. Existing retry/next do not reuse effects.

## Green start button page visibility (2026-10-29)
Status: Verified. Reference /Users/zhangshuai/Movies/2026-10-29 16-24-24.mov, sampled at60fps. At2.633s Shop remains visible, at2.650s Home button is already at its fixed final bounds; no visible translation/scaling in subsequent17frames. Implement quick visibility: immediately hide StartButton when a page transition starts; reveal it at its authored position once returning Home background has completed its existing fade. Snap/reenable shows only selected Home. Keep existing page/selector timings, stable transforms, press feedback, current-level label, popup85%-103%-100%, entry and all purchase behavior. No new Sprite or scene bindings. Same page CanvasGroup remains input owner; hidden stale callbacks remain rejected by CanUsePage. Runtime callbacks only toggle the cached button object on transitions and never recapture its scale. Verify six directions, rapid retarget, duplicate target, Home start gate, disabled/reenable and pointer press recovery; preserve existing unsaved Editor scene contents.

Verification:46 native Play Mode assertions passed across all six page directions, immediate hiding, hidden stale start callbacks, return visibility, stable authored transform (0,335)/scale1, rapid retarget, duplicate selection, unscaled progress and disable/reenable. Results: /Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/home-button-transition/checks.json. Existing page input gates and press-feedback component remain unchanged. No additional tests for this reversible UI adjustment. Python convention validator passes with0 warnings; Unity compiled successfully. Only SortHomeViewController.Transition.cs and this spec changed; no scene, prefab, image or save schema writes. User confirmed quick visibility and requested no final preview. Pre-existing unsaved HomeScene edits retained in Editor. A capture helper used after native checks caused the temporary running instance to lose its nonserialized navigation cache; that verification session was stopped; no capture helper is part of production or the change.

Fresh Play startup rechecked after stopping the temporary capture session: navigation cache initialized correctly, Shop/Settings/Home selection, immediate hiding, stale input rejection and final visibility passed. Fresh-session console returned0 error entries. Verification Play Mode stopped; original unsaved Editor scene retained.
