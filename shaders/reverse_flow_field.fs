/*{
  "DESCRIPTION": "Reverse Flow Field — Aurora Borealis edition. Colored seeds streaked backward through a cellular flow field; cool arctic palette (cyan, violet, electric teal, lime). Linear HDR output; peaks 1.6–2.2 on aurora streams.",
  "CREDIT": "Ported from Shadertoy X3BBD1 by webwarrior (Material Maker output)",
  "CATEGORIES": ["Generator", "Flow"],
  "INPUTS": [
    { "NAME": "iterations",  "LABEL": "Trace Steps",     "TYPE": "float", "DEFAULT": 48.0, "MIN": 8.0,  "MAX": 128.0 },
    { "NAME": "stepExp",     "LABEL": "Step Size (2^-x)","TYPE": "float", "DEFAULT": 9.0,  "MIN": 6.0,  "MAX": 14.0 },
    { "NAME": "flowScale",   "LABEL": "Flow Scale",      "TYPE": "float", "DEFAULT": 4.0,  "MIN": 1.0,  "MAX": 12.0 },
    { "NAME": "flowSpeed",   "LABEL": "Flow Speed",      "TYPE": "float", "DEFAULT": 1.0,  "MIN": 0.0,  "MAX": 4.0 },
    { "NAME": "octaves",     "LABEL": "Octaves",         "TYPE": "float", "DEFAULT": 3.0,  "MIN": 1.0,  "MAX": 6.0 },
    { "NAME": "persistence", "LABEL": "Persistence",     "TYPE": "float", "DEFAULT": 0.5,  "MIN": 0.1,  "MAX": 0.9 },
    { "NAME": "dotDensity",  "LABEL": "Seed Density",    "TYPE": "float", "DEFAULT": 0.35, "MIN": 0.05, "MAX": 0.8 },
    { "NAME": "intensity",   "LABEL": "Brightness",      "TYPE": "float", "DEFAULT": 1.4,  "MIN": 0.2,  "MAX": 3.0 },
    { "NAME": "audioBoost",  "LABEL": "Audio Boost",     "TYPE": "float", "DEFAULT": 0.6,  "MIN": 0.0,  "MAX": 2.0 }
  ],
  "PASSES": [
    { "TARGET": "directions" },
    { "TARGET": "positions"  },
    {}
  ]
}*/

// ──────────────────────────────────────────────────────────────────────
// Shared hashes
// ──────────────────────────────────────────────────────────────────────
float rand1(vec2 x) {
    return fract(cos(mod(dot(x, vec2(13.9898, 8.141)), 3.14)) * 43758.5453);
}
vec2 rand2(vec2 x) {
    return fract(cos(mod(vec2(dot(x, vec2(13.9898, 8.141)),
                              dot(x, vec2(3.4562, 17.398))), vec2(3.14))) * 43758.5453);
}
vec3 rand3(vec2 x) {
    return fract(cos(mod(vec3(dot(x, vec2(13.9898, 8.141)),
                              dot(x, vec2(3.4562, 17.398)),
                              dot(x, vec2(13.254, 5.867))), vec3(3.14))) * 43758.5453);
}

// ──────────────────────────────────────────────────────────────────────
// Unified UV: full screen, [0,1] x [0,1], aspect-corrected via flowScale
// ──────────────────────────────────────────────────────────────────────
vec2 screenUV(vec2 fragCoord) {
    return fragCoord / RENDERSIZE;
}

// ──────────────────────────────────────────────────────────────────────
// Buffer A — animated cellular FBM, encoded as a direction vector
// ──────────────────────────────────────────────────────────────────────
float cellular6_noise_2d(vec2 coord, vec2 size, float offset, float seed) {
    vec2 o = floor(coord) + rand2(vec2(seed, 1.0 - seed)) + size;
    vec2 f = fract(coord);
    float min_dist1 = 2.0;
    float min_dist2 = 2.0;
    for (float x = -1.0; x <= 1.0; x += 1.0) {
        for (float y = -1.0; y <= 1.0; y += 1.0) {
            vec2 neighbor = vec2(x, y);
            vec2 node = rand2(mod(o + vec2(x, y), size)) + vec2(x, y);
            node = 0.5 + 0.25 * sin(offset * 6.28318530718 + 6.28318530718 * node);
            vec2 diff = neighbor + node - f;
            float dist = max(abs(diff.x), abs(diff.y));
            if (min_dist1 > dist) {
                min_dist2 = min_dist1;
                min_dist1 = dist;
            } else if (min_dist2 > dist) {
                min_dist2 = dist;
            }
        }
    }
    return min_dist2 - min_dist1;
}

float fbm_2d_cellular6(vec2 coord, vec2 size, int octaves_, float persistence_, float offset, float seed) {
    float normalize_factor = 0.0;
    float value = 0.0;
    float scale = 1.0;
    for (int i = 0; i < 8; i++) {
        if (i >= octaves_) break;
        float noise = cellular6_noise_2d(coord * size, size, offset, seed);
        value += noise * scale;
        normalize_factor += scale;
        size *= 2.0;
        scale *= persistence_;
    }
    return value / normalize_factor;
}

vec4 passDirections(vec2 fragCoord) {
    vec2 UV = screenUV(fragCoord);
    UV += TIME * flowSpeed / 24.0;
    float field = fbm_2d_cellular6(UV, vec2(flowScale, flowScale), int(octaves), persistence, TIME * flowSpeed * 0.05, 0.0);
    float theta = field * 6.28318530718;
    return vec4(cos(theta) * 0.5 + 0.5, sin(theta) * 0.5 + 0.5, 0.0, 1.0);
}

// ──────────────────────────────────────────────────────────────────────
// Buffer B — colored seeds (grass-tip palette)
// ──────────────────────────────────────────────────────────────────────
vec3 color_dots(vec2 uv, float size, float seed) {
    vec2 seed2 = rand2(vec2(seed, 1.0 - seed));
    uv /= size;
    vec2 point_pos = floor(uv) + vec2(0.5);
    return rand3(seed2 + point_pos);
}

float dots(vec2 uv, float size, float density, float seed) {
    vec2 seed2 = rand2(vec2(seed, 1.0 - seed));
    uv /= size;
    vec2 point_pos = floor(uv) + vec2(0.5);
    return step(rand1(seed2 + point_pos), density);
}

// Arctic aurora palette: deep void → cyan → violet → electric teal → lime
vec3 grassPalette(float t) {
    vec3 a = vec3(0.0, 0.04, 0.12);   // deep arctic night
    vec3 b = vec3(0.0, 0.85, 0.85);   // aurora cyan
    vec3 c = vec3(0.55, 0.1, 1.00);   // aurora violet
    vec3 d = vec3(0.0, 1.00, 0.45);   // electric teal/lime
    if (t < 0.33) return mix(a, b, t / 0.33);
    if (t < 0.66) return mix(b, c, (t - 0.33) / 0.33);
    return mix(c, d, (t - 0.66) / 0.34);
}

vec4 passPositions(vec2 fragCoord) {
    vec2 UV = screenUV(fragCoord);
    // Cell size in UV space: ~96 cells across the long axis
    float cellSize = 1.0 / 96.0;
    vec3 dotColor = color_dots(UV, cellSize, 0.0);
    vec3 grad     = grassPalette(fract(dot(dotColor, vec3(0.333)) * 1.7));
    float dotMask = dots(UV, cellSize, dotDensity, 0.334808);
    vec3  rgb     = grad * dotMask;
    return vec4(rgb, dotMask);
}

// ──────────────────────────────────────────────────────────────────────
// Procedural fallback — used when the trace yields no signal
// ──────────────────────────────────────────────────────────────────────
vec3 proceduralFallback(vec2 UV) {
    // Direct sample of the flow field as colored streams
    vec2 uv = UV * flowScale + TIME * flowSpeed * 0.08;
    float n = fbm_2d_cellular6(uv, vec2(flowScale, flowScale), int(octaves), persistence, TIME * flowSpeed * 0.05, 0.0);
    vec3 col = grassPalette(fract(n * 2.3 + TIME * 0.05));
    // Stream highlight where field is low (seam regions)
    float stream = smoothstep(0.6, 0.0, n);
    col *= 0.4 + 1.6 * stream;
    return col;
}

// ──────────────────────────────────────────────────────────────────────
// Image — backward trace through the flow field
// ──────────────────────────────────────────────────────────────────────
vec3 traceIntensity(vec2 pos, out float totalAlpha) {
    float stepLen = pow(2.0, -stepExp);
    vec3 color    = vec3(0.0);
    float alpha   = 0.0;
    vec2 p        = pos;
    int N         = int(iterations);
    float fN      = float(N);
    for (int i = 0; i < 128; i++) {
        if (i >= N) break;
        // Wrap p into [0,1] so we always read the buffer interior
        vec2 sp = fract(p);
        vec4 samp = texture2D(positions, sp);
        // Triangle weight: peak in the middle of the trace
        float t = float(i) / fN;
        float w = 1.0 - abs(t * 2.0 - 1.0);
        alpha += samp.a * w;
        color += samp.rgb * w;
        vec3 dir = texture2D(directions, sp).rgb;
        p = p - (dir.xy - 0.5) * 2.0 * stepLen;
    }
    totalAlpha = alpha;
    // Streak normalization: divide by number of hits, scale by step count for HDR streaks
    return color / (alpha + 0.5) * (fN / 24.0);
}

vec4 passImage(vec2 fragCoord) {
    vec2 UV = screenUV(fragCoord);
    float alphaSum = 0.0;
    vec3 traced = traceIntensity(UV, alphaSum);

    // Procedural fallback always present so output is visible without audio
    // and even if buffer reads fail. Mix in based on trace confidence.
    vec3 fallback = proceduralFallback(UV);
    float traceConf = smoothstep(0.05, 0.6, alphaSum);
    vec3 col = mix(fallback, traced, traceConf);
    // Ensure the fallback always contributes a baseline glow
    col += fallback * 0.25;

    // Linear HDR — no tonemap. Aurora peaks lifted onto flow streams.
    // Target peak ~1.6–2.2 linear (aurora colors are intrinsically bright).
    float streamMass = clamp(dot(col, vec3(0.333)), 0.0, 1.5);
    float streamLift = 1.0 + 0.72 * smoothstep(0.20, 0.85, streamMass);

    // Audio is a modulator (multiplicative lift), never a gate — output is visible at zero audio.
    float audio = max(audioLevel, audioBass);
    float audioLift = 1.0 + audioBoost * audio;

    col *= intensity * streamLift * audioLift;

    return vec4(col, 1.0);
}

// ──────────────────────────────────────────────────────────────────────
void main() {
    vec2 fragCoord = gl_FragCoord.xy;
    if      (PASSINDEX == 0) gl_FragColor = passDirections(fragCoord);
    else if (PASSINDEX == 1) gl_FragColor = passPositions(fragCoord);
    else                     gl_FragColor = passImage(fragCoord);
}
