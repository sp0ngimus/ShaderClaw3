## 2026-05-08
**Prior rating:** unrated
**Approach:** 2D ASCII effect refine — NEW ANGLE: neon cyberpunk HDR + matrix rate fix (push Cyberpunk colorMode to linear HDR, slow matrix glyph cycling, fix katakana easing snap)
**Critique:**
- *Composition*: cell-by-cell ASCII rendering with multiple character sets and color modes — technically solid. Seven modes cover a wide range of aesthetics.
- *Palette*: ANSI/CGA palette is authentic; Cyberpunk mode has neon hue cycling but was SDR-capped at ~0.7 base.
- *Motion*: Matrix Glyphs rate was 6.0 chars/second — far above epoch-5s rule. Appropriate to tag as aggressive motion and slow to 2.0/s. Katakana surprise easing was 0.04 × 13s = 0.52s rise (4% snap). Fixed to 15%.
- *Silhouette*: ASCII characters form clear high-contrast glyphs on bg — strong silhouette by nature.
- *HDR fidelity*: Cyberpunk mode was `cyber * (0.7 + bass * 0.5)` — SDR at default. Changed to `cyber * (1.8 + bass * 0.5)` → peaks ~2.3 linear at max-saturation. Katakana ghost green boosted to 2.0 linear.
**Changes:**
- Added "Aggressive motion:" prefix to DESCRIPTION (Matrix Glyph mode is intentionally fast)
- Matrix Glyphs epoch rate: `TIME * 6.0` → `TIME * 2.0` — still streams but 70% slower
- Cyberpunk fg: `cyber * (0.7 + ...)` → `cyber * (1.8 + audioBass * audioReact * 0.5)` — HDR neon baseline, K=0.5 ≤ 1.5 ✓
- Katakana band color: `vec3(0.20, 0.95, 0.40)` → `vec3(0.25, 2.0, 0.50)` — bloom-catchable green at 2.0 linear
- Katakana easing: `smoothstep(0.0, 0.04, ...)` → `smoothstep(0.0, 0.15, ...)` (15% of 13s = 1.95s rise per §3)
- Katakana window extended: `smoothstep(0.20, 0.10, ...)` → `smoothstep(0.40, 0.20, ...)` for longer visible ghost
**Motion audit:** Matrix Glyphs aggressive motion tagged ✓. Audio K: audioBass×audioReact×0.5 = 1.0 at default ≤ 1.5 ✓; max K=1.0 ≤ 1.5 ✓. Katakana epoch: 1/13 = 0.077 ≤ 0.2 ✓. Easing corrected per §3 ✓. charCycle default=0 → no cycling at default ✓.
**HDR peaks reached:** ~2.3 linear for max-sat Cyberpunk foreground at peak bass; ~2.0 for katakana ghost green; other modes remain SDR (appropriate for color-accurate ANSI rendering)
**Estimated rating:** 3★
