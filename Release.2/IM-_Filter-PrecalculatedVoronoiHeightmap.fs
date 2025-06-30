/*{
  "DESCRIPTION": "Precalculated Voronoi Heightmap Raymarch by Inigo Quilez (iq) and others, converted to ISF with parameter smoothing. Original ShaderToy: https://www.shadertoy.com/view/MdX3Rr",
  "CREDIT": "Original ShaderToy by InigoQuilez (@iq), @Nimitz, @Fabrice, @Coyote. ISF version by @dot2dot (bareimage).",
  "ISFVSN": "2.0",
  "CATEGORIES": ["FILTER", "3D", "TEXTURE"],
  "INPUTS": [
    { "NAME": "overallSpeed", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 2.0, "LABEL": "Overall Animation Speed" },
    { "NAME": "voronoiFrequency1", "TYPE": "float", "DEFAULT": 5.0, "MIN": 1.0, "MAX": 20.0, "LABEL": "Voronoi Frequency Layer 1" },
    { "NAME": "voronoiFrequency2", "TYPE": "float", "DEFAULT": 10.0, "MIN": 5.0, "MAX": 40.0, "LABEL": "Voronoi Frequency Layer 2" },
    { "NAME": "voronoiFrequency3", "TYPE": "float", "DEFAULT": 25.0, "MIN": 10.0, "MAX": 80.0, "LABEL": "Voronoi Frequency Layer 3" },
    { "NAME": "voronoiPerturbAmount", "TYPE": "float", "DEFAULT": 0.01, "MIN": 0.0, "MAX": 0.05, "LABEL": "Voronoi Perturb Amount" },
    { "NAME": "textureScale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 5.0, "LABEL": "Texture Scale (Image Pass)" },
    { "NAME": "lightDistanceFactor", "TYPE": "float", "DEFAULT": 0.125, "MIN": 0.01, "MAX": 0.5, "LABEL": "Light Distance Attenuation" },
    { "NAME": "specularPower", "TYPE": "float", "DEFAULT": 16.0, "MIN": 2.0, "MAX": 64.0, "LABEL": "Specular Power" },
    { "NAME": "fresnelPower", "TYPE": "float", "DEFAULT": 2.0, "MIN": 0.1, "MAX": 10.0, "LABEL": "Fresnel Power" },
    { "NAME": "curveAmp", "TYPE": "float", "DEFAULT": 7.5, "MIN": 1.0, "MAX": 20.0, "LABEL": "Curve Amplitude" },
    { "NAME": "curveAmpInit", "TYPE": "float", "DEFAULT": 0.525, "MIN": 0.0, "MAX": 2.0, "LABEL": "Curve Initial Amplitude" },
    { "NAME": "aoSamples", "TYPE": "float", "DEFAULT": 5.0, "MIN": 1.0, "MAX": 10.0, "LABEL": "AO Samples" },
    { "NAME": "aoIntensity", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 2.0, "LABEL": "AO Intensity" },
    { "NAME": "noiseTexture", "TYPE": "image", "LABEL": "Noise Detail Texture (for Buffer A)"},
    { "NAME": "showHeightmap", "TYPE": "bool", "DEFAULT": false, "LABEL": "Show Heightmap Only (Image Pass)" },
    { "NAME": "transitionSpeed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 10.0, "LABEL": "Parameter Transition Smoothness" }
  ],
  "PASSES": [
    { "TARGET": "timeBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "voronoiParamsBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "lightingParamsBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "aoCurveParamsBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "sceneBuffer", "PERSISTENT": true, "FLOAT": true },
    { "TARGET": "finalOutput" }
  ]
}*/

precision highp float;

// --- Global smoothed parameters (populated by smoothing passes) ---
float currentOverallSpeed;
float currentVoronoiFrequency1;
float currentVoronoiFrequency2;
float currentVoronoiFrequency3;
float currentVoronoiPerturbAmount;
float currentTextureScale;
float currentLightDistanceFactor;
float currentSpecularPower;
float currentFresnelPower;
float currentCurveAmp;
float currentCurveAmpInit;
float currentAOSamples_float; // Stores the smoothed AO samples as a float
float currentAOIntensity;
bool currentShowHeightmap;

// Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License
//
// This work is licensed under the Creative Commons Attribution-NonCommercial-ShareAlike 
// 3.0 Unported License. To view a copy of this license, visit 
// http://creativecommons.org/licenses/by-nc-sa/3.0/ or send a letter to Creative Commons, 
// PO Box 1866, Mountain View, CA 94042, USA.
//
// You are free to:
// - Share: copy and redistribute the material in any medium or format
// - Adapt: remix, transform, and build upon the material
//
// Under the following terms:
// - Attribution: You must give appropriate credit, provide a link to the license, 
//   and indicate if changes were made. You may do so in any reasonable manner, 
//   but not in any way that suggests the licensor endorses you or your use.
// - NonCommercial: You may not use the material for commercial purposes.
// - ShareAlike: If you remix, transform, or build upon the material, you must 
//   distribute your contributions under the same license as the original.
//
// No additional restrictions: You may not apply legal terms or technological 
// measures that legally restrict others from doing anything the license permits.
//
// DISCLAIMER: This work is provided "AS IS" without warranty of any kind, express 
// or implied. The licensor makes no warranties regarding this work and disclaims 
// liability for damages resulting from its use to the fullest extent possible

// --- Helper Functions ---
vec2 hash22(vec2 p) {
    p = mod(p, 5.0);
    float n = sin(dot(p, vec2(41.0, 289.0)));
    return fract(vec2(8.0, 1.0) * 262144.0 * n);
}

float voronoi(vec2 p) {
    vec2 g = floor(p), o;
    p -= g;
    vec3 d = vec3(1.0);
    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            o = vec2(float(x), float(y));
            o += hash22(g + o) - p;
            float dot_o = dot(o, o);
            d.z = dot_o;
            d.y = max(d.x, min(d.y, d.z));
            d.x = min(d.x, d.z);
        }
    }
    return d.y - d.x;
}

vec3 triplanarTexture(sampler2D tex, vec3 p, vec3 n) {
    p = fract(p);
    n = max(n * n, vec3(0.001));
    n /= (n.x + n.y + n.z);
    return (texture(tex, p.yz) * n.x + texture(tex, p.zx) * n.y + texture(tex, p.xy) * n.z).xyz;
}

// --- Pass 4: Buffer A - Generate Stone Texture and Heightmap ---
void bufferA_main(out vec4 fragColor, vec2 fragCoord_norm) {
    vec2 p = fragCoord_norm;

    if (floor(hash22(p + TIME).x * 8.0) < 1.0 || FRAMEINDEX == 0) {
        vec3 tx = texture(noiseTexture, p).xyz;
        tx *= tx;
        tx = smoothstep(0.0, 0.5, tx);

        vec2 p_perturbed = p + sin(p * 6.2831853 * 2.0 - cos(p.yx * 6.2831853 * 4.0)) * currentVoronoiPerturbAmount;

        float c = voronoi(p_perturbed * currentVoronoiFrequency1 - 0.35) * 0.6 +
                  voronoi((p_perturbed.yx + 0.5) * currentVoronoiFrequency2) * 0.3 +
                  (1.0 - voronoi(p_perturbed * currentVoronoiFrequency3)) * 0.075 +
                  voronoi(p_perturbed * currentVoronoiFrequency3 * 2.4) * 0.025;

        c += dot(tx, vec3(0.299, 0.587, 0.114)) * 0.1;

        fragColor.xyz = tx;
        fragColor.w = min(c / 1.1, 1.0);
    } else {
        fragColor = IMG_NORM_PIXEL(sceneBuffer, fragCoord_norm);
    }
}

// --- Pass 5: Image - Raymarching with Heightmap ---
float heightMap_image(vec2 p, sampler2D heightMapTex) {
    return texture(heightMapTex, fract(p / 2.0)).w;
}

float map_image(vec3 p, sampler2D heightMapTex) {
    float c = heightMap_image(p.xy, heightMapTex);
    return 1.0 - p.z - c * 0.1;
}

vec3 getNormal_image(vec3 pos, sampler2D heightMapTex) {
    vec2 e = vec2(0.001, -0.001);
    return normalize(
        e.xyy * map_image(pos + e.xyy, heightMapTex) +
        e.yyx * map_image(pos + e.yyx, heightMapTex) +
        e.yxy * map_image(pos + e.yxy, heightMapTex) +
        e.xxx * map_image(pos + e.xxx, heightMapTex)
    );
}

// calculateAO_image now takes an int for numSamples
float calculateAO_image(vec3 p, vec3 n, sampler2D heightMapTex, int numSamples, float intensity) {
    float r = 1.0;
    float w = 1.0;
    for (int i = 1; i <= numSamples; i++) { // Loop with int
        float d0 = float(i) / float(numSamples); // Cast i to float for division
        r += w * (map_image(p + n * d0, heightMapTex) - d0);
        w *= 0.5 * intensity;
    }
    return clamp(r, 0.0, 1.0);
}

float curve_image(vec3 p, sampler2D heightMapTex, float amp, float ampInit) {
    float eps = 0.0225;
    vec2 e = vec2(-1.0, 1.0) * eps;
    float t1 = map_image(p + e.yxx, heightMapTex);
    float t2 = map_image(p + e.xxy, heightMapTex);
    float t3 = map_image(p + e.xyx, heightMapTex);
    float t4 = map_image(p + e.yyy, heightMapTex);
    return clamp((t1 + t2 + t3 + t4 - 4.0 * map_image(p, heightMapTex)) * amp + ampInit, 0.0, 1.0);
}

void image_main(out vec4 fragColor, vec2 fragCoord_norm, sampler2D heightMapTex) {
    float tm = TIME * currentOverallSpeed * 0.5;
    vec2 th = sin(vec2(1.57, 0.0) + sin(tm / 4.0) * 0.3);

    vec3 rd = normalize(vec3(fragCoord_norm * RENDERSIZE.xy - RENDERSIZE.xy * 0.5, min(RENDERSIZE.y * 0.75, 600.0)));
    rd.xy = mat2(th.x, th.y, -th.y, th.x) * rd.xy;

    vec3 ro = vec3(tm, cos(tm / 4.0), 0.0);
    vec3 lp = ro + vec3(cos(tm / 4.0) * 0.5, sin(tm / 4.0) * 0.5, -0.5);

    float t = 0.0;
    float d;
    for (int j = 0; j < 32; j++) {
        d = map_image(ro + rd * t, heightMapTex);
        if (d < 0.001) break;
        t += d * 0.7;
    }

    vec3 sp = ro + rd * t;
    vec3 sn = getNormal_image(sp, heightMapTex);
    vec3 ld = lp - sp;

    float tSize0 = 1.0 / currentTextureScale;
    float c_height = heightMap_image(sp.xy, heightMapTex);

    vec3 oC = triplanarTexture(heightMapTex, sp * tSize0, sn);
    oC *= pow(max(c_height * c_height * sn * 0.7 + vec3(1.0), vec3(0.0)), vec3(2.0)) * c_height;

    float lDist = max(length(ld), 0.001);
    float atten = 1.0 / (1.0 + lDist * currentLightDistanceFactor);
    ld = normalize(ld);

    float diff = max(dot(ld, sn), 0.0);
    diff = pow(diff, 4.0) * 2.0;
    float spec = pow(max(dot(reflect(-ld, sn), -rd), 0.0), currentSpecularPower);
    float fre = pow(clamp(dot(sn, rd) + 1.0, 0.0, 1.0), currentFresnelPower);
    float Schlick = pow(1.0 - max(dot(rd, normalize(rd + ld)), 0.0), 5.0);
    float fre2 = mix(0.5, 1.0, Schlick);

    float crv = curve_image(sp, heightMapTex, currentCurveAmp, currentCurveAmpInit);
    
    // Convert currentAOSamples_float to int for the AO calculation
    int final_ao_samples = int(floor(currentAOSamples_float + 0.5)); // floor + 0.5 for rounding
    float ao = calculateAO_image(sp, sn, heightMapTex, final_ao_samples, currentAOIntensity);


    vec3 col = (oC * (diff + 0.25 + vec3(0.5, 0.7, 1.0) * spec * fre2 * 4.0 + vec3(1.0, 0.1, 0.2) * fre * 8.0));
    col *= atten * crv * ao;

    if (currentShowHeightmap) {
        vec2 uv_heightmap = fragCoord_norm;
        uv_heightmap = mat2(th.x, th.y, -th.y, th.x) * uv_heightmap;
        uv_heightmap += vec2(TIME, cos(TIME / 4.0)) / 2.0;
        vec4 tex_heightmap = texture(heightMapTex, fract(uv_heightmap / 1.0));
        col = sqrt(tex_heightmap.xyz) * tex_heightmap.w;
    }

    fragColor = vec4(sqrt(clamp(col, 0.0, 1.0)), 1.0);
}


// --- ISF Main Function ---
void main() {
    if (PASSINDEX == 0) {
        vec4 prevData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        float smoothedSpeed = (FRAMEINDEX == 0) ? overallSpeed : mix(prevData.r, overallSpeed, min(1.0, TIMEDELTA * transitionSpeed));
        gl_FragColor = vec4(smoothedSpeed, 0.0, 0.0, 1.0);
        return;
    }

    if (PASSINDEX == 1) {
        vec4 prevData = IMG_NORM_PIXEL(voronoiParamsBuffer, vec2(0.5));
        float smVoronoiFrequency1 = (FRAMEINDEX == 0) ? voronoiFrequency1 : mix(prevData.r, voronoiFrequency1, min(1.0, TIMEDELTA * transitionSpeed));
        float smVoronoiFrequency2 = (FRAMEINDEX == 0) ? voronoiFrequency2 : mix(prevData.g, voronoiFrequency2, min(1.0, TIMEDELTA * transitionSpeed));
        float smVoronoiFrequency3 = (FRAMEINDEX == 0) ? voronoiFrequency3 : mix(prevData.b, voronoiFrequency3, min(1.0, TIMEDELTA * transitionSpeed));
        float smVoronoiPerturbAmount = (FRAMEINDEX == 0) ? voronoiPerturbAmount : mix(prevData.a, voronoiPerturbAmount, min(1.0, TIMEDELTA * transitionSpeed));
        gl_FragColor = vec4(smVoronoiFrequency1, smVoronoiFrequency2, smVoronoiFrequency3, smVoronoiPerturbAmount);
        return;
    }

    if (PASSINDEX == 2) {
        vec4 prevData = IMG_NORM_PIXEL(lightingParamsBuffer, vec2(0.5));
        float smTextureScale = (FRAMEINDEX == 0) ? textureScale : mix(prevData.r, textureScale, min(1.0, TIMEDELTA * transitionSpeed));
        float smLightDistanceFactor = (FRAMEINDEX == 0) ? lightDistanceFactor : mix(prevData.g, lightDistanceFactor, min(1.0, TIMEDELTA * transitionSpeed));
        float smSpecularPower = (FRAMEINDEX == 0) ? specularPower : mix(prevData.b, specularPower, min(1.0, TIMEDELTA * transitionSpeed));
        float smFresnelPower = (FRAMEINDEX == 0) ? fresnelPower : mix(prevData.a, fresnelPower, min(1.0, TIMEDELTA * transitionSpeed));
        gl_FragColor = vec4(smTextureScale, smLightDistanceFactor, smSpecularPower, smFresnelPower);
        return;
    }

    if (PASSINDEX == 3) { // AO and Curve parameters smoothing
        vec4 prevData = IMG_NORM_PIXEL(aoCurveParamsBuffer, vec2(0.5));
        float smCurveAmp = (FRAMEINDEX == 0) ? curveAmp : mix(prevData.r, curveAmp, min(1.0, TIMEDELTA * transitionSpeed));
        float smCurveAmpInit = (FRAMEINDEX == 0) ? curveAmpInit : mix(prevData.g, curveAmpInit, min(1.0, TIMEDELTA * transitionSpeed));
        // aoSamples is now a float input. Smooth it as a float.
        float smAOSamples_float_val = (FRAMEINDEX == 0) ? aoSamples : mix(prevData.b, aoSamples, min(1.0, TIMEDELTA * transitionSpeed));
        float smAOIntensity = (FRAMEINDEX == 0) ? aoIntensity : mix(prevData.a, aoIntensity, min(1.0, TIMEDELTA * transitionSpeed));
        gl_FragColor = vec4(smCurveAmp, smCurveAmpInit, smAOSamples_float_val, smAOIntensity); // Store the smoothed float
        return;
    }

    if (PASSINDEX == 4) { // Render Buffer A
        vec4 voronoiParams = IMG_NORM_PIXEL(voronoiParamsBuffer, vec2(0.5));
        currentVoronoiFrequency1 = voronoiParams.r;
        currentVoronoiFrequency2 = voronoiParams.g;
        currentVoronoiFrequency3 = voronoiParams.b;
        currentVoronoiPerturbAmount = voronoiParams.a;

        bufferA_main(gl_FragColor, isf_FragNormCoord);
        return;
    }

    if (PASSINDEX == 5) { // Render Image
        vec4 timeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        currentOverallSpeed = timeData.r;

        vec4 lightingParams = IMG_NORM_PIXEL(lightingParamsBuffer, vec2(0.5));
        currentTextureScale = lightingParams.r;
        currentLightDistanceFactor = lightingParams.g;
        currentSpecularPower = lightingParams.b;
        currentFresnelPower = lightingParams.a;

        vec4 aoCurveParams = IMG_NORM_PIXEL(aoCurveParamsBuffer, vec2(0.5));
        currentCurveAmp = aoCurveParams.r;
        currentCurveAmpInit = aoCurveParams.g;
        currentAOSamples_float = aoCurveParams.b; // Retrieve the smoothed float value
        currentAOIntensity = aoCurveParams.a;

        currentShowHeightmap = showHeightmap;

        image_main(gl_FragColor, isf_FragNormCoord, sceneBuffer);
        return;
    }
}
