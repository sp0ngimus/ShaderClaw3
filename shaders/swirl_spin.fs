/*{
  "DESCRIPTION": "Swirl Spin — moving Lissajous spot painted into a slowly rotating/shrinking feedback field, by TekF",
  "CREDIT": "Ported from Shadertoy XsyGzz",
  "CATEGORIES": ["Generator", "Feedback"],
  "INPUTS": [
    { "NAME": "speed",       "LABEL": "Speed",        "TYPE": "float", "DEFAULT": 0.5,  "MIN": 0.0, "MAX": 3.0 },
    { "NAME": "spotSize",    "LABEL": "Spot Size",    "TYPE": "float", "DEFAULT": 1.0,  "MIN": 0.2, "MAX": 4.0 },
    { "NAME": "shrink",      "LABEL": "Shrink",       "TYPE": "float", "DEFAULT": 0.98, "MIN": 0.90, "MAX": 1.00 },
    { "NAME": "drift",       "LABEL": "Drift",        "TYPE": "float", "DEFAULT": 0.01, "MIN": 0.0, "MAX": 0.05 },
    { "NAME": "twist",       "LABEL": "Twist",        "TYPE": "float", "DEFAULT": 0.03, "MIN": 0.0, "MAX": 0.20 },
    { "NAME": "gamma",       "LABEL": "Gamma",        "TYPE": "float", "DEFAULT": 2.2,  "MIN": 1.0, "MAX": 3.0 }
  ],
  "PASSES": [
    { "TARGET": "swirlBuf", "PERSISTENT": true },
    {}
  ]
}*/

vec4 passSwirl(vec2 fragCoord) {
    vec2 res = RENDERSIZE;
    vec2 c2  = fragCoord - res * 0.5;
    vec2 sampleP = (fragCoord * shrink
                   + res * drift
                   + c2.yx * vec2(-twist, twist)) / res;
    vec4 prev = texture(swirlBuf, sampleP);

    float t = TIME * speed * (1.0 + audioLevel * 2.0);
    vec2 nfrag = fragCoord / res;
    vec4 col = vec4(sin(t * vec3(13.0, 11.0, 17.0) + nfrag.xyx * 3.0) * 0.5 + 0.5, 1.0);

    float audioScale = 1.0 + audioLevel * 1.5;
    vec2 spotDrift = vec2(cos(t * 0.5), sin(t * 0.7)) * 50.0 * audioScale;
    vec2 spotCenter = sin(vec2(11.0, 13.0) * t) * 60.0 * audioScale + spotDrift + res * 0.5;
    float idx = smoothstep(6.0 * spotSize, 20.0 * spotSize, length(fragCoord - spotCenter));

    // Spatially-varying background hue — raises palette entropy across audit frames.
    vec3 bgHue = 0.5 + 0.5 * sin(t * 0.3 + nfrag.x * 4.0 + nfrag.y * 2.5 + vec3(0.0, 2.094, 4.189));
    vec4 warm = mix(vec4(bgHue * 0.55, 1.0), prev, min(length(prev.rgb) * 5.0, 1.0));
    return mix(col, warm, idx);
}

vec4 passFinal(vec2 fragCoord) {
    vec2 uv = fragCoord / RENDERSIZE;
    return pow(texture(swirlBuf, uv), vec4(1.0 / gamma));
}

void main() {
    vec2 fragCoord = gl_FragCoord.xy;
    if (PASSINDEX == 0) FragColor = passSwirl(fragCoord);
    else                FragColor = passFinal(fragCoord);
}
