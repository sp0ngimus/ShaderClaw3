/*{
  "CATEGORIES": ["Generator", "Art Movement", "Audio Reactive"],
  "DESCRIPTION": "Pollock action painting — N independent drippers on curl-noise paths. NEW: Night Studio mode adds a Black Pourings (1952) variant — near-void black ground with luminous neon paint (cadmium yellow HDR 2.5, alizarin crimson HDR 2.2, electric white HDR 2.8) for maximum contrast. Classic raw-canvas works retained. Linear HDR peaks on wet paint tips.",
  "INPUTS": [
    { "NAME": "pollockWork", "LABEL": "Painting", "TYPE": "long", "DEFAULT": 0, "VALUES": [0, 1, 2, 3, 4, 5], "LABELS": ["Autumn Rhythm (1950)", "Lavender Mist (1950)", "Number 1A (1948)", "Blue Poles (1952)", "Convergence (1952)", "Black Pourings (1952)"] },
    { "NAME": "drippers", "LABEL": "Drippers", "TYPE": "float", "MIN": 4.0, "MAX": 100.0, "DEFAULT": 40.0 },
    { "NAME": "strokeWidth", "LABEL": "Stroke Width", "TYPE": "float", "MIN": 0.001, "MAX": 0.025, "DEFAULT": 0.006 },
    { "NAME": "turbulence", "LABEL": "Turbulence", "TYPE": "float", "MIN": 0.5, "MAX": 6.0, "DEFAULT": 2.4 },
    { "NAME": "wanderSpeed", "LABEL": "Wander Speed", "TYPE": "float", "MIN": 0.01, "MAX": 0.4, "DEFAULT": 0.18 },
    { "NAME": "paintFade", "LABEL": "Paint Persistence", "TYPE": "float", "MIN": 0.94, "MAX": 1.0, "DEFAULT": 0.985 },
    { "NAME": "splatterDensity", "LABEL": "Splatter", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.25 },
    { "NAME": "wetness", "LABEL": "Wetness", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.55 },
    { "NAME": "blackWeight", "LABEL": "Black Skein", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.45 },
    { "NAME": "audioReact", "LABEL": "Audio React", "TYPE": "float", "MIN": 0.0, "MAX": 2.0, "DEFAULT": 1.0 },
    { "NAME": "useTexColor", "LABEL": "Use Tex Colour", "TYPE": "bool", "DEFAULT": false },
    { "NAME": "resetField", "LABEL": "Reset", "TYPE": "bool", "DEFAULT": false },
    { "NAME": "inputTex", "LABEL": "Texture", "TYPE": "image" }
  ],
  "PASSES": [
    { "TARGET": "paintBuf", "PERSISTENT": true },
    {}
  ]
}*/

// Pollock's classical drip palette: enamel black, lead white, aluminium
// silver, cadmium red. The skein needs distinctive separation between
// strokes — random rainbow defeats the read.
const vec3 POL_BLACK = vec3(0.05, 0.04, 0.04);
const vec3 POL_WHITE = vec3(0.95, 0.93, 0.88);
const vec3 POL_SILVR = vec3(0.62, 0.64, 0.65);
const vec3 POL_RED   = vec3(0.78, 0.16, 0.12);
const vec3 POL_OCHRE = vec3(0.62, 0.46, 0.18);
const vec3 RAW_CANVAS = vec3(0.88, 0.83, 0.72);

float hash21(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}
float hash11(float p) {
    return fract(sin(p * 12.9898) * 43758.5453);
}

float vnoise(vec2 p) {
    vec2 ip = floor(p), fp = fract(p);
    fp = fp * fp * (3.0 - 2.0 * fp);
    float a = hash21(ip);
    float b = hash21(ip + vec2(1.0, 0.0));
    float c = hash21(ip + vec2(0.0, 1.0));
    float d = hash21(ip + vec2(1.0, 1.0));
    return mix(mix(a, b, fp.x), mix(c, d, fp.x), fp.y);
}

// 2D curl of a noise potential — divergence-free vector field. Each
// "dripper" follows this so its trajectory wanders without crossing
// itself the way Bezier or sine paths do.
vec2 curl(vec2 p) {
    float e = 0.01;
    float a = vnoise(p + vec2(0.0, e)) - vnoise(p - vec2(0.0, e));
    float b = vnoise(p + vec2(e, 0.0)) - vnoise(p - vec2(e, 0.0));
    return normalize(vec2(a, -b) + 1e-5);
}

// Compute a dripper's current position by Eulerian-stepping along the
// curl field. Doing N steps per fragment is expensive but each fragment
// only walks N=12 steps → O(N) per fragment per dripper.
vec2 dripperPos(int id, float t, float turb, float speed) {
    float fid = float(id);
    vec2 base = vec2(hash11(fid * 1.31), hash11(fid * 2.97 + 4.7));
    vec2 p = base;
    // Walk a fixed number of steps with longer stride — saves 57% of
    // noise calls vs the original 14-step loop while preserving total
    // path length. The per-frame TIME term inside curl already gives
    // continuous wandering.
    for (int i = 0; i < 6; i++) {
        p += curl(p * turb + fid * 11.7 + TIME * 0.02) * speed * 0.08;
        p = clamp(p, 0.02, 0.98);
    }
    // Per-frame walk so the dripper moves continuously rather than
    // settling at a fixed integration result.
    p += 0.02 * vec2(sin(TIME * 0.50 + fid),
                     cos(TIME * 0.40 + fid));
    // Optional pooling — every few seconds, 15% of drippers dwell near
    // their base point so the canvas develops paint pools.
    if (hash11(fid + floor(TIME * 0.3)) > 0.85) p = mix(p, base, 0.5);
    return clamp(p, 0.02, 0.98);
}

// Per-painting palette swap — Pollock's drip-period spans wildly
// different colour worlds: Lavender Mist's pinks and pale greys vs
// Blue Poles's cobalt verticals vs Number 1A's ochre/black.
void pollockPalette(int w, out vec3 c0, out vec3 c1, out vec3 c2,
                    out vec3 c3, out vec3 c4) {
    if (w == 1) {            // Lavender Mist 1950 — pink/grey/white veils
        c0 = vec3(0.18, 0.16, 0.18); c1 = vec3(0.93, 0.91, 0.92);
        c2 = vec3(0.78, 0.66, 0.74); c3 = vec3(0.55, 0.50, 0.58);
        c4 = vec3(0.88, 0.78, 0.82);
    } else if (w == 2) {     // Number 1A 1948 — handprints, ochre + black
        c0 = vec3(0.06, 0.05, 0.05); c1 = vec3(0.96, 0.94, 0.88);
        c2 = vec3(0.70, 0.55, 0.20); c3 = vec3(0.40, 0.32, 0.20);
        c4 = vec3(0.85, 0.75, 0.55);
    } else if (w == 3) {     // Blue Poles 1952 — cobalt verticals
        c0 = vec3(0.05, 0.04, 0.04); c1 = vec3(0.92, 0.88, 0.78);
        c2 = vec3(0.10, 0.22, 0.62); c3 = vec3(0.78, 0.18, 0.14);
        c4 = vec3(0.65, 0.55, 0.20);
    } else if (w == 4) {     // Convergence 1952 — white/red/yellow chaos
        c0 = vec3(0.05, 0.04, 0.04); c1 = vec3(0.96, 0.95, 0.92);
        c2 = vec3(0.85, 0.20, 0.18); c3 = vec3(0.92, 0.78, 0.20);
        c4 = vec3(0.20, 0.30, 0.55);
    } else if (w == 5) {     // Black Pourings 1952 — near-void ground, HDR neon drips
        // Ground is near-black; paint is luminous neon so strokes READ HOT.
        // HDR peaks handled via the existing corePeak system → bloom catches them.
        c0 = vec3(0.02, 0.02, 0.03);   // void black (tiny, replaces black skein)
        c1 = vec3(2.80, 2.75, 2.60);   // titanium white HDR 2.8
        c2 = vec3(2.50, 2.10, 0.05);   // cadmium yellow HDR 2.5
        c3 = vec3(2.20, 0.08, 0.06);   // alizarin crimson HDR 2.2
        c4 = vec3(0.05, 0.30, 2.40);   // electric cobalt HDR 2.4
    } else {                 // 0 = Autumn Rhythm 1950 (default)
        c0 = POL_BLACK; c1 = POL_WHITE; c2 = POL_SILVR;
        c3 = POL_RED;   c4 = POL_OCHRE;
    }
}

vec3 dripperColor(int id, vec3 srcSample, float blackBias) {
    vec3 c0, c1, c2, c3, c4;
    pollockPalette(int(pollockWork), c0, c1, c2, c3, c4);
    float h = hash11(float(id) * 7.13);
    if (h < blackBias)            return c0;
    if (h < blackBias + 0.18)     return c1;
    if (h < blackBias + 0.32)     return c2;
    if (h < blackBias + 0.42)     return c3;
    return c4;
}

void main() {
    vec2 uv = gl_FragCoord.xy / RENDERSIZE.xy;
    float aspect = RENDERSIZE.x / max(RENDERSIZE.y, 1.0);
    int N = int(clamp(drippers, 1.0, 100.0));

    // ============= PASS 0 — paintBuf accumulation =============
    if (PASSINDEX == 0) {

        if (FRAMEINDEX < 2 || resetField) {
            // Black Pourings uses near-void ground instead of raw canvas
            vec3 groundCol = (int(pollockWork) == 5) ? vec3(0.02, 0.02, 0.03) : RAW_CANVAS;
            gl_FragColor = vec4(groundCol, 1.0);
            return;
        }

        // Slow self-decay toward canvas — paint stays for many seconds,
        // long enough for a dense skein to build, but eventually clears
        // so live performance never saturates the buffer.
        vec3 groundCol = (int(pollockWork) == 5) ? vec3(0.02, 0.02, 0.03) : RAW_CANVAS;
        vec3 prev = texture(paintBuf, uv).rgb;
        prev = mix(groundCol, prev, paintFade);

        // No self-advection — Pollock paint stays where the gesture put
        // it. (Removed to differentiate visually from Fauvism's flowing
        // paint buffer; both used to be persistent paint + curl drift.)

        // Deposit: walk each dripper to its CURRENT position (not its
        // whole history) and check if this fragment is on the stroke.
        // Wider strokes for low-frequency content (bass kicks).
        // Non-gating: 1.0 base advance at audio=0, audio only adds.
        float t = TIME * wanderSpeed * (1.0 + audioMid * audioReact * 1.0);
        // Width chosen per dripper per second so strokes vary thickness
        // like real flicks of enamel — not all uniform.
        float wHash = hash11(float(0) + floor(TIME * 1.2));  // shared baseline
        float w = strokeWidth * (1.0 + audioLevel * audioReact * 0.8);
        // Soft AA: pixel footprint in UV space for smoothstep edges.
        float pxUV = max(fwidth(uv.x), fwidth(uv.y));
        // Hard loop bound 100 — GLSL needs a constant upper limit on
        // for-loop counters. The early `break` keeps actual cost
        // proportional to the live N, not to the 100 ceiling.
        for (int i = 0; i < 100; i++) {
            if (i >= N) break;
            float fi = float(i);
            // Sample each dripper at TWO time-offsets and deposit along
            // the SEGMENT between previous and current head positions.
            // Converts per-frame stamps into continuous skeins —
            // *Autumn Rhythm* lattice density instead of bead-chains.
            float tOff = hash11(fi * 0.71) * 8.0;
            vec2 pNow  = dripperPos(i, t + tOff, turbulence, 1.0);
            vec2 pPrev = dripperPos(i, t + tOff - 0.06, turbulence, 1.0);
            // Distance from fragment to the segment pPrev→pNow
            vec2 ab = pNow - pPrev;
            float h = clamp(dot(uv - pPrev, ab) / max(dot(ab, ab), 1e-6),
                            0.0, 1.0);
            vec2 cl = pPrev + ab * h;
            vec2 d  = uv - cl;
            d.x *= aspect;
            float ds = length(d);
            if (ds > w * 4.0) continue;
            // Soft AA edge: ramp using fwidth-derived pixel footprint
            // so stroke boundaries resolve at sub-pixel scale.
            float aa = max(pxUV * 1.5, 1e-5);
            float falloff = smoothstep(w + aa, max(w * 0.4 - aa, 0.0), ds);
            if (falloff < 0.001) continue;
            vec3 src = (IMG_SIZE_inputTex.x > 0.0)
                     ? texture(inputTex, cl).rgb : vec3(0.5);
            vec3 c = useTexColor ? src
                                 : dripperColor(i, src, blackWeight);
            // HDR core: a thin wet ridge along the stroke center hits
            // 1.4–2.0 linear so the bloom pass picks up specular peaks
            // on metallic/white drippers without making everything glow.
            float corePeak = smoothstep(w * 0.55, w * 0.15, ds);
            float metalLike = max(max(c.r, c.g), c.b);
            // Only the brightest dripper colors (white/silver/cad-red/ochre)
            // pick up specular HDR; black skein stays matte.
            float hdrAmt = corePeak * smoothstep(0.45, 0.85, metalLike) * wetness;
            vec3 deposit = c + c * hdrAmt * 1.1;
            prev = mix(prev, deposit, falloff);
        }

        gl_FragColor = vec4(prev, 1.0);
        return;
    }

    // ============= PASS 1 — output ============================================

    vec3 col = texture(paintBuf, uv).rgb;

    // Splatter: tiny solid dots scattered at hashed positions, replacing
    // the underlying canvas value. Treble adds density without gating.
    if (splatterDensity > 0.0) {
        vec2 g = uv * 480.0;
        vec2 gi = floor(g);
        float roll = hash21(gi);
        // Non-gating: 1.0 base coverage at audio=0, treble adds.
        if (roll > 1.0 - splatterDensity * 0.05
                * (1.0 + audioHigh * audioReact * 1.0)) {
            // Soft AA dot: smoothstep with fwidth-derived edge.
            vec2 dC = fract(g) - 0.5;
            float dr = length(dC);
            float aa = fwidth(dr) + 1e-5;
            float spat = 1.0 - smoothstep(0.18 - aa, 0.18 + aa, dr);
            int cidx = int(hash21(gi + 17.3) * 4.0);
            vec3 sc = (cidx == 0) ? POL_BLACK
                    : (cidx == 1) ? POL_WHITE
                    : (cidx == 2) ? POL_RED : POL_OCHRE;
            // HDR drip highlight: bright splatters get a wet specular
            // peak (1.4–2.0 linear). Black splatters stay matte.
            float bright = max(max(sc.r, sc.g), sc.b);
            float coreSp = (1.0 - smoothstep(0.0, 0.06, dr)) * smoothstep(0.45, 0.85, bright);
            vec3 hdrSc = sc + sc * coreSp * 1.0;  // peaks ~2.0 linear on white
            col = mix(col, hdrSc, spat);
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // Depth & paint dimensionality: simulate raised paint surface by
    // computing local "height" (average luminance neighbourhood) and
    // applying directional lighting. Paint where the dots are pops up.
    // ──────────────────────────────────────────────────────────────────
    {
        // Sample 5 neighbours to compute a coarse height field.
        vec2 e = 1.0 / RENDERSIZE;
        float hC = dot(texture(paintBuf, uv).rgb,                 vec3(0.299, 0.587, 0.114));
        float hL = dot(texture(paintBuf, uv - vec2(e.x, 0.0)).rgb, vec3(0.299, 0.587, 0.114));
        float hR = dot(texture(paintBuf, uv + vec2(e.x, 0.0)).rgb, vec3(0.299, 0.587, 0.114));
        float hD = dot(texture(paintBuf, uv - vec2(0.0, e.y)).rgb, vec3(0.299, 0.587, 0.114));
        float hU = dot(texture(paintBuf, uv + vec2(0.0, e.y)).rgb, vec3(0.299, 0.587, 0.114));
        // Surface normal from height gradient
        vec2 grad = vec2(hR - hL, hU - hD);
        // Light from upper-left at 45° elevation
        vec2 lightDir = normalize(vec2(-0.6, 0.8));
        float diff = clamp(dot(normalize(vec2(grad.x, grad.y) * 8.0 + vec2(0.0, 0.0001)), lightDir), -1.0, 1.0);
        float specBoost = pow(max(diff, 0.0), 8.0);
        // Highlight on raised paint, shadow on the down-slope side.
        // Specular peaks lifted to HDR (1.4–2.0 linear) so the Phase Q
        // bloom pass picks up wet-paint glints — but only on raised,
        // bright paint. Ridge mask isolates ridges from flat areas.
        float ridge = smoothstep(0.02, 0.18, length(grad) * 8.0);
        col *= 1.0 + diff * 0.18;
        // HDR specular: warm white peak; ridge × specBoost confines it.
        col += vec3(1.0, 0.95, 0.85) * specBoost * ridge * 1.1;
        // Wet paint glistening — small bright dots where paint is densest
        // pushed into HDR (~1.6 linear) on the brightest pixels only.
        float wetMask = smoothstep(0.78, 1.0, hC) * wetness;
        col += vec3(1.0, 0.94, 0.82) * wetMask * 0.9;
    }

    // Multiple splatter scales — large drops + medium + fine spray
    // already exist in one scale; add a second pass at finer scale for
    // depth and busyness in dense areas. Non-gating: 1.0 base + audio.
    {
        float n2 = 80.0;
        vec2 g2 = uv * n2;
        vec2 gi2 = floor(g2);
        float r2 = hash21(gi2 + 73.1);
        if (r2 > 1.0 - splatterDensity * 0.04 * (1.0 + audioHigh * audioReact * 1.0)) {
            // Soft AA disc.
            vec2 dC2 = fract(g2) - 0.5;
            float dr2 = length(dC2);
            float aa2 = fwidth(dr2) + 1e-5;
            float spat2 = 1.0 - smoothstep(0.22 - aa2, 0.22 + aa2, dr2);
            int cidx2 = int(hash21(gi2 + 27.3) * 4.0);
            vec3 sc2 = (cidx2 == 0) ? POL_BLACK
                     : (cidx2 == 1) ? POL_WHITE
                     : (cidx2 == 2) ? POL_RED : POL_OCHRE;
            // HDR core for bright fine-spray dots (~1.5 linear peak).
            float bright2 = max(max(sc2.r, sc2.g), sc2.b);
            float core2 = (1.0 - smoothstep(0.0, 0.08, dr2)) * smoothstep(0.45, 0.85, bright2);
            vec3 hdr2 = sc2 + sc2 * core2 * 0.7;
            col = mix(col, hdr2, spat2 * 0.7);
        }
    }

    // Surprise: every ~14 seconds a gold-leaf flash washes the brightest
    // splatters with metallic gold for ~1 second. HDR peak so the bloom
    // pass picks it up as a true highlight.
    float goldPhase = fract(TIME / 14.0);
    float goldFlash = smoothstep(0.0, 0.10, goldPhase) * smoothstep(0.18, 0.10, goldPhase);
    float lum = dot(col, vec3(0.299, 0.587, 0.114));
    float goldMask = goldFlash * smoothstep(0.55, 0.85, lum);
    // Push gold into HDR (~1.7 linear) on brightest pixels only.
    col = mix(col, vec3(1.50, 1.28, 0.52), goldMask * 0.6);

    // NO TONEMAP — output linear HDR; downstream Phase Q v4 bloom and
    // tonemapping handle the final compression.
    gl_FragColor = vec4(col, 1.0);
}
