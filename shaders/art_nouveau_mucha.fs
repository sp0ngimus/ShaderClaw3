/*{
  "CATEGORIES": ["Generator", "Art Movement", "Audio Reactive"],
  "DESCRIPTION": "Art Nouveau ornament frame after Mucha posters / vectorstock floral-swirl frames — heavy ornate border running the canvas perimeter, sinuous whiplash S-curves swirling around the edges, stylized lily/iris cartouches at corners and mid-spans. Mauve + gold + black palette. The interior is left open for a texture or live image to read through.",
  "INPUTS": [
    { "NAME": "frameWidth",     "LABEL": "Frame Width",     "TYPE":"float","MIN":0.05,"MAX":0.30, "DEFAULT":0.14 },
    { "NAME": "frameTint",      "LABEL": "Frame Tint",      "TYPE":"color","DEFAULT":[0.5647, 0.2941, 0.5098, 1.0] },
    { "NAME": "goldStrength",   "LABEL": "Gold Strength",   "TYPE":"float","MIN":0.0, "MAX":1.0,  "DEFAULT":0.75 },
    { "NAME": "swirlCount",     "LABEL": "Swirl Strands",   "TYPE":"float","MIN":2.0, "MAX":12.0, "DEFAULT":7.0 },
    { "NAME": "swirlAmplitude", "LABEL": "Swirl Amplitude", "TYPE":"float","MIN":0.0, "MAX":0.30, "DEFAULT":0.10 },
    { "NAME": "swirlFreq",      "LABEL": "Swirl Frequency", "TYPE":"float","MIN":1.0, "MAX":12.0, "DEFAULT":4.5 },
    { "NAME": "swirlWidth",     "LABEL": "Swirl Width",     "TYPE":"float","MIN":0.001,"MAX":0.012,"DEFAULT":0.0035 },
    { "NAME": "rotateSpeed",    "LABEL": "Animation Speed", "TYPE":"float","MIN":0.0, "MAX":1.5,  "DEFAULT":0.25 },
    { "NAME": "lilyShow",       "LABEL": "Corner Lilies",   "TYPE":"float","MIN":0.0, "MAX":1.0,  "DEFAULT":0.85 },
    { "NAME": "lilySize",       "LABEL": "Lily Size",       "TYPE":"float","MIN":0.04,"MAX":0.18, "DEFAULT":0.10 },
    { "NAME": "midCartouche",   "LABEL": "Mid Cartouches",  "TYPE":"float","MIN":0.0, "MAX":1.0,  "DEFAULT":0.65 },
    { "NAME": "innerLine",      "LABEL": "Inner Frame Line","TYPE":"float","MIN":0.0, "MAX":0.012,"DEFAULT":0.003 },
    { "NAME": "centerFlower",   "LABEL": "Center Flower",   "TYPE":"float","MIN":0.0, "MAX":1.0,  "DEFAULT":0.0 },
    { "NAME": "interiorWash",   "LABEL": "Interior Wash",   "TYPE":"float","MIN":0.0, "MAX":1.0,  "DEFAULT":0.20 },
    { "NAME": "audioReact",     "LABEL": "Audio React",     "TYPE":"float","MIN":0.0, "MAX":2.0,  "DEFAULT":1.0 },
    { "NAME": "inputTex",       "LABEL": "Texture",         "TYPE":"image" }
  ]
}*/

const vec3 PALE_CREAM = vec3(0.96, 0.92, 0.78);
const vec3 GOLD       = vec3(0.95, 0.78, 0.30);
const vec3 BRONZE     = vec3(0.72, 0.52, 0.16);
const vec3 INK        = vec3(0.10, 0.06, 0.10);

float hash11(float n) { return fract(sin(n * 12.9898) * 43758.5453); }
float hash21(vec2 p)  { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float vnoise(vec2 p) {
    vec2 ip = floor(p), fp = fract(p);
    fp = fp * fp * (3.0 - 2.0 * fp);
    float a = hash21(ip);
    float b = hash21(ip + vec2(1.0, 0.0));
    float c = hash21(ip + vec2(0.0, 1.0));
    float d = hash21(ip + vec2(1.0, 1.0));
    return mix(mix(a, b, fp.x), mix(c, d, fp.x), fp.y);
}

// ──────────────────────────────────────────────────────────────────────
// Distance from a point to a sinuous curve that tracks the canvas edge.
// The curve follows the outer rectangle perimeter at offset `offset` from
// the edge, modulated by sin waves to produce whiplash S-curves.
// Returns the closest distance over a sampled set of perimeter points.
// ──────────────────────────────────────────────────────────────────────
float distToFrameSwirl(vec2 uv, float offset, float amplitude, float frequency, float phase) {
    float minD = 1e9;
    // Walk perimeter parameter t in [0,4): each unit = one side of the rect
    // 0..1 = bottom, 1..2 = right, 2..3 = top, 3..4 = left
    const int SAMPLES = 80;
    for (int i = 0; i < SAMPLES; i++) {
        float t = float(i) / float(SAMPLES) * 4.0;
        float side = floor(t);
        float u    = fract(t);
        // Offset from the edge perpendicular to the side
        // and a wave-amp sine along the perimeter parameter
        float wave = sin(t * frequency + phase) * amplitude;
        vec2 p;
        if      (side < 0.5) p = vec2(u,        offset + wave);             // bottom
        else if (side < 1.5) p = vec2(1.0 - offset - wave, u);             // right
        else if (side < 2.5) p = vec2(1.0 - u,  1.0 - offset - wave);      // top
        else                  p = vec2(offset + wave, 1.0 - u);             // left
        float d = length(uv - p);
        if (d < minD) minD = d;
    }
    return minD;
}

// Stylized lily SDF — symmetric three-petal lily.
float sdLily(vec2 p, float r) {
    p.x = abs(p.x);
    // Central petal
    float c = length(p - vec2(0.0, 0.6 * r)) - r * 0.6;
    // Side petals — rotated -45°
    vec2 q = mat2(0.7071, -0.7071, 0.7071, 0.7071) * (p - vec2(0.4 * r, 0.2 * r));
    float s = length(q) - r * 0.55;
    // Stem
    float st = max(abs(p.x) - r * 0.05, abs(p.y + r * 0.5) - r * 0.5);
    return min(min(c, s), st);
}

// Filigree sweep — long sinuous quadratic curves that loop within the
// frame band. Returns distance to the curve, sampled along bezier.
float distToFiligree(vec2 uv, vec2 a, vec2 b, vec2 c) {
    float minD = 1e9;
    for (int i = 0; i <= 8; i++) {
        float t = float(i) / 8.0;
        vec2 q = mix(mix(a, b, t), mix(b, c, t), t);
        float d = length(uv - q);
        if (d < minD) minD = d;
    }
    return minD;
}

void main() {
    vec2 uv = gl_FragCoord.xy / RENDERSIZE.xy;
    float aspect = RENDERSIZE.x / max(RENDERSIZE.y, 1.0);
    float t = TIME * rotateSpeed;

    vec3 col = PALE_CREAM;
    col *= 0.94 + 0.06 * vnoise(uv * 220.0);

    // Distance from the canvas edge in normalized space
    vec2 edgeD = min(uv, 1.0 - uv);
    float edgeMin = min(edgeD.x, edgeD.y);

    // ── Outer frame band (mauve fill with gold edge bevels) ───────────
    float fW = frameWidth * (1.0 + audioBass * audioReact * 0.04);
    float frameMask = step(edgeMin, fW);
    if (frameMask > 0.5) {
        // Mauve base
        vec3 frame = frameTint.rgb;
        // Gold inner-edge highlight — HDR specular hot-spot at bevel peak
        float innerDist = fW - edgeMin;
        float innerEdge = smoothstep(0.0, 0.012, innerDist) * smoothstep(0.025, 0.005, innerDist);
        vec3 GOLD_HDR = GOLD * (1.0 + innerEdge * 1.3); // peaks ~2.2 linear at bevel center
        frame = mix(frame, GOLD_HDR, innerEdge * goldStrength);
        // Gold outer-edge highlight
        float outerEdge = smoothstep(0.0, 0.008, edgeMin);
        frame = mix(frame, BRONZE * 1.2, (1.0 - outerEdge) * goldStrength * 0.4);
        col = frame;
    }

    // ── Sinuous floral swirl strands tracking the perimeter ──────────
    int N = int(clamp(swirlCount, 1.0, 12.0));
    for (int i = 0; i < 12; i++) {
        if (i >= N) break;
        float fi = float(i);
        // Each strand sits at a different offset within the frame band
        float baseOffset = mix(0.025, fW * 0.75, hash11(fi * 7.13));
        // Slow phase walk so swirls sweep around the frame
        float phase = fi * 1.7 + t * 1.3;
        float amp = swirlAmplitude * (0.7 + hash11(fi * 11.7) * 0.6);
        float freq = swirlFreq * (0.6 + hash11(fi * 13.3) * 0.8);
        float d = distToFrameSwirl(uv, baseOffset, amp, freq, phase);
        // Stroke width with per-strand variance
        float w = swirlWidth * (0.6 + hash11(fi * 17.9) * 0.9);
        float maskCore  = smoothstep(w, 0.0, d);
        float maskGlow  = smoothstep(w * 2.5, w * 0.8, d);
        // Color: ink for the curve, HDR gold halo
        col = mix(col, INK, maskCore * 0.9);
        float haloGold = (maskGlow - maskCore) * goldStrength * 0.65;
        col = mix(col, GOLD * 1.9, haloGold);
    }

    // ── Filigree spirals inside the frame band — extra ornamentation ─
    {
        // Six anchor points along the perimeter, each spawning a curl
        for (int s = 0; s < 6; s++) {
            float fs = float(s);
            float pT = mod(fs * 0.667 + t * 0.05, 4.0);
            float side = floor(pT);
            float pu   = fract(pT);
            vec2 anchor;
            if      (side < 0.5) anchor = vec2(pu, fW * 0.4);
            else if (side < 1.5) anchor = vec2(1.0 - fW * 0.4, pu);
            else if (side < 2.5) anchor = vec2(1.0 - pu, 1.0 - fW * 0.4);
            else                  anchor = vec2(fW * 0.4, 1.0 - pu);

            // Curl direction — tangent to perimeter then arc inward
            vec2 inward = vec2(0.5) - anchor;
            inward = normalize(inward + vec2(1e-4));
            vec2 a = anchor;
            vec2 b = anchor + inward * fW * 0.7 + vec2(-inward.y, inward.x) * fW * 0.5;
            vec2 c = anchor + inward * fW * 0.3 - vec2(-inward.y, inward.x) * fW * 0.4;
            float d = distToFiligree(uv, a, b, c);
            float maskF = smoothstep(swirlWidth * 1.4, 0.0, d);
            col = mix(col, GOLD * 2.0, maskF * goldStrength * 0.85);
            // Tiny dot at the curl tip
            float dotMask = smoothstep(swirlWidth * 2.5, 0.0, length(uv - c));
            col = mix(col, INK, dotMask * 0.6);
        }
    }

    // ── Corner lilies ────────────────────────────────────────────────
    if (lilyShow > 0.001) {
        for (int c = 0; c < 4; c++) {
            float fc = float(c);
            vec2 corner = vec2(mod(fc, 2.0), floor(fc / 2.0));
            // Inset slightly
            vec2 cPos = mix(corner, vec2(0.5), 0.05);
            // Rotate based on which corner
            float ang = fc * 1.5708 + sin(t * 0.4 + fc) * 0.10;
            float ca = cos(ang), sa = sin(ang);
            vec2 lp = (uv - cPos) / lilySize;
            lp = mat2(ca, -sa, sa, ca) * lp;
            // Direction toward the canvas
            float lily = sdLily(lp, 1.0);
            float fillMask = smoothstep(0.04, -0.04, lily);
            col = mix(col, GOLD, fillMask * lilyShow * goldStrength);
            float lineMask = smoothstep(0.06, 0.02, abs(lily));
            col = mix(col, INK, lineMask * lilyShow * 0.7);
        }
    }

    // ── Mid-span cartouches (decorative ovals on each side midpoint) ─
    if (midCartouche > 0.001) {
        for (int s = 0; s < 4; s++) {
            float fs = float(s);
            vec2 midPos;
            if      (fs < 0.5) midPos = vec2(0.5, fW * 0.5);
            else if (fs < 1.5) midPos = vec2(1.0 - fW * 0.5, 0.5);
            else if (fs < 2.5) midPos = vec2(0.5, 1.0 - fW * 0.5);
            else               midPos = vec2(fW * 0.5, 0.5);
            vec2 d2 = uv - midPos;
            // Oval oriented along the side
            if (fs < 0.5 || fs > 1.5 && fs < 2.5) d2.x *= 0.5; else d2.y *= 0.5;
            float r = length(d2);
            float oval = smoothstep(fW * 0.30, fW * 0.25, r);
            col = mix(col, GOLD * 1.8, oval * midCartouche * goldStrength * 0.85);
            // Inner ink dot
            float dot_ = smoothstep(fW * 0.10, fW * 0.07, r);
            col = mix(col, INK, dot_ * midCartouche * 0.55);
        }
    }

    // ── Inner frame line ─────────────────────────────────────────────
    if (innerLine > 0.001) {
        float lineDist = abs(edgeMin - fW);
        float inner = smoothstep(innerLine, 0.0, lineDist);
        col = mix(col, INK, inner * 0.7);
    }

    // ── Optional center flower (off by default; user wanted frames) ──
    if (centerFlower > 0.001) {
        vec2 cuv = (uv - 0.5) * vec2(aspect, 1.0);
        float r = length(cuv);
        float th = atan(cuv.y, cuv.x);
        float petalR = 0.10 * (1.0 + 0.35 * cos(8.0 * (th + t * 0.5)));
        float ring = smoothstep(0.005, 0.0, abs(r - petalR));
        float fillM = smoothstep(petalR + 0.003, petalR - 0.005, r);
        col = mix(col, frameTint.rgb, fillM * centerFlower);
        col = mix(col, INK, ring * centerFlower);
    }

    // ── Interior wash from input texture ─────────────────────────────
    if (IMG_SIZE_inputTex.x > 0.0 && interiorWash > 0.0) {
        vec3 src = texture(inputTex, uv).rgb;
        // Only apply inside the frame
        float interior = smoothstep(0.0, 0.03, edgeMin - fW);
        col = mix(col, src, interior * interiorWash);
    }

    // Audio breath
    col *= 0.94 + audioLevel * audioReact * 0.10;

    gl_FragColor = vec4(col, 1.0);
}
