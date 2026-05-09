/*{
  "DESCRIPTION": "2D Abstract Expressionist painting — de Kooning / Kline style gestural brush strokes, cool cobalt/black/titanium palette",
  "CREDIT": "ShaderClaw auto-improve 2026-05-09",
  "ISFVSN": "2",
  "CATEGORIES": ["Generator", "Audio Reactive"],
  "INPUTS": [
    { "NAME": "brushSize",   "LABEL": "Brush Size",   "TYPE": "float", "DEFAULT": 4.0, "MIN": 1.0, "MAX": 12.0 },
    { "NAME": "impasto",     "LABEL": "Impasto",      "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 2.0 },
    { "NAME": "paintSpec",   "LABEL": "Specular",     "TYPE": "float", "DEFAULT": 0.9, "MIN": 0.0, "MAX": 2.0 },
    { "NAME": "swirlSpeed",  "LABEL": "Swirl Speed",  "TYPE": "float", "DEFAULT": 0.18,"MIN": 0.0, "MAX": 1.0 },
    { "NAME": "coolWarm",    "LABEL": "Cool/Warm",    "TYPE": "float", "DEFAULT": 0.3, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "audioReact",  "LABEL": "Audio React",  "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 2.0 },
    { "NAME": "vignetteAmt", "LABEL": "Vignette",     "TYPE": "float", "DEFAULT": 0.8, "MIN": 0.0, "MAX": 3.0 },
    { "NAME": "inputTex",    "LABEL": "Texture",      "TYPE": "image" }
  ],
  "PASSES": [
    { "TARGET": "paintBuf", "PERSISTENT": false },
    {}
  ]
}*/

#define PI 3.14159265358979323846
#define TAU 6.28318530717958647692

// ---- Noise / hash ----
float hash11(float n) { return fract(sin(n) * 43758.5453123); }
float hash21(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123); }

// Value noise
float vnoise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);
    float a = hash21(i);
    float b = hash21(i + vec2(1.0, 0.0));
    float c = hash21(i + vec2(0.0, 1.0));
    float d = hash21(i + vec2(1.0, 1.0));
    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

// FBM — 4 octaves, directional bias for gestural strokes
float fbm(vec2 p, float angle) {
    float c = cos(angle), s = sin(angle);
    mat2 rot = mat2(c, -s, s, c);
    float v = 0.0, amp = 0.5, freq = 1.0;
    for (int i = 0; i < 4; i++) {
        v += amp * vnoise(p * freq);
        p = rot * p + vec2(0.31, 0.17);
        freq *= 2.1;
        amp *= 0.5;
    }
    return v;
}

// Domain-warped FBM for gestural brush strokes
float gestural(vec2 uv, float t, float audioEnergy) {
    // Two levels of domain warp to simulate brush pressure and direction
    float brushAngle = PI * 0.15;  // dominant stroke direction (slight diagonal, like de Kooning)
    float audioBoost = 1.0 + audioEnergy * 1.2 * audioReact;  // K=1.2

    // First warp: large-scale stroke layout
    float warpScale = brushSize * 0.5;
    vec2 q = vec2(
        fbm(uv * warpScale + vec2(0.0, 0.0), brushAngle),
        fbm(uv * warpScale + vec2(5.2, 1.3), brushAngle + PI * 0.5)
    );

    // Second warp: finer stroke texture
    float innerScale = brushSize * 1.2;
    vec2 r = vec2(
        fbm(uv * innerScale + 4.0 * q + vec2(1.7, 9.2) + t * swirlSpeed, brushAngle + 0.3),
        fbm(uv * innerScale + 4.0 * q + vec2(8.3, 2.8) + t * swirlSpeed * 0.7, brushAngle - 0.3)
    );

    return fbm(uv * brushSize * 0.8 + 4.0 * r * audioBoost, brushAngle);
}

// ---- Palette: carbon black, cobalt blue, titanium white, slate gray ----
// coolWarm=0: cool cobalt/black, coolWarm=1: warm umber/sienna
vec3 procPigment(float v, float t, float audioEnergy) {
    // Clamp value to [0,1]
    v = clamp(v, 0.0, 1.0);

    // Cool palette base
    vec3 black  = vec3(0.02, 0.02, 0.03);          // carbon black
    vec3 cobalt = vec3(0.08, 0.25, 0.85) * 1.2;    // cobalt blue 1.2 (HDR)
    vec3 slate  = vec3(0.25, 0.30, 0.40) * 0.8;    // slate gray 0.8
    vec3 white  = vec3(1.0, 1.05, 1.1) * 2.5;      // titanium white 2.5

    // Warm palette for coolWarm>0
    vec3 umber  = vec3(0.55, 0.28, 0.08) * 1.0;    // raw umber
    vec3 sienna = vec3(0.8, 0.35, 0.12) * 1.2;     // burnt sienna
    vec3 warmWh = vec3(1.1, 1.05, 0.95) * 2.5;     // warm white

    // 4 stops for cool: black → cobalt → slate → titanium white
    vec3 coolCol;
    if (v < 0.25) {
        coolCol = mix(black, cobalt, v / 0.25);
    } else if (v < 0.55) {
        coolCol = mix(cobalt, slate, (v - 0.25) / 0.30);
    } else if (v < 0.80) {
        coolCol = mix(slate, white * 0.6, (v - 0.55) / 0.25);
    } else {
        // Ridge peaks: titanium white 2.5 → 3.0 on spikes
        float ridgeFactor = (v - 0.80) / 0.20;
        coolCol = mix(white * 0.6, white * (1.0 + ridgeFactor * 0.4), ridgeFactor);
    }

    // 4 stops for warm: black → umber → sienna → warm white
    vec3 warmCol;
    if (v < 0.25) {
        warmCol = mix(black, umber, v / 0.25);
    } else if (v < 0.55) {
        warmCol = mix(umber, sienna, (v - 0.25) / 0.30);
    } else if (v < 0.80) {
        warmCol = mix(sienna, warmWh * 0.6, (v - 0.55) / 0.25);
    } else {
        float ridgeFactor = (v - 0.80) / 0.20;
        warmCol = mix(warmWh * 0.6, warmWh * (1.0 + ridgeFactor * 0.4), ridgeFactor);
    }

    // Blend cool/warm by parameter
    return mix(coolCol, warmCol, coolWarm);
}

void main() {
    vec2 pos = gl_FragCoord.xy;
    vec2 uv = pos / RENDERSIZE;

    // ==== PASS 0: Gestural painting generation ====
    if (PASSINDEX == 0) {
        float t = TIME;

        // Audio energy — drives brush stroke intensity
        float audioEnergy = audioBass * 0.6 + audioMid * 0.3 + audioHigh * 0.1;

        // Scale UV to preserve aspect-correct stroke proportions
        float aspect = RENDERSIZE.x / max(RENDERSIZE.y, 1.0);
        vec2 sUV = vec2(uv.x * aspect, uv.y);

        // Multiple overlapping stroke fields — de Kooning layered approach
        float stroke1 = gestural(sUV * 0.8, t, audioEnergy);
        float stroke2 = gestural(sUV * 1.3 + vec2(3.1, 7.4), t * 1.1, audioEnergy * 0.7);
        float stroke3 = gestural(sUV * 0.5 + vec2(1.9, 4.2), t * 0.8, audioEnergy * 0.5);

        // Combine strokes: dominant stroke1 with accent interference
        float combined = stroke1 * 0.60 + stroke2 * 0.25 + stroke3 * 0.15;

        // Add sharp ridge discontinuities (palette knife scrapes)
        float ridge = abs(fract(combined * 3.0 + t * swirlSpeed * 0.1) - 0.5) * 2.0;
        float ridgeMask = smoothstep(0.7, 1.0, ridge) * impasto * 0.4;
        combined = combined + ridgeMask * 0.2;

        // Normalize to [0,1] range
        combined = clamp(combined, 0.0, 1.0);

        // Apply pigment palette
        vec3 paintColor = procPigment(combined, t, audioEnergy);

        // Optional: if inputTex is bound, use as base layer (Kuwahara-style blend)
        // We check if the texture has content by sampling center
        // (ISF provides inputTex if bound, otherwise it's black)
        vec4 texSample = IMG_NORM_PIXEL(inputTex, uv);
        float texPresent = step(0.01, dot(texSample.rgb, vec3(0.333)));
        if (texPresent > 0.5) {
            // Blend texture with painting at 30% texture / 70% painting
            paintColor = mix(paintColor, texSample.rgb * paintColor, 0.3);
        }

        gl_FragColor = vec4(paintColor, 1.0);
        return;
    }

    // ==== PASS 1: Impasto relief lighting + specular + vignette ====
    vec2 texel = 1.0 / RENDERSIZE;

    // Sample luminance neighborhood for surface normal estimation
    float valC = dot(texture2D(paintBuf, uv).rgb, vec3(0.2126, 0.7152, 0.0722));
    float valR = dot(texture2D(paintBuf, uv + vec2( texel.x,      0.0)).rgb, vec3(0.2126, 0.7152, 0.0722));
    float valL = dot(texture2D(paintBuf, uv - vec2( texel.x,      0.0)).rgb, vec3(0.2126, 0.7152, 0.0722));
    float valU = dot(texture2D(paintBuf, uv + vec2(     0.0, texel.y)).rgb, vec3(0.2126, 0.7152, 0.0722));
    float valD = dot(texture2D(paintBuf, uv - vec2(     0.0, texel.y)).rgb, vec3(0.2126, 0.7152, 0.0722));

    // Impasto controls relief depth
    float reliefDepth = 80.0 + impasto * 120.0;
    vec3 norm = normalize(vec3(
        (valR - valL) / texel.x * impasto,
        (valU - valD) / texel.y * impasto,
        reliefDepth
    ));

    // Raking light from upper-left (classic studio lighting for impasto)
    vec3 light = normalize(vec3(-0.8, 1.2, 1.4));
    float diff = clamp(dot(norm, light) * 0.5 + 0.5, 0.0, 1.0);

    // Specular — titanium white paint has high gloss on ridges
    vec3 viewDir = vec3(0.0, 0.0, 1.0);
    vec3 halfVec = normalize(light + viewDir);
    float spec = pow(max(0.0, dot(norm, halfVec)), 32.0) * paintSpec;

    // Apply diffuse to paint color
    vec4 paintCol = texture2D(paintBuf, uv);
    vec3 lit = paintCol.rgb * mix(diff, 1.0, 0.6);

    // Specular highlight — warm gloss on titanium white peaks
    lit += spec * vec3(1.0, 1.02, 1.05) * 1.5;

    // Vignette — darkens edges to focus on canvas center
    if (vignetteAmt > 0.0) {
        vec2 scc = (pos - 0.5 * RENDERSIZE) / RENDERSIZE.x;
        float vign = 1.1 - vignetteAmt * dot(scc, scc);
        vign *= 1.0 - 0.7 * vignetteAmt * exp(-sin(uv.x * PI) * 40.0);
        vign *= 1.0 - 0.7 * vignetteAmt * exp(-sin(uv.y * PI) * 20.0);
        lit *= max(vign, 0.0);
    }

    // Output linear HDR — no tonemapping, no ACES, no clamp
    gl_FragColor = vec4(lit, 1.0);
}
