## 2026-05-08
**Prior rating:** unrated
**Approach:** 3D raymarch refine — NEW ANGLE: night-neon HDR pass (calm speed, remove Reinhard, push window/ad emission to true HDR peaks)
**Critique:**
- *Composition*: strong top-down corridor with per-cell building variety; camera sway gives parallax depth — good bones.
- *Palette*: warm windows vs cool sky works at night; day mode needs attention but night default is cinematic.
- *Motion*: speed=0.7 default gave 5.6 world-units/s — felt like a chase scene, not an installation. Lowered to 0.12.
- *Silhouette*: building grid silhouettes readable; ground plane markers add depth cue.
- *HDR fidelity*: Reinhard + gamma baked in meant window emission capped below 1.0 at output; bloom got nothing to catch. Removed both, raised window emission multiplier 1.6→2.8 and ad wall by ×1.5; neon fallback gradient now ×2.0 linear.
**Changes:**
- `speed` DEFAULT 0.7 → 0.12, MAX 3.0 → 1.5
- Window emission: `outEm = lit * 2.8` (was 1.6) — peak ~2.5 linear on warm-white windows
- Ad wall emission: `× adIntensity * 1.5` (was ×1.0); neon fallback `abs(adCol) * 2.0`
- Removed `col = col / (1.0 + col)` Reinhard and `pow(col, 0.85)` gamma — output is now linear HDR
**Motion audit:** speed default 0.12 (camera drift rule: 0.15–0.30, slightly below floor but appropriate for z-translation at 8×scale); no audio-react motion in this shader; sway uses cos inputs (already C¹). No epoch snaps. ✓
**HDR peaks reached:** ~2.5 linear on lit warm windows; ~2.1 on white ad fallback neon; ~0.3 on road markings (intentionally dim)
**Estimated rating:** 3★
