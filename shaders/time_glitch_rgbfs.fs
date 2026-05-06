/*{
    "DESCRIPTION": "Cyber Floor — perspective 3D neon floor grid with glitch displacement. Tron-style racing circuit. Standalone HDR generator.",
    "CREDIT": "auto-improve",
    "ISFVSN": "2",
    "CATEGORIES": ["Generator", "3D", "Abstract"],
    "INPUTS": [
        {"NAME":"driveSpeed","TYPE":"float","DEFAULT":1.5,"MIN":0.0,"MAX":5.0,"LABEL":"Drive Speed"},
        {"NAME":"gridScale","TYPE":"float","DEFAULT":1.0,"MIN":0.1,"MAX":3.0,"LABEL":"Grid Scale"},
        {"NAME":"glitchRate","TYPE":"float","DEFAULT":0.5,"MIN":0.0,"MAX":2.0,"LABEL":"Glitch Rate"},
        {"NAME":"gridPeak","TYPE":"float","DEFAULT":2.5,"MIN":1.0,"MAX":5.0,"LABEL":"HDR Peak"},
        {"NAME":"audioMod","TYPE":"float","DEFAULT":0.5,"MIN":0.0,"MAX":1.0,"LABEL":"Audio Mod"},
        {"NAME":"camHeight","TYPE":"float","DEFAULT":0.35,"MIN":0.05,"MAX":1.0,"LABEL":"Cam Height"}
    ]
}*/

float hash1(float n) { return fract(sin(n * 127.1 + 311.7) * 43758.5453); }
float hash2(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }

void main() {
    vec2 uv = isf_FragNormCoord * 2.0 - 1.0;
    uv.x *= RENDERSIZE.x / RENDERSIZE.y;
    float t = TIME;
    float audio = 1.0 + (audioLevel + audioBass * 0.8) * audioMod;

    vec3 ro = vec3(0.0, camHeight, 0.0);
    vec3 rd = normalize(vec3(uv.x * 0.8, uv.y - 0.15, 1.0));

    vec3 col = vec3(0.0);

    // Ray-plane intersection with floor (y=0)
    if (rd.y < -0.001) {
        float rayT = (0.0 - ro.y) / rd.y;
        vec3 hit = ro + rd * rayT;

        // Forward camera motion
        float camZ = t * driveSpeed;
        vec2 gp = vec2(hit.x, hit.z + camZ) / gridScale;

        // Glitch: displace entire horizontal row
        float rowID = floor(gp.y);
        float glitchSeed = hash1(rowID + floor(t * glitchRate * 4.0) * 17.3);
        float glitchActive = step(0.85, glitchSeed);
        gp.x += glitchActive * (hash1(rowID * 7.3 + t) - 0.5) * 4.0;

        vec2 gf = fract(gp);
        vec2 gfAA = fwidth(gf);

        // Grid lines
        float lineX = smoothstep(0.05 + gfAA.x, 0.0, gf.x) + smoothstep(0.05 + gfAA.x, 0.0, 1.0 - gf.x);
        float lineY = smoothstep(0.05 + gfAA.y, 0.0, gf.y) + smoothstep(0.05 + gfAA.y, 0.0, 1.0 - gf.y);
        float grid = clamp(lineX + lineY, 0.0, 1.0);

        // Intersection glow (golden)
        float corner = lineX * lineY;

        // Distance fog
        float fog = exp(-rayT * 0.15);

        vec3 neonGreen = vec3(0.0, gridPeak * audio, 0.0);
        vec3 crimsonGlitch = vec3(gridPeak * 0.9 * audio, 0.0, 0.0);
        vec3 goldCorner = vec3(gridPeak * 0.8, gridPeak * 0.55, 0.0) * audio;

        vec3 gridCol = mix(neonGreen, crimsonGlitch, glitchActive);
        col = gridCol * grid * fog + goldCorner * corner * fog * 2.0;

    } else {
        // Sky: void black with faint horizon glow
        float horizonDist = abs(uv.y - 0.15 + 0.15);
        col = vec3(0.0, gridPeak * 0.02, 0.0) * exp(-horizonDist * 10.0);
    }

    gl_FragColor = vec4(col, 1.0);
}
