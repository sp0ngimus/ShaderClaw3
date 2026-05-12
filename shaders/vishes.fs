/*{
  "DESCRIPTION": "Crystal Cave Formations — 3D raymarched cave interior with hexagonal emerald and amethyst crystal columns growing from floor and ceiling. Cinematic low-angle camera slowly drifts through the cavern. LINEAR HDR out, no tonemap.",
  "CREDIT": "ShaderClaw auto-improve 2026-05-12",
  "CATEGORIES": ["Generator", "3D", "Audio Reactive"],
  "INPUTS": [
    { "NAME": "crystalColor1", "LABEL": "Emerald",      "TYPE": "color", "DEFAULT": [0.05, 0.85, 0.45, 1.0] },
    { "NAME": "crystalColor2", "LABEL": "Amethyst",     "TYPE": "color", "DEFAULT": [0.65, 0.15, 1.00, 1.0] },
    { "NAME": "specPeak",      "LABEL": "Specular HDR", "TYPE": "float", "MIN": 1.0, "MAX": 6.0,  "DEFAULT": 3.5 },
    { "NAME": "driftSpeed",    "LABEL": "Drift Speed",  "TYPE": "float", "MIN": 0.0, "MAX": 1.5,  "DEFAULT": 0.20 },
    { "NAME": "caveHeight",    "LABEL": "Cave Height",  "TYPE": "float", "MIN": 0.5, "MAX": 3.0,  "DEFAULT": 1.6 },
    { "NAME": "audioReact",    "LABEL": "Audio React",  "TYPE": "float", "MIN": 0.0, "MAX": 2.0,  "DEFAULT": 1.0 }
  ]
}*/

#define MAX_STEPS  80
#define EPS        0.003
#define PI         3.14159265

float h11(float n) { return fract(sin(n*127.1)  * 43758.5453); }
float h21(vec2  p) { return fract(sin(dot(p, vec2(127.1,311.7))) * 43758.5453); }
mat2  rot2(float a) { float c=cos(a),s=sin(a); return mat2(c,-s,s,c); }

// Hexagonal SDF column: radius r, half-height h, axis Y
float sdHexPrism(vec3 p, float r, float h) {
    const vec3 k = vec3(-0.8660254, 0.5, 0.57735);
    p = abs(p);
    p.xy -= 2.0 * min(dot(k.xy, p.xy), 0.0) * k.xy;
    vec2 d = vec2(
        length(p.xy - vec2(clamp(p.x, -k.z*r, k.z*r), r)) * sign(p.y - r),
        p.z - h
    );
    return min(max(d.x, d.y), 0.0) + length(max(d, 0.0));
}

// Cave ceiling and floor as planes + slight dome
float sdCave(vec3 p) {
    float floor_   = p.y + 0.05;                              // floor at y=-0.05
    float ceiling  = caveHeight - p.y + 0.05;                 // ceiling at y=caveHeight
    return min(floor_, ceiling);
}

// Hash-placed crystal columns
float sdCrystals(vec3 p, float bass) {
    float dMin = 1e9;
    float sp   = 0.95;   // column grid spacing

    for (int xi = -3; xi <= 3; xi++) {
        for (int zi = -3; zi <= 3; zi++) {
            float fx = float(xi), fz = float(zi);
            vec2 cell = vec2(fx, fz);

            float jx = (h21(cell + vec2( 0.7, 0.3)) - 0.5) * 0.4;
            float jz = (h21(cell + vec2( 0.2, 0.9)) - 0.5) * 0.4;
            float cx = fx * sp + jx;
            float cz = fz * sp + jz;

            float r = 0.06 + h21(cell + vec2(1.1, 2.3)) * 0.08;
            float grow = 0.4 + h21(cell + vec2(3.7, 1.5)) * 0.55;
            // Audio breathing: crystal height pulses with bass
            grow *= 1.0 + bass * 0.12;

            // Floor column (growing up)
            {
                vec3 q = p - vec3(cx, 0.0, cz);
                // Slight random lean
                float leanAng = (h21(cell + vec2(5.1,7.2)) - 0.5) * 0.35;
                q.xy = rot2(leanAng) * q.xy;
                float h = grow * caveHeight * 0.5;
                vec3  pLocal = q - vec3(0.0, h, 0.0);
                float d = sdHexPrism(pLocal, r, h);
                dMin = min(dMin, d);
            }

            // Ceiling stalactite (growing down from ceiling)
            {
                vec2  cell2 = cell + vec2(0.5, 0.5);   // offset grid
                float cx2 = (fx + 0.5) * sp + (h21(cell2 + vec2(0.4,1.6)) - 0.5) * 0.4;
                float cz2 = (fz + 0.5) * sp + (h21(cell2 + vec2(2.2,0.7)) - 0.5) * 0.4;
                float r2  = 0.05 + h21(cell2 + vec2(4.1,0.2)) * 0.07;
                float grow2 = (0.3 + h21(cell2 + vec2(1.8,3.3)) * 0.45) * (1.0 + bass*0.10);
                vec3 q2 = p - vec3(cx2, caveHeight, cz2);
                // flip Y for ceiling crystal
                q2.y = -q2.y;
                float h2 = grow2 * caveHeight * 0.45;
                vec3  pLocal2 = q2 - vec3(0.0, h2, 0.0);
                float d2 = sdHexPrism(pLocal2, r2, h2);
                dMin = min(dMin, d2);
            }
        }
    }
    return dMin;
}

float mapScene(vec3 p, float bass) {
    return min(sdCave(p), sdCrystals(p, bass));
}

vec3 calcNormal(vec3 p, float bass) {
    const vec2 e = vec2(EPS, 0.0);
    return normalize(vec3(
        mapScene(p+e.xyy,bass) - mapScene(p-e.xyy,bass),
        mapScene(p+e.yxy,bass) - mapScene(p-e.yxy,bass),
        mapScene(p+e.yyx,bass) - mapScene(p-e.yyx,bass)
    ));
}

void main() {
    vec2 uv = (gl_FragCoord.xy / RENDERSIZE.xy) * 2.0 - 1.0;
    uv.x *= RENDERSIZE.x / RENDERSIZE.y;

    float bass = clamp(audioBass, 0.0, 1.0) * audioReact;
    float high = clamp(audioHigh, 0.0, 1.0) * audioReact;

    // Calm drift through the cave (panning side to side, never static)
    float driftX = sin(TIME * driftSpeed * 0.31) * 0.55;
    float driftZ = TIME * driftSpeed * 0.25;
    vec3 ro = vec3(driftX, caveHeight * 0.32, driftZ);

    // Look slightly downward and forward
    float yaw   = sin(TIME * driftSpeed * 0.15) * 0.35;
    float pitch = -0.12 - 0.06 * sin(TIME * driftSpeed * 0.22);
    vec3 fwd   = normalize(vec3(sin(yaw), sin(pitch), -cos(yaw)));
    vec3 rgt   = normalize(cross(vec3(0,1,0), fwd));
    vec3 upV   = cross(fwd, rgt);
    vec3 rd    = normalize(fwd + rgt*uv.x*0.72 + upV*uv.y*0.72);

    // Raymarch
    float t   = 0.02;
    bool  hit = false;
    for (int i = 0; i < MAX_STEPS; i++) {
        vec3 p = ro + rd * t;
        float d = mapScene(p, bass);
        if (d < EPS) { hit = true; break; }
        if (t > 8.0) break;
        t += max(d * 0.80, EPS * 1.5);
    }

    vec3 col = vec3(0.0);

    if (hit) {
        vec3 p = ro + rd * t;
        vec3 n = calcNormal(p, bass);
        vec3 v = -rd;

        // Determine material — cave wall or crystal column
        float dCave  = sdCave(p);
        float dCryst = sdCrystals(p, bass);
        bool  onCrystal = (dCryst < dCave + 0.002);

        // Crystal color: alternate emerald / amethyst by hex column position
        float colSel = h21(floor(p.xz / 0.95));
        vec3  crystalCol = mix(crystalColor1.rgb, crystalColor2.rgb, step(0.5, colSel));

        // Point light from camera (torch-like)
        vec3 lTorch = normalize(ro - p);
        float dTorch = length(ro - p);
        float att   = 1.0 / (1.0 + dTorch * dTorch * 0.35);
        float dL    = max(dot(n, lTorch), 0.0);

        // Specular
        vec3 hT = normalize(lTorch + v);
        float sp = pow(max(dot(n, hT), 0.0), onCrystal ? 128.0 : 8.0);

        if (onCrystal) {
            vec3 lit  = crystalCol * (0.15 + 0.85*dL) * att;
            lit += vec3(1.0, 0.97, 1.0) * sp * specPeak * att;
            // Subsurface glow: faint emissive inside crystal
            float sss = exp(-abs(dCryst) * 60.0) * 0.4;
            lit += crystalCol * sss * (1.0 + bass * 0.5);
            // Audio-reactive rim
            float rim = pow(1.0 - max(dot(n, v), 0.0), 2.0);
            lit += crystalCol * rim * 0.6 * (1.0 + high * 0.5);
            col = lit;
        } else {
            // Cave wall: dark wet rock, slight green bioluminescent tint
            vec3 rockCol = vec3(0.04, 0.06, 0.05) + vec3(0.02,0.06,0.04)*dL*att;
            rockCol += vec3(0.08, 0.12, 0.08) * sp * 0.25 * att;
            // Faint crystal glow bleeding onto wall
            rockCol += crystalColor1.rgb * 0.05 * exp(-dCryst * 5.0);
            col = rockCol;
        }

        // fwidth AA on crystal facet edges
        float edgeM = clamp(fwidth(dCryst) * 200.0, 0.0, 1.0);
        col *= 1.0 - edgeM * 0.25;

        // Cave fog
        float fog = exp(-t * 0.22);
        col = mix(vec3(0.01, 0.015, 0.018), col, fog);
    } else {
        col = vec3(0.008, 0.012, 0.015);
    }

    // Ambient crystal light — faint emerald/amethyst breath
    float breath = 0.5 + 0.5 * sin(TIME * 0.5);
    col += mix(crystalColor1.rgb, crystalColor2.rgb, breath) * 0.018 * (1.0 + bass * 0.5);

    // LINEAR HDR — no tonemap, no clamp
    gl_FragColor = vec4(col, 1.0);
}
