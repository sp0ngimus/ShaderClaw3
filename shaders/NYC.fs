/*{
  "DESCRIPTION": "NYC — procedural 3D city flythrough. Raymarched building grid with HDR lit windows (2.5 linear), LED ad walls, and wet-street mirror reflections from the ground plane. Time of day, mood, speed, drive/fly height, variety, ad brightness exposed. Speed default 0.25 (calm). LINEAR HDR out — no tonemap.",
  "CATEGORIES": ["Generator", "3D"],
  "INPUTS": [
    { "NAME": "ad1",           "LABEL": "Ad Texture A",        "TYPE": "image" },
    { "NAME": "ad2",           "LABEL": "Ad Texture B",        "TYPE": "image" },
    { "NAME": "ad3",           "LABEL": "Ad Texture C",        "TYPE": "image" },
    { "NAME": "timeOfDay",     "LABEL": "Time of Day",         "TYPE": "float", "DEFAULT": 0.12, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "windowLights",  "LABEL": "Window Lights",       "TYPE": "float", "DEFAULT": 0.55, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "mood",          "LABEL": "Mood (dark/bright)",  "TYPE": "float", "DEFAULT": 0.5,  "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "adIntensity",   "LABEL": "Ad Brightness",       "TYPE": "float", "DEFAULT": 2.00, "MIN": 0.0, "MAX": 4.0 },
    { "NAME": "variety",       "LABEL": "Building Variety",    "TYPE": "float", "DEFAULT": 0.85, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "speed",         "LABEL": "Movement Speed",      "TYPE": "float", "DEFAULT": 0.25, "MIN": 0.0, "MAX": 1.5 },
    { "NAME": "flightMode",    "LABEL": "Drive(0) / Fly(1)",   "TYPE": "float", "DEFAULT": 0.0,  "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "fog",           "LABEL": "Fog Density",         "TYPE": "float", "DEFAULT": 0.5,  "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "wetStreet",     "LABEL": "Wet Street",          "TYPE": "float", "DEFAULT": 0.55, "MIN": 0.0, "MAX": 1.0 }
  ]
}*/

// NYC: procedural raymarched city. NEW: linear HDR out (no tonemap),
// speed default 0.25, window emission 2.5 HDR, wet-street mirror reflections.

#define CELL        8.0
#define BUILD_HALF  3.0
#define MAX_STEPS   96
#define MAX_DIST    260.0
#define EPS         0.015

float hash11(float p) { return fract(sin(p * 127.1) * 43758.5453); }
vec2  hash22(vec2  p) {
    p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
    return fract(sin(p) * 43758.5453);
}

vec4 buildingInfo(vec2 cell) {
    vec2 r  = hash22(cell);
    float h = mix(10.0, 6.0 + pow(r.x, 1.8) * 42.0, variety);
    float hasAd = step(0.88, r.y);
    float adId  = floor(hash11(cell.x * 13.7 + cell.y * 7.3) * 3.0);
    return vec4(h, r.x, hasAd, adId);
}

float sdBox(vec3 p, vec3 b) {
    vec3 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, max(d.y, d.z)), 0.0);
}

float mapScene(vec3 p, out vec2 outCell, out vec4 outInfo) {
    vec2 base = floor(p.xz / CELL);
    float best = 1e9;
    vec2 bestCell = base; vec4 bestInfo = vec4(0.0);
    for (int j = -1; j <= 1; j++) for (int i = -1; i <= 1; i++) {
        vec2 cell = base + vec2(float(i), float(j));
        vec4 info = buildingInfo(cell);
        float h  = info.x;
        float hw = BUILD_HALF * (0.55 + info.y * 0.45);
        vec3 c   = vec3((cell.x+0.5)*CELL, h*0.5, (cell.y+0.5)*CELL);
        float d  = sdBox(p - c, vec3(hw, h*0.5, hw));
        if (d < best) { best = d; bestCell = cell; bestInfo = info; }
    }
    float ground = p.y;
    if (ground < best) { best = ground; bestCell = vec2(0.0); bestInfo = vec4(0,0,-1,-1); }
    outCell = bestCell; outInfo = bestInfo;
    return best;
}

vec3 estimateNormal(vec3 p) {
    vec2 dc; vec4 di;
    vec2 e = vec2(0.002, 0.0);
    return normalize(vec3(
        mapScene(p+e.xyy,dc,di) - mapScene(p-e.xyy,dc,di),
        mapScene(p+e.yxy,dc,di) - mapScene(p-e.yxy,dc,di),
        mapScene(p+e.yyx,dc,di) - mapScene(p-e.yyx,dc,di)
    ));
}

vec3 skyColor(vec3 rd, float tod) {
    float sunAngle = tod * 6.2831853 - 1.5707963;
    float sunH  = sin(sunAngle);
    vec3 night  = vec3(0.02, 0.03, 0.08);
    vec3 day    = vec3(0.55, 0.72, 0.95);
    vec3 warm   = vec3(0.95, 0.50, 0.35);
    float dayM  = smoothstep(-0.1, 0.35, sunH);
    vec3 col    = mix(night, day, dayM);
    float goldM = smoothstep(0.0, 0.2, abs(sunH)) * (1.0 - smoothstep(0.25, 0.55, abs(sunH)));
    col = mix(col, warm, goldM * 0.45);
    float horizon = 1.0 - abs(rd.y);
    col += vec3(0.6, 0.35, 0.25) * pow(horizon, 6.0) * goldM;
    if (dayM < 0.2) {
        vec2 sc = rd.xy * 80.0;
        float st = step(0.997, hash11(floor(sc.x)*31.7 + floor(sc.y)*17.3));
        col += vec3(st) * (1.0 - dayM) * 1.2;  // HDR stars
    }
    return col;
}

void surfaceShade(vec3 p, vec3 n, vec2 cell, vec4 info,
                  out vec3 outCol, out float outEm)
{
    vec3 concrete = mix(vec3(0.15,0.16,0.20), vec3(0.35,0.36,0.42),
                        hash11(dot(cell, vec2(1.7, 3.1))));
    outCol = concrete; outEm = 0.0;

    if (info.z < 0.0) {
        // Ground — wet asphalt with road markings
        outCol = vec3(0.04, 0.04, 0.06);
        float laneZ = fract(p.z * 0.04);
        float laneX = abs(fract(p.x / CELL) - 0.5);
        if (laneX < 0.015 && laneZ > 0.5 && laneZ < 0.6) {
            outCol = vec3(0.95, 0.90, 0.45);
            outEm  = 0.3;
        }
        return;
    }

    vec3 an = abs(n);
    if (an.y > max(an.x, an.z)) return;   // roof: no windows

    vec2 uv;
    float faceSign;
    if (an.x > an.z) { uv = vec2(p.z, p.y); faceSign = sign(n.x); }
    else              { uv = vec2(p.x, p.y); faceSign = sign(n.z); }

    vec2 tile    = vec2(1.2, 2.0);
    vec2 winCell = floor(uv / tile);
    vec2 winLocal = fract(uv / tile);

    float mask = step(0.15, winLocal.x) * step(winLocal.x, 0.85)
               * step(0.15, winLocal.y) * step(winLocal.y, 0.85);
    float litRnd = hash11(dot(winCell, vec2(12.7, 7.3)) + cell.x*2.1 + cell.y*0.7);
    float lit    = step(1.0 - windowLights, litRnd) * mask;

    vec3 warmInt = vec3(1.00, 0.82, 0.55);
    vec3 coolInt = vec3(0.80, 0.90, 1.00);
    vec3 winCol  = mix(warmInt, coolInt, hash11(litRnd * 7.0));
    vec3 glass   = mask * mix(vec3(0.08, 0.12, 0.18), winCol, lit);
    outCol = mix(concrete, glass, mask);
    // HDR window emission: lit windows push to 2.5 linear
    outEm  = lit * 2.50;

    // LED ad wall
    if (info.z > 0.5 && faceSign > 0.5) {
        float adY0 = info.x * 0.15, adY1 = info.x * 0.85;
        if (p.y > adY0 && p.y < adY1) {
            float u, v;
            if (an.x > an.z) u = (p.z - (cell.y+0.5)*CELL + BUILD_HALF) / (BUILD_HALF*2.0);
            else              u = (p.x - (cell.x+0.5)*CELL + BUILD_HALF) / (BUILD_HALF*2.0);
            v = (p.y - adY0) / (adY1 - adY0);
            u = clamp(u, 0.0, 1.0); v = clamp(v, 0.0, 1.0);
            vec3 adCol;
            if      (info.w < 0.5) adCol = texture2D(ad1, vec2(u, 1.0-v)).rgb;
            else if (info.w < 1.5) adCol = texture2D(ad2, vec2(u, 1.0-v)).rgb;
            else                   adCol = texture2D(ad3, vec2(u, 1.0-v)).rgb;
            if (dot(adCol, vec3(1.0)) < 0.01) {
                adCol = vec3(0.4 + 0.6*sin(u*8.0+TIME),
                             0.3 + 0.7*sin(v*6.0-TIME*1.3),
                             0.6 + 0.4*sin((u+v)*4.0+TIME*0.7));
                adCol = abs(adCol);
            }
            outCol = adCol;
            outEm  = dot(adCol, vec3(0.33)) * adIntensity;
        }
    }
}

// ── Wet-street mirror pass ─────────────────────────────────────────────
// Shoots a second ray from the ground plane upward and looks up buildings.
// Only used for ground hits. Returns HDR colour of reflected scene.
vec3 wetReflection(vec3 groundPos, vec3 reflRd, float tod) {
    vec3 ro2 = groundPos + vec3(0.0, 0.01, 0.0);
    float td2 = 0.0;
    vec2 hC2; vec4 hI2; bool hit2 = false; vec3 hitP2;
    for (int i = 0; i < 48; i++) {
        hitP2 = ro2 + reflRd * td2;
        float d = mapScene(hitP2, hC2, hI2);
        if (d < EPS * 2.0) { hit2 = true; break; }
        if (td2 > 120.0) break;
        td2 += max(d * 0.9, 0.04);
    }
    if (!hit2) return skyColor(reflRd, tod) * 0.55;
    vec3 n2 = estimateNormal(hitP2);
    vec3 diff2; float em2;
    surfaceShade(hitP2, n2, hC2, hI2, diff2, em2);
    float sunH = sin(tod * 6.2831853 - 1.5707963);
    vec3  sunDir = normalize(vec3(0.6, max(sunH, -0.1)+0.15, 0.4));
    float lDiff  = max(dot(n2, sunDir), 0.0);
    float amb    = 0.18 + 0.35 * max(sunH, 0.0);
    vec3 litRef  = diff2 * (amb + lDiff * max(sunH, 0.0) * 0.8) + diff2 * em2;
    return litRef * 0.55;
}

void main() {
    float t  = TIME * speed;
    float flyH  = mix(1.7, 26.0, flightMode);
    float lookH = mix(0.3,  6.0, flightMode);
    float swayX = sin(t * 0.35) * mix(1.5, 4.0, flightMode);
    float swayY = sin(t * 0.22) * mix(0.2, 3.0, flightMode);

    vec3 ro = vec3(swayX, flyH + swayY, t * 8.0);
    vec3 target = ro + vec3(sin(t*0.17)*2.0, lookH - flyH, 10.0);
    vec3 fwd = normalize(target - ro);
    vec3 rgt = normalize(cross(fwd, vec3(0,1,0)));
    vec3 up  = cross(rgt, fwd);

    vec2 uv = (gl_FragCoord.xy / RENDERSIZE.xy) * 2.0 - 1.0;
    uv.x *= RENDERSIZE.x / RENDERSIZE.y;
    vec3 rd = normalize(fwd + rgt * uv.x * 0.95 + up * uv.y * 0.95);

    // March
    float td = 0.0; vec2 hitCell; vec4 hitInfo; bool hit = false; vec3 hitPos;
    for (int i = 0; i < MAX_STEPS; i++) {
        hitPos = ro + rd * td;
        float d = mapScene(hitPos, hitCell, hitInfo);
        if (d < EPS) { hit = true; break; }
        if (td > MAX_DIST) break;
        td += max(d * 0.9, 0.02);
    }

    vec3 sky = skyColor(rd, timeOfDay);
    vec3 col;

    if (hit) {
        vec3 n = estimateNormal(hitPos);
        vec3 diffuse; float emission;
        surfaceShade(hitPos, n, hitCell, hitInfo, diffuse, emission);

        float sunHeight = sin(timeOfDay * 6.2831853 - 1.5707963);
        vec3  sunDir    = normalize(vec3(0.6, max(sunHeight, -0.1)+0.15, 0.4));
        float diff      = max(dot(n, sunDir), 0.0);
        float ambient   = 0.18 + 0.35 * max(sunHeight, 0.0);
        vec3  lit       = diffuse * (ambient + diff * max(sunHeight, 0.0) * 0.8);
        lit += diffuse * emission;

        // Wet-street reflection on ground plane
        bool isGround = (hitInfo.z < 0.0);
        if (isGround && wetStreet > 0.001) {
            vec3 reflRd = reflect(rd, n);
            vec3 reflCol = wetReflection(hitPos, reflRd, timeOfDay);
            // Puddle mask: random per world-XZ tile so reflections are patchy
            float puddleMask = smoothstep(0.55, 0.70,
                hash11(floor(hitPos.x * 0.5) * 17.3 + floor(hitPos.z * 0.5) * 11.7));
            // Fresnel: wider viewing angle = more reflective
            float fresnel = pow(1.0 - max(dot(-rd, n), 0.0), 2.0);
            lit = mix(lit, reflCol, wetStreet * puddleMask * fresnel * 0.8);
        }

        col = lit;

        float fogAmt = 1.0 - exp(-td * fog * 0.018);
        col = mix(col, sky, clamp(fogAmt, 0.0, 0.92));
    } else {
        col = sky;
    }

    // Mood: lift or crush the frame (post-fog, pre-HDR-out)
    col = mix(col * 0.35, col * 1.25, mood);

    // LINEAR HDR out — NO TONEMAP. Host handles compression.
    gl_FragColor = vec4(col, 1.0);
}
