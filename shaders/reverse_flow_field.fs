/*{
    "DESCRIPTION": "Plasma Storm — raymarched 3D volumetric tornado vortex with electric lightning palette. Standalone HDR generator.",
    "CREDIT": "auto-improve",
    "ISFVSN": "2",
    "CATEGORIES": ["Generator", "3D", "Abstract"],
    "INPUTS": [
        {"NAME":"stormSpeed","TYPE":"float","DEFAULT":0.6,"MIN":0.0,"MAX":3.0,"LABEL":"Storm Speed"},
        {"NAME":"vortexRadius","TYPE":"float","DEFAULT":1.2,"MIN":0.2,"MAX":3.0,"LABEL":"Vortex Radius"},
        {"NAME":"density","TYPE":"float","DEFAULT":1.5,"MIN":0.1,"MAX":4.0,"LABEL":"Density"},
        {"NAME":"hdrPeak","TYPE":"float","DEFAULT":2.5,"MIN":1.0,"MAX":5.0,"LABEL":"HDR Peak"},
        {"NAME":"audioMod","TYPE":"float","DEFAULT":0.6,"MIN":0.0,"MAX":1.0,"LABEL":"Audio Mod"},
        {"NAME":"camFov","TYPE":"float","DEFAULT":0.8,"MIN":0.3,"MAX":1.5,"LABEL":"Field of View"}
    ]
}*/

float hash(vec3 p) {
    p = fract(p * vec3(443.8975, 397.2973, 491.1871));
    p += dot(p.zxy, p.yxz + 19.19);
    return fract(p.x * p.y * p.z);
}

float noise3(vec3 p) {
    vec3 i = floor(p); vec3 f = fract(p);
    f = f*f*(3.0-2.0*f);
    return mix(mix(mix(hash(i),hash(i+vec3(1,0,0)),f.x),
                   mix(hash(i+vec3(0,1,0)),hash(i+vec3(1,1,0)),f.x),f.y),
               mix(mix(hash(i+vec3(0,0,1)),hash(i+vec3(1,0,1)),f.x),
                   mix(hash(i+vec3(0,1,1)),hash(i+vec3(1,1,1)),f.x),f.y),f.z);
}

float fbm3(vec3 p) {
    float v = 0.0; float a = 0.5;
    for (int i = 0; i < 4; i++) {
        v += a * noise3(p);
        p = p * 2.1 + vec3(1.7, 9.2, 3.4);
        a *= 0.5;
    }
    return v;
}

float vortexDensity(vec3 p, float t) {
    float ang = atan(p.z, p.x) + t * stormSpeed;
    float r = length(p.xz);
    float height = clamp(p.y / 6.0, 0.0, 1.0);
    float vortR = vortexRadius * (0.1 + height);
    float radial = exp(-max(0.0, r - vortR) * 3.0);
    vec3 wp = p + vec3(sin(ang * 2.0 + t) * 0.3, t * 0.4, cos(ang * 2.0 + t) * 0.3);
    float nz = fbm3(wp * 1.2);
    return radial * (nz * 2.0 - 0.3) * density;
}

void main() {
    vec2 uv = isf_FragNormCoord * 2.0 - 1.0;
    uv.x *= RENDERSIZE.x / RENDERSIZE.y;
    float t = TIME;
    float audio = 1.0 + (audioLevel + audioBass * 0.7) * audioMod;

    vec3 ro = vec3(sin(t * 0.07) * 4.0, 0.5, cos(t * 0.07) * 4.0);
    vec3 ta = vec3(0.0, 3.0, 0.0);
    vec3 fwd = normalize(ta - ro);
    vec3 right = normalize(cross(fwd, vec3(0.0, 1.0, 0.0)));
    vec3 up = cross(right, fwd);
    vec3 rd = normalize(fwd + uv.x * right * camFov + uv.y * up * camFov);

    vec3 col = vec3(0.0);
    float transmit = 1.0;
    float stepSize = 0.15;

    for (int i = 0; i < 64; i++) {
        float rayT = float(i) * stepSize;
        vec3 p = ro + rd * rayT;
        if (rayT > 12.0) break;

        float d = vortexDensity(p, t) * audio;
        if (d > 0.0) {
            float h = clamp(p.y / 6.0, 0.0, 1.0);
            vec3 purple = vec3(0.6, 0.0, 2.0);
            vec3 blue = vec3(0.0, 0.4, 2.0);
            vec3 cyan = vec3(0.0, 1.5, 2.5);
            vec3 white = vec3(hdrPeak * audio, hdrPeak * audio * 0.95, hdrPeak * audio * 1.1);
            vec3 c = mix(mix(purple, blue, h), mix(cyan, white, h * h), h);
            float absorb = exp(-d * stepSize * 2.0);
            col += transmit * c * d * stepSize * hdrPeak;
            transmit *= absorb;
            if (transmit < 0.01) break;
        }
    }

    gl_FragColor = vec4(col, 1.0);
}
