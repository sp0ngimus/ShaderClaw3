# body_fluid.fs — Critique (2026-05-08)
**Angle: Fluid Speed Default, Audio K Cap, HDR Metallic Specular**

## 5-Axis Assessment

### 1. Composition / Layout
Sophisticated 4-pass ISF fluid simulation: velocity field, UV advection, pose snapshot, and final liquid-metal render. The bone-attraction system (12 skeleton segments) is architecturally clever — fluid only exists where the skeleton has disturbed it, creating the "liquid stick figure" silhouette. Edge bounce prevents velocity from leaking off canvas.

### 2. Palette / Color
The iridescent environment color via HSV rotation provides full-spectrum saturation without being statically fixed. `metalColor` default (0.7, 0.85, 1.0) is a cool blue-silver that reads as convincing liquid metal. The iridescence parameter shifts hue continuously so the fluid never reads as paint.

### 3. Motion Discipline
**Two violations fixed:**
1. **fluidSpeed DEFAULT 4.0:** Even for a simulation, this drives the UV advection step at `vel * 0.003 * fluidSpeed`, making the fluid visually thrash at rest. Reduced DEFAULT to 2.0 and MAX from 15.0 to 8.0, consistent with sensible fluid simulation pacing.
2. **audioBass K=3.0:** `splatRadius * (1.0 + audioBass * 3.0)` multiplies the audio-triggered bass splat radius by up to 4× at peak bass. K=3.0 exceeds the K≤1.5 cap. Changed to K=1.5: `splatRadius * (1.0 + audioBass * 1.5)`.

### 4. Silhouette / Clarity
Excellent — fluid visibility is driven by `dispMag` (UV displacement magnitude), so fluid only renders where skeleton motion has disturbed it. Clean early-out on `fluidAlpha < 0.001` keeps the background purely black, maximizing contrast. The surface normal derivation from UV displacement gradient is physically motivated and produces convincing liquid curvature.

### 5. HDR Fidelity
The primary specular `spec = pow(dot, specPow) * specAmount` with specAmount DEFAULT=2.0 already peaks at 2.0 for perfect alignment — already HDR. The secondary specular (`spec2`) was only scaled by `specAmount * 0.4 = 0.8`, capping it below 1.0. Boosted secondary spec × 1.5 (`vec3(spec2) * 1.5`), pushing broad-angle metallic sheen to ~1.2 HDR. Combined specular can now reach ~3.2 at specular hotspot — bloom will enhance the liquid metal surface quality.

## Change Summary
- `fluidSpeed` DEFAULT: 4.0 → 2.0, MAX: 15.0 → 8.0
- `audioBass * 3.0` → `audioBass * 1.5` (K cap enforcement, K was 3.0)
- `col += vec3(spec + spec2)` → `col += vec3(spec) + vec3(spec2) * 1.5` (HDR secondary spec boost)
