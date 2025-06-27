/*{
    "CREDIT": "@Butadiene (original), ISF Unbuffered Version 1to1 @dot2dot, Modified with smooth speed transitions",
    "DESCRIPTION": "Fractal tetrahedral structure with animated camera and smooth speed transitions. Converted from GLSL to ISF 2.0.",
    "ISFVSN": "2.0",
    "CATEGORIES": ["Geometry", "Animation"],
    "INPUTS": [
        {
            "NAME": "speed",
            "TYPE": "float",
            "DEFAULT": 0.2,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Speed"
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
    ],
    "LICENSE": "MIT"
}*/

// --- Preserve original copyright notice ---
// Copyright (c) 2021 Butadiene
// Released under the MIT license
// https://opensource.org/licenses/mit-license.php

// MIT License
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// ADDITIONAL ATTRIBUTION REQUIREMENT:
// When using, modifying, or distributing this software, proper acknowledgment
// and credit must be maintained for both the original authors and any
// substantial contributors to derivative works.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// --- Global Constants ---
#define PI 3.141592653589793
const mat2 MAT2_ROT_03 = mat2(cos(0.3), sin(0.3), -sin(0.3), cos(0.3));
const mat2 MAT2_ROT_PI_4 = mat2(0.70710678118, 0.70710678118, -0.70710678118, 0.70710678118);
const mat2 MAT2_ROT_PI_2 = mat2(0.0, 1.0, -1.0, 0.0);
const float RKT_ANGLE_CONST = 0.5 * PI + 1.05;
const float COS_RKT_CONST = cos(RKT_ANGLE_CONST);
const float SIN_RKT_CONST = sin(RKT_ANGLE_CONST);

// --- Utility Functions ---
vec2 pmod(vec2 p, float n) {
    float angle_step = 2.0 * PI / n;
    float current_angle = atan(p.y, p.x);
    current_angle -= 0.5 * angle_step;
    current_angle = mod(current_angle, angle_step) - 0.5 * angle_step;
    return length(p) * vec2(cos(current_angle), sin(current_angle));
}

mat2 rot(float r) {
    vec2 s = vec2(cos(r), sin(r));
    return mat2(s.x, s.y, -s.y, s.x);
}

float cube(vec3 p, vec3 s) {
    vec3 q = abs(p);
    vec3 m = max(s - q, 0.0);
    return length(max(q - s, 0.0)) - min(min(m.x, m.y), m.z);
}

// --- Core Geometry Functions ---
vec4 tetcol(vec3 p, vec3 offset, float scale, vec3 col_acc) {
    vec4 z = vec4(p, 1.0);
    vec3 current_col_acc = col_acc;
    vec3 effective_translation = offset * (1.0 - scale);
    
    for (int i = 0; i < 12; i++) {
        if (z.x + z.y < 0.0) { z.xy = -z.yx; current_col_acc.z += 1.0; }
        if (z.x + z.z < 0.0) { z.xz = -z.zx; current_col_acc.y += 1.0; }
        if (z.y + z.z < 0.0) { z.yz = -z.zy; current_col_acc.x += 1.0; }
        z *= scale;
        z.xyz += effective_translation;
    }
    float dist_to_cube = cube(z.xyz, vec3(1.5));
    return vec4(current_col_acc, dist_to_cube / z.w);
}

vec4 dist(vec3 p) {
    vec3 current_p = p;
    current_p.xy *= -1.0;
    current_p.xz = pmod(current_p.xz, 24.0);
    current_p.x -= 5.1;
    current_p.xy = MAT2_ROT_03 * current_p.xy;
    current_p.xz = MAT2_ROT_PI_4 * current_p.xz;
    current_p.yz = MAT2_ROT_PI_2 * current_p.yz;
    current_p.z = abs(current_p.z) - 3.0;
    current_p = abs(current_p) - 8.0;
    current_p = abs(current_p) - 4.0;
    current_p = abs(current_p) - 2.0;
    current_p = abs(current_p) - 1.0;
    vec4 sd = tetcol(current_p, vec3(1.0), 1.8, vec3(0.0));
    vec3 color_val = 0.7 - 0.1 * sd.xyz;
    color_val *= exp(-2.5 * sd.w) * 2.0;
    return vec4(color_val, sd.w);
}

// --- Main ISF Entry Point ---
void main() {
    // Declare variables at the top level for use in all passes
    vec4 prevTimeData;
    float accumulatedTime, currentSpeed, adjustedSpeed, newTime;
    float effectiveTime;
    
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
    else {
        // Final pass: Render the shader using the accumulated time
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        
        // Get the accumulated time
        effectiveTime = prevTimeData.r;
        
        vec2 fragCoord = gl_FragCoord.xy;
        vec2 uv = fragCoord / RENDERSIZE;
        vec2 p_screen = (uv - 0.5) * 2.0;
        p_screen.y *= RENDERSIZE.y / RENDERSIZE.x;

        // Use effectiveTime instead of TIME * speed
        float rsa = 0.1 + mod(effectiveTime, 32.0);
        vec3 ro = vec3(rsa * COS_RKT_CONST, -1.2, rsa * SIN_RKT_CONST);
        vec3 ta = vec3(0.0, -1.3, 0.0);
        vec3 cdir = normalize(ta - ro);
        vec3 side = normalize(cross(cdir, vec3(0.0, 1.0, 0.0)));
        vec3 up = normalize(cross(side, cdir));
        vec3 rd = normalize(p_screen.x * side + p_screen.y * up + 0.4 * cdir);

        float total_dist_marched = 0.0;
        vec3 accumulated_color = vec3(0.0);
        const float epsilon = 0.0001;

        for (int i = 0; i < 66; i++) {
            vec4 rsd = dist(ro + rd * total_dist_marched);
            total_dist_marched += rsd.w;
            accumulated_color += rsd.xyz;
            if (rsd.w < epsilon || total_dist_marched > 100.0) break;
        }

        vec3 final_col = 0.04 * accumulated_color;
        if (all(lessThan(final_col, vec3(0.1)))) final_col = vec3(0.0);
        gl_FragColor = vec4(final_col, 1.0);
    }
}