/*{
  "DESCRIPTION": "Soap Bubble Iridescence — 3D raymarched glass sphere with thin-film rainbow interference. Hue sweeps around the surface with viewing angle; HDR specular peaks on twin glints. Calm orbital camera. LINEAR HDR out, no tonemap.",
  "CREDIT": "ShaderClaw auto-improve 2026-05-12",
  "CATEGORIES": ["Generator", "3D", "Audio Reactive"],
  "INPUTS": [
    { "NAME": "bubbleSize",  "LABEL": "Bubble Size",  "TYPE": "float", "MIN": 0.3, "MAX": 1.5,  "DEFAULT": 0.85 },
    { "NAME": "filmSpeed",   "LABEL": "Film Speed",   "TYPE": "float", "MIN": 0.0, "MAX": 2.0,  "DEFAULT": 0.35 },
    { "NAME": "filmTurns",   "LABEL": "Hue Turns",    "TYPE": "float", "MIN": 1.0, "MAX": 6.0,  "DEFAULT": 2.5 },
    { "NAME": "orbitSpeed",  "LABEL": "Orbit Speed",  "TYPE": "float", "MIN": 0.0, "MAX": 1.0,  "DEFAULT": 0.07 },
    { "NAME": "specPeak",    "LABEL": "Specular HDR", "TYPE": "float", "MIN": 1.0, "MAX": 6.0,  "DEFAULT": 3.5 },
    { "NAME": "audioReact",  "LABEL": "Audio React",  "TYPE": "float", "MIN": 0.0, "MAX": 2.0,  "DEFAULT": 1.0 }
  ]
}*/

#define PI         3.14159265
#define MAX_STEPS  64
#define MAX_DIST   12.0
#define EPS        0.0012

vec3 hsv2rgb(vec3 c) {
    vec4 K = vec4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

float sdSphere(vec3 p, float r) { return length(p) - r; }

vec3 sphNorm(vec3 p, float r) {
    vec2 e = vec2(EPS, 0.0);
    return normalize(vec3(
        sdSphere(p+e.xyy,r) - sdSphere(p-e.xyy,r),
        sdSphere(p+e.yxy,r) - sdSphere(p-e.yxy,r),
        sdSphere(p+e.yyx,r) - sdSphere(p-e.yyx,r)
    ));
}

void main() {
    vec2 uv = (gl_FragCoord.xy / RENDERSIZE.xy) * 2.0 - 1.0;
    uv.x *= RENDERSIZE.x / RENDERSIZE.y;

    float bass = clamp(audioBass, 0.0, 1.0) * audioReact;
    float high = clamp(audioHigh, 0.0, 1.0) * audioReact;

    // Calm orbital camera
    float ang = TIME * orbitSpeed;
    vec3 ro  = vec3(sin(ang) * 2.8, 0.35 + 0.2*sin(TIME*0.22), cos(ang) * 2.8);
    vec3 fwd = normalize(-ro);
    vec3 rgt = normalize(cross(vec3(0,1,0), fwd));
    vec3 upV = cross(fwd, rgt);
    vec3 rd  = normalize(fwd + rgt*uv.x*0.65 + upV*uv.y*0.65);

    float R = bubbleSize * (1.0 + bass * 0.07);

    // Sphere raymarch
    float t = 0.1;
    bool hit = false;
    for (int i = 0; i < MAX_STEPS; i++) {
        float d = sdSphere(ro + rd*t, R);
        if (d < EPS) { hit = true; break; }
        if (t > MAX_DIST) break;
        t += d;
    }

    vec3 col = vec3(0.0);

    if (hit) {
        vec3 p = ro + rd * t;
        vec3 n = sphNorm(p, R);
        vec3 v = -rd;

        // Thin-film iridescence — hue driven by Fresnel angle + latitude
        float fres   = 1.0 - max(dot(n, v), 0.0);
        float posAng = atan(p.y, length(p.xz)) / PI;
        float hue    = fract(fres * filmTurns + posAng * 0.5 + TIME * filmSpeed * 0.04 + high * 0.15);
        vec3 film1   = hsv2rgb(vec3(hue, 1.0, 1.0)) * 1.6;

        // Cross-polarization shimmer band
        float hue2   = fract(hue + 0.37 + TIME * filmSpeed * 0.017);
        vec3 film2   = hsv2rgb(vec3(hue2, 0.92, 1.0));
        vec3 film    = mix(film1, film2, 0.28);

        // Two key lights
        vec3 lKey  = normalize(vec3( 1.4, 1.2, 0.8));
        vec3 lFill = normalize(vec3(-0.8, 0.5, 1.2));
        float dKey  = max(dot(n, lKey),  0.0);
        float dFill = max(dot(n, lFill), 0.0) * 0.35;

        // Specular — sharp soap-bubble glints (HDR)
        vec3 hK = normalize(lKey  + v);
        vec3 hF = normalize(lFill + v);
        float spK = pow(max(dot(n, hK), 0.0), 256.0);
        float spF = pow(max(dot(n, hF), 0.0), 80.0);

        col  = film * (0.22 + 0.78*dKey + dFill);
        col += vec3(1.0, 0.97, 0.93) * spK * specPeak;        // warm white glint  HDR
        col += vec3(0.65, 0.88, 1.0) * spF * specPeak * 0.4;  // cool fill glint

        // Fresnel rim: darken at silhouette so bubble reads as translucent
        float rim = pow(fres, 3.5);
        col = mix(col, vec3(0.0), rim * 0.32);

        // Audio-reactive glow breath
        col += film * bass * 0.35;
    }

    // Void background — faint radial nebula so bubble has context
    float bgR = length(uv);
    col += vec3(0.025, 0.008, 0.05) * exp(-bgR * bgR * 0.8);
    col += vec3(0.01, 0.02, 0.04)  * (1.0 - bgR * 0.5);

    // LINEAR HDR — no tonemap, no clamp
    gl_FragColor = vec4(col, 1.0);
}
