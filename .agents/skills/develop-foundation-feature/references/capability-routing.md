# Capability Routing

| Need | Foundation contract | Project responsibility |
| --- | --- | --- |
| Local save | `ISaveStore`, `ISaveSerializer`, `VersionedSaveService<T>` | Define save data, version and migrations |
| Content update | `IHotUpdateService`, `IContentUpdateBackend`; optional `YooAssetContentUpdateBackend` | Configure package/CDN/tags/encryption and own UX or rollout policy |
| Vendor SDK | `ISdkHost`, `ISdkAdapter` | Wrap vendors, supply consent/environment, enforce prerequisite readiness and identity synchronization; host ordering alone is not a dependency gate |
| Popup flow | `UiPopupRouter`, `IUiPopupView` | Create prefab/controller and send business Commands |
| Screen flow | `UiScreenNavigator`, `IUiScreenView` | Own route names, page state and transitions |
| Figma UI import | UI normalizer/validator; download/sync tools are external | Independently integrate the design tool; choose source frame, semantic names, production prefab and bindings |
| Time/logging | `IFoundationClock`, `IFoundationLogger` | Override only for platform or test needs |

Do not route gameplay, levels, rank, economy, store, purchasing, rewards, experiments, or product analytics schemas into Foundation. A vendor adapter may translate project events, but Foundation owns only its lifecycle and failure isolation.
