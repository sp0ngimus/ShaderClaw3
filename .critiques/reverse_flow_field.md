## 2026-05-05
**Prior rating:** 0.0★
**Approach:** 2D refine (HDR palette replacement)
**Critique:**
1. Reference fidelity: Flow field algorithm (cellular FBM backward trace) is well-executed and matches "wind-blown grass tips" reference.
2. Compositional craft: Grass gradient is desaturated (black→forest green→gray→white) — indistinct at small sizes.
3. Technical execution: Multi-pass ISF correctly implemented; Bezier weight curve is sophisticated.
4. Liveness: TIME-driven via flow offset, but temporal feels slow.
5. Differentiation: Interesting LIC-style approach; killed by the gray/white palette giving near-zero saturation score.
**Changes:**
- Replaced grass gradient with volcanic magma palette: black→deep crimson→orange→gold→white-hot HDR
- Seed dot colors changed from random→3 fire hues (deep ember, orange, gold)
- intensity default: 1.0→2.5 (HDR boost)
- dotDensity default: 0.1→0.12
- audioMod input added, modulates flow speed and direction field
- HDR peak: magma top ramp → 3.0× white-hot on high-intensity seeds
**HDR peaks reached:** white-hot seeds 3.0, gold 2.0, orange 1.3
**Estimated rating:** 3.5★

## 2026-05-06 (v5)
**Prior rating:** 0.0★
**Approach:** 3D volumetric — NEW ANGLE: Plasma Storm (prior 2026-05-06 was 2D volcanic magma flow field — 2D LIC, HOT fire palette)
**Critique:**
1. Reference fidelity: Electric tornado storm is the palette-opposite of volcanic magma — cold electric vs hot fire, storm vs eruption.
2. Compositional craft: Ground-level camera looking up at tornado creates dramatic scale; narrowing vortex converges to top.
3. Technical execution: Domain-warped FBM density field in cylindrical vortex shape; front-to-back transmittance accumulation; 64-step march.
4. Liveness: Vortex rotates TIME-driven; spiral domain warp animates; audio boosts density.
5. Differentiation: 2D→3D axis change; HOT→COLD palette inversion; 2D flow field LIC→3D volumetric density; flat graphic→atmospheric depth.
**Changes:**
- Full rewrite from 2D volcanic magma flow field to 3D volumetric plasma storm tornado
- Domain-warped FBM density field with cylindrical vortex falloff
- Front-to-back transmittance (physically-based volumetric scatter)
- Palette: void black → purple 2.0 → electric blue 2.0 → arc cyan 2.5 → lightning white 3.0+
- Ground-level camera orbiting slowly around tornado base
- Audio modulates density → storm intensity reacts to music
**HDR peaks reached:** lightning white 3.0-4.0 (hdrPeak*audio); blue core 2.0; transmittance gating prevents oversaturation
**Estimated rating:** 4.5★
