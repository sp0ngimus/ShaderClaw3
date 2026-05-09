/*{
  "DESCRIPTION": "Neon Pillar Crypt — 64-step raymarched SDF grid of infinite cylindrical pillars in a void underground space. Neon-lit from above with crimson rim and gold diffuse. LINEAR HDR output.",
  "CREDIT": "ShaderClaw3 auto-improve 2026-05-09",
  "CATEGORIES": ["Generator", "3D", "Audio Reactive"],
  "INPUTS": [
    { "NAME": "pillarSpacing", "LABEL": "Pillar Spacing", "TYPE": "float", "DEFAULT": 1.8, "MIN": 0.8, "MAX": 4.0 },
    { "NAME": "pillarRadius",  "LABEL": "Pillar Radius",  "TYPE": "float", "DEFAULT": 0.28, "MIN": 0.1, "MAX": 0.6 },
    { "NAME": "pillarHeight",  "LABEL": "Pillar Height",  "TYPE": "float", "DEFAULT": 2.5, "MIN": 1.0, "MAX": 6.0 },
    { "NAME": "orbitSpeed",    "LABEL": "Orbit Speed",    "TYPE": "float", "DEFAULT": 0.07, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "hdrPeak",       "LABEL": "HDR Peak",       "TYPE": "float", "DEFAULT": 2.5, "MIN": 1.0, "MAX": 4.0 },
    { "NAME": "audioReact",    "LABEL": "Audio React",    "TYPE": "float", "DEFAULT": 0.8, "MIN": 0.0, "MAX": 2.0 }
  ]
}*/

// ── Utilities ─────────────────────────────────────────────────────────────────

float sdCylinder(vec3 p, float r) {
    return length(p.xz) - r;
}

float sdSphere(vec3 p, float r) {
    return length(p) - r;
}

// Map function: repeating pillars with caps
float sceneSDF(vec3 p) {
    // Infinite pillar grid via mod repetition
    vec3 q = p;
    q.x = mod(q.x + pillarSpacing * 0.5, pillarSpacing) - pillarSpacing * 0.5;
    q.z = mod(q.z + pillarSpacing * 0.5, pillarSpacing) - pillarSpacing * 0.5;

    // Infinite cylinder — clamp below floor (y < 0)
    float dCyl = sdCylinder(q, pillarRadius);
    float pillar = max(dCyl, -p.y);                       // cut below floor
    float pillarBody = max(dCyl, -(p.y - pillarHeight));  // cut above cap

    // Cap sphere at pillar top
    vec3 capPos = q - vec3(0.0, pillarHeight, 0.0);
    float dCap = sdSphere(capPos, pillarRadius * 1.2);

    float column = min(pillarBody, dCap);

    // Floor plane
    float floor_ = p.y;

    return min(column, floor_);
}

// Estimate surface normal via central differences
vec3 calcNormal(vec3 p) {
    float eps = 0.001;
    vec2 e = vec2(eps, 0.0);
    return normalize(vec3(
        sceneSDF(p + e.xyy) - sceneSDF(p - e.xyy),
        sceneSDF(p + e.yxy) - sceneSDF(p - e.yxy),
        sceneSDF(p + e.yyx) - sceneSDF(p - e.yyx)
    ));
}

// ── Lighting ──────────────────────────────────────────────────────────────────

// Palette: void black, crimson rim, electric gold diffuse, bone white spec
vec3 cryptLighting(vec3 pos, vec3 nor, vec3 rayDir, float bassAmp) {

    // Ceiling point light — downward illumination (overhead at y=8)
    vec3 lightPos  = vec3(0.0, 8.0, 0.0);
    vec3 lightDir  = normalize(lightPos - pos);
    float diff     = max(dot(nor, lightDir), 0.0);

    // Electric gold diffuse (HDR 2.0)
    vec3 goldColor = vec3(2.0, 1.4, 0.1) * hdrPeak * 0.8;
    vec3 diffuse   = goldColor * diff;

    // Specular blinn-phong — bone white 3.0
    vec3 halfDir   = normalize(lightDir - rayDir);
    float spec     = pow(max(dot(nor, halfDir), 0.0), 64.0);
    vec3 specColor = vec3(3.0, 2.8, 2.5) * spec * hdrPeak * 1.2;

    // Crimson rim light — from camera side (2.5 HDR)
    vec3 rimColor  = vec3(2.5, 0.05, 0.08) * hdrPeak * 0.6;
    float rim      = pow(max(1.0 - dot(nor, -rayDir), 0.0), 3.0);
    vec3 rimLight  = rimColor * rim;

    // Audio: bass modulates glow intensity — K ≤ 1.2
    float glow     = 1.0 + bassAmp * 1.2;

    // Ambient — deep void near-zero
    vec3 ambient   = vec3(0.0, 0.0, 0.02);

    return (ambient + diffuse * glow + specColor + rimLight * glow);
}

// ── Raymarcher ────────────────────────────────────────────────────────────────

void main() {
    vec2 uv = (gl_FragCoord.xy - 0.5 * RENDERSIZE.xy) / RENDERSIZE.y;

    // Camera orbit — slow patrol through the crypt
    float angle   = TIME * orbitSpeed;
    float camX    = cos(angle) * 0.5;
    float camZ    = sin(angle) * 0.5;
    vec3 ro       = vec3(camX, 1.2, camZ + TIME * 0.1);  // chest height, forward drift
    vec3 target   = ro + vec3(sin(angle + 0.3), -0.1, cos(angle + 0.3));
    vec3 forward  = normalize(target - ro);
    vec3 right    = normalize(cross(vec3(0.0, 1.0, 0.0), forward));
    vec3 up       = cross(forward, right);
    vec3 rd       = normalize(uv.x * right + uv.y * up + 1.5 * forward);

    // Audio bass input (ISF uniform)
    float bassIn  = clamp(audioBass * audioReact, 0.0, 1.0);

    // 64-step raymarch
    float t    = 0.001;
    float tMax = 30.0;
    bool  hit  = false;
    float d;

    for (int i = 0; i < 64; i++) {
        vec3 p = ro + rd * t;
        d = sceneSDF(p);
        if (d < 0.001) { hit = true; break; }
        if (t > tMax)  { break; }
        t += d;
    }

    vec3 col = vec3(0.0);  // void black default

    if (hit) {
        vec3 pos = ro + rd * t;
        vec3 nor = calcNormal(pos);

        // fwidth-based AA on silhouette edges
        float edgeAA = fwidth(d);
        float edge   = smoothstep(0.0, edgeAA * 2.0, abs(d));

        col = cryptLighting(pos, nor, rd, bassIn);

        // Distance fog to void black (far pillars fade to black)
        float fog = exp(-t * 0.04);
        col *= fog;
    }

    // Void floor glow (subtle ambient when ray hits the floor)
    if (hit) {
        vec3 pos = ro + rd * t;
        // If close to floor (y < 0.05), add subtle floor reflection
        if (pos.y < 0.05) {
            col += vec3(0.08, 0.0, 0.02) * hdrPeak * 0.15;
        }
    }

    gl_FragColor = vec4(col, 1.0);
}
