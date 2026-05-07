## 2026-05-07
**Prior rating:** 0★
**Approach:** 2D refine — NEW ANGLE: Night Studio / Black Pourings (1952) — near-void black ground, luminous neon paint (cadmium yellow HDR 2.5, alizarin crimson HDR 2.2, electric cobalt HDR 2.4, titanium white HDR 2.8) — completely different colour world from warm raw-canvas works
**Critique:**
1. Composition: Curl-noise dripper system is exceptional — the all-over skein composition is authentic and has real density.
2. Palette: Existing 5 works all use warm canvas + ochre/black/red on beige; all feel related. No dark-ground variant.
3. Motion: wanderSpeed DEFAULT 0.18 ✓; K_audio for speed: `audioMid * audioReact * 1.0` → K=1.0 ✓; splatter K_treble = 1.0 ✓.
4. Silhouette: Dense all-over — deliberate per Pollock's method; the new dark ground creates extreme contrast with HDR paint.
5. HDR fidelity: Existing HDR logic good (wet-paint corePeak 1.1-2.0 on white/silver); but black ground with neon WILL push peaks visibly now.
**Changes:**
- Add pollockWork == 5 "Black Pourings (1952)" palette: void black ground, neon HDR paint colours
- Ground init uses near-black [0.02, 0.02, 0.03] for mode 5
- Paint fade also decays toward black ground for mode 5
- Neon palette: titanium white 2.80 HDR, cad yellow 2.50, crimson 2.20, cobalt 2.40
- DESCRIPTION updated with Night Studio note
**Motion audit:** wanderSpeed DEFAULT 0.18 ✓; K_speed = 1.0 ✓; K_splatter = 1.0 ✓; K_width = 0.8 ✓.
**HDR peaks reached:** Titanium white strokes: 2.80; cadmium yellow: 2.50; crimson: 2.20; cobalt: 2.40.
**Estimated rating:** 4.5★
