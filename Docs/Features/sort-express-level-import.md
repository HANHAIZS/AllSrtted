---
id: sort-express-level-import
title: Original-number Sort Express source level import
status: Verified
owner: Project
created: 2026-10-30
updated: 2026-10-30
---

## Goal

Import all 2200 supplied Sort Express iOS2165 level configurations byte-exact, preserve original level numbers and unknown fields, expose a searchable project-local catalog and source layout preview, explicitly mark unsupported runtime mechanics. User selected fidelity and original numbering over simplified/rebaked playable stages.

## User-visible behavior

Editor menu All Sorted/Levels opens imported catalog or reimports a selected configs/levels folder. Every original ID1–2200 is accessible; raw JSON and SHA256 trace its source. Fixed/mixed/generated labels describe encoded structure, not proven runtime behavior. Layout preview shows source coordinates, selected layer and cell states without claiming gameplay equivalence. All source levels remain pending runtime validation; source files do not silently replace current gameplay with approximations.

## Out of scope

Automatic activation of unverified source stages, guessing original generator algorithms or special mechanics, importing reference art, rewriting existing three-level gameplay or saves. This delivery is source archive/catalog integration, not runtime stage support.

## Ownership and architecture

Editor/Levels owns stateless importer, strict parsing/validation and read-only EditorWindow. No model state mutation, command, second architecture, runtime dependency or shared Foundation edits. Full gameplay reconstruction is a separate compatibility boundary: generator algorithm, layer semantics, non-three widths, special cells and flags cannot be inferred from JSON alone. Import must not fabricate them.

## Assets and lifecycle

Raw design sources and SourceManifest.json: Assets/Art/DesignImports/SortExpress/Levels. Source files unchanged from /Users/zhangshuai/Documents/sort-express-2165/configs/levels. Only Editor AssetDatabase input; not Resources or startup-loaded. Catalog loads manifest on open and one source on selection; window-local cache disposed when closed. No scene/prefab/Inspector modifications, no save schema/economy/SDK/network changes.

## Failures and recovery

Validate all files before destination writes. Reject malformed/duplicate-key/invalid UTF8 JSON, missing/duplicate numbering, wrong shape, conflicting existing archive. Reimport identical content is idempotent; never delete or overwrite different prior data. I/O interruption may leave a partial set of byte-exact files; same-source retry completes missing files. Missing manifest/source shows recoverable error and permits reimport; no gameplay state changes.

## Performance

One-time parse/hash2200 files, reused source byte arrays during validation; source archive not included in player through Resources/StreamingAssets. Catalog reads only selected source. No per-frame runtime scans, components, textures or allocations. Measure archive bytes and import time; device profile irrelevant to Editor-only input.

## Acceptance criteria

- [x] Exactly2200 original-numbered raw JSON files imported, all SHA256 equal source.
- [x] Manifest counts7 fixed,44 mixed,2149 generated; all have explicit runtime-support status.
- [x] Catalog can inspect1,10,40,100,2200, show original positions/layers and unmodified raw content.
- [x] Invalid/conflicting import rejected; deterministic repeat import unchanged.
- [x] Unity compile/tests and convention validator pass; existing native scenes/prefabs unchanged.

## Verification matrix

| Boundary | Evidence | Result |
|---|---|---|
| Import | all hashes, numbering, classifier, repeated import | Passed |
| Failures |11 focused NUnit cases | Passed |
| Catalog | first/generated/special/last IDs readback | Passed |
| Protection | native file hashes before/after, compile, validator | Passed |

## Definition of Done

Source import and catalog verified, actual runnable support stated separately and never implied by data presence. No claim2200 playable levels until runtime compatibility has independent proof.

## Verified source-import delivery

- Actual Unity import2200 files,7 fixed/44 mixed/2149 generated. First import3.04s; repeat10.41s under Editor, no conflicting content. Raw bytes19,021,347; independent SHA256 comparison2200/2200 matches, IDs1–2200 complete. This is not runtime memory or device benchmark.
- EditMode11/11 passed, job11eaf7a698d042d5a929d763265a5c52, covering byte/deterministic repeat and malformed/UTF8/missing/duplicate/conflicting source failures.
- Catalog readback1/10/40/100/2200 loaded without errors; complex level2200 now scrolls1074×656 canvas, review confirms0 overlapping shelf-card pairs. Opens at original1 via All Sorted/Levels/Sort Express 配置目录.
- Native Scenes and Resources/Prefabs SHA256 unchanged; no runtime or player-save changes. Compilation no errors, Console0 errors, Foundation validator0 warnings. Saved HomeScene remains Edit Mode.
- Changed artifacts: Editor/Levels/SortExpressLevelImporter.cs, SortExpressLevelCatalogWindow.cs, Editor/Tests/SortExpressLevelImporterTests.cs, raw archive and SourceManifest.json. Evidence `/Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/level-import/import-verification.json`.
- Independent review confirmed all source bytes/hashes and no false support labels; its catalog overlap finding was fixed and rechecked. No Inspector work required.
- Gameplay integration remains unimplemented: the existing3-level mainline is still active. Original generator, unknown item types5/7, variable-width/special mechanics and original rule semantics need separate compatibility implementation and verification before enabling source levels. The user's fidelity preference is retained rather than silently substituting simplified gameplay.

## Subsequent runtime integration (2026-10-30)
The archive/catalog verification above remains byte-exact. Runtime support for source1–5 is now implemented separately in `sort-express-first-five-runtime.md`:1–3 preserve fixed layouts;4/5 use user-approved compatible deterministic generation. The original archive/SourceManifest continues to record the import-time unsupported status rather than changing source metadata.6–2200 remain archive-only. Production mainline uses the5-stage repository; old3-stage layouts/solutions remain as legacy regression content. Read the newer spec for actual verification status and limits.
