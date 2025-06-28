/*{
    "DESCRIPTION": "Raymarched fractal sphere deformation with adjustable, smoothed speed using virtual time.",
    "CREDIT": "Original logic from isf.video (various authors), ISF conversion by @dot2dot, Virtual time based on exemple by @ProjectileObjects",
    "ISFVSN": "2",
    "CATEGORIES": [
        "Generator",
        "Fractal",
        "3D"
    ],
    "INPUTS": [
        {
            "NAME": "speedControl",
            "TYPE": "float",
            "LABEL": "Animation Speed",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 10.0
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

// Macro for rotation using Rodrigues' rotation formula
#define R(p,a,r) mix(a*dot(p,a),p,cos(r))+sin(r)*cross(p,a)

// Macro for color calculation (cosine palette variation)
#define H(t) (cos((vec3(0.0, 2.0, -2.0)/3.0+t)*6.2831853)*0.5+0.5)

// Macro for Distance Estimation. Includes effectiveTime dependency.
#define D(a, l_dist, effTime) (length(vec2(fract(log(length(a.xy)) - effTime * 0.5) - 0.5, a.z)) / 3.0 - 0.005 * pow(l_dist, 0.03))

void main()
{
    vec4 fragColor = vec4(0.0);
    
    if (PASSINDEX == 0) {
        // First pass: Update the accumulated time in the persistent buffer
        vec4 prevData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        
        // Extract previous accumulated time
        float accumulatedTime = prevData.r;
        
        // Calculate new accumulated time
        float newTime;
        
        if (FRAMEINDEX == 0) {
            // Initialize time on first frame
            newTime = 0.0;
        } else {
            // Accumulate time based on speed and frame delta
            newTime = accumulatedTime + speedControl * TIMEDELTA;
        }
        
        // Store the accumulated time
        fragColor = vec4(newTime, 0.0, 0.0, 1.0);
    }
    else if (PASSINDEX == 1) {
        // Second pass: Render the fractal using the accumulated time
        vec4 timeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        
        // Get the accumulated time
        float effectiveTime = timeData.r;

        // Get output resolution and pixel coordinates
        vec2 resolution = RENDERSIZE;
        vec2 fragCoords = gl_FragCoord.xy;

        float total_dist = 0.0; // Total distance marched along the ray
        float current_l = 0.0;  // Distance from origin to current point p
        float step_dist = 0.0;  // Distance estimated in current step

        // Raymarching loop
        for(int i = 1; i < 99; ++i) {
            // Calculate ray direction for perspective projection
            vec3 dir = normalize(vec3((fragCoords - 0.5 * resolution.xy) / resolution.y, 1.0));

            // Calculate current point 'p' along the ray
            // Use effectiveTime for rotation speed
            vec3 p = R(total_dist * dir - vec3(0.0, 0.0, 6.0), normalize(vec3(1.0, 2.0, 0.0)), effectiveTime * 0.2);

            // Calculate distance from origin to current point 'p'
            current_l = length(p);

            // Calculate the signed distance estimate (SDF)
            // Pass effectiveTime to the D macro for fractal evolution speed
            step_dist = min(min(D(p, current_l, effectiveTime), D(p.zxy, current_l, effectiveTime)), D(p.yzx, current_l, effectiveTime));

            // Advance the ray along its direction by the estimated distance
            total_dist += step_dist;

            // Check if the ray is close enough to the surface
            if(step_dist < 0.005) {
                // If close, add color contribution.
                fragColor.rgb += mix(vec3(1.0), H(current_l), 0.7) / float(i);
            }

            // Safety breaks
            if (total_dist > 100.0 || step_dist < 0.0001) break;
        }

        // Set alpha channel to fully opaque
        fragColor.a = 1.0;
    }
    
    // Output the final color
    gl_FragColor = fragColor;
}
