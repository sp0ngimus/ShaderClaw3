/*{
  "DESCRIPTION": "Clusters — text lives inside soft cell circles that fuse into metaball clusters (Flüx-style). Each chunk of the message gets its own circle; circles within the same cluster bridge with smooth-min SDFs into organic blobs. New clusters keep spawning across the canvas, popping in and fading out so the composition is always evolving. Two-color palette with text-on-color contrast.",
  "CREDIT": "ShaderClaw — inspired by Clear Supply Flüx Modular soft cell kit",
  "CATEGORIES": ["Generator", "Text"],
  "INPUTS": [
    { "NAME": "msg", "TYPE": "text", "DEFAULT": "FLUX MODULAR SOFT CELLS BLOOM ACROSS THE CANVAS", "MAX_LENGTH": 48 },
    { "NAME": "fontFamily", "LABEL": "Font", "TYPE": "long", "DEFAULT": 0, "VALUES": [0,1,2,3], "LABELS": ["Inter","Times","Caslon","Outfit"] },
    { "NAME": "clusterCount", "LABEL": "Clusters", "TYPE": "long", "DEFAULT": 9, "VALUES": [4,6,8,9,10,12,14,16], "LABELS": ["4","6","8","9","10","12","14","16"] },
    { "NAME": "nodesPerCluster", "LABEL": "Nodes / Cluster", "TYPE": "long", "DEFAULT": 3, "VALUES": [2,3,4,5], "LABELS": ["2","3","4","5"] },
    { "NAME": "spawnRate", "LABEL": "Spawn Rate", "TYPE": "float", "DEFAULT": 0.18, "MIN": 0.04, "MAX": 0.8 },
    { "NAME": "bridgeK", "LABEL": "Bridge Smoothness", "TYPE": "float", "DEFAULT": 0.045, "MIN": 0.0, "MAX": 0.12 },
    { "NAME": "interBridgeK", "LABEL": "Inter-Cluster Bridge", "TYPE": "float", "DEFAULT": 0.02, "MIN": 0.0, "MAX": 0.35 },
    { "NAME": "morphAmp", "LABEL": "Bridge Morph", "TYPE": "float", "DEFAULT": 0.45, "MIN": 0.0, "MAX": 1.5 },
    { "NAME": "nodeRadius", "LABEL": "Node Radius", "TYPE": "float", "DEFAULT": 0.095, "MIN": 0.025, "MAX": 0.18 },
    { "NAME": "radiusVariance", "LABEL": "Radius Variance", "TYPE": "float", "DEFAULT": 0.55, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "orbitSpeed", "LABEL": "Orbit Speed", "TYPE": "float", "DEFAULT": 0.15, "MIN": 0.0, "MAX": 1.5 },
    { "NAME": "textScale", "LABEL": "Text Size", "TYPE": "float", "DEFAULT": 0.020, "MIN": 0.010, "MAX": 0.06 },
    { "NAME": "kerning", "LABEL": "Kerning", "TYPE": "float", "DEFAULT": 0.85, "MIN": 0.55, "MAX": 1.4 },
    { "NAME": "audioReact", "LABEL": "Audio React", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 2.0 },
    { "NAME": "autoTextColor", "LABEL": "Auto Text Color", "TYPE": "bool", "DEFAULT": 1.0 },
    { "NAME": "bgColor", "LABEL": "Background", "TYPE": "color", "DEFAULT": [0.42, 0.42, 0.45, 1.0] },
    { "NAME": "cellA", "LABEL": "Cell Color A", "TYPE": "color", "DEFAULT": [0.84, 0.66, 0.86, 1.0] },
    { "NAME": "cellB", "LABEL": "Cell Color B", "TYPE": "color", "DEFAULT": [1.00, 0.49, 0.39, 1.0] },
    { "NAME": "manualTextColor", "LABEL": "Manual Text", "TYPE": "color", "DEFAULT": [0.05, 0.05, 0.07, 1.0] },
    { "NAME": "transparentBg", "LABEL": "Transparent BG", "TYPE": "bool", "DEFAULT": 0.0 }
  ]
}*/

// =====================================================================
// Clusters — text lives in metaball-fused circles. Each "node" is a
// circle with a chunk of the message; nodes within the same cluster
// smooth-min into one organic blob. Clusters spawn at deterministic
// positions, pop in, hold, fade out — composition evolves continuously.
// =====================================================================

#define MAX_CLUSTERS 16
#define MAX_NODES    5
#define MAX_WALK     64
#define SPACE_CH     26

// ─── Font atlas ─────────────────────────────────────────────────────
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

float hash11(float n) { return fract(sin(n * 127.1) * 43758.5453); }
vec2  hash21(float n) { return vec2(hash11(n), hash11(n + 17.31)); }

// Smooth minimum — metaball glue between two SDFs.
float smin(float a, float b, float k) {
    float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0);
    return mix(b, a, h) - k * h * (1.0 - h);
}

// Smooth-min variant that ALSO returns the blend factor `h`. h=1 means
// "a" wins, h=0 means "b" wins, anything between is the bridge zone
// — used to mix the two clusters' colors along the connecting tissue.
float smin_h(float a, float b, float k, out float h) {
    h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0);
    return mix(b, a, h) - k * h * (1.0 - h);
}

// Cheap value noise + 3-octave fbm — used as the slow morphing field
// that perturbs the bridge SDF so connections feel organic, not linear.
float vnoise(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash11(dot(i, vec2(1.0, 157.0)));
    float b = hash11(dot(i + vec2(1.0, 0.0), vec2(1.0, 157.0)));
    float c = hash11(dot(i + vec2(0.0, 1.0), vec2(1.0, 157.0)));
    float d = hash11(dot(i + vec2(1.0, 1.0), vec2(1.0, 157.0)));
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}
float fbm2(vec2 p) {
    float v = 0.0, a = 0.5;
    for (int i = 0; i < 3; i++) {
        v += a * vnoise(p);
        p = p * 2.07 + vec2(11.3, 5.7);
        a *= 0.5;
    }
    return v;
}

void main() {
    vec2 res = RENDERSIZE;
    vec2 uv  = gl_FragCoord.xy / res;
    float aspect = res.x / res.y;

    // Aspect-corrected, centered.
    vec2 p;
    p.x = (uv.x - 0.5) * aspect;
    p.y = uv.y - 0.5;

    float audio = clamp(audioReact, 0.0, 2.0);
    float bass  = audioBass;

    int   clusters = int(clusterCount);
    if (clusters > MAX_CLUSTERS) clusters = MAX_CLUSTERS;
    int   nodesEach = int(nodesPerCluster);
    if (nodesEach > MAX_NODES) nodesEach = MAX_NODES;
    int   total     = charCount();
    float charH     = textScale;
    float charW     = charH * (5.0 / 7.0);
    float kern      = charW * kerning;

    // Each node now hosts a multi-line word-wrapped paragraph chunk
    // (set in the rendering block below from the bubble's char budget).
    // chunkLen kept here only as a coarse upper bound used for chunk
    // assignment across nodes.
    int chunkLen = total;
    if (chunkLen > 48) chunkLen = 48;

    // Each cluster's life cycles every `lifetime` seconds. Phase staggered
    // by clusterIdx/clusters so spawns never bunch up.
    float lifetime = float(clusters) / max(spawnRate, 0.05);

    // ── Background ─────────────────────────────────────────────────
    vec3 col = bgColor.rgb;

    // Accumulators across all clusters: best (smallest) SDF, the chosen
    // cluster's color, the chosen node's character cell.
    float blobSdf  = 1e6;
    vec3  blobCol  = vec3(0.0);
    float charMask = 0.0;
    vec3  textCol  = vec3(0.0);

    for (int c = 0; c < MAX_CLUSTERS; c++) {
        if (c >= clusters) break;
        float fc = float(c);

        // Phase in [0,1): 0=fresh spawn, 1=expired.
        float phase = mod(TIME / lifetime + fc / float(clusters), 1.0);
        float popIn  = smoothstep(0.0, 0.10, phase);
        float fadeOut = 1.0 - smoothstep(0.85, 1.0, phase);
        float env = popIn * fadeOut;
        if (env < 0.01) continue;

        // Grid-packed deterministic anchor — each cluster gets its own
        // cell so bubbles don't pile up. Grid sized from cluster count
        // and aspect; small jitter inside the cell keeps the layout
        // breathing rather than rigid. Like discrete speech bubbles
        // across the canvas, each in its own spatial neighborhood.
        vec2 baseSeed = hash21(fc * 13.7);
        int gridX = int(ceil(sqrt(float(clusters) * max(aspect, 0.5))));
        if (gridX < 1) gridX = 1;
        int gridY = (clusters + gridX - 1) / gridX;
        int cx = c - (c / gridX) * gridX;
        int cy = c / gridX;
        float canvasW = aspect - 0.18;
        float canvasH = 0.90;
        float cellW   = canvasW / float(gridX);
        float cellH   = canvasH / float(gridY);
        vec2 anchor;
        anchor.x = -0.5 * canvasW + (float(cx) + 0.5) * cellW;
        anchor.y = -0.5 * canvasH + (float(cy) + 0.5) * cellH;
        // Per-cluster jitter inside the cell — keeps the grid from
        // looking like a literal grid. Capped so bubbles can't reach
        // the cell boundary (≤ 18% of cell extent each way).
        anchor.x += (baseSeed.x - 0.5) * cellW * 0.18;
        anchor.y += (baseSeed.y - 0.5) * cellH * 0.18;
        // Slow per-cluster drift — gentle organic motion within the cell.
        float driftSeed = fc * 7.21;
        anchor.x += cellW * 0.06 * sin(TIME * 0.18 + driftSeed);
        anchor.y += cellH * 0.06 * cos(TIME * 0.22 + driftSeed * 1.7);

        // Two-color tint: alternate by cluster index for visual rhythm.
        vec3 cTint = (mod(fc, 2.0) < 0.5) ? cellA.rgb : cellB.rgb;
        // Pop-in scale (cluster grows from a point at spawn).
        float clusterScale = mix(0.4, 1.0, popIn);

        // Auto-fit: clamp the cluster's max footprint to ~42% of the
        // smaller cell dimension so neighbours never touch. Cluster
        // extent ≈ nodeRadius * (max orbR factor 2.0 + max rad factor
        // 1.32) ≈ nodeRadius * 3.32. fitScale shrinks oversize requests.
        float fitMax  = 0.42 * min(cellW, cellH);
        float fitRad  = min(nodeRadius, fitMax / 3.32);

        // Build the cluster's metaball SDF: smooth-min of every node circle.
        // Track the closest node so we know which character cell to draw.
        float clusterSdf  = 1e6;
        int   nearestNode = 0;
        float nearestDist = 1e6;
        vec2  nearestPos  = anchor;
        float nearestRad  = fitRad;

        for (int n = 0; n < MAX_NODES; n++) {
            if (n >= nodesEach) break;
            float fn = float(n);
            // Node-specific seed.
            vec2 ns = hash21(fc * 41.3 + fn * 7.7);

            // Node sits offset from anchor on a slow orbit; offset radius
            // scales with cluster's mean node size.
            float orbR  = fitRad * (1.4 + 0.6 * ns.x);
            float orbA  = ns.y * 6.2832
                        + TIME * orbitSpeed * (1.0 + 0.4 * (ns.x - 0.5));
            // First node sits closer to anchor; later nodes spread outward.
            float spread = (n == 0) ? 0.4 : 1.0;
            vec2 nodeP = anchor + spread * orbR
                       * vec2(cos(orbA), sin(orbA)) * clusterScale;

            // Node radius variance — some nodes much bigger than others
            // (matches the "1 big + 2 small bridges" look of the reference).
            float rad = fitRad
                      * mix(1.0, 0.4 + 1.4 * hash11(fc * 31.1 + fn * 5.3),
                            radiusVariance);
            // Bass pulse on the freshest cluster.
            if (phase < 0.25) rad *= 1.0 + 0.15 * bass * audio
                                       * (1.0 - phase / 0.25);
            rad *= clusterScale;

            // Circle SDF.
            float d = length(p - nodeP) - rad;

            // Metaball-glue with running min.
            if (n == 0) clusterSdf = d;
            else        clusterSdf = smin(clusterSdf, d, bridgeK);

            // Track nearest node for character cell mapping.
            float pd = length(p - nodeP);
            if (pd < nearestDist) {
                nearestDist = pd;
                nearestNode = n;
                nearestPos  = nodeP;
                nearestRad  = rad;
            }
        }

        // Fade-aware SDF (puff out slightly as it spawns / expires for
        // softer appearance).
        clusterSdf -= (env - 1.0) * 0.005;

        // ─── Inter-cluster bridge ───────────────────────────────
        // Chain smooth-min ALL clusters into the running compositeSdf
        // with a wider k than intra-cluster. Distant clusters retain
        // their separate silhouettes; close ones grow connecting
        // ribbons. The h factor blends the two clusters' colors
        // along the bridge so the underlying tissue reads smoothly.
        float bk = max(interBridgeK, 0.001);
        if (c == 0) {
            blobSdf = clusterSdf;
            blobCol = cTint;
        } else {
            float h;
            blobSdf = smin_h(blobSdf, clusterSdf, bk, h);
            // h=1 means existing wins; h=0 means current wins.
            blobCol = mix(cTint, blobCol, h);
        }

        // Anti-aliased fill check for the pixel — uses the per-cluster
        // SDF so text only renders inside its OWN cluster (the bridges
        // are background tissue, not text-bearing nodes).
        float fw   = fwidth(clusterSdf);
        float fill = 1.0 - smoothstep(-fw, fw, clusterSdf);
        if (fill < 0.001) continue;

        // ─── Multi-line word-wrapped text inside the nearest node ───
        // Inscribed axis-aligned text box, top-left anchored. Word-wrap:
        // a word that won't fit on the current line wraps as a unit
        // (never mid-word); single words longer than charsPerRow hard-
        // wrap. Speech-bubble layout — each node holds a small paragraph.
        vec2 localP = p - nearestPos;

        // Inscribed box: 62% of node radius keeps a margin so text
        // doesn't kiss the bubble silhouette.
        float boxHalf = nearestRad * 0.62;

        float effCharH = min(charH, nearestRad * 0.22);
        float effCharW = effCharH * (5.0 / 7.0);
        float effKern  = effCharW * kerning;
        float lineH    = effCharH * 1.30;

        int charsPerRow = int(floor((boxHalf * 2.0) / effKern));
        int maxRows     = int(floor((boxHalf * 2.0) / lineH));
        if (charsPerRow < 1) charsPerRow = 1;
        if (maxRows     < 1) maxRows     = 1;

        // Pixel position inside the text box. Top-left origin so rows
        // read top→bottom and columns read left→right (left-aligned).
        float lx = localP.x + boxHalf;
        float ly = boxHalf - localP.y;
        if (lx < 0.0 || lx > boxHalf * 2.0) continue;
        if (ly < 0.0 || ly > boxHalf * 2.0) continue;

        int targetCol = int(floor(lx / effKern));
        int targetRow = int(floor(ly / lineH));
        if (targetCol >= charsPerRow) continue;
        if (targetRow >= maxRows)     continue;

        // Row band gap — only the upper effCharH of the lineH strip
        // carries the glyph; the remainder is inter-line whitespace.
        float yInRow = ly - float(targetRow) * lineH;
        if (yInRow > effCharH) continue;

        // Each node owns a paragraph-sized chunk of the wrapped
        // message stream, sized to roughly fill the bubble's grid.
        int budget = charsPerRow * maxRows;
        int chunkStart = (c * nodesEach + nearestNode) * budget;

        // Word-wrap walk. Maintain (cursorR, cursorC); a word that
        // won't fit wraps as a unit. outCh is filled when the walk
        // reaches the target cell.
        int cursorR = 0;
        int cursorC = 0;
        int outCh = -1;

        for (int i = 0; i < MAX_WALK; i++) {
            if (i >= budget) break;
            if (cursorR > targetRow) break;

            int rawIdx    = chunkStart + i;
            int globalIdx = rawIdx - (rawIdx / total) * total;
            int ch        = getChar(globalIdx);

            if (ch == SPACE_CH) {
                // Look-ahead: length of the upcoming word.
                int wlen = 0;
                for (int j = 1; j < MAX_WALK; j++) {
                    int jj = i + j;
                    if (jj >= budget) break;
                    int gj  = chunkStart + jj;
                    int gjm = gj - (gj / total) * total;
                    int chj = getChar(gjm);
                    if (chj == SPACE_CH || chj < 0 || chj > 36) break;
                    wlen++;
                }
                if (cursorC > 0 && cursorC + 1 + wlen > charsPerRow) {
                    // Wrap before the word: drop the space, advance row.
                    cursorR++;
                    cursorC = 0;
                } else if (cursorC > 0) {
                    if (cursorR == targetRow && cursorC == targetCol) {
                        outCh = SPACE_CH;
                    }
                    cursorC++;
                }
                // Leading-space-on-new-row is silently consumed.
            } else if (ch >= 0 && ch <= 36) {
                if (cursorR == targetRow && cursorC == targetCol) {
                    outCh = ch;
                }
                cursorC++;
                if (cursorC >= charsPerRow) {
                    // Hard wrap — single word exceeded row width.
                    cursorR++;
                    cursorC = 0;
                }
            }
        }

        // Space cells render nothing (atlas idx 26 is blank); skip.
        if (outCh < 0 || outCh > 35 || outCh == SPACE_CH) continue;

        // V flipped: ly grows top→bottom on screen, atlas glyphs are stored
        // with V=1 at top (matches OpenGL texture origin at bottom-left).
        vec2 cellLocal = vec2((lx - float(targetCol) * effKern) / effCharW,
                              1.0 - yInRow / effCharH);
        float s = sampleChar(outCh, cellLocal);
        s = smoothstep(0.18, 0.55, s);
        if (s > 0.001) {
            vec3 inkColor;
            if (autoTextColor) {
                float lum = dot(cTint, vec3(0.299, 0.587, 0.114));
                inkColor = (lum > 0.55) ? vec3(0.04, 0.04, 0.07) : vec3(1.0);
            } else {
                inkColor = manualTextColor.rgb;
            }
            float w = s * env * fill;
            charMask = max(charMask, w);
            textCol  = mix(textCol, inkColor, w);
        }
    }

    // Slow morphing perturbation on the bridges — nudges the SDF
    // by a low-frequency fbm so the connecting tissue between
    // clusters wobbles and blooms organically rather than reading
    // as straight metaball capsules.
    if (morphAmp > 0.001) {
        float n = fbm2(p * 1.4 + vec2(TIME * 0.07, TIME * -0.05));
        blobSdf -= (n - 0.5) * morphAmp * 0.07;
    }

    // Compose: bg ← cluster blob ← text ink.
    float blobFw   = fwidth(blobSdf);
    float blobFill = 1.0 - smoothstep(-blobFw, blobFw, blobSdf);
    col = mix(col, blobCol, blobFill);
    col = mix(col, textCol, charMask);

    float alpha = 1.0;
    if (transparentBg) {
        alpha = clamp(blobFill, 0.0, 1.0);
        col   = mix(blobCol, textCol, charMask);
    }

    gl_FragColor = vec4(col, alpha);
}
