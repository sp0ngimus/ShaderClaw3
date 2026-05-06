/*{
  "DESCRIPTION": "Body Fluid — liquid metal that clings to your skeleton. Fluid is attracted toward body bones, forming a moving liquid figure. Optimized for pose tracking.",
  "CREDIT": "ShaderClaw — UV-advection fluid (Paketa12) + liquid metal visuals (flockaroo) + skeleton attraction",
  "CATEGORIES": ["VFX", "Simulation"],
  "INPUTS": [
    { "NAME": "inputTex", "LABEL": "Image/Video", "TYPE": "image" },
    { "NAME": "fluidSpeed", "LABEL": "Fluid Speed", "TYPE": "float", "DEFAULT": 4.0, "MIN": 0.5, "MAX": 15.0 },
    { "NAME": "bonePull", "LABEL": "Bone Pull", "TYPE": "float", "DEFAULT": 2.0, "MIN": 0.0, "MAX": 5.0 },
    { "NAME": "boneRadius", "LABEL": "Bone Radius", "TYPE": "float", "DEFAULT": 0.2, "MIN": 0.03, "MAX": 0.5 },
    { "NAME": "bodyFollow", "LABEL": "Body Follow", "TYPE": "float", "DEFAULT": 5.0, "MIN": 0.0, "MAX": 15.0 },
    { "NAME": "viscosity", "LABEL": "Viscosity", "TYPE": "float", "DEFAULT": 0.02, "MIN": 0.0, "MAX": 0.1 },
    { "NAME": "vorticity", "LABEL": "Vorticity", "TYPE": "float", "DEFAULT": 1.5, "MIN": 0.0, "MAX": 5.0 },
    { "NAME": "splatRadius", "LABEL": "Splat Radius", "TYPE": "float", "DEFAULT": 0.06, "MIN": 0.01, "MAX": 0.2 },
    { "NAME": "splatForce", "LABEL": "Splat Force", "TYPE": "float", "DEFAULT": 1.5, "MIN": 0.1, "MAX": 8.0 },
    { "NAME": "metalColor", "LABEL": "Metal Tint", "TYPE": "color", "DEFAULT": [0.7, 0.85, 1.0, 1.0] },
    { "NAME": "envBright", "LABEL": "Reflection", "TYPE": "float", "DEFAULT": 1.2, "MIN": 0.0, "MAX": 3.0 },
    { "NAME": "bumpHeight", "LABEL": "Surface Depth", "TYPE": "float", "DEFAULT": 100.0, "MIN": 0.0, "MAX": 300.0 },
    { "NAME": "specAmount", "LABEL": "Specular", "TYPE": "float", "DEFAULT": 2.0, "MIN": 0.0, "MAX": 5.0 },
    { "NAME": "specPow", "LABEL": "Spec Power", "TYPE": "float", "DEFAULT": 48.0, "MIN": 4.0, "MAX": 128.0 },
    { "NAME": "iridescence", "LABEL": "Iridescence", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 1.5 },
    { "NAME": "texBlend", "LABEL": "Tex Blend", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "transparentBg", "LABEL": "Transparent", "TYPE": "bool", "DEFAULT": false },
    { "NAME": "mpPoseActive", "LABEL": "Pose Active", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 }
  ],
  "PASSES": [
    { "TARGET": "velBuf", "PERSISTENT": true },
    { "TARGET": "uvBuf", "PERSISTENT": true },
    { "TARGET": "prevPoseBuf", "PERSISTENT": true, "WIDTH": "33", "HEIGHT": "1" },
    {}
  ]
}*/

// Body Fluid — fluid attracted toward the skeleton, forming a liquid figure.
// Pass 0: Velocity field (self-advection + bone attraction + body follow + mouse)
// Pass 1: UV advection buffer
// Pass 2: prevPoseBuf snapshot (33x1)
// Pass 3: Final render (liquid metal shading)

#define PI2 6.283185
#define RotNum 5

// ---- Helpers ----
float hash21(vec2 p) {
    vec3 p3 = fract(vec3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.x + p3.y) * p3.z);
}

vec3 hsv2rgb(vec3 c) {
    vec4 K = vec4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

float _ang = PI2 / float(RotNum);

vec2 rot2(vec2 v, float a) {
    float c = cos(a), s = sin(a);
    return vec2(c * v.x - s * v.y, s * v.x + c * v.y);
}

float getRot(vec2 pos, vec2 b, vec2 Res) {
    vec2 p = b;
    float rotSum = 0.0;
    for (int i = 0; i < RotNum; i++) {
        vec2 samp = texture2D(velBuf, fract((pos + p) / Res)).xy - vec2(0.5);
        rotSum += dot(samp, p.yx * vec2(1.0, -1.0));
        p = rot2(p, _ang);
    }
    return rotSum / float(RotNum) / dot(b, b);
}

// ---- Pose landmark sampling ----
vec2 poseJoint(int idx) {
    vec4 lm = texture2D(mpPoseLandmarks, vec2((float(idx) + 0.5) / 33.0, 0.5));
    return vec2(1.0 - lm.r, lm.g);
}
float poseVis(int idx) {
    return texture2D(mpPoseLandmarks, vec2((float(idx) + 0.5) / 33.0, 0.5)).a;
}
vec2 prevJoint(int idx) {
    vec4 lm = texture2D(prevPoseBuf, vec2((float(idx) + 0.5) / 33.0, 0.5));
    return vec2(1.0 - lm.r, lm.g);
}

// ---- Bone attraction: pull fluid toward a line segment between two joints ----
// Returns force vector pointing from `p` toward the nearest point on segment (a, b),
// with smooth falloff. Also adds the segment's velocity (body follow).
void boneAttract(int idxA, int idxB, vec2 p, float aspect,
                 float pullStr, float radius, float followStr,
                 inout vec2 totalForce) {
    float visA = poseVis(idxA);
    float visB = poseVis(idxB);
    if (visA < 0.2 || visB < 0.2) return;

    vec2 a = poseJoint(idxA);
    vec2 b = poseJoint(idxB);

    // Aspect-correct the positions for distance calc
    vec2 pa = p - a;  pa.x *= aspect;
    vec2 ba = b - a;  ba.x *= aspect;

    // Project p onto segment a-b, clamped to [0,1]
    float t = clamp(dot(pa, ba) / (dot(ba, ba) + 0.0001), 0.0, 1.0);

    // Closest point on the bone (in original UV space, not aspect-corrected)
    vec2 closest = a + t * (b - a);

    // Distance from p to closest point (aspect-corrected)
    vec2 diff = closest - p;
    vec2 diffAC = diff;  diffAC.x *= aspect;
    float dist = length(diffAC);

    // Attraction: aggressive pull that makes fluid snap to the bone.
    // Two layers: broad smooth pull + sharp exponential cling near the bone.
    float falloff = smoothstep(radius * 1.5, 0.0, dist); // wider reach
    float innerPull = exp(-dist * dist / (radius * radius * 0.15)); // sharper cling

    // Pull direction: toward the bone — 10x stronger than before
    vec2 pullDir = diff / (dist + 0.001);
    totalForce += pullDir * (falloff * 0.4 + innerPull * 0.6) * pullStr * 0.1;

    // Body follow: push fluid hard in the direction the bone moved
    vec2 prevA = prevJoint(idxA);
    vec2 prevB = prevJoint(idxB);
    if (dot(prevA, prevA) < 0.001 || dot(prevB, prevB) < 0.001) return;
    vec2 prevMid = (prevA + prevB) * 0.5;
    vec2 curMid  = (a + b) * 0.5;
    vec2 boneDelta = curMid - prevMid; // no clamp — let big movements create big forces
    totalForce += boneDelta * followStr * (falloff + innerPull) * 2.0;
}

void main() {
    vec2 Res = RENDERSIZE;
    vec2 pos = gl_FragCoord.xy;
    vec2 uv = isf_FragNormCoord;
    float aspect = Res.x / Res.y;

    // ===== PASS 0: Velocity Field =====
    if (PASSINDEX == 0) {
        vec2 b = cos(float(FRAMEINDEX) * 0.3 - vec2(0.0, 1.57));
        vec2 v = vec2(0.0);
        float bbMax = 0.5 * Res.y;
        bbMax *= bbMax;

        for (int l = 0; l < 20; l++) {
            if (dot(b, b) > bbMax) break;
            vec2 p = b;
            for (int i = 0; i < RotNum; i++) {
                v += p.yx * getRot(pos + p, -rot2(b, _ang * 0.5), Res);
                p = rot2(p, _ang);
            }
            b *= 2.0;
        }

        v *= mix(0.5, 2.0, vorticity / 5.0);

        float speedScale = fluidSpeed * sqrt(Res.x / 600.0);
        vec2 advUV = fract((pos - v * vec2(-1.0, 1.0) * speedScale) / Res);
        vec4 col = texture2D(velBuf, advUV);

        // Viscosity damping
        col.xy = mix(col.xy, vec2(0.5), viscosity);

        // ---- BONE ATTRACTION: pull fluid toward every visible skeleton segment ----
        if (mpPoseActive > 0.5) {
            vec2 boneForce = vec2(0.0);

            // 12 bone connections matching POSE_CONNECTIONS:
            // torso
            boneAttract(11, 12, uv, aspect, bonePull, boneRadius, bodyFollow, boneForce); // shoulders
            boneAttract(11, 23, uv, aspect, bonePull, boneRadius, bodyFollow, boneForce); // L torso
            boneAttract(12, 24, uv, aspect, bonePull, boneRadius, bodyFollow, boneForce); // R torso
            boneAttract(23, 24, uv, aspect, bonePull, boneRadius, bodyFollow, boneForce); // hips
            // arms
            boneAttract(11, 13, uv, aspect, bonePull, boneRadius, bodyFollow, boneForce); // L upper arm
            boneAttract(13, 15, uv, aspect, bonePull, boneRadius, bodyFollow, boneForce); // L forearm
            boneAttract(12, 14, uv, aspect, bonePull, boneRadius, bodyFollow, boneForce); // R upper arm
            boneAttract(14, 16, uv, aspect, bonePull, boneRadius, bodyFollow, boneForce); // R forearm
            // legs
            boneAttract(23, 25, uv, aspect, bonePull, boneRadius, bodyFollow, boneForce); // L thigh
            boneAttract(25, 27, uv, aspect, bonePull, boneRadius, bodyFollow, boneForce); // L shin
            boneAttract(24, 26, uv, aspect, bonePull, boneRadius, bodyFollow, boneForce); // R thigh
            boneAttract(26, 28, uv, aspect, bonePull, boneRadius, bodyFollow, boneForce); // R shin

            col.xy += boneForce;
            col.xy = clamp(col.xy, 0.0, 1.0);
        }

        // Edge bounce
        float edgeDist = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
        float edgeForce = smoothstep(0.05, 0.0, edgeDist);
        if (edgeForce > 0.0) {
            vec2 edgeNormal = vec2(0.0);
            if (uv.x < 0.05) edgeNormal.x = 1.0;
            if (uv.x > 0.95) edgeNormal.x = -1.0;
            if (uv.y < 0.05) edgeNormal.y = 1.0;
            if (uv.y > 0.95) edgeNormal.y = -1.0;
            edgeNormal = normalize(edgeNormal + 0.001);
            vec2 vel = (col.xy - 0.5) * 2.0;
            float into = dot(vel, -edgeNormal);
            if (into > 0.0) {
                vel += edgeNormal * into * 2.0 * edgeForce;
                col.xy = vel * 0.5 + 0.5;
            }
        }

        // Mouse interaction
        float interacting = max(mouseDown, pinchHold);
        if (interacting > 0.3) {
            vec2 mDiff = uv - mousePos;
            mDiff.x *= aspect;
            float dist2 = dot(mDiff, mDiff);
            float r2 = splatRadius * splatRadius;
            if (dist2 < r2 * 12.0) {
                float falloff = exp(-dist2 / r2);
                vec2 force = mouseDelta * Res * splatForce * 0.0003 * interacting;
                if (dot(force, force) < 0.000001) {
                    force = normalize(mDiff + 0.001) * 0.02 * interacting * splatForce;
                }
                col.xy += clamp(force, vec2(-0.3), vec2(0.3)) * falloff;
                col.xy = clamp(col.xy, 0.0, 1.0);
            }
        }

        // Audio: bass creates expanding ripple from center
        if (audioBass > 0.25) {
            float at = float(FRAMEINDEX) * 0.1;
            float angle = hash21(vec2(at, 3.0)) * PI2;
            float dist = 0.1 + hash21(vec2(at, 5.0)) * 0.3;
            vec2 splatPos = 0.5 + vec2(cos(angle), sin(angle)) * dist;
            vec2 mDiff = uv - splatPos;
            mDiff.x *= aspect;
            float d2 = dot(mDiff, mDiff);
            float bassR = splatRadius * (1.0 + audioBass * 3.0);
            if (d2 < bassR * bassR * 12.0) {
                float falloff = exp(-d2 / (bassR * bassR));
                vec2 outDir = normalize(splatPos - 0.5 + 0.001);
                col.xy += outDir * audioBass * 0.2 * splatForce * falloff;
                col.xy = clamp(col.xy, 0.0, 1.0);
            }
        }

        // Start completely still -- black canvas, no fluid until the body creates it.
        // 0.5, 0.5 = zero velocity in the encoded field.
        if (FRAMEINDEX < 4) {
            col = vec4(0.5, 0.5, 0.0, 1.0);
        }

        gl_FragColor = col;
        return;
    }

    // ===== PASS 1: UV Advection =====
    if (PASSINDEX == 1) {
        if (FRAMEINDEX < 4) {
            gl_FragColor = vec4(uv, 0.0, 1.0);
            return;
        }

        vec2 vel = (texture2D(velBuf, uv).xy - 0.5) * 2.0;
        vec2 advUV = fract(uv - vel * 0.003);
        vec2 storedUV = texture2D(uvBuf, advUV).rg;
        storedUV = mix(storedUV, uv, 0.002);

        gl_FragColor = vec4(storedUV, 0.0, 1.0);
        return;
    }

    // ===== PASS 2: prevPoseBuf snapshot (33x1) =====
    if (PASSINDEX == 2) {
        gl_FragColor = (mpPoseActive > 0.5)
            ? texture2D(mpPoseLandmarks, uv)
            : vec4(0.0);
        return;
    }

    // ===== PASS 3: Final Render — Liquid Metal =====

    vec2 warpedUV = texture2D(uvBuf, uv).rg;
    vec2 uvDisp = warpedUV - uv;
    float dispMag = length(uvDisp);

    // Surface normal from UV displacement gradient
    float delta = max(1.0 / Res.x, 1.0 / Res.y);
    vec2 uvL = texture2D(uvBuf, uv + vec2(-delta, 0.0)).rg;
    vec2 uvR = texture2D(uvBuf, uv + vec2( delta, 0.0)).rg;
    vec2 uvU = texture2D(uvBuf, uv + vec2(0.0,  delta)).rg;
    vec2 uvD = texture2D(uvBuf, uv + vec2(0.0, -delta)).rg;

    float hL = length(uvL - (uv + vec2(-delta, 0.0)));
    float hR = length(uvR - (uv + vec2( delta, 0.0)));
    float hU = length(uvU - (uv + vec2(0.0,  delta)));
    float hD = length(uvD - (uv + vec2(0.0, -delta)));

    vec3 n = normalize(vec3(
        (hR - hL) * bumpHeight,
        (hU - hD) * bumpHeight,
        1.0
    ));

    // Iridescent environment
    float envAngle = atan(n.y, n.x) / PI2 + 0.5;
    float envElev = n.z;
    vec3 envColor = hsv2rgb(vec3(
        envAngle + TIME * 0.02 + dispMag * iridescence * 3.0,
        0.5 + 0.3 * (1.0 - envElev),
        0.8 + 0.2 * envElev
    ));

    // Fluid visibility: only show color where the fluid has actually been disturbed.
    // dispMag near zero = untouched pixel = black. This gives the "fluid stick figure"
    // effect where fluid only exists where the skeleton created it.
    float fluidAlpha = smoothstep(0.001, 0.008, dispMag);

    // Early out: black where there's no fluid
    if (fluidAlpha < 0.001) {
        gl_FragColor = vec4(0.0, 0.0, 0.0, transparentBg ? 0.0 : 1.0);
        return;
    }

    // Lighting
    vec3 lightDir = normalize(vec3(0.5, 0.8, 1.0));
    float diff = clamp(dot(n, lightDir), 0.3, 1.0);

    vec2 sc = (pos - Res * 0.5) / Res.x;
    vec3 viewDir = normalize(vec3(sc, -1.0));
    vec3 halfVec = normalize(lightDir - viewDir);
    float spec = pow(max(dot(n, halfVec), 0.0), specPow) * specAmount;

    vec3 lightDir2 = normalize(vec3(-0.7, -0.3, 0.8));
    float diff2 = clamp(dot(n, lightDir2), 0.0, 1.0) * 0.3;
    vec3 halfVec2 = normalize(lightDir2 - viewDir);
    float spec2 = pow(max(dot(n, halfVec2), 0.0), specPow * 0.5) * specAmount * 0.4;

    // Liquid metal color -- only where fluid exists
    vec3 metal = metalColor.rgb * (diff + diff2);
    vec3 env = envColor * envBright * (1.0 - envElev * 0.5);
    vec3 col = mix(metal, env, 0.4 + dispMag * 2.0);
    col += vec3(spec + spec2);

    // Optional texture blend
    if (texBlend > 0.0) {
        bool hasInput = IMG_SIZE_inputTex.x > 0.0;
        if (hasInput) {
            vec3 texCol = texture2D(inputTex, warpedUV).rgb;
            col = mix(col, texCol * diff, texBlend);
        }
    }

    // Fade from black based on fluid presence
    col *= fluidAlpha;

    float alpha = transparentBg ? fluidAlpha : 1.0;

    gl_FragColor = vec4(col, alpha);
}
