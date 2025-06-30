/*{
  "DESCRIPTION": "3D Tunnel Effect with Smooth Parameter Transitions and Radial Blur (VDMX-Compatible)",
  "CREDIT": "Original by @zguerrero, ISF 2.0 adaptation, Metal Conversion, Buffer Implementation by @dot2dot (@bareimage)",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR", "3D", "BLUR"],
  "INPUTS": [
    { "NAME": "speed", "TYPE": "float", "DEFAULT": 3.0, "MIN": 0.0, "MAX": 10.0, "LABEL": "Animation Speed" },
    { "NAME": "pistonSpeed", "TYPE": "float", "DEFAULT": 10.0, "MIN": 0.0, "MAX": 30.0, "LABEL": "Piston Movement Speed" },
    { "NAME": "radialBlurIntensity", "TYPE": "float", "DEFAULT": 0.01, "MIN": 0.0, "MAX": 0.1, "LABEL": "Radial Blur Intensity" },
    { "NAME": "tunnelScale", "TYPE": "float", "DEFAULT": 8.5, "MIN": 5.0, "MAX": 15.0, "LABEL": "Tunnel Scale" },
    { "NAME": "curvAmount", "TYPE": "float", "DEFAULT": 0.075, "MIN": 0.0, "MAX": 0.2, "LABEL": "Curvature Amount" },
    { "NAME": "reflAmount", "TYPE": "float", "DEFAULT": 0.8, "MIN": 0.0, "MAX": 1.0, "LABEL": "Reflection Amount" },
    { "NAME": "transitionSpeed", "TYPE": "float", "DEFAULT": 2.0, "MIN": 0.1, "MAX": 10.0, "LABEL": "Transition Smoothness" },
    { "NAME": "mainLightColor", "TYPE": "color", "DEFAULT": [0.3, 0.6, 1.0, 1.0], "LABEL": "Main Light Color" },
    { "NAME": "lightIntensity", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 3.0, "LABEL": "Light Intensity" }
  ],
  "PASSES": [
    { "TARGET": "timeBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "paramBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "pistonBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "lightParamBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "lightColorBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "sceneBuffer", "PERSISTENT": false, "FLOAT": true },
    { "TARGET": "finalOutput" }
  ]
}*/

const float epsilon = 0.02;
const float pi = 3.14159265359;
const vec3 wallsColor = vec3(0.05, 0.025, 0.025);
const vec3 fogColor = vec3(0.05, 0.05, 0.2);

precision highp float;

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

float sdCylinder(vec3 p, vec3 c) { return length(c.xy - p.xz) - c.z; }
float sdSphere(vec3 p, float s) { return length(p) - s; }
float sdBox(vec3 p, vec3 b) { vec3 d = abs(p) - b; return min(max(d.x, max(d.y, d.z)), 0.0) + length(max(d, 0.0)); }
vec3 opRep(vec3 p, vec3 c) { return mod(p, c) - 0.5 * c; }
vec2 linearStep2(vec2 mi, vec2 ma, vec2 v) { return clamp((v - mi)/(ma - mi), 0.0, 1.0); }
float tunnel(vec3 p, vec3 c) { return -length(c.xy - p.xz) + c.z; }

// distfunc now takes pistonPhaseArg instead of instantaneous piston speed for the sin wave.
// The 'time' parameter is removed from distfunc as it's no longer used directly for the piston sin wave.
// mainSpeedArg is the overall animation speed (currentSpeed), which is currently unused in distfunc.
vec4 distfunc(vec3 pos, float pistonPhaseArg, float tunnelScaleArg, float mainSpeedArg) {
    vec3 repPos = opRep(pos, vec3(4.0, 1.0, 4.0));
    vec2 sinPos = sin((pos.z * pi / 8.0) + vec2(0.0, pi)) * 1.75;
    vec3 repPosSin = opRep(pos.xxz + vec3(sinPos.x, sinPos.y, 0.0), vec3(4.0, 4.0, 0.0));
    float cylinders = sdCylinder(vec3(repPos.x, pos.y, repPos.z), vec3(0.0, 0.0, 0.5));
    
    // Piston effect using accumulated pistonPhaseArg
    float s = sin(pistonPhaseArg + floor(pos.z * 0.25)); 

    float cutCylinders1 = sdBox(vec3(pos.x, pos.y, repPos.z), vec3(100.0, clamp(s, 0.025, 0.75), 1.0));
    float cutCylinders2 = sdBox(vec3(repPos.x, pos.y, repPos.z), vec3(0.035, 100.0, 10.0));
    float cuttedCylinders = max(-cutCylinders2, max(-cutCylinders1, cylinders));
    float innerCylinders = sdCylinder(vec3(repPos.x, pos.y, repPos.z), vec3(0.0, 0.0, 0.15));
    float tubes1 = sdCylinder(vec3(repPosSin.x, 0.0, pos.y - 0.85), vec3(0.0, 0.0, 0.025));
    float tubes2 = sdCylinder(vec3(repPosSin.y, 0.0, pos.y + 0.85), vec3(0.0, 0.0, 0.025));
    float tubes = min(tubes1, tubes2);
    float lightsGeom = min(tubes, innerCylinders);
    float resultCylinders = min(cuttedCylinders, lightsGeom);
    float spheres = sdSphere(vec3(repPos.x, pos.y, repPos.z), (s * 0.5 + 0.5) * 1.5);
    float light = min(tubes, spheres);
    vec2 planeMod = abs(fract(pos.xx * vec2(0.25, 4.0) + 0.5) * 4.0 - 2.0) - 1.0;
    float planeMod2 = clamp(planeMod.y, -0.02, 0.02) * min(0.0, planeMod.x);
    float cylindersCutPlane = sdCylinder(vec3(repPos.x, pos.y, repPos.z), vec3(0.0, 0.0, 0.6));
    float spheresCutPlane = sdSphere(vec3(repPos.x, pos.y, repPos.z), 1.3);
    float plane = 1.0 - abs(pos.y + clamp(planeMod.x, -0.04, 0.04) + planeMod2);
    float t = tunnel(pos.xzy * vec3(1.0, 1.0, 3.0), vec3(0.0, 0.0, tunnelScaleArg));
    float cutTunnel = sdBox(vec3(pos.x, pos.y, repPos.z), vec3(100.0, 100.0, 0.1));
    plane = min(max(-cutTunnel, t), max(-spheresCutPlane, max(-cylindersCutPlane, plane)));
    float dist = min(resultCylinders, plane);
    float occ = min(cuttedCylinders, plane);
    float id = 0.0;
    if(lightsGeom < epsilon) id = 1.0;
    return vec4(dist, id, light, occ);
}

// Helper functions now take accumulatedPistonPhase
vec3 rayMarch(vec3 rayDir, vec3 cameraOrigin, float currentSpeed, float currentTunnelScale, float accumulatedPistonPhase) {
    const int maxItter = 100;
    const float maxDist = 30.0;
    float totalDist = 0.0;
    vec3 pos = cameraOrigin;
    vec4 distVal = vec4(epsilon);
    for(int i = 0; i < maxItter; i++) {
        // Pass currentSpeed as the mainSpeedArg to distfunc (which is unused in current distfunc)
        distVal = distfunc(pos, accumulatedPistonPhase, currentTunnelScale, currentSpeed);
        totalDist += distVal.x;
        pos += distVal.x * rayDir;
        if(distVal.x < epsilon || totalDist > maxDist) break;
    }
    return vec3(distVal.x, totalDist, distVal.y);
}

vec3 rayMarchReflection(vec3 rayDir, vec3 cameraOrigin, float currentSpeed, float currentTunnelScale, float accumulatedPistonPhase) {
    const int maxItter = 30;
    const float maxDist = 20.0;
    float totalDist = 0.0;
    vec3 pos = cameraOrigin;
    vec4 distVal = vec4(epsilon);
    for(int i = 0; i < maxItter; i++) {
        distVal = distfunc(pos, accumulatedPistonPhase, currentTunnelScale, currentSpeed);
        totalDist += distVal.x;
        pos += distVal.x * rayDir;
        if(distVal.x < epsilon || totalDist > maxDist) break;
    }
    return vec3(distVal.x, totalDist, distVal.y);
}

vec2 AOandFakeAreaLights(vec3 pos, vec3 n, float currentSpeed, float currentTunnelScale, float accumulatedPistonPhase) {
    vec4 res = vec4(0.0);
    for(int i = 0; i < 3; i++) {
        vec3 aopos = pos + n * 0.3 * float(i);
        vec4 d = distfunc(aopos, accumulatedPistonPhase, currentTunnelScale, currentSpeed);
        res += d;
    }
    float ao = clamp(res.w, 0.0, 1.0);
    float light = 1.0 - clamp(res.z * 0.3, 0.0, 1.0);
    return vec2(ao, light * ao);
}

mat3 setCamera(in vec3 ro, in vec3 ta, float cr) {
    vec3 cw = normalize(ta - ro);
    vec3 cp = vec3(sin(cr), cos(cr), 0.0);
    vec3 cu = normalize(cross(cw, cp));
    vec3 cv = normalize(cross(cu, cw));
    return mat3(cu, cv, cw);
}

vec4 norcurv(in vec3 p, float currentSpeed, float currentTunnelScale, float accumulatedPistonPhase) {
    vec2 e = vec2(-epsilon, epsilon);
    float t1 = distfunc(p + e.yxx, accumulatedPistonPhase, currentTunnelScale, currentSpeed).x;
    float t2 = distfunc(p + e.xxy, accumulatedPistonPhase, currentTunnelScale, currentSpeed).x;
    float t3 = distfunc(p + e.xyx, accumulatedPistonPhase, currentTunnelScale, currentSpeed).x;
    float t4 = distfunc(p + e.yyy, accumulatedPistonPhase, currentTunnelScale, currentSpeed).x;
    float curv = 0.25/e.y * (t1 + t2 + t3 + t4 - 4.0 * distfunc(p, accumulatedPistonPhase, currentTunnelScale, currentSpeed).x);
    return vec4(normalize(e.yxx*t1 + e.xxy*t2 + e.xyx*t3 + e.yyy*t4), curv);
}

vec4 lighting(vec3 n, vec3 rayDir, vec3 reflectDir, vec3 pos, float lightTime, float lightAnimSpeed, vec3 mainLightCol, float mainLightIntensity) {
    vec3 lightPos = vec3(0.0, 0.0, 2.0 + lightTime * lightAnimSpeed);
    vec3 lightVec = lightPos - pos;
    vec3 lightDir = normalize(lightVec);
    float atten = clamp(1.0 - length(lightVec) * 0.1, 0.0, 1.0);
    float spec = pow(max(0.0, dot(reflectDir, lightDir)), 10.0);
    float rim = (1.0 - max(0.0, dot(-n, rayDir)));
    return vec4(spec * atten * mainLightCol * mainLightIntensity + rim * 0.2, rim);
}

vec3 color(float id, vec3 pos, vec3 mainLightCol, float mainLightIntensity) {
    vec2 fp = vec2(1.0) - linearStep2(vec2(0.0), vec2(0.01), abs(fract(pos.xz * vec2(0.25, 1.0) + vec2(0.0, 0.5)) - 0.5));
    float s = fp.y + fp.x;
    return mix(wallsColor + s * mainLightCol * 0.5 * mainLightIntensity, mainLightCol * mainLightIntensity, id);
}

vec4 finalColor(vec3 rayDir, vec3 reflectDir, vec3 pos, vec3 normal, float ao, float id, vec3 currentMainLightColor, float currentLightIntensity) {
    vec4 l = lighting(normal, rayDir, reflectDir, pos, 0.0, 0.0, currentMainLightColor, currentLightIntensity);
    vec3 col = color(id, pos, currentMainLightColor, currentLightIntensity);
    float ao1 = 0.5 * ao + 0.5;
    float ao2 = 0.25 * ao + 0.75;
    vec3 res = (mix(col * ao1, col, id) + l.xyz) * ao2;
    return vec4(res, l.w);
}

void main() {
    // Pass 0: Time and main speed smoothing
    if(PASSINDEX == 0) {
        vec4 prevData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        float smoothedSpeed = (FRAMEINDEX == 0) ? speed : mix(prevData.g, speed, min(1.0, TIMEDELTA * transitionSpeed));
        float accumulatedTime = (FRAMEINDEX == 0) ? 0.0 : prevData.r + smoothedSpeed * TIMEDELTA;
        gl_FragColor = vec4(accumulatedTime, smoothedSpeed, 0.0, 1.0);
        return;
    }
    // Pass 1: Parameter smoothing (tunnelScale, curvAmount, reflAmount)
    if(PASSINDEX == 1) {
        vec4 prevData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5));
        float ts = (FRAMEINDEX == 0) ? tunnelScale : mix(prevData.r, tunnelScale, min(1.0, TIMEDELTA * transitionSpeed));
        float ca = (FRAMEINDEX == 0) ? curvAmount : mix(prevData.g, curvAmount, min(1.0, TIMEDELTA * transitionSpeed));
        float ra = (FRAMEINDEX == 0) ? reflAmount : mix(prevData.b, reflAmount, min(1.0, TIMEDELTA * transitionSpeed));
        gl_FragColor = vec4(ts, ca, ra, 1.0);
        return;
    }
    // Pass 2: Piston speed and phase smoothing/accumulation
    if(PASSINDEX == 2) {
        vec4 prevData = IMG_NORM_PIXEL(pistonBuffer, vec2(0.5)); // .r = prev smoothed pistonSpeed, .g = prev pistonPhase
        
        float smoothedPistonSpeed = (FRAMEINDEX == 0) ? pistonSpeed : mix(prevData.r, pistonSpeed, min(1.0, TIMEDELTA * transitionSpeed));
        // Accumulate phase using the newly smoothed pistonSpeed. Consider a scaling factor if phase change is too fast/slow.
        // float phaseIncrementFactor = 10.0; // Example: makes phase change faster for given pistonSpeed values
        float accumulatedPistonPhase = (FRAMEINDEX == 0) ? 0.0 : prevData.g + smoothedPistonSpeed * TIMEDELTA; // * phaseIncrementFactor;
        
        gl_FragColor = vec4(smoothedPistonSpeed, accumulatedPistonPhase, 0.0, 1.0);
        return;
    }
    // Pass 3: Light intensity smoothing
    if(PASSINDEX == 3) {
        vec4 prevData = IMG_NORM_PIXEL(lightParamBuffer, vec2(0.5));
        float li = (FRAMEINDEX == 0) ? lightIntensity : mix(prevData.r, lightIntensity, min(1.0, TIMEDELTA * transitionSpeed));
        gl_FragColor = vec4(li, 0.0, 0.0, 1.0);
        return;
    }
    // Pass 4: Light color smoothing
    if(PASSINDEX == 4) {
        vec4 prevData = IMG_NORM_PIXEL(lightColorBuffer, vec2(0.5));
        vec3 lc = (FRAMEINDEX == 0) ? mainLightColor.rgb : mix(prevData.rgb, mainLightColor.rgb, min(1.0, TIMEDELTA * transitionSpeed));
        gl_FragColor = vec4(lc, 1.0);
        return;
    }
    // Pass 5: Render 3D Scene to sceneBuffer
    if(PASSINDEX == 5) {
        vec4 tdata = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));             // .r = currentTime, .g = currentSpeed
        vec4 pdata = IMG_NORM_PIXEL(paramBuffer, vec2(0.5));            // .r = currentTunnelScale, .g = currentCurvAmount, .b = currentReflAmount
        vec4 pistondata = IMG_NORM_PIXEL(pistonBuffer, vec2(0.5));      // .r = currentSmoothedPistonSpeed, .g = currentPistonPhase
        vec4 lparam = IMG_NORM_PIXEL(lightParamBuffer, vec2(0.5));      // .r = currentLightIntensity
        vec4 lcolor = IMG_NORM_PIXEL(lightColorBuffer, vec2(0.5));      // .rgb = currentMainLightColor
        
        float currentTime = tdata.r; 
        float currentSpeed = tdata.g; 
        float currentTunnelScale = pdata.r;
        float currentCurvAmount = pdata.g;
        float currentReflAmount = pdata.b;
        // float currentSmoothedPistonSpeed = pistondata.r; // Still available if needed elsewhere
        float currentPistonPhase = pistondata.g; 
        float currentLightIntensity = lparam.r;
        vec3 currentMainLightColor = lcolor.rgb;

        vec2 uv = isf_FragNormCoord.xy;
        float move = currentTime; 
        vec2 sinMove = sin((move * pi) / 16.0 + vec2(1.0, -1.0)) * vec2(5.0, 0.35);
        vec3 cameraOrigin = vec3(sinMove.x, 0.0, -5.0 + move);
        vec3 cameraTarget = vec3(0.0, 0.0, cameraOrigin.z + 10.0);
        vec2 screenPos = uv * 2.0 - 1.0;
        screenPos.x *= RENDERSIZE.x / RENDERSIZE.y;
        mat3 cam = setCamera(cameraOrigin, cameraTarget, sinMove.y);
        vec3 rayDir = cam * normalize(vec3(screenPos.xy, 1.0));

        // Pass currentPistonPhase to rayMarch and other helpers
        vec3 distResult = rayMarch(rayDir, cameraOrigin, currentSpeed, currentTunnelScale, currentPistonPhase);

        vec3 resColor;
        if(distResult.x < epsilon) {
            vec3 pos = cameraOrigin + distResult.y * rayDir;
            vec4 n = norcurv(pos, currentSpeed, currentTunnelScale, currentPistonPhase);
            vec2 ao = AOandFakeAreaLights(pos, n.xyz, currentSpeed, currentTunnelScale, currentPistonPhase);
            vec3 r = reflect(rayDir, n.xyz);
            vec3 rpos = pos + n.xyz * 0.02;
            vec3 reflectDist = rayMarchReflection(r, rpos, currentSpeed, currentTunnelScale, currentPistonPhase);
            float fog = clamp(1.0 / exp(distResult.y * 0.15), 0.0, 1.0);
            
            vec4 direct = finalColor(rayDir, r, pos, n.xyz, ao.x, distResult.z, currentMainLightColor, currentLightIntensity) + n.w * currentCurvAmount;
            vec3 reflFinal = vec3(0.0);
            if(reflectDist.x < epsilon) {
                vec3 reflPos = rpos + reflectDist.y * r;
                vec4 reflN = norcurv(reflPos, currentSpeed, currentTunnelScale, currentPistonPhase);
                vec2 reflAO = AOandFakeAreaLights(reflPos, reflN.xyz, currentSpeed, currentTunnelScale, currentPistonPhase);
                vec3 rr = reflect(r, reflN.xyz);
                vec4 refl = finalColor(r, rr, reflPos, reflN.xyz, reflAO.x, reflectDist.z, currentMainLightColor, currentLightIntensity);
                vec3 reflAreaLights = reflAO.y * currentMainLightColor * 0.5 * currentLightIntensity;
                reflFinal = (refl.xyz + reflN.w * currentCurvAmount + reflAreaLights) * clamp(1.0 / exp(reflectDist.y * 0.2), 0.0, 1.0) * currentReflAmount * direct.w;
            }
            vec3 areaLightsColor = ao.y * currentMainLightColor * 0.5 * currentLightIntensity;
            resColor = mix(fogColor, direct.xyz + reflFinal + areaLightsColor, fog);
        } else {
            resColor = fogColor;
        }
        
        gl_FragColor = vec4(resColor, 1.0);
        return;
    }
    // Pass 6: Radial Blur and Vignette
    if(PASSINDEX == 6) {
        vec2 uv = isf_FragNormCoord.xy;
        vec4 sceneColor = IMG_NORM_PIXEL(sceneBuffer, uv); 
        vec4 finalColorValue; 

        if (radialBlurIntensity <= 0.0001) { 
            finalColorValue = sceneColor;
        } else {
            vec2 blurCenter = vec2(0.5, 0.5);
            vec2 toCenter = blurCenter - uv; 

            vec4 accumulatedColor = vec4(0.0);
            const int NUM_SAMPLES = 16; 

            for (int i = 0; i < NUM_SAMPLES; ++i) {
                float sampleFraction = float(i) / float(NUM_SAMPLES - 1); 
                vec2 offset = toCenter * sampleFraction * radialBlurIntensity;
                vec2 sampleUV = uv + offset;
                
                accumulatedColor += IMG_NORM_PIXEL(sceneBuffer, clamp(sampleUV, vec2(0.0), vec2(1.0)));
            }
            finalColorValue = accumulatedColor / float(NUM_SAMPLES);
        }

        float vignette = 1.0 - smoothstep(0.5, 1.5, length(uv - 0.5) * 1.5);
        vec3 resWithVignette = finalColorValue.rgb * vignette;

        gl_FragColor = vec4(resWithVignette, 1.0);
        return;
    }
}
