/*{
  "DESCRIPTION": "Vishes Mandala — cellular walkers leave hue-drifting trails in N-fold kaleidoscope symmetry. 8-fold default creates mandala patterns; all trails are fully saturated HDR 2.5×. LINEAR HDR out.",
  "CREDIT": "ShaderClaw — cell-walker sketch translated to multi-pass ISF",
  "CATEGORIES": ["Generator"],
  "INPUTS": [
    { "NAME": "gridSize", "LABEL": "Grid Size", "TYPE": "float", "DEFAULT": 120.0, "MIN": 20.0, "MAX": 400.0 },
    { "NAME": "walkers", "LABEL": "Walkers", "TYPE": "float", "DEFAULT": 6.0, "MIN": 1.0, "MAX": 16.0 },
    { "NAME": "stepRate", "LABEL": "Step Rate", "TYPE": "float", "DEFAULT": 40.0, "MIN": 1.0, "MAX": 240.0 },
    { "NAME": "hueDrift", "LABEL": "Hue Drift", "TYPE": "float", "DEFAULT": 0.015, "MIN": 0.0, "MAX": 0.1 },
    { "NAME": "fadeRate", "LABEL": "Trail Fade", "TYPE": "float", "DEFAULT": 0.004, "MIN": 0.0, "MAX": 0.08 },
    { "NAME": "hdrPeak", "LABEL": "HDR Peak", "TYPE": "float", "DEFAULT": 2.5, "MIN": 1.0, "MAX": 4.0 },
    { "NAME": "bloom", "LABEL": "Bloom", "TYPE": "float", "DEFAULT": 0.35, "MIN": 0.0, "MAX": 1.5 },
    { "NAME": "pulse", "LABEL": "Audio Pulse", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 2.0 },
    { "NAME": "symmetryFold", "LABEL": "Symmetry", "TYPE": "float", "DEFAULT": 8.0, "MIN": 2.0, "MAX": 16.0 },
    { "NAME": "bounceEdges", "LABEL": "Bounce Edges", "TYPE": "bool", "DEFAULT": true },
    { "NAME": "backgroundColor", "LABEL": "BG Color", "TYPE": "color", "DEFAULT": [0.0, 0.0, 0.0, 1.0] }
  ],
  "PASSES": [
    { "TARGET": "stateBuf", "PERSISTENT": true, "WIDTH": 16, "HEIGHT": 1 },
    { "TARGET": "canvas", "PERSISTENT": true },
    {}
  ]
}*/

#define MAX_WALKERS 16
#define TAU 6.28318530718

float hash11(float p) {
    p = fract(p * 0.1031);
    p *= p + 33.33;
    p *= p + p;
    return fract(p);
}

float hash12(vec2 p) {
    vec3 p3 = fract(vec3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.x + p3.y) * p3.z);
}

vec3 hsv2rgb(vec3 c) {
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

vec2 neighborDir(int dir) {
    if (dir == 0) return vec2(-1.0, -1.0);
    if (dir == 1) return vec2( 0.0, -1.0);
    if (dir == 2) return vec2( 1.0, -1.0);
    if (dir == 3) return vec2(-1.0,  0.0);
    if (dir == 4) return vec2( 1.0,  0.0);
    if (dir == 5) return vec2(-1.0,  1.0);
    if (dir == 6) return vec2( 0.0,  1.0);
    return vec2( 1.0,  1.0);
}

vec4 readWalker(float id) {
    return texture2D(stateBuf, vec2((id + 0.5) / 16.0, 0.5));
}

// Fold UV into one sector of N-fold rotational symmetry
vec2 mandalaUV(vec2 uv, float folds) {
    // Center the UV
    vec2 p = uv * 2.0 - 1.0;
    // Polar coords
    float r = length(p);
    float a = atan(p.y, p.x);
    // Fold into one sector
    float sector = TAU / folds;
    a = mod(a, sector);
    if (a > sector * 0.5) a = sector - a; // mirror within sector
    // Back to cartesian, then back to [0,1]
    p = r * vec2(cos(a), sin(a));
    return p * 0.5 + 0.5;
}

void main() {
    vec2 Res = RENDERSIZE;
    vec2 pos = gl_FragCoord.xy;
    float cell = 1.0 / max(gridSize, 1.0);
    float audio = 1.0 + audioLevel * pulse;

    // =============================================================
    // PASS 0: advance walker state buffer (16x1)
    // state encoding: vec4(x_norm, y_norm, hue, stepAccumulator)
    // =============================================================
    if (PASSINDEX == 0) {
        float id = floor(pos.x);
        if (id >= walkers) {
            gl_FragColor = vec4(0.0);
            return;
        }

        // Seed the walker in center quadrant on first frames
        // (their 1/8 sector gets reflected × 8 via mandalaUV)
        if (FRAMEINDEX < 2) {
            float jx = (hash11(id * 7.31 + 1.0) - 0.5) * 0.15;
            float jy = (hash11(id * 3.19 + 2.0) - 0.5) * 0.15;
            float h0 = hash11(id * 11.7 + 3.0);
            gl_FragColor = vec4(0.5 + jx, 0.5 + jy, h0, 0.0);
            return;
        }

        vec4 prev = readWalker(id);
        vec2 p = prev.rg;
        float h = prev.b;
        float acc = prev.a + TIMEDELTA * stepRate * audio;

        // Walk up to 6 discrete cell steps this frame
        for (int s = 0; s < 6; s++) {
            if (acc < 1.0) break;
            acc -= 1.0;

            float seed = TIME * 97.13 + id * 13.7 + float(s) * 3.31;
            float r = hash12(vec2(seed, seed * 0.47));
            int dir = int(floor(r * 8.0));
            vec2 stepVec = neighborDir(dir) * cell;
            p += stepVec;

            if (bounceEdges) {
                if (p.x < 0.0) p.x = -p.x;
                if (p.x > 1.0) p.x = 2.0 - p.x;
                if (p.y < 0.0) p.y = -p.y;
                if (p.y > 1.0) p.y = 2.0 - p.y;
            } else {
                p = fract(p);
            }

            float dh = (hash12(vec2(seed + 7.7, id)) - 0.5) * 2.0 * hueDrift;
            h = fract(h + dh + 1.0);
        }

        gl_FragColor = vec4(p, h, acc);
        return;
    }

    // =============================================================
    // PASS 1: update persistent canvas (fade + paint walker cells)
    // Mandala kaleidoscope: fold canvas UV before checking walker
    // =============================================================
    if (PASSINDEX == 1) {
        vec2 uv = pos / Res;
        vec4 prev = texture2D(canvas, uv);
        vec4 col = prev * (1.0 - fadeRate);

        // Aspect-correct grid so cells stay square
        float aspect = Res.x / Res.y;

        // Fold the canvas UV into mandala space (N-fold symmetry)
        float folds = max(symmetryFold, 2.0);
        vec2 foldedUV = mandalaUV(uv, folds);
        vec2 gridUV = vec2(foldedUV.x * aspect, foldedUV.y);
        vec2 pxCell = floor(gridUV * gridSize);

        for (int i = 0; i < MAX_WALKERS; i++) {
            if (float(i) >= walkers) break;
            vec4 st = readWalker(float(i));
            // Walker positions are in center quadrant — map to folded space
            vec2 wGridUV = vec2(st.r * aspect * 0.5 + 0.25, st.g * 0.5 + 0.25);
            vec2 wCell = floor(wGridUV * gridSize);
            vec2 diff = abs(wCell - pxCell);
            if (diff.x < 0.5 && diff.y < 0.5) {
                // saturation hardcoded 1.0, brightness = hdrPeak * audio
                vec3 rgb = hsv2rgb(vec3(st.b, 1.0, hdrPeak * audio));
                col = vec4(rgb, 1.0);
            }
        }

        gl_FragColor = col;
        return;
    }

    // =============================================================
    // PASS 2: final display (bloom + background blend)
    // =============================================================
    vec2 uv = pos / Res;
    vec3 c = texture2D(canvas, uv).rgb;

    if (bloom > 0.001) {
        vec3 sum = vec3(0.0);
        float r = 2.5 / min(Res.x, Res.y);
        for (int x = -2; x <= 2; x++) {
            for (int y = -2; y <= 2; y++) {
                vec2 off = vec2(float(x), float(y)) * r;
                sum += texture2D(canvas, uv + off).rgb;
            }
        }
        sum /= 25.0;
        c += sum * bloom;
    }

    float lum = max(c.r, max(c.g, c.b));
    float alpha = clamp(lum * 8.0, 0.0, 1.0);
    vec3 outRgb = mix(backgroundColor.rgb, c, alpha);
    gl_FragColor = vec4(outRgb, 1.0);
}
