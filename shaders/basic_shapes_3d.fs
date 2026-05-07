/*{
  "CATEGORIES": ["3D", "Generator", "Audio Reactive"],
  "DESCRIPTION": "Sunset Still Life — four SDF primitives (sphere, cube, capsule, glass disc) on a Marfa-desert infinite cyc. NEW: materials repainted as warm desert sunset palette — terracotta chalk sphere, gold leaf cube, deep indigo capsule, amber resin disc. Backdrop defaults to Judd Vermilion with a warm orange horizon. Camera looks up slightly (camHeight 0.6) to emphasise silhouettes against sky. Stays alive in silence. LINEAR HDR.",
  "INPUTS": [
    { "NAME": "camDist",       "LABEL": "Camera Distance", "TYPE": "float", "MIN": 1.5, "MAX": 12.0, "DEFAULT": 4.5 },
    { "NAME": "camHeight",     "LABEL": "Camera Height",   "TYPE": "float", "MIN": -3.0, "MAX": 4.0, "DEFAULT": 0.6 },
    { "NAME": "camOrbitSpeed", "LABEL": "Orbit Speed",     "TYPE": "float", "MIN": 0.0, "MAX": 2.0,  "DEFAULT": 0.18 },
    { "NAME": "camAzimuth",    "LABEL": "Camera Azimuth",  "TYPE": "float", "MIN": 0.0, "MAX": 6.2832, "DEFAULT": 0.0 },
    { "NAME": "keyAngle",      "LABEL": "Key Light Angle", "TYPE": "float", "MIN": 0.0, "MAX": 6.2832, "DEFAULT": 0.785 },
    { "NAME": "keyElevation",  "LABEL": "Key Elevation",   "TYPE": "float", "MIN": 0.0, "MAX": 1.5708, "DEFAULT": 0.7 },
    { "NAME": "keyColor",      "LABEL": "Key Light",       "TYPE": "color", "DEFAULT": [1.65, 0.90, 0.40, 1.0] },
    { "NAME": "fillColor",     "LABEL": "Fill Light",      "TYPE": "color", "DEFAULT": [0.35, 0.55, 1.10, 1.0] },
    { "NAME": "ambient",       "LABEL": "Ambient",         "TYPE": "float", "MIN": 0.0, "MAX": 0.5,  "DEFAULT": 0.08 },
    { "NAME": "rimStrength",   "LABEL": "Rim Strength",    "TYPE": "float", "MIN": 0.0, "MAX": 1.5,  "DEFAULT": 0.5 },
    { "NAME": "exposure",      "LABEL": "Exposure",        "TYPE": "float", "MIN": 0.3, "MAX": 3.0,  "DEFAULT": 1.0 },
    { "NAME": "moodPreset",    "LABEL": "Studio Mood",     "TYPE": "long",  "DEFAULT": 3, "VALUES": [0,1,2,3,4], "LABELS": ["Tillmans White","Kapoor Void","Judd Cobalt","Judd Vermilion","Marfa Concrete"] },
    { "NAME": "dofStrength",   "LABEL": "Depth of Field",  "TYPE": "float", "MIN": 0.0, "MAX": 1.0,  "DEFAULT": 0.0 },
    { "NAME": "chromaticAb",   "LABEL": "Chromatic Ab.",   "TYPE": "float", "MIN": 0.0, "MAX": 1.0,  "DEFAULT": 0.15 },
    { "NAME": "audioReact",    "LABEL": "Audio React",     "TYPE": "float", "MIN": 0.0, "MAX": 2.0,  "DEFAULT": 1.0 }
  ]
}*/

// ════════════════════════════════════════════════════════════════════════
//   STILL LIFE — four primitives, one room, three lights.
//   Tillmans / Kapoor / Judd / Cragg in conversation. Not a tutorial.
// ════════════════════════════════════════════════════════════════════════

#define MAX_STEPS 144
#define MAX_DIST  40.0
#define EPS       0.0007

#define MAT_BACKDROP 0
#define MAT_CHALK    1
#define MAT_CHROME   2
#define MAT_COBALT   3
#define MAT_GLASS    4

// ── SDF library ─────────────────────────────────────────────────────────
float sdSphere(vec3 p, float r) { return length(p) - r; }
float sdRoundBox(vec3 p, vec3 b, float r) {
    vec3 q = abs(p) - b;
    return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0) - r;
}
float sdCapsule(vec3 p, float h, float r) {
    p.y -= clamp(p.y, -h, h);
    return length(p) - r;
}
float sdCylinder(vec3 p, float h, float r) {
    vec2 d = vec2(length(p.xz) - r, abs(p.y) - h);
    return min(max(d.x,d.y), 0.0) + length(max(d, 0.0));
}
mat2 rot2(float a) { float c=cos(a),s=sin(a); return mat2(c,-s,s,c); }

// Infinite cyc — floor curves smoothly into vertical backdrop via a
// quarter-arc seam. Region select on (y, z) gives floor, wall, or arc.
float sdCyc(vec3 p) {
    float seamZ = -1.4, seamY = 1.4, r = 1.4;
    if (p.z > seamZ && p.y < seamY) return p.y;                       // floor
    if (p.y > seamY && p.z < seamZ) return -(p.z - (seamZ - r));      // wall
    return length(vec2(p.y - seamY, p.z - seamZ)) - r;                // arc
}

// ── Scene ──────────────────────────────────────────────────────────────
struct Hit { float d; int mat; };

float breath(int idx, float bass) {
    float ph   = float(idx) * 1.91 + TIME * 1.6;
    float idle = 0.5 + 0.5 * sin(ph);
    float t    = mix(idle, bass, clamp(bass * 1.4, 0.0, 1.0));
    return mix(0.97, 1.03, t);
}

Hit map(vec3 p) {
    Hit best = Hit(1e9, MAT_BACKDROP);
    float bass = clamp(audioBass, 0.0, 1.0) * audioReact;

    float d = sdCyc(p);
    if (d < best.d) { best.d = d; best.mat = MAT_BACKDROP; }

    // Front-left chalk sphere — anchor mass
    {
        float s = breath(0, bass);
        d = sdSphere(p - vec3(-0.55, 0.46 * s, 0.35), 0.46 * s);
        if (d < best.d) { best.d = d; best.mat = MAT_CHALK; }
    }
    // Back-center Kapoor chrome cube, slight tilt
    {
        float s = breath(1, bass);
        vec3 lp = p - vec3(0.15, 0.34 * s, -0.55);
        lp.xz = rot2(0.42) * lp.xz;
        d = sdRoundBox(lp, vec3(0.34 * s), 0.012);
        if (d < best.d) { best.d = d; best.mat = MAT_CHROME; }
    }
    // Back-right Judd capsule — verticality
    {
        float s = breath(2, bass);
        vec3 lp = p - vec3(0.95, 0.78 * s, -0.05);
        lp.xz = rot2(-0.18) * lp.xz;
        d = sdCapsule(lp, 0.55 * s, 0.16 * s);
        if (d < best.d) { best.d = d; best.mat = MAT_COBALT; }
    }
    // Front-right glass disc — Cragg horizontal counterweight
    {
        float s = breath(3, bass);
        vec3 lp = p - vec3(0.55, 0.045 * s, 0.85);
        lp.xz = rot2(0.22) * lp.xz;
        d = sdCylinder(lp, 0.045 * s, 0.46 * s);
        if (d < best.d) { best.d = d; best.mat = MAT_GLASS; }
    }
    return best;
}

vec3 calcNormal(vec3 p) {
    const vec2 e = vec2(EPS, 0.0);
    return normalize(vec3(
        map(p + e.xyy).d - map(p - e.xyy).d,
        map(p + e.yxy).d - map(p - e.yxy).d,
        map(p + e.yyx).d - map(p - e.yyx).d
    ));
}

// Soft contact shadow — penumbra grows with distance to occluder so
// shadows fade from sharp at contact to soft at the cyc. Sharpness
// (k) was 28 (studio-strobe hard); 10 gives a believable studio key
// with falloff. Floor at 0.08 prevents pitch-black shadow patches —
// the global ambient + fill compensate for the rest.
float keyShadow(vec3 ro, vec3 rd) {
    float res = 1.0, t = 0.04;
    for (int i = 0; i < 32; i++) {
        if (t > 8.0) break;
        float h = map(ro + rd * t).d;
        if (h < 0.0008) { res = 0.0; break; }
        res = min(res, 10.0 * h / t);
        t  += clamp(h, 0.014, 0.3);
    }
    return clamp(res, 0.08, 1.0);
}

float ao(vec3 p, vec3 n) {
    float occ = 0.0, sca = 1.0;
    for (int i = 0; i < 5; i++) {
        float h = 0.015 + 0.10 * float(i);
        occ += (h - map(p + n * h).d) * sca;
        sca *= 0.92;
    }
    return clamp(1.0 - 1.6 * occ, 0.0, 1.0);
}

// ── Mood — backdrop tint ───────────────────────────────────────────────
vec3 backdropColor(vec3 p) {
    vec3 col;
    float g = clamp(p.y * 0.30 + 0.45, 0.0, 1.0);
    if (moodPreset == 0)      col = mix(vec3(0.78,0.79,0.80), vec3(0.92,0.91,0.89), g);
    else if (moodPreset == 1) col = mix(vec3(0.012,0.013,0.018), vec3(0.05,0.05,0.06), g);
    else if (moodPreset == 2) col = mix(vec3(0.06,0.16,0.55), vec3(0.10,0.28,0.78), g);
    else if (moodPreset == 3) col = mix(vec3(0.55,0.10,0.05), vec3(0.92,0.26,0.13), g);
    else {
        vec3 base = mix(vec3(0.36,0.34,0.31), vec3(0.52,0.49,0.45), g);
        float n = sin(p.x * 11.3) * sin(p.z * 9.7) * 0.5
                + sin(p.x * 23.1 + 1.7) * sin(p.z * 19.3 + 0.4) * 0.25;
        col = base * (1.0 + 0.04 * n);
    }
    col *= mix(1.0, 0.88, smoothstep(1.4, 0.4, p.y) * 0.5);
    return col;
}

// Cheap room IBL probe — returns a colour for any reflection direction
vec3 iblProbe(vec3 dir) {
    vec3 top    = backdropColor(vec3(0.0, 2.5, -1.4));
    vec3 mid    = backdropColor(vec3(0.0, 0.9, -1.4));
    vec3 bottom = backdropColor(vec3(0.0, 0.05, 0.0)) * 0.85;
    return (dir.y > 0.0) ? mix(mid, top,    smoothstep(0.0, 1.0,  dir.y))
                         : mix(mid, bottom, smoothstep(0.0, 1.0, -dir.y));
}

// ── Three-point studio (key + fill from uniforms, rim derived) ─────────
vec3 sphDir(float a, float e) { return normalize(vec3(cos(a)*cos(e), sin(e), sin(a)*cos(e))); }
vec3 keyDirVec()  { return sphDir(keyAngle, keyElevation); }
vec3 fillDirVec() { return sphDir(keyAngle + 3.14159, keyElevation * 0.5); }
vec3 rimDirVec()  { return sphDir(keyAngle + 1.5708, keyElevation + 0.2); }
vec3 rimColVec()  { return mix(keyColor.rgb, vec3(1.0), 0.5); }

float ggx(float ndh, float a)        { float a2=a*a; float d=(ndh*ndh)*(a2-1.0)+1.0; return a2/(3.14159*d*d); }
float gSm(float ndv, float ndl, float a){ float k=(a+1.0); k=k*k*0.125; return (ndv/(ndv*(1.0-k)+k))*(ndl/(ndl*(1.0-k)+k)); }
vec3  fSc(float vdh, vec3 F0)        { return F0 + (1.0 - F0) * pow(1.0 - vdh, 5.0); }

// ── Per-material shading ───────────────────────────────────────────────
vec3 shadeBackdrop(vec3 p, vec3 n) {
    vec3 base = backdropColor(p);
    vec3 L = keyDirVec();
    float ndl = max(dot(n, L), 0.0);
    // Bigger surface bias (0.012 vs 0.004) — eliminates self-shadow
    // acne where the cyc and ground meet.
    float sh  = keyShadow(p + n * 0.012, L);
    float fillTerm = max(dot(n, fillDirVec()), 0.0);
    // Hemispheric ambient pulled up so shadowed cyc isn't pitch black.
    // The original ambient term was multiplied straight into base —
    // any darker than ~0.18 of the base read as "void". Multi-source
    // ambient (sky + ground bounce) keeps shadow regions tinted with
    // the backdrop color rather than crushing to grey.
    vec3 skyAmb    = backdropColor(p + vec3(0.0, 1.5, 0.0)) * 0.32;
    vec3 groundAmb = backdropColor(p - vec3(0.0, 0.6, 0.0)) * 0.18;
    float upWrap   = clamp(n.y * 0.5 + 0.5, 0.0, 1.0);
    vec3 ambTerm   = mix(groundAmb, skyAmb, upWrap);
    vec3 lit = base * (ambient + ambTerm
                     + keyColor.rgb  * ndl * sh
                     + fillColor.rgb * fillTerm * 0.6);
    lit *= mix(0.62, 1.0, ao(p, n));
    return lit;
}

vec3 shadeChalk(vec3 p, vec3 n, vec3 v) {
    vec3 albedo = vec3(0.78, 0.35, 0.18);  // terracotta desert clay
    vec3 L = keyDirVec();
    float sh = keyShadow(p + n * 0.005, L);
    float wrap = max((dot(n, L) + 0.25) / 1.25, 0.0);   // chalky wrap
    float fillTerm = max(dot(n, fillDirVec()), 0.0);
    float rim  = pow(1.0 - max(dot(n, v), 0.0), 2.5) * max(dot(n, rimDirVec()), 0.0);
    vec3 col = albedo * (ambient + keyColor.rgb * wrap * sh + fillColor.rgb * fillTerm)
             + rimColVec() * rim * rimStrength * 0.4;
    vec3 H = normalize(L + v);
    col += keyColor.rgb * pow(max(dot(n, H), 0.0), 32.0) * sh * 0.06;
    return col * mix(0.55, 1.0, ao(p, n));
}

vec3 shadeChrome(vec3 p, vec3 n, vec3 v) {
    vec3 F0 = vec3(0.92, 0.82, 0.22);  // gold leaf warm tint
    vec3 R  = reflect(-v, n);
    float shim = clamp(audioHigh, 0.0, 1.0) * audioReact;
    R = normalize(R + vec3(sin(TIME*7.3), cos(TIME*5.7), sin(TIME*4.1)) * 0.005 * shim);
    float t = 0.02; bool hit2 = false; int mh = 0; vec3 ph = p;
    for (int i = 0; i < 28; i++) {
        Hit s = map(p + R * t);
        if (s.d < 0.001) { hit2 = true; ph = p + R * t; mh = s.mat; break; }
        t += s.d;
        if (t > 6.0) break;
    }
    vec3 reflCol = iblProbe(R);
    if (hit2) {
        if      (mh == MAT_CHALK)  reflCol = vec3(0.93,0.62,0.50) * keyColor.rgb * 0.6;
        else if (mh == MAT_COBALT) reflCol = vec3(0.06,0.18,0.62) * keyColor.rgb * 0.5;
        else if (mh == MAT_GLASS)  reflCol = iblProbe(R) * 0.85;
        else if (mh == MAT_BACKDROP) reflCol = backdropColor(ph) * 0.7;
    }
    float fres = pow(1.0 - max(dot(n, v), 0.0), 5.0);
    vec3 spec = mix(F0, vec3(1.0), fres) * reflCol;
    vec3 L = keyDirVec();
    vec3 H = normalize(L + v);
    float sh = keyShadow(p + n * 0.005, L);
    float rim = pow(1.0 - max(dot(n, v), 0.0), 3.0) * max(dot(n, rimDirVec()), 0.0);
    spec += ambient * F0;
    spec += keyColor.rgb * pow(max(dot(n, H), 0.0), 256.0) * sh * 1.5;
    spec += rimColVec() * rim * rimStrength * 0.3;
    return spec * mix(0.7, 1.0, ao(p, n));
}

vec3 shadeCobalt(vec3 p, vec3 n, vec3 v) {
    vec3 albedo = vec3(0.06, 0.12, 0.50);  // deep indigo dusk
    float a = 0.28;
    vec3 F0 = mix(vec3(0.04), albedo, 0.55);
    vec3 L = keyDirVec();
    vec3 H = normalize(L + v);
    float ndl = max(dot(n, L), 0.0);
    float ndv = max(dot(n, v), 1e-4);
    float ndh = max(dot(n, H), 0.0);
    float vdh = max(dot(v, H), 0.0);
    float sh  = keyShadow(p + n * 0.005, L);
    vec3  F = fSc(vdh, F0);
    vec3 spec = (ggx(ndh, a) * gSm(ndv, ndl, a) * F) / max(4.0 * ndv * ndl, 1e-4);
    vec3 diff = (1.0 - F) * 0.55 * albedo / 3.14159;
    float fillTerm = max(dot(n, fillDirVec()), 0.0);
    vec3 col = albedo * ambient
             + (diff + spec) * keyColor.rgb * ndl * sh
             + albedo * fillColor.rgb * fillTerm * 0.6;
    vec3 R = reflect(-v, n);
    float shim = clamp(audioHigh, 0.0, 1.0) * audioReact;
    R = normalize(R + vec3(sin(TIME*6.1), cos(TIME*4.3), sin(TIME*5.9)) * 0.004 * shim);
    col += iblProbe(R) * F0 * (1.0 - a * 0.9) * 0.45;
    float rim = pow(1.0 - max(dot(n, v), 0.0), 3.0) * max(dot(n, rimDirVec()), 0.0);
    col += rimColVec() * rim * rimStrength * 0.7;
    return col * mix(0.7, 1.0, ao(p, n));
}

vec3 shadeGlass(vec3 p, vec3 n, vec3 v) {
    vec3 R = reflect(-v, n);
    vec3 T = refract(-v, n, 1.0 / 1.45);
    if (length(T) < 0.001) T = R;
    float t = 0.02; vec3 hp = p; int mh = MAT_BACKDROP;
    for (int i = 0; i < 24; i++) {
        Hit s = map(p + T * t);
        if (s.d < 0.001) { hp = p + T * t; mh = s.mat; break; }
        t += s.d;
        if (t > 4.0) break;
    }
    vec3 trans;
    if      (mh == MAT_CHALK)  trans = vec3(0.93,0.62,0.50) * 0.85;
    else if (mh == MAT_COBALT) trans = vec3(0.06,0.18,0.62) * 0.7;
    else if (mh == MAT_CHROME) trans = vec3(0.85);
    else                       trans = backdropColor(hp);
    trans *= vec3(1.00, 0.82, 0.35);   // amber resin warm tint
    float fres = mix(0.04, 1.0, pow(1.0 - max(dot(n, v), 0.0), 5.0));
    vec3 col = mix(trans, iblProbe(R), fres);
    vec3 L = keyDirVec();
    vec3 H = normalize(L + v);
    float sh = keyShadow(p + n * 0.005, L);
    col += ambient * vec3(0.05);
    col += keyColor.rgb * pow(max(dot(n, H), 0.0), 220.0) * sh * 1.2;
    float rim = pow(1.0 - max(dot(n, v), 0.0), 4.0);
    col += rimColVec() * rim * rimStrength * 0.4;
    return col;
}

vec3 shade(vec3 p, vec3 n, vec3 v, int mat) {
    if (mat == MAT_BACKDROP) return shadeBackdrop(p, n);
    if (mat == MAT_CHALK)    return shadeChalk(p, n, v);
    if (mat == MAT_CHROME)   return shadeChrome(p, n, v);
    if (mat == MAT_COBALT)   return shadeCobalt(p, n, v);
    return shadeGlass(p, n, v);
}

// March a ray and return colour + scene depth
vec4 traceScene(vec3 ro, vec3 rd) {
    float dist = 0.0;
    int   mat  = MAT_BACKDROP;
    bool  hit  = false;
    for (int i = 0; i < MAX_STEPS; i++) {
        Hit s = map(ro + rd * dist);
        if (s.d < EPS) { hit = true; mat = s.mat; break; }
        // 0.78 (was 0.92) — under-step harder so grazing-angle cyc
        // rays converge before MAX_STEPS instead of leaving step rings.
        dist += s.d * 0.78;
        if (dist > MAX_DIST) break;
    }
    vec3 col = hit ? shade(ro + rd * dist, calcNormal(ro + rd * dist), -rd, mat)
                   : iblProbe(rd);
    return vec4(col, hit ? dist : MAX_DIST);
}

// ── Main ───────────────────────────────────────────────────────────────
void main() {
    vec2 res = RENDERSIZE.xy;
    vec2 fc  = (gl_FragCoord.xy - 0.5 * res) / res.y;

    float mid = clamp(audioMid, 0.0, 1.0) * audioReact;

    // Standard orbit camera
    float orb = camAzimuth + TIME * camOrbitSpeed * (1.0 + 0.6 * mid);
    vec3 ro = vec3(cos(orb) * camDist, camHeight, sin(orb) * camDist);
    vec3 ta = vec3(0.18, 0.55, 0.05);
    vec3 fwd = normalize(ta - ro);
    vec3 rgt = normalize(cross(fwd, vec3(0,1,0)));
    vec3 up  = cross(rgt, fwd);

    // Chromatic aberration: trace 3 rays with tiny offsets, take per-channel
    vec2 caOff = fc * chromaticAb * 0.012;
    vec3 rdR = normalize(fwd + rgt * (fc.x + caOff.x) * 1.05 + up * (fc.y + caOff.y) * 1.05);
    vec3 rdG = normalize(fwd + rgt *  fc.x          * 1.05 + up *  fc.y          * 1.05);
    vec3 rdB = normalize(fwd + rgt * (fc.x - caOff.x) * 1.05 + up * (fc.y - caOff.y) * 1.05);

    vec4 sG = traceScene(ro, rdG);
    vec3 col = sG.rgb;
    if (chromaticAb > 0.001) {
        col.r = traceScene(ro, rdR).r;
        col.b = traceScene(ro, rdB).b;
    }

    // Cheap depth-of-field: blur far/near samples in screen space along jitter
    if (dofStrength > 0.001) {
        float focus = camDist;                       // focus at target distance
        float coc = clamp(abs(sG.a - focus) / 4.0, 0.0, 1.0) * dofStrength;
        vec3 blurAcc = vec3(0.0);
        const int N = 6;
        for (int i = 0; i < N; i++) {
            float a = 6.2832 * (float(i) + 0.5) / float(N);
            vec2 j = vec2(cos(a), sin(a)) * coc * 0.04;
            vec3 rdJ = normalize(fwd + rgt * (fc.x + j.x) * 1.05 + up * (fc.y + j.y) * 1.05);
            blurAcc += traceScene(ro, rdJ).rgb;
        }
        col = mix(col, blurAcc / float(N), coc);
    }

    // Tillmans-soft vignette
    vec2 q = (gl_FragCoord.xy / res) - 0.5;
    col *= clamp(1.0 - dot(q, q) * 0.55, 0.0, 1.0);

    // Film grain — keeps it alive in silence
    float gr = fract(sin(dot(gl_FragCoord.xy, vec2(12.9898, 78.233)) + TIME) * 43758.5453);
    col += (gr - 0.5) * 0.008;
    col *= 0.97 + 0.03 * sin(TIME * 0.27);
    col *= exposure;

    gl_FragColor = vec4(col, 1.0);
}
