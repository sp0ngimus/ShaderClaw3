/*{
  "DESCRIPTION": "Night Rain Tokyo Neon — 3D urban nightscape in heavy rain. Amber streetlights, crimson neon kanji, electric blue signage reflected in wet streets. Chrome SDF primitives drift through rain. LINEAR HDR, no tonemap. Alive in silence.",
  "CATEGORIES": ["Generator", "Glitch", "Audio Reactive"],
  "CREDIT": "Easel — combines vaporwave_floral_shoppe + hologram_glitch",
  "INPUTS": [
    { "NAME": "horizonY",         "LABEL": "Horizon",         "TYPE": "float", "MIN": 0.40, "MAX": 0.75, "DEFAULT": 0.55 },
    { "NAME": "skyTopColor",      "LABEL": "Sky Top",         "TYPE": "color", "DEFAULT": [0.02, 0.01, 0.05, 1.0] },
    { "NAME": "skyHorizonColor",  "LABEL": "Sky Horizon",     "TYPE": "color", "DEFAULT": [0.25, 0.04, 0.08, 1.0] },
    { "NAME": "neonHDR",          "LABEL": "Neon HDR Peak",   "TYPE": "float", "MIN": 1.0,  "MAX": 4.0,  "DEFAULT": 2.8 },
    { "NAME": "gridDensity",      "LABEL": "Sign Density",    "TYPE": "float", "MIN": 4.0,  "MAX": 24.0, "DEFAULT": 12.0 },
    { "NAME": "gridPersp",        "LABEL": "Street Depth",    "TYPE": "float", "MIN": 0.5,  "MAX": 4.0,  "DEFAULT": 1.8 },
    { "NAME": "gridSpeed",        "LABEL": "Rain Shimmer",    "TYPE": "float", "MIN": 0.0,  "MAX": 1.0,  "DEFAULT": 0.25 },
    { "NAME": "y2kCount",         "LABEL": "Urban Object Count","TYPE": "float", "MIN": 0.0, "MAX": 20.0, "DEFAULT": 12.0 },
    { "NAME": "y2kSpeed",         "LABEL": "Object Speed",    "TYPE": "float", "MIN": 0.0,  "MAX": 2.0,  "DEFAULT": 0.6 },
    { "NAME": "y2kSize",          "LABEL": "Object Size",     "TYPE": "float", "MIN": 0.02, "MAX": 0.20, "DEFAULT": 0.07 },
    { "NAME": "y2kChaos",         "LABEL": "Chaos",           "TYPE": "float", "MIN": 0.0,  "MAX": 1.0,  "DEFAULT": 0.7 },
    { "NAME": "katakanaIntensity","LABEL": "Kanji",           "TYPE": "float", "MIN": 0.0,  "MAX": 1.0,  "DEFAULT": 0.6 },
    { "NAME": "vaporPosterize",   "LABEL": "Neon Posterize",  "TYPE": "float", "MIN": 1.0,  "MAX": 32.0, "DEFAULT": 16.0 },
    { "NAME": "holoChroma",       "LABEL": "Holo Chroma",     "TYPE": "float", "MIN": 0.0,  "MAX": 0.04, "DEFAULT": 0.012 },
    { "NAME": "holoScanFreq",     "LABEL": "Holo Scanlines",  "TYPE": "float", "MIN": 1.0,  "MAX": 4.0,  "DEFAULT": 2.0 },
    { "NAME": "holoTear",         "LABEL": "Tear Probability","TYPE": "float", "MIN": 0.0,  "MAX": 0.3,  "DEFAULT": 0.06 },
    { "NAME": "holoBreak",        "LABEL": "EMI Break",       "TYPE": "float", "MIN": 0.0,  "MAX": 1.0,  "DEFAULT": 0.3 },
    { "NAME": "holoGlow",         "LABEL": "Holo Glow",       "TYPE": "float", "MIN": 0.0,  "MAX": 2.0,  "DEFAULT": 0.7 },
    { "NAME": "holoTint",         "LABEL": "Hologram Tint",   "TYPE": "color", "DEFAULT": [0.55, 1.0, 0.95, 1.0] },
    { "NAME": "holoMix",          "LABEL": "Hologram Mix",    "TYPE": "float", "MIN": 0.0,  "MAX": 1.0,  "DEFAULT": 0.85 },
    { "NAME": "audioReact",       "LABEL": "Audio React",     "TYPE": "float", "MIN": 0.0,  "MAX": 2.0,  "DEFAULT": 1.0 },
    { "NAME": "inputTex",         "LABEL": "Texture (optional)", "TYPE": "image" }
  ],
  "PASSES": [
    { "TARGET": "vapor" },
    {}
  ]
}*/

// ──────────────────────────────────────────────────────────────────────
// Shared
// ──────────────────────────────────────────────────────────────────────
float hash21(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float hash11(float n) { return fract(sin(n * 12.9898) * 43758.5453); }

vec3 hsv2rgb(vec3 c) {
    vec4 K = vec4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

// ──────────────────────────────────────────────────────────────────────
// Y2K SDF shapes (retained as urban sculptures)
// ──────────────────────────────────────────────────────────────────────
float sdHeart(vec2 p) {
    p.x = abs(p.x);
    if (p.y + p.x > 1.0)
        return sqrt(dot(p - vec2(0.25, 0.75), p - vec2(0.25, 0.75))) - sqrt(2.0) / 4.0;
    return sqrt(min(dot(p - vec2(0.0, 1.0),  p - vec2(0.0, 1.0)),
                    dot(p - 0.5 * max(p.x + p.y, 0.0), p - 0.5 * max(p.x + p.y, 0.0))))
         * sign(p.x - p.y);
}
float sdStar5(vec2 p, float r) {
    const vec2 k1 = vec2(0.809016994, -0.587785252);
    const vec2 k2 = vec2(-k1.x, k1.y);
    p.x = abs(p.x);
    p -= 2.0 * max(dot(k1, p), 0.0) * k1;
    p -= 2.0 * max(dot(k2, p), 0.0) * k2;
    p.x = abs(p.x);
    p.y -= r;
    vec2 ba = vec2(-0.309016994, 0.951056516) * 0.4;
    float h = clamp(dot(p, ba) / dot(ba, ba), 0.0, 1.0);
    return length(p - ba * h) * sign(p.y * ba.x - p.x * ba.y);
}
float sdSparkle(vec2 p) {
    return min(max(abs(p.x) - 0.08, abs(p.y) - 0.30),
               max(abs(p.y) - 0.08, abs(p.x) - 0.30));
}
float sdRoundBox(vec2 p, vec2 b, float r) {
    vec2 q = abs(p) - b + r;
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
}
float sdSmiley(vec2 p, float r) {
    float face  = length(p) - r;
    float eyeL  = length(p - vec2(-r * 0.35, r * 0.25)) - r * 0.10;
    float eyeR  = length(p - vec2( r * 0.35, r * 0.25)) - r * 0.10;
    float mr1   = abs(length(p - vec2(0.0, -r * 0.05)) - r * 0.45) - r * 0.06;
    float mouth = max(mr1, -p.y);
    float feat  = min(min(eyeL, eyeR), mouth);
    return max(face, -feat);
}

// ──────────────────────────────────────────────────────────────────────
// Neon sign rectangles — amber, crimson, electric blue (HDR)
// ──────────────────────────────────────────────────────────────────────
vec3 neonSigns(vec2 uv, float aspect, float bass) {
    vec3 acc = vec3(0.0);

    // Sign 1: amber on left
    float sx1 = 0.22, sy1 = horizonY + 0.05, sw1 = 0.09, sh1 = 0.06;
    float d1x = max(abs(uv.x - sx1) - sw1, 0.0) * aspect;
    float d1y = max(abs(uv.y - sy1) - sh1, 0.0);
    float d1 = sqrt(d1x * d1x + d1y * d1y);
    float sign1 = exp(-d1 * d1 * 120.0);
    acc += vec3(1.0, 0.55, 0.10) * sign1 * 2.5 * (neonHDR / 2.8); // amber

    // Sign 2: crimson center
    float sx2 = 0.5, sy2 = horizonY + 0.09, sw2 = 0.05, sh2 = 0.08;
    float d2x = max(abs(uv.x - sx2) - sw2, 0.0) * aspect;
    float d2y = max(abs(uv.y - sy2) - sh2, 0.0);
    float d2 = sqrt(d2x * d2x + d2y * d2y);
    float sign2 = exp(-d2 * d2 * 120.0);
    acc += vec3(1.0, 0.05, 0.20) * sign2 * 2.8 * (neonHDR / 2.8); // crimson

    // Sign 3: electric blue right
    float sx3 = 0.78, sy3 = horizonY + 0.06, sw3 = 0.07, sh3 = 0.07;
    float d3x = max(abs(uv.x - sx3) - sw3, 0.0) * aspect;
    float d3y = max(abs(uv.y - sy3) - sh3, 0.0);
    float d3 = sqrt(d3x * d3x + d3y * d3y);
    float sign3 = exp(-d3 * d3 * 120.0);
    acc += vec3(0.10, 0.40, 1.0) * sign3 * 3.0 * (neonHDR / 2.8); // electric blue

    // Audio: bass pulses sign brightness — K=0.4 <= 1.5
    acc *= 1.0 + bass * 0.4;
    return acc;
}

// ──────────────────────────────────────────────────────────────────────
// Wet street — dark asphalt with neon puddle reflections and rain ripples
// ──────────────────────────────────────────────────────────────────────
vec4 wetStreet(vec2 uv, float aspect, float bass, float mid) {
    if (uv.y >= horizonY) return vec4(0.0);
    float dh = horizonY - uv.y;

    // Dark wet asphalt base
    float asphalt = 0.04 + 0.02 * sin(uv.x * 80.0 + 0.3) * sin(uv.y * 40.0);
    vec3 streetBase = vec3(asphalt, asphalt * 0.9, asphalt * 1.1);

    // Neon reflections in puddles — mirror the sign colors upward
    vec2 reflUV = vec2(uv.x, horizonY + (horizonY - uv.y) * 0.6);
    vec3 reflNeon = neonSigns(reflUV, aspect, bass);
    float puddle = 0.5 + 0.5 * sin(uv.x * 15.0 + TIME * 0.15) * sin(uv.y * 12.0 + TIME * 0.12);
    puddle = smoothstep(0.5, 0.8, puddle);
    float distFade = exp(-dh * 3.0); // fade reflection near horizon
    vec3 col = streetBase + reflNeon * puddle * distFade * 0.7;

    // Rain ripple circles — slow concentric rings at random street points
    float ripple = 0.0;
    for (int i = 0; i < 4; i++) {
        float fi = float(i);
        float rx = hash11(fi * 7.3 + floor(TIME * 0.5 + fi)) * aspect;
        float ry = hash11(fi * 3.1 + floor(TIME * 0.5 + fi)) * horizonY;
        float rd = length(vec2(uv.x * aspect - rx, uv.y - ry));
        float rphase = fract(TIME * 0.5 + fi * 0.25);
        float ring = exp(-abs(rd - rphase * 0.15) * 60.0) * (1.0 - rphase);
        ripple += ring;
    }
    col += vec3(0.4, 0.6, 1.0) * ripple * 0.3;

    return vec4(col, 1.0);
}

// ──────────────────────────────────────────────────────────────────────
// PASS 0 — Render Tokyo night scene to "vapor" buffer
// ──────────────────────────────────────────────────────────────────────
vec4 passVapor(vec2 fragCoord) {
    vec2 uv = fragCoord / RENDERSIZE.xy;
    float aspect = RENDERSIZE.x / max(RENDERSIZE.y, 1.0);

    float bass = clamp(audioBass * audioReact, 0.0, 1.0);
    float mid  = clamp(audioMid  * audioReact, 0.0, 1.0);

    // Dark night sky gradient — deep indigo to ember horizon
    vec3 sky = mix(skyHorizonColor.rgb, skyTopColor.rgb,
                   smoothstep(horizonY - 0.05, 1.0, uv.y));
    vec3 col = sky;

    // Neon signs above horizon
    col += neonSigns(uv, aspect, bass);

    // Wet street below horizon
    if (uv.y < horizonY) {
        vec4 street = wetStreet(uv, aspect, bass, mid);
        col = mix(col, street.rgb, street.a);
    }

    // Urban sculptures — the Y2K SDF objects as chrome city art
    int N = int(clamp(y2kCount, 0.0, 20.0));
    for (int i = 0; i < 20; i++) {
        if (i >= N) break;
        float fi = float(i);
        float cycle = floor(TIME * y2kSpeed * (0.3 + hash11(fi * 1.3) * 0.7) + fi * 0.7);
        float life  = fract(TIME * y2kSpeed * (0.3 + hash11(fi * 1.3) * 0.7) + fi * 0.7);
        float h1 = hash11(fi + cycle * 7.13);
        float h2 = hash11(fi + cycle * 13.7);
        float h3 = hash11(fi + cycle * 19.3);
        float h4 = hash11(fi + cycle * 23.1);
        vec2 startP = vec2(h1, h2);
        vec2 vel    = (vec2(h3, h4) - 0.5) * 1.5;
        vec2 ctr    = startP + vel * life * y2kChaos;
        ctr = vec2(0.5 + sin(ctr.x * 3.14159) * 0.45,
                   0.5 + sin(ctr.y * 3.14159) * 0.45);
        float sz   = y2kSize * (0.6 + h1 * 0.8)
                   * (0.7 + 0.3 * sin(TIME * 4.0 + fi))
                   * (1.0 + bass * 0.4);
        // Urban neon colors: amber, crimson, blue — matching the sign palette
        float hue;
        int hci = int(mod(fi, 3.0));
        if      (hci == 0) hue = 0.08; // amber/orange
        else if (hci == 1) hue = 0.97; // crimson/red
        else               hue = 0.62; // electric blue
        vec3 shapeCol = hsv2rgb(vec3(hue, 0.95, 1.0));
        float vis = smoothstep(0.0, 0.15, life) * smoothstep(1.0, 0.85, life);
        float rot = TIME * (0.5 + h3 * 2.0) + fi * 1.7;
        float ca  = cos(rot), sa = sin(rot);
        vec2 d    = uv - ctr; d.x *= aspect;
        vec2 lp   = vec2(ca * d.x - sa * d.y, sa * d.x + ca * d.y) / max(sz, 1e-4);
        int kind = int(hash11(fi * 31.7) * 5.0);
        float dist;
        if      (kind == 0) dist = sdHeart(lp + vec2(0.0, 0.5));
        else if (kind == 1) dist = sdStar5(lp, 0.85);
        else if (kind == 2) dist = sdSparkle(lp * 1.2);
        else if (kind == 3) dist = sdRoundBox(lp, vec2(0.85, 0.40), 0.20);
        else                dist = sdSmiley(lp, 0.85);
        if (dist < 0.0) col = mix(col, shapeCol, vis);
        col = mix(col, vec3(1.0), smoothstep(0.04, 0.0, abs(dist)) * vis * 0.5);
    }

    // Optional input texture overlay
    if (IMG_SIZE_inputTex.x > 0.0) {
        vec3 src = texture(inputTex, fract(uv + vec2(sin(TIME * 0.3) * 0.05, 0.0))).rgb;
        float sL = dot(src, vec3(0.299, 0.587, 0.114));
        col = mix(col, src, smoothstep(0.20, 0.40, sL) * 0.6);
    }

    // Kanji ribbon — amber character strokes above the scene
    {
        float total = 0.0;
        for (int g = 0; g < 6; g++) {
            float fg = float(g);
            vec2 origin = vec2(0.05 + fg * 0.15, 0.85);
            vec2 ld = (uv - origin) * vec2(60.0, 28.0);
            if (ld.x < 0.0 || ld.y < 0.0 || ld.x > 8.0 || ld.y > 4.0) continue;
            vec2 ci = floor(ld);
            float h = hash21(ci + floor(TIME * (0.4 + audioHigh * audioReact * 1.2)));
            float vert = step(h, 0.55) * step(0.30, fract(ld.x)) * step(fract(ld.x), 0.55);
            float bar  = step(0.55, h) * step(h, 0.85) * step(0.40, fract(ld.y)) * step(fract(ld.y), 0.62);
            total = max(total, max(vert, bar));
        }
        // Amber kanji color (warm neon)
        col = mix(col, vec3(1.0, 0.6, 0.1), total * katakanaIntensity);
    }

    // Rain streak overlay — diagonal fast lines
    vec2 rainUV = uv * vec2(80.0, 20.0) + vec2(-TIME * 3.0 * 0.6, -TIME * 8.0 * 0.6);
    float rainStreak = fract(rainUV.y + sin(rainUV.x * 0.3) * 0.5);
    float rainLine = fract(rainUV.x);
    float rain = smoothstep(0.95, 1.0, rainStreak) * step(rainLine, 0.15);
    col += vec3(0.6, 0.7, 1.0) * rain * 0.3;

    // Neon posterize (quantized HDR values give hologram something to glitch)
    if (vaporPosterize > 1.0) col = floor(col * vaporPosterize) / vaporPosterize;

    return vec4(col, 1.0);
}

// ──────────────────────────────────────────────────────────────────────
// PASS 1 — Hologram glitch over vapor buffer
// ──────────────────────────────────────────────────────────────────────
vec4 passHologram(vec2 fragCoord) {
    vec2 uv = fragCoord / RENDERSIZE.xy;

    // Vertical tear — band-shifted bands of vapor
    float bandH = 0.04;
    float bandY = floor(uv.y / bandH) * bandH;
    float tearTrig = step(1.0 - holoTear * (1.0 + audioBass * audioReact),
                          hash21(vec2(bandY, floor(TIME * 8.0))));
    uv.x += tearTrig * (hash21(vec2(bandY, TIME)) - 0.5) * 0.15;

    // RGB chromatic shift on the vapor buffer
    float ch = holoChroma * (1.0 + audioHigh * audioReact);
    float r = texture(vapor, clamp(uv + vec2( ch, 0.0), 0.0, 1.0)).r;
    float g = texture(vapor, clamp(uv,                  0.0, 1.0)).g;
    float b = texture(vapor, clamp(uv - vec2( ch, 0.0), 0.0, 1.0)).b;
    vec3 holo = vec3(r, g, b) * holoTint.rgb;

    // Scanlines (resolution-aware)
    holo *= 0.85 + 0.15 * sin(gl_FragCoord.y * holoScanFreq * 0.5);

    // EMI break: rare bursts replace fragments with hash noise
    float breakTrig = step(0.9, hash21(vec2(floor(TIME * 4.0), 0.0)));
    holo = mix(holo, vec3(hash21(uv * TIME)),
               holoBreak * audioBass * audioReact * 0.4 * breakTrig);

    // Mid-band flicker
    float flicker = 0.92 + 0.08 * sin(TIME * 60.0
                  + hash21(vec2(floor(TIME * 30.0))) * 6.28);
    holo *= mix(1.0, flicker, audioMid * audioReact * 0.5);

    // Edge bloom — bright pixels glow beyond their position
    float lum = dot(holo, vec3(0.299, 0.587, 0.114));
    holo += holoTint.rgb * pow(lum, 1.4) * holoGlow * 0.3;

    // Transmission strength — low audio dims the hologram (signal weakens)
    holo *= 0.5 + audioLevel * 0.6;

    // Mix: 0 = pure scene, 1 = full hologram
    vec3 vapor_ = texture(vapor, fragCoord / RENDERSIZE.xy).rgb;
    return vec4(mix(vapor_, holo, holoMix), 1.0);
}

// ──────────────────────────────────────────────────────────────────────
void main() {
    if (PASSINDEX == 0) FragColor = passVapor(gl_FragCoord.xy);
    else                FragColor = passHologram(gl_FragCoord.xy);
}
