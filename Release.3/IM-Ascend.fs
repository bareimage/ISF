/*{
  "DESCRIPTION": "Ascend algorithm by @XorDev, with time accumulation and horizontal offset control. Based on a ISF Conversion Method by @dot2dot.",
  "CREDIT": "Ascend Algorithm: @XorDev. ISF Version by @dot2dot (bareimage).",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR"],
  "INPUTS": [
    { "NAME": "speed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0, "LABEL": "Animation Speed (for Ascend)" },
    { "NAME": "horizontalOffset", "TYPE": "float", "DEFAULT": 0.0, "MIN": -5.0, "MAX": 5.0, "LABEL": "Horizontal Offset" },
    { "NAME": "transitionSpeed", "TYPE": "float", "DEFAULT": 2.0, "MIN": 0.1, "MAX": 10.0, "LABEL": "Parameter Transition Smoothness" }
  ],
  "PASSES": [
    { "TARGET": "timeBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "offsetBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "finalOutput" }
  ]
}*/

precision highp float;

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
    // Pass 0: Time and main speed smoothing
    if(PASSINDEX == 0) {
        vec4 prevData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        // Smooth speed
        float smoothedSpeed = (FRAMEINDEX == 0) ? speed : mix(prevData.g, speed, min(1.0, TIMEDELTA * transitionSpeed));
        // Accumulate time
        float accumulatedTime = (FRAMEINDEX == 0) ? 0.0 : prevData.r + smoothedSpeed * TIMEDELTA;
        gl_FragColor = vec4(accumulatedTime, smoothedSpeed, 0.0, 1.0); // .r = time, .g = smoothed speed
        return;
    }

    // Pass 1: Horizontal offset smoothing
    if(PASSINDEX == 1) {
        vec4 prevData = IMG_NORM_PIXEL(offsetBuffer, vec2(0.5));
        // Smooth horizontalOffset
        float smoothedOffset = (FRAMEINDEX == 0) ? horizontalOffset : mix(prevData.r, horizontalOffset, min(1.0, TIMEDELTA * transitionSpeed));
        gl_FragColor = vec4(smoothedOffset, 0.0, 0.0, 1.0); // Store smoothed offset in .r
        return;
    }

    // Pass 2: Render "Ascend" Algorithm to finalOutput
    if(PASSINDEX == 2) {
        vec4 timeDataFromBuffer = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        float t = timeDataFromBuffer.r; // This is the accumulated, smoothed time for Ascend

        vec4 offsetDataFromBuffer = IMG_NORM_PIXEL(offsetBuffer, vec2(0.5));
        float currentHorizontalOffset = offsetDataFromBuffer.r; // Smoothed horizontal offset

        vec3 finalAscendColor = vec3(0.0); // Output color from Ascend algorithm

        // Ray direction setup (remains the same, offset is applied to evaluation point)
        vec3 s_rayDir = normalize(
            vec3(
                (isf_FragNormCoord.x * RENDERSIZE.x) * 2.1 - RENDERSIZE.x,
                (isf_FragNormCoord.y * RENDERSIZE.y) * 2.1 - RENDERSIZE.y,
                -RENDERSIZE.y
            )
        );

        float z_dist_marched = 0.0;
        float d_sdf_dist = 0.0;

        for(float i_loop = 0.0; i_loop < 30.0; i_loop++) {
            vec3 p_current = z_dist_marched * s_rayDir;

            // Apply horizontal offset to the evaluation point
            vec3 p_eval = p_current;
            p_eval.x -= currentHorizontalOffset;


            vec3 c_val = abs(s_rayDir);
            float c_val_y = c_val.y;
            if (abs(c_val_y) < 0.0001) {
                c_val_y = 0.0001;
            }
            c_val /= c_val_y;
            c_val.z += t;

            // Apply global time shift to the Z coordinate of the evaluation point
            p_eval.z -= t;

            float p_y_before_mod = p_eval.y; // Use y from p_eval

            // Modify y component of p_eval
            p_eval.y = abs(mod(p_eval.y - 2.0, 4.0) - 2.0);

            float len_ps_term = abs(p_eval.y - p_y_before_mod);

            // SDF calculation using p_eval
            float sdf_term1 = 0.5 * length(sin(2.0 * p_eval).xz + p_eval.y * 0.4);
            // Note: c_val is based on s_rayDir (camera), p_eval.z is the depth into the scene
            float sdf_term2_exp_arg = sin(length(sin(c_val)) * 40.0 + p_eval.z);
            float sdf_term2 = 0.1 * exp(sdf_term2_exp_arg) * len_ps_term;

            d_sdf_dist = 0.01 + sdf_term1 + sdf_term2;

            z_dist_marched += d_sdf_dist;

            if (d_sdf_dist > 0.0001 && z_dist_marched > 0.0001) {
                // Color accumulation using p_eval
                finalAscendColor += (sin(p_eval * 0.6) + vec3(1.1)) / (d_sdf_dist * z_dist_marched);
            }

            if(z_dist_marched > 100.0 || d_sdf_dist < 0.001) break;
        }

        finalAscendColor = tanh(finalAscendColor / 50.0);

        vec2 uv = isf_FragNormCoord.xy;
        float vignette = 1.0 - smoothstep(0.5, 1.5, length(uv - 0.5) * 1.5);
        vec3 resWithVignette = finalAscendColor.rgb * vignette;

        gl_FragColor = vec4(resWithVignette, 1.0);
        return;
    }
}
