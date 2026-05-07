/*{
  "CATEGORIES": ["Generator", "Art Movement", "Audio Reactive", "3D"],
  "DESCRIPTION": "Bauhaus after Kandinsky (3D) — volumetric SDF installation: a raymarched studio space where the yellow triangle becomes a 3D tetrahedron, the red square a polished lacquer box, the blue circle a reflective sphere. Each floats on a white plinth in a dark gallery with studio 3-point lighting (warm gilt key, cool fill, rim). Five Kandinsky works cycle the palette and composition. Bass pulses shape scale; mids drive orbit; treble shimmers specular. LINEAR HDR peaks 2.5+ on specular tips. Calm defaults.",
  "INPUTS": [
    { "NAME": "kandinskyWork", "LABEL": "Painting", "TYPE": "long", "DEFAULT": 0, "VALUES": [0,1,2,3,4], "LABELS": ["Composition VIII (1923)","Several Circles (1926)","Yellow Red Blue (1925)","Composition X (1939)","On White II (1923)"] },
    { "NAME": "camDist",       "LABEL": "Camera Distance", "TYPE": "float", "MIN": 2.0, "MAX": 10.0, "DEFAULT": 5.0 },
    { "NAME": "camHeight",     "LABEL": "Camera Height",   "TYPE": "float", "MIN": -1.0, "MAX": 3.0,  "DEFAULT": 1.2 },
    { "NAME": "camOrbitSpeed", "LABEL": "Orbit Speed",     "TYPE": "float", "MIN": 0.0, "MAX": 1.0,   "DEFAULT": 0.07 },
    { "NAME": "camAzimuth",    "LABEL": "Azimuth",         "TYPE": "float", "MIN": -3.1416, "MAX": 3.1416, "DEFAULT": 0.3 },
    { "NAME": "keyAngle",      "LABEL": "Key Light Angle", "TYPE": "float", "MIN": 0.0, "MAX": 6.2832, "DEFAULT": 0.8 },
    { "NAME": "keyElevation",  "LABEL": "Key Elevation",   "TYPE": "float", "MIN": 0.0, "MAX": 1.5708, "DEFAULT": 0.65 },
    { "NAME": "keyColor",      "LABEL": "Key Light",       "TYPE": "color", "DEFAULT": [1.40, 1.20, 0.80, 1.0] },
    { "NAME": "fillColor",     "LABEL": "Fill Light",      "TYPE": "color", "DEFAULT": [0.45, 0.55, 0.85, 1.0] },
    { "NAME": "ambient",       "LABEL": "Ambient",         "TYPE": "float", "MIN": 0.0, "MAX": 0.5, "DEFAULT": 0.10 },
    { "NAME": "rimStrength",   "LABEL": "Rim Strength",    "TYPE": "float", "MIN": 0.0, "MAX": 2.0, "DEFAULT": 0.70 },
    { "NAME": "audioReact",    "LABEL": "Audio React",     "TYPE": "float", "MIN": 0.0, "MAX": 2.0, "DEFAULT": 1.0 },
    { "NAME": "exposure",      "LABEL": "Exposure",        "TYPE": "float", "MIN": 0.3, "MAX": 3.0, "DEFAULT": 1.0 }
  ]
}*/

// Bauhaus 3D: three Kandinsky primitives become 3D volumes in a studio
// gallery. Raymarched SDF. Studio 3-point lighting. Linear HDR.

#define MAX_STEPS 96
#define MAX_DIST  30.0
#define EPS       0.0012

// Palette IDs
#define MAT_VOID    0
#define MAT_YELLOW  1   // tetrahedron — anodised yellow lacquer
#define MAT_RED     2   // cube — deep red lacquer, satin sheen
#define MAT_BLUE    3   // sphere — cobalt gloss PBR
#define MAT_WHITE   4   // plinth / gallery floor
#define MAT_LINE    5   // Kandinsky support lines (black wire)

mat2 rot2(float a) { float c=cos(a),s=sin(a); return mat2(c,-s,s,c); }

float sdSphere(vec3 p, float r) { return length(p) - r; }

float sdBox(vec3 p, vec3 b) {
    vec3 d = abs(p) - b;
    return length(max(d,0.0)) + min(max(d.x,max(d.y,d.z)),0.0);
}

// Regular tetrahedron SDF, radius r
float sdTetrahedron(vec3 p, float r) {
    float md = max(max(-p.x-p.y-p.z, p.x+p.y-p.z),
                   max(-p.x+p.y+p.z,  p.x-p.y+p.z));
    return (md - r) / sqrt(3.0);
}

// Infinite plinth plane — floor at y = yFloor
float sdFloor(vec3 p, float yFloor) { return p.y - yFloor; }

// Thin capsule wire for Kandinsky support lines
float sdCapsule(vec3 p, vec3 a, vec3 b, float r) {
    vec3 pa = p-a, ba = b-a;
    float h = clamp(dot(pa,ba)/dot(ba,ba), 0.0, 1.0);
    return length(pa - ba*h) - r;
}

struct Hit { float d; int mat; };
Hit hmin(Hit a, Hit b) { return (a.d < b.d) ? a : b; }

// Shape scale pulses 0.97-1.03 with audio bass (staggered per shape)
float breathScale(int idx, float bass) {
    float ph  = float(idx) * 1.91 + TIME * 1.20;
    float idle = 0.5 + 0.5 * sin(ph);
    return mix(0.97, 1.03, mix(idle, bass, clamp(bass * 1.2, 0.0, 1.0)));
}

Hit map(vec3 p, int kw, float bass, float mid) {
    // Gallery floor
    Hit h = Hit(sdFloor(p, -0.70), MAT_WHITE);

    // Plinth slab — thin white box
    {
        vec3 q = p - vec3(0.0, -0.55, 0.0);
        float d = sdBox(q, vec3(1.60, 0.15, 1.10));
        h = hmin(h, Hit(d, MAT_WHITE));
    }

    // ── Tetrahedron (yellow) — left ───────────────────────────────────
    {
        float s = breathScale(0, bass);
        vec3 q = p - vec3(-0.85, -0.40 + 0.30*s, 0.0);
        // Slowly spin around y, mid-band orbit
        float a = TIME * (0.10 + mid * 0.15) * (1.0 + float(kw) * 0.07);
        q.xz = rot2(a) * q.xz;
        q.yz = rot2(0.28) * q.yz;
        float d = sdTetrahedron(q, 0.36 * s);
        h = hmin(h, Hit(d, MAT_YELLOW));
    }

    // ── Box (red) — centre back ───────────────────────────────────────
    {
        float s = breathScale(1, bass);
        vec3 q = p - vec3(0.10, -0.38 + 0.35*s, -0.45);
        float a = TIME * (0.08 + mid * 0.12);
        q.xz = rot2(a + 0.55) * q.xz;
        float d = sdBox(q, vec3(0.32*s));
        h = hmin(h, Hit(d, MAT_RED));
    }

    // ── Sphere (blue) — right ─────────────────────────────────────────
    {
        float s = breathScale(2, bass);
        vec3 q = p - vec3(0.90, -0.30 + 0.28*s, 0.20);
        float d = sdSphere(q, 0.32 * s);
        h = hmin(h, Hit(d, MAT_BLUE));
    }

    // ── Kandinsky support line wires ──────────────────────────────────
    // Two thin diagonal wire rods floating across the scene.
    {
        vec3 a1 = vec3(-1.8, -0.55 + 0.10, -0.20);
        vec3 b1 = vec3( 1.8,  0.20,         0.30);
        float d = sdCapsule(p, a1, b1, 0.008);
        h = hmin(h, Hit(d, MAT_LINE));
    }
    {
        vec3 a2 = vec3(-1.6,  0.60, -0.60);
        vec3 b2 = vec3( 1.6, -0.55,  0.60);
        float d = sdCapsule(p, a2, b2, 0.006);
        h = hmin(h, Hit(d, MAT_LINE));
    }

    return h;
}

vec3 calcNormal(vec3 p, int kw, float bass, float mid) {
    const vec2 e = vec2(EPS, 0.0);
    return normalize(vec3(
        map(p+e.xyy, kw,bass,mid).d - map(p-e.xyy, kw,bass,mid).d,
        map(p+e.yxy, kw,bass,mid).d - map(p-e.yxy, kw,bass,mid).d,
        map(p+e.yyx, kw,bass,mid).d - map(p-e.yyx, kw,bass,mid).d
    ));
}

// Soft shadow along key-light ray
float keyShadow(vec3 ro, vec3 rd, int kw, float bass, float mid) {
    float res = 1.0, t = 0.02;
    for (int i = 0; i < 24; i++) {
        if (t > 8.0) break;
        float h = map(ro + rd * t, kw, bass, mid).d;
        if (h < 0.001) { res = 0.0; break; }
        res = min(res, 8.0 * h / t);
        t  += clamp(h, 0.016, 0.3);
    }
    return clamp(res, 0.06, 1.0);
}

// Palette colours — Kandinsky's primaries, full saturation, no white mixing.
// HDR values: specular tip peaks 2.5 linear.
vec3 matAlbedo(int mat, int kw) {
    if (mat == MAT_YELLOW) {
        // yellow: Composition VIII → pure cadmium; Several Circles → lemon
        return (kw == 1) ? vec3(0.98, 0.92, 0.05) : vec3(0.96, 0.80, 0.08);
    } else if (mat == MAT_RED) {
        return (kw == 3 || kw == 4) ? vec3(0.60, 0.06, 0.06) : vec3(0.92, 0.10, 0.12);
    } else if (mat == MAT_BLUE) {
        return (kw == 2) ? vec3(0.08, 0.12, 0.72) : vec3(0.10, 0.18, 0.68);
    } else if (mat == MAT_WHITE) {
        return (kw == 1 || kw == 3) ? vec3(0.06, 0.06, 0.08) : vec3(0.92, 0.90, 0.86);
    } else { // LINE
        return vec3(0.04, 0.04, 0.05);
    }
}

vec3 shade(vec3 pos, vec3 nor, vec3 rd, int mat, int kw,
           vec3 kDir, vec3 kC, vec3 fC, float amb, float rim,
           float shadow, float treble) {
    vec3 albedo = matAlbedo(mat, kw);
    float gloss, spec;
    if (mat == MAT_YELLOW) { gloss = 48.0; spec = 0.15; }
    else if (mat == MAT_RED)  { gloss = 64.0; spec = 0.22; }
    else if (mat == MAT_BLUE) { gloss = 96.0; spec = 0.40; }
    else if (mat == MAT_WHITE){ gloss = 18.0; spec = 0.06; }
    else                       { gloss = 12.0; spec = 0.02; }

    float ndK = max(dot(nor, kDir), 0.0);
    // Fill from opposite side, slightly above
    vec3 fDir = normalize(vec3(-kDir.x, kDir.y * 0.4 + 0.3, -kDir.z));
    float ndF = max(dot(nor, fDir), 0.0);
    vec3  hK  = normalize(kDir - rd);
    float sp  = pow(max(dot(nor, hK), 0.0), gloss);
    // HDR specular: push into 2.5 on blue/red shiny surfaces
    float hdrSpec = sp * spec * (2.40 + treble * 0.5);

    float rimT = pow(1.0 - max(dot(nor, -rd), 0.0), 3.0);

    vec3 col  = albedo * (amb + ndK * shadow * kC + ndF * fC * 0.55);
    col += kC * hdrSpec * shadow;
    col += albedo * rimT * rim * 0.30;

    // Floor tile gets a faint Mondrian-style grid texture
    if (mat == MAT_WHITE) {
        float gx = fwidth(pos.x);
        float gz = fwidth(pos.z);
        float gridX = smoothstep(0.02 - gx, 0.02 + gx, abs(fract(pos.x * 1.5) - 0.5));
        float gridZ = smoothstep(0.02 - gz, 0.02 + gz, abs(fract(pos.z * 1.5) - 0.5));
        float grid = min(gridX, gridZ);
        col *= (1.0 - 0.12 * (1.0 - grid));
    }

    return col;
}

void main() {
    vec2 uv  = (isf_FragNormCoord.xy * 2.0 - 1.0)
             * vec2(RENDERSIZE.x / RENDERSIZE.y, 1.0);
    int  kw  = int(clamp(kandinskyWork, 0.0, 4.0));
    float aR = clamp(audioReact, 0.0, 2.0);
    float bass   = clamp(audioBass, 0.0, 1.0) * aR;
    float mid    = clamp(audioMid,  0.0, 1.0) * aR;
    float treble = clamp(audioHigh, 0.0, 1.0) * aR;

    // ── Camera ────────────────────────────────────────────────────────
    // Orbit speed K: camOrbitSpeed + camOrbitSpeed * mid * 1.5 ≤ 1.5 ✓
    float orbT = camAzimuth + TIME * camOrbitSpeed * (1.0 + mid * 1.5);
    vec3  ro   = vec3(cos(orbT) * camDist, camHeight, sin(orbT) * camDist);
    vec3  ta   = vec3(0.0, 0.10, 0.0);
    vec3  fw   = normalize(ta - ro);
    vec3  ri   = normalize(cross(vec3(0.0, 1.0, 0.0), fw));
    vec3  up   = cross(fw, ri);
    vec3  rd   = normalize(fw + uv.x * ri + uv.y * up);

    // ── Key light ─────────────────────────────────────────────────────
    float ce   = cos(keyElevation);
    vec3  kDir = normalize(vec3(cos(keyAngle)*ce, sin(keyElevation), sin(keyAngle)*ce));

    // ── Background ────────────────────────────────────────────────────
    // Gallery wall: near-white for Comp VIII, near-black for Several Circles.
    vec3 bgTop, bgBot;
    if (kw == 1 || kw == 3) {
        bgTop = vec3(0.04, 0.04, 0.06);
        bgBot = vec3(0.02, 0.02, 0.03);
    } else if (kw == 4) {
        bgTop = vec3(0.90, 0.88, 0.84);
        bgBot = vec3(0.82, 0.80, 0.78);
    } else {
        bgTop = vec3(0.92, 0.90, 0.86);
        bgBot = vec3(0.78, 0.76, 0.72);
    }
    vec3 bg = mix(bgBot, bgTop, clamp(rd.y * 0.5 + 0.6, 0.0, 1.0));

    // ── March ─────────────────────────────────────────────────────────
    float t = 0.0; int matHit = MAT_VOID; vec3 pos = ro;
    for (int i = 0; i < MAX_STEPS; i++) {
        pos = ro + rd * t;
        Hit h = map(pos, kw, bass, mid);
        if (h.d < EPS) { matHit = h.mat; break; }
        if (t > MAX_DIST) break;
        t += h.d * 0.9;
    }

    vec3 col = bg;
    if (matHit != MAT_VOID) {
        vec3 nor   = calcNormal(pos, kw, bass, mid);
        float sh   = keyShadow(pos + nor * 0.012, kDir, kw, bass, mid);
        col = shade(pos, nor, rd, matHit, kw,
                    kDir, keyColor.rgb, fillColor.rgb,
                    ambient, rimStrength, sh, treble);
    }

    col *= exposure;
    // Slight vignette
    col *= 1.0 - 0.25 * dot(uv * 0.5, uv * 0.5);

    gl_FragColor = vec4(col, 1.0);
}
