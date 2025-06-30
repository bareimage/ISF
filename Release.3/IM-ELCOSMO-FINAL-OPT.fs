/*{
   "DESCRIPTION": "Refactored Shadertoy shader with additional smoothed controls (zoom, distortion, roll) and unified speed/parameter smoothing. OPTIMIZED VERSION.",
   "CREDIT": "Original by ShaderToy author @diatribes, ISF Version by d@ot2dot (bareimage)",
   "ISFVSN": "2.0",
   "CATEGORIES": ["GENERATOR"],
   "INPUTS": [
     {
       "NAME": "speed",
       "TYPE": "float",
       "DEFAULT": 1.0,
       "MIN": 0.0,
       "MAX": 15.0,
       "LABEL": "Speed"
     },
     {
       "NAME": "paramsTransitionSpeed",
       "TYPE": "float",
       "DEFAULT": 1.0,
       "MIN": 0.1,
       "MAX": 10.0,
       "LABEL": "Params Smoothness"
     },
     {
       "NAME": "zoomFactor",
       "TYPE": "float",
       "DEFAULT": 1.0,
       "MIN": 0.2,
       "MAX": 3.0,
       "LABEL": "Zoom Factor"
     },
     {
       "NAME": "distortionIntensity",
       "TYPE": "float",
       "DEFAULT": 1.0,
       "MIN": 0.0,
       "MAX": 5.0,
       "LABEL": "Distortion Intensity"
     },
     {
       "NAME": "rollFactor",
       "TYPE": "float",
       "DEFAULT": 1.0,
       "MIN": 0.0,
       "MAX": 3.0,
       "LABEL": "Roll Factor"
     }
   ],
   "PASSES": [
     {
       "TARGET": "timeBuffer",
       "PERSISTENT": true,
       "FLOAT": true,
       "WIDTH": 1, "HEIGHT": 1
     },
     {
       "TARGET": "paramsBuffer",
       "PERSISTENT": true,
       "FLOAT": true,
       "WIDTH": 1, "HEIGHT": 1
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

// --- Helper Functions (some modified for new params) ---

mat2 quirkyRotate(float angle) {
    // This creates mat2(cos(angle), cos(angle+33), cos(angle+11), cos(angle))
    // It's intentionally "quirky", not a standard rotation.
    return mat2(cos(angle + vec4(0,33,11,0)));
}

vec3 getCameraPathPoint(float z_coord) {
    return vec3(tanh(cos(z_coord * 0.2) * 0.3) * 16.0,
                tanh(cos(z_coord * 0.3) * 0.4) * 16.0,
                z_coord);
}

float evaluateAsteroidShapeValue(vec3 position, float globalTime) {
    vec3 q = position;
    q.xy *= quirkyRotate(globalTime * 0.6);
    q.z += dot(cos(q * 0.8), sin(q * 1.3));
    return 2.2 - dot(cos(q * 0.6), cos(q));
}

float evaluateLightDistance(vec3 position, float globalTime) {
    vec3 q = position + cos(globalTime + position.zxy);
    return dot(abs(q - floor(q) - 0.5), vec3(0.7));
}

// Modified to include distortionIntensity and optimized loop
vec3 distortSpaceForTunnel(vec3 position, float globalTime, float intensity) {
    vec3 p_distorted = position;
    if (intensity == 0.0) return p_distorted; // No distortion if intensity is zero

    // OPTIMIZATION: Pre-calculate intensity factors
    float intensity_factor_dot = 0.02 * intensity;
    float intensity_factor_sin = 0.44 * intensity;

    // OPTIMIZATION: Reduced inner loop iterations for performance.
    // Original: s_noise *= 1.41421356237 (sqrt(2)), 5 iterations.
    // New: s_noise *= 2.0, 3 iterations (0.2, 0.4, 0.8).
    // This will change the distortion's appearance but improve speed.
    for (float s_noise = 0.2; s_noise < 1.0; s_noise *= 2.0) {
        p_distorted -= dot(cos(globalTime + p_distorted * s_noise * 8.0), vec3(intensity_factor_dot)) / s_noise;
        p_distorted += sin(p_distorted.zxy * 0.13) * intensity_factor_sin;
    }
    return p_distorted;
}

float evaluateTunnelDistance(vec3 distorted_position) {
    vec2 path_projection_at_depth = getCameraPathPoint(distorted_position.z).xy;
    vec3 p_eval_tunnel = distorted_position - vec3(1.0);

    // OPTIMIZATION: Clarified length(scalar) to abs(scalar) for readability. No performance change.
    float s_tunnel_shape = 0.5 - min(abs(p_eval_tunnel.y - path_projection_at_depth.x),
                                     min(length(p_eval_tunnel.xy - path_projection_at_depth.xy),
                                         abs(p_eval_tunnel.x - path_projection_at_depth.y)));
    return 0.03 + abs(s_tunnel_shape) * 0.2;
}

vec4 accumulateRaymarchColor(vec4 current_accumulated_color, float distance_to_surface_step, float total_distance_marched) {
    vec4 phase_offsets = vec4(1.0, 2.0, 4.0, 0.0);
    // Using max to prevent division by zero or very small numbers causing extreme brightness.
    vec4 color_contribution = (1.0 + cos(total_distance_marched + phase_offsets)) /
                              max(distance_to_surface_step, 0.0001) /
                              max(total_distance_marched, 0.0001);
    return current_accumulated_color + color_contribution;
}

vec4 applyTonemapping(vec4 raw_color) {
    vec4 color = raw_color;
    color = color * color / 200000.0; // Simple exposure/brightness adjustment
    color = (color / (color + 0.155)) * 1.019; // Reinhard-like tonemapping
    return color;
}

void main() {
    if (PASSINDEX == 0) {
        // --- Pass 0: Update timeBuffer (time and main animation speed) ---
        // This pass is very lightweight (1x1 pixel) and unlikely to be a bottleneck.
        vec4 prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        float accumulatedTime = prevTimeData.r;
        float currentActualSpeed = prevTimeData.g; // Speed used in the previous frame
        float newTime;
        float adjustedSpeed;

        if (FRAMEINDEX == 0) {
            newTime = 0.0;
            adjustedSpeed = speed; // Use raw speed input for the first frame
        } else {
            float t_mix = min(1.0, TIMEDELTA * paramsTransitionSpeed);
            adjustedSpeed = mix(currentActualSpeed, speed, t_mix);
            newTime = accumulatedTime + adjustedSpeed * TIMEDELTA;
        }
        gl_FragColor = vec4(newTime, adjustedSpeed, 0.0, 1.0);
    }
    else if (PASSINDEX == 1) {
        // --- Pass 1: Update paramsBuffer (zoom, distortion, roll) ---
        // This pass is very lightweight (1x1 pixel) and unlikely to be a bottleneck.
        vec4 prevParamsData = IMG_NORM_PIXEL(paramsBuffer, vec2(0.5, 0.5));
        float currentZoom = prevParamsData.r;
        float currentDistortion = prevParamsData.g;
        float currentRoll = prevParamsData.b;

        float smoothedZoom, smoothedDistortion, smoothedRoll;

        if (FRAMEINDEX == 0) {
            smoothedZoom = zoomFactor;
            smoothedDistortion = distortionIntensity;
            smoothedRoll = rollFactor;
        } else {
            float t_params_mix = min(1.0, TIMEDELTA * paramsTransitionSpeed);
            smoothedZoom = mix(currentZoom, zoomFactor, t_params_mix);
            smoothedDistortion = mix(currentDistortion, distortionIntensity, t_params_mix);
            smoothedRoll = mix(currentRoll, rollFactor, t_params_mix);
        }
        gl_FragColor = vec4(smoothedZoom, smoothedDistortion, smoothedRoll, 1.0);
    }
    else { // PASSINDEX == 2
        // --- Pass 2: Main shader pass (visual rendering) ---
        vec4 timeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        float effectiveTime = timeData.r;

        vec4 paramsData = IMG_NORM_PIXEL(paramsBuffer, vec2(0.5, 0.5));
        float sZoom = paramsData.r;
        float sDistortion = paramsData.g;
        float sRoll = paramsData.b;

        float sceneTime = effectiveTime * 1.5;

        vec2 r = RENDERSIZE.xy;
        vec2 u = isf_FragNormCoord.xy * r; // In ISF, isf_FragNormCoord is already 0-1
                                         // If you need pixel coords: gl_FragCoord.xy
                                         // For this shader, u seems to be intended as pixel coordinates.
                                         // Using gl_FragCoord.xy is more standard for pixel coordinates.
                                         // Let's assume the original intent with isf_FragNormCoord * r was correct for its logic.

        vec4 accumulatedColor = vec4(0.0);

        vec3 ro = getCameraPathPoint(sceneTime);
        vec3 camLookAt = getCameraPathPoint(sceneTime + 1.0); // Point slightly ahead for direction
        vec3 Z_cam = normalize(camLookAt - ro);
        // Create a robust camera basis
        vec3 upApprox = vec3(0.0, 1.0, 0.0);
        if (abs(Z_cam.y) > 0.99) upApprox = vec3(1.0, 0.0, 0.0); // Handle looking straight up/down
        vec3 X_cam = normalize(cross(upApprox, Z_cam)); // Ensure X is perpendicular to Z and roughly horizontal
        vec3 Y_cam = cross(Z_cam, X_cam); // Y will be orthogonal to Z and X


        vec2 screen_pos_centered_normalized = (u - r.xy * 0.5) / r.y;

        float rollAngle = sin(ro.z * 0.2) * sRoll;
        mat2 rollMatrix = quirkyRotate(rollAngle); // Using the quirky rotate as intended

        // Apply roll to screen coordinates before constructing ray direction
        vec2 rolled_screen_pos = rollMatrix * screen_pos_centered_normalized;
        
        vec3 D_ray = normalize(X_cam * rolled_screen_pos.x + Y_cam * rolled_screen_pos.y + Z_cam * sZoom);
        // The original calculation for D_ray was:
        // vec3 D_ray_orig = vec3(quirkyRotate(rollAngle) * screen_pos_centered_normalized, sZoom) * mat3(-X_cam, Y_cam, Z_cam);
        // D_ray_orig = normalize(D_ray_orig);
        // The new method is a more standard way to construct a view ray with roll and zoom.
        // sZoom here acts more like a field-of-view factor on Z.
        // If sZoom is FoV, it should be `Z_cam / sZoom`. If it's a dolly zoom, then it's more complex.
        // Given the original `vec3(..., sZoom)`, sZoom likely adjusts the z-component of the ray in camera space before transformation.
        // Let's stick closer to original ray construction logic if it was intentional for a specific effect:
        vec3 ray_camera_space = vec3(rolled_screen_pos, sZoom); // sZoom modifies the "depth" component in camera space
        D_ray = normalize(mat3(X_cam, Y_cam, Z_cam) * ray_camera_space); // Transform camera space ray to world space


        float total_distance_marched = 0.0;

        // OPTIMIZATION: Reduced main raymarching loop iterations.
        // Original: 100.0. Tune this for performance vs. quality/distance.
        for(float i = 0.0; i < 60.0; i++) { // << OPTIMIZED LOOP COUNT
            // The 0.8 factor here might be an artistic choice or a specific technique from the original shader.
            // It makes the sampling point lag behind the accumulated distance.
            vec3 p_march = ro + D_ray * total_distance_marched * 0.8;

            float asteroid_g_value = evaluateAsteroidShapeValue(p_march, sceneTime);
            float asteroid_dist = 0.033 + abs(asteroid_g_value * 3.0) * 0.25;

            float light_dist = evaluateLightDistance(p_march, sceneTime);

            vec3 p_distorted_for_tunnel = distortSpaceForTunnel(p_march, sceneTime, sDistortion);
            float tunnel_dist = evaluateTunnelDistance(p_distorted_for_tunnel);

            float step_dist = min(light_dist, min(tunnel_dist, asteroid_dist));
            total_distance_marched += step_dist;

            accumulatedColor = accumulateRaymarchColor(accumulatedColor, step_dist, total_distance_marched);
            
            if (step_dist < 0.001 || total_distance_marched > 200.0) {
                 break;
            }
        }
        gl_FragColor = applyTonemapping(accumulatedColor);
    }
}
