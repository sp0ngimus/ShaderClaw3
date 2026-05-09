## 2026-05-09
**Prior rating:** 0★
**Approach:** 2D standalone — NEW ANGLE: Wave Interference Generator (prior 2026-05-05 was 3D RGB data planes, 2026-05-06 was 2D prism rings — both never committed)
**Critique:**
1. Composition: concentric interference rings from drifting sources fill whole canvas — environmental vs prior geometric primitives
2. Palette: signal red / data green / electric blue — additive overlap creates yellow, magenta, cyan, white at interference peaks
3. Motion: slow source drift (0.20 default, Lissajous paths); wave propagation independent; all calm
4. Silhouette: interference fringe pattern has no single focal element — whole-canvas composition
5. HDR: constructive interference peaks at hdrPeak × N sources → 2.5–8.0+ at dense overlaps; additive RGB → white HDR
**Changes:**
- Full rewrite from VIDVOX 9-pass persistent frame-buffer to single-pass wave interference
- N colored sources (R/G/B), cosine wave from each, additive accumulation
- Source positions drift via slow Lissajous (driftSpeed 0.20 default)
- Audio: freqMod K=0.8, speedMod K=0.5 (both ≤1.5)
- fwidth() AA on interference fringe luminance gradient
**Motion audit:** driftSpeed default 0.20 (within 0.15–0.30 calm range); waveSpeed 0.6 (within 0.5–1.5 pulse range); audio K≤0.8
**HDR peaks reached:** single-source peak = hdrPeak (2.5); 3-source constructive = 7.5; typical 2.5–5.0 at interference zones
**Estimated rating:** 4.0★
