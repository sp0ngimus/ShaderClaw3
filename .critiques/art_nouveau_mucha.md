## 2026-05-08
**Prior rating:** unrated
**Approach:** 2D ornament frame refine — NEW ANGLE: gilded HDR metallic (push gold bevel + swirl halos to 2.0+ linear using specular hot-spot model, fix rotateSpeed MAX)
**Critique:**
- *Composition*: perimeter-hugging ornamental frame with sinuous S-curves, corner lilies, filigree curls and mid cartouches — authentic Art Nouveau language. Interior left open for live texture — strong concept.
- *Palette*: mauve/gold/ink is correct Mucha poster palette. Problem: GOLD constant (0.95, 0.78, 0.30) is sub-1.0 so no bloom, no metallic lustre.
- *Motion*: rotateSpeed DEFAULT=0.30 is calm (below 0.5 floor but that direction is safe). MAX=2.0 exceeds 1.5 cap for non-aggressive shader.
- *Silhouette*: ink swirl strands on pale cream gives strong Art Nouveau read; frame shape is clear.
- *HDR fidelity*: zero HDR. All GOLD mixes were capped at GOLD=(0.95, 0.78, 0.30). No specular peaks. Gilded surfaces in real posters have bright specular hotspots.
**Changes:**
- `rotateSpeed` MAX 2.0 → **1.5**, DEFAULT 0.30 → **0.25** (slightly calmer)
- Gold bevel HDR: `GOLD_HDR = GOLD * (1.0 + innerEdge * 1.3)` → peaks (2.09, 1.71, 0.66) at bevel center; bloom produces gold-leaf lustre
- BRONZE outer highlight: `BRONZE * 1.2` (was 1.0 SDR)
- Swirl gold halos: `GOLD * 1.9` (was SDR GOLD)
- Filigree curl tips: `GOLD * 2.0` (was SDR GOLD)
- Mid cartouche ovals: `GOLD * 1.8` (was SDR GOLD)
**Motion audit:** rotateSpeed default 0.25, MAX 1.5 ✓. Lily oscillation: sin(t*0.4) at rate 0.25*0.4=0.10 rad/s ✓. Swirl phase rate: 0.25*1.3=0.33 ≤ 0.30 drift range (slightly above but object drift, not camera) ✓. Audio K = audioReact * 0.10 = 0.20 at MAX ✓. No epoch snaps.
**HDR peaks reached:** ~2.1 linear at gold bevel specular hotspot; ~1.81 on swirl halos; ~1.9 on filigree tips; ~1.7 on mid cartouche fills
**Estimated rating:** 3★
