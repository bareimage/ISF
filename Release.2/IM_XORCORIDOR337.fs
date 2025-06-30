/*{
    "DESCRIPTION": "Corridor Raymarcher - ISF port with smoothed controls. Based on XorDev's Shadertoy original.",
    "CREDIT": "Original by @XorDev, ISF conversion and modifications by @dot2dot - (bareimage)",
    "ISFVSN": "2.0",
    "CATEGORIES": ["3D", "Raymarching", "Generator"],
    "INPUTS": [
        { "NAME": "animSpeed", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 20.0, "LABEL": "Animation Speed" },
        { "NAME": "transitionSpeed", "TYPE": "float", "DEFAULT": 2.0, "MIN": 0.1, "MAX": 10.0, "LABEL": "Parameter Smoothing" },
        { "NAME": "raymarchSteps", "TYPE": "float", "DEFAULT": 100.0, "MIN": 10.0, "MAX": 250.0, "LABEL":"Raymarch Steps"},
        { "NAME": "wobbleFreq1", "TYPE": "float", "DEFAULT": 3.1, "MIN": 0.1, "MAX": 20.0, "LABEL": "Wobble Freq 1" },
        { "NAME": "wobbleFreq2", "TYPE": "float", "DEFAULT": 2.0, "MIN": 0.1, "MAX": 20.0, "LABEL": "Wobble Freq 2" },
        { "NAME": "wobbleAmplitude", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 5.0, "LABEL": "Wobble Amplitude" },
        { "NAME": "emissiveFactor", "TYPE": "float", "DEFAULT": 0.03, "MIN": 0.001, "MAX": 0.5, "LABEL": "Emissive Factor" },
        { "NAME": "brightnessFactor", "TYPE": "float", "DEFAULT": 0.9, "MIN": 0.1, "MAX": 10.0, "LABEL": "Brightness Falloff" },
        { "NAME": "toneExposure", "TYPE": "float", "DEFAULT": 300.0, "MIN": 300.0, "MAX": 10.0, "LABEL": "Tone Exposure" },
        { "NAME": "zoomEffect", "TYPE": "float", "DEFAULT": 0.0, "MIN": -1000.0, "MAX": 1000.0, "LABEL":"Ray Z Offset"},
        { "NAME": "colorBaseR", "TYPE": "float", "DEFAULT": 1.2, "MIN": 0.0, "MAX": 3.0, "LABEL": "Color Base R" },
        { "NAME": "colorBaseG", "TYPE": "float", "DEFAULT": 1.2, "MIN": 0.0, "MAX": 3.0, "LABEL": "Color Base G" },
        { "NAME": "colorBaseB", "TYPE": "float", "DEFAULT": 1.2, "MIN": 0.0, "MAX": 3.0, "LABEL": "Color Base B" },
        { "NAME": "colorSpread", "TYPE": "float", "DEFAULT": 1.0, "MIN": -2.0, "MAX": 2.0, "LABEL": "Color Spread" }
    ],
    "PASSES": [
        { "TARGET": "speedBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
        { "TARGET": "wobbleCtrlBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
        { "TARGET": "appearanceBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
        { "TARGET": "colorControlBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
        { "TARGET": "finalOutput" }
    ]
}*/

#define PI 3.14159265359
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

// Helper function for smoothing parameters
float smoothParam(float prevVal, float inputVal, float transSpeed) {
    if (FRAMEINDEX == 0) return inputVal;
    return mix(prevVal, inputVal, min(1.0, TIMEDELTA * transSpeed));
}

vec4 renderCorridorEffect(
    vec2 fragCoord_pixels,
    float currentTime,
    float currentRaymarchSteps,
    float currentWobbleFreq1,
    float currentWobbleFreq2,
    float currentWobbleAmplitude,
    float currentEmissiveFactor,
    float currentBrightnessFactor,
    float currentToneExposure,
    float currentZoomEffect,
    vec3 currentColorBase,
    float currentColorSpread
) {
    vec4 O = vec4(0.0);
    float t = currentTime;
    float i = 0.0, d, z = 0.0;
    int steps = int(currentRaymarchSteps);

    for(; i++<float(steps);) { // Loop for specified steps
        // Ray direction using pixel coordinates and zoom effect
        vec3 ray_dir = normalize(vec3(fragCoord_pixels + fragCoord_pixels, currentZoomEffect) - RENDERSIZE.xyy);
        vec3 p = z * ray_dir;

        vec3 w_calc = abs(ray_dir);
        // Prevent division by zero for w_calc
        w_calc /= (max(w_calc.x, max(w_calc.y, 1e-5)) + 1e-5); // Ensure divisor isn't zero, and also ensure it's not just max(x,y) if both are tiny.

        w_calc.z += t;
        p.z -= t;

        vec3 r_temp = ++p; // Pre-increment p, store in r_temp
        vec3 p_for_sdf = p; // Use a copy for SDF modification

        // Distance calculation with controllable wobble
        d = length(
            (p_for_sdf.xy = abs(mod(p_for_sdf.xy - 2.0, 4.0) - 2.0)) - 1.0
            + currentWobbleAmplitude * cos(p_for_sdf.z / vec2(currentWobbleFreq1, currentWobbleFreq2))
        )
        + 0.1 * length(p_for_sdf - r_temp)
        * exp(dot(cos(ceil(w_calc /= 0.3)), sin(w_calc / 0.6).yzx));

        z += d; // Accumulate distance marched

        // Accumulate color if hit is close enough
        if (d > 0.001) { // Original was d > 0.0, slightly increased for safety / small emissive factors
            O.rgb += ( (cos(p_for_sdf) * currentColorSpread + currentColorBase) ) / (d + currentEmissiveFactor) / (z + currentBrightnessFactor);
        } else {
            // Optional: Add a very bright color if d is extremely small (surface hit)
            // O.rgb += (currentColorBase * 10.0) / (z + currentBrightnessFactor);
        }
         if (z > 20.0) break; // Bail out if we've marched too far
    }

    // Tonemapping
    O = tanh(O / currentToneExposure);
    return vec4(O.rgb, 1.0); // Ensure alpha is 1.0
}

void main() {
    // Pass 0: Speed parameter smoothing and time accumulation
    if (PASSINDEX == 0) {
        vec4 prevSpeedData = IMG_NORM_PIXEL(speedBuffer, vec2(0.5));
        float smoothedAnimSpeed = smoothParam(prevSpeedData.g, animSpeed, transitionSpeed);
        float accumulatedGeneralAnimTime = (FRAMEINDEX == 0) ? 0.0 : prevSpeedData.r + smoothedAnimSpeed * TIMEDELTA;
        gl_FragColor = vec4(accumulatedGeneralAnimTime, smoothedAnimSpeed, 0.0, 0.0); // Store time and smoothed speed
        return;
    }

    // Pass 1: Wobble Control parameters smoothing
    if (PASSINDEX == 1) {
        vec4 prevData = IMG_NORM_PIXEL(wobbleCtrlBuffer, vec2(0.5));
        float smoothedWobbleFreq1 = smoothParam(prevData.r, wobbleFreq1, transitionSpeed);
        float smoothedWobbleFreq2 = smoothParam(prevData.g, wobbleFreq2, transitionSpeed);
        float smoothedWobbleAmplitude = smoothParam(prevData.b, wobbleAmplitude, transitionSpeed);
        float smoothedRaymarchSteps = smoothParam(prevData.a, raymarchSteps, transitionSpeed);
        gl_FragColor = vec4(smoothedWobbleFreq1, smoothedWobbleFreq2, smoothedWobbleAmplitude, smoothedRaymarchSteps);
        return;
    }

    // Pass 2: Appearance parameters smoothing
    if (PASSINDEX == 2) {
        vec4 prevData = IMG_NORM_PIXEL(appearanceBuffer, vec2(0.5));
        float smoothedEmissiveFactor = smoothParam(prevData.r, emissiveFactor, transitionSpeed);
        float smoothedBrightnessFactor = smoothParam(prevData.g, brightnessFactor, transitionSpeed);
        float smoothedToneExposure = smoothParam(prevData.b, toneExposure, transitionSpeed);
        float smoothedZoomEffect = smoothParam(prevData.a, zoomEffect, transitionSpeed);
        gl_FragColor = vec4(smoothedEmissiveFactor, smoothedBrightnessFactor, smoothedToneExposure, smoothedZoomEffect);
        return;
    }

    // Pass 3: Color Control parameters smoothing
    if (PASSINDEX == 3) {
        vec4 prevData = IMG_NORM_PIXEL(colorControlBuffer, vec2(0.5));
        float smoothedColorBaseR = smoothParam(prevData.r, colorBaseR, transitionSpeed);
        float smoothedColorBaseG = smoothParam(prevData.g, colorBaseG, transitionSpeed);
        float smoothedColorBaseB = smoothParam(prevData.b, colorBaseB, transitionSpeed);
        float smoothedColorSpread = smoothParam(prevData.a, colorSpread, transitionSpeed);
        gl_FragColor = vec4(smoothedColorBaseR, smoothedColorBaseG, smoothedColorBaseB, smoothedColorSpread);
        return;
    }

    // Final Pass: Main rendering (PASSINDEX == 4)
    if (PASSINDEX == 4) {
        vec2 fragCoord_pixels = isf_FragNormCoord.xy * RENDERSIZE.xy;

        vec4 speedData = IMG_NORM_PIXEL(speedBuffer, vec2(0.5));
        vec4 wobbleData = IMG_NORM_PIXEL(wobbleCtrlBuffer, vec2(0.5));
        vec4 appearanceData = IMG_NORM_PIXEL(appearanceBuffer, vec2(0.5));
        vec4 colorData = IMG_NORM_PIXEL(colorControlBuffer, vec2(0.5));

        float currentTime = speedData.r;
        // float currentAnimSpeed = speedData.g; // Available if needed

        float currentWobbleFreq1 = wobbleData.r;
        float currentWobbleFreq2 = wobbleData.g;
        float currentWobbleAmplitude = wobbleData.b;
        float currentRaymarchSteps = wobbleData.a;

        float currentEmissiveFactor = appearanceData.r;
        float currentBrightnessFactor = appearanceData.g;
        float currentToneExposure = appearanceData.b;
        float currentZoomEffect = appearanceData.a;

        vec3 currentColorBase = vec3(colorData.r, colorData.g, colorData.b);
        float currentColorSpread = colorData.a;

        gl_FragColor = renderCorridorEffect(
            fragCoord_pixels,
            currentTime,
            currentRaymarchSteps,
            currentWobbleFreq1,
            currentWobbleFreq2,
            currentWobbleAmplitude,
            currentEmissiveFactor,
            currentBrightnessFactor,
            currentToneExposure,
            currentZoomEffect,
            currentColorBase,
            currentColorSpread
        );
        return;
    }
}
