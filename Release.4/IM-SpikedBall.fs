/*{
  "DESCRIPTION": "Spherized raymarched tunnels, based on a Shadertoy by nimitz. Converted to ISF with smoothed speed and panning controls.",
  "CREDIT": "@nimitz (Shadertoy), @ISF 2.0 version by @dot2dot",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR"],
  "INPUTS": [
    {
      "NAME": "speed",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": -5.0,
      "MAX": 10.0,
      "LABEL": "Speed"
    },
    {
      "NAME": "amplitude",
      "TYPE": "float",
      "DEFAULT": 9.0,
      "MIN": 1.0,
      "MAX": 20.0,
      "LABEL": "Amplitude"
    },
    {
      "NAME": "patternScale",
      "TYPE": "float",
      "DEFAULT": 43.0,
      "MIN": 10.0,
      "MAX": 100.0,
      "LABEL": "Pattern Scale"
    },
    {
      "NAME": "panX",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": -2.0,
      "MAX": 2.0,
      "LABEL": "Pan X"
    },
    {
      "NAME": "panY",
      "TYPE": "float",
      "DEFAULT": 0.5,
      "MIN": -2.0,
      "MAX": 2.0,
      "LABEL": "Pan Y"
    },
    {
      "NAME": "raymarchSteps",
      "TYPE": "float",
      "DEFAULT": 70.0,
      "MIN": 20.0,
      "MAX": 150.0,
      "LABEL": "Raymarch Steps"
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

// GLSL ES 1.00 doesn't have a built-in round() function. This is a standard replacement.
float round(float x) {
  return floor(x + 0.5);
}

// Helper function for hue, from original shader
vec3 H(float a) {
    return (cos(radians(vec3(180, 90, 0)) + a * 6.2832) * 0.5 + 0.5);
}

// Signed Distance Function (SDF), adapted from original shader
float map(vec3 u, float v, float t, float A) {
    float l = 5.0;   // loop to reduce clipping
    float f = 1e10, i = 0.0, y, z;
    
    // polar transform
    u.xy = vec2(atan(u.x, u.y), length(u.xy));
    // counter rotation
    u.x += t * v * 3.1416 * 0.7;
    
    for (; i++ < l;) {
        vec3 p = u;
        y = round((p.y - i) / l) * l + i;
        p.x *= y;
        p.x -= y * y * t * 3.1416;
        p.x -= round(p.x / 6.2832) * 6.2832;
        p.y -= y;
        z = cos(y * t * 6.2832) * 0.5 + 0.5; // z wave
        // tubes SDF
        f = min(f, max(length(p.xy), -p.z - z * A) - 0.1 - z * 0.2 - p.z / 1e2);
    }
    return f;
}


void main() {
    // -- Common variables for ISF Passes --
    vec4 prevTimeData;
    float accumulatedTime, currentSpeed, adjustedSpeed, newTime, effectiveTime;

    // -- Variables for Final Rendering Pass --
    vec2 R, fragCoord, m, j;
    vec3 camOrigin, rayDir, fragColor, p, k;
    float t, v, i, d, s, f, z, r;
    bool b;

    if (PASSINDEX == 0) {
        // First pass: Update the accumulated time in the persistent buffer
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        
        // Extract previous accumulated time and speed
        accumulatedTime = prevTimeData.r;
        currentSpeed = prevTimeData.g;
        
        // Calculate new accumulated time with smoothing
        if (FRAMEINDEX == 0) {
            // Initialize time on the first frame
            newTime = 0.0;
            adjustedSpeed = speed;
        } else {
            // Smoothly transition to the target speed
            adjustedSpeed = mix(currentSpeed, speed, min(1.0, TIMEDELTA * transitionSpeed));
            newTime = accumulatedTime + adjustedSpeed * TIMEDELTA;
        }
        
        // Store the new accumulated time and current speed in the buffer
        gl_FragColor = vec4(newTime, adjustedSpeed, 0.0, 1.0);
    }
    else { // PASSINDEX == 1
        // Final pass: Render the main visual
        
        // Get the smoothed time from the buffer
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        effectiveTime = prevTimeData.r;

        // ---- Start of ported Shadertoy mainImage logic ----
        R = RENDERSIZE;
        fragCoord = gl_FragCoord.xy;
        
        // Panning controls (replaces iMouse)
        m = vec2(panX, panY);
        
        camOrigin = vec3(0.0, 0.0, -130.0); // camera position
        rayDir = normalize(vec3(fragCoord - R * 0.5, R.y)); // ray direction
        fragColor = vec3(0.0);
        
        t = effectiveTime / 300.0; // Scaled and smoothed time
        v = patternScale;
        i = 0.0;
        d = 0.0; // distance marched
        
        // Raymarching loop
        for (; i++ < raymarchSteps;) {
            p = rayDir * d + camOrigin;
            p.xy /= v;                               // scale down
            r = length(p.xy);                        // radius
            z = abs(1.0 - r*r);                      // z warp
            b = r < 1.0;                             // are we inside the sphere?
            if (b) z = sqrt(z);
            p.xy /= z + 1.0;                         // spherize
            p.xy -= m;                               // pan with controls
            p.xy *= v;                               // scale back up
            p.xy -= cos(p.z / 8.0 + t * 3e2 + vec2(0, 1.5708) + z / 2.0) * 0.2; // wave along z
            
            s = map(p, v, t, amplitude); // Get distance from SDF
            
            r = length(p.xy); // new radius
            f = cos(round(r) * t * 6.2832) * 0.5 + 0.5; // multiples
            k = H(0.2 - f / 3.0 + t + p.z / 2e2);       // calculate color
            if (b) k = 1.0 - k;                        // flip color inside sphere
            
            // Accumulate color based on distance and other properties
            fragColor += min(exp(s / -0.05), s)            // shapes
                       * (f + 0.01)                        // shade pattern
                       * min(z, 1.0)                       // darken edges
                       * sqrt(cos(r * 6.2832) * 0.5 + 0.5)  // shade between rows
                       * k * k;                            // color
            
            // Break if we're close enough or too far
            if (s < 1e-3 || d > 1e3) break;
            
            // Advance the ray
            d += s * clamp(z, 0.3, 0.9); // smaller steps towards sphere edge
        }
        
        // Add light tips effect - Corrected from original code
        float lightTipFactor = min(exp(-p.z - f * amplitude) * z * 0.01 / max(s, 1e-6), 1.0);
        fragColor += k * lightTipFactor;
        
        // Adjust brightness
        j = p.xy / v + m; // 2d coords
        fragColor /= clamp(dot(j, j) * 4.0, 0.04, 4.0);
        
        // Gamma correction and final output
        gl_FragColor = vec4(pow(fragColor, vec3(1.0 / 2.2)), 1.0);
    }
}