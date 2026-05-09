/*{
    "DESCRIPTION": "3D Neon Wireframe City — raymarched SDF cityscape at night with glowing neon edge lines",
    "CREDIT": "ShaderClaw auto-improve 2026-05-09",
    "ISFVSN": "2",
    "CATEGORIES": ["Generator", "3D", "Audio Reactive"],
    "INPUTS": [
        {
            "NAME": "buildingDensity",
            "TYPE": "float",
            "DEFAULT": 0.55,
            "MIN": 0.1,
            "MAX": 1.0,
            "LABEL": "Building Density"
        },
        {
            "NAME": "buildingHeight",
            "TYPE": "float",
            "DEFAULT": 1.2,
            "MIN": 0.3,
            "MAX": 3.0,
            "LABEL": "Building Height"
        },
        {
            "NAME": "cityRadius",
            "TYPE": "float",
            "DEFAULT": 3.5,
            "MIN": 1.0,
            "MAX": 8.0,
            "LABEL": "City Radius"
        },
        {
            "NAME": "edgeGlow",
            "TYPE": "float",
            "DEFAULT": 2.5,
            "MIN": 1.0,
            "MAX": 4.0,
            "LABEL": "Edge HDR Brightness"
        },
        {
            "NAME": "orbitSpeed",
            "TYPE": "float",
            "DEFAULT": 0.07,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Camera Orbit"
        },
        {
            "NAME": "audioReact",
            "TYPE": "float",
            "DEFAULT": 0.8,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Audio Reactivity"
        }
    ]
}*/

// ---- Constants ----
#define MAX_STEPS 64
#define MAX_DIST  25.0
#define SURF_DIST 0.004
#define PI        3.14159265358979323846
#define TAU       6.28318530717958647692

// ---- Hash utilities ----
float hash11(float n) {
    return fract(sin(n) * 43758.5453123);
}
float hash21(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

// ---- SDF primitives ----
float sdBox(vec3 p, vec3 b) {
    vec3 q = abs(p) - b;
    return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
}

float sdPlane(vec3 p, float y) {
    return p.y - y;
}

// ---- Edge proximity for a box ----
// Returns how far from the nearest edge line (small = at an edge)
float boxEdgeProximity(vec3 p, vec3 b) {
    vec3 q = abs(p);
    vec3 d = b - q;
    // Near an edge means two dimensions are near the face boundary simultaneously
    float d0 = d.x, d1 = d.y, d2 = d.z;
    // Sort to find two smallest
    if (d0 < d1) { float tmp = d0; d0 = d1; d1 = tmp; }
    if (d1 < d2) { float tmp = d1; d1 = d2; d2 = tmp; }
    if (d0 < d1) { float tmp = d0; d0 = d1; d1 = tmp; }
    // d1, d2 are the two smallest — both near 0 = near edge
    return max(d1, d2);
}

// ---- Scene result packed as vec3(dist, mat, hash) ----
// mat: 1.0=building, 2.0=ground
vec3 mapCity(vec3 p, float t) {
    float audioHScale = 1.0 + audioBass * 1.0 * audioReact;  // K=1.0

    float bestDist = MAX_DIST;
    float bestMat = 0.0;
    float bestHash = 0.0;

    float spacing = 1.4;
    vec2 cellXZ = floor(p.xz / spacing);

    // Check 3x3 neighborhood for correct SDF continuity
    for (int ci = -1; ci <= 1; ci++) {
        for (int cj = -1; cj <= 1; cj++) {
            vec2 cell = cellXZ + vec2(float(ci), float(cj));

            float h1 = hash21(cell);
            float h2 = hash21(cell + vec2(3.7, 11.3));
            float h3 = hash21(cell + vec2(17.1, 5.9));

            // Density check
            if (h1 > buildingDensity) continue;

            // City radius clip
            if (length(cell * spacing) > cityRadius) continue;

            float bw = 0.22 + h2 * 0.18;
            float bh = (0.3 + h3 * buildingHeight) * audioHScale;
            float bd = 0.22 + h1 * 0.18;

            vec3 bCenter = vec3((cell.x + 0.5) * spacing, bh * 0.5, (cell.y + 0.5) * spacing);
            vec3 pRel = p - bCenter;
            float d = sdBox(pRel, vec3(bw, bh * 0.5, bd));

            if (d < bestDist) {
                bestDist = d;
                bestMat = 1.0;
                bestHash = h1;
            }
        }
    }

    // Ground plane
    float gd = sdPlane(p, 0.0);
    if (gd < bestDist) {
        bestDist = gd;
        bestMat = 2.0;
        bestHash = 0.0;
    }

    return vec3(bestDist, bestMat, bestHash);
}

float mapDist(vec3 p, float t) {
    return mapCity(p, t).x;
}

// ---- Normal estimation ----
vec3 calcNormal(vec3 p, float t) {
    float eps = 0.002;
    vec2 e = vec2(eps, 0.0);
    return normalize(vec3(
        mapDist(p + e.xyy, t) - mapDist(p - e.xyy, t),
        mapDist(p + e.yxy, t) - mapDist(p - e.yxy, t),
        mapDist(p + e.yyx, t) - mapDist(p - e.yyx, t)
    ));
}

// ---- Palette: void black, cyan 3.0, magenta 2.0, yellow 2.5, white 3.0 ----
vec3 edgeColor(float cellHash, float t) {
    float idx = floor(fract(cellHash * 7.391) * 4.0);
    vec3 col;
    if (idx < 1.0)      col = vec3(0.0, 1.0, 1.0) * 3.0;   // neon cyan 3.0
    else if (idx < 2.0) col = vec3(1.0, 0.0, 0.9) * 2.0;   // neon magenta 2.0
    else if (idx < 3.0) col = vec3(1.0, 0.9, 0.0) * 2.5;   // electric yellow 2.5
    else                col = vec3(1.0, 1.0, 1.0) * 3.0;   // hot white spec 3.0
    return col;
}

// ---- Ground grid ----
vec3 groundGridColor(vec3 p) {
    vec2 gUV = p.xz / 1.4;
    vec2 gFrac = fract(gUV);
    float lineX = min(gFrac.x, 1.0 - gFrac.x);
    float lineZ = min(gFrac.y, 1.0 - gFrac.y);
    float fw = fwidth(min(lineX, lineZ));
    float gridLine = 1.0 - smoothstep(0.0, fw * 3.0 + 0.01, min(lineX, lineZ));
    float gridBright = 0.4 * (1.0 + audioMid * 1.0 * audioReact);
    return vec3(0.0, 0.5, 1.0) * gridBright * gridLine;
}

// ---- Main ----
void main() {
    vec2 uv = isf_FragNormCoord.xy * 2.0 - 1.0;
    float aspect = RENDERSIZE.x / max(RENDERSIZE.y, 1.0);
    uv.x *= aspect;

    float t = TIME;

    // Camera orbits overhead looking down at city
    float orbitAngle = t * orbitSpeed;
    float camHeight = 4.5;
    float camDist = cityRadius * 0.85;

    vec3 camPos = vec3(
        cos(orbitAngle) * camDist,
        camHeight,
        sin(orbitAngle) * camDist
    );

    vec3 target = vec3(0.0, 0.5, 0.0);
    vec3 forward = normalize(target - camPos);
    vec3 right = normalize(cross(forward, vec3(0.0, 1.0, 0.0)));
    vec3 up = cross(right, forward);

    vec3 rd = normalize(forward + uv.x * right * 0.6 + uv.y * up * 0.6);
    vec3 ro = camPos;

    // Raymarching
    float dist = 0.0;
    bool hit = false;
    float hitMat = 0.0;
    vec3 hitPos = ro;
    float hitHash = 0.0;

    for (int i = 0; i < MAX_STEPS; i++) {
        vec3 p = ro + rd * dist;
        vec3 res = mapCity(p, t);
        float d = res.x;

        if (d < SURF_DIST) {
            hit = true;
            hitMat = res.y;
            hitPos = p;
            hitHash = res.z;
            break;
        }

        dist += max(d * 0.85, SURF_DIST * 2.0);
        if (dist > MAX_DIST) break;
    }

    vec3 finalColor = vec3(0.0);  // void black

    if (hit) {
        vec3 nor = calcNormal(hitPos, t);

        if (hitMat > 1.5) {
            // Ground plane — grid lines only
            finalColor = groundGridColor(hitPos);
            float cityGlow = exp(-length(hitPos.xz) * 0.15) * 0.08;
            finalColor += vec3(0.0, 0.3, 0.5) * cityGlow;

        } else {
            // Building — edge proximity for fwidth-based glow
            float spacing = 1.4;
            vec2 cellXZ2 = floor(hitPos.xz / spacing);
            float bestEdge = 999.0;

            for (int ci = -1; ci <= 1; ci++) {
                for (int cj = -1; cj <= 1; cj++) {
                    vec2 cell = cellXZ2 + vec2(float(ci), float(cj));
                    float h1 = hash21(cell);
                    // Match the hit building by hash
                    if (abs(h1 - hitHash) > 0.001) continue;
                    float h2 = hash21(cell + vec2(3.7, 11.3));
                    float h3 = hash21(cell + vec2(17.1, 5.9));
                    float audioHScale2 = 1.0 + audioBass * 1.0 * audioReact;
                    float bw = 0.22 + h2 * 0.18;
                    float bh = (0.3 + h3 * buildingHeight) * audioHScale2;
                    float bd = 0.22 + h1 * 0.18;
                    vec3 bCenter = vec3((cell.x + 0.5) * spacing, bh * 0.5, (cell.y + 0.5) * spacing);
                    vec3 pRel = hitPos - bCenter;
                    float ep = boxEdgeProximity(pRel, vec3(bw, bh * 0.5, bd));
                    if (ep < bestEdge) bestEdge = ep;
                }
            }

            // fwidth-based edge glow — anti-aliased neon lines
            float edgeWidth = fwidth(bestEdge);
            float edgeMask = 1.0 - smoothstep(0.0, edgeWidth * 5.0 + 0.025, bestEdge);

            // Rim lighting adds edge emphasis at silhouette
            float rim = pow(max(0.0, 1.0 - abs(dot(nor, -rd))), 2.5);
            float edgeFactor = max(edgeMask, rim * 0.5);

            // Audio modulates edge glow brightness (K=1.0)
            float glowBright = edgeGlow * (1.0 + audioHigh * 1.0 * audioReact);

            vec3 eCol = edgeColor(hitHash, t);
            finalColor = eCol * edgeFactor * glowBright;

            // Faint interior — nearly void
            finalColor += eCol * 0.03 * (1.0 - edgeFactor);
        }
    }

    // Volumetric halo pass — soft atmospheric glow around edges
    {
        vec3 haloCol = vec3(0.0);
        float hStep = MAX_DIST / 24.0;
        float hDist = 0.5;

        for (int hi = 0; hi < 24; hi++) {
            vec3 hp = ro + rd * hDist;
            vec3 hr = mapCity(hp, t);
            if (hr.y > 0.5 && hr.y < 1.5) {
                float proximity = exp(-abs(hr.x) * 18.0);
                vec3 hc = edgeColor(hr.z, t);
                float ab = 1.0 + audioMid * 0.8 * audioReact;
                haloCol += hc * proximity * 0.04 * ab;
            }
            hDist += hStep;
            if (hDist > MAX_DIST) break;
        }

        finalColor += clamp(haloCol, 0.0, 3.0);
    }

    // Output linear HDR — no tonemapping, no ACES, no clamp
    gl_FragColor = vec4(finalColor, 1.0);
}
