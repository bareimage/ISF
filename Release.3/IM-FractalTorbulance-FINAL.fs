/*{
  "DESCRIPTION": "Fractal turbulence landscape with raymarching and smooth parameter transitions. Includes highlight clamping and further performance optimizations (adjusted raymarching iterations and exit conditions).",
  "CREDIT": "Original by @iapafoto, @diatribes. ISF Version by @dot2dot.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR"],
  "INPUTS": [
    {
      "NAME": "speed",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 20.0,
      "LABEL": "Animation Speed"
    },
    {
      "NAME": "turbulenceScale",
      "TYPE": "float",
      "DEFAULT": 24.0,
      "MIN": 10.0,
      "MAX": 50.0,
      "LABEL": "Turbulence Scale"
    },
    {
      "NAME": "skyIntensity",
      "TYPE": "float",
      "DEFAULT": 0.1,
      "MIN": 0.01,
      "MAX": 0.5,
      "LABEL": "Sky Intensity"
    },
    {
      "NAME": "groundIntensity",
      "TYPE": "float",
      "DEFAULT": 0.2,
      "MIN": 0.01,
      "MAX": 1.0,
      "LABEL": "Ground Intensity"
    },
    {
      "NAME": "colorTint",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.1,
      "MAX": 3.0,
      "LABEL": "Color Tint"
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

// Helper structure for turbulence results
struct TurbulentResult {
    vec3 p;
    vec3 q;
};

// Custom implementation of tanh function
// Outputs values in the range [-1, 1]
vec4 tanh_custom(vec4 x) {
    vec4 exp2x = exp(2.0 * x);
    return (exp2x - 1.0) / (exp2x + 1.0);
}

// Normalizes fragment coordinates to a UV space where (0,0) is center
vec2 normalizeCoords(vec2 fragCoord, vec3 resolution) {
    return (fragCoord - resolution.xy * 0.5) / resolution.y;
}

// Applies turbulence to a given position
TurbulentResult applyTurbulence(vec3 initial_pos, float time, float turbScale) {
    vec3 p_val = initial_pos;
    vec3 q_val = initial_pos;
    vec3 const_01  = vec3(0.01); // Small constant for dot product
    vec3 const_005 = vec3(0.005); // Another small constant

    // Iteratively apply noise (simplified fractal turbulence)
    // 8 octaves
    for(float s_octave = 0.01; s_octave < 2.0; s_octave *= 2.0) { // Loop condition s_octave < 2.0 for 8 iterations (0.01 to 1.28)
        p_val += abs(dot(sin(p_val * s_octave * turbScale), const_01)) / s_octave;
        q_val += abs(dot(sin(0.3 * time + q_val * s_octave * 16.0), const_005)) / s_octave;
    }
    return TurbulentResult(p_val, q_val);
}

// Calculates the step size for raymarching based on scene geometry
float getSceneStep(vec3 turbulent_p, vec3 turbulent_q, float skyInt, float groundInt) {
    // Distance estimator for sky
    float sky = 0.03 + abs(9.0 - turbulent_q.y) * skyInt;
    // Distance estimator for ground
    float ground = 0.01 + abs(1.0 + turbulent_p.y) * groundInt;
    // Return the minimum of sky and ground, with a base minimum step
    return 0.01 + min(sky, ground);
}

// Applies tonemapping to the raw accumulated color
vec4 tonemapColor(vec4 raw_color, vec2 uv, float tint) {
    // Apply color tinting. Note: alpha channel of tinted color becomes 0.
    vec4 tinted = raw_color * vec4(tint, tint * 2.0, tint * 4.0, 0.0);
    
    // Divisor factor, becomes very small near uv = vec2(0.2), potentially causing large values after division.
    // 1e-7 is a small epsilon to prevent division by zero.
    float divisor = max(length(uv - vec2(0.2)), 1e-7); 
    
    // Calculate argument for the tanh function. This can become very large.
    vec4 arg_to_tanh = tinted / (10000.0 * divisor);

    // Clamp the RGB components of the argument to tanh_custom to prevent "overblown" highlights.
    const float CLAMP_MAX_RGB_ARG = 3.0f; 
    arg_to_tanh.r = clamp(arg_to_tanh.r, 0.0f, CLAMP_MAX_RGB_ARG);
    arg_to_tanh.g = clamp(arg_to_tanh.g, 0.0f, CLAMP_MAX_RGB_ARG);
    arg_to_tanh.b = clamp(arg_to_tanh.b, 0.0f, CLAMP_MAX_RGB_ARG);
    
    return tanh_custom(arg_to_tanh);
}

void main() {
    // Declare variables at the top level for use in all passes.
    vec4 prevTimeData, prevParamData;
    float accumulatedTime, currentSpeed, adjustedSpeed, newTime;
    float currentTurbScale, currentSkyInt, currentGroundInt, currentColorTint;
    float effectiveTime, effectiveTurbScale, effectiveSkyInt, effectiveGroundInt, effectiveColorTint;
    
    if (PASSINDEX == 0) {
        // First pass: Update and store accumulated time and current speed.
        // This pass writes to 'timeBuffer'.
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5)); // Read from own buffer
        
        accumulatedTime = prevTimeData.r; // Previous accumulated time
        currentSpeed = prevTimeData.g;    // Previous smoothed speed
        
        if (FRAMEINDEX == 0) {
            // Initialize time and speed on the first frame
            newTime = 0.0;
            adjustedSpeed = speed; // Use target speed directly
        } else {
            // Smoothly transition current speed towards the target 'speed' input
            adjustedSpeed = mix(currentSpeed, speed, min(1.0, TIMEDELTA * transitionSpeed));
            // Update accumulated time
            newTime = accumulatedTime + adjustedSpeed * TIMEDELTA;
        }
        
        // Store new accumulated time and the adjusted speed for the next frame
        gl_FragColor = vec4(newTime, adjustedSpeed, 0.0, 1.0);

    } else if (PASSINDEX == 1) {
        // Second pass: Update and store shader parameters with smooth transitions.
        // This pass writes to 'paramBuffer'.
        prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5, 0.5)); // Read from own buffer
        
        if (FRAMEINDEX == 0) {
            // Initialize parameters on the first frame
            currentTurbScale = turbulenceScale;
            currentSkyInt = skyIntensity;
            currentGroundInt = groundIntensity;
            currentColorTint = colorTint;
        } else {
            // Retrieve previously smoothed parameters
            currentTurbScale = prevParamData.r;
            currentSkyInt = prevParamData.g;
            currentGroundInt = prevParamData.b;
            currentColorTint = prevParamData.a;
            
            // Smoothly transition each parameter towards its target input value
            float transitionFactor = min(1.0, TIMEDELTA * transitionSpeed);
            currentTurbScale = mix(currentTurbScale, turbulenceScale, transitionFactor);
            currentSkyInt = mix(currentSkyInt, skyIntensity, transitionFactor);
            currentGroundInt = mix(currentGroundInt, groundIntensity, transitionFactor);
            currentColorTint = mix(currentColorTint, colorTint, transitionFactor);
        }
        
        // Store the newly smoothed parameters for the next frame
        gl_FragColor = vec4(currentTurbScale, currentSkyInt, currentGroundInt, currentColorTint);

    } else { // PASSINDEX == 2 (or any other pass, assuming this is the final render pass)
        // Final pass: Render the fractal landscape using smoothed time and parameters.
        // This pass reads from 'timeBuffer' and 'paramBuffer'.
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5, 0.5));
        
        // Get the effective (smoothed) values from the buffers
        effectiveTime = prevTimeData.r;
        // effectiveSpeed = prevTimeData.g; // Not directly used in rendering, but available

        effectiveTurbScale = prevParamData.r;
        effectiveSkyInt = prevParamData.g;
        effectiveGroundInt = prevParamData.b;
        effectiveColorTint = prevParamData.a;
        
        // Normalize fragment coordinates
        vec2 u = normalizeCoords(gl_FragCoord.xy, vec3(RENDERSIZE.xy, 0.0));
        
        vec4 accumulated_color = vec4(0.0); // Accumulator for color/light
        float distance_marched = 0.0;       // Total distance marched

        // --- OPTIMIZATION: Define constants for raymarching control ---
        const float MAX_RAY_ITERATIONS = 80.0; // Reduced from 100.0
        const float MAX_STEP_SIZE_BREAK_THRESHOLD = 10.0; // Reduced from 15.0, more aggressive
        // const float MAX_TOTAL_DISTANCE_MARCHED = 200.0; // Optional: Safety break for total distance

        // Raymarching loop
        for(float i = 0.0; i < MAX_RAY_ITERATIONS; i++) {
            // Current position along the ray
            vec3 pos = vec3(u * distance_marched, distance_marched + effectiveTime);
            
            // Apply turbulence to the position
            TurbulentResult tr = applyTurbulence(pos, effectiveTime, effectiveTurbScale);
            
            // Get the estimated distance to the scene (step size)
            float step_size = getSceneStep(tr.p, tr.q, effectiveSkyInt, effectiveGroundInt);
            
            // --- OPTIMIZATION: Early exit based on step_size ---
            if (step_size > MAX_STEP_SIZE_BREAK_THRESHOLD) {
                break; 
            }
            
            // March forward
            distance_marched += step_size;
            
            // Accumulate color/light (simple additive blending based on inverse step size)
            // Ensure step_size is not zero or extremely small to prevent division by zero or infinity.
            // getSceneStep returns at least 0.01, so step_size should be safe.
            accumulated_color += 1.0 / step_size; 

            // Optional: Safety break for total distance
            // if (distance_marched > MAX_TOTAL_DISTANCE_MARCHED) {
            //     break;
            // }
        }

        // Apply tonemapping (which now includes clamping)
        gl_FragColor = tonemapColor(accumulated_color, u, effectiveColorTint);
        gl_FragColor.a = 1.0; // Ensure final output is opaque
    }
}
