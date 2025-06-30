/*{
  "DESCRIPTION": "Cosmic Ray Marcher with smooth transitions",
  "CREDIT": "Code by @XorDev's, ISF 2.0 Version by @dot2dot (bareimage)",
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
      "NAME": "cameraDistance",
      "TYPE": "float",
      "DEFAULT": 0.5,
      "MIN": 0.1,
      "MAX": 1.0,
      "LABEL": "Camera Distance"
    },
    {
      "NAME": "rotationSpeed",
      "TYPE": "float",
      "DEFAULT": 0.1,
      "MIN": -0.5,
      "MAX": 0.5,
      "LABEL": "Rotation Speed"
    },
    {
      "NAME": "rotationDamping",
      "TYPE": "float",
      "DEFAULT": 0.8,
      "MIN": 0.1,
      "MAX": 0.99,
      "LABEL": "Rotation Damping"
    },
    {
      "NAME": "colorShift",
      "TYPE": "float",
      "DEFAULT": 0.2,
      "MIN": 0.0,
      "MAX": 1.0,
      "LABEL": "Color Shift Speed"
    },
    {
      "NAME": "iterations",
      "TYPE": "float",
      "DEFAULT": 100.0,
      "MIN": 10.0,
      "MAX": 200.0,
      "LABEL": "Ray Iterations"
    },
    {
      "NAME": "colorMix",
      "TYPE": "color",
      "DEFAULT": [1.0, 1.0, 1.0, 1.0],
      "LABEL": "Color Mix"
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
      "TARGET": "paramBuffer",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": 1,
      "HEIGHT": 1
    },
    {
      "TARGET": "rotationBuffer",
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

void main() {
    // Declare variables at the top level for use in all passes
    vec4 prevTimeData, prevParamData, prevRotData;
    float accumulatedTime, currentSpeed, adjustedSpeed, newTime;
    float currentCamDist, currentRotSpeed, currentColorShift, currentIterations;
    float effectiveTime, effectiveCamDist, effectiveRotSpeed, effectiveColorShift, effectiveIterations;
    float targetRotation, currentRotation, dampedRotation;
    vec4 effectiveColorMix;
    
    if (PASSINDEX == 0) {
        // First pass: Update the accumulated time in the persistent buffer
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        
        // Extract previous accumulated time and speed
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
        // Second pass: Update other parameters with smooth transitions
        prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5, 0.5));
        
        if (FRAMEINDEX == 0) {
            // Initialize on first frame
            currentCamDist = cameraDistance;
            currentRotSpeed = rotationSpeed;
            currentColorShift = colorShift;
            currentIterations = iterations;
            effectiveColorMix = vec4(colorMix.rgb, 1.0);
        } else {
            // Smoothly transition all parameters
            currentCamDist = prevParamData.r;
            currentRotSpeed = prevParamData.g;
            currentColorShift = prevParamData.b;
            currentIterations = prevParamData.a;
            
            float blendFactor = min(1.0, TIMEDELTA * transitionSpeed);
            
            currentCamDist = mix(currentCamDist, cameraDistance, blendFactor);
            currentRotSpeed = mix(currentRotSpeed, rotationSpeed, blendFactor);
            currentColorShift = mix(currentColorShift, colorShift, blendFactor);
            currentIterations = mix(currentIterations, iterations, blendFactor);
        }
        
        gl_FragColor = vec4(currentCamDist, currentRotSpeed, currentColorShift, currentIterations);
    }
    else if (PASSINDEX == 2) {
        // Third pass: Handle rotation with special damping
        prevRotData = IMG_NORM_PIXEL(rotationBuffer, vec2(0.5, 0.5));
        prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5, 0.5));
        
        // Get current rotation speed from parameter buffer
        currentRotSpeed = prevParamData.g;
        
        if (FRAMEINDEX == 0) {
            // Initialize on first frame
            currentRotation = 0.0;
            targetRotation = currentRotSpeed;
            dampedRotation = currentRotSpeed;
        } else {
            // Apply damping to rotation
            currentRotation = prevRotData.r;
            targetRotation = currentRotSpeed;
            
            // Calculate damped rotation using exponential smoothing
            // Higher rotationDamping = more smoothing
            dampedRotation = mix(targetRotation, currentRotation, rotationDamping);
            
            // Add a small amount of the target to prevent complete stalling
            dampedRotation += (targetRotation - dampedRotation) * (1.0 - rotationDamping) * 0.1;
        }
        
        gl_FragColor = vec4(dampedRotation, 0.0, 0.0, 1.0);
    }
    else {
        // Final pass: Render the shader using the accumulated values
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5, 0.5));
        prevRotData = IMG_NORM_PIXEL(rotationBuffer, vec2(0.5, 0.5));
        
        // Get the accumulated values
        effectiveTime = prevTimeData.r;
        effectiveCamDist = prevParamData.r;
        effectiveRotSpeed = prevRotData.r; // Use damped rotation from rotation buffer
        effectiveColorShift = prevParamData.b;
        effectiveIterations = prevParamData.a;
        
        // Convert ShaderToy code to ISF
        vec2 uv = (isf_FragNormCoord.xy * RENDERSIZE - 0.5 * RENDERSIZE) / RENDERSIZE.y;
        vec3 r = vec3(uv, 1.0);
        vec4 o = vec4(0.0);
        float t = effectiveTime;
        vec3 p;
        
        float z = 0.0;
        float d;
        
        for (float i = 0.0; i < effectiveIterations; i++) {
            // Ray direction, modulated by time and camera
            p = z * normalize(vec3(uv, effectiveCamDist));
            p.z += t;
            
            // Rotating plane using a cos matrix with damped rotation
            vec4 angle = vec4(0.0, 33.0, 11.0, 0.0);
            vec4 a = z * 0.2 + t * effectiveRotSpeed + angle;
            p.xy *= mat2(cos(a.x), -sin(a.x), sin(a.x), cos(a.x));
            
            // Distance estimator
            z += d = length(cos(p + cos(p.yzx + p.z - t * effectiveColorShift)).xy) / 6.0;
            
            // Color accumulation using sin palette
            o += (sin(p.x + t + vec4(0.0, 2.0, 3.0, 0.0)) + 1.0) / d;
        }
        
        o = tanh(o / 5000.0);
        
        // Apply color mix
        o.rgb *= colorMix.rgb;
        
        gl_FragColor = vec4(o.rgb, 1.0);
    }
}
