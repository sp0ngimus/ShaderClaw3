/*{
    "DESCRIPTION": "Tokyo Rain — cyberpunk city night scene with rain and neon reflections in wet pavement. Standalone HDR generator.",
    "CREDIT": "auto-improve",
    "ISFVSN": "2",
    "CATEGORIES": ["Generator", "Abstract"],
    "INPUTS": [
        {"NAME":"rainSpeed","TYPE":"float","DEFAULT":1.5,"MIN":0.0,"MAX":4.0,"LABEL":"Rain Speed"},
        {"NAME":"rainDensity","TYPE":"float","DEFAULT":0.6,"MIN":0.1,"MAX":1.0,"LABEL":"Rain Density"},
        {"NAME":"neonPeak","TYPE":"float","DEFAULT":2.5,"MIN":1.0,"MAX":5.0,"LABEL":"HDR Peak"},
        {"NAME":"audioMod","TYPE":"float","DEFAULT":0.5,"MIN":0.0,"MAX":1.0,"LABEL":"Audio Mod"},
        {"NAME":"puddleBlur","TYPE":"float","DEFAULT":0.4,"MIN":0.0,"MAX":1.0,"LABEL":"Puddle Blur"}
    ]
}*/

float hash1(float n) { return fract(sin(n * 127.1) * 43758.5453); }
float hash2(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }

float rain(vec2 uv, float t) {
    float col = 0.0;
    float numStreaks = rainDensity * 80.0;
    for (float i = 0.0; i < 80.0; i++) {
        if (i >= numStreaks) break;
        float seed = hash1(i * 3.7);
        float x = (seed - 0.5) * 2.5;
        float speed = 0.8 + seed * 1.5;
        float length = 0.02 + seed * 0.06;
        float y = mod(uv.y + t * speed * rainSpeed + seed * 10.0, 2.5) - 1.25;
        float dx = uv.x - x + uv.y * 0.05; // slight angle
        float streak = smoothstep(0.004, 0.001, abs(dx)) * smoothstep(length, 0.0, abs(y));
        col += streak * (0.5 + seed * 0.5);
    }
    return clamp(col, 0.0, 1.0);
}

void main() {
    vec2 uv = isf_FragNormCoord * 2.0 - 1.0;
    float asp = RENDERSIZE.x / RENDERSIZE.y;
    uv.x *= asp;
    float t = TIME;
    float audio = 1.0 + (audioLevel + audioBass * 0.7) * audioMod;

    float horizon = -0.15;
    vec3 col = vec3(0.0);

    if (uv.y > horizon) {
        // Sky / building zone
        col = vec3(0.0, 0.002, 0.008); // near void night sky

        // Building silhouettes
        for (float i = 0.0; i < 8.0; i++) {
            float fi = i / 8.0;
            float bx = (fi - 0.5) * asp * 2.0;
            float bw = 0.12 + hash1(i * 7.3) * 0.15;
            float bh = 0.3 + hash1(i * 13.7) * 0.7;
            float btop = horizon + bh;

            if (abs(uv.x - bx) < bw && uv.y < btop) {
                col = vec3(0.005, 0.005, 0.01); // near-black building
                // Windows
                vec2 wp = vec2((uv.x - bx + bw) / (bw * 2.0), (uv.y - horizon) / bh);
                vec2 wg = fract(wp * vec2(5.0, 10.0));
                float win = step(0.15, wg.x) * step(0.15, wg.y) * step(wg.x, 0.85) * step(wg.y, 0.85);
                float wlit = step(0.5, hash1(fi * 31.7 + floor(wp.y * 10.0) * 17.3 + floor(t * 0.2)));
                col += vec3(1.8, 0.9, 0.1) * win * wlit; // warm gold windows
            }
        }

        // Neon signs
        float ns1 = smoothstep(0.02, 0.0, abs(length(uv - vec2(-0.5 * asp, 0.1)) - 0.06));
        float ns2 = smoothstep(0.015, 0.0, abs(uv.y - 0.25)) * step(abs(uv.x - 0.4 * asp), 0.08);
        float ns3 = smoothstep(0.02, 0.0, abs(length(uv - vec2(0.3 * asp, 0.4)) - 0.05));
        float flicker = 0.85 + 0.15 * sin(t * 13.7 + 2.1) * step(0.8, hash1(t * 0.1));
        col += vec3(0.0, 1.2, neonPeak) * ns1 * flicker * audio;
        col += vec3(neonPeak, 0.0, 0.8) * ns2 * audio;
        col += vec3(0.0, 1.0, neonPeak * 0.9) * ns3 * flicker * audio;

    } else {
        // Puddle / wet pavement zone
        float depth = clamp((-uv.y + horizon) / 0.5, 0.0, 1.0);
        col = vec3(0.005, 0.005, 0.01); // dark pavement

        // Reflection of neon signs (wavy, upside-down)
        vec2 refUV = vec2(uv.x, horizon + (horizon - uv.y));
        refUV += vec2(sin(uv.y * 20.0 + t * 3.0) * puddleBlur * 0.02, 0.0);

        float rns1 = smoothstep(0.02, 0.0, abs(length(refUV - vec2(-0.5 * asp, 0.1)) - 0.06));
        float rns2 = smoothstep(0.015, 0.0, abs(refUV.y - 0.25)) * step(abs(refUV.x - 0.4 * asp), 0.08);
        float reflStrength = exp(-depth * 5.0);
        col += vec3(0.0, 0.8, neonPeak * 0.7) * rns1 * reflStrength * audio;
        col += vec3(neonPeak * 0.7, 0.0, 0.5) * rns2 * reflStrength * audio;

        // Puddle shimmer from windows
        float shimmer = sin(uv.x * 15.0 + t * 4.0) * 0.5 + 0.5;
        col += vec3(0.3, 0.15, 0.0) * shimmer * reflStrength * 0.3;
    }

    // Rain overlay
    float r = rain(uv, t);
    vec3 rainCol = vec3(0.5, 0.7, 1.0) * neonPeak * 0.3 * audio;
    col += rainCol * r;

    gl_FragColor = vec4(col, 1.0);
}
