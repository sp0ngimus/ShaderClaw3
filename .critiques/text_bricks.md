## 2026-05-06 (v5)
**Prior rating:** 0.0★
**Approach:** 2D procedural 3D-perspective — NEW ANGLE: Cyberpunk City Skyline (prior 1: neon brick wall 2D; prior 2: lava cracks 2D)
**Critique:**
1. Reference fidelity: Night city skyline is a completely different reference from brick walls or lava — urban architecture vs texture patterns.
2. Compositional craft: Perspective floor grid + building silhouettes + window glow creates genuine spatial depth not achievable with flat textures.
3. Technical execution: Perspective street grid from floor plane; building windows as parameter grid; rooftop neon signs at alternating hues.
4. Liveness: Street grid drifts forward TIME-driven; windows flicker at slow rate; audio modulates text glow.
5. Differentiation: 2D flat bg→perspective city depth; brick/lava texture→architectural silhouette; neon bricks/fire orange→void+gold windows+magenta neon.
**Changes:**
- Replaced bg function with nightCityBg() — perspective city skyline at night
- Perspective floor grid: magenta neon street lines [0.8, 0.0, 1.6] * 1.5
- Building silhouettes: void black [0.01, 0.01, 0.02] on void sky
- Windows: warm gold [2.0, 1.2, 0.0] HDR emission
- Rooftop neon signs: alternating cyan [0.0, 1.5, 2.0] and magenta [2.0, 0.0, 1.0]
- textColor: hot magenta [1.0, 0.0, 0.8] * 2.2 HDR
- transparentBg default: false
**HDR peaks reached:** windows 2.0 (gold); neon signs 2.0 (cyan/magenta); street grid 1.5 (magenta)
**Estimated rating:** 4.0★
