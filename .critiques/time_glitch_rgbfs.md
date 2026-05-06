## 2026-05-06 (v4)
**Prior rating:** 0.0★
**Approach:** 3D perspective — NEW ANGLE: Tron Cyber Floor Grid (prior 1: 3D vertical RGB data planes; prior 2: 2D RGB prism rings)
**Critique:**
1. Reference fidelity: Tron light-cycle racing circuit is a completely different reference — game/film vs signal interference metaphor.
2. Compositional craft: Floor perspective receding to horizon creates dramatic depth not possible in planes or rings; foreshortening maximizes dimensionality.
3. Technical execution: Ray-plane intersection (exact, no march needed); fwidth() grid line AA; per-row hash glitch displacement.
4. Liveness: Camera drives forward TIME*speed; glitch rows flash at glitchRate; audio boosts grid brightness.
5. Differentiation: Vertical planes→horizontal floor (90° rotation); 2D rings→3D perspective vanishing point; RGB separated→neon green+crimson glitch+gold intersections.
**Changes:**
- Full rewrite from Signal Interference/RGB rings to Tron Cyber Floor Grid
- Ray-plane intersection for exact floor rendering (faster than march)
- Per-row hash glitch: entire row displaced horizontally when glitchSeed > 0.85
- Grid palette: neon green 2.5 primary, crimson 2.25 glitch, gold 2.0 intersection
- fwidth() AA on all grid edges
- Forward camera motion + distance fog for atmospheric depth
**HDR peaks reached:** grid lines 2.5; gold intersections 2.0 doubled → 4.0 (corner = lineX*lineY*2); glitch rows 2.25
**Estimated rating:** 4.0★
