# blobscillator.fs — Critique (2026-05-08)
**Angle: Color Palette, HDR Peaks, Audio Reactivity**

## 5-Axis Assessment

### 1. Composition / Layout
Solid reaction-diffusion-style blob field. The sin-distance accumulator over polar sample points creates the characteristic metaball rings. `loops` and `freq1/freq2` give good compositional control over ring density and blob scale. `center` offset lets the field be reframed.

### 2. Palette / Color
**Critical issue fixed.** `gl_FragColor = vec4(vec3(dist), 1.0)` output raw grayscale — effectively a diagnostic output. `dist` from the sin accumulator ranges widely and is purely monochrome. Added IQ cosine palette: `0.5 + 0.5 * cos(6.283 * (t + vec3(0.0, 0.333, 0.667)))` driven by dist and a slow TIME drift. This produces a full-spectrum cycling palette with no white-mixing (the three channels always occupy different hue positions 120° apart).

### 3. Motion Discipline
**Audio K added safely.** Original shader had zero audio reactivity. Added bass-driven rate modulation: `rate * (1.0 + audioBass * 0.8)` — K=0.8 for the animation-pulse-rate category (≤1.5 cap). The slow TIME drift on the palette phase (× 0.06) is within the animation-pulse-rate default range.

### 4. Silhouette / Clarity
The blob contour rings emerge as strong iso-curves within the cosine palette — each ring reads as a distinct hue band against adjacent rings. The `loops` parameter directly controls ring spacing.

### 5. HDR Fidelity
**Zero HDR before this pass.** `vec3(dist)` with negative dist values produced black clips; positive values above 1 produced white clips — essentially SDR via saturation. The new palette maps dist through cos() → [0,1], then applies `col * col * 2.2`. At a palette peak (cos component = 1.0): 1.0 × 1.0 × 2.2 = 2.2 HDR. At mid value (0.5): 0.5 × 0.5 × 2.2 = 0.55. Bright blobs now fire bloom.

## Change Summary
- `C = sin(TIME * rate)` → `sin(TIME * rate * (1.0 + audioBass * 0.8))` (audio reactivity, K=0.8)
- `gl_FragColor = vec4(vec3(dist), 1.0)` → IQ cosine palette + `col * col * 2.2` (color + HDR)
