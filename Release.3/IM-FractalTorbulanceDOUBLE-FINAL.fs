/*{
  "DESCRIPTION": "Fractal turbulence with dual planes and raymarching, smooth parameter transitions",
  "CREDIT": "Original ShaderToy by @diatribes, ISF Version by @dot2dot",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR"],
  "INPUTS": [
    {
      "NAME": "speed",
      "TYPE": "float",
      "DEFAULT": 2.0,
      "MIN": 0.0,
      "MAX": 50.0,
      "LABEL": "Animation Speed"
    },
    {
      "NAME": "noiseScale",
      "TYPE": "float",
      "DEFAULT": 4.0,
      "MIN": 1.0,
      "MAX": 10.0,
      "LABEL": "Noise Scale"
    },
    {
      "NAME": "planeOffset",
      "TYPE": "float",
      "DEFAULT": 0.2,
      "MIN": 0.0,
      "MAX": 1.0,
      "LABEL": "Plane Offset"
    },
    {
      "NAME": "groundHeight",
      "TYPE": "float",
      "DEFAULT": 2.5,
      "MIN": 1.0,
      "MAX": 5.0,
      "LABEL": "Ground Height"
    },
    {
      "NAME": "stepSize",
      "TYPE": "float",
      "DEFAULT": 0.04,
      "MIN": 0.01,
      "MAX": 0.1,
      "LABEL": "Step Size"
    },
    {
      "NAME": "colorIntensity",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.1,
      "MAX": 3.0,
      "LABEL": "Color Intensity"
    },
    {
      "NAME": "transitionSpeed",
      "TYPE": "float",
      "DEFAULT": 2.0,
      "MIN": 0.1,
      "MAX": 10.0,
      "LABEL": "Transition Smoothness"
    }
  ],
  "PASSES": [
    {
      "TARGET": "timeBuffer",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": 1,
      "HEIGHT": 1
    },
    {
      "TARGET": "paramBuffer1",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": 1,
      "HEIGHT": 1
    },
    {
      "TARGET": "paramBuffer2",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": 1,
      "HEIGHT": 1
    },
    {
      "TARGET": "finalOutput"
    }
  ]
}*/

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

// Custom implementation of tanh function
vec4 tanh_custom(vec4 x) {
    // Clamp input to exp to prevent overflow to INF, which would lead to NaN
    // exp(y) overflows around y=88. Since we use exp(2.0*x), 2.0*x should be < 88, so x < 44.
    // We already clamp x to a safer range before calling this, but an internal clamp is robust.
    // However, the primary clamp should be done *before* calling tanh_custom on the scaled vector.
    vec4 exp2x = exp(2.0 * x);
    return (exp2x - 1.0) / (exp2x + 1.0);
}

void main() {
    // Declare variables at the top level for use in all passes
    vec4 prevTimeData, prevParamData1, prevParamData2;
    float accumulatedTime, currentSpeed, adjustedSpeed, newTime;
    float currentNoiseScale, currentPlaneOffset, currentGroundHeight;
    float currentStepSize, currentColorIntensity;
    float effectiveTime, effectiveNoiseScale, effectivePlaneOffset;
    float effectiveGroundHeight, effectiveStepSize, effectiveColorIntensity;
    
    if (PASSINDEX == 0) {
        // First pass: Update the accumulated time in the persistent buffer
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        
        // Extract previous accumulated time
        accumulatedTime = prevTimeData.r;
        currentSpeed = prevTimeData.g;
        
        // Calculate new accumulated time
        if (FRAMEINDEX == 0) {
            // Initialize time on first frame
            newTime = 0.0;
            adjustedSpeed = speed;
        } else {
            // Smoothly transition to target speed
            adjustedSpeed = mix(currentSpeed, speed, min(1.0, TIMEDELTA * transitionSpeed));
            newTime = accumulatedTime + adjustedSpeed * TIMEDELTA;
        }
        
        // Store the accumulated time and current speed
        gl_FragColor = vec4(newTime, adjustedSpeed, 0.0, 1.0);
    }
    else if (PASSINDEX == 1) {
        // Second pass: Update first set of parameters with smooth transitions
        prevParamData1 = IMG_NORM_PIXEL(paramBuffer1, vec2(0.5, 0.5));
        
        if (FRAMEINDEX == 0) {
            // Initialize on first frame
            currentNoiseScale = noiseScale;
            currentPlaneOffset = planeOffset;
            currentGroundHeight = groundHeight;
        } else {
            // Smoothly transition to target parameter values
            currentNoiseScale = prevParamData1.r;
            currentPlaneOffset = prevParamData1.g;
            currentGroundHeight = prevParamData1.b;
            
            currentNoiseScale = mix(currentNoiseScale, noiseScale, min(1.0, TIMEDELTA * transitionSpeed));
            currentPlaneOffset = mix(currentPlaneOffset, planeOffset, min(1.0, TIMEDELTA * transitionSpeed));
            currentGroundHeight = mix(currentGroundHeight, groundHeight, min(1.0, TIMEDELTA * transitionSpeed));
        }
        
        gl_FragColor = vec4(currentNoiseScale, currentPlaneOffset, currentGroundHeight, 1.0);
    }
    else if (PASSINDEX == 2) {
        // Third pass: Update second set of parameters with smooth transitions
        prevParamData2 = IMG_NORM_PIXEL(paramBuffer2, vec2(0.5, 0.5));
        
        if (FRAMEINDEX == 0) {
            // Initialize on first frame
            currentStepSize = stepSize;
            currentColorIntensity = colorIntensity;
        } else {
            // Smoothly transition to target parameter values
            currentStepSize = prevParamData2.r;
            currentColorIntensity = prevParamData2.g;
            
            currentStepSize = mix(currentStepSize, stepSize, min(1.0, TIMEDELTA * transitionSpeed));
            currentColorIntensity = mix(currentColorIntensity, colorIntensity, min(1.0, TIMEDELTA * transitionSpeed));
        }
        
        gl_FragColor = vec4(currentStepSize, currentColorIntensity, 0.0, 1.0);
    }
    else { // PASSINDEX == 3 (Final Render Pass)
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        prevParamData1 = IMG_NORM_PIXEL(paramBuffer1, vec2(0.5, 0.5));
        prevParamData2 = IMG_NORM_PIXEL(paramBuffer2, vec2(0.5, 0.5));
        
        effectiveTime = prevTimeData.r;
        effectiveNoiseScale = prevParamData1.r;
        effectivePlaneOffset = prevParamData1.g;
        effectiveGroundHeight = prevParamData1.b;
        effectiveStepSize = prevParamData2.r;
        effectiveColorIntensity = prevParamData2.g;
        
        float i = 0.0, d = 0.0, s_noise_accum_factor = 0.0; // s_noise_accum_factor is 's' from original noise loop
        float t = effectiveTime;
        // p_rendersize_init is the 'p' from original: vec3 q, p = vec3(RENDERSIZE, 0.0);
        vec3 p_rendersize_init = vec3(RENDERSIZE.x, RENDERSIZE.y, 0.0); 
        vec2 u = (gl_FragCoord.xy - p_rendersize_init.xy / 2.0) / p_rendersize_init.y;
        vec4 o = vec4(0.0);
        
        for(; i < 100.0; i++) {
            // p_current_march_pos is the 'p' (and 'q') from original: q = p = vec3(u * d, d + t);
            vec3 p_current_march_pos = vec3(u * d, d + t); 
            
            // The inner 'p' for noise calculation starts as p_current_march_pos
            vec3 p_for_noise = p_current_march_pos;

            for (s_noise_accum_factor = 0.03; s_noise_accum_factor < 2.0; s_noise_accum_factor += s_noise_accum_factor) {
                p_for_noise += abs(dot(sin(p_for_noise * s_noise_accum_factor * effectiveNoiseScale), vec3(0.035))) / s_noise_accum_factor;
            }
            
            // s_dist_step is the 's' from original: d += s = effectiveStepSize + ...
            float s_dist_step = effectiveStepSize + 0.6 * abs(min(effectivePlaneOffset - p_current_march_pos.y - cos(p_for_noise.x) * 0.2, effectiveGroundHeight + p_for_noise.y));
            d += s_dist_step;
            
            // Accumulate color. Add a small epsilon to s_dist_step in denominator to prevent division by zero if it gets extremely small.
            o += (1.0 / max(s_dist_step, 0.0001f)); 

            // Optional: Break if step is too small or d is too large (far clip)
            if (s_dist_step < 0.0001f || d > 200.0f) break; // Increased far clip slightly
        }
        
        // Apply tonemapping with color intensity control
        float sunAtten = length(u - vec2(0.1));
        // MODIFICATION: Prevent sunAtten from being too small to avoid excessive brightness / division by zero.
        // Adjust 0.02f as needed: smaller values = sharper/brighter sun, larger = softer/dimmer.
        sunAtten = max(sunAtten, 0.02f); 

        // Calculate base color before channel-specific multipliers and tanh
        // The division by 4000.0f is a scaling factor from the original.
        vec4 tonemapBase = o * (effectiveColorIntensity / 4000.0f) / sunAtten;
        
        // Input to tanh function, with channel-specific multipliers
        vec4 inputToTanh = vec4(4.0f, 2.0f, 1.0f, 1.0f) * tonemapBase;

        // MODIFICATION: CRITICAL FIX - Clamp the input to tanh_custom.
        // This prevents exp(2*x) in tanh_custom from receiving excessively large values,
        // which would cause it to return INF, leading to NaN in the division.
        // Max value for x in exp(2*x) before float overflow is approx 43-44. So clamp input to e.g. 40.
        const float TANH_INPUT_CLAMP_LIMIT = 40.0f;
        inputToTanh = clamp(inputToTanh, -TANH_INPUT_CLAMP_LIMIT, TANH_INPUT_CLAMP_LIMIT);
        
        o = tanh_custom(inputToTanh);
        
        // tanh_custom output is in [-1, 1]. Since inputs to tanh_custom should now be positive 
        // (due to o and other factors being positive, and then clamping to [0, TANH_INPUT_CLAMP_LIMIT]),
        // the output of tanh_custom will be in [0, ~1.0), which is suitable for gl_FragColor.
        gl_FragColor = o;
        gl_FragColor.a = 1.0; // Force opaque output
    }
}
