/*{
  "DESCRIPTION": "Converted ShaderToy 'The Weave' by chronos. Explores volume tracing turbulently distorted SDFs. Animation speed is smoothed.",
  "CREDIT": "Original ShaderToy by chronos (https://www.shadertoy.com/view/W3SSRm), ISF 2.0 by @dot2dot",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR"],
  "INPUTS": [
    {
      "NAME": "speed",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 10.0,
      "LABEL": "Animation Speed"
    },
    {
      "NAME": "focalLength",
      "TYPE": "float",
      "DEFAULT": 2.25,
      "MIN": 0.1,
      "MAX": 5.0,
      "LABEL": "Focal Length"
    },
    {
      "NAME": "colorPhaseX",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": -5.0,
      "MAX": 5.0,
      "LABEL": "Color Phase X"
    },
    {
      "NAME": "colorPhaseY",
      "TYPE": "float",
      "DEFAULT": 2.0,
      "MIN": -5.0,
      "MAX": 5.0,
      "LABEL": "Color Phase Y"
    },
    {
      "NAME": "colorPhaseZ",
      "TYPE": "float",
      "DEFAULT": 3.0,
      "MIN": -5.0,
      "MAX": 5.0,
      "LABEL": "Color Phase Z"
    },
    {
      "NAME": "colorPower",
      "TYPE": "float",
      "DEFAULT": 2.5,
      "MIN": 0.1,
      "MAX": 5.0,
      "LABEL": "Color Power"
    },
    {
      "NAME": "brightnessFactor",
      "TYPE": "float",
      "DEFAULT": 2e-3,
      "MIN": 1e-4,
      "MAX": 1e-2,
      "LABEL": "Brightness Factor",
      "VALUES": [1e-4, 5e-4, 1e-3, 2e-3, 5e-3, 1e-2],
      "LABELS": ["Dimmer", "Dim", "Normal", "Bright", "Brighter", "Brightest"]
    },
    {
      "NAME": "stepSizeFactor",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.25,
      "MAX": 4.0,
      "LABEL": "Detail / Step Size (Smaller=More Detail)"
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

// Global constants
const float PI = 3.14159265;

// Helper function for color mapping, using INPUTS for customization
vec3 cmap(float val, vec3 phase, float powerVal) {
    return pow(0.5 + 0.5 * cos(PI * val + phase), vec3(powerVal));
}

void main() {
    if (PASSINDEX == 0) {
        // --- Time Smoothing Pass ---
        // This pass updates and stores the accumulated time with smoothing.

        // Retrieve previous time data (accumulated time and last speed)
        vec4 prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        float accumulatedTime = prevTimeData.r;
        float currentSpeedFromBuffer = prevTimeData.g; // Speed stored in buffer

        float smoothedSpeed;
        float newAccumulatedTime;

        if (FRAMEINDEX == 0) {
            // Initialize on the first frame
            newAccumulatedTime = 0.0;
            smoothedSpeed = speed; // 'speed' is the INPUT from ISF JSON
        } else {
            // Smoothly transition to the target speed
            smoothedSpeed = mix(currentSpeedFromBuffer, speed, min(1.0, TIMEDELTA * transitionSpeed));
            // Update accumulated time based on the smoothed speed and time delta
            newAccumulatedTime = accumulatedTime + smoothedSpeed * TIMEDELTA;
        }

        // Store the new accumulated time in .r and the current smoothed speed in .g
        gl_FragColor = vec4(newAccumulatedTime, smoothedSpeed, 0.0, 1.0);

    } else { // Final Render Pass (PASSINDEX == 1)
        // --- Main Visual Rendering Pass ---

        // Retrieve the smoothed accumulated time from the buffer
        vec4 prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        float effectiveTime = prevTimeData.r;

        // Convert ISF normalized coordinates to ShaderToy's fragment coordinates
        vec2 fragCoord = gl_FragCoord.xy; // Pixel coordinates

        // Calculate UV coordinates as in the original ShaderToy
        // uv.y ranges from -1 (bottom) to 1 (top)
        // uv.x ranges from -aspect_ratio to +aspect_ratio
        vec2 uv_st = (2.0 * fragCoord - RENDERSIZE.xy) / RENDERSIZE.y;

        // Setup ray origin (ro) and ray direction (rd)
        vec3 ro = vec3(0.0, 0.0, effectiveTime); // Ray origin moves along Z over time
        vec3 rd = normalize(vec3(uv_st, -focalLength)); // 'focalLength' is an INPUT

        vec3 accumulatedColor = vec3(0.0);
        float currentRayDistance = 0.0; // Distance marched along the ray (t in original)

        // INPUTS for cmap customization
        vec3 CMapPhase = vec3(colorPhaseX, colorPhaseY, colorPhaseZ);
        // 'colorPower', 'brightnessFactor', 'stepSizeFactor' are also INPUTS

        // Ray marching loop (99 steps as in original)
        for (int i_loop = 0; i_loop < 99; i_loop++) {
            // Current point in 3D space
            vec3 p = ro + currentRayDistance * rd;

            // Apply time-dependent rotation to p.xy (twisting effect)
            float rotationPhase = (currentRayDistance + effectiveTime) / 5.0;
            float cos_rot = cos(rotationPhase);
            float sin_rot = sin(rotationPhase);
            p.xy = mat2(cos_rot, -sin_rot, sin_rot, cos_rot) * p.xy;

            // Apply turbulent distortion (fractal noise like)
            for (float f_loop = 0.0; f_loop < 9.0; f_loop += 1.0) {
                float amplitude_attenuator = exp(f_loop) * pow(2.0, -f_loop); // exp(f)/exp2(f)
                p += cos(p.yzx * amplitude_attenuator + effectiveTime) / amplitude_attenuator;
            }

            // Calculate distance 'd_dist' for volume rendering step.
            // This defines the "density" or "surface" region.
            // Original: float d = 1./50. + abs((ro -p-vec3(0,1,0)).y-1.)/10.;
            // Simplified: float d = 0.02 + abs(ro.y - p.y - 2.0) / 10.0;
            float dist_measure = (0.02 + abs(ro.y - p.y - 2.0) / 10.0) / stepSizeFactor;

            // Ensure dist_measure is not too small to prevent artifacts or excessive contribution
            dist_measure = max(0.001, dist_measure);

            // Accumulate color based on cmap and distance
            accumulatedColor += cmap(currentRayDistance, CMapPhase, colorPower) * brightnessFactor / dist_measure;

            // Advance ray
            currentRayDistance += dist_measure;

            // Safety break if ray travels too far (prevents infinite loops with small dist_measure)
            if (currentRayDistance > 20.0) break;
        }

        // Post-processing color transformations
        accumulatedColor *= accumulatedColor * accumulatedColor; // Enhances contrast
        accumulatedColor = 1.0 - exp(-accumulatedColor);       // Tone mapping (similar to Reinhard)
        accumulatedColor = pow(accumulatedColor, vec3(1.0 / 2.2)); // Gamma correction

        gl_FragColor = vec4(accumulatedColor, 1.0);
    }
}