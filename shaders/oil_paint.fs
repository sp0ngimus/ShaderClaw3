/*{
  "DESCRIPTION": "Oil paint — Kuwahara painterly filter with HDR impasto relief, specular highlights, and a procedural cool night-seascape pigment fallback when no texture is bound. Prussian blue, cerulean, seafoam, slate. TIME-driven living surface, audio non-gating.",
  "CREDIT": "ShaderClaw (Kuwahara approach inspired by flockaroo)",
  "CATEGORIES": ["Effect", "Generator", "Audio Reactive"],
  "INPUTS": [
    { "NAME": "brushRadius", "LABEL": "Brush Size", "TYPE": "float", "DEFAULT": 4.0, "MIN": 1.0, "MAX": 12.0 },
    { "NAME": "impasto", "LABEL": "Impasto", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 2.0 },
    { "NAME": "paintSpec", "LABEL": "Specular", "TYPE": "float", "DEFAULT": 0.9, "MIN": 0.0, "MAX": 2.0 },
    { "NAME": "vignetteAmt", "LABEL": "Vignette", "TYPE": "float", "DEFAULT": 0.8, "MIN": 0.0, "MAX": 3.0 },
    { "NAME": "audioReact", "LABEL": "Audio React", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 2.0 },
    { "NAME": "swirlSpeed", "LABEL": "Swirl Speed", "TYPE": "float", "DEFAULT": 0.18, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "transparentBg", "LABEL": "Transparent", "TYPE": "bool", "DEFAULT": 0.0 },
    { "NAME": "inputTex", "LABEL": "Texture", "TYPE": "image" }
  ],
  "PASSES": [
    { "TARGET": "paintBuf", "PERSISTENT": false },
    {}
  ]
}*/

#define PI 3.1415927

// ---------- utilities ----------
float h21(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float vnoise(vec2 p) {
    vec2 ip = floor(p), fp = fract(p);
    fp = fp * fp * (3.0 - 2.0 * fp);
    float a = h21(ip);
    float b = h21(ip + vec2(1.0, 0.0));
    float c = h21(ip + vec2(0.0, 1.0));
    float d = h21(ip + vec2(1.0, 1.0));
    return mix(mix(a, b, fp.x), mix(c, d, fp.x), fp.y);
}
float fbm(vec2 p) {
    float s = 0.0, a = 0.5;
    for (int i = 0; i < 5; i++) {
        s += a * vnoise(p);
        p = p * 2.03 + vec2(13.1, 7.7);
        a *= 0.5;
    }
    return s;
}

// sRGB → linear (cheap approx) so video/image inputs land in linear-HDR space.
vec3 sToL(vec3 c) { return c * c * (0.6 + 0.4 * c); }

// ---------- procedural pigment blob field (fallback) ----------
// Saturated oil palette: cadmium red, ultramarine, viridian, naples
// yellow, raw umber. 3-5 large drifting blob clusters mixed via metaball
// weights, then overlaid with palette-knife streaks so the canvas reads
// as thick paint even with nothing plugged in.
vec3 procPigment(vec2 uv, float t) {
    // Aspect-correct local coords centered, so blobs stay round.
    vec2 p = (uv - 0.5) * vec2(RENDERSIZE.x / RENDERSIZE.y, 1.0);

    // Five drifting blob anchors — slow Lissajous so they wander.
    vec2 c0 = 0.55 * vec2(sin(t * 0.21 + 0.0), cos(t * 0.17 + 1.3));
    vec2 c1 = 0.50 * vec2(cos(t * 0.15 + 2.1), sin(t * 0.23 - 0.7));
    vec2 c2 = 0.60 * vec2(sin(t * 0.13 - 1.7), cos(t * 0.19 + 2.4));
    vec2 c3 = 0.45 * vec2(cos(t * 0.27 + 0.9), sin(t * 0.11 + 0.4));
    vec2 c4 = 0.52 * vec2(sin(t * 0.18 + 3.1), cos(t * 0.25 - 1.9));

    // Domain-warp p with a low-freq curl so blob edges feather like wet pigment.
    vec2 warp = vec2(
        fbm(p * 1.6 + vec2(0.0, t * 0.11)) - 0.5,
        fbm(p * 1.6 + vec2(7.3, t * 0.09 + 2.0)) - 0.5
    );
    vec2 pp = p + 0.32 * warp;

    // Cool night-seascape palette: prussian blue, cerulean, seafoam, slate, ink.
    vec3 prussian  = vec3(0.02, 0.12, 0.38);   // deep night sea
    vec3 cerulean  = vec3(0.05, 0.45, 0.88);   // sky reflection
    vec3 seafoam   = vec3(0.18, 0.82, 0.72);   // breaking wave foam — HDR
    vec3 slate     = vec3(0.28, 0.30, 0.38);   // grey rock shelf
    vec3 inkBlue   = vec3(0.01, 0.04, 0.12);   // canvas base (near-black ocean)

    // Per-blob radial weights. Variable sizes for compositional interest.
    float w0 = exp(-dot(pp - c0, pp - c0) * 5.5);
    float w1 = exp(-dot(pp - c1, pp - c1) * 6.5);
    float w2 = exp(-dot(pp - c2, pp - c2) * 5.0);
    float w3 = exp(-dot(pp - c3, pp - c3) * 7.5);
    float w4 = exp(-dot(pp - c4, pp - c4) * 6.0);
    float wSum = w0 + w1 + w2 + w3 + w4 + 0.18;

    // Weighted color blend — inkBlue as base canvas.
    vec3 col = inkBlue * 0.18;
    col += prussian * w0;
    col += cerulean * w1;
    col += seafoam  * w2 * 0.9;
    col += slate    * w3;
    col += prussian * w4 * 0.6 + cerulean * w4 * 0.4;
    col /= wSum;

    // Palette-knife streak texture
    vec2 sp = pp * 9.0;
    float angle = 0.6 + 0.4 * fbm(pp * 1.2);
    vec2 dir = vec2(cos(angle), sin(angle));
    float streak = fbm(vec2(dot(sp, dir) * 0.6, dot(sp, vec2(-dir.y, dir.x)) * 3.5));
    float knife = smoothstep(0.35, 0.85, streak);
    col *= mix(0.78, 1.32, knife);

    // Pigment grain
    float grain = fbm(pp * 22.0 + t * 0.05);
    col *= mix(0.92, 1.08, grain);

    // HDR foam crest highlights — seafoam peaks at ~1.9 linear for bloom.
    float ridge = smoothstep(0.78, 0.98, streak) *
                  smoothstep(0.35, 0.95, max(max(w0, w1), max(max(w2, w3), w4)));
    col += vec3(0.75, 1.00, 1.05) * ridge * 1.1;   // cool white seafoam HDR

    return col;
}

// ---------- Kuwahara filter on bound texture ----------
// Aspect-correct UV that "covers" the screen while keeping the texture
// upright and undistorted.
vec2 fitUV(vec2 pos) {
    vec2 ts = max(IMG_SIZE_inputTex, vec2(1.0));
    return (pos - 0.5 * RENDERSIZE) * min(ts.y / RENDERSIZE.y, ts.x / RENDERSIZE.x) / ts + 0.5;
}

// Find the quadrant with lowest variance and use its mean colour:
// flat painterly patches that abstract the source.
vec3 kuwahara(vec2 uv, float radius) {
    vec3 mean[4];
    vec3 var_acc[4];
    float count[4];
    for (int i = 0; i < 4; i++) {
        mean[i] = vec3(0.0);
        var_acc[i] = vec3(0.0);
        count[i] = 0.0;
    }
    for (int j = -6; j <= 6; j++) {
        for (int i = -6; i <= 6; i++) {
            if (abs(float(i)) > radius || abs(float(j)) > radius) continue;
            vec2 sUV = fitUV((uv * RENDERSIZE) + vec2(float(i), float(j)));
            vec3 c = sToL(texture2D(inputTex, sUV).rgb);
            int qi = (i >= 0) ? 0 : 1;
            int qj = (j >= 0) ? 0 : 2;
            int q = qi + qj;
            for (int k = 0; k < 4; k++) {
                if (k == q) {
                    mean[k] += c;
                    var_acc[k] += c * c;
                    count[k] += 1.0;
                }
            }
        }
    }
    float minVar = 1e8;
    vec3 result = vec3(0.0);
    for (int q = 0; q < 4; q++) {
        if (count[q] < 1.0) continue;
        vec3 m = mean[q] / count[q];
        vec3 v = var_acc[q] / count[q] - m * m;
        float totalVar = v.r + v.g + v.b;
        if (totalVar < minVar) {
            minVar = totalVar;
            result = m;
        }
    }
    return result;
}

void main() {
    vec2 pos = gl_FragCoord.xy;
    vec2 uv = pos / RENDERSIZE;

    // Audio NEVER gates output — every term has a baseline >= 1.0 so the
    // image breathes with sound but is fully visible in silence.
    float aL = audioLevel * audioReact;
    float aB = audioBass  * audioReact;
    float aH = audioHigh  * audioReact;

    bool hasTex = (IMG_SIZE_inputTex.x > 0.5);

    // ==== PASS 0: paint base into paintBuf ====
    if (PASSINDEX == 0) {
        vec3 base;
        if (hasTex) {
            // Kuwahara radius breathes with TIME and (optionally) audio.
            float r = brushRadius * (1.0 + 0.10 * sin(TIME * 0.6) + 0.25 * aL);
            r = clamp(r, 1.0, 6.0);
            base = kuwahara(uv, r);
        } else {
            // Procedural fallback — warm pigments swirling on TIME.
            float t = TIME * (0.4 + swirlSpeed) + 0.6 * aB;
            // Slight uv warp tied to TIME so the canvas wanders.
            vec2 wuv = uv + 0.012 * vec2(
                sin(uv.y * 6.0 + TIME * 0.4),
                cos(uv.x * 5.0 - TIME * 0.35)
            );
            base = procPigment(wuv, t);
        }
        // Linear HDR — cresting ridges allowed up to ~1.6 here so bloom
        // sees them after the relief pass adds specular.
        gl_FragColor = vec4(base, 1.0);
        return;
    }

    // ==== PASS 1: impasto relief + HDR specular + vignette ====
    vec2 texel = 1.0 / RENDERSIZE;

    // Sample neighbourhood luminance in linear space for height map.
    vec3 cC = texture2D(paintBuf, uv).rgb;
    vec3 cR = texture2D(paintBuf, uv + vec2(texel.x, 0.0)).rgb;
    vec3 cL = texture2D(paintBuf, uv - vec2(texel.x, 0.0)).rgb;
    vec3 cU = texture2D(paintBuf, uv + vec2(0.0, texel.y)).rgb;
    vec3 cD = texture2D(paintBuf, uv - vec2(0.0, texel.y)).rgb;

    float lC = dot(cC, vec3(0.299, 0.587, 0.114));
    float lR = dot(cR, vec3(0.299, 0.587, 0.114));
    float lL = dot(cL, vec3(0.299, 0.587, 0.114));
    float lU = dot(cU, vec3(0.299, 0.587, 0.114));
    float lD = dot(cD, vec3(0.299, 0.587, 0.114));

    // Surface normal from luminance gradient — taller bumps when impasto>1.
    float reliefScale = 80.0 / max(impasto, 0.001);
    vec3 norm = normalize(vec3(
        (lR - lL) / texel.x,
        (lU - lD) / texel.y,
        reliefScale
    ));

    // Light wobbles slightly with TIME (gallery raking light).
    vec3 light = normalize(vec3(
        -1.0 + 0.15 * sin(TIME * 0.25),
         1.0 + 0.10 * cos(TIME * 0.21),
         1.4
    ));

    float diff = clamp(dot(norm, light), 0.0, 1.0);
    // Half-vector specular for softer wet sheen.
    vec3 viewV = vec3(0.0, 0.0, 1.0);
    vec3 halfV = normalize(light + viewV);
    float specBase = pow(max(dot(norm, halfV), 0.0), 28.0);

    // fwidth() AA — soften the edge between flat patch and brush ridge so
    // the relief reads at any zoom. Edge strength = gradient magnitude.
    float gradMag = length(vec2(lR - lL, lU - lD)) / max(texel.x, 1e-5);
    float aaW = fwidth(lC) + 1e-5;
    float ridgeMask = smoothstep(0.0, aaW * 80.0 + 0.05, gradMag * texel.x);

    // HDR specular peak — 1.8 linear on bright ridges so the bloom
    // pass picks them up. Multiply by paintSpec for control, with a
    // baseline so ridges always glint a little.
    float specPeak = mix(0.55, 1.4, paintSpec);
    float specHDR  = specBase * ridgeMask * (1.8 + 0.4 * paintSpec) * specPeak;

    // Audio non-gating sparkle on highs — adds, never subtracts.
    specHDR *= 1.0 + 0.6 * aH;

    // Diffuse term — keep at least 0.55 so colour never goes black.
    float diffuse = mix(0.55, 1.0, diff);

    vec3 lit = cC * diffuse;
    // Warm-cool tinted specular — paint highlight is slightly cool-white
    // because of gallery daylight.
    vec3 specColor = vec3(0.90, 0.96, 1.05) * specHDR;
    vec3 outCol = lit + specColor;

    // Push brightest highlights toward 1.5–2.0 linear for bloom.
    float highMask = smoothstep(0.85, 1.05, dot(outCol, vec3(0.299, 0.587, 0.114)));
    outCol += vec3(0.85, 0.92, 1.0) * highMask * 0.7;

    // Vignette — darkens corners but never crushes to zero.
    if (vignetteAmt > 0.0) {
        vec2 scc = (pos - 0.5 * RENDERSIZE) / RENDERSIZE.x;
        float vign = 1.1 - vignetteAmt * dot(scc, scc);
        vign *= 1.0 - 0.35 * vignetteAmt * exp(-sin(pos.x / RENDERSIZE.x * PI) * 40.0);
        vign *= 1.0 - 0.35 * vignetteAmt * exp(-sin(pos.y / RENDERSIZE.y * PI) * 20.0);
        outCol *= max(vign, 0.25);
    }

    // Linear HDR output. No final tonemap — bloom/composite handles it.
    gl_FragColor = vec4(outCol, 1.0);
}
