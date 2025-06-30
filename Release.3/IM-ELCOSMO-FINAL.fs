/*{
   "DESCRIPTION": "Refactored Shadertoy shader with additional smoothed controls (zoom, distortion, roll) and unified speed/parameter smoothing.",
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

// Modified to include distortionIntensity
vec3 distortSpaceForTunnel(vec3 position, float globalTime, float intensity) {
    vec3 p_distorted = position;
    if (intensity == 0.0) return p_distorted; // No distortion if intensity is zero

    for (float s_noise = 0.2; s_noise < 1.0; s_noise *= 1.41421356237) {
        p_distorted -= dot(cos(globalTime + p_distorted * s_noise * 8.0), vec3(0.02 * intensity)) / s_noise;
        p_distorted += sin(p_distorted.zxy * 0.13) * (0.44 * intensity);
    }
    return p_distorted;
}

float evaluateTunnelDistance(vec3 distorted_position) {
    vec2 path_projection_at_depth = getCameraPathPoint(distorted_position.z).xy;
    vec3 p_eval_tunnel = distorted_position - vec3(1.0);
    float s_tunnel_shape = 0.5 - min(length(p_eval_tunnel.y - path_projection_at_depth.x),
                                     min(length(p_eval_tunnel.xy - path_projection_at_depth.xy),
                                         length(p_eval_tunnel.x - path_projection_at_depth.y)));
    return 0.03 + abs(s_tunnel_shape) * 0.2;
}

vec4 accumulateRaymarchColor(vec4 current_accumulated_color, float distance_to_surface_step, float total_distance_marched) {
    vec4 phase_offsets = vec4(1.0, 2.0, 4.0, 0.0);
    vec4 color_contribution = (1.0 + cos(total_distance_marched + phase_offsets)) /
                              max(distance_to_surface_step, 0.0001) /
                              max(total_distance_marched, 0.0001);
    return current_accumulated_color + color_contribution;
}

vec4 applyTonemapping(vec4 raw_color) {
    vec4 color = raw_color;
    color = color * color / 200000.0;
    color = (color / (color + 0.155)) * 1.019;
    return color;
}

void main() {
    if (PASSINDEX == 0) {
        // --- Pass 0: Update timeBuffer (time and main animation speed) ---
        vec4 prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        float accumulatedTime = prevTimeData.r;
        float currentActualSpeed = prevTimeData.g; // Speed used in the previous frame
        float newTime;
        float adjustedSpeed;

        if (FRAMEINDEX == 0) {
            newTime = 0.0;
            adjustedSpeed = speed; // Use raw speed input for the first frame
        } else {
            // Dampen speed using paramsTransitionSpeed (linear interpolation)
            float t_mix = min(1.0, TIMEDELTA * paramsTransitionSpeed);
            adjustedSpeed = mix(currentActualSpeed, speed, t_mix);
            newTime = accumulatedTime + adjustedSpeed * TIMEDELTA;
        }
        gl_FragColor = vec4(newTime, adjustedSpeed, 0.0, 1.0); // Store smoothed time and current adjusted speed
    }
    else if (PASSINDEX == 1) {
        // --- Pass 1: Update paramsBuffer (zoom, distortion, roll) ---
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
            float t_params_mix = min(1.0, TIMEDELTA * paramsTransitionSpeed); // Linear mix for these params
            smoothedZoom = mix(currentZoom, zoomFactor, t_params_mix);
            smoothedDistortion = mix(currentDistortion, distortionIntensity, t_params_mix);
            smoothedRoll = mix(currentRoll, rollFactor, t_params_mix);
        }
        gl_FragColor = vec4(smoothedZoom, smoothedDistortion, smoothedRoll, 1.0);
    }
    else { // PASSINDEX == 2
        // --- Pass 2: Main shader pass (visual rendering) ---
        vec4 timeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        float effectiveTime = timeData.r; // Get the smoothed time

        vec4 paramsData = IMG_NORM_PIXEL(paramsBuffer, vec2(0.5, 0.5));
        float sZoom = paramsData.r;
        float sDistortion = paramsData.g;
        float sRoll = paramsData.b;

        float sceneTime = effectiveTime * 1.5;

        vec2 r = RENDERSIZE.xy;
        vec2 u = isf_FragNormCoord.xy * r;
        vec4 accumulatedColor = vec4(0.0);

        vec3 ro = getCameraPathPoint(sceneTime);
        vec3 Z_cam = normalize(getCameraPathPoint(sceneTime + 1.0) - ro);
        vec3 X_cam = normalize(vec3(Z_cam.z, 0.0, -Z_cam.x));
        vec3 Y_cam = cross(X_cam, Z_cam);

        vec2 screen_pos_centered_normalized = (u - r.xy / 2.0) / r.y;
        // Apply smoothed rollFactor and zoomFactor
        float rollAngle = sin(ro.z * 0.2) * sRoll;
        vec3 D_ray = vec3(quirkyRotate(rollAngle) * screen_pos_centered_normalized, sZoom) * mat3(-X_cam, Y_cam, Z_cam);
        D_ray = normalize(D_ray);

        float total_distance_marched = 0.0;

        for(float i = 0.0; i < 100.0; i++) {
            vec3 p_march = ro + D_ray * total_distance_marched * 0.8;

            float asteroid_g_value = evaluateAsteroidShapeValue(p_march, sceneTime);
            float asteroid_dist = 0.033 + abs(asteroid_g_value * 3.0) * 0.25;

            float light_dist = evaluateLightDistance(p_march, sceneTime);

            // Apply smoothed distortionIntensity
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
