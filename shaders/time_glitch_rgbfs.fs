/*{
  "DESCRIPTION": "Wave Interference — multiple colored wave sources creating additive interference patterns. HDR peaks where waves constructively interfere. Signal red 2.5, data green 2.5, electric blue 3.0. Standalone generator. LINEAR HDR out.",
  "CREDIT": "Easel — wave_interference_gen",
  "CATEGORIES": ["Generator", "Audio Reactive"],
  "INPUTS": [
    { "NAME": "sourceCount", "LABEL": "Wave Sources", "TYPE": "float", "DEFAULT": 5.0, "MIN": 2.0, "MAX": 8.0 },
    { "NAME": "waveFreq", "LABEL": "Wave Frequency", "TYPE": "float", "DEFAULT": 12.0, "MIN": 4.0, "MAX": 30.0 },
    { "NAME": "waveSpeed", "LABEL": "Wave Speed", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 2.0 },
    { "NAME": "driftSpeed", "LABEL": "Source Drift", "TYPE": "float", "DEFAULT": 0.20, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "hdrPeak", "LABEL": "HDR Peak", "TYPE": "float", "DEFAULT": 2.5, "MIN": 1.0, "MAX": 4.0 },
    { "NAME": "audioReact", "LABEL": "Audio React", "TYPE": "float", "DEFAULT": 0.8, "MIN": 0.0, "MAX": 2.0 }
  ]
}*/

float hash11(float n) { return fract(sin(n * 127.1) * 43758.5453); }

void main() {
    vec2 uv = gl_FragCoord.xy / RENDERSIZE.xy;
    float aspect = RENDERSIZE.x / max(RENDERSIZE.y, 1.0);
    vec2 p = vec2(uv.x * aspect, uv.y);
    float t = TIME;

    // Audio modulator — never gates (always has baseline 1.0)
    float audio = clamp(audioBass, 0.0, 1.0) * audioReact;
    float freqMod  = waveFreq  * (1.0 + audio * 0.8); // K=0.8 <= 1.5
    float speedMod = waveSpeed * (1.0 + audio * 0.5); // K=0.5 <= 1.5

    vec3 col = vec3(0.0, 0.0, 0.01); // near-black void base
    int N = int(clamp(sourceCount, 2.0, 8.0));

    for (int i = 0; i < 8; i++) {
        if (i >= N) break;
        float fi = float(i);

        // Source position drifts slowly via Lissajous
        float ox = 0.3 * aspect * sin(t * driftSpeed * (0.7 + hash11(fi * 3.1) * 0.6) + fi * 2.1);
        float oy = 0.3 * sin(t * driftSpeed * (0.5 + hash11(fi * 7.3) * 0.8) + fi * 1.7);
        vec2 src = vec2(aspect * 0.5 + ox, 0.5 + oy);

        float d = length(p - src);

        // Wave: cos(d * freq - t * speed), phase offset per source
        float wave = cos(d * freqMod - t * speedMod + fi * 1.047);
        wave = wave * 0.5 + 0.5; // remap to [0,1]

        // Per-source color: cycle through R, G, B
        vec3 srcColor;
        int ci = int(mod(fi, 3.0));
        if      (ci == 0) srcColor = vec3(1.0, 0.0,  0.0);  // signal red
        else if (ci == 1) srcColor = vec3(0.0, 1.0,  0.0);  // data green
        else              srcColor = vec3(0.0, 0.2,  1.0);  // electric blue

        // Falloff with distance (keep interference visible across whole screen)
        float falloff = 1.0 / (1.0 + d * d * 2.0);

        col += srcColor * wave * falloff * hdrPeak;
    }

    // fwidth-based AA on interference fringes
    float lumC = dot(col, vec3(0.299, 0.587, 0.114));
    float fw = fwidth(lumC);
    col = mix(col, col * smoothstep(0.0, fw * 4.0, lumC), 0.3);

    gl_FragColor = vec4(col, 1.0);
}
