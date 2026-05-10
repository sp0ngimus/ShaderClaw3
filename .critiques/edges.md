## 2026-05-05
**Prior rating:** 0.0★
**Approach:** 2D refine (particle system, 3D category added)
**Critique:**
1. Reference fidelity: Particle bounce concept is solid but LED grid default masks all output — black on dark bg.
2. Compositional craft: Capsule streak particles are a strong visual idea, lost in default darkness.
3. Technical execution: uses undeclared audio uniforms (audioBass, audioHigh) safely; LED mode quantizes to near-black at default ledSize.
4. Liveness: TIME-driven particle motion works but LED mode destroys visibility.
5. Differentiation: Unique capsule-stretch bounce system; killed by LED default and desaturated colorJitter mixing with white.
**Changes:**
- Removed LED wall mode entirely (was default ON, producing near-black output)
- Replaced colorJitter white-mixing with fully saturated 6-hue neon palette (magenta→cyan→gold→orange→violet→lime)
- Glow boosted: default 1.3 → 2.5 (HDR range)
- Particle count stays at 128 (was N=256 const regardless of particleCount input)
- Added 3D category
- Black background (0.0, 0.0, 0.01)
- Stretch, particle size defaults tuned up for visibility
**HDR peaks reached:** particle cores + halo accumulation → 2.5+ per cluster
**Estimated rating:** 4.0★

## 2026-05-10
**Prior rating:** 0.0★
**Approach:** 2D refine — NEW ANGLE: Dark Canvas Chiaroscuro (prior 2026-05-05 was 2D refine particle/LED system)
**Critique:**
1. Composition: same Picasso face SDF structure, but polarity reversed — light strokes on dark ground vs. dark ink on cream paper. Different lighting style axis.
2. Palette: per-mood neon HDR strokes (charcoal→warm white 2.2, pencil→electric cyan 2.2, etching→amber neon 2.35, schiele→orange-red 2.4, hockney→cobalt 2.5) on near-black. Vs prior warm cream paper with dark ink.
3. Motion: fixed jitter epoch from t*1.3 (~0.77s per direction flip) → t*0.15 (~6.7s per direction flip). Now ≥5s minimum per §4.
4. Silhouette: strong face portrait SDF strokes now glow bright on black — improved contrast and focal clarity.
5. HDR fidelity: neon stroke colors 2.2–2.5 linear; Caravaggio single key light adds dramatic rake. fwidth() rim edges preserved.
**Changes:**
- Added `darkCanvas` bool input (default true) — reverses paper/ink polarity
- Dramatic single warm key light from upper-left (Caravaggio raking light)
- Per-mood neon stroke colors 2.2–2.5 HDR on near-black ground
- Dark mode hatching glows teal/sage neon instead of white-on-cream
- Strong corner vignette crushes to true black at frame edges
- Fixed jitter epoch: t*1.3 → t*0.15 (§4 compliant, ≥5s period)
**Motion audit:** sway=0.010*sin(t*0.55) — very gentle; jitter epoch fixed; audio K≤1.5 ✓
**HDR peaks reached:** neon strokes 2.2–2.5; red ear stud 2.5; rim glow ~2.0+
**Estimated rating:** 4.2★
