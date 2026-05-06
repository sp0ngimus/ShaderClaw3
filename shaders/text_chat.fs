/*{
  "DESCRIPTION": "Chat — speech-bubble note taker. Splits the message into chunks and emits each chunk as a small chat bubble that floats up the screen. Each bubble cycles through a 6-slot palette so consecutive messages get wildly different colors. Bubbles alternate left/right sides like a chaotic group chat. Drop a long sentence in MSG and watch it self-organize. Tighter kerning so chunks read like dense one-liners.",
  "CREDIT": "ShaderClaw",
  "CATEGORIES": ["Generator", "Text"],
  "INPUTS": [
    { "NAME": "msg", "TYPE": "text", "DEFAULT": "HEY DID YOU SEE THIS LOOKS CRAZY RIGHT YEAH ITS WILD", "MAX_LENGTH": 48 },
    { "NAME": "fontFamily", "LABEL": "Font", "TYPE": "long", "DEFAULT": 0, "VALUES": [0,1,2,3], "LABELS": ["Inter","Times","Caslon","Outfit"] },
    { "NAME": "bubbleCount", "LABEL": "Bubbles On Screen", "TYPE": "long", "DEFAULT": 3, "VALUES": [1,2,3,4,5,6,7], "LABELS": ["1","2","3","4","5","6","7"] },
    { "NAME": "wrapAt", "LABEL": "Wrap At (chars)", "TYPE": "long", "DEFAULT": 14, "VALUES": [8,10,12,14,16,18,22,28], "LABELS": ["8","10","12","14","16","18","22","28"] },
    { "NAME": "spawnRate", "LABEL": "Spawn Rate", "TYPE": "float", "DEFAULT": 0.25, "MIN": 0.1, "MAX": 1.5 },
    { "NAME": "floatSpeed", "LABEL": "Float Speed", "TYPE": "float", "DEFAULT": 0.18, "MIN": 0.05, "MAX": 0.6 },
    { "NAME": "wobble", "LABEL": "Wobble", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.5 },
    { "NAME": "textScale", "LABEL": "Text Size", "TYPE": "float", "DEFAULT": 0.044, "MIN": 0.018, "MAX": 0.07 },
    { "NAME": "kerning", "LABEL": "Kerning", "TYPE": "float", "DEFAULT": 0.92, "MIN": 0.7, "MAX": 1.4 },
    { "NAME": "bubblePadding", "LABEL": "Bubble Padding", "TYPE": "float", "DEFAULT": 0.030, "MIN": 0.005, "MAX": 0.06 },
    { "NAME": "cornerRadius", "LABEL": "Corner Radius", "TYPE": "float", "DEFAULT": 0.030, "MIN": 0.0, "MAX": 0.06 },
    { "NAME": "audioReact", "LABEL": "Audio React", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 2.0 },
    { "NAME": "autoTextColor", "LABEL": "Auto Text Color", "TYPE": "bool", "DEFAULT": 1.0 },
    { "NAME": "inputTex", "LABEL": "Layer Source", "TYPE": "image" },
    { "NAME": "pageBgUsesLayer", "LABEL": "Page BG = Layer", "TYPE": "bool", "DEFAULT": 1.0 },
    { "NAME": "bubbleFillMode", "LABEL": "Bubble Fill", "TYPE": "long", "DEFAULT": 0, "VALUES": [0,1,2], "LABELS": ["Solid Color","Layer Texture","Layer Tinted"] },
    { "NAME": "bubbleTexTint", "LABEL": "Bubble Tint Mix", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "bgOpacity", "LABEL": "Page BG Opacity", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "bgColor", "LABEL": "Background", "TYPE": "color", "DEFAULT": [0.06, 0.07, 0.10, 1.0] },
    { "NAME": "color1", "LABEL": "Bubble 1", "TYPE": "color", "DEFAULT": [0.10, 0.55, 1.00, 1.0] },
    { "NAME": "color2", "LABEL": "Bubble 2", "TYPE": "color", "DEFAULT": [1.00, 0.20, 0.55, 1.0] },
    { "NAME": "color3", "LABEL": "Bubble 3", "TYPE": "color", "DEFAULT": [0.30, 1.00, 0.45, 1.0] },
    { "NAME": "color4", "LABEL": "Bubble 4", "TYPE": "color", "DEFAULT": [1.00, 0.75, 0.10, 1.0] },
    { "NAME": "color5", "LABEL": "Bubble 5", "TYPE": "color", "DEFAULT": [0.65, 0.20, 1.00, 1.0] },
    { "NAME": "color6", "LABEL": "Bubble 6", "TYPE": "color", "DEFAULT": [0.10, 0.95, 0.95, 1.0] },
    { "NAME": "manualTextColor", "LABEL": "Manual Text", "TYPE": "color", "DEFAULT": [1.0, 1.0, 1.0, 1.0] },
    { "NAME": "transparentBg", "LABEL": "Transparent BG", "TYPE": "bool", "DEFAULT": 0.0 }
  ]
}*/

// ===========================================================
// Chat — chunks the message into bubbles that float upward
// alternating sides. Each bubble has a lifetime; new ones
// spawn at the bottom while old ones drift off the top.
// LINEAR HDR.
// ===========================================================

#define MAX_BUBBLES 7

// ─── Font atlas ────────────────────────────────────────────
float sampleChar(int ch, vec2 uv) {
    if (ch < 0 || ch > 36) return 0.0;
    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) return 0.0;
    return texture2D(fontAtlasTex, vec2((float(ch) + uv.x) / 37.0, uv.y)).r;
}

int getChar(int slot) {
    if (slot ==  0) return int(msg_0);
    if (slot ==  1) return int(msg_1);
    if (slot ==  2) return int(msg_2);
    if (slot ==  3) return int(msg_3);
    if (slot ==  4) return int(msg_4);
    if (slot ==  5) return int(msg_5);
    if (slot ==  6) return int(msg_6);
    if (slot ==  7) return int(msg_7);
    if (slot ==  8) return int(msg_8);
    if (slot ==  9) return int(msg_9);
    if (slot == 10) return int(msg_10);
    if (slot == 11) return int(msg_11);
    if (slot == 12) return int(msg_12);
    if (slot == 13) return int(msg_13);
    if (slot == 14) return int(msg_14);
    if (slot == 15) return int(msg_15);
    if (slot == 16) return int(msg_16);
    if (slot == 17) return int(msg_17);
    if (slot == 18) return int(msg_18);
    if (slot == 19) return int(msg_19);
    if (slot == 20) return int(msg_20);
    if (slot == 21) return int(msg_21);
    if (slot == 22) return int(msg_22);
    if (slot == 23) return int(msg_23);
    if (slot == 24) return int(msg_24);
    if (slot == 25) return int(msg_25);
    if (slot == 26) return int(msg_26);
    if (slot == 27) return int(msg_27);
    if (slot == 28) return int(msg_28);
    if (slot == 29) return int(msg_29);
    if (slot == 30) return int(msg_30);
    if (slot == 31) return int(msg_31);
    if (slot == 32) return int(msg_32);
    if (slot == 33) return int(msg_33);
    if (slot == 34) return int(msg_34);
    if (slot == 35) return int(msg_35);
    if (slot == 36) return int(msg_36);
    if (slot == 37) return int(msg_37);
    if (slot == 38) return int(msg_38);
    if (slot == 39) return int(msg_39);
    if (slot == 40) return int(msg_40);
    if (slot == 41) return int(msg_41);
    if (slot == 42) return int(msg_42);
    if (slot == 43) return int(msg_43);
    if (slot == 44) return int(msg_44);
    if (slot == 45) return int(msg_45);
    if (slot == 46) return int(msg_46);
    if (slot == 47) return int(msg_47);
    return -1;
}

int charCount() {
    int n = int(msg_len);
    if (n <= 0) return 12;
    if (n > 48) return 48;
    return n;
}

// ─── SDF rounded rectangle with tail ────────────────────────
// p in bubble-local coords (center at 0). hb = box half-extent.
// r = corner radius. (`half` is a GLSL reserved word, hence `hb`.)
float sdRoundedRectTail(vec2 p, vec2 hb, float r, float tailX, float tailY) {
    vec2 q = abs(p) - (hb - vec2(r));
    float box = min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r;

    // Tail: small triangle pointing away from box bottom corner.
    if (tailX != 0.0) {
        // Tail tip at (tailX, tailY), base at the bubble's bottom edge.
        vec2 tipP   = vec2(tailX, tailY);
        // Distance to the tail-shape — approx a circular notch.
        float td = length(p - tipP * 0.55) - 0.012;
        return min(box, td);
    }
    return box;
}

// ─── hash for per-bubble jitter ─────────────────────────────
float hash11(float n) { return fract(sin(n * 127.1) * 43758.5453); }

void main() {
    vec2 res = RENDERSIZE;
    vec2 uv  = gl_FragCoord.xy / res;
    float aspect = res.x / res.y;

    // Centered, aspect-corrected. Y goes 0(bottom) → 1(top).
    vec2 p;
    p.x = (uv.x - 0.5) * aspect;
    p.y = uv.y;

    float audio = clamp(audioReact, 0.0, 2.0);
    float bass  = audioBass;

    int total       = charCount();
    int bubbles     = int(bubbleCount);
    if (bubbles > MAX_BUBBLES) bubbles = MAX_BUBBLES;
    float charH     = textScale;
    float charW     = charH * (5.0 / 7.0);
    // Audio bass micro-boost the spawn rate.
    float effSpawn  = spawnRate * (1.0 + 0.3 * bass * audio);
    float lifetime  = float(bubbles) / max(effSpawn, 0.05);
    // Each bubble's "global age" in seconds.
    // Bubble k spawned at time T - (k / effSpawn) (approx).

    // Chunk the message: each bubble holds chars [k*chunk, (k+1)*chunk).
    int chunkLen = (total + bubbles - 1) / bubbles;
    if (chunkLen < 1) chunkLen = 1;

    // ─── Background ────────────────────────────────────────
    // Sample the bound layer once for both page-bg and bubble-fill
    // modes. The page-bg toggle decides whether the chat lives on
    // the layer (Page BG = Layer ON) or on a flat bg color. The
    // bubble-fill enum independently lets each bubble be filled
    // with the same layer (great for "speech bubbles cut into a
    // shader").
    vec3 layerSample = IMG_NORM_PIXEL(inputTex, uv).rgb;
    vec3 col = pageBgUsesLayer
        ? mix(bgColor.rgb, layerSample, bgOpacity)
        : bgColor.rgb;
    // Mild radial vignette so bubbles read well.
    vec2 vignP = vec2((uv.x - 0.5) * aspect, uv.y - 0.5);
    float vd = length(vignP);
    col *= mix(1.0, 0.7, smoothstep(0.45, 0.95, vd));

    int fillMode = int(bubbleFillMode);

    float bubbleAlpha = 0.0;          // accumulated bubble fill mask
    vec3  bubbleCol   = vec3(0.0);    // accumulated bubble color
    float charMask    = 0.0;          // accumulated text mask
    vec3  charCol     = vec3(0.0);    // accumulated text color

    // Pre-compute per-side lane counts so same-side bubbles are spaced
    // by enough vertical phase to never overlap.
    int rightLanes = (bubbles + 1) / 2;          // bubbles 0,2,4,...
    int leftLanes  = bubbles / 2;                // bubbles 1,3,5,...

    for (int k = 0; k < MAX_BUBBLES; k++) {
        if (k >= bubbles) break;
        float fk = float(k);
        bool senderSide = (mod(fk, 2.0) < 0.5);
        // Per-side stagger index — 0,1,2,... within each side. Two
        // bubbles on the same side are now spaced by exactly
        // 1/<sameSideCount> in phase, which guarantees the box-size
        // gap is the same regardless of total bubble count.
        int sideIdx = k / 2;
        int sideN   = senderSide ? rightLanes : leftLanes;
        if (sideN < 1) sideN = 1;

        // Phase in [0,1): 0 = just spawned at bottom, 1 = expired at top.
        // Each bubble cycles every `lifetime` seconds; offset across the
        // two sides by 0.5/sideN so left/right interleave neatly.
        float sideOffset = senderSide ? 0.0 : 0.5 / float(sideN);
        float phase = mod(TIME / lifetime
                        + float(sideIdx) / float(sideN)
                        + sideOffset, 1.0);

        // Pop-in: short scale-up at the start.
        float popIn  = smoothstep(0.0, 0.06, phase);
        // Fade-out near end.
        float fadeOut = 1.0 - smoothstep(0.85, 1.0, phase);
        float env = popIn * fadeOut;
        if (env < 0.01) continue;

        // Vertical position: rises from bottom (y=0.18) to top (y=0.92).
        float by = mix(0.18, 0.92, phase);
        // Horizontal: locked sender/receiver columns. No x-wobble — that
        // was the source of side-by-side overlap. Vertical micro-bob
        // gives some life without colliding neighbours.
        by += wobble * 0.012 * sin(TIME * 0.9 + fk * 1.7);
        float bx = senderSide ? aspect * 0.25 : -aspect * 0.25;

        vec2 bubbleC = vec2(bx, by);
        vec2 d = p - bubbleC;

        // Multi-line wrap layout — bubble sized for `wrapAt` chars per row.
        // Long chunks get taller bubbles (more rows), so a full sentence
        // fits inside one bubble instead of being split across many.
        float kernX        = charW * kerning;
        int   charsPerRow  = int(wrapAt);
        if (charsPerRow > chunkLen) charsPerRow = chunkLen;
        if (charsPerRow < 1)        charsPerRow = 1;
        int   numRows      = (chunkLen + charsPerRow - 1) / charsPerRow;
        float boxW         = float(charsPerRow) * kernX + bubblePadding * 2.0;
        float lineH        = charH * 1.35;            // breathing room between rows
        float boxH         = float(numRows) * lineH + bubblePadding * 2.0
                           - (numRows > 0 ? (lineH - charH) : 0.0);
        vec2  halfBox      = vec2(boxW, boxH) * 0.5;

        // Pop-in scales the bubble.
        float scale = mix(0.6, 1.0, popIn);
        d /= scale;

        // Tail: sender → tail bottom-right; receiver → tail bottom-left.
        float tailX = senderSide ?  halfBox.x * 0.72 : -halfBox.x * 0.72;
        float tailY = -halfBox.y * 1.05;

        float sdf = sdRoundedRectTail(d, halfBox, cornerRadius, tailX, tailY);

        // Anti-aliased bubble fill.
        float fw   = fwidth(sdf);
        float fill = 1.0 - smoothstep(-fw, fw, sdf);
        if (fill < 0.001) continue;

        // Per-bubble palette color (always computed — used directly in
        // mode 0, used as tint in mode 2, used as text-contrast hint
        // for the auto-text branch even in mode 1).
        int paletteIdx = int(mod(fk, 6.0));
        vec3 paletteColor = color1.rgb;
        if (paletteIdx == 1) paletteColor = color2.rgb;
        else if (paletteIdx == 2) paletteColor = color3.rgb;
        else if (paletteIdx == 3) paletteColor = color4.rgb;
        else if (paletteIdx == 4) paletteColor = color5.rgb;
        else if (paletteIdx == 5) paletteColor = color6.rgb;

        // Bubble fill — three modes:
        //   0 Solid Color: pure palette
        //   1 Layer Texture: sample bound layer at this pixel — bubbles
        //     reveal the layer through their silhouette
        //   2 Layer Tinted: layer × palette (mix controlled by tint slider)
        vec3 bubColor;
        if (fillMode == 1) {
            bubColor = layerSample;
        } else if (fillMode == 2) {
            bubColor = mix(layerSample, layerSample * paletteColor * 1.5,
                           bubbleTexTint);
        } else {
            bubColor = paletteColor;
        }

        // Vertical sheen for that glossy iMessage feel — applied to
        // every fill mode so textured bubbles still have shape.
        float sheen = smoothstep(-halfBox.y, halfBox.y * 0.7, d.y);
        bubColor = mix(bubColor * 0.92, bubColor * 1.12, sheen);
        // Bass pulse on the freshest bubble (just spawned).
        if (phase < 0.2) bubColor *= 1.0 + 0.15 * bass * audio * (1.0 - phase / 0.2);

        // Composite — newest bubble wins overlap.
        bubbleAlpha = max(bubbleAlpha, fill * env);
        bubbleCol   = mix(bubbleCol, bubColor, fill * env);

        // ─── Text inside bubble (multi-row wrap) ───
        // Coords: x=0 at inner-left, y=0 at inner-TOP (rows fall downward
        // toward bottom of bubble). Each row is lineH tall; chars within
        // a row sit on a charH-tall band.
        float innerL = -halfBox.x + bubblePadding;
        float innerT =  halfBox.y - bubblePadding;
        float lx     = (d.x - innerL);
        float ly     = (innerT - d.y);          // y down
        if (lx < 0.0 || ly < 0.0) continue;

        int rowIdx = int(floor(ly / lineH));
        if (rowIdx < 0 || rowIdx >= numRows) continue;
        float yInRow = ly - float(rowIdx) * lineH;
        if (yInRow > charH) continue;            // gap between rows

        int colIdx = int(floor(lx / kernX));
        if (colIdx < 0 || colIdx >= charsPerRow) continue;

        int charIdxInBubble = rowIdx * charsPerRow + colIdx;
        if (charIdxInBubble >= chunkLen) continue;
        int globalIdx = k * chunkLen + charIdxInBubble;
        if (globalIdx >= total) continue;

        int ch = getChar(globalIdx);
        vec2 cellLocal = vec2((lx - float(colIdx) * kernX) / charW,
                              yInRow / charH);
        float s = sampleChar(ch, cellLocal);
        s = smoothstep(0.18, 0.55, s);
        if (s > 0.001) {
            // Auto contrast: pick black/white based on bubble luminance,
            // so any "crazy" palette entry stays readable. Manual override
            // with autoTextColor=false uses manualTextColor instead.
            vec3 txtColor;
            if (autoTextColor) {
                float lum = dot(bubColor, vec3(0.299, 0.587, 0.114));
                txtColor = (lum > 0.55) ? vec3(0.04) : vec3(1.0);
            } else {
                txtColor = manualTextColor.rgb;
            }
            charMask = max(charMask, s * env * fill);
            charCol  = mix(charCol, txtColor, s * env * fill);
        }
    }

    // Compose background ← bubble ← text.
    col = mix(col, bubbleCol, bubbleAlpha);
    col = mix(col, charCol,   charMask);

    // Subtle drop shadow for newest bubble approximation: faint dark
    // ring just below where any bubble edge sits. Cheap & cheerful.

    float alpha = 1.0;
    if (transparentBg) {
        alpha = max(bubbleAlpha, charMask);
        col   = mix(bubbleCol, charCol, charMask);
    }

    gl_FragColor = vec4(col, alpha);
}
