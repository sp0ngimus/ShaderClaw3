/*{
  "CATEGORIES": ["Generator", "Audio Reactive"],
  "DESCRIPTION": "Neon Cascade — ASCII density columns rewritten as a fully saturated neon installation. Each column runs a unique hue from the palette; leading-edge cells push to HDR 2.5+ so bloom catches every drip-tip. Four palette moods: Void (cyan/magenta on black), Acid (yellow/green), Blood (crimson/orange), Ghost (white/lavender). Audio-reactive: bass bursts column speed (K≤1.5), treble brightens drip tips. Calm defaults — soothing waterfall at rest, energetic under music.",
  "INPUTS": [
    { "NAME": "charSize", "LABEL": "Char Size", "TYPE": "float", "MIN": 4.0, "MAX": 32.0, "DEFAULT": 7.0 },
    { "NAME": "scrollSpeed", "LABEL": "Scroll Speed", "TYPE": "float", "MIN": 0.05, "MAX": 1.5, "DEFAULT": 0.15 },
    { "NAME": "palette", "LABEL": "Palette", "TYPE": "long", "DEFAULT": 0, "VALUES": [0,1,2,3], "LABELS": ["Void","Acid","Blood","Ghost"] },
    { "NAME": "hdrPeak", "LABEL": "HDR Peak", "TYPE": "float", "MIN": 1.0, "MAX": 3.0, "DEFAULT": 2.40 },
    { "NAME": "density", "LABEL": "Density", "TYPE": "float", "MIN": 0.1, "MAX": 1.0, "DEFAULT": 0.30 },
    { "NAME": "trailLength", "LABEL": "Trail Length", "TYPE": "float", "MIN": 0.5, "MAX": 4.0, "DEFAULT": 2.0 },
    { "NAME": "hueSpread", "LABEL": "Hue Spread", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.45 },
    { "NAME": "audioReact", "LABEL": "Audio React", "TYPE": "float", "MIN": 0.0, "MAX": 2.0, "DEFAULT": 1.0 },
    { "NAME": "glowWidth", "LABEL": "Column Glow", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.40 },
    { "NAME": "transparentBg", "LABEL": "Transparent", "TYPE": "bool", "DEFAULT": false }
  ]
}*/

// =======================================================================
// Neon Cascade — saturated HDR rewrite of the ASCII matrix.
// Same 5×7 bitmap font, but every column glows with a unique neon hue.
// Leading drip-edge pixels: HDR 2.5+ so bloom captures them hard.
// Trailing ghost cells: sub-luminance fade to deep colour (not grey).
// =======================================================================

float hash(float n)  { return fract(sin(n * 127.1) * 43758.5453); }
float hash2(vec2 p)  { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }

// 5×7 ASCII density glyph bitmaps (space . : - = + * # % @)
vec2 asciiChar(int idx) {
    if (idx == 0) return vec2(0.0, 0.0);
    if (idx == 1) return vec2(0.0, 4096.0);
    if (idx == 2) return vec2(4096.0, 4096.0);
    if (idx == 3) return vec2(0.0, 14336.0);
    if (idx == 4) return vec2(14336.0, 14336.0);
    if (idx == 5) return vec2(4100.0, 14724.0);
    if (idx == 6) return vec2(141873.0, 4564.0);
    if (idx == 7) return vec2(718609.0, 23213.0);
    if (idx == 8) return vec2(575022.0, 14897.0);
    return vec2(1033777.0, 14897.0);
}

float asciiPixel(int idx, float col, float row) {
    vec2 data = asciiChar(idx);
    float ri = floor(row);
    float rv = (ri < 4.0)
        ? mod(floor(data.x / pow(32.0, ri)), 32.0)
        : mod(floor(data.y / pow(32.0, ri - 4.0)), 32.0);
    return mod(floor(rv / pow(2.0, 4.0 - floor(col))), 2.0);
}

// ── Palette — four fully saturated HDR colour worlds ──────────────────
// colHue is per-column (0..1). Returns base colour at full brightness.
vec3 paletteColor(float colHue, int pal) {
    if (pal == 0) {
        // Void: rotates between electric cyan (0) and hot magenta (0.5)
        float t = abs(sin(colHue * 3.14159));
        vec3 cyan    = vec3(0.0,  1.0, 1.0);
        vec3 magenta = vec3(1.0,  0.0, 1.0);
        return mix(cyan, magenta, t);
    } else if (pal == 1) {
        // Acid: acid yellow to neon green
        float t = colHue;
        vec3 yellow = vec3(1.0, 1.0, 0.0);
        vec3 green  = vec3(0.1, 1.0, 0.1);
        return mix(yellow, green, t);
    } else if (pal == 2) {
        // Blood: crimson to deep amber
        float t = colHue;
        vec3 crimson = vec3(1.0, 0.04, 0.04);
        vec3 amber   = vec3(1.0, 0.55, 0.0);
        return mix(crimson, amber, t * t);
    } else {
        // Ghost: pure white to cool lavender
        float t = colHue;
        vec3 white   = vec3(1.0, 1.0, 1.0);
        vec3 lavender = vec3(0.60, 0.45, 1.0);
        return mix(white, lavender, t);
    }
}

void main() {
    vec2 px = gl_FragCoord.xy;
    vec2 uv = px / RENDERSIZE.xy;

    float aR  = clamp(audioReact, 0.0, 2.0);
    float bass = clamp(audioBass, 0.0, 1.0) * aR;
    float high = clamp(audioHigh, 0.0, 1.0) * aR;

    // ── Grid ──────────────────────────────────────────────────────────
    float cellW = charSize;
    float cellH = charSize * 1.4;
    vec2 cell   = floor(px / vec2(cellW, cellH));
    vec2 cellUV = mod(px, vec2(cellW, cellH)) / vec2(cellW, cellH);

    // ── Column properties ─────────────────────────────────────────────
    float colSeed   = hash(cell.x * 73.1);
    float colSpeed  = 0.5 + colSeed * 1.5;
    float colOffset = colSeed * 100.0;
    float colActive = step(1.0 - density, hash(cell.x * 31.7 + 0.5));

    // Per-column hue for neon variety
    float colHue = fract(cell.x / max(RENDERSIZE.x / cellW, 1.0)
                       + hash(cell.x * 0.137) * hueSpread);

    // Scroll speed: base * (1 + bass * K), K = 1.5 ≤ 1.5 ✓
    float effectiveSpeed = scrollSpeed * colSpeed * (1.0 + bass * 1.5);
    float scroll    = TIME * effectiveSpeed + colOffset;
    float scrolledY = cell.y + floor(scroll);

    // ── Character selection ───────────────────────────────────────────
    float charSeed = hash2(vec2(cell.x, scrolledY));

    // Trail fade: head is full brightness, tail dims over trailLength cells.
    float headPos     = fract(scroll);
    float distFromHead = mod(cell.y / (RENDERSIZE.y / cellH) + headPos, 1.0);
    float trail = pow(max(1.0 - distFromHead * trailLength, 0.0), 2.0 + 1.5);
    float brightness = trail * colActive;

    // Leading edge bright spike (top ~5% of head)
    float headGlow = smoothstep(0.95, 1.0, 1.0 - distFromHead) * colActive;

    int charIdx = int(clamp(brightness * 9.99, 0.0, 9.0));

    // Occasional character flicker at the drip head
    float changeRate = hash2(vec2(cell.x, floor(TIME * 3.0 + cell.y * 0.1)));
    if (changeRate > 0.85) {
        charIdx = int(clamp(hash2(vec2(cell.x * 7.0, floor(TIME * 8.0))) * 9.99, 0.0, 9.0));
    }

    // ── Glyph sampling ───────────────────────────────────────────────
    float cropFactor = 0.75;
    vec2 glyphUV = 0.5 + (cellUV - 0.5) * cropFactor;
    float col5   = glyphUV.x * 5.0;
    float row7   = (1.0 - glyphUV.y) * 7.0;
    float pixel  = 0.0;
    if (glyphUV.x >= 0.0 && glyphUV.x <= 1.0 && glyphUV.y >= 0.0 && glyphUV.y <= 1.0) {
        pixel = asciiPixel(charIdx, col5, row7);
    }

    // ── Colour ───────────────────────────────────────────────────────
    vec3 baseCol = paletteColor(colHue, int(palette));   // unit-range base

    // Head spark: treble boosts tip brightness (K ≤ 1.5 via clamp).
    float tipBoost = headGlow * (1.0 + clamp(high, 0.0, 1.5));

    // Character luminance = trail intensity scaled to HDR at tip.
    // Non-tip trail: colour * brightness (SDR scale → bloom won't pick up)
    // Tip: colour pushed to hdrPeak (2.0-2.5)
    float trailScale = brightness * (1.0 - headGlow);
    float tipScale   = headGlow * (hdrPeak + tipBoost * 0.5);

    vec3 charCol = baseCol * (trailScale + tipScale);

    // ── Column ambient glow (neon tube effect) ────────────────────────
    // A wide, very dim halo behind the active column so it reads as a
    // neon tube even in the faded tail region.
    float glowMask = exp(-pow((cellUV.x - 0.5) * 4.0, 2.0)) * brightness * glowWidth;
    vec3 ambientGlow = baseCol * glowMask * 0.35;

    // ── Final composite ───────────────────────────────────────────────
    vec3 finalCol = vec3(0.0);
    float alpha = transparentBg ? 0.0 : 1.0;

    float mask = pixel * brightness;

    // Glyph pixels: full neon colour
    finalCol += charCol * pixel;
    // Background glow for neon tube halo
    finalCol += ambientGlow * (1.0 - pixel);

    if (transparentBg) {
        alpha = clamp(mask + glowMask * glowWidth, 0.0, 1.0);
    }

    // LINEAR HDR out — no tonemap.
    gl_FragColor = vec4(finalCol, alpha);
}
