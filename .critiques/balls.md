## 2026-05-08
**Prior rating:** unrated
**Approach:** 3D multi-pass refine — NEW ANGLE: calm orbit + bloom HDR amplified (fix rotSpeed to soothing default, cap Heart Pump positional K, push bloom-sphere base to 2.0+ linear so multi-pass bloom has real HDR peaks)
**Critique:**
- *Composition*: 25 metallic spheres orbiting with selective bloom using 4-pass pipeline — strong visual concept; multiple movement modes give variety.
- *Palette*: accentColor user-defined (white default); dark bgColor gives contrast; bloom provides the HDR glow. Palette concept sound.
- *Motion*: rotSpeed DEFAULT=0.3 exceeds camera orbit calm floor (0.05-0.10). MAX=2.0 exceeds cap of 1.0 for orbit. Heart Pump mode: positional K at MAX audioDrive=5.0 was 5.0×0.30=1.5 which at max hash factor gave pump≈3.1× radius (positional rule K≤0.6 → max should be 0.12).
- *Silhouette*: sphere SDF gives clean round silhouettes; bloom ring gives aura separation.
- *HDR fidelity*: bloom spheres were `col *= 1.0 + audioLevel * audioDrive * 0.5` — at default settings barely 1.5×. Changed to HDR base `col *= 2.0 + ...` so bloom-tagged spheres output 2.0+ linear for the multi-pass bloom to catch. Audio K also corrected.
**Changes:**
- `rotSpeed` DEFAULT 0.3 → **0.07**, MAX 2.0 → **1.0** (camera orbit rule: default 0.05-0.10, MAX 1.0)
- Heart Pump positional factor: `beat * 0.30` → `beat * 0.12` — K_pos = 5.0 × 0.12 = 0.60 ≤ 0.6 ✓
- Bloom sphere HDR: `col *= 1.0 + level * drive * 0.5` → `col *= 2.0 + level * drive * 0.3` — base 2.0× linear, K = 0.3 × 5.0 = 1.5 ≤ cap ✓
**Motion audit:** rotSpeed default 0.07 ✓, MAX 1.0 ✓. audioDrive pulse K = 0.3 at default ≤ 1.5 ✓. Heart Pump positional K = 0.12 × audioDrive_default = 0.12 ≤ 0.6 ✓. No epoch snaps. Orbit uses cos/sin inputs (C¹) ✓.
**HDR peaks reached:** ~2.3 linear on bloom-tagged spheres at default audio; ~3.5 at MAX audioDrive + level; non-bloom spheres remain SDR (~0.8-1.2)
**Estimated rating:** 3★
