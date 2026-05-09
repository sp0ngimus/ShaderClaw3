/*{
  "CATEGORIES": ["Generator", "Text"],
  "DESCRIPTION": "Digifade — glitch dissolve with thermal infrared false-color field. Text appears as heat signatures; cold zones are near-black, hot zones white-amber. HDR text 2.8× for bloom. Palette: void black → crimson → amber → white-hot.",
  "INPUTS": [
    { "NAME": "msg", "TYPE": "text", "DEFAULT": " ETHEREA", "MAX_LENGTH": 48 },
    { "NAME": "preset", "LABEL": "Style", "TYPE": "long", "VALUES": [0,1], "LABELS": ["Digifade","Digifade Glitch"], "DEFAULT": 0 },
    { "NAME": "fontFamily", "LABEL": "Font", "TYPE": "long", "VALUES": [0,1,2,3], "LABELS": ["Inter","Times New Roman","Libre Caslon","Outfit"], "DEFAULT": 0 },
    { "NAME": "speed", "LABEL": "Speed", "TYPE": "float", "MIN": 0.1, "MAX": 3.0, "DEFAULT": 0.5 },
    { "NAME": "intensity", "LABEL": "Glitch", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
    { "NAME": "density", "LABEL": "Dissolve", "TYPE": "float", "MIN": 0.0, "MAX": 1.0, "DEFAULT": 0.5 },
    { "NAME": "textScale", "LABEL": "Size", "TYPE": "float", "MIN": 0.3, "MAX": 2.0, "DEFAULT": 1.0 },
    { "NAME": "textColor", "LABEL": "Color", "TYPE": "color", "DEFAULT": [1.0, 0.9, 0.4, 1.0] },
    { "NAME": "bgColor", "LABEL": "Background", "TYPE": "color", "DEFAULT": [0.0, 0.0, 0.02, 1.0] },
    { "NAME": "transparentBg", "LABEL": "Transparent", "TYPE": "bool", "DEFAULT": false },
    { "NAME": "hdrBoost", "LABEL": "HDR Boost", "TYPE": "float", "DEFAULT": 2.8, "MIN": 1.0, "MAX": 4.0 },
    { "NAME": "thermalNoise", "LABEL": "Thermal Noise", "TYPE": "float", "DEFAULT": 0.4, "MIN": 0.0, "MAX": 1.0 }
  ]
}*/

const float PI = 3.14159265;
const float TWO_PI = 6.28318530;

// Atlas-only font engine (no bitmap fallback — faster ANGLE compile)
float charPixel(int ch, float col, float row) {
    if (ch < 0 || ch > 36) return 0.0;
    vec2 uv = vec2(col / 5.0, row / 7.0);
    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) return 0.0;
    return smoothstep(0.1, 0.55, texture2D(fontAtlasTex, vec2((float(ch) + uv.x) / 37.0, uv.y)).r);
}

int getChar(int slot) {
    if (slot == 0)  return int(msg_0);
    if (slot == 1)  return int(msg_1);
    if (slot == 2)  return int(msg_2);
    if (slot == 3)  return int(msg_3);
    if (slot == 4)  return int(msg_4);
    if (slot == 5)  return int(msg_5);
    if (slot == 6)  return int(msg_6);
    if (slot == 7)  return int(msg_7);
    if (slot == 8)  return int(msg_8);
    if (slot == 9)  return int(msg_9);
    if (slot == 10) return int(msg_10);
    if (slot == 11) return int(msg_11);
    if (slot == 12) return int(msg_12);
    if (slot == 13) return int(msg_13);
    if (slot == 14) return int(msg_14);
    if (slot == 15) return int(msg_15);
    if (slot == 16) return int(msg_16);
    if (slot == 17) return int(msg_17);
    if (slot == 18) return int(msg_18);
    if (slot == 19) return int(msg_19);
    if (slot == 20) return int(msg_20);
    if (slot == 21) return int(msg_21);
    if (slot == 22) return int(msg_22);
    if (slot == 23) return int(msg_23);
    if (slot == 24) return int(msg_24);
    if (slot == 25) return int(msg_25);
    if (slot == 26) return int(msg_26);
    if (slot == 27) return int(msg_27);
    if (slot == 28) return int(msg_28);
    if (slot == 29) return int(msg_29);
    if (slot == 30) return int(msg_30);
    if (slot == 31) return int(msg_31);
    if (slot == 32) return int(msg_32);
    if (slot == 33) return int(msg_33);
    if (slot == 34) return int(msg_34);
    if (slot == 35) return int(msg_35);
    if (slot == 36) return int(msg_36);
    if (slot == 37) return int(msg_37);
    if (slot == 38) return int(msg_38);
    if (slot == 39) return int(msg_39);
    if (slot == 40) return int(msg_40);
    if (slot == 41) return int(msg_41);
    if (slot == 42) return int(msg_42);
    if (slot == 43) return int(msg_43);
    if (slot == 44) return int(msg_44);
    if (slot == 45) return int(msg_45);
    if (slot == 46) return int(msg_46);
    return int(msg_47);
}

int charCount() {
    int n = int(msg_len);
    return n > 0 ? n : 1;
}

float sampleChar(int ch, vec2 uv) {
    if (ch < 0 || ch > 36) return 0.0;
    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) return 0.0;
    return texture2D(fontAtlasTex, vec2((float(ch) + uv.x) / 37.0, uv.y)).r;
}

float hash(float n) { return fract(sin(n * 127.1) * 43758.5453); }

// =======================================================================
// THERMAL INFRARED BACKGROUND
// =======================================================================

float hash21(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float vnoise(vec2 p) {
    vec2 i = floor(p); vec2 f = fract(p);
    f = f*f*(3.0-2.0*f);
    return mix(mix(hash21(i), hash21(i+vec2(1,0)), f.x),
               mix(hash21(i+vec2(0,1)), hash21(i+vec2(1,1)), f.x), f.y);
}

vec3 thermalBg(vec2 uv) {
    // Thermal noise field — slow drift
    float n = vnoise(uv * 4.0 + vec2(TIME * 0.08, TIME * 0.05));
    n = n * 0.6 + vnoise(uv * 9.0 + vec2(TIME * 0.12, -TIME * 0.07)) * 0.4;
    n = clamp(n * (0.5 + thermalNoise * 0.8), 0.0, 1.0);
    // False-color palette: cold=black → cool=deep crimson → warm=orange → hot=white-amber
    vec3 cold   = vec3(0.0, 0.0, 0.02);
    vec3 cool   = vec3(0.4, 0.0, 0.15);
    vec3 warm   = vec3(1.0, 0.3, 0.0);
    vec3 hot    = vec3(1.0, 0.85, 0.5);
    if (n < 0.33) return mix(cold, cool, n / 0.33);
    if (n < 0.66) return mix(cool, warm, (n-0.33)/0.33);
    return mix(warm, hot, (n-0.66)/0.34);
}

// =======================================================================
// EFFECT: DIGIFADE - glitch dissolve
// =======================================================================

vec4 effectDigifade(vec2 uv, int sub) {
    float aspect = RENDERSIZE.x / RENDERSIZE.y;
    int numChars = charCount();
    float glitchAmount = intensity;
    float sliceCount = mix(5.0, 100.0, density);

    float complexity = 1.0, sweepSpeed = 1.0, vertGlitch = 0.0, maxDisp = 0.3;
    if (sub == 1) { complexity = 2.0; sweepSpeed = 1.3; maxDisp = 0.5; vertGlitch = 0.4; }

    float t = TIME * speed * sweepSpeed;
    vec2 p = vec2((uv.x - 0.5) * aspect + 0.5, uv.y);

    // Single-line layout: all chars on one row, scale to fit width
    float cH = 0.18 * textScale;
    if (aspect < 1.0) cH *= aspect;
    float cW = cH * (5.0/7.0);
    float gW = cW * 0.2;

    // Scale down if text is wider than screen
    float totalTextW = float(numChars) * cW + float(numChars - 1) * gW;
    float maxW = 0.9 * aspect;
    float fitScale = totalTextW > maxW ? maxW / totalTextW : 1.0;
    cH *= fitScale;
    cW *= fitScale;
    gW *= fitScale;

    float rowW = float(numChars) * cW + float(numChars - 1) * gW;
    float startX = 0.5 - rowW * 0.5;
    float startY = 0.5 - cH * 0.5;

    float si = floor(uv.y * sliceCount);
    float n1 = hash(si + floor(t*2.0));
    float n2 = hash(si*3.7 + floor(t*3.0));

    float textHit = 0.0;

    float sw = sin(t*0.7)*0.5+0.5;
    float ps = smoothstep(sw-0.15, sw+0.1, (p.x-startX)/max(rowW, 0.001));

    float dx = abs(ps*n1*glitchAmount*maxDisp + ps*sin(si*0.3*complexity+t)*glitchAmount*maxDisp*0.3);
    float dy = vertGlitch > 0.01 ? ps*(n2-0.5)*vertGlitch*glitchAmount*0.06 : 0.0;

    vec2 samp = vec2(p.x - dx, p.y - dy);
    float rx = samp.x - startX, ry = samp.y - startY;

    if (rx >= 0.0 && rx <= rowW && ry >= 0.0 && ry <= cH) {
        float cs = cW + gW;
        float csF = rx / cs;
        int slot = int(floor(csF));
        float clx = fract(csF), cf = cW/cs;
        if (clx < cf && slot >= 0 && slot < numChars) {
            float gc = (clx/cf)*5.0, gr = (ry/cH)*7.0;
            if (gc >= 0.0 && gc < 5.0 && gr >= 0.0 && gr < 7.0) {
                int ch = getChar(slot);
                if (ch >= 0 && ch <= 36 && ch != 26) textHit = max(textHit, charPixel(ch, gc, gr));
            }
        }
    }

    vec3 bgThermal = thermalBg(uv);
    vec3 fc;
    float a = 1.0;
    if (transparentBg) {
        a = textHit;
        fc = textColor.rgb * hdrBoost;
    } else {
        fc = mix(bgThermal, textColor.rgb * hdrBoost, smoothstep(0.2, 0.8, textHit));
    }
    return vec4(fc, a);
}

// =======================================================================
// MAIN
// =======================================================================

void main() {
    vec2 uv = gl_FragCoord.xy / RENDERSIZE.xy;
    int p = int(preset);
    vec4 col = effectDigifade(uv, p);

    if (_voiceGlitch > 0.01) {
        float g = _voiceGlitch;
        float t = TIME * 17.0;
        float band = floor(uv.y * mix(8.0, 40.0, g) + t * 3.0);
        float bandNoise = fract(sin(band * 91.7 + t) * 43758.5);
        float bandActive = step(1.0 - g * 0.6, bandNoise);
        float shift = (bandNoise - 0.5) * 0.08 * g * bandActive;
        float chromaAmt = g * 0.015;
        vec2 uvR = uv + vec2(shift + chromaAmt, 0.0);
        vec2 uvB = uv + vec2(shift - chromaAmt, 0.0);
        vec2 uvG = uv + vec2(shift, chromaAmt * 0.5);
        vec4 cR = effectDigifade(uvR, p);
        vec4 cG = effectDigifade(uvG, p);
        vec4 cB = effectDigifade(uvB, p);
        vec4 glitched = vec4(cR.r, cG.g, cB.b, max(max(cR.a, cG.a), cB.a));
        float scanline = 0.95 + 0.05 * sin(uv.y * RENDERSIZE.y * 1.5 + t * 40.0);
        float blockX = floor(uv.x * 6.0);
        float blockY = floor(uv.y * 4.0);
        float blockNoise = fract(sin((blockX + blockY * 7.0) * 113.1 + floor(t * 8.0)) * 43758.5);
        float dropout = step(1.0 - g * 0.15, blockNoise);
        glitched.rgb *= scanline;
        glitched.rgb *= 1.0 - dropout;
        col = mix(col, glitched, smoothstep(0.0, 0.3, g));
    }

    gl_FragColor = col;
}
