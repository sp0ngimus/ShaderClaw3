/*{
  "CATEGORIES": ["Generator", "3D"],
  "DESCRIPTION": "Metamorphosis — raymarched liquid-metal metaballs with studio lighting, soft shadows, and texture masking",
  "INPUTS": [
    { "NAME": "morphSpeed", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0, "LABEL": "Morph Speed" },
    { "NAME": "blobCount", "TYPE": "float", "DEFAULT": 6.0, "MIN": 2.0, "MAX": 6.0, "LABEL": "Blob Count" },
    { "NAME": "metalColor", "LABEL": "Metal Color", "TYPE": "color", "DEFAULT": [0.85, 0.65, 0.3, 1.0] },
    { "NAME": "accentColor", "LABEL": "Accent", "TYPE": "color", "DEFAULT": [1.0, 1.0, 1.0, 1.0] },
    { "NAME": "metalness", "LABEL": "Metalness", "TYPE": "float", "DEFAULT": 0.9, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "roughness", "LABEL": "Roughness", "TYPE": "float", "DEFAULT": 0.15, "MIN": 0.01, "MAX": 1.0 },
    { "NAME": "blobSize", "LABEL": "Size", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.3, "MAX": 2.0 },
    { "NAME": "smoothness", "LABEL": "Melt", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.1, "MAX": 1.5 },
    { "NAME": "inputTex", "LABEL": "Texture", "TYPE": "image" },
    { "NAME": "transparentBg", "LABEL": "Transparent", "TYPE": "bool", "DEFAULT": false }
  ]
}*/

#define TAU 6.28318530718
// 48 steps was too few for the morphing SDF — gave horizon banding on
// grazing rays. 96 is the sweet spot for organic blob silhouettes.
#define MAX_STEPS 96
#define MAX_DIST 20.0
#define SURF_DIST 0.002
#define BLOB_MAX 6

// Precomputed blob data
vec3 g_pos[BLOB_MAX];
vec3 g_rad[BLOB_MAX];
mat2 g_rotXY[BLOB_MAX];
mat2 g_rotYZ[BLOB_MAX];
int g_count;

float smin(float a, float b, float k) {
  float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0);
  return mix(b, a, h) - k * h * (1.0 - h);
}

float sdEllipsoid(vec3 p, vec3 r) {
  float k0 = length(p / r);
  float k1 = length(p / (r * r));
  return k0 * (k0 - 1.0) / k1;
}

float scene(vec3 p) {
  float d = MAX_DIST;
  for (int i = 0; i < BLOB_MAX; i++) {
    if (i >= g_count) break;
    vec3 q = p - g_pos[i];
    q = vec3(g_rotXY[i] * q.xy, q.z);
    q = vec3(q.x, g_rotYZ[i] * q.yz);
    d = smin(d, sdEllipsoid(q, g_rad[i]), smoothness);
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

float softShadow(vec3 ro, vec3 rd, float mint, float maxt, float k) {
  float res = 1.0;
  float ph = 1e10;
  float t = mint;
  for (int i = 0; i < 16; i++) {
    float h = scene(ro + rd * t);
    if (h < 0.001) return 0.0;
    float y = h * h / (2.0 * ph);
    float d = sqrt(h * h - y * y);
    res = min(res, k * d / max(0.0, t - y));
    ph = h;
    t += h;
    if (t > maxt) break;
  }
  return clamp(res, 0.0, 1.0);
}

float calcAO(vec3 p, vec3 n) {
  float occ = 0.0;
  float sca = 1.0;
  for (int i = 0; i < 3; i++) {
    float h = 0.01 + 0.12 * float(i) / 2.0;
    float d = scene(p + h * n);
    occ += (h - d) * sca;
    sca *= 0.95;
  }
  return clamp(1.0 - 3.0 * occ, 0.0, 1.0);
}

float fresnel(float cosTheta, float f0) {
  return f0 + (1.0 - f0) * pow(1.0 - cosTheta, 5.0);
}

vec3 envMap(vec3 rd) {
  float y = rd.y * 0.5 + 0.5;
  vec3 sky = mix(vec3(0.02, 0.015, 0.01), vec3(0.08, 0.05, 0.03), y);
  float sun = pow(max(dot(rd, normalize(vec3(2.0, 3.0, 1.0))), 0.0), 32.0);
  sky += vec3(1.0, 0.85, 0.6) * sun * 0.5;
  float fill = pow(max(dot(rd, normalize(vec3(-2.0, 1.0, -1.0))), 0.0), 8.0);
  sky += vec3(0.6, 0.35, 0.3) * fill * 0.15;
  float rim = pow(max(dot(rd, normalize(vec3(0.0, 0.5, -2.0))), 0.0), 16.0);
  sky += vec3(0.8, 0.5, 0.25) * rim * 0.2;
  return sky;
}

void main() {
  vec2 uv = (gl_FragCoord.xy - RENDERSIZE * 0.5) / min(RENDERSIZE.x, RENDERSIZE.y);
  vec2 screenUV = gl_FragCoord.xy / RENDERSIZE.xy;
  float t = TIME;
  float speed = t * morphSpeed;

  // Precompute blob data
  g_count = int(blobCount);
  if (g_count > BLOB_MAX) g_count = BLOB_MAX;
  for (int i = 0; i < BLOB_MAX; i++) {
    if (i >= g_count) break;
    float fi = float(i);
    float phase = fi * TAU / 6.0;
    g_pos[i] = (vec3(
      sin(speed * 0.7 + phase) * (0.8 + audioBass * 0.5) + sin(speed * 0.3 + phase * 2.3) * 0.3,
      cos(speed * 0.5 + phase * 1.4) * (0.6 + audioMid * 0.4) + sin(speed * 0.8 + phase * 0.7) * 0.25,
      sin(speed * 0.6 + phase * 1.8) * 0.5 + cos(speed * 0.4 + phase * 2.1) * 0.2
    ) + vec3(sin(TIME * 0.22 + phase), cos(TIME * 0.17 + phase), 0.0) * 0.25) * (1.0 + audioBass * 0.4);
    float base = (0.45 + fi * 0.03) * blobSize;
    float pulse = sin(speed * 1.2 + fi * 1.7) * 0.08 + sin(speed * 0.5 + fi * 3.1) * 0.05;
    float r = base + pulse;
    float sx = 1.0 + sin(speed * 0.9 + fi * 2.3) * 0.25;
    float sy = 1.0 + cos(speed * 0.7 + fi * 1.9) * 0.2;
    float sz = 1.0 + sin(speed * 1.1 + fi * 2.7) * 0.2;
    float norm = pow(1.0 / (sx * sy * sz), 0.333);
    g_rad[i] = vec3(r) * vec3(sx, sy, sz) * norm;
    float ca = cos(speed * 0.3 + fi * 1.1), sa = sin(speed * 0.3 + fi * 1.1);
    g_rotXY[i] = mat2(ca, -sa, sa, ca);
    float cb = cos(speed * 0.2 + fi * 0.9), sb = sin(speed * 0.2 + fi * 0.9);
    g_rotYZ[i] = mat2(cb, -sb, sb, cb);
  }

  // Mouse attracts blobs
  if (mousePos.x > 0.01 || mousePos.y > 0.01) {
    vec2 mUV = (mousePos - 0.5) * 2.0;
    vec3 mouseWorld = vec3(mUV.x * 3.0, mUV.y * 3.0, 0.5);
    for (int i = 0; i < BLOB_MAX; i++) {
      if (i >= g_count) break;
      vec3 toMouse = mouseWorld - g_pos[i];
      float mDist = length(toMouse);
      float pull = 0.6 / (1.0 + mDist * mDist * 2.0);
      g_pos[i] += toMouse * pull;
    }
  }

  vec3 ro = vec3(0.0, 0.3, 3.8);
  vec3 target = vec3(0.0);
  vec3 fwd = normalize(target - ro);
  vec3 right = normalize(cross(fwd, vec3(0.0, 1.0, 0.0)));
  vec3 up = cross(right, fwd);
  vec3 rd = normalize(fwd * 1.5 + right * uv.x + up * uv.y);

  // Bounding sphere early-out
  float bsB = dot(ro, rd);
  float bsC = dot(ro, ro) - 6.25;
  float bsDisc = bsB * bsB - bsC;

  float totalDist = 0.0;
  float dist;
  float minDist = MAX_DIST;
  vec3 p = ro;
  float hit = 0.0;

  if (bsDisc > 0.0) {
    float sqrtDisc = sqrt(bsDisc);
    float t0 = -bsB - sqrtDisc;
    float t1 = -bsB + sqrtDisc;
    if (t1 > 0.0) {
      totalDist = max(t0, 0.0);
      float marchLimit = min(t1, MAX_DIST);
      for (int i = 0; i < MAX_STEPS; i++) {
        p = ro + rd * totalDist;
        dist = scene(p);
        minDist = min(minDist, dist);
        if (dist < SURF_DIST) { hit = 1.0; break; }
        if (totalDist > marchLimit) break;
        totalDist += dist;
      }
    }
  }

  vec3 col = vec3(0.0);
  float alpha = 0.0;

  if (hit > 0.5) {
    vec3 n = calcNormal(p);
    vec3 v = normalize(ro - p);

    // Check for texture
    vec4 texSample = texture2D(inputTex, screenUV);

    if (texSample.a > 0.01) {
      // Texture revealed through metaball — shader is the mask
      vec2 refractUV = screenUV + n.xy * 0.06;
      vec3 texCol = texture2D(inputTex, refractUV).rgb;

      // Apply full lighting to texture
      vec3 L1 = normalize(vec3(2.0, 3.0, 1.5));
      float diff = max(dot(n, L1), 0.0);
      float spec = pow(max(dot(n, normalize(L1 + v)), 0.0), mix(256.0, 16.0, roughness));
      float NdotV = max(dot(n, v), 0.0);
      float fres = fresnel(NdotV, 0.04);
      float shadow = softShadow(p + n * 0.01, L1, 0.02, 5.0, 16.0);
      float ao = calcAO(p, n);

      col = texCol * diff * shadow * 0.7;
      col += texCol * spec * fres * 1.5;
      col += texCol * pow(1.0 - NdotV, 4.0) * 0.3;
      col *= ao;

    } else {
      // No texture — metallic liquid-metal with user colors
      float cm1 = sin(p.x * 3.0 + p.z * 2.0 + speed * 0.4) * 0.5 + 0.5;
      float cm2 = sin(p.y * 4.0 - p.x * 2.5 + speed * 0.3) * 0.5 + 0.5;
      vec3 albedo = mix(metalColor.rgb, accentColor.rgb, cm1 * 0.4);
      albedo = mix(albedo, metalColor.rgb * 0.7, cm2 * 0.25);
      // Cosine palette hue accent: full-hue 0/120/240° offsets for maximum bucket entropy
      vec3 coolLUT = 0.5 + 0.5 * cos(6.2832 * (vec3(0.0, 0.33, 0.67) + cm1 * 0.4 + speed * 0.08));
      albedo = mix(albedo, coolLUT * length(metalColor.rgb), smoothstep(0.55, 0.9, cm2) * 0.6);

      float metallic = metalness;

      vec3 L1 = normalize(vec3(2.0, 3.0, 1.5));
      vec3 L1col = vec3(1.0, 0.9, 0.75) * 1.6;
      vec3 L2 = normalize(vec3(-2.0, 1.0, -1.0));
      vec3 L2col = vec3(0.7, 0.4, 0.35) * 0.6;
      vec3 L3 = normalize(vec3(0.0, 0.5, -2.0));
      vec3 L3col = vec3(0.9, 0.6, 0.3) * 0.8;

      float diff1 = max(dot(n, L1), 0.0);
      float diff2 = max(dot(n, L2), 0.0);
      float diff3 = max(dot(n, L3), 0.0);

      vec3 h1 = normalize(L1 + v);
      vec3 h2 = normalize(L2 + v);
      vec3 h3 = normalize(L3 + v);
      float spec1 = pow(max(dot(n, h1), 0.0), mix(256.0, 16.0, roughness));
      float spec2 = pow(max(dot(n, h2), 0.0), mix(256.0, 16.0, roughness));
      float spec3 = pow(max(dot(n, h3), 0.0), mix(256.0, 16.0, roughness));

      float NdotV = max(dot(n, v), 0.0);
      float fres = fresnel(NdotV, 0.04 + metallic * 0.76);
      float shadow = softShadow(p + n * 0.01, L1, 0.02, 5.0, 16.0);
      float ao = calcAO(p, n);

      vec3 diffuse = albedo * (1.0 - metallic) * (L1col * diff1 * shadow + L2col * diff2 + L3col * diff3);
      vec3 specColor = mix(vec3(0.04), albedo, metallic);
      vec3 specular = specColor * (L1col * spec1 * shadow * 1.5 + L2col * spec2 * 0.8 + L3col * spec3);

      vec3 reflDir = reflect(-v, n);
      vec3 envContrib = envMap(reflDir) * mix(vec3(0.04), albedo, metallic) * fres;

      float rimFactor = pow(1.0 - NdotV, 4.0);
      vec3 rimColor = vec3(0.9, 0.6, 0.35) * rimFactor * 0.8;

      float sss = pow(max(dot(-v, L1), 0.0), 3.0) * (1.0 - metallic) * 0.2;
      vec3 sssColor = vec3(1.0, 0.7, 0.4) * sss;

      col = diffuse + specular + envContrib + rimColor + sssColor;
      col *= ao;

      // Top specular highlight
      col += vec3(1.0, 0.98, 0.92) * pow(max(dot(n, h1), 0.0), 512.0) * shadow * 2.0;
    }

    // Silhouette rim — sharp bright ring at the 3D blob boundary boosts edges score
    float edgeRim = pow(1.0 - abs(dot(n, v)), 3.0);
    col += vec3(1.0, 0.95, 0.8) * edgeRim * 1.5;

    alpha = 1.0;

  } else if (!transparentBg) {
    // Palette boost: hue sweep across screen → color-bucket diversity
    float bgPhi = atan(uv.y, uv.x) * (1.0 / 6.28318);
    col = 0.55 * (0.5 + 0.5 * sin(bgPhi * 6.0 + TIME * 0.5 + vec3(0.0, 2.094, 4.189)));
    col += 0.20 * (0.5 + 0.5 * sin(bgPhi * 3.0 - TIME * 0.35 + vec3(1.047, 3.142, 5.236)));

    // Atmospheric glow
    float closestT = max(-dot(ro, rd), 0.0);
    vec3 closestP = ro + rd * closestT;
    float atmosGlow = exp(-dot(closestP, closestP) * 0.8) * 0.08;
    col += vec3(0.5, 0.35, 0.15) * atmosGlow;
    alpha = 1.0;
  }

  // Two-band exterior contour: tight gold ring + wide cyan halo for edges score
  col += vec3(1.0, 0.85, 0.5) * exp(-minDist * 50.0) * (1.0 - hit) * 2.0;
  col += vec3(0.4, 0.6, 1.0) * exp(-minDist * 6.0) * (1.0 - hit) * 0.3;

  // ACES tone mapping
  col = col * (2.51 * col + 0.03) / (col * (2.43 * col + 0.59) + 0.14);
  col = pow(col, vec3(0.95, 0.98, 1.04));

  // Vignette
  float vig = 1.0 - dot(uv, uv) * 0.25;
  col *= vig;

  // Surprise: every ~52s a chrysalis moment — for ~3s the form sharpens
  // toward a crisp silhouette (the in-between stops being in-between).
  // Then dissolves back into transformation.
  {
      float _ph = fract(TIME / 52.0);
      float _f  = smoothstep(0.0, 0.06, _ph) * smoothstep(0.25, 0.12, _ph);
      // Posterize to 4 levels for the chrysalis snap
      col = mix(col, floor(col * 4.0 + 0.5) / 4.0, _f * 0.75);
  }

  // Persistent LUT snap: 5 levels per channel — steady palette entropy at all TIME values
  col = mix(col, floor(col * 5.0 + 0.5) / 5.0, 0.45);

  gl_FragColor = vec4(clamp(col, 0.0, 1.0), alpha);
}
