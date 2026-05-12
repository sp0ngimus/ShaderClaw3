/*{
  "DESCRIPTION": "Neon Circuit Board — 3D raymarched PCB surface. SDF circuit traces (copper extrusions) on dark substrate, RGB data pulses traveling along horizontal and vertical routes. Low-angle orbit camera. LINEAR HDR out, no tonemap.",
  "CREDIT": "ShaderClaw auto-improve 2026-05-12",
  "CATEGORIES": ["Generator", "3D", "Audio Reactive"],
  "INPUTS": [
    { "NAME": "traceColor",  "LABEL": "Trace Metal",  "TYPE": "color", "DEFAULT": [0.90, 0.60, 0.10, 1.0] },
    { "NAME": "boardColor",  "LABEL": "PCB Base",     "TYPE": "color", "DEFAULT": [0.04, 0.12, 0.06, 1.0] },
    { "NAME": "pulseSpeed",  "LABEL": "Pulse Speed",  "TYPE": "float", "MIN": 0.0, "MAX": 1.5, "DEFAULT": 0.55 },
    { "NAME": "orbitSpeed",  "LABEL": "Orbit Speed",  "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.08 },
    { "NAME": "hdrPeak",     "LABEL": "Pulse HDR",    "TYPE": "float", "MIN": 1.0, "MAX": 6.0, "DEFAULT": 3.0 },
    { "NAME": "traceGrid",   "LABEL": "Trace Spacing","TYPE": "float", "MIN": 0.05,"MAX": 0.5, "DEFAULT": 0.20 },
    { "NAME": "audioReact",  "LABEL": "Audio React",  "TYPE": "float", "MIN": 0.0, "MAX": 2.0, "DEFAULT": 1.0 }
  ]
}*/

#define MAX_STEPS 72
#define EPS       0.0025
#define PI        3.14159265

float h11(float n) { return fract(sin(n*127.1) * 43758.5453); }
float h21(vec2  p) { return fract(sin(dot(p, vec2(127.1,311.7))) * 43758.5453); }

// Flat PCB plane (y=0, thickness 0.05)
float sdBoard(vec3 p) { return abs(p.y + 0.025) - 0.025; }

// Horizontal trace at quantised Z position; extends full X range
float sdHTrace(vec3 p, float sp) {
    float zSlot  = round(p.z / sp);
    float active = step(0.45, h11(zSlot * 5.37 + 11.0));    // ~55% present
    float dz     = abs(p.z - zSlot * sp) - 0.012;
    float dy     = abs(p.y - 0.038) - 0.016;
    return max(max(dz, dy), 0.0) + active * 1e4;
}

// Vertical trace at quantised X position; extends full Z range
float sdVTrace(vec3 p, float sp) {
    float xSlot  = round(p.x / sp);
    float active = step(0.45, h11(xSlot * 7.13 + 23.0));    // ~55% present
    float dx     = abs(p.x - xSlot * sp) - 0.012;
    float dy     = abs(p.y - 0.038) - 0.016;
    return max(max(dx, dy), 0.0) + active * 1e4;
}

// Via pad at trace intersections (small disc extrusion)
float sdVia(vec3 p, float sp) {
    float xSlot = round(p.x / sp);
    float zSlot = round(p.z / sp);
    bool  hPresent = h11(zSlot*5.37+11.0) > 0.45;
    bool  vPresent = h11(xSlot*7.13+23.0) > 0.45;
    if (!hPresent && !vPresent) return 1e4;
    float cx = xSlot * sp, cz = zSlot * sp;
    float dr = length(vec2(p.x - cx, p.z - cz)) - 0.022;
    float dy = abs(p.y - 0.048) - 0.012;
    return max(dr, dy);
}

float mapScene(vec3 p, float sp) {
    float d = sdBoard(p);
    d = min(d, sdHTrace(p, sp));
    d = min(d, sdVTrace(p, sp));
    d = min(d, sdVia(p, sp));
    return d;
}

vec3 calcNormal(vec3 p, float sp) {
    const vec2 e = vec2(EPS, 0.0);
    return normalize(vec3(
        mapScene(p+e.xyy,sp) - mapScene(p-e.xyy,sp),
        mapScene(p+e.yxy,sp) - mapScene(p-e.yxy,sp),
        mapScene(p+e.yyx,sp) - mapScene(p-e.yyx,sp)
    ));
}

// Data pulses: HDR dots traveling along H and V traces
vec3 dataPulse(vec3 p, float sp) {
    vec3 acc = vec3(0.0);
    float bass = clamp(audioBass, 0.0, 1.0) * audioReact;

    // Horizontal pulses along Z-slotted H-traces
    for (int i = 0; i < 6; i++) {
        float fi  = float(i);
        float slot = floor(fi * 1.73 + 0.5);
        if (h11(slot*5.37+11.0) < 0.45) continue;   // trace absent
        float pz   = slot * sp;
        float spd  = (0.4 + h11(fi*3.7) * 0.6) * pulseSpeed * (1.0 + bass * 0.4);
        float px   = fract(TIME * spd + h11(fi*5.3)) * 6.0 - 3.0;
        float d2   = dot(p.xz - vec2(px, pz), p.xz - vec2(px, pz));
        // RGB split: R leads, B trails
        float r    = exp(-dot(p.xz - vec2(px+0.012,pz), p.xz - vec2(px+0.012,pz)) * 220.0);
        float g    = exp(-d2 * 240.0);
        float b    = exp(-dot(p.xz - vec2(px-0.012,pz), p.xz - vec2(px-0.012,pz)) * 220.0);
        acc += vec3(r, g, b) * (1.2 + h11(fi*2.1)*0.8);
    }

    // Vertical pulses along X-slotted V-traces
    for (int i = 0; i < 5; i++) {
        float fi   = float(i);
        float slot = floor(fi * 2.31 + 0.7);
        if (h11(slot*7.13+23.0) < 0.45) continue;
        float px   = slot * sp;
        float spd  = (0.35 + h11(fi*4.1) * 0.55) * pulseSpeed * 0.85 * (1.0 + bass * 0.4);
        float pz   = fract(TIME * spd * 0.8 + h11(fi*6.7)) * 6.0 - 3.0;
        float r    = exp(-dot(p.xz - vec2(px,pz+0.012), p.xz - vec2(px,pz+0.012)) * 220.0);
        float g    = exp(-dot(p.xz - vec2(px,pz),       p.xz - vec2(px,pz))       * 240.0);
        float b    = exp(-dot(p.xz - vec2(px,pz-0.012), p.xz - vec2(px,pz-0.012)) * 220.0);
        acc += vec3(r, g, b) * (1.0 + h11(fi*3.3)*0.7);
    }
    return acc;
}

void main() {
    vec2 uv = (gl_FragCoord.xy / RENDERSIZE.xy) * 2.0 - 1.0;
    uv.x *= RENDERSIZE.x / RENDERSIZE.y;

    float bass = clamp(audioBass, 0.0, 1.0) * audioReact;
    float mid  = clamp(audioMid,  0.0, 1.0) * audioReact;
    float sp   = traceGrid;

    // Low-angle orbit — sweeps across the PCB surface
    float ang  = TIME * orbitSpeed;
    float camH = 0.32 + 0.08*sin(TIME * 0.19);
    vec3 ro    = vec3(sin(ang)*2.6, camH, cos(ang)*2.6);
    vec3 target = vec3(0.0, 0.02, 0.0);
    vec3 fwd   = normalize(target - ro);
    vec3 rgt   = normalize(cross(vec3(0,1,0), fwd));
    vec3 upV   = cross(fwd, rgt);
    vec3 rd    = normalize(fwd + rgt*uv.x*0.80 + upV*uv.y*0.80);

    // Raymarch
    float t   = 0.01;
    bool  hit = false;
    for (int i = 0; i < MAX_STEPS; i++) {
        vec3 p = ro + rd*t;
        float d = mapScene(p, sp);
        if (d < EPS) { hit = true; break; }
        if (t > 7.0) break;
        t += d * 0.85;
    }

    vec3 col = vec3(0.0);

    if (hit) {
        vec3 p = ro + rd * t;
        vec3 n = calcNormal(p, sp);
        vec3 v = -rd;

        bool onTrace = (p.y > 0.015);
        vec3 surfCol  = onTrace ? traceColor.rgb : boardColor.rgb;

        // Key light from above-front; soft ambient
        vec3 lKey = normalize(vec3(0.4, 2.2, 0.6));
        float dKey = max(dot(n, lKey), 0.0);
        vec3  hK   = normalize(lKey + v);
        float sp2  = pow(max(dot(n, hK), 0.0), onTrace ? 64.0 : 12.0);

        vec3 lit  = surfCol * (0.12 + 0.88*dKey);
        lit += vec3(0.95, 0.90, 0.75) * sp2 * (onTrace ? 2.2 : 0.35);

        // Data pulse glow (additive, in HDR range)
        vec3 pulse = dataPulse(p, sp) * hdrPeak * (1.0 + bass * (1.0 + 0.5 * float(onTrace)));
        lit += pulse * (onTrace ? 1.0 : 0.25);   // pulse brighter on traces

        // fwidth edge AA on trace top surface
        float eM = clamp(fwidth(p.y) * 300.0, 0.0, 1.0);
        lit *= 1.0 - eM * 0.30;

        // Depth fog (subtle)
        float fog = exp(-t * 0.12);
        lit = mix(vec3(0.008, 0.022, 0.012), lit, fog);

        col = lit;
    } else {
        // Dark studio background
        col = vec3(0.008, 0.018, 0.010)
            + vec3(0.04, 0.10, 0.05) * exp(-length(uv)*1.8);
    }

    // Volumetric pulse glimmer along ray (very faint — gives depth)
    vec3 midP = ro + rd * min(t, 4.0);
    col += dataPulse(midP, sp) * vec3(0.0, 0.012, 0.006) * (1.0 + mid * 0.5);

    // LINEAR HDR — no tonemap, no clamp
    gl_FragColor = vec4(col, 1.0);
}
