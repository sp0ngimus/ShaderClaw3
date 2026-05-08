/*{
  "DESCRIPTION": "Challah — a raymarched 3D braided challah loaf, 3 to 6 strands woven around a central axis, with golden crust, optional sesame seeds, smooth end-cap rolls, and an orbiting camera. Audio drives loaf swelling, braid spin, and a subtle high-frequency glaze shimmer.",
  "CREDIT": "ShaderClaw — Lu / Challah",
  "CATEGORIES": ["Generator", "3D", "Audio"],
  "INPUTS": [
    { "NAME": "strandCount",     "LABEL": "Strands",        "TYPE": "long",  "DEFAULT": 3,
      "VALUES": [3,4,5,6], "LABELS": ["3","4","5","6"] },
    { "NAME": "loafLength",      "LABEL": "Loaf Length",    "TYPE": "float", "DEFAULT": 2.4,  "MIN": 1.2,  "MAX": 4.0  },
    { "NAME": "braidRadius",     "LABEL": "Braid Radius",   "TYPE": "float", "DEFAULT": 0.55, "MIN": 0.25, "MAX": 0.95 },
    { "NAME": "threadRadius",    "LABEL": "Strand Thickness","TYPE": "float","DEFAULT": 0.30, "MIN": 0.12, "MAX": 0.55 },
    { "NAME": "twist",           "LABEL": "Twist Wraps",    "TYPE": "float", "DEFAULT": 1.6,  "MIN": 0.4,  "MAX": 4.0  },
    { "NAME": "morphSpeed",      "LABEL": "Braid Spin",     "TYPE": "float", "DEFAULT": 0.35, "MIN": 0.0,  "MAX": 2.0  },
    { "NAME": "orbitSpeed",      "LABEL": "Camera Orbit",   "TYPE": "float", "DEFAULT": 0.25, "MIN": 0.0,  "MAX": 2.0  },
    { "NAME": "camDist",         "LABEL": "Camera Dist",    "TYPE": "float", "DEFAULT": 5.5,  "MIN": 2.5,  "MAX": 10.0 },
    { "NAME": "camHeight",       "LABEL": "Camera Height",  "TYPE": "float", "DEFAULT": 1.4,  "MIN": -2.0, "MAX": 4.0  },
    { "NAME": "tiltAngle",       "LABEL": "Loaf Tilt",      "TYPE": "float", "DEFAULT": 0.10, "MIN": -0.6, "MAX": 0.6  },
    { "NAME": "crustNoise",      "LABEL": "Crust Texture",  "TYPE": "float", "DEFAULT": 0.55, "MIN": 0.0,  "MAX": 1.5  },
    { "NAME": "shine",           "LABEL": "Glaze Shine",    "TYPE": "float", "DEFAULT": 1.1,  "MIN": 0.0,  "MAX": 3.0  },
    { "NAME": "rimAmount",       "LABEL": "Rim Light",      "TYPE": "float", "DEFAULT": 0.35, "MIN": 0.0,  "MAX": 1.5  },
    { "NAME": "seedDensity",     "LABEL": "Sesame Seeds",   "TYPE": "float", "DEFAULT": 0.08, "MIN": 0.0,  "MAX": 0.5  },
    { "NAME": "seedScale",       "LABEL": "Seed Scale",     "TYPE": "float", "DEFAULT": 24.0, "MIN": 8.0,  "MAX": 60.0 },
    { "NAME": "audioBassSwell",  "LABEL": "Bass Swell",     "TYPE": "float", "DEFAULT": 0.7,  "MIN": 0.0,  "MAX": 3.0  },
    { "NAME": "audioMidSpin",    "LABEL": "Mid Spin",       "TYPE": "float", "DEFAULT": 0.6,  "MIN": 0.0,  "MAX": 3.0  },
    { "NAME": "audioHighGlaze",  "LABEL": "High Glaze",     "TYPE": "float", "DEFAULT": 0.6,  "MIN": 0.0,  "MAX": 3.0  },
    { "NAME": "bakeColor",       "LABEL": "Bread",          "TYPE": "color", "DEFAULT": [0.95, 0.66, 0.30, 1.0] },
    { "NAME": "crustColor",      "LABEL": "Crust",          "TYPE": "color", "DEFAULT": [0.42, 0.20, 0.08, 1.0] },
    { "NAME": "highlightColor",  "LABEL": "Glaze",          "TYPE": "color", "DEFAULT": [1.00, 0.92, 0.65, 1.0] },
    { "NAME": "seedColor",       "LABEL": "Sesame",         "TYPE": "color", "DEFAULT": [0.96, 0.91, 0.78, 1.0] },
    { "NAME": "bgColor",         "LABEL": "Background",     "TYPE": "color", "DEFAULT": [0.05, 0.04, 0.06, 1.0] },
    { "NAME": "transparentBg",   "LABEL": "Transparent BG", "TYPE": "bool",  "DEFAULT": 0.0  }
  ]
}*/

#ifdef GL_ES
precision highp float;
#endif

#define PI         3.14159265358979
#define TAU        6.28318530718
#define MAX_STEPS  96
#define MAX_DIST   24.0
#define HIT_EPS    0.0015
#define MAX_STRANDS 6

// ───────── hash / noise ─────────────────────────────────────
float hash11(float x){ return fract(sin(x * 127.1) * 43758.5453); }
float hash13(vec3 p){
    p = fract(p * vec3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yzx + 33.33);
    return fract((p.x + p.y) * p.z);
}
float vnoise3(vec3 p){
    vec3 i = floor(p);
    vec3 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float n000 = hash13(i + vec3(0,0,0));
    float n100 = hash13(i + vec3(1,0,0));
    float n010 = hash13(i + vec3(0,1,0));
    float n110 = hash13(i + vec3(1,1,0));
    float n001 = hash13(i + vec3(0,0,1));
    float n101 = hash13(i + vec3(1,0,1));
    float n011 = hash13(i + vec3(0,1,1));
    float n111 = hash13(i + vec3(1,1,1));
    return mix(
        mix(mix(n000, n100, f.x), mix(n010, n110, f.x), f.y),
        mix(mix(n001, n101, f.x), mix(n011, n111, f.x), f.y), f.z);
}
float fbm3(vec3 p){
    float v = 0.0, a = 0.5;
    for (int i = 0; i < 4; i++){ v += a * vnoise3(p); p = p * 2.07 + 11.3; a *= 0.5; }
    return v;
}

// ───────── helpers ─────────────────────────────────────────
float smin(float a, float b, float k){
    float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0);
    return mix(b, a, h) - k * h * (1.0 - h);
}
// Smooth max — needed to soften the strand length clamp. The hard
// max(ds, abs(p.x) - lL) used previously created an SDF crease at
// the loaf ends that fbm displacement amplified into rippled fractal
// raymarch artifacts on the right side of the loaf.
float smax(float a, float b, float k){
    float h = clamp(0.5 - 0.5 * (a - b) / k, 0.0, 1.0);
    return mix(a, b, h) + k * h * (1.0 - h);
}
mat2 rot(float a){ float s = sin(a), c = cos(a); return mat2(c, -s, s, c); }

// ───────── challah SDF ─────────────────────────────────────
// p is in object space, x along loaf axis.
// We approximate each strand as a ring whose center revolves around the loaf
// axis: strand i lies at offset bR * (sin(k*x + phase_i), cos(k*x + phase_i))
// in the y/z plane. Min over strands → braid envelope.
// End caps are smooth-min'd spheres so the ends roll closed.
float challahSDF(vec3 p){
    float bass = audioBass * audioBassSwell;
    float swell = 1.0 + bass * 0.18;
    float lL = loafLength * swell;
    float bR = braidRadius * swell;
    float tR = threadRadius * (1.0 + bass * 0.10);

    float k = twist * TAU / max(lL, 0.001);
    int   N = int(strandCount);
    float Nf = float(N);
    float spin = TIME * morphSpeed + bass * 0.7;

    float d = 1e10;
    for (int i = 0; i < MAX_STRANDS; i++){
        if (i >= N) break;
        float ph  = float(i) * TAU / Nf + spin;
        float ang = k * p.x + ph;
        vec2  c   = bR * vec2(sin(ang), cos(ang));
        float ds  = length(p.yz - c) - tR;
        // Smooth clamp to loaf length — using smax instead of max so the
        // strand→end-cap transition has a continuous gradient, killing
        // the fractal ripple artifacts that surface noise was amplifying.
        ds = smax(ds, abs(p.x) - lL, 0.10);
        d = min(d, ds);
    }

    // End-cap rolls (slightly larger than the braid envelope so ends pinch closed)
    float capR = tR + bR * 0.55;
    float dCapL = length(p - vec3(-lL, 0.0, 0.0)) - capR;
    float dCapR = length(p - vec3( lL, 0.0, 0.0)) - capR;
    d = smin(d, dCapL, 0.22);
    d = smin(d, dCapR, 0.22);

    // Crust micro-displacement (very subtle so the SDF stays Lipschitz —
    // amplitude × frequency was high enough to confuse the raymarch step
    // size and produce shimmering fractal patterns near silhouette edges).
    if (crustNoise > 0.001) {
        float nb = fbm3(p * 3.0);
        d -= (nb - 0.5) * 0.008 * crustNoise;
    }

    return d;
}

vec3 calcNormal(vec3 p){
    vec2 e = vec2(0.0012, 0.0);
    return normalize(vec3(
        challahSDF(p + e.xyy) - challahSDF(p - e.xyy),
        challahSDF(p + e.yxy) - challahSDF(p - e.yxy),
        challahSDF(p + e.yyx) - challahSDF(p - e.yyx)
    ));
}

float softShadow(vec3 ro, vec3 rd, float mint, float maxt, float kSh){
    float res = 1.0;
    float t = mint;
    for (int i = 0; i < 24; i++){
        if (t > maxt) break;
        float h = challahSDF(ro + rd * t);
        if (h < 0.001) return 0.0;
        res = min(res, kSh * h / t);
        t += clamp(h, 0.02, 0.20);
    }
    return clamp(res, 0.0, 1.0);
}

float ambientOcclusion(vec3 p, vec3 n){
    float occ = 0.0, sca = 1.0;
    for (int i = 0; i < 5; i++){
        float h = 0.02 + 0.08 * float(i);
        float d = challahSDF(p + n * h);
        occ += (h - d) * sca;
        sca *= 0.85;
    }
    return clamp(1.0 - 1.4 * occ, 0.0, 1.0);
}

void main(){
    vec2 res = RENDERSIZE;
    vec2 puv = (gl_FragCoord.xy - 0.5 * res) / res.y;

    float bass = audioBass;
    float mid  = audioMid;
    float high = audioHigh;

    // ── orbit camera ─────────────────────────────────────────
    float camAng = TIME * orbitSpeed * (1.0 + mid * audioMidSpin * 0.5);
    vec3 eye = vec3(sin(camAng) * camDist, camHeight, cos(camAng) * camDist);
    vec3 target = vec3(0.0, 0.0, 0.0);
    vec3 forward = normalize(target - eye);
    vec3 right   = normalize(cross(vec3(0.0, 1.0, 0.0), forward));
    vec3 up      = cross(forward, right);
    float fov = 1.2;
    vec3 ray = normalize(forward + right * puv.x * fov + up * puv.y * fov);

    // ── raymarch ──────────────────────────────────────────────
    float t = 0.0;
    float dHit = MAX_DIST;
    bool  hit = false;
    vec3  hp;
    for (int i = 0; i < MAX_STEPS; i++){
        hp = eye + ray * t;
        // Apply tilt to the sample point so the loaf appears tilted in world
        vec3 sp = hp;
        sp.xy = rot(tiltAngle) * sp.xy;
        float d = challahSDF(sp);
        if (d < HIT_EPS) { hit = true; dHit = t; break; }
        if (t > MAX_DIST) break;
        // 0.7 (down from 0.85) — smaller per-step advance so the raymarch
        // doesn't overshoot through the smoothed seam between strands and
        // end caps where the displaced SDF gradient is steepest.
        t += d * 0.7;
    }

    vec3 col = bgColor.rgb;
    float aOut = transparentBg ? 0.0 : 1.0;

    if (hit){
        // Recompute in object/tilt space for shading
        vec3 sp = hp;
        sp.xy = rot(tiltAngle) * sp.xy;
        vec3 n = calcNormal(sp);
        // un-tilt normal back to world for view-dependent terms
        vec3 nWorld = vec3(rot(-tiltAngle) * n.xy, n.z);

        vec3 viewDir = normalize(eye - hp);
        vec3 lightDir = normalize(vec3(0.55, 0.85, 0.50));
        float diff = max(dot(nWorld, lightDir), 0.0);
        float ao   = ambientOcclusion(sp, n);

        // Specular (Blinn)
        vec3 halfV = normalize(lightDir + viewDir);
        float spec = pow(max(dot(nWorld, halfV), 0.0), 38.0);
        float rim  = pow(1.0 - max(dot(nWorld, viewDir), 0.0), 3.0);

        // Surface noise → crust darkness modulation
        float crustN = fbm3(sp * 6.0);

        // Base bake colour: crust in shadow, bake in light
        vec3 base = mix(crustColor.rgb, bakeColor.rgb, diff);
        base = mix(base, crustColor.rgb, (1.0 - crustN) * 0.35 * crustNoise);

        // Sesame seeds: voronoi-ish hashes on surface in object space
        vec3 seedKey = floor(sp * seedScale);
        float sH = hash13(seedKey);
        // soft fall-off so seeds look like little ovals not perfect cells
        vec3 fseed = fract(sp * seedScale) - 0.5;
        float seedShape = smoothstep(0.42, 0.30, length(fseed));
        float seedHit = step(1.0 - seedDensity, sH) * seedShape;
        base = mix(base, seedColor.rgb, seedHit * 0.95);

        // Combine
        col = base * (0.30 + 0.85 * diff) * (0.55 + 0.45 * ao);
        col += highlightColor.rgb * spec * shine * (0.7 + 0.5 * crustN);
        col += highlightColor.rgb * rim  * rimAmount;

        // Audio high glaze shimmer
        col += highlightColor.rgb * spec * high * audioHighGlaze * 0.6;
        col *= 1.0 + bass * audioBassSwell * 0.06;

        // Distance fade into bg
        float fog = clamp(dHit / (MAX_DIST * 0.9), 0.0, 1.0);
        col = mix(col, bgColor.rgb, fog * fog * 0.4);

        aOut = 1.0;
    } else {
        // Soft radial vignette on the bg
        float v = length(puv);
        col = mix(bgColor.rgb, bgColor.rgb * 0.6, smoothstep(0.4, 1.2, v));
        if (transparentBg) aOut = 0.0;
    }

    gl_FragColor = vec4(col, aOut);
}
