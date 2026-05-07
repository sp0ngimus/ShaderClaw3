## 2026-05-07
**Prior rating:** 0★
**Approach:** 3D raymarch — NEW ANGLE: wet-street mirror reflections on ground (patchy puddles, Fresnel-weighted); window emission raised to 2.5 HDR; tonemap removed; speed default fixed
**Critique:**
1. Composition: Procedural city corridor flythrough — strong vanishing-point perspective with building grid; excellent 3D foundation.
2. Palette: Window emission at 1.6 was below HDR threshold; ad walls with procedural fallback OK but emission still post-tonemapped.
3. Motion: speed DEFAULT 0.7 too high (camera orbit/drift: should be 0.05-0.30); should default to calm.
4. Silhouette: Building silhouettes against sky good; ground was flat asphalt without visual interest at low altitudes.
5. HDR fidelity: Major violation: `col = col / (1.0 + col); col = pow(col, vec3(0.85));` — tonemap applied inside shader, destroying linear HDR pipeline.
**Changes:**
- REMOVE tonemap (col/(1+col) and pow(col, 0.85) lines deleted)
- speed DEFAULT: 0.7 → 0.25; MAX: 3.0 → 1.5
- Window emission: 1.6 → 2.50 linear HDR
- Wet street reflections: wetReflection() shoots 48-step secondary ray; patchy puddle mask (hash per XZ tile); Fresnel-weighted blend
- Night star HDR: sky stars pushed to 1.2× for bloom
- adIntensity DEFAULT raised 1.4 → 2.00 for HDR ad walls
**Motion audit:** speed DEFAULT 0.25 ✓; no audio multipliers (audio-inactive shader) ✓; swayX/swayY rates 0.35/0.22 ✓.
**HDR peaks reached:** Lit windows 2.50, ad walls up to adIntensity=2.0+, road markings 0.95 (intentionally dim).
**Estimated rating:** 4.0★
