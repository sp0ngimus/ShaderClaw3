/*{
  "DESCRIPTION": "Underwater — looking up from the deep. Volumetric god rays pierce down through dancing caustics, rising bubbles trail upward toward the rippling surface. Deep-to-aqua depth gradient, HDR linear output so the sun disc and brightest caustic peaks catch bloom.",
  "CREDIT": "ShaderClaw — original underwater god-ray composition",
  "CATEGORIES": ["Generator", "3D", "Audio Reactive"],
  "INPUTS": [
    { "NAME": "sunPosX",          "LABEL": "Sun X",            "TYPE": "float", "DEFAULT": 0.50, "MIN": 0.0,  "MAX": 1.0 },
    { "NAME": "sunPosY",          "LABEL": "Sun Y",            "TYPE": "float", "DEFAULT": 0.92, "MIN": 0.5,  "MAX": 1.0 },
    { "NAME": "godrayIntensity",  "LABEL": "God-Ray Intensity","TYPE": "float", "DEFAULT": 1.20, "MIN": 0.0,  "MAX": 3.0 },
    { "NAME": "godraySamples",    "LABEL": "Ray Samples",      "TYPE": "long",  "DEFAULT": 64,   "VALUES": [16,32,48,64,96,128], "LABELS": ["16","32","48","64","96","128"] },
    { "NAME": "godrayDecay",      "LABEL": "Ray Decay",        "TYPE": "float", "DEFAULT": 0.965,"MIN": 0.85, "MAX": 0.995 },
    { "NAME": "causticIntensity", "LABEL": "Caustics",         "TYPE": "float", "DEFAULT": 1.20, "MIN": 0.0,  "MAX": 3.0 },
    { "NAME": "causticSpeed",     "LABEL": "Caustic Speed",    "TYPE": "float", "DEFAULT": 0.35, "MIN": 0.0,  "MAX": 1.5 },
    { "NAME": "causticScale",     "LABEL": "Caustic Scale",    "TYPE": "float", "DEFAULT": 4.0,  "MIN": 1.0,  "MAX": 10.0 },
    { "NAME": "bubbleCount",      "LABEL": "Bubbles",          "TYPE": "long",  "DEFAULT": 24,   "VALUES": [0,12,24,48,96],      "LABELS": ["0","12","24","48","96"] },
    { "NAME": "bubbleRise",       "LABEL": "Bubble Rise",      "TYPE": "float", "DEFAULT": 0.12, "MIN": 0.0,  "MAX": 0.6 },
    { "NAME": "depthColor",       "LABEL": "Deep Color",       "TYPE": "color", "DEFAULT": [0.015, 0.045, 0.115, 1.0] },
    { "NAME": "surfaceColor",     "LABEL": "Surface Color",    "TYPE": "color", "DEFAULT": [0.20,  0.65,  0.85,  1.0] },
    { "NAME": "sunColor",         "LABEL": "Sun Color",        "TYPE": "color", "DEFAULT": [1.00,  0.95,  0.80,  1.0] },
    { "NAME": "audioReact",       "LABEL": "Audio React",      "TYPE": "float", "DEFAULT": 0.80, "MIN": 0.0,  "MAX": 2.0 },
    { "NAME": "vignette",         "LABEL": "Vignette",         "TYPE": "float", "DEFAULT": 0.35, "MIN": 0.0,  "MAX": 0.8 }
  ]
}*/

// ====================================================================
// Underwater — looking up from the deep.
//
// Screen-space composition (no raymarching needed for this view):
//   • Depth gradient → fake 3D depth
//   • Animated caustics → wavy light refraction patterns
//   • Volumetric god rays → radial brightness march toward the sun
//   • Rising bubbles → upward-drifting particles with rim highlights
//   • Drifting motes → ambient particulate
//   • Surface ripple haze at the top of the frame
//
// HDR linear output — peaks at the sun disc, caustic crests, and
// bubble rims exceed 1.0 so Easel's bloom pass catches them.
// ====================================================================

#define PI 3.14159265

float h11(float x) { return fract(sin(x * 127.1) * 43758.5453); }
float h12(vec2 p)  { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }

float vnoise(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = h12(i);
    float b = h12(i + vec2(1.0, 0.0));
    float c = h12(i + vec2(0.0, 1.0));
    float d = h12(i + vec2(1.0, 1.0));
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

// Sharp caustic field — 4 oriented sin layers with sharpened peaks,
// warped by a low-frequency distortion so cells dance organically
// rather than tile rigidly.
float caustic(vec2 p, float t) {
    vec2 q = p;
    q.x += sin(p.y * 1.5 + t * 0.7) * 0.45;
    q.y += cos(p.x * 1.8 - t * 0.9) * 0.45;
    float c = 0.0;
    for (int i = 0; i < 4; i++) {
        float fi = float(i);
        vec2 d = vec2(sin(fi * 1.7), cos(fi * 2.3));
        float k = 1.6 + fi * 0.45;
        float s = 0.5 + 0.5 * sin(dot(q, d) * k + t * (0.3 + fi * 0.2));
        c += pow(s, 6.0);
    }
    return c * 0.6;
}

// Cheap sin-grid caustic used inside the god-ray loop (full caustic
// is too costly to call 64-128 times per pixel).
float causticFast(vec2 p, float t) {
    float a = 0.5 + 0.5 * sin(p.x * 12.0 + t * 1.5);
    float b = 0.5 + 0.5 * sin(p.y * 14.0 - t * 1.1);
    return pow(a * b, 3.0);
}

void main() {
    vec2 res    = RENDERSIZE;
    vec2 uv     = gl_FragCoord.xy / res;
    float aspect = res.x / res.y;
    float audio = clamp(audioReact, 0.0, 2.0);
    float t     = TIME;

    // ─── Depth gradient — dark abyss below, surface light above ───
    float depth = pow(uv.y, 1.15);
    vec3 col = mix(depthColor.rgb, surfaceColor.rgb, depth);

    // Aspect-corrected UV for spatial features (caustics, bubbles).
    vec2 puv = vec2(uv.x * aspect, uv.y);

    // ─── Caustics — strongest near the surface, fade with depth ───
    float surfaceProx = smoothstep(0.0, 0.85, uv.y);
    float c1 = caustic(puv * causticScale,                              t * causticSpeed);
    float c2 = caustic(puv * causticScale * 1.7 + vec2(5.3, 2.1),       t * causticSpeed * 1.3);
    float c  = max(c1, c2 * 0.7) * surfaceProx;
    c *= 1.0 + audioMid * audio * 0.7;
    col += sunColor.rgb * c * causticIntensity * 0.55;

    // ─── God rays — radial march toward the sun, accumulating
    // brightness through a soft sun mask and a cheap surface-refraction
    // mask that makes rays "shimmer" through the wavy water surface.
    vec2 sunUV = vec2(sunPosX, sunPosY);
    int N = int(godraySamples);
    if (N < 4)   N = 4;
    if (N > 256) N = 256;
    float Nf = float(N);
    vec2 deltaUV = (uv - sunUV) / Nf;
    vec2 samplePos = uv;
    float illum  = 0.0;
    float weight = 1.0;
    for (int i = 0; i < 256; i++) {
        if (i >= N) break;
        samplePos -= deltaUV;
        float maskDist = length((samplePos - sunUV) * vec2(aspect * 0.6, 1.0));
        float sunMask  = exp(-maskDist * 3.2);
        float surfMask = causticFast(samplePos * vec2(aspect, 1.0), t * causticSpeed * 0.8);
        surfMask *= smoothstep(0.55, 1.0, samplePos.y);
        illum  += max(sunMask, surfMask * 0.55) * weight;
        weight *= godrayDecay;
    }
    illum /= Nf;
    illum *= godrayIntensity;
    illum *= 1.0 + audioBass * audio * 1.3;
    col += sunColor.rgb * illum * 2.4;

    // ─── Sun disc + halo ─────────────────────────────────────────
    float sunDist = length((uv - sunUV) * vec2(aspect, 1.0));
    float sunDisc = smoothstep(0.12, 0.02, sunDist);
    col += sunColor.rgb * sunDisc * 2.0;
    float halo = exp(-sunDist * 2.6);
    col += sunColor.rgb * halo * 0.45;

    // ─── Bubbles — rising motes with a bright rim ─────────────────
    int B = int(bubbleCount);
    if (B > 96) B = 96;
    for (int i = 0; i < 96; i++) {
        if (i >= B) break;
        float fi   = float(i);
        vec2 seed  = vec2(h11(fi * 7.1), h11(fi * 13.3));
        float bx   = seed.x * aspect;
        float size = mix(0.003, 0.014, h11(fi * 23.7));
        float life = mix(4.0, 12.0, h11(fi * 31.1));
        float phase = fract((t + seed.y * 100.0) * bubbleRise / life * 8.0);
        bx += sin(t * 0.5 + fi * 2.1) * 0.018;
        vec2 bp = vec2(bx, phase);
        float bd = length(puv - bp);
        float fill = smoothstep(size, size * 0.4, bd);
        float rim  = smoothstep(size * 0.95, size * 0.75, bd) -
                     smoothstep(size * 0.75, size * 0.5, bd);
        float lifeFade = smoothstep(0.0, 0.10, phase) *
                        smoothstep(1.0, 0.85, phase);
        col += vec3(0.65, 0.85, 0.95) * fill * lifeFade * 0.22;
        col += vec3(1.30, 1.55, 1.70) * rim  * lifeFade * 0.65;
    }

    // ─── Drifting motes / particulate ────────────────────────────
    float motes = pow(vnoise(uv * 90.0 + vec2(0.0, t * 0.06)), 9.0);
    col += vec3(0.45, 0.65, 0.85) * motes * 0.45;

    // ─── Surface ripple band at the very top ─────────────────────
    if (uv.y > 0.90) {
        float ripple = 0.5 + 0.5 * sin(uv.x * 80.0 * aspect + t * 3.5);
        ripple *= smoothstep(0.90, 1.0, uv.y);
        col += sunColor.rgb * ripple * 0.20;
    }

    // ─── Vignette ────────────────────────────────────────────────
    vec2 vuv = uv * (1.0 - uv.yx);
    float vig = pow(max(vuv.x * vuv.y * 16.0, 0.0), max(vignette, 0.001));
    col *= mix(1.0, vig, clamp(vignette, 0.0, 1.0));

    // HDR linear output — peaks > 1.0 are intentional for bloom.
    gl_FragColor = vec4(col, 1.0);
}
