/*{
  "DESCRIPTION": "Sunset effect with smooth parameter transitions",
  "CREDIT": "Original by @XorDev, converted to ISF with enhancements by dot2dot",
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
      "NAME": "cloudDensity",
      "TYPE": "float",
      "DEFAULT": 0.3,
      "MIN": 0.1,
      "MAX": 0.5,
      "LABEL": "Cloud Density"
    },
    {
      "NAME": "turbulence",
      "TYPE": "float",
      "DEFAULT": 0.6,
      "MIN": 0.1,
      "MAX": 1.0,
      "LABEL": "Turbulence"
    },
    {
      "NAME": "colorShift",
      "TYPE": "float",
      "DEFAULT": 0.5,
      "MIN": 0.0,
      "MAX": 2.0,
      "LABEL": "Color Shift"
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
      "TARGET": "finalOutput"
    }
  ]
}*/

// Custom implementation of tanh function
vec4 tanh_custom(vec4 x) {
    vec4 exp2x = exp(2.0 * x);
    return (exp2x - 1.0) / (exp2x + 1.0);
}

void main() {
    // Declare variables at the top level for use in all passes
    vec4 prevTimeData, prevParamData;
    float accumulatedTime, currentSpeed, adjustedSpeed, newTime;
    float currentCloudDensity, currentTurbulence, currentColorShift;
    float effectiveTime, effectiveCloudDensity, effectiveTurbulence, effectiveColorShift;
    vec2 I;
    vec4 O;
    
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
            currentCloudDensity = cloudDensity;
            currentTurbulence = turbulence;
            currentColorShift = colorShift;
        } else {
            // Smoothly transition to target parameter values
            currentCloudDensity = prevParamData.r;
            currentTurbulence = prevParamData.g;
            currentColorShift = prevParamData.b;
            
            // Apply smooth transitions
            currentCloudDensity = mix(currentCloudDensity, cloudDensity, min(1.0, TIMEDELTA * transitionSpeed));
            currentTurbulence = mix(currentTurbulence, turbulence, min(1.0, TIMEDELTA * transitionSpeed));
            currentColorShift = mix(currentColorShift, colorShift, min(1.0, TIMEDELTA * transitionSpeed));
        }
        
        gl_FragColor = vec4(currentCloudDensity, currentTurbulence, currentColorShift, 1.0);
    }
    else {
        // Final pass: Render the shader using the accumulated values
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5, 0.5));
        
        // Get the accumulated values
        effectiveTime = prevTimeData.r;
        effectiveCloudDensity = prevParamData.r;
        effectiveTurbulence = prevParamData.g;
        effectiveColorShift = prevParamData.b;
        
        // Convert ShaderToy coordinates to ISF coordinates
        I = isf_FragNormCoord.xy * RENDERSIZE;
        O = vec4(0.0);
        
        // Raymarch iterator
        float i = 0.0,
        // Raymarch depth
        z = 0.0,
        // Step distance
        d = 0.0,
        // Signed distance
        s = 0.0;
        
        // Clear fragcolor and raymarch with 100 iterations
        for(; i < 100.0; i++) {
            // Compute raymarch sample point
            vec3 p = z * normalize(vec3(I+I, 0.0) - RENDERSIZE.xyy);
            
            // Turbulence loop
            for(d = 5.0; d < 200.0; d += d) {
                p += effectiveTurbulence * sin(p.yzx * d - 0.2 * effectiveTime) / d;
            }
            
            // Compute distance (smaller steps in clouds when s is negative)
            s = effectiveCloudDensity - abs(p.y);
            z += d = 0.005 + max(s, -s * 0.2) / 4.0;
            
            // Coloring with sine wave using cloud depth and x-coordinate
            O += (cos(s / 0.07 + p.x + effectiveColorShift * effectiveTime - vec4(3, 4, 5, 0)) + 1.5) * exp(s / 0.1) / d;
        }
        
        // Tanh tonemapping using our custom implementation
        O = tanh_custom(O * O / 4e8);
        
        gl_FragColor = O;
    }
}
