/*{
  "DESCRIPTION": "Geo — 2D path-traced circle-circle intersection. Two orbiting circles compute their algebraic intersection points in real time; a 68-sample radial path tracer accumulates global illumination across the canvas, producing painterly glow against a graph-paper grid. Ported from Yusef28's Shadertoy (7l2XDm).",
  "CREDIT": "Original: Yusef28 (Shadertoy 7l2XDm). Port: ShaderClaw — adapted to ISF with procedural noise replacing iChannel0.",
  "CATEGORIES": ["Generator", "Audio Reactive"],
  "INPUTS": [
    { "NAME": "speed",          "LABEL": "Orbit Speed",    "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0,  "MAX": 3.0 },
    { "NAME": "radius1",        "LABEL": "Circle A Radius","TYPE": "float", "DEFAULT": 2.0, "MIN": 0.5,  "MAX": 5.0 },
    { "NAME": "radius2",        "LABEL": "Circle B Radius","TYPE": "float", "DEFAULT": 3.0, "MIN": 0.5,  "MAX": 6.0 },
    { "NAME": "samples",        "LABEL": "Path Samples",   "TYPE": "long",  "DEFAULT": 68, "VALUES": [16,32,48,68,96,128], "LABELS": ["16","32","48","68","96","128"] },
    { "NAME": "exposure",       "LABEL": "Exposure",       "TYPE": "float", "DEFAULT": 2.0, "MIN": 0.5,  "MAX": 4.0 },
    { "NAME": "gamma",          "LABEL": "Gamma",          "TYPE": "float", "DEFAULT": 0.75,"MIN": 0.4,  "MAX": 1.4 },
    { "NAME": "vignetteAmount", "LABEL": "Vignette",       "TYPE": "float", "DEFAULT": 0.15,"MIN": 0.0,  "MAX": 0.6 },
    { "NAME": "audioReact",     "LABEL": "Audio React",    "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0,  "MAX": 2.0 },
    { "NAME": "colorA",         "LABEL": "Circle A Tint",  "TYPE": "color", "DEFAULT": [0.8, 0.3, 0.7, 1.0] },
    { "NAME": "colorB",         "LABEL": "Circle B Tint",  "TYPE": "color", "DEFAULT": [0.2, 0.5, 0.9, 1.0] },
    { "NAME": "colorIntersect", "LABEL": "Intersection",   "TYPE": "color", "DEFAULT": [0.6, 0.7, 1.0, 1.0] }
  ]
}*/

// ====================================================================
// Geo — Circle-Circle Intersection with 2D Path Tracing
// Source: https://www.shadertoy.com/view/7l2XDm by Yusef28
// Adapted to ISF: iTime → TIME, iResolution → RENDERSIZE, iChannel0
// replaced with cheap procedural value noise. Added control knobs.
// ====================================================================

float rnd(vec2 uv) {
    return fract(sin(dot(uv, vec2(12.9898, 78.233))) * 43758.5453123);
}

// Cheap value noise — replaces iChannel0 texture sampling from the
// original shader. Two octaves are enough to keep the soft-grain
// "paper" feel of the graph background.
float vnoise(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = rnd(i);
    float b = rnd(i + vec2(1.0, 0.0));
    float c = rnd(i + vec2(0.0, 1.0));
    float d = rnd(i + vec2(1.0, 1.0));
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

void addObj(float dist, vec3 color, inout float endDist, inout vec3 endColor) {
    if (dist < endDist) {
        endDist = dist;
        endColor = color;
    }
}

void mapScene(vec2 uv, float t, inout float d, inout vec3 color) {
    // Two orbiting circle centers — sped/slowed via the `speed` knob.
    vec2 center1 = vec2(-4.0 * cos(t * 1.0), -3.0 * sin(t * 2.0));
    vec2 center2 = vec2( 0.8 * sin(t),        0.5 * cos(t));

    float c1 = radius1;
    float c2 = radius2;

    // Algebraic intersection: subtract the two circle equations to get a
    // radical line, parameterise as x = y*w - u, substitute back into
    // circle 1 to get a quadratic in y. Two roots = two intersection
    // points. Discriminant < 0 ⇒ no intersection (circles separated).
    float h1 = center1.x, k1 = center1.y;
    float h2 = center2.x, k2 = center2.y;

    float k3 = k1 - k2;
    float h3 = h1 - h2;
    float c3 = k1*k1 - (k2*k2) + h1*h1 - (h2*h2) - (c1*c1) + (c2*c2);
    float w = -(k3 / h3);
    float u = -c3 / (2.0 * h3);

    float a = w*w + 1.0;
    float b = 2.0 * (w * h1 + w * u + k1);
    float cc = (u*u) + 2.0 * u * h1 + k1*k1 - (c1*c1) + h1*h1;

    float disc = b*b - 4.0 * a * cc;
    float y1 = (-b + sqrt(max(disc, 0.0))) / (2.0 * a);
    float y2 = (-b - sqrt(max(disc, 0.0))) / (2.0 * a);
    float x1 = y1 * w + u;
    float x2 = y2 * w + u;
    vec2 ip1 = -vec2(x1, y1);
    vec2 ip2 = -vec2(x2, y2);

    d = 1e9;
    color = vec3(0.0);
    float f;

    // Circle A (always drawn).
    f = abs(length(uv - center1) - c1);
    addObj(f, colorA.rgb / 4.0, d, color);

    // Circle B — drawn only outside circle A so the intersection lobe
    // reads as a window through, not a stacked overlap.
    if (length(uv - center1) > c1) {
        f = abs(length(uv - center2) - c2);
        addObj(f, colorB.rgb / 3.0, d, color);
    }

    // Intersection markers — only when the discriminant is non-negative
    // (circles actually intersect).
    if (disc >= 0.0) {
        f = abs(length(uv - ip1) - 0.12);
        addObj(f, colorIntersect.rgb * 2.0, d, color);
        f = abs(length(uv - ip2) - 0.12);
        addObj(f, colorIntersect.rgb * 2.0, d, color);
    }

    // Two distant light sources orbiting off-canvas — keeps the path
    // tracer fed even when the main circles separate.
    f = abs(length(uv - vec2( 7.0, 4.0 * sin(t))) - 0.5);
    addObj(f, vec3(1.0, 1.0, 0.7), d, color);
    f = abs(length(uv + vec2( 7.0, 4.0 * cos(t))) - 0.5);
    addObj(f, vec3(1.4, 0.9, 0.5), d, color);
}

float trace(vec2 ro, vec2 rd, float t, inout vec3 color, vec3 grid) {
    float tt = 0.0;
    for (int i = 0; i < 30; i++) {
        float d;
        mapScene(ro + rd * tt, t, d, color);
        if (d < 0.0001 || tt > 10.0) break;
        tt += d;
    }
    if (tt > 10.0) color = grid;   // no hit → background grid sample
    return tt;
}

void main() {
    vec2 res = RENDERSIZE;
    // Time scaled by user knob + a gentle audio nudge on bass so the
    // composition pulses with low end without losing its rhythm.
    float audio = clamp(audioReact, 0.0, 2.0);
    float t = TIME * (speed + audioBass * audio * 0.4);

    // Aspect-preserving centred UV (matches original layout).
    vec2 uv = (gl_FragCoord.xy - res * 0.5) / res.y;

    // ── Graph paper background ──
    vec2 st = uv;
    uv *= 8.0;
    vec3 col = vec3(0.0);
    col = mix(col, vec3(0.16), 1.0 - length(uv / 8.0));
    // Soft procedural "paper" noise replaces the original iChannel0.
    float tex = vnoise(st * 80.0 + vec2(13.0, 7.0));
    col = mix(col, vec3(0.25), pow(tex, 2.0));
    // Fine grid
    vec2 lines = fract(uv * 5.0);
    lines = smoothstep(vec2(0.45), vec2(0.52), abs(lines - 0.5));
    col = mix(col, vec3(0.3), lines.x);
    col = mix(col, vec3(0.3), lines.y);
    // Major grid
    lines = fract(uv);
    lines = smoothstep(vec2(0.47), vec2(0.52), abs(lines - 0.5));
    col = mix(col, vec3(0.5), lines.x);
    col = mix(col, vec3(0.5), lines.y);
    // Axes
    lines = smoothstep(vec2(0.0), vec2(0.02), abs(uv));
    col = mix(col, vec3(0.6), 1.0 - lines.x);
    col = mix(col, vec3(0.6), 1.0 - lines.y);

    vec3 grid = col / 2.0;

    // ── Path-trace pass ──
    uv = (gl_FragCoord.xy - res * 0.5) / res.y * 10.0;
    vec2 ro = uv;
    vec2 rd;
    vec3 tmpColor;
    vec3 marchColor = vec3(0.0);

    int N = samples;
    if (N < 4)   N = 4;
    if (N > 256) N = 256;
    float Nf = float(N);

    for (int i = 0; i < 256; i++) {
        if (i >= N) break;
        float fi = float(i);
        float angle = (fi + rnd(uv + fi)) / Nf * 3.1415 * 2.0;
        rd = vec2(cos(angle), sin(angle));
        trace(ro, rd, t, tmpColor, grid);
        marchColor += tmpColor;
    }
    marchColor /= Nf;
    col = marchColor * exposure;

    // ── Post ──
    col = pow(max(col, 0.0), vec3(gamma));

    // Vignette
    vec2 sv = gl_FragCoord.xy / res;
    sv *= 1.0 - sv.yx;
    float vig = sv.x * sv.y * 15.0;
    vig = pow(max(vig, 0.0), max(vignetteAmount, 0.001));

    gl_FragColor = vec4(col * vig, 1.0);
}
