/*{
  "CATEGORIES": [
    "Radiant"
  ],
  "DESCRIPTION": "Rotating crystal cubes with glass-like lighting, revealed through texture masking",
  "INPUTS": [
    {
      "NAME": "baseColor",
      "LABEL": "Color",
      "TYPE": "color",
      "DEFAULT": [1.0, 1.0, 1.0, 1.0]
    },
    {
      "NAME": "inputTex",
      "LABEL": "Texture",
      "TYPE": "image"
    },
    {
      "NAME": "transparentBg",
      "LABEL": "Transparent",
      "TYPE": "bool",
      "DEFAULT": true
    },
    {
      "NAME": "cubeCount",
      "LABEL": "Cubes",
      "TYPE": "float",
      "DEFAULT": 7.0,
      "MIN": 2.0,
      "MAX": 12.0
    },
    {
      "NAME": "roundness",
      "LABEL": "Roundness",
      "TYPE": "float",
      "DEFAULT": 0.06,
      "MIN": 0.0,
      "MAX": 0.2
    },
    {
      "NAME": "refractionStrength",
      "LABEL": "Refraction",
      "TYPE": "float",
      "DEFAULT": 0.08,
      "MIN": 0.0,
      "MAX": 0.3
    }
  ]
}*/

// 48 → 96: crystal-cube edges showed step staircasing on grazing rays.
#define MAX_STEPS 96
#define SURF_DIST 0.002
#define MAX_DIST 20.0

mat2 rot2(float a) { float c = cos(a), s = sin(a); return mat2(c, -s, s, c); }

float sdRoundBox(vec3 p, vec3 b, float r) {
  vec3 q = abs(p) - b;
  return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0) - r;
}

float scene(vec3 p) {
  float d = MAX_DIST;
  float bassPulse = smoothstep(0.0, 1.0, audioBass) * 0.15;
  float midRot = smoothstep(0.0, 1.0, audioMid) * 0.5 + 0.3;

  for (int i = 0; i < 12; i++) {
    if (float(i) >= cubeCount) break;
    float fi = float(i);
    float phase = fi * 1.2566; // TAU/5

    // Position: orbit around center
    vec3 pos = vec3(
      sin(TIME * 0.4 + phase) * (1.0 + fi * 0.2),
      cos(TIME * 0.3 + phase * 1.3) * 0.6,
      sin(TIME * 0.35 + phase * 0.7) * 0.8
    );

    vec3 q = p - pos;

    // Rotation per cube at different speeds
    float rotSpeed = midRot * (0.5 + fi * 0.2);
    q.xz *= rot2(TIME * rotSpeed + fi * 0.8);
    q.yz *= rot2(TIME * rotSpeed * 0.7 + fi * 1.1);

    // Size with bass pulse
    float size = 0.28 + fi * 0.04 + bassPulse;
    d = min(d, sdRoundBox(q, vec3(size), roundness));
  }
  return d;
}

vec3 calcNormal(vec3 p) {
  vec2 e = vec2(0.002, -0.002);
  return normalize(
    e.xyy * scene(p + e.xyy) +
    e.yyx * scene(p + e.yyx) +
    e.yxy * scene(p + e.yxy) +
    e.xxx * scene(p + e.xxx)
  );
}

void main() {
  vec2 uv = (gl_FragCoord.xy - RENDERSIZE.xy * 0.5) / min(RENDERSIZE.x, RENDERSIZE.y);

  // Camera with mouse control
  vec3 ro = vec3(0.0, 0.5, 4.5);
  vec3 target = vec3(0.0);
  if (mousePos.x > 0.0 || mousePos.y > 0.0) {
    target += vec3((mousePos - 0.5) * 2.0, 0.0) * 0.5;
  }
  vec3 fwd = normalize(target - ro);
  vec3 right = normalize(cross(fwd, vec3(0.0, 1.0, 0.0)));
  vec3 up = cross(right, fwd);
  vec3 rd = normalize(fwd * 1.8 + right * uv.x + up * uv.y);

  // Raymarch
  float totalDist = 0.0;
  bool hit = false;
  vec3 p;
  for (int i = 0; i < MAX_STEPS; i++) {
    p = ro + rd * totalDist;
    float d = scene(p);
    if (d < SURF_DIST) { hit = true; break; }
    if (totalDist > MAX_DIST) break;
    totalDist += d;
  }

  // Texture sampling
  vec2 texUV = gl_FragCoord.xy / RENDERSIZE.xy;
  vec4 texSample = texture2D(inputTex, texUV);
  bool hasTexture = texSample.a > 0.01;

  vec3 col = vec3(0.0);
  float alpha = 0.0;

  if (hit) {
    vec3 n = calcNormal(p);
    vec3 v = normalize(ro - p);
    vec3 L1 = normalize(vec3(2.0, 3.0, 2.0));
    vec3 L2 = normalize(vec3(-1.5, 1.0, -1.0));
    vec3 H1 = normalize(L1 + v);
    vec3 H2 = normalize(L2 + v);

    float diff1 = max(dot(n, L1), 0.0);
    float diff2 = max(dot(n, L2), 0.0) * 0.3;

    // Soft AA on facet specular edges via fwidth on the half-vector dot
    float ndh1 = max(dot(n, H1), 0.0);
    float ndh2 = max(dot(n, H2), 0.0);
    float aa1 = fwidth(ndh1) + 1e-4;
    float aa2 = fwidth(ndh2) + 1e-4;
    float spec1 = pow(smoothstep(-aa1, aa1, ndh1) * ndh1, 128.0);
    float spec2 = pow(smoothstep(-aa2, aa2, ndh2) * ndh2, 64.0);
    float fresnel = 0.6 + 0.4 * pow(1.0 - max(dot(n, v), 0.0), 4.0);

    if (hasTexture) {
      // Refract texture UV through crystal surface
      vec2 refractUV = texUV + n.xy * refractionStrength;
      vec3 texCol = texture2D(inputTex, refractUV).rgb;

      col = texCol * (diff1 + diff2) * 0.5;
      // HDR peak: facet glints push to ~2.6 linear for bloom
      col += texCol * (spec1 + spec2 * 0.5) * fresnel * 2.6;
      col += vec3(1.0) * spec1 * fresnel * 2.2;
      col += texCol * pow(1.0 - max(dot(n, v), 0.0), 3.0) * 0.25;
    } else {
      // Procedural "demo" texture — colorful gradient + slow noise so
      // the crystal's refraction has something rich to bend, instead of
      // showing all-white when no inputTex is bound.
      vec2 procUV = texUV + n.xy * refractionStrength;
      // Cosine-palette gradient
      vec3 grad = 0.5 + 0.5 * cos(6.28318 *
                  (procUV.x * 1.2 + procUV.y * 0.8 + TIME * 0.05) +
                  vec3(0.0, 2.094, 4.188));
      // Soft animated bands
      float band = sin(procUV.y * 14.0 + TIME * 0.3 + procUV.x * 4.0) * 0.5 + 0.5;
      vec3 procCol = mix(grad, vec3(0.85, 0.92, 1.0), 0.3) * (0.7 + 0.3 * band);

      col = procCol * (diff1 + diff2) * 0.5;
      // HDR peak: refractive highlights hit ~2.8 linear so they sparkle through bloom
      col += procCol * (spec1 + spec2 * 0.5) * fresnel * 2.8;
      col += vec3(1.0) * spec1 * fresnel * 2.6;
      col += procCol * pow(1.0 - max(dot(n, v), 0.0), 3.0) * 0.35;
    }

    // Audio non-gating: alive at audio=0, audio adds a subtle tint when present
    col += vec3(0.04, 0.025, 0.015);
    col += smoothstep(0.0, 1.0, audioLevel) * vec3(0.05, 0.03, 0.02);
    alpha = 1.0;
  }

  col *= baseColor.rgb;

  // No tonemap — preserve HDR for Phase Q v4 bloom

  if (!hit && transparentBg) {
    alpha = 0.0;
  }

  // Surprise: every ~36s the crystals all chord together — for ~1.5s
  // every cube fires a bright internal refraction at once, then dims.
  // Like a chandelier catching morning light all at the same moment.
  {
    float _ph = fract(TIME / 36.0);
    float _f  = smoothstep(0.0, 0.06, _ph) * smoothstep(0.28, 0.16, _ph);
    if (hit) {
      float _b = dot(col, vec3(0.299, 0.587, 0.114));
      col += vec3(0.7, 0.85, 1.0) * _f * (0.4 + _b * 0.6);
    }
  }

  gl_FragColor = vec4(col, alpha);
}
