## 2026-05-07
**Prior rating:** 0★
**Approach:** 3D raymarch — NEW ANGLE: full 3D volumetric gallery installation; 2D floating SDFs become 3D tetrahedron/box/sphere on plinths; studio 3-point lighting replaces flat 2D halo compositing
**Critique:**
1. Composition: Original 2D had strong Lissajous orbit composition but no depth or material substance; shapes felt like vector graphics.
2. Palette: Primary yellow/red/blue faithful to Kandinsky ✓; but as 2D flat fills they had no specular response or depth cue.
3. Motion: orbitSpeed DEFAULT 0.25 ✓; audioMid K=0.4 ✓; springReact K≈0.12 ✓ — original motion was within rules.
4. Silhouette: 2D shapes rendered via SDF min() compositing; no occlusion or cast shadow between shapes.
5. HDR fidelity: No HDR in original; flat fills with halo softness only.
**Changes:**
- Full 3D rewrite: sdTetrahedron (yellow), sdBox (red), sdSphere (blue) on sdBox plinth
- 64-step raymarch, 96 MAX_STEPS, SURF_EPS 0.0012
- Studio 3-point: warm gilt key + cool fill + rim
- Soft shadow via keyShadow() — 24-step penumbra
- Specular HDR: gloss 48-96, spec 0.15-0.40; hdrSpec peaks 2.40-2.90 linear on shiny materials
- Kandinsky support lines become 3D capsule rods floating in gallery
- Camera orbit K: camOrbitSpeed * (1 + mid * 1.5) ≤ 1.5 ✓
- "3D" added to CATEGORIES
**Motion audit:** camOrbitSpeed DEFAULT 0.07 ✓ (calm gallery orbit); audio K_mid = 0.105/0.07 = 1.5 ✓; per-shape spin: K_mid = 0.15/0.10 = 1.5 ✓.
**HDR peaks reached:** Blue sphere specular: 2.50-2.90; red lacquer specular: 2.0-2.4; yellow specular: ~1.8.
**Estimated rating:** 4.0★
