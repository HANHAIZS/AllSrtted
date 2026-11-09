# UI authoring ownership

Prefab conventions own structural schema; the consuming project owns presentation and interaction policy. Run normalization only for the chosen asset and preview its structural changes first. Layer mounting follows [UiLayers.md](UiLayers.md); paths and resource lifetime follow [ResourceLoading.md](ResourceLoading.md).

## Preserve authored controls

- Existing button Image fitting, including `preserveAspect` and `Image.Type`, is Inspector-owned. Both enabled and disabled Preserve Aspect are valid. Builders, repair tools, normalization and runtime binding must not replace these choices unless the requested change explicitly owns them. New copies retain their template's values.
- Preserve text, sprites, transforms, colors, transitions, interactable flags, callbacks, serialized references and prefab-instance overrides outside the requested boundary. Runtime presentation may update explicitly bound dynamic/localized text; it must not overwrite static authored captions merely to initialize a view.
- Animation endpoints come from the project's declared authoring source, captured before animation. Do not silently replace scene overrides with stale code defaults or recapture endpoints from an in-flight animation.
- Regeneration creates missing assets or changes explicitly selected fields. It does not rebuild existing project UI wholesale. Existing UI convention profiles retain their asset identity and authored values during Setup repair.
- Button disabled tint, variable-price text states, action wording, tab geometry, camera, resolution and gameplay pause policy are project choices. Foundation does not impose them as global defaults.

## Validation

Use Unity Editor APIs and read back exact properties. For normalization changes, validate structural schema and compare protected control properties before/after, including both Preserve Aspect states. A valid structure is not permission to rewrite unrelated visual fields. For runtime binding changes, check reopening, listeners, visibility and input eligibility using the project's focused tests. Preserve enabled/disabled input behavior independently of visual style.
