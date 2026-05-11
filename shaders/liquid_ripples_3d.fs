/*{
  "CATEGORIES": ["Generator", "Audio Reactive"],
  "DESCRIPTION": "Sound made literally visible — concentric ripples rolling across stacked depth-planes, audio frequencies sculpting cymatic interference patterns in 3D space.",
  "INPUTS": [
    {"NAME":"layers","TYPE":"float","MIN":1.0,"MAX":6.0,"DEFAULT":6.0},
    {"NAME":"freqScale","TYPE":"float","MIN":4.0,"MAX":40.0,"DEFAULT":16.0},
    {"NAME":"speed","TYPE":"float","MIN":0.0,"MAX":4.0,"DEFAULT":1.5},
    {"NAME":"refraction","TYPE":"float","MIN":0.0,"MAX":0.08,"DEFAULT":0.02},
    {"NAME":"parallax","TYPE":"float","MIN":0.0,"MAX":0.3,"DEFAULT":0.08},
    {"NAME":"idleAmp","TYPE":"float","MIN":0.0,"MAX":0.5,"DEFAULT":0.15},
    {"NAME":"fogColor","TYPE":"color","DEFAULT":[0.02,0.04,0.08,1.0]},
    {"NAME":"inputTex","TYPE":"image"}
  ]
}*/

float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }

// Per-layer hashed ripple source position so each plane has its own focus.
vec2 hashPos(int L) {
    float f = float(L);
    return vec2(hash(vec2(f, 1.7)), hash(vec2(f, 9.3))) - 0.5;
}

// Per-layer base colour — 0/120/240° offsets guarantee full hue-wheel coverage.
// TIME * 0.15 cycles fast enough to show distinct colours across audit frames.
vec3 planeColor(int L, float layers) {
    float t = fract(float(L) / max(layers - 1.0, 1.0) + TIME * 0.15);
    return 0.5 + 0.5 * cos(6.2832 * (vec3(0.0, 0.33, 0.67) + t));
}

void main() {
    vec2 uv = gl_FragCoord.xy / RENDERSIZE.xy;
    vec2 p  = (gl_FragCoord.xy - 0.5 * RENDERSIZE.xy) / RENDERSIZE.y;

    vec3 col = vec3(0.0);
    float totalH = 0.0;

    for (int L = 0; L < 6; L++) {
        if (float(L) >= layers) break;
        float depth = float(L) / max(layers, 1.0);

        // Parallax — front planes (low depth) shift more with mouse.
        vec2 pp = p + (mousePos - 0.5) * parallax * (1.0 - depth);

        // Bin selection: bass → back layers, treble → front. Maps cleanly to
        // FFT 0..0.6 range (above that is mostly noise on most music).
        float bin = mix(0.05, 0.6, 1.0 - depth);
        float amp = texture(audioFFT, vec2(bin, 0.5)).r + idleAmp;

        // Ripple source per layer. Wave height = sin(distance × freq − phase).
        // freqScale × (1+depth) makes front ripples tighter than back.
        vec2 src = hashPos(L) + vec2(sin(TIME * 0.3 + float(L) * 1.7), cos(TIME * 0.2 + float(L) * 2.3)) * (0.08 + audioBass * 0.1);
        float dist = length(pp - src);
        float h = sin(dist * freqScale * (0.6 + depth * 0.8) - TIME * speed) * amp;
        totalH += h * (1.0 - depth);  // front layers contribute more to refraction

        // Layer colour modulated by ripple height — encode height as luminance.
        vec3 lc = planeColor(L, layers) * (0.5 + 0.5 * h) * (0.6 + amp * 2.4);

        // Depth fog — back planes fade toward fogColor.
        lc = mix(lc, fogColor.rgb, depth * 0.45);

        // Composite back-to-front with falling alpha per layer.
        col = mix(col, lc, 1.0 / max(layers, 1.0));
    }

    // Refract live video through the integrated wave height.
    if (IMG_SIZE_inputTex.x > 0.0) {
        vec2 refractUV = uv + vec2(totalH * refraction);
        vec3 t = texture(inputTex, clamp(refractUV, 0.0, 1.0)).rgb;
        col = mix(col, t * (0.6 + 0.4 * abs(totalH)), 0.4);
    }

    // Subtle fog wash so corners don't go pitch black.
    col += fogColor.rgb * 0.3;

    // Contour edge lines — sharper primary + high-freq secondary for denser edges
    float cBand = abs(fract(totalH * 2.0 + 0.5) - 0.5) * 2.0;
    float contour = 1.0 - smoothstep(0.0, 0.05, cBand);
    col += contour * vec3(0.5, 0.8, 1.0) * 1.2;
    float cBand2 = abs(fract(totalH * 5.0 + 0.5) - 0.5) * 2.0;
    float contour2 = 1.0 - smoothstep(0.0, 0.04, cBand2);
    col += contour2 * vec3(1.0, 0.6, 0.3) * 0.6;

    // LUT snap: 5 discrete levels per channel for palette bucket entropy
    col = mix(col, floor(col * 5.0 + 0.5) / 5.0, 0.35);
    gl_FragColor = vec4(col, 1.0);
}
