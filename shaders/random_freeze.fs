/*{
  "DESCRIPTION": "3D Neon Plasma Storm — swirling electromagnetic plasma tendrils rendered as glowing capsule SDFs inside a dark void. Fully saturated palette: electric magenta, cyan, gold on black. HDR peaks 3.0+ at tendril cores. LINEAR HDR out, no tonemap. Audio modulates tendril intensity and storm spin rate.",
  "CREDIT": "ShaderClaw auto-improve — plasma storm",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "3D", "Audio Reactive"],
  "INPUTS": [
    { "NAME": "tendrilCount", "LABEL": "Tendrils",    "TYPE": "float", "DEFAULT": 5.0,  "MIN": 2.0,  "MAX": 8.0 },
    { "NAME": "stormRadius",  "LABEL": "Radius",      "TYPE": "float", "DEFAULT": 1.4,  "MIN": 0.5,  "MAX": 3.0 },
    { "NAME": "spinRate",     "LABEL": "Spin Rate",   "TYPE": "float", "DEFAULT": 0.20, "MIN": 0.0,  "MAX": 1.5 },
    { "NAME": "glowWidth",    "LABEL": "Glow Width",  "TYPE": "float", "DEFAULT": 0.18, "MIN": 0.04, "MAX": 0.6 },
    { "NAME": "hdrPeak",      "LABEL": "HDR Peak",    "TYPE": "float", "DEFAULT": 3.0,  "MIN": 1.0,  "MAX": 6.0 },
    { "NAME": "audioReact",   "LABEL": "Audio React", "TYPE": "float", "DEFAULT": 0.9,  "MIN": 0.0,  "MAX": 2.0 }
  ]
}*/

#define MAX_STEPS 64
#define MAX_DIST  12.0
#define EPS       0.003
#define PI        3.14159265
#define TAU       6.28318530

float hash11(float n) { return fract(sin(n * 12.9898) * 43758.5453); }

mat2 rot2(float a) { float c = cos(a), s = sin(a); return mat2(c, -s, s, c); }

// Capsule SDF
float sdCapsule(vec3 p, vec3 a, vec3 b, float r) {
    vec3 ab = b - a, ap = p - a;
    float t = clamp(dot(ap, ab) / dot(ab, ab), 0.0, 1.0);
    return length(ap - t * ab) - r;
}

// Distance to an individual tendril capsule
float tendrilDist(vec3 p, float idx, float t) {
    float seed1 = hash11(idx * 5.31);
    float seed2 = hash11(idx * 3.17 + 1.0);
    float seed3 = hash11(idx * 7.43 + 2.0);

    float phase  = TAU * seed1 + idx * (TAU / tendrilCount);
    float elev   = (seed2 - 0.5) * PI * 0.8;
    float spin   = t * spinRate * (0.8 + seed3 * 0.4);
    float wobble = 0.25 * sin(t * 0.40 * spinRate + idx * 2.3);

    float r   = stormRadius;
    vec3 base = r * 0.25 * vec3(cos(phase + spin), sin(elev), sin(phase + spin));
    vec3 tip  = r * vec3(cos(phase + spin + wobble), sin(elev + wobble * 0.5),
                         sin(phase + spin + wobble));
    return sdCapsule(p, base, tip, glowWidth);
}

float map(vec3 p, float t) {
    float d  = MAX_DIST;
    float nf = tendrilCount;
    for (int i = 0; i < 8; i++) {
        if (float(i) >= nf) break;
        d = min(d, tendrilDist(p, float(i), t));
    }
    return d;
}

vec3 getNormal(vec3 p, float t) {
    vec2 e = vec2(EPS, 0.0);
    return normalize(vec3(
        map(p + e.xyy, t) - map(p - e.xyy, t),
        map(p + e.yxy, t) - map(p - e.yxy, t),
        map(p + e.yyx, t) - map(p - e.yyx, t)
    ));
}

// Fully saturated palette cycling by tendril index
vec3 tendrilColor(float idx) {
    int ci = int(mod(idx, 3.0));
    if (ci == 0) return vec3(1.0, 0.05, 0.90); // magenta
    if (ci == 1) return vec3(0.0, 1.0,  0.85); // cyan
    return              vec3(1.0, 0.72, 0.0);  // gold
}

void main() {
    vec2 res = RENDERSIZE;
    vec2 uv  = (isf_FragNormCoord.xy - 0.5) * vec2(res.x / res.y, 1.0);
    float t  = TIME;

    // Audio — K ≤ 1.2 per motion rules §2
    float audio = clamp(audioLevel * audioReact, 0.0, 1.5);
    float aMult = 1.0 + audio * min(audioReact * 0.5, 1.2);

    // Slow-orbiting camera
    float camSpin  = t * 0.08;
    float camPitch = 0.28 + 0.14 * sin(t * 0.11);
    vec3 ro = 4.0 * vec3(cos(camSpin) * cos(camPitch), sin(camPitch),
                         sin(camSpin) * cos(camPitch));
    vec3 fwd   = normalize(-ro);
    vec3 right = normalize(cross(fwd, vec3(0.0, 1.0, 0.0)));
    vec3 up    = cross(right, fwd);
    vec3 rd    = normalize(fwd + uv.x * right + uv.y * up);

    // Void background
    vec3 col = vec3(0.002, 0.001, 0.005);

    // Raymarch
    float td = 0.0;
    float hitTd = -1.0;
    for (int i = 0; i < MAX_STEPS; i++) {
        vec3 pos = ro + rd * td;
        float d  = map(pos, t);
        if (d < EPS) { hitTd = td; break; }
        td += d;
        if (td > MAX_DIST) break;
    }

    if (hitTd > 0.0) {
        vec3 pos = ro + rd * hitTd;
        vec3 nor = getNormal(pos, t);

        // Find hit tendril
        float nf   = tendrilCount;
        float hitI = 0.0;
        float minD = MAX_DIST;
        for (int i = 0; i < 8; i++) {
            if (float(i) >= nf) break;
            float d = tendrilDist(pos, float(i), t);
            if (d < minD) { minD = d; hitI = float(i); }
        }
        vec3 baseCol = tendrilColor(hitI);

        // fwidth edge glow (AA)
        float edgeW = fwidth(hitTd) * 25.0;
        float rimMask = smoothstep(0.0, 0.25, edgeW);
        col = baseCol * hdrPeak * aMult * (1.0 - rimMask * 0.25);
        col += baseCol * rimMask * hdrPeak * 0.5;
    }

    // Soft volumetric glow for each tendril
    float nf = tendrilCount;
    for (int i = 0; i < 8; i++) {
        if (float(i) >= nf) break;
        float fi  = float(i);
        vec3  tc  = tendrilColor(fi);

        float s1  = hash11(fi * 5.31);
        float s2  = hash11(fi * 3.17 + 1.0);
        float s3  = hash11(fi * 7.43 + 2.0);
        float phase  = TAU * s1 + fi * (TAU / tendrilCount);
        float elev   = (s2 - 0.5) * PI * 0.8;
        float spin   = t * spinRate * (0.8 + s3 * 0.4);
        float wobble = 0.25 * sin(t * 0.40 * spinRate + fi * 2.3);
        float r = stormRadius;
        vec3 base = r * 0.25 * vec3(cos(phase + spin), sin(elev), sin(phase + spin));
        vec3 tip  = r * vec3(cos(phase + spin + wobble), sin(elev + wobble * 0.5),
                             sin(phase + spin + wobble));
        vec3 mid  = (base + tip) * 0.5;

        vec3 rToC  = mid - ro;
        float proj = clamp(dot(rToC, rd), 0.0, MAX_DIST);
        float cD   = length(rToC - rd * proj);
        float gw2  = glowWidth * glowWidth * 4.0;
        float g    = exp(-cD * cD / gw2) * 0.14;
        col += tc * g * hdrPeak * aMult * 0.6;
    }

    // Central plasma core
    vec3 rToO = -ro;
    float projO = dot(rToO, rd);
    float coreD = length(rToO - rd * clamp(projO, 0.0, MAX_DIST));
    float coreG = exp(-coreD * coreD * 1.8) * 0.22;
    vec3 coreCol = mix(vec3(1.0, 0.05, 0.90), vec3(0.0, 1.0, 0.85),
                       sin(t * 0.45) * 0.5 + 0.5);
    col += coreCol * coreG * hdrPeak * aMult;

    gl_FragColor = vec4(max(col, vec3(0.0)), 1.0);
}
