/*{
  "DESCRIPTION": "American Flag at Night — 13 stripes + 50-star canton, billowing against a deep starry night sky. HDR peaks: red stripes 2.0, white 2.5, canton blue 1.8, star tips 2.5+. Audio K ≤ 1.5. Moonlight-style directional fabric shading. Calm wind default.",
  "CREDIT": "ShaderClaw",
  "CATEGORIES": ["Generator", "Audio Reactive"],
  "INPUTS": [
    { "NAME": "windStrength", "LABEL": "Wind",        "TYPE": "float", "DEFAULT": 0.06, "MIN": 0.0, "MAX": 0.25 },
    { "NAME": "windSpeed",    "LABEL": "Wind Speed",  "TYPE": "float", "DEFAULT": 1.2,  "MIN": 0.0, "MAX": 4.0 },
    { "NAME": "windScale",    "LABEL": "Wind Scale",  "TYPE": "float", "DEFAULT": 4.5,  "MIN": 0.5, "MAX": 12.0 },
    { "NAME": "flagFill",     "LABEL": "Flag Fill",   "TYPE": "float", "DEFAULT": 0.88, "MIN": 0.5, "MAX": 1.0 },
    { "NAME": "shadeStrength","LABEL": "Fabric Shading","TYPE": "float","DEFAULT": 0.55,"MIN": 0.0, "MAX": 1.5 },
    { "NAME": "fabricNoise",  "LABEL": "Fabric Noise","TYPE": "float", "DEFAULT": 0.06, "MIN": 0.0, "MAX": 0.4 },
    { "NAME": "audioGust",    "LABEL": "Audio Gust",  "TYPE": "float", "DEFAULT": 0.6,  "MIN": 0.0, "MAX": 1.5 },
    { "NAME": "audioFlap",    "LABEL": "Audio Flap",  "TYPE": "float", "DEFAULT": 0.5,  "MIN": 0.0, "MAX": 1.5 },
    { "NAME": "starGlow",     "LABEL": "Star Glow",   "TYPE": "float", "DEFAULT": 0.60, "MIN": 0.0, "MAX": 1.5 },
    { "NAME": "starSize",     "LABEL": "Star Size",   "TYPE": "float", "DEFAULT": 0.42, "MIN": 0.15,"MAX": 0.7 },
    { "NAME": "redColor",     "LABEL": "Red",         "TYPE": "color", "DEFAULT": [0.820, 0.08, 0.10, 1.0] },
    { "NAME": "whiteColor",   "LABEL": "White",       "TYPE": "color", "DEFAULT": [1.0, 1.0, 1.0, 1.0] },
    { "NAME": "blueColor",    "LABEL": "Blue (Canton)","TYPE": "color","DEFAULT": [0.18, 0.20, 0.55, 1.0] },
    { "NAME": "nightSky",     "LABEL": "Night Sky",   "TYPE": "bool",  "DEFAULT": true },
    { "NAME": "moonAngle",    "LABEL": "Moon Angle",  "TYPE": "float", "DEFAULT": 0.8, "MIN": 0.0, "MAX": 6.2832 }
  ]
}*/

#ifdef GL_ES
precision highp float;
#endif

#define PI 3.14159265358979
#define TAU 6.28318530718

// Official US flag proportions
#define FLAG_RATIO  1.9
#define CANTON_W    (2.0/5.0)
#define CANTON_H    (7.0/13.0)
#define STRIPE_H    (1.0/13.0)

float hash12(vec2 p) {
    vec3 p3 = fract(vec3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.x + p3.y) * p3.z);
}

float vnoise(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash12(i), b = hash12(i + vec2(1,0));
    float c = hash12(i + vec2(0,1)), d = hash12(i + vec2(1,1));
    return mix(mix(a,b,f.x), mix(c,d,f.x), f.y);
}

// 5-pointed star SDF in flag-local star cell coords (-1..1).
float starSDF(vec2 p, float r) {
    const float k1x = 0.809016994;
    const float k1y = 0.587785252;
    const float k2x = 0.309016994;
    const float k2y = 0.951056516;
    p.x = abs(p.x);
    p -= 2.0 * max(dot(vec2(-k1x,k1y), p), 0.0) * vec2(-k1x, k1y);
    p -= 2.0 * max(dot(vec2( k1x,k1y), p), 0.0) * vec2( k1x, k1y);
    p.x = abs(p.x);
    p.y -= r;
    vec2 ba = vec2(k2x, -k2y) * r * 0.5;
    float h = clamp(dot(p, ba) / dot(ba, ba), 0.0, r);
    return length(p - ba * h) * sign(p.y * ba.x - p.x * ba.y);
}

// ── Night sky background ──────────────────────────────────────────────
vec3 nightSkyColor(vec2 uv, float moonAng) {
    // Deep indigo gradient
    vec3 skyTop = vec3(0.02, 0.03, 0.10);
    vec3 skyBot = vec3(0.05, 0.06, 0.18);
    vec3 col = mix(skyBot, skyTop, uv.y);

    // Stars — procedural hash grid
    vec2 sg = uv * 200.0;
    vec2 sgi = floor(sg);
    float sr = hash12(sgi);
    if (sr > 0.94) {
        vec2 sOf = fract(sg) - 0.5;
        float sd = length(sOf);
        float smag = hash12(sgi + 17.3);
        float sbright = pow(smag, 3.0) * 2.50 + 0.40;   // HDR stars
        col += vec3(sbright) * smoothstep(0.18, 0.0, sd);
    }

    // Moon — directional glow
    vec2 moonDir = normalize(vec2(cos(moonAng), sin(moonAng) * 0.5 + 0.8));
    float moonGlow = exp(-pow(distance(uv, vec2(0.5) + moonDir * 0.4) * 5.0, 2.0));
    col += vec3(0.90, 0.94, 1.00) * moonGlow * 0.80;

    // Horizon glow
    col += vec3(0.08, 0.10, 0.25) * (1.0 - uv.y) * 0.6;

    return col;
}

void main() {
    vec2 Res = RENDERSIZE;
    vec2 uv  = isf_FragNormCoord.xy;
    float aspect = Res.x / Res.y;

    float bass  = audioBass;
    float high  = audioHigh;
    float level = audioLevel;

    // ── Flag placement ────────────────────────────────────────────────
    float vpRatio = aspect;
    float fillW, fillH;
    if (vpRatio > FLAG_RATIO) {
        fillH = flagFill; fillW = fillH * FLAG_RATIO / vpRatio;
    } else {
        fillW = flagFill; fillH = fillW * vpRatio / FLAG_RATIO;
    }
    vec2 flagMin = vec2(0.5 - fillW*0.5, 0.5 - fillH*0.5);
    vec2 flagMax = vec2(0.5 + fillW*0.5, 0.5 + fillH*0.5);
    vec2 flagUV  = (uv - flagMin) / (flagMax - flagMin);
    bool inside  = flagUV.x >= 0.0 && flagUV.x <= 1.0
                && flagUV.y >= 0.0 && flagUV.y <= 1.0;

    // ── Wind displacement ─────────────────────────────────────────────
    float anchor = clamp(flagUV.x, 0.0, 1.0);
    // Audio K capped: total gust = 1 + audioGust*(bass*1.0 + 0.5) ≤ 1 + 1.5*1.5 = 3.25
    // But audioGust MAX is 1.5 and K = audioGust*(bass + 0.5) at max = 1.5*1.5 = 2.25.
    // Fix: restructure as baseRate*(1 + K*bass), K = audioGust ≤ 1.5 ✓
    float gust = 1.0 + audioGust * bass;   // K = audioGust ≤ 1.5 ✓
    float flap = audioFlap * high;          // K = audioFlap ≤ 1.5 ✓

    float t  = TIME * windSpeed;
    float wx = flagUV.x * windScale;
    float wy = flagUV.y * windScale * 0.6;

    float wave  = sin(wx * 1.5 - t * 1.2 + flagUV.y * 2.0);
    wave       += 0.55 * sin(wx * 3.1 - t * 1.9 + flagUV.y * 4.3);
    wave       += 0.30 * sin(wx * 5.7 - t * 2.7 + flagUV.y * 7.1 + flap * 4.0);
    float crossWave = sin(wy * 2.3 + t * 0.9) * 0.5;
    float disp = wave + crossWave;
    float anchorMask = pow(anchor, 1.4);

    vec2 warped = flagUV;
    warped.y += disp * windStrength * anchorMask * gust;
    warped.x += sin(wy * 1.7 + t * 0.7) * windStrength * 0.35 * anchorMask * gust;

    // Fabric shading from wave slope
    float slope = cos(wx * 1.5 - t * 1.2 + flagUV.y * 2.0)
                + 0.55 * cos(wx * 3.1 - t * 1.9 + flagUV.y * 4.3) * 1.5;
    float shade = 1.0 + slope * 0.18 * shadeStrength * anchorMask;

    // Moonlight directional shading: brighter on the side facing moon
    float moonBias = 0.5 + 0.5 * cos(moonAngle);
    shade *= 0.85 + 0.15 * moonBias;

    float folds = vnoise(vec2(warped.x * 18.0, warped.y * 26.0) + t * 0.3);
    shade *= 1.0 + (folds - 0.5) * fabricNoise;

    // ── Background ────────────────────────────────────────────────────
    vec3 col;
    if (nightSky) {
        col = nightSkyColor(uv, moonAngle);
    } else {
        col = vec3(0.02, 0.02, 0.06);
    }

    if (inside) {
        vec2 fc = clamp(warped, vec2(0.0), vec2(1.0));

        // Stripes — HDR saturated colours
        float yTop    = 1.0 - fc.y;
        float row     = floor(yTop / STRIPE_H);
        bool  redSt   = mod(row, 2.0) < 0.5;
        // Red: 2.0 HDR, white: 2.5 HDR for bloom
        vec3 redHDR   = redColor.rgb * 2.00;
        vec3 whiteHDR = whiteColor.rgb * 2.50;
        vec3 stripeCol = redSt ? redHDR : whiteHDR;

        bool inCanton = fc.x < CANTON_W && yTop < CANTON_H;
        vec3 surface  = stripeCol;

        if (inCanton) {
            surface = blueColor.rgb * 1.80;   // canton blue HDR

            vec2 cantonUV = vec2(fc.x / CANTON_W, yTop / CANTON_H);
            float colsBase = 6.0, rows = 9.0;
            float ry = cantonUV.y * rows;
            float rIdx = floor(ry), rFrac = ry - rIdx;
            bool  oddRow = mod(rIdx, 2.0) > 0.5;
            float colCount = oddRow ? 5.0 : 6.0;
            float xOffset  = oddRow ? 0.5 / 6.0 : 0.0;
            float rx = (cantonUV.x - xOffset) * colsBase;
            float cIdx = floor(rx), cFrac = rx - cIdx;
            bool validCol = cIdx >= 0.0 && cIdx < colCount;

            vec2 sp = vec2(cFrac - 0.5, rFrac - 0.5) * 2.0;
            float cellW = CANTON_W / colsBase * fillW;
            float cellH = CANTON_H / rows    * fillH;
            sp.x *= (cellW / cellH) / vpRatio;

            float d = starSDF(sp, starSize);
            if (validCol) {
                float starMask = smoothstep(0.02, -0.01, d);
                surface = mix(surface, whiteHDR, starMask);
                // HDR glow: star tips push to 2.5+
                float glow = exp(-max(d, 0.0) * 18.0) * starGlow
                           * (1.0 + high * 0.8);   // treble shimmer K≤1.5 ✓
                surface += vec3(0.70, 0.80, 1.00) * glow * 1.20;
            }
        }

        col = surface * shade;

        // Audio brightness lift (non-gating baseline)
        col *= 1.0 + level * 0.12;

        // Edge shadow at flag perimeter
        float edgeDist = min(min(flagUV.x, 1.0-flagUV.x), min(flagUV.y, 1.0-flagUV.y));
        col *= mix(0.70, 1.0, smoothstep(0.0, 0.025, edgeDist));
    } else {
        // Drop shadow under flag
        vec2 sOff = vec2(0.012, -0.014);
        vec2 sUV  = uv - sOff;
        bool inShadow = sUV.x > flagMin.x && sUV.x < flagMax.x
                     && sUV.y > flagMin.y && sUV.y < flagMax.y;
        if (inShadow) {
            float dx = min(sUV.x - flagMin.x, flagMax.x - sUV.x);
            float dy = min(sUV.y - flagMin.y, flagMax.y - sUV.y);
            float shadow = smoothstep(0.0, 0.03, min(dx,dy)) * 0.55;
            col = mix(col, vec3(0.0), shadow);
        }
    }

    // Surprise: every ~40s a star in the canton briefly fans the spectrum.
    {
        float _ph = fract(TIME / 40.0);
        float _f  = smoothstep(0.0, 0.06, _ph) * smoothstep(0.30, 0.18, _ph);
        if (uv.x < 0.38 && uv.y > 0.46) {
            float _h = fract(TIME * 0.6);
            vec3 _rainbow = 0.5 + 0.5 * cos(TAU * _h + vec3(0.0, 2.094, 4.188));
            col = mix(col, _rainbow * 2.20, _f * 0.35 * step(0.86, dot(col, vec3(0.299,0.587,0.114))));
        }
    }

    // LINEAR HDR out.
    gl_FragColor = vec4(col, 1.0);
}
