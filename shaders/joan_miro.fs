/*{
  "CATEGORIES": ["Generator", "Art Movement", "Audio Reactive"],
  "DESCRIPTION": "Joan Miró surrealist dreamscape — biomorphic creatures, primary-colour blobs, and bold sinuous black lines float on a warm two-tone ground. After The Harlequin's Carnival (1924), Dutch Interior I (1928), The Hunter — Catalan Landscape (1923), and Constellations (1941).",
  "INPUTS": [
    { "NAME": "miroWork",    "LABEL": "Painting",      "TYPE": "long",  "DEFAULT": 0, "VALUES": [0,1,2,3], "LABELS": ["Harlequin's Carnival (1924)","Dutch Interior I (1928)","The Hunter (1923)","Constellations (1941)"] },
    { "NAME": "blobCount",   "LABEL": "Blobs",          "TYPE": "float", "MIN": 4.0,  "MAX": 18.0, "DEFAULT": 11.0 },
    { "NAME": "lineWaves",   "LABEL": "Black Lines",    "TYPE": "float", "MIN": 1.0,  "MAX": 6.0,  "DEFAULT": 3.0  },
    { "NAME": "eyeCount",    "LABEL": "Eyes",           "TYPE": "float", "MIN": 0.0,  "MAX": 8.0,  "DEFAULT": 4.0  },
    { "NAME": "starCount",   "LABEL": "Stars",          "TYPE": "float", "MIN": 0.0,  "MAX": 8.0,  "DEFAULT": 5.0  },
    { "NAME": "driftSpeed",  "LABEL": "Drift Speed",    "TYPE": "float", "MIN": 0.0,  "MAX": 1.0,  "DEFAULT": 0.18 },
    { "NAME": "groundSplit", "LABEL": "Horizon",        "TYPE": "float", "MIN": 0.3,  "MAX": 0.7,  "DEFAULT": 0.48 },
    { "NAME": "audioReact",  "LABEL": "Audio React",    "TYPE": "float", "MIN": 0.0,  "MAX": 2.0,  "DEFAULT": 1.0  },
    { "NAME": "seed",        "LABEL": "Seed",           "TYPE": "float", "MIN": 0.0,  "MAX": 50.0, "DEFAULT": 0.0  },
    { "NAME": "transparentBg", "LABEL": "Transparent BG", "TYPE": "bool", "DEFAULT": false }
  ]
}*/

float h11(float n) { return fract(sin(n * 127.1) * 43758.5453); }

float sdCircle(vec2 p, float r) { return length(p) - r; }

// Smooth minimum — used for organic blob merging (no atan, no branch cuts)
float smin(float a, float b, float k) {
    float h = max(k - abs(a - b), 0.0) / k;
    return min(a, b) - h * h * k * 0.25;
}

// Organic blob: smooth union of a main body + two slowly-orbiting satellite lobes.
// Purely Cartesian — avoids atan and its precision edge cases.
float sdOrganic(vec2 p, float r, float fi, float T) {
    float d  = sdCircle(p, r * 0.72);
    float a1 = h11(fi * 11.7) * 6.2832 + T * 0.12 * (h11(fi * 13.3) * 2.0 - 1.0);
    float a2 = h11(fi * 31.1) * 6.2832 + T * 0.10 * (h11(fi * 37.3) * 2.0 - 1.0);
    vec2  s1 = vec2(cos(a1), sin(a1)) * r * (0.35 + h11(fi * 17.7) * 0.35);
    vec2  s2 = vec2(cos(a2), sin(a2)) * r * (0.30 + h11(fi * 41.7) * 0.30);
    float r1 = r * (0.30 + h11(fi * 23.3) * 0.30);
    float r2 = r * (0.25 + h11(fi * 47.1) * 0.25);
    d = smin(d, sdCircle(p - s1, r1), r * 0.45);
    d = smin(d, sdCircle(p - s2, r2), r * 0.40);
    return d;
}

// 5-pointed star SDF (iq formulation, epsilon on sign to avoid zero)
float sdStar5(vec2 p, float r) {
    const float PI = 3.14159265;
    const float an = PI / 5.0;
    const float en = PI / 2.5;
    vec2 acs = vec2(cos(an), sin(an));
    vec2 ecs = vec2(cos(en), sin(en));
    float bn = mod(atan(p.x, p.y), 2.0 * an) - an;
    p = length(p) * vec2(cos(bn), abs(sin(bn)));
    p -= r * acs;
    p += ecs * clamp(-dot(p, ecs), 0.0, r * acs.y / ecs.y);
    return length(p) * sign(p.x + 1e-5);
}

// Crescent: outer circle with inner circle subtracted
float sdCrescent(vec2 p, float rOut, vec2 innerOff, float rIn) {
    return max(sdCircle(p, rOut), -sdCircle(p - innerOff, rIn));
}

// All smoothstep calls use proper lo < hi form to avoid GLSL ES undefined behaviour.
// For "filled inside" patterns we use:  1.0 - smoothstep(lo, hi, sd)
// For "outline ring" patterns we use the same but on an expanded SDF.

void main() {
    vec2  uv  = gl_FragCoord.xy / RENDERSIZE.xy;
    float asp = RENDERSIZE.x / max(RENDERSIZE.y, 1.0);
    vec2  P   = vec2(uv.x * asp, uv.y);

    float T = TIME * driftSpeed;
    float S = seed;

    // ── Palette ───────────────────────────────────────────────────────────────
    vec3 mBlk = vec3(0.055, 0.042, 0.038);
    vec3 mWht = vec3(0.960, 0.945, 0.910);
    vec3 mRed = vec3(0.878, 0.095, 0.110);
    vec3 mYel = vec3(0.972, 0.820, 0.058);
    vec3 mBlu = vec3(0.120, 0.215, 0.680);
    vec3 mBrn = vec3(0.400, 0.200, 0.090);
    vec3 mGry = vec3(0.560, 0.560, 0.570);

    // ── Painting backgrounds ──────────────────────────────────────────────────
    vec3 topCol, botCol;
    int  kw = int(miroWork);
    if (kw == 1) {
        topCol = vec3(0.900, 0.878, 0.820);
        botCol = vec3(0.520, 0.295, 0.165);
    } else if (kw == 2) {
        topCol = vec3(0.850, 0.800, 0.600);
        botCol = vec3(0.740, 0.530, 0.260);
    } else if (kw == 3) {
        topCol = vec3(0.060, 0.060, 0.120);
        botCol = vec3(0.025, 0.025, 0.060);
    } else {
        topCol = vec3(0.918, 0.858, 0.729);
        botCol = vec3(0.671, 0.376, 0.224);
    }

    // Single-harmonic horizon wave (two harmonics caused inter-wave beating)
    float split = groundSplit + 0.018 * sin(uv.x * 5.71 + T * 0.4);
    vec3  col   = mix(botCol, topCol, smoothstep(split - 0.012, split + 0.012, uv.y));

    // ── Biomorphic blobs — painter's algorithm ────────────────────────────────
    int N = int(clamp(blobCount, 1.0, 18.0));
    for (int i = 0; i < 18; i++) {
        if (i >= N) break;
        float fi = float(i) + S * 1.618;

        vec2 home = vec2(
            0.08 + h11(fi * 1.31) * 0.84,
            0.08 + h11(fi * 2.73) * 0.84
        );
        home.x *= asp;
        vec2 drift = vec2(
            sin(T * (0.28 + h11(fi * 3.07) * 0.35) + fi * 2.11),
            cos(T * (0.21 + h11(fi * 4.69) * 0.28) + fi * 1.57)
        ) * (0.045 + audioLevel * audioReact * 0.012);

        vec2  ctr = home + drift;
        float r   = (0.035 + h11(fi * 5.53) * 0.065)
                  * (1.0 + audioBass * audioReact * 0.12);
        float sd  = sdOrganic(P - ctr, r, fi, T);

        int bci = int(mod(float(i) + S * 0.37, 6.0));
        vec3 blobCol;
        if      (bci == 0) blobCol = mRed;
        else if (bci == 1) blobCol = mYel;
        else if (bci == 2) blobCol = mBlu;
        else if (bci == 3) blobCol = mWht;
        else if (bci == 4) blobCol = mBrn;
        else               blobCol = mGry;

        // Outline: ring between (sd+0.009) and (sd+0.003)
        float outline = (1.0 - smoothstep(0.0, 0.006, sd + 0.003))
                      * smoothstep(0.0, 0.006, sd + 0.009);
        col = mix(col, mBlk,    outline);
        // Fill: inside sd = 0
        col = mix(col, blobCol, 1.0 - smoothstep(-0.004, 0.004, sd));
    }

    // ── Eyes ─────────────────────────────────────────────────────────────────
    int NE = int(clamp(eyeCount, 0.0, 8.0));
    for (int i = 0; i < 8; i++) {
        if (i >= NE) break;
        float fi = float(i) * 3.14159 + S * 2.718;

        vec2 home = vec2(
            0.08 + h11(fi * 11.37) * 0.84,
            0.08 + h11(fi * 13.71) * 0.84
        );
        home.x *= asp;
        vec2 ctr = home + vec2(sin(T * 0.7 + fi), cos(T * 0.55 + fi * 1.3)) * 0.035;

        float rOut = 0.025 + h11(fi * 17.13) * 0.025;
        float rIn  = rOut * 0.55;
        float rPup = rIn  * 0.48;

        float dOut = sdCircle(P - ctr, rOut);
        float dIn  = sdCircle(P - ctr, rIn);
        float dPup = sdCircle(P - ctr, rPup);

        float outlineW = 0.005;
        col = mix(col, mBlk, (1.0 - smoothstep(0.0, outlineW, dOut + outlineW))
                            * smoothstep(0.0, outlineW, dOut));
        col = mix(col, mWht, 1.0 - smoothstep(-0.003, 0.003, dOut));

        int ic = int(mod(fi * 2.3, 3.0));
        vec3 iris = (ic == 0) ? mBlu : (ic == 1) ? mRed : mYel;
        col = mix(col, iris, 1.0 - smoothstep(-0.002, 0.002, dIn));
        col = mix(col, mBlk, 1.0 - smoothstep(-0.0015, 0.0015, dPup));
    }

    // ── Stars ────────────────────────────────────────────────────────────────
    int NS = int(clamp(starCount, 0.0, 8.0));
    for (int i = 0; i < 8; i++) {
        if (i >= NS) break;
        float fi = float(i) * 5.555 + S * 0.993;

        vec2 home = vec2(
            0.06 + h11(fi * 23.13) * 0.88,
            0.06 + h11(fi * 29.71) * 0.88
        );
        home.x *= asp;
        vec2 ctr = home + vec2(sin(T * 0.5 + fi * 1.2), cos(T * 0.4 + fi)) * 0.03;

        float rot = T * 0.45 * (h11(fi * 31.3) * 2.0 - 1.0) + h11(fi * 37.7) * 6.2832;
        float ca = cos(rot), sa = sin(rot);
        vec2 lp = vec2(ca * (P.x - ctr.x) - sa * (P.y - ctr.y),
                       sa * (P.x - ctr.x) + ca * (P.y - ctr.y));

        float r  = (0.014 + h11(fi * 41.3) * 0.018)
                 * (1.0 + audioHigh * audioReact * 0.25);
        float sd = sdStar5(lp, r);

        int sc = int(mod(fi * 3.11, 3.0));
        vec3 starCol = (sc == 0) ? mYel : (sc == 1) ? mWht : mRed;

        col = mix(col, mBlk,    (1.0 - smoothstep(0.0, 0.004, sd + 0.004))
                               * smoothstep(0.0, 0.004, sd));
        col = mix(col, starCol, 1.0 - smoothstep(-0.002, 0.002, sd));
    }

    // ── Crescent moon ────────────────────────────────────────────────────────
    {
        vec2  moonCtr = vec2(0.76 * asp, 0.70 + 0.025 * sin(T * 0.28));
        float mSD = sdCrescent(P - moonCtr, 0.052,
                               vec2(0.024 + 0.006 * sin(T * 0.2), 0.0), 0.046);
        col = mix(col, mBlk, (1.0 - smoothstep(0.0, 0.005, mSD + 0.005))
                            * smoothstep(0.0, 0.005, mSD));
        col = mix(col, mYel, 1.0 - smoothstep(-0.003, 0.003, mSD));
    }

    // ── Bold sinuous black lines ──────────────────────────────────────────────
    // Single sine wave per line — a second harmonic at a different phase rate
    // creates a beat envelope that makes the line intermittently vanish.
    // Miró's lines are grand sweeping curves, not high-frequency wiggles.
    int NL = int(clamp(lineWaves, 1.0, 6.0));
    for (int k = 0; k < 6; k++) {
        if (k >= NL) break;
        float fk = float(k) + S * 1.4142;

        float baseY = 0.20 + h11(fk * 41.37) * 0.60;
        float amp   = 0.06 + h11(fk * 43.71) * 0.09;   // 0.06–0.15
        float frq   = 0.8  + h11(fk * 47.13) * 1.2;    // 0.8–2.0 cycles (was up to 4.5)
        float ph    = T * (0.20 + h11(fk * 61.33) * 0.25) + fk * 2.1;

        float targetY = baseY + amp * sin(frq * uv.x * 6.2832 + ph);
        float dist    = abs(uv.y - targetY);

        float lw = (0.007 + h11(fk * 71.33) * 0.006)
                 * (1.0 + audioMid * audioReact * 0.35);
        col = mix(col, mBlk, 1.0 - smoothstep(lw * 0.25, lw, dist));
    }

    // ── Thin antennae / tendrils ──────────────────────────────────────────────
    for (int k = 0; k < 10; k++) {
        float fk  = float(k) * 7.77 + S * 0.37;
        vec2  p0  = vec2(h11(fk * 73.13) * asp, h11(fk * 79.37));
        float ang = h11(fk * 83.71) * 6.2832 + T * 0.15 * (h11(fk * 89.3) * 2.0 - 1.0);
        vec2  dir = vec2(cos(ang), sin(ang));
        float len = 0.08 + h11(fk * 97.3) * 0.18;

        vec2  pa   = P - p0;
        float proj = clamp(dot(pa, dir), 0.0, len);
        float d    = length(pa - dir * proj);
        col = mix(col, mBlk, (1.0 - smoothstep(0.0, 0.003, d)) * 0.9);
    }

    // ── Scattered dots ────────────────────────────────────────────────────────
    for (int k = 0; k < 14; k++) {
        float fk  = float(k) * 13.37 + S * 2.54;
        vec2  ctr = vec2(h11(fk * 89.17) * asp, h11(fk * 97.33));
        ctr += vec2(sin(T * 0.5 + fk), cos(T * 0.4 + fk * 1.1)) * 0.018;

        float sd = sdCircle(P - ctr, 0.004 + h11(fk * 101.7) * 0.007);

        int dc = int(mod(fk * 2.11, 4.0));
        vec3 dotCol = (dc == 0) ? mBlk
                    : (dc == 1) ? mRed
                    : (dc == 2) ? mYel
                    : mBlu;
        col = mix(col, dotCol, 1.0 - smoothstep(-0.0012, 0.0012, sd));
    }

    gl_FragColor = vec4(col, 1.0);
}
