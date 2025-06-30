/*{
    "CATEGORIES": [
        "GENERATOR"
    ],
    "CREDIT": "Original by ShaderToy, ISF Version @dot2dot",
    "DESCRIPTION": "Abstract Iterative Fractal Explorer with Dampened Controls, Optimized Ray Calculation, Reduced Buffer Complexity, and Multiple Colors.",
    "IMPORTED": {
        "fractalParamsBuffer": {
            "PASSINDEX": 1
        },
        "primaryColorBuffer": {
            "PASSINDEX": 2
        },
        "secondaryColorBuffer": {
            "PASSINDEX": 3
        },
        "timeAndDetailBuffer": {
            "PASSINDEX": 0
        }
    },
    "INPUTS": [
        {
            "DEFAULT": 0.2,
            "LABEL": "Animation Speed",
            "MAX": 10,
            "MIN": 0,
            "NAME": "timeScale",
            "TYPE": "float"
        },
        {
            "DEFAULT": 30,
            "LABEL": "Main Iterations",
            "MAX": 70,
            "MIN": 10,
            "NAME": "iterationsMain",
            "TYPE": "float"
        },
        {
            "DEFAULT": 6,
            "LABEL": "Detail Iterations",
            "MAX": 20,
            "MIN": 1,
            "NAME": "iterationsDetail",
            "TYPE": "float"
        },
        {
            "DEFAULT": 5000000,
            "LABEL": "Color Divisor (Affects Intensity)",
            "MAX": 10000000,
            "MIN": 100000,
            "NAME": "colorDivisor",
            "TYPE": "float"
        },
        {
            "DEFAULT": 0.4,
            "LABEL": "Julia Offset Factor",
            "MAX": 2,
            "MIN": 0,
            "NAME": "juliaOffsetFactor",
            "TYPE": "float"
        },
        {
            "DEFAULT": 0.6,
            "LABEL": "UV Time Scale Factor",
            "MAX": 2,
            "MIN": 0,
            "NAME": "uvTimeScaleFactor",
            "TYPE": "float"
        },
        {
            "DEFAULT": 5e-04,
            "LABEL": "Detail Stability Epsilon",
            "MAX": 0.1,
            "MIN": 1e-05,
            "NAME": "detailEpsilon",
            "TYPE": "float"
        },
        {
            "DEFAULT": [
                0.1,
                0.2,
                0.8,
                1
            ],
            "LABEL": "Primary Color",
            "NAME": "primaryColorInput",
            "TYPE": "color"
        },
        {
            "DEFAULT": [
                0.9,
                0.7,
                0.1,
                1
            ],
            "LABEL": "Secondary Color",
            "NAME": "secondaryColorInput",
            "TYPE": "color"
        },
        {
            "DEFAULT": 1,
            "LABEL": "Transition Smoothness",
            "MAX": 10,
            "MIN": 0.1,
            "NAME": "transitionSpeed",
            "TYPE": "float"
        }
    ],
    "ISFVSN": "2",
    "PASSES": [
        {
            "FLOAT": true,
            "HEIGHT": 1,
            "PERSISTENT": true,
            "TARGET": "timeAndDetailBuffer",
            "WIDTH": 1
        },
        {
            "FLOAT": true,
            "HEIGHT": 1,
            "PERSISTENT": true,
            "TARGET": "fractalParamsBuffer",
            "WIDTH": 1
        },
        {
            "FLOAT": true,
            "HEIGHT": 1,
            "PERSISTENT": true,
            "TARGET": "primaryColorBuffer",
            "WIDTH": 1
        },
        {
            "FLOAT": true,
            "HEIGHT": 1,
            "PERSISTENT": true,
            "TARGET": "secondaryColorBuffer",
            "WIDTH": 1
        },
        {
            "TARGET": "finalOutput"
        }
    ]
}
*/

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

// Helper function for fractal transformation
// Accepts currentDetailEpsilon to use the smoothed value
float fractalTransform(inout vec3 p_inout, float currentDetailEpsilon) {
    p_inout = abs(sin(p_inout)) - 1.0;
    // Using a configurable epsilon (currentDetailEpsilon)
    return 1.3 / max(currentDetailEpsilon, dot(p_inout, p_inout));
}

// Optimized Main color accumulation logic, now returns vec3 for color
// Accepts currentDetailEpsilon to pass to fractalTransform
// Accepts effectivePrimaryColor and effectiveSecondaryColor for color mixing
vec3 accumulateColor(vec2 fragCoord_pixel, float currentTime, vec2 currentRenderSize,
                      float mainLoopIterations, float detailLoopIterations, float colorAccumDivisor,
                      float currentJuliaOffsetFactor, float currentUvTimeScaleFactor, float currentDetailEpsilon,
                      vec3 effectivePrimaryColor, vec3 effectiveSecondaryColor) {
    float z_depth = 0.0;
    float s_boundary;
    float intensity_accumulator = 0.0; // Renamed from accumulated_color_val
    float l_scale;

    // --- Optimization: Pre-calculate the base normalized ray direction ---
    float time_uv_dynamic_scale = 2.0 + sin(currentTime * currentUvTimeScaleFactor);
    vec3 base_ray_direction_unnormalized =
        vec3(fragCoord_pixel * time_uv_dynamic_scale, 0.0) -
        vec3(currentRenderSize.x, currentRenderSize.y, currentRenderSize.x);

    vec3 base_normalized_ray_dir;
    if (length(base_ray_direction_unnormalized) < 0.00001) {
        base_normalized_ray_dir = vec3(0.0, 0.0, 1.0);
    } else {
        base_normalized_ray_dir = normalize(base_ray_direction_unnormalized);
    }
    // --- End Optimization ---

    for (float i = 0.0; i < mainLoopIterations; i++) {
        vec3 p_ray = z_depth * base_normalized_ray_dir;
        p_ray += sin(currentTime + p_ray.yzx) * currentJuliaOffsetFactor;
        s_boundary = 3.0 - length(p_ray.xy - vec2(z_depth));

        float w_inverse_scale_acc = 1.0;
        for (float d_counter = 0.0; d_counter < detailLoopIterations; d_counter++) {
            l_scale = fractalTransform(p_ray, currentDetailEpsilon);
            p_ray *= l_scale;
            w_inverse_scale_acc *= l_scale;
        }

        float ray_step_dist = max(length(p_ray) / max(0.00001, w_inverse_scale_acc), s_boundary);
        z_depth += ray_step_dist;
        intensity_accumulator += (z_depth / ray_step_dist) / colorAccumDivisor;

        if (z_depth > 10000.0 || isnan(z_depth) || isinf(z_depth) || isnan(intensity_accumulator) || isinf(intensity_accumulator)) {
            break;
        }
    }
    // Apply tanh to compress intensity range for mixing
    float mix_factor = tanh(intensity_accumulator);
    // Ensure mix_factor is in [0,1] if tanh output is [-1,1]
    // mix_factor = (mix_factor + 1.0) * 0.5; // If primary is background and secondary is foreground
    // Or, if tanh_accumulator is mostly positive, simple clamp might be okay, or just use it.
    // For now, let's assume tanh gives a good 0-1 range or that negative values map to primaryColor.
    // A common use of tanh for color intensity is to keep it as is, as it saturates.
    
    return mix(effectivePrimaryColor, effectiveSecondaryColor, clamp(mix_factor, 0.0, 1.0));
}

void main() {
    // Variables for storing previous buffer data
    vec4 prevTimeAndDetailData, prevFractalParamsData, prevPrimaryColorData, prevSecondaryColorData;

    // Variables for current smoothed values
    float newAccumulatedTime, newSmoothedTimeScale, newSmoothedUvTimeScaleFactor, newSmoothedDetailEpsilon;
    float newSmoothedIterationsMain, newSmoothedIterationsDetail, newSmoothedColorDivisor, newSmoothedJuliaOffsetFactor;
    vec3 newSmoothedPrimaryColor, newSmoothedSecondaryColor;

    // Blend factor for smoothing transitions
    float blendFactor = min(1.0, TIMEDELTA * transitionSpeed);

    if (PASSINDEX == 0) { // timeAndDetailBuffer Update
        // .r: newAccumulatedTime, .g: newSmoothedTimeScale, .b: newSmoothedUvTimeScaleFactor, .a: newSmoothedDetailEpsilon
        prevTimeAndDetailData = IMG_NORM_PIXEL(timeAndDetailBuffer, vec2(0.5, 0.5));
        if (FRAMEINDEX == 0) {
            newAccumulatedTime = 0.0;
            newSmoothedTimeScale = timeScale;
            newSmoothedUvTimeScaleFactor = uvTimeScaleFactor;
            newSmoothedDetailEpsilon = detailEpsilon;
        } else {
            newSmoothedTimeScale = mix(prevTimeAndDetailData.g, timeScale, blendFactor);
            newSmoothedUvTimeScaleFactor = mix(prevTimeAndDetailData.b, uvTimeScaleFactor, blendFactor);
            newSmoothedDetailEpsilon = mix(prevTimeAndDetailData.a, detailEpsilon, blendFactor);
            newAccumulatedTime = prevTimeAndDetailData.r + newSmoothedTimeScale * TIMEDELTA;
        }
        gl_FragColor = vec4(newAccumulatedTime, newSmoothedTimeScale, newSmoothedUvTimeScaleFactor, newSmoothedDetailEpsilon);

    } else if (PASSINDEX == 1) { // FractalParamsBuffer Update
        // .r: iterMain, .g: iterDetail, .b: colorDiv, .a: juliaOffset
        prevFractalParamsData = IMG_NORM_PIXEL(fractalParamsBuffer, vec2(0.5, 0.5));
        if (FRAMEINDEX == 0) {
            newSmoothedIterationsMain = iterationsMain;
            newSmoothedIterationsDetail = iterationsDetail;
            newSmoothedColorDivisor = colorDivisor;
            newSmoothedJuliaOffsetFactor = juliaOffsetFactor;
        } else {
            newSmoothedIterationsMain = mix(prevFractalParamsData.r, iterationsMain, blendFactor);
            newSmoothedIterationsDetail = mix(prevFractalParamsData.g, iterationsDetail, blendFactor);
            newSmoothedColorDivisor = mix(prevFractalParamsData.b, colorDivisor, blendFactor);
            newSmoothedJuliaOffsetFactor = mix(prevFractalParamsData.a, juliaOffsetFactor, blendFactor);
        }
        gl_FragColor = vec4(newSmoothedIterationsMain, newSmoothedIterationsDetail, newSmoothedColorDivisor, newSmoothedJuliaOffsetFactor);

    } else if (PASSINDEX == 2) { // primaryColorBuffer Update
        prevPrimaryColorData = IMG_NORM_PIXEL(primaryColorBuffer, vec2(0.5, 0.5));
        if (FRAMEINDEX == 0) {
            newSmoothedPrimaryColor = primaryColorInput.rgb;
        } else {
            newSmoothedPrimaryColor = mix(prevPrimaryColorData.rgb, primaryColorInput.rgb, blendFactor);
        }
        gl_FragColor = vec4(newSmoothedPrimaryColor, 1.0); // Alpha is unused but required for vec4

    } else if (PASSINDEX == 3) { // secondaryColorBuffer Update
        prevSecondaryColorData = IMG_NORM_PIXEL(secondaryColorBuffer, vec2(0.5, 0.5));
        if (FRAMEINDEX == 0) {
            newSmoothedSecondaryColor = secondaryColorInput.rgb;
        } else {
            newSmoothedSecondaryColor = mix(prevSecondaryColorData.rgb, secondaryColorInput.rgb, blendFactor);
        }
        gl_FragColor = vec4(newSmoothedSecondaryColor, 1.0); // Alpha is unused

    } else { // PASSINDEX == 4 - Final Render Pass
        // Read all smoothed values from their respective buffers
        vec4 timeAndDetailData = IMG_NORM_PIXEL(timeAndDetailBuffer, vec2(0.5, 0.5));
        float effectiveTime = timeAndDetailData.r;
        float effectiveUvTimeScaleFactor = timeAndDetailData.b;
        float effectiveDetailEpsilon = timeAndDetailData.a;

        vec4 fractalParamsData = IMG_NORM_PIXEL(fractalParamsBuffer, vec2(0.5, 0.5));
        float effectiveIterationsMain = fractalParamsData.r;
        float effectiveIterationsDetail = fractalParamsData.g;
        float effectiveColorDivisor = fractalParamsData.b;
        float effectiveJuliaOffsetFactor = fractalParamsData.a;

        vec3 effectivePrimaryColor = IMG_NORM_PIXEL(primaryColorBuffer, vec2(0.5,0.5)).rgb;
        vec3 effectiveSecondaryColor = IMG_NORM_PIXEL(secondaryColorBuffer, vec2(0.5,0.5)).rgb;

        // Call the main color accumulation function with all effective (smoothed) parameters
        vec3 final_color_rgb = accumulateColor(gl_FragCoord.xy, effectiveTime, RENDERSIZE.xy,
                                            effectiveIterationsMain, effectiveIterationsDetail, effectiveColorDivisor,
                                            effectiveJuliaOffsetFactor, effectiveUvTimeScaleFactor, effectiveDetailEpsilon,
                                            effectivePrimaryColor, effectiveSecondaryColor);

        // Set the final pixel color
        gl_FragColor = vec4(final_color_rgb, 1.0);
    }
}
