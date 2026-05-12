/*{
  "CATEGORIES": ["Filter", "Drawing", "Audio Reactive"],
  "DESCRIPTION": "Edges v5 — drawing as medium. Sobel-on-line-art: a wide coastal panorama — sea cliff, lighthouse tower, gulls, and horizon — drawn with bold SDF strokes in five moods (Charcoal/Pencil/Etching/Schiele/Hockney). HDR ink peaks 2.0+ so bloom bleeds; beacon lamp glows HDR amber. Linear HDR out. Audio drives line weight, jitter, hatching.",
  "CREDIT": "Easel / edges v5 — landscape composition",
  "INPUTS": [
    { "NAME": "inputTex",    "TYPE": "image" },
    { "NAME": "mood",        "LABEL": "Drawing Mood", "TYPE": "long",  "DEFAULT": 3,
      "VALUES": [0,1,2,3,4],
      "LABELS": ["Charcoal","Pencil","Etching","Schiele","Hockney"] },
    { "NAME": "lineWeight",  "LABEL": "Line Weight",  "TYPE": "float", "MIN": 0.4, "MAX": 3.0, "DEFAULT": 1.25 },
    { "NAME": "edgeGain",    "LABEL": "Edge Sensitivity", "TYPE": "float", "MIN": 0.3, "MAX": 4.0, "DEFAULT": 1.5 },
    { "NAME": "jitter",      "LABEL": "Hand-Drawn Jitter", "TYPE": "float", "MIN": 0.0, "MAX": 2.0, "DEFAULT": 0.85 },
    { "NAME": "hatchDensity","LABEL": "Cross-Hatching",  "TYPE": "float", "MIN": 0.0, "MAX": 2.0, "DEFAULT": 1.0 },
    { "NAME": "paperWarmth", "LABEL": "Paper Warmth",    "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.7 },
    { "NAME": "grain",       "LABEL": "Paper Grain",     "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.55 },
    { "NAME": "audioReact",  "LABEL": "Audio React",     "TYPE": "float", "MIN": 0.0, "MAX": 2.0, "DEFAULT": 1.0 }
  ]
}*/

// ════════════════════════════════════════════════════════════════════════
//  edges v4 — Picasso line subject + Sobel + drawing-mood re-render.
//  The procedural fallback is a STYLIZED FACE PROFILE drawn as ~6 thick
//  SDF strokes (chin curve, neck, hairline, nose ridge, ear curl, eye dot)
//  + one HOT red accent (lip / ear stud) for bloom seed.
//
//  Output is LINEAR HDR. Ink peaks 2.0+ linear, red accent 2.5 linear.
//  Host applies the tonemap.
// ════════════════════════════════════════════════════════════════════════

float hash11(float n) { return fract(sin(n * 12.9898) * 43758.5453); }
float hash21(vec2 p)  { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float hash22f(vec2 p) { return fract(sin(dot(p, vec2(269.5, 183.3))) * 51217.137); }

float vnoise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    float a = hash21(i);
    float b = hash21(i + vec2(1.0, 0.0));
    float c = hash21(i + vec2(0.0, 1.0));
    float d = hash21(i + vec2(1.0, 1.0));
    vec2 u = f * f * (3.0 - 2.0 * f);
    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

float fbm(vec2 p) {
    float v = 0.0, a = 0.5;
    for (int i = 0; i < 4; i++) { v += a * vnoise(p); p *= 2.03; a *= 0.5; }
    return v;
}

float lum(vec3 c) { return dot(c, vec3(0.2126, 0.7152, 0.0722)); }

// ─── SDF helpers for the face profile ────────────────────────────────────
// Distance from point p to segment a-b
float sdSeg(vec2 p, vec2 a, vec2 b) {
    vec2 pa = p - a, ba = b - a;
    float h = clamp(dot(pa, ba) / max(dot(ba, ba), 1e-6), 0.0, 1.0);
    return length(pa - ba * h);
}
// Quadratic bezier distance (approx by line subdivision)
float sdBezier(vec2 p, vec2 a, vec2 b, vec2 c) {
    float d = 1e9;
    vec2 prev = a;
    for (int i = 1; i <= 12; i++) {
        float t = float(i) / 12.0;
        vec2 q = mix(mix(a, b, t), mix(b, c, t), t);
        d = min(d, sdSeg(p, prev, q));
        prev = q;
    }
    return d;
}
// Arc segment (circle centered c, radius r, angle range [a0,a1])
float sdArc(vec2 p, vec2 c, float r, float a0, float a1) {
    vec2 d = p - c;
    float ang = atan(d.y, d.x);
    // wrap into [a0, a1]
    float clamped = clamp(ang, a0, a1);
    vec2 onArc = c + r * vec2(cos(clamped), sin(clamped));
    return length(p - onArc);
}

// ─── COASTAL PANORAMA ───────────────────────────────────────────────────
// Wide-format scene: sea cliff (left half), lighthouse tower (right-centre),
// three seagull arcs in sky, horizon line, simple wave suggestion at bottom.
// Returns ink density + HDR amber accent (lighthouse beacon lamp).
struct LineArt { float ink; float red; };

LineArt coastalScene(vec2 uv, float t) {
    LineArt o; o.ink = 0.0; o.red = 0.0;

    // Slight global sway (wind effect)
    float sway = 0.004 * sin(t * 0.42);
    vec2 p = uv + vec2(sway, 0.0);

    float wBold = 0.0055;   // cliffs, tower
    float wMid  = 0.0040;   // horizon, waves
    float wThin = 0.0028;   // gulls, detail

    // 1) HORIZON LINE — straight from left to right at y≈0.46
    {
        float d = abs(p.y - (0.455 + 0.004 * sin(t * 0.3)));
        o.ink = max(o.ink, smoothstep(wMid, wMid * 0.35, d));
    }

    // 2) CLIFF SILHOUETTE — left side, rugged diagonal bezier up from bottom-left
    {
        vec2 a  = vec2(0.02, 0.18);   // base of cliff
        vec2 b  = vec2(0.12, 0.38);   // mid-slope bulge
        vec2 cc = vec2(0.22, 0.46);   // cliff top at horizon
        float d = sdBezier(p, a, b, cc);
        o.ink = max(o.ink, smoothstep(wBold, wBold * 0.35, d));
        // Second craggy outcrop
        float d2 = sdSeg(p, vec2(0.08, 0.28), vec2(0.05, 0.20));
        o.ink = max(o.ink, smoothstep(wMid, wMid * 0.4, d2));
    }

    // 3) LIGHTHOUSE TOWER — vertical rectangle shaft, centred at x≈0.62
    //    tapered: slightly wider at base than top
    {
        float tx = 0.62;
        // Shaft left edge
        float d1 = sdSeg(p, vec2(tx - 0.020, 0.46), vec2(tx - 0.014, 0.62));
        // Shaft right edge
        float d2 = sdSeg(p, vec2(tx + 0.020, 0.46), vec2(tx + 0.014, 0.62));
        // Base platform
        float d3 = sdSeg(p, vec2(tx - 0.028, 0.46), vec2(tx + 0.028, 0.46));
        // Lantern house top arc
        float d4 = sdArc(p, vec2(tx, 0.64), 0.018, 0.0, 3.14159);
        o.ink = max(o.ink, smoothstep(wBold, wBold * 0.35, min(min(d1, d2), min(d3, d4))));

        // BEACON LAMP — single saturated dot at lantern centre (HDR amber accent)
        float dLamp = length(p - vec2(tx + 0.001 * sin(t * 1.2), 0.645));
        o.red = max(o.red, smoothstep(0.014, 0.004, dLamp));
    }

    // 4) WAVE SUGGESTIONS — short sub-horizon arc strokes
    {
        float wy = 0.38 + 0.01 * sin(t * 0.8);
        float d1 = sdArc(p, vec2(0.28, wy), 0.045, -0.8, 0.8);
        float d2 = sdArc(p, vec2(0.45, wy - 0.025), 0.035, -0.9, 0.9);
        float d3 = sdArc(p, vec2(0.15, wy + 0.018), 0.030, -0.7, 1.0);
        o.ink = max(o.ink, smoothstep(wThin, wThin * 0.4, min(min(d1, d2), d3)));
    }

    // 5) SEAGULLS — three M-arch pairs in the sky
    {
        // Each gull: two short arc segments opening upward
        for (int i = 0; i < 3; i++) {
            float fi = float(i);
            float gx = 0.30 + fi * 0.22 + 0.012 * sin(t * (0.5 + fi * 0.3) + fi * 1.7);
            float gy = 0.72 + fi * 0.04 + 0.008 * cos(t * (0.4 + fi * 0.2));
            float r  = 0.020 - fi * 0.003;
            float dL = sdArc(p, vec2(gx - r, gy), r, -2.8, 0.0);
            float dR = sdArc(p, vec2(gx + r, gy), r,  0.0, 2.8);
            o.ink = max(o.ink, smoothstep(wThin, wThin * 0.35, min(dL, dR)));
        }
    }

    // 6) DISTANT HEADLAND — faint bump on horizon, far right
    {
        vec2 a = vec2(0.78, 0.455);
        vec2 b = vec2(0.85, 0.480);
        vec2 cc= vec2(0.92, 0.455);
        float d = sdBezier(p, a, b, cc);
        o.ink = max(o.ink, smoothstep(wThin, wThin * 0.4, d));
    }

    return o;
}

// Read input image (or fallback) as a luminance field for Sobel.
// In fallback mode we render the LINE ART itself (dark ink on light paper)
// so Sobel finds those edges and the mood re-render kicks in.
float field(vec2 uv, float t, bool useFallback) {
    if (useFallback) {
        LineArt la = coastalScene(uv, t);
        // Dark ink on cream: 1.0 paper, 0.0 ink.
        // Red accent pushes a separate dark band so Sobel finds it too.
        float v = 1.0 - la.ink * 0.95 - la.red * 0.6;
        // Faint tonal background gradient so the field isn't perfectly flat
        v -= 0.04 * (uv.y - 0.5);
        return clamp(v, 0.0, 1.0);
    }
    return lum(IMG_NORM_PIXEL(inputTex,
                clamp(uv, vec2(0.0), vec2(1.0))).rgb);
}

vec3 sobel(vec2 uv, vec2 px, float t, bool useFallback) {
    float tl = field(uv + vec2(-px.x,  px.y), t, useFallback);
    float  l = field(uv + vec2(-px.x,   0.0), t, useFallback);
    float bl = field(uv + vec2(-px.x, -px.y), t, useFallback);
    float  T = field(uv + vec2(  0.0,  px.y), t, useFallback);
    float  B = field(uv + vec2(  0.0, -px.y), t, useFallback);
    float tr = field(uv + vec2( px.x,  px.y), t, useFallback);
    float  R = field(uv + vec2( px.x,   0.0), t, useFallback);
    float br = field(uv + vec2( px.x, -px.y), t, useFallback);

    float gx = (tr + 2.0 * R + br) - (tl + 2.0 * l + bl);
    float gy = (tl + 2.0 * T + tr) - (bl + 2.0 * B + br);
    float mag = sqrt(gx * gx + gy * gy);
    float ang = atan(gy, gx);
    return vec3(mag, ang, (T + B + l + R) * 0.25);
}

float hatchAt(vec2 pxPos, float angle, float pitch, float thickness) {
    float c = cos(angle), s = sin(angle);
    float u = -s * pxPos.x + c * pxPos.y;
    float v = mod(u, pitch);
    float d = min(v, pitch - v);
    return smoothstep(thickness, thickness * 0.4, d);
}

void main() {
    vec2 uv  = isf_FragNormCoord.xy;
    vec2 res = RENDERSIZE.xy;
    float t  = TIME;

    float aR     = clamp(audioReact, 0.0, 2.0);
    float aBass  = aR;
    float aMid   = aR * 0.85;
    float aHigh  = aR * 0.7;

    bool useFallback = (IMG_SIZE(inputTex).x < 1.0) ||
                       (IMG_SIZE(inputTex).y < 1.0);

    int   modeI = int(mood + 0.5);
    float lw    = lineWeight * (1.0 + 0.55 * aBass);
    float jit   = jitter     * (0.5 + 0.9  * aMid);
    float hd    = hatchDensity * (0.5 + 0.9 * aHigh);

    vec2 pxSize = 1.0 / res;
    vec2 jpos   = uv * res;
    float n1 = hash22f(floor(jpos) + floor(t * 1.3));
    float n2 = hash22f(floor(jpos) + 47.0 + floor(t * 1.3));
    vec2 jdir = (vec2(n1, n2) - 0.5) * 2.0;
    float jScale = 0.0;
    if      (modeI == 0) jScale = 1.1;
    else if (modeI == 1) jScale = 0.25;
    else if (modeI == 2) jScale = 0.15;
    else if (modeI == 3) jScale = 1.6;
    else                  jScale = 0.30;
    vec2 sampleUV = uv + jdir * pxSize * jScale * jit;

    vec2 px2 = pxSize * 1.0;
    vec3 s = sobel(sampleUV, px2, t, useFallback);
    float mag = s.x * edgeGain;
    float ang = s.y;
    float midTone = field(sampleUV, t, useFallback);

    // For the procedural fallback the line-art itself is the subject —
    // sample the ink mask directly so we get strong solid strokes,
    // then OR with the Sobel response for crisp edge boost.
    LineArt la;
    la.ink = 0.0; la.red = 0.0;
    if (useFallback) la = coastalScene(sampleUV, t);

    float sobelInk = smoothstep(0.05, 0.55, mag);
    sobelInk *= mix(0.7, 1.6, lw * 0.5);

    // Strong combined ink: solid SDF strokes win, Sobel adds crispness.
    float ink = useFallback ? max(la.ink, sobelInk * 0.85) : sobelInk;

    float tooth = fbm(uv * 220.0);
    if (modeI == 0) ink *= mix(0.6, 1.1, tooth);
    if (modeI == 3) {
        float lift = vnoise(uv * 90.0 + t * 0.3);
        ink *= smoothstep(0.14, 0.42, lift);
    }

    // ─── Cross-hatching in shadow zones (kept; reads great on cream)
    float shadow = 1.0 - smoothstep(0.15, 0.55, midTone);
    float hatch = 0.0;
    vec2 pxPos = uv * res;
    if (hd > 0.01 && (modeI == 1 || modeI == 2 || modeI == 0)) {
        float pitch1 = mix(9.0, 5.0, clamp(hd * 0.5, 0.0, 1.0));
        float pitch2 = pitch1 * 1.3;
        float thick  = mix(0.7, 1.4, lw * 0.4);
        float h1 = hatchAt(pxPos, 0.78, pitch1, thick);
        float h2 = hatchAt(pxPos, -0.78, pitch2, thick);
        if (modeI == 2) {
            float h3 = hatchAt(pxPos, 1.57, pitch1 * 1.6, thick * 0.7);
            hatch = max(h1, max(h2, h3 * 0.7));
        } else if (modeI == 0) {
            hatch = max(h1, h2) * 0.65;
        } else {
            hatch = max(h1, h2);
        }
        hatch *= shadow * hd;
    }

    // ─── Cream paper background, warm and grainy.
    vec3 paperWarm = vec3(0.985, 0.94, 0.84);   // warm cream (pushed warmer)
    vec3 paperCool = vec3(0.97, 0.965, 0.94);
    vec3 paper = mix(paperCool, paperWarm, paperWarmth);
    float vert = 0.5 + 0.5 * sin(uv.x * res.x * 0.15);
    float grainN = fbm(uv * vec2(180.0, 60.0));
    paper *= 1.0 - 0.05 * grain * (vert * 0.4 + grainN * 0.8 - 0.5);
    float stain = fbm(uv * 3.0 + 4.0);
    paper *= 1.0 - 0.05 * (stain - 0.5);

    // ─── Stroke colour by mood (linear-light, keep DEEP black for HDR push)
    vec3 inkColor;
    if      (modeI == 0) inkColor = vec3(0.020, 0.018, 0.016); // Charcoal
    else if (modeI == 1) inkColor = vec3(0.16, 0.16, 0.18);    // Pencil graphite
    else if (modeI == 2) inkColor = vec3(0.018, 0.014, 0.010); // Etching deep black
    else if (modeI == 3) inkColor = vec3(0.26, 0.14, 0.07);    // Schiele raw umber
    else                  inkColor = vec3(0.08, 0.14, 0.38);   // Hockney cobalt

    if (modeI == 4) {
        float a01 = (ang + 3.14159) / 6.28318;
        float idx = floor(a01 * 5.0);
        if      (idx < 0.5) inkColor = vec3(0.10, 0.30, 0.78);
        else if (idx < 1.5) inkColor = vec3(0.85, 0.18, 0.16);
        else if (idx < 2.5) inkColor = vec3(0.16, 0.55, 0.28);
        else if (idx < 3.5) inkColor = vec3(0.95, 0.70, 0.16);
        else                inkColor = vec3(0.55, 0.20, 0.55);
    }

    vec3 base = paper;
    if (modeI == 4) {
        float band = floor(midTone * 4.0) / 4.0;
        vec3 flatCol;
        if      (band < 0.26) flatCol = vec3(0.95, 0.78, 0.55);
        else if (band < 0.51) flatCol = vec3(0.55, 0.78, 0.92);
        else if (band < 0.76) flatCol = vec3(0.82, 0.92, 0.78);
        else                  flatCol = vec3(0.99, 0.95, 0.86);
        base = mix(paper, flatCol, 0.55);
    }

    // ─── Compose: hatch under, ink line on top.
    vec3 col = base;
    col = mix(col, inkColor, hatch * 0.55);
    float strokeAlpha = clamp(ink, 0.0, 1.0);
    col = mix(col, inkColor, strokeAlpha);

    // ─── HDR INK BLOOM SEED — push solid strokes >2.0 in linear light.
    // The black ink absorbs paper; the OVERSHOOT here is what bloom grabs.
    // We add a darker-than-black "anti-light" then a sharp bright rim.
    if (useFallback || strokeAlpha > 0.2) {
        // Sharp edge rim from Sobel — bright sliver at the stroke boundary
        float rim = smoothstep(0.30, 0.85, mag) * (1.0 - smoothstep(0.85, 0.99, strokeAlpha));
        // Mood-tinted rim hot-spot
        vec3 rimCol;
        if      (modeI == 0) rimCol = vec3(2.20, 2.10, 1.95);   // charcoal: white slash
        else if (modeI == 1) rimCol = vec3(2.00, 2.00, 2.05);   // pencil: cool white
        else if (modeI == 2) rimCol = vec3(2.30, 2.20, 2.00);   // etching: hot burin
        else if (modeI == 3) rimCol = vec3(2.10, 1.70, 1.20);   // schiele: amber
        else                  rimCol = vec3(2.00, 2.05, 2.30);  // hockney: cool pop
        col += rimCol * rim * 0.55;
    }

    // ─── Mood-specific HDR flourishes (kept, intensified)
    if (modeI == 0) {
        float smudge = smoothstep(0.10, 0.45, mag) * 0.22 * tooth;
        col = mix(col, inkColor, smudge);
        float slash = smoothstep(0.78, 1.0, strokeAlpha)
                    * smoothstep(0.55, 0.95, tooth);
        col += vec3(2.10, 2.04, 1.95) * slash * 0.95;
    }
    if (modeI == 3) {
        float wash = (1.0 - midTone) * 0.20 * fbm(uv * 8.0);
        col = mix(col, vec3(0.45, 0.30, 0.18), wash);
    }
    if (modeI == 2) {
        float crest = smoothstep(0.55, 0.95, mag);
        float catchN = vnoise(uv * 320.0);
        float etchHi = crest * smoothstep(0.62, 0.85, catchN);
        col += vec3(2.30, 2.22, 2.05) * etchHi * 0.85;
    }
    if (modeI == 4) {
        float pop = smoothstep(0.85, 1.0, strokeAlpha);
        col += vec3(1.80, 1.70, 1.55) * pop * 0.5;
    }

    // ─── HOT RED ACCENT — the ear-stud / lip dot.
    // Single saturated red, peaks at 2.5 linear so bloom blooms HARD.
    if (useFallback) {
        // Re-render the red mask from current sampleUV to anti-alias with jitter.
        float redMask = la.red;
        // Mood-tinted red (kept saturated; just shift hue subtly)
        vec3 redCol;
        if      (modeI == 0) redCol = vec3(2.50, 0.30, 0.20);   // crimson
        else if (modeI == 1) redCol = vec3(2.40, 0.45, 0.30);   // rose
        else if (modeI == 2) redCol = vec3(2.55, 0.25, 0.15);   // vermillion
        else if (modeI == 3) redCol = vec3(2.45, 0.40, 0.20);   // schiele red
        else                  redCol = vec3(2.50, 0.20, 0.30);  // hockney red
        col = mix(col, redCol, redMask);
    }

    // ─── Paper hand-feel & vignette
    col *= 1.0 - 0.04 * grain * (fbm(uv * 800.0) - 0.5);
    vec2 vc = uv - 0.5;
    col *= 1.0 - 0.18 * dot(vc, vc) * 1.6;

    col = max(col, vec3(0.0));
    gl_FragColor = vec4(col, 1.0);
}
