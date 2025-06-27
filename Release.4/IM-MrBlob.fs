/*{
    "DESCRIPTION": "Volumetric Fluorescent Effect with smoothed speed, movement, pattern focus, and color control.",
    "CREDIT": "Based on ideas described in 'Fluorescent' by @XorDev, @dot2dot. ISF Version @dot2dot (bareimage). Enhanced by @dot2dot",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "Generator"
    ],
    "INPUTS": [
        {
            "NAME": "inputImage",
            "TYPE": "image"
        },
        {
            "NAME": "speed",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 50.0,
            "LABEL": "Animation Speed"
        },
        {
            "NAME": "transitionSpeed",
            "TYPE": "float",
            "DEFAULT": 2.0,
            "MIN": 0.1,
            "MAX": 10.0,
            "LABEL": "Speed Transition"
        },
        {
            "NAME": "colorControl",
            "TYPE": "color",
            "DEFAULT": [0.8, 0.9, 1.0, 1.0],
            "LABEL": "Base Color Tint"
        },
        {
            "NAME": "targetMovementX",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -3.0,
            "MAX": 3.0,
            "LABEL": "Target Movement X"
        },
        {
            "NAME": "targetMovementY",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -3.0,
            "MAX": 3.0,
            "LABEL": "Target Movement Y"
        },
        {
            "NAME": "movementTransitionSpeed",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 10.0,
            "LABEL": "Movement Transition"
        },
        {
            "NAME": "patternFocusTarget",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 5.0,
            "LABEL": "Pattern Focus Target"
        },
        {
            "NAME": "patternFocusTransitionSpeed",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 10.0,
            "LABEL": "Focus Transition"
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
            "TARGET": "movementBuffer",
            "PERSISTENT": true,
            "FLOAT": true,
            "WIDTH": 1,
            "HEIGHT": 1
        },
        {
            "TARGET": "patternFocusBuffer",
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

////////////////////////////////////////////////////////////////////////////////
// ISF Volumetric Fluorescent Shader
// Somewhat based on Fluorescent" by @XorDev, I was trying to study hist style
// And ended up going completly of rails. This shader is totally useless
// but fun.
// Based on my (@dot2dot) shader https://www.shadertoy.com/view/tc3SDM
////////////////////////////////////////////////////////////////////////////////

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

// --- Global Constants ---
const mat2 ROTATION_MATRIX = 0.1 * mat2(8.0, -6.0, 6.0, 8.0); // Pre-scaled rotation matrix

// --- Forward declarations for helper functions used by multiple passes if needed ---
// (Currently not strictly needed as they are mostly used in the final pass or are simple)

// --- Helper Functions ---
vec3 getRayDirection(vec2 fragCoord, vec2 resolution) {
    vec2 uv = (fragCoord - 0.5 * resolution.xy) / resolution.y;
    float FOCAL_LENGTH = 1.0;
    vec3 dir = vec3(uv, FOCAL_LENGTH);
    return normalize(dir);
}

vec3 transformScenePoint(vec3 p_ray_sample, vec2 movementOffset) {
    vec3 p_transformed = p_ray_sample;
    p_transformed.yz *= ROTATION_MATRIX;
    p_transformed.xy += movementOffset; // Apply smoothed XY movement
    p_transformed.z += 6.0;
    return p_transformed;
}

float getScaledDistanceToOrigin(vec3 p_in_scene_space) {
    return length(p_in_scene_space) * 0.1;
}

float getRaymarchStepDistance(float scaledDistL_to_scene_origin) {
    return 1.0 + abs(scaledDistL_to_scene_origin - 1.2);
}

float calculateVolumetricPattern(vec3 p_in_scene_space, float time, float l_incremented_dist_in_scene, float focus) {
    vec3 p_focused = p_in_scene_space * focus; // Apply pattern focus
    vec3 cos_term_arg = p_focused / l_incremented_dist_in_scene - time;
    vec3 cos_term = cos(cos_term_arg);
    vec3 sin_term_arg = p_focused / (l_incremented_dist_in_scene * 0.4) + time;
    vec3 sin_term = sin(sin_term_arg).yzx;
    return dot(cos_term, sin_term);
}

vec4 calculateColorContribution(float l_incremented_dist_in_scene, float pattern_b, float ray_depth_from_camera, vec4 tintColor) {
    vec4 color_wave_arg_offset = vec4(2.0, 3.0, 4.0, 0.0);
    float tanh_arg = tanh(l_incremented_dist_in_scene - 6.0) * 6.0;
    vec4 raw_color_wave = 1.0 + cos(tanh_arg - color_wave_arg_offset);
    vec4 tinted_color_wave = raw_color_wave * tintColor; // Apply colorControl tint

    float b_factor = pattern_b * pattern_b * pattern_b * pattern_b;
    vec4 contribution = tinted_color_wave * b_factor;

    if (ray_depth_from_camera > 0.001) {
        return contribution / ray_depth_from_camera;
    }
    return vec4(0.0);
}

vec4 applyTonemapping(vec4 accumulatedColor) {
    return tanh(accumulatedColor / 2.0);
}

// --- Main Shader Logic ---
void main() {
    // Buffer data variables
    vec4 prevTimeData, prevMovementData, prevPatternFocusData;

    // Smoothed value variables
    float accumulatedTime, currentSpeed, adjustedSpeed, newTime;
    float effectiveTime;

    float currentMovementX, currentMovementY, adjustedMovementX, adjustedMovementY;
    vec2 effectiveMovement;

    float currentPatternFocus, adjustedPatternFocus;
    float effectivePatternFocus;

    // Rendering variables (primarily for the final pass)
    vec2 resolution;
    vec2 fragCoord;
    vec4 currentAccumulatedColor; // Renamed from accumulatedColor to avoid conflict
    float current_ray_depth_val; // Renamed
    vec3 rayDirection_val; // Renamed
    float timeVal;


    if (PASSINDEX == 0) { // Time Buffer Update
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        accumulatedTime = prevTimeData.r;
        currentSpeed = prevTimeData.g;

        if (FRAMEINDEX == 0) {
            newTime = 0.0;
            adjustedSpeed = speed;
        } else {
            adjustedSpeed = mix(currentSpeed, speed, min(1.0, TIMEDELTA * transitionSpeed));
            newTime = accumulatedTime + adjustedSpeed * TIMEDELTA;
        }
        gl_FragColor = vec4(newTime, adjustedSpeed, 0.0, 1.0);

    } else if (PASSINDEX == 1) { // Movement Buffer Update
        prevMovementData = IMG_NORM_PIXEL(movementBuffer, vec2(0.5, 0.5));
        currentMovementX = prevMovementData.r;
        currentMovementY = prevMovementData.g;

        if (FRAMEINDEX == 0) {
            adjustedMovementX = targetMovementX;
            adjustedMovementY = targetMovementY;
        } else {
            adjustedMovementX = mix(currentMovementX, targetMovementX, min(1.0, TIMEDELTA * movementTransitionSpeed));
            adjustedMovementY = mix(currentMovementY, targetMovementY, min(1.0, TIMEDELTA * movementTransitionSpeed));
        }
        gl_FragColor = vec4(adjustedMovementX, adjustedMovementY, 0.0, 1.0);

    } else if (PASSINDEX == 2) { // Pattern Focus Buffer Update
        prevPatternFocusData = IMG_NORM_PIXEL(patternFocusBuffer, vec2(0.5, 0.5));
        currentPatternFocus = prevPatternFocusData.r;

        if (FRAMEINDEX == 0) {
            adjustedPatternFocus = patternFocusTarget;
        } else {
            adjustedPatternFocus = mix(currentPatternFocus, patternFocusTarget, min(1.0, TIMEDELTA * patternFocusTransitionSpeed));
        }
        gl_FragColor = vec4(adjustedPatternFocus, 0.0, 0.0, 1.0);

    } else if (PASSINDEX == 3) { // Final Rendering Pass
        // Read from buffers
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        effectiveTime = prevTimeData.r;

        prevMovementData = IMG_NORM_PIXEL(movementBuffer, vec2(0.5, 0.5));
        effectiveMovement = prevMovementData.rg;

        prevPatternFocusData = IMG_NORM_PIXEL(patternFocusBuffer, vec2(0.5, 0.5));
        effectivePatternFocus = prevPatternFocusData.r;

        // Setup for rendering
        timeVal = effectiveTime;
        resolution = RENDERSIZE;
        fragCoord = gl_FragCoord.xy;
        currentAccumulatedColor = vec4(0.0);
        current_ray_depth_val = 0.0;
        rayDirection_val = getRayDirection(fragCoord, resolution);

        // Raymarching loop
        for (float i = 0.0; i < 60.0; i++) {
            vec3 p_sample_in_view_space = current_ray_depth_val * rayDirection_val;
            vec3 p_in_scene_space = transformScenePoint(p_sample_in_view_space, effectiveMovement);
            float l_base_scene = getScaledDistanceToOrigin(p_in_scene_space);
            float step_distance = getRaymarchStepDistance(l_base_scene);
            current_ray_depth_val += step_distance;
            float l_incremented_scene = l_base_scene + 1.0;

            float b_pattern = calculateVolumetricPattern(p_in_scene_space, timeVal, l_incremented_scene, effectivePatternFocus);
            vec4 step_color_contribution = calculateColorContribution(l_incremented_scene, b_pattern, current_ray_depth_val, colorControl);
            currentAccumulatedColor += step_color_contribution;

            if(current_ray_depth_val > 100.0) break;
        }

        gl_FragColor = applyTonemapping(currentAccumulatedColor);
    }
}