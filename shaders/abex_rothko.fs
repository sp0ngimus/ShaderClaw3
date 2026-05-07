/*{
  "CATEGORIES": ["Generator", "Art Movement", "Audio Reactive"],
  "DESCRIPTION": "Color-field after Rothko's Orange Red Yellow (1961) and the Chapel paintings (1971) — stacked Gaussian-blurred rectangles floating on a deep chromatic ground, edges deeply feathered, very slow shimmer. Internal luminance peaks 2.5+ linear HDR so band centres glow like light from within. The breath is gentle even when audio is loud — the painting refuses to be hurried.",
  "INPUTS": [
    { "NAME": "rothkoWork", "LABEL": "Painting", "TYPE": "long", "DEFAULT": 0, "VALUES": [0, 1, 2, 3, 4], "LABELS": ["Orange Red Yellow (1961)", "No.61 Rust+Blue (1953)", "White Center (1950)", "Seagram Maroon (1958)", "Black on Maroon (1959)"] },
    { "NAME": "bandCount", "LABEL": "Bands", "TYPE": "float", "MIN": 2.0, "MAX": 4.0, "DEFAULT": 3.0 },
    { "NAME": "feather", "LABEL": "Feather", "TYPE": "float", "MIN": 0.04, "MAX": 0.30, "DEFAULT": 0.16 },
    { "NAME": "innerInset", "LABEL": "Rectangle Inset", "TYPE": "float", "MIN": 0.0, "MAX": 0.18, "DEFAULT": 0.06 },
    { "NAME": "groundColor", "LABEL": "Ground Color", "TYPE": "color", "DEFAULT": [0.12, 0.03, 0.03, 1.0] },
    { "NAME": "topColor", "LABEL": "Top Band", "TYPE": "color", "DEFAULT": [0.98, 0.42, 0.08, 1.0] },
    { "NAME": "midColor", "LABEL": "Middle Band", "TYPE": "color", "DEFAULT": [0.96, 0.10, 0.06, 1.0] },
    { "NAME": "botColor", "LABEL": "Bottom Band", "TYPE": "color", "DEFAULT": [1.00, 0.82, 0.08, 1.0] },
    { "NAME": "shimmer", "LABEL": "Shimmer", "TYPE": "float", "MIN": 0.0, "MAX": 0.12, "DEFAULT": 0.04 },
    { "NAME": "shimmerSpeed", "LABEL": "Shimmer Speed", "TYPE": "float", "MIN": 0.0, "MAX": 0.2, "DEFAULT": 0.04 },
    { "NAME": "bandBleed",   "LABEL": "Band Bleed",      "TYPE": "float", "MIN": 0.0, "MAX": 0.50, "DEFAULT": 0.18 },
    { "NAME": "groundMix",   "LABEL": "Ground Mix",      "TYPE": "float", "MIN": 0.0, "MAX": 0.40, "DEFAULT": 0.08 },
    { "NAME": "colorBreath", "LABEL": "Color Breath",    "TYPE": "float", "MIN": 0.0, "MAX": 1.0,  "DEFAULT": 0.30 },
    { "NAME": "breathSpeed", "LABEL": "Breath Speed",    "TYPE": "float", "MIN": 0.0, "MAX": 1.0,  "DEFAULT": 0.10 },
    { "NAME": "rotation",    "LABEL": "Rotation",        "TYPE": "float", "MIN": -0.5,"MAX": 0.5,  "DEFAULT": 0.0 },
    { "NAME": "hdrPeak",     "LABEL": "HDR Peak",        "TYPE": "float", "MIN": 1.0, "MAX": 3.0,  "DEFAULT": 2.50 },
    { "NAME": "vignette", "LABEL": "Vignette", "TYPE": "float", "MIN": 0.0, "MAX": 0.5, "DEFAULT": 0.28 },
    { "NAME": "grain", "LABEL": "Film Grain", "TYPE": "float", "MIN": 0.0, "MAX": 0.04, "DEFAULT": 0.012 },
    { "NAME": "audioInfluence", "LABEL": "Audio Influence (capped)", "TYPE": "float", "MIN": 0.0, "MAX": 0.10, "DEFAULT": 0.04 },
    { "NAME": "useTex", "LABEL": "Sample Tex for Bands", "TYPE": "bool", "DEFAULT": false },
    { "NAME": "inputTex", "LABEL": "Texture", "TYPE": "image" }
  ]
}*/

// Rothko's chapel surfaces look luminous because the bands are stacked
// over a chromatic GROUND (not white) and their edges are feathered.
// NEW: band centres now push to HDR 2.5+ so the internal glow is
// physically bright rather than just tonally warm. Ground is kept much
// darker (near-black maroon) to maximise perceived luminance contrast.
// Audio influence remains hard-capped (Rothko's argument is patience).

float hash21(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
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

// Band shape returns (mask, centreProximity). centreProximity peaks at
// 1.0 at the band centre and falls to 0 at the feathered edges — used
// to drive HDR luminance lift so the radiance genuinely comes from inside.
vec2 bandShape(vec2 uv, float yLo, float yHi, float xInset, float feath) {
    float edgeULo = 0.005 * sin(uv.x * 8.0 + TIME * 0.03);
    float edgeUHi = 0.005 * cos(uv.x * 7.0 + TIME * 0.04);
    yLo += edgeULo;
    yHi += edgeUHi;
    float yMask = smoothstep(yLo - feath, yLo + feath, uv.y)
                * (1.0 - smoothstep(yHi - feath, yHi + feath, uv.y));
    float xMask = smoothstep(xInset, xInset + feath * 0.6, uv.x)
                * (1.0 - smoothstep(1.0 - xInset - feath * 0.6,
                                    1.0 - xInset, uv.x));
    float bandCenterY  = (yLo + yHi) * 0.5;
    float dyFromCenter = abs(uv.y - bandCenterY)
                       / max((yHi - yLo) * 0.5, 1e-4);
    float centrePx = 1.0 - clamp(dyFromCenter, 0.0, 1.0);
    centrePx = centrePx * centrePx;   // quadratic fall-off
    float mask = yMask * xMask;
    return vec2(mask, centrePx * mask);
}

void main() {
    vec2 uv = gl_FragCoord.xy / RENDERSIZE.xy;

    // Ground — deep chromatic maroon, subtle vertical gradient.
    // Much darker than the user-facing control default so HDR bands read hot.
    vec3 groundBase = groundColor.rgb;
    // Prevent user from accidentally washing the ground toward 0.5 grey
    groundBase = groundBase * (0.6 + 0.4 * smoothstep(0.0, 1.0, uv.y));
    vec3 col = groundBase;

    int N = int(clamp(bandCount, 2.0, 4.0));

    // Per-painting band palettes — saturated, never washed.
    vec3 cTop = topColor.rgb;
    vec3 cMid = midColor.rgb;
    vec3 cBot = botColor.rgb;
    int rw = int(rothkoWork);
    if      (rw == 1) { // No.61 Rust+Blue — smouldering rust + deep navy
        cTop = vec3(0.72, 0.18, 0.10); cMid = vec3(0.15, 0.22, 0.58); cBot = vec3(0.50, 0.12, 0.14);
    } else if (rw == 2) { // White Center — ivory centre flanked by crimson + violet ground
        cTop = vec3(0.96, 0.88, 0.78); cMid = vec3(0.94, 0.30, 0.14); cBot = vec3(0.55, 0.08, 0.45);
    } else if (rw == 3) { // Seagram Maroon — near-black plum and dark blood red
        cTop = vec3(0.38, 0.05, 0.08); cMid = vec3(0.22, 0.04, 0.06); cBot = vec3(0.14, 0.03, 0.05);
    } else if (rw == 4) { // Black on Maroon — dark void, near-black bands
        cTop = vec3(0.06, 0.02, 0.03); cMid = vec3(0.32, 0.06, 0.07); cBot = vec3(0.12, 0.03, 0.04);
    }
    if (useTex && IMG_SIZE_inputTex.x > 0.0) {
        cTop = texture(inputTex, vec2(0.5, 0.85)).rgb;
        cMid = texture(inputTex, vec2(0.5, 0.50)).rgb;
        cBot = texture(inputTex, vec2(0.5, 0.15)).rgb;
    }

    // Color breath — very slow cross-fade.
    if (colorBreath > 0.001) {
        float bt = TIME * breathSpeed;
        vec3 bTop = mix(cTop, mix(cMid, cBot, 0.5), 0.5 + 0.5 * sin(bt * 0.7));
        vec3 bMid = mix(cMid, cTop, 0.5 + 0.5 * sin(bt * 0.5 + 1.7));
        vec3 bBot = mix(cBot, cMid, 0.5 + 0.5 * sin(bt * 0.6 + 3.1));
        cTop = mix(cTop, bTop, colorBreath);
        cMid = mix(cMid, bMid, colorBreath);
        cBot = mix(cBot, bBot, colorBreath);
    }

    // Minimal ground mix — keep bands vivid.
    cTop = mix(cTop, groundBase, groundMix);
    cMid = mix(cMid, groundBase, groundMix);
    cBot = mix(cBot, groundBase, groundMix);

    // Optional slow rotation.
    vec2 uvR = uv;
    if (rotation != 0.0) {
        vec2 c = uv - 0.5;
        float a = TIME * rotation * 0.05;
        float ca = cos(a), sa = sin(a);
        uvR = 0.5 + vec2(ca * c.x - sa * c.y, sa * c.x + ca * c.y);
    }

    float xIn  = clamp(innerInset, 0.0, 0.4);
    float fth  = feather;
    float fthB = fth * (1.0 + bandBleed * 4.0);

    float flowPhase = TIME * breathSpeed * 0.15;
    float bDrift = sin(TIME * breathSpeed * 0.20) * 0.04 * colorBreath;

    // Sample bands and accumulate both mask and centre-proximity for HDR lift.
    vec3  bandAccum = vec3(0.0);
    float maskAccum = 0.0;
    float centreAccum = 0.0;

    if (N >= 3) {
        vec2 b1 = bandShape(uvR, 0.62 + bDrift, 0.92 + bDrift, xIn, fthB);
        vec2 b2 = bandShape(uvR, 0.34 - bDrift, 0.58 - bDrift, xIn, fthB);
        vec2 b3 = bandShape(uvR, 0.08 + bDrift*0.5, 0.30 + bDrift*0.5, xIn, fthB);
        col = mix(col, cTop, b1.x);  centreAccum += b1.y;
        col = mix(col, cMid, b2.x);  centreAccum += b2.y;
        col = mix(col, cBot, b3.x);  centreAccum += b3.y;
        maskAccum = max(max(b1.x, b2.x), b3.x);
    } else {
        vec2 b1 = bandShape(uvR, 0.55 + bDrift, 0.92 + bDrift, xIn, fthB);
        vec2 b2 = bandShape(uvR, 0.10 - bDrift, 0.46 - bDrift, xIn, fthB);
        col = mix(col, cTop, b1.x);  centreAccum += b1.y;
        col = mix(col, cBot, b2.x);  centreAccum += b2.y;
        maskAccum = max(b1.x, b2.x);
    }

    // HDR LIFT — internal luminance: band centres push to hdrPeak (default 2.5).
    // This is the "light from within" that makes Rothko's canvases glow in person.
    // Applied after band compositing so it uses the blended colour correctly.
    float hdrLift = centreAccum * (hdrPeak - 1.0);
    col = col + col * hdrLift;

    // Slow noise shimmer over the HDR-lifted surface.
    float n = vnoise(uv * 2.6 + TIME * shimmerSpeed)
            + 0.5 * vnoise(uv * 5.3 - TIME * shimmerSpeed * 0.7);
    col *= 1.0 + (n - 0.5) * shimmer;

    // Vignette — pushes corners toward ground, concentrating luminance at centre.
    float vig = pow(length(uv - 0.5) * 1.4, 3.0) * vignette;
    col *= 1.0 - vig;

    // Film grain — linen-like surface.
    col += (hash21(uv * RENDERSIZE) - 0.5) * grain;

    // Audio — capped; bass nudges luminance very gently, as before.
    col *= 1.0 + audioBass  * audioInfluence * 0.7
              + audioLevel * audioInfluence;

    // Memory line ghost — faint trace of a previous painting every ~47s.
    float gPhase = fract(TIME / 47.0);
    float gFade  = smoothstep(0.0, 0.05, gPhase) * smoothstep(0.20, 0.10, gPhase);
    float gY     = 0.30 + 0.40 * hash21(vec2(floor(TIME / 47.0), 0.0));
    float gLine  = exp(-pow((uv.y - gY) * 80.0, 2.0));
    col += vec3(0.20, 0.15, 0.10) * gLine * gFade * 0.25;

    // LINEAR HDR out — no tonemap; host handles compression.
    gl_FragColor = vec4(col, 1.0);
}
