/*{
  "DESCRIPTION": "Ethereal waves with smooth parameter transitions",
  "CREDIT": "Original by @iapafoto, converted to ISF by dot2dot",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR"],
  "INPUTS": [
    {
      "NAME": "speed",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 5.0,
      "LABEL": "Animation Speed"
    },
    {
      "NAME": "waveAmplitude",
      "TYPE": "float",
      "DEFAULT": 0.2,
      "MIN": 0.05,
      "MAX": 0.5,
      "LABEL": "Wave Amplitude"
    },
    {
      "NAME": "waveFrequency",
      "TYPE": "float",
      "DEFAULT": 0.6,
      "MIN": 0.1,
      "MAX": 2.0,
      "LABEL": "Wave Frequency"
    },
    {
      "NAME": "verticalWave",
      "TYPE": "float",
      "DEFAULT": 0.03,
      "MIN": 0.0,
      "MAX": 0.1,
      "LABEL": "Vertical Wave Strength"
    },
    {
      "NAME": "fractalDetail",
      "TYPE": "float",
      "DEFAULT": 24.0,
      "MIN": 5.0,
      "MAX": 50.0,
      "LABEL": "Fractal Detail"
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
    },
    {
      "NAME": "exposureControl",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.5,
      "MAX": 5.0,
      "LABEL": "Exposure Control"
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
      "TARGET": "paramBuffer",
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

// Custom implementation of tanh function with exposure control
vec4 tanh_custom(vec4 x, float exposure) {
    // Apply exposure control to prevent overexposure
    x = x * exposure;
    vec4 exp2x = exp(2.0 * clamp(x, -20.0, 20.0)); // Clamp to prevent exp overflow
    return (exp2x - 1.0) / (exp2x + 1.0);
}

void main() {
    // Declare variables at the top level for use in all passes
    vec4 prevTimeData, prevParamData;
    float accumulatedTime, currentSpeed, adjustedSpeed, newTime;
    float currentWaveAmplitude, currentWaveFrequency, currentVerticalWave;
    float currentFractalDetail, currentColorIntensity;
    float effectiveTime, effectiveWaveAmplitude, effectiveWaveFrequency;
    float effectiveVerticalWave, effectiveFractalDetail, effectiveColorIntensity;
    
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
        // Second pass: Update the parameters with smooth transitions
        prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5, 0.5));
        
        if (FRAMEINDEX == 0) {
            // Initialize on first frame
            currentWaveAmplitude = waveAmplitude;
            currentWaveFrequency = waveFrequency;
            currentVerticalWave = verticalWave;
            currentFractalDetail = fractalDetail;
            currentColorIntensity = colorIntensity;
        } else {
            // Smoothly transition to target parameter values
            currentWaveAmplitude = prevParamData.r;
            currentWaveFrequency = prevParamData.g;
            currentVerticalWave = prevParamData.b;
            currentFractalDetail = prevParamData.a;
            
            // Apply smooth transitions
            currentWaveAmplitude = mix(currentWaveAmplitude, waveAmplitude, min(1.0, TIMEDELTA * transitionSpeed));
            currentWaveFrequency = mix(currentWaveFrequency, waveFrequency, min(1.0, TIMEDELTA * transitionSpeed));
            currentVerticalWave = mix(currentVerticalWave, verticalWave, min(1.0, TIMEDELTA * transitionSpeed));
            currentFractalDetail = mix(currentFractalDetail, fractalDetail, min(1.0, TIMEDELTA * transitionSpeed));
            currentColorIntensity = mix(currentColorIntensity, colorIntensity, min(1.0, TIMEDELTA * transitionSpeed));
        }
        
        gl_FragColor = vec4(currentWaveAmplitude, currentWaveFrequency, currentVerticalWave, currentFractalDetail);
    }
    else {
        // Final pass: Render the shader using the accumulated values
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5, 0.5));
        
        // Get the accumulated values
        effectiveTime = prevTimeData.r;
        effectiveWaveAmplitude = prevParamData.r;
        effectiveWaveFrequency = prevParamData.g;
        effectiveVerticalWave = prevParamData.b;
        effectiveFractalDetail = prevParamData.a;
        effectiveColorIntensity = colorIntensity; // Using direct value for simplicity
        
        // Add safeguards against extreme values
        effectiveWaveAmplitude = clamp(effectiveWaveAmplitude, 0.01, 0.5);
        effectiveWaveFrequency = clamp(effectiveWaveFrequency, 0.1, 2.0);
        effectiveVerticalWave = clamp(effectiveVerticalWave, 0.0, 0.1);
        effectiveFractalDetail = clamp(effectiveFractalDetail, 1.0, 50.0);
        effectiveColorIntensity = max(0.001, effectiveColorIntensity);
        float exposureFactor = clamp(1.0 / exposureControl, 0.01, 10.0);
        
        // Convert coordinates to match the original shader
        vec2 u = isf_FragNormCoord.xy * RENDERSIZE;
        vec4 o = vec4(0.0);
        vec3 p = vec3(RENDERSIZE, 0.0);
        
        // Normalize coordinates as in the original shader
        u = (u - p.xy / 2.0) / p.y;
        
        float i = 0.0, d = 0.0, s = 0.0;
        
        // Main raymarch loop - initialize o to zero vector
        o *= i; // This matches the original code's o*=i where i=0 initially
        
        for(; i < 100.0; i++) {
            p = vec3(u * d, d + effectiveTime);
            
            // Fractal detail loop - restructured for better compatibility
            for(s = 0.15; s < 1.0; s *= 1.5) {
                p += cos(effectiveTime + p.yzx * effectiveWaveFrequency) * sin(p.z * 0.1) * effectiveWaveAmplitude;
                p.y += sin(effectiveTime + p.x) * effectiveVerticalWave;
                p += abs(dot(sin(p * s * effectiveFractalDetail), p - p + 0.01)) / s;
            }
            
            d += s = max(0.03 + abs(2.0 + p.y) * 0.3, 0.01); // Prevent s from being too small
            o += vec4(1.0, 2.0, 4.0, 0.0) / s * effectiveColorIntensity;
        }
        
        // Apply final color transformation with improved exposure control
        u -= 0.35;
        float dotUU = max(dot(u, u), 0.001); // Prevent division by zero
        
        // Calculate intensity scale with safeguards
        float intensityScale = max(7e3 * dotUU * (1.0/effectiveColorIntensity), 0.001);
        
        // Apply a smoother adjustment to prevent overexposure in the center
        float centerBoost = 1.0 + smoothstep(0.1, 0.0, dotUU) * exposureControl;
        intensityScale *= centerBoost;
        
        // Apply modified tanh with exposure control
        o = tanh_custom(o / intensityScale, exposureFactor);
        
        // Apply gamma correction to further control brightness
        o = pow(max(o, 0.0), vec4(1.0/2.2)); // Prevent negative values before pow
        
        // Ensure alpha is 1.0
        gl_FragColor = vec4(o.rgb, 1.0);
    }
}
