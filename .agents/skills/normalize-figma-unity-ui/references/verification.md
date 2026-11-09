# UI Verification

## Structural

- File name equals root name and satisfies the kind suffix.
- Required direct children exist exactly once.
- Unknown direct children are intentionally placed.
- No duplicate sibling names or generated placeholder names remain in production structure.
- Dim and Safe Area settings match the active profile.

## References

- ViewController serialized fields resolve.
- Button callbacks and event subscriptions are not duplicated.
- Prefab variants still inherit from the intended base.
- AnimationClip paths resolve after every rename or move.
- Figma source and node IDs remain unchanged.

## Visual/runtime

- Capture representative target resolutions and both orientations if supported.
- Check notches, home indicators, long localization, dynamic content and text overflow.
- Open, close, reopen, replace and queue popup flows.
- Verify dim raycast blocks underlying input and the intended controls remain interactive.
- Inspect layout rebuilds, per-frame allocations, unnecessary canvases and repeated component lookup.

Record whether each check was editor hierarchy, EditMode, Play Mode, or screenshot evidence.
