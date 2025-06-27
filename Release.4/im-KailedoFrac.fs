/*{
    "CREDIT": "Butadiene (original), ISF Unbuffered Version 1to1 @dot2dot, Modified by @dot2dot for organic motion & advanced controls",
    "DESCRIPTION": "Advanced fractal tetrahedral structure with smoothed camera zoom/rotation, dynamic colors, and multi-segment kaleidoscopic symmetry. All major parameters are buffered for smooth transitions.",
    "ISFVSN": "2.0",
    "CATEGORIES": ["Geometry", "Animation", "Abstract", "Kaleidoscope", "Interactive"],
    "INPUTS": [
        {
            "NAME": "speed",
            "TYPE": "float",
            "DEFAULT": 0.1,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Animation Speed"
        },
        {
            "NAME": "modValue",
            "TYPE": "float",
            "DEFAULT": 2.0,
            "MIN": 0.1,
            "MAX": 10.0,
            "LABEL": "Geometry Mod/Detail"
        },
        {
            "NAME": "baseColorInput",
            "TYPE": "color",
            "DEFAULT": [0.2, 0.3, 0.7, 1.0],
            "LABEL": "Base Color"
        },
        {
            "NAME": "highlightColorInput",
            "TYPE": "color",
            "DEFAULT": [0.8, 0.8, 0.2, 1.0],
            "LABEL": "Highlight Color"
        },
        {
            "NAME": "viewRotationSpeed",
            "TYPE": "float",
            "DEFAULT": 0.05,
            "MIN": -0.5,
            "MAX": 0.5,
            "LABEL": "View Rotation Speed"
        },
        {
            "NAME": "kaleidoscopeSegments",
            "TYPE": "float",
            "DEFAULT": 2.0, 
            "MIN": 2.0,     
            "MAX": 16.0,    
            "LABEL": "Kaleidoscope Segments"
        },
        {
            "NAME": "zoomLevel",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.2,
            "MAX": 3.0,
            "LABEL": "Zoom Level"
        },
        {
            "NAME": "transitionSmoothness",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 10.0,
            "LABEL": "Parameter Smoothness"
        }
    ],
    "PASSES": [
        {
            "TARGET": "timeSpeedRotBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1
        },
        {
            "TARGET": "baseColorBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1
        },
        {
            "TARGET": "highlightColorBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1
        },
        {
            "TARGET": "kaleidoscopeBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1
        },
        {
            "TARGET": "zoomBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1
        },
        {
            "TARGET": "finalOutput"
        }
    ],
    "LICENSE": "MIT"
}*/

// --- Preserve original copyright notice ---
// Copyright (c) 2021 Butadiene
// Kaleidoscope & Buffer Logic (c) dot2dot
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
mat2 MAT2_ROT_03 = mat2(cos(0.3), sin(0.3), -sin(0.3), cos(0.3));
mat2 MAT2_ROT_PI_4 = mat2(0.70710678118, 0.70710678118, -0.70710678118, 0.70710678118); // Rotation by PI/4

// --- Utility Functions ---

// pmod: Applies a polar modulo operation.
// p: input 2D vector
// n: number of segments for the modulo operation
vec2 pmod(vec2 p, float n) {
    n = max(1.0, floor(n)); // Ensure n is at least 1 and an integer for clear segments
    float angle_step = 2.0 * PI / n;
    float current_angle = atan(p.y, p.x);
    current_angle -= 0.5 * angle_step; 
    current_angle = mod(current_angle, angle_step) - 0.5 * angle_step;
    return length(p) * vec2(cos(current_angle), sin(current_angle));
}

// rot: Creates a 2D rotation matrix.
// r: rotation angle in radians
mat2 rot(float r) {
    vec2 s = vec2(cos(r), sin(r));
    return mat2(s.x, s.y, -s.y, s.x);
}

// cube: Signed distance function for a cube.
// p: point in 3D space
// s: half-sizes of the cube (vec3)
float cube(vec3 p, vec3 s) {
    vec3 q = abs(p);
    vec3 m = max(s - q, 0.0);
    return length(max(q - s, 0.0)) - min(min(m.x, m.y), m.z);
}

// --- Core Geometry Functions ---

// tetcol: Generates the fractal geometry and accumulates color information.
vec4 tetcol(vec3 p, vec3 offset, float scale, vec3 col_acc, float current_dynamic_mod_value) {
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
    float dist_to_cube = cube(z.xyz, vec3(max(0.05, current_dynamic_mod_value)));
    return vec4(current_col_acc, dist_to_cube / z.w); 
}

// dist: Main distance estimator, now using smoothed color inputs.
vec4 dist(vec3 p, float effectiveTime, float currentModValue, vec3 smoothedBaseCol, vec3 smoothedHighlightCol) {
    vec3 current_p = p;

    float geom_mod_freq = 0.25; 
    float shear_mat_freq = 0.3;

    float dynamicGeomMod = currentModValue * (0.8 + 0.4 * sin(effectiveTime * geom_mod_freq));
    dynamicGeomMod = max(0.1, dynamicGeomMod); 

    current_p.xy *= -1.0; 
    current_p.xz = pmod(current_p.xz, 24.0); 
    current_p.x -= 5.1; 

    current_p.xy = MAT2_ROT_03 * current_p.xy;
    current_p.xz = MAT2_ROT_PI_4 * current_p.xz;

    mat2 dynamic_shear_mat = mat2(0.0, 1.0, -sin(effectiveTime * shear_mat_freq), 0.0);
    current_p.yz = dynamic_shear_mat * current_p.yz;

    current_p.z = abs(current_p.z) - 3.0;
    current_p = abs(current_p) - 8.0;
    current_p = abs(current_p) - 4.0;
    current_p = abs(current_p) - 2.0;
    current_p = abs(current_p) - 1.0;

    vec4 sd = tetcol(current_p, vec3(1.0), 1.8, vec3(0.0), dynamicGeomMod);

    // --- Color calculation using smoothed inputs ---
    vec3 color_val = smoothedBaseCol - 0.05 * sd.xyz; // Base color modulated by fractal structure
    // Add highlight contribution - more prominent for closer surfaces (smaller sd.w)
    // and slightly modulated by the fractal's inherent color accumulation (sd.xyz)
    color_val += smoothedHighlightCol * pow(max(0.0, 1.0 - sd.w * 2.0), 8.0) * (1.0 - 0.1 * length(sd.xyz));
    
    color_val *= exp(-2.5 * sd.w) * 1.8; // Apply fog/depth effect and adjust brightness

    return vec4(color_val, sd.w);
}

// --- Main ISF Entry Point ---
void main() {
    // Declare variables for smoothed parameters
    float effectiveTime;
    float smoothedSpeed;
    float accumulatedViewRotation;
    float smoothedRotationSpeed;
    vec3 smoothedBaseColor;
    vec3 smoothedHighlightColor;
    float smoothedKaleidoscopeSegments;
    float smoothedZoomLevel;
    float currentTransitionFactor = min(1.0, TIMEDELTA * transitionSmoothness);

    if (PASSINDEX == 0) {
        // --- Pass 0: Update timeSpeedRotBuffer ---
        vec4 prevBuffer = IMG_NORM_PIXEL(timeSpeedRotBuffer, vec2(0.5));
        float prevTime = prevBuffer.r;
        float prevSpeed = prevBuffer.g;
        float prevRotation = prevBuffer.a; // Stored accumulated rotation in alpha
        float prevRotationSpeed = prevBuffer.b; // Stored rotation speed in beta

        if (FRAMEINDEX == 0) {
            effectiveTime = 0.0;
            smoothedSpeed = speed;
            accumulatedViewRotation = 0.0;
            smoothedRotationSpeed = viewRotationSpeed;
        } else {
            smoothedSpeed = mix(prevSpeed, speed, currentTransitionFactor);
            effectiveTime = prevTime + smoothedSpeed * TIMEDELTA;
            smoothedRotationSpeed = mix(prevRotationSpeed, viewRotationSpeed, currentTransitionFactor);
            accumulatedViewRotation = prevRotation + smoothedRotationSpeed * TIMEDELTA;
        }
        gl_FragColor = vec4(effectiveTime, smoothedSpeed, smoothedRotationSpeed, accumulatedViewRotation);

    } else if (PASSINDEX == 1) {
        // --- Pass 1: Update baseColorBuffer ---
        vec3 prevColor = IMG_NORM_PIXEL(baseColorBuffer, vec2(0.5)).rgb;
        smoothedBaseColor = mix(prevColor, baseColorInput.rgb, currentTransitionFactor);
        gl_FragColor = vec4(smoothedBaseColor, 1.0);

    } else if (PASSINDEX == 2) {
        // --- Pass 2: Update highlightColorBuffer ---
        vec3 prevColor = IMG_NORM_PIXEL(highlightColorBuffer, vec2(0.5)).rgb;
        smoothedHighlightColor = mix(prevColor, highlightColorInput.rgb, currentTransitionFactor);
        gl_FragColor = vec4(smoothedHighlightColor, 1.0);

    } else if (PASSINDEX == 3) {
        // --- Pass 3: Update kaleidoscopeBuffer ---
        float prevSegments = IMG_NORM_PIXEL(kaleidoscopeBuffer, vec2(0.5)).r;
        smoothedKaleidoscopeSegments = mix(prevSegments, kaleidoscopeSegments, currentTransitionFactor);
        gl_FragColor = vec4(smoothedKaleidoscopeSegments, 0.0, 0.0, 1.0);
        
    } else if (PASSINDEX == 4) {
        // --- Pass 4: Update zoomBuffer ---
        float prevZoom = IMG_NORM_PIXEL(zoomBuffer, vec2(0.5)).r;
        smoothedZoomLevel = mix(prevZoom, zoomLevel, currentTransitionFactor);
        gl_FragColor = vec4(smoothedZoomLevel, 0.0, 0.0, 1.0);

    } else if (PASSINDEX == 5) {
        // --- Pass 5: Final Rendering Pass ---
        vec4 timeData = IMG_NORM_PIXEL(timeSpeedRotBuffer, vec2(0.5));
        effectiveTime = timeData.r;
        // smoothedSpeed = timeData.g; // Not directly used in this pass, but available
        smoothedRotationSpeed = timeData.b; // Current rotation speed
        accumulatedViewRotation = timeData.a; // Total accumulated rotation

        smoothedBaseColor = IMG_NORM_PIXEL(baseColorBuffer, vec2(0.5)).rgb;
        smoothedHighlightColor = IMG_NORM_PIXEL(highlightColorBuffer, vec2(0.5)).rgb;
        smoothedKaleidoscopeSegments = IMG_NORM_PIXEL(kaleidoscopeBuffer, vec2(0.5)).r;
        smoothedZoomLevel = IMG_NORM_PIXEL(zoomBuffer, vec2(0.5)).r;

        vec2 fragCoord = gl_FragCoord.xy;
        vec2 uv = fragCoord / RENDERSIZE;
        vec2 p_screen = (uv - 0.5) * 2.0;
        p_screen.y *= RENDERSIZE.y / RENDERSIZE.x;

        // Apply view rotation (around Z axis, looking into screen)
        p_screen = rot(accumulatedViewRotation) * p_screen;
        
        // Apply zoom
        p_screen /= max(0.01, smoothedZoomLevel); // Avoid division by zero

        // Apply kaleidoscopic effect
        // Ensure at least 2 segments for symmetry, otherwise it's just mirrored or normal
        p_screen = pmod(p_screen, max(2.0, floor(smoothedKaleidoscopeSegments)));


        // --- Camera Setup (Retaining "breathing" zoom, modulated by smoothedZoomLevel) ---
        float base_zoom_radius = 25.0 / smoothedZoomLevel; // Base radius affected by zoom
        float zoom_amplitude = 15.0 / smoothedZoomLevel;   // Amplitude affected by zoom
        float zoom_cycle_freq = 0.05;      
        float pulsation_amplitude = 3.0 / smoothedZoomLevel; 
        float pulsation_freq = 0.2;        
        float ro_angle_freq = 0.05;        
        float ro_height_freq = 0.2;        
        float ta_height_freq = 0.03;       

        float main_zoom_component = base_zoom_radius + zoom_amplitude * sin(effectiveTime * zoom_cycle_freq);
        float pulsation_component = pulsation_amplitude * sin(effectiveTime * pulsation_freq);
        float rsa = main_zoom_component + pulsation_component;
        rsa = max(5.0 / smoothedZoomLevel, rsa); // Ensure camera doesn't get too close

        float cameraAngle = effectiveTime * ro_angle_freq; 
        float ro_y = -1.2 + 2.0 * sin(effectiveTime * ro_height_freq); 
        vec3 ro = vec3(rsa * cos(cameraAngle), ro_y, rsa * sin(cameraAngle));
        vec3 ta = vec3(0.0, -1.3 + 0.5 * cos(effectiveTime * ta_height_freq), 0.0); 
        
        vec3 cdir = normalize(ta - ro);
        vec3 side = normalize(cross(cdir, vec3(0.0, 1.0, 0.0)));
        vec3 up = normalize(cross(side, cdir));
        vec3 rd = normalize(p_screen.x * side + p_screen.y * up + 0.4 * cdir); 

        // --- Raymarching ---
        float total_dist_marched = 0.0;
        vec3 accumulated_color = vec3(0.0);
        const float epsilon = 0.0001;
        const float max_march_dist = 100.0; // Max distance to march
        const int max_march_steps = 66;     // Max raymarching steps

        for (int i = 0; i < max_march_steps; i++) {
            vec4 rsd = dist(ro + rd * total_dist_marched, effectiveTime, modValue, smoothedBaseColor, smoothedHighlightColor);
            total_dist_marched += rsd.w;
            accumulated_color += rsd.xyz;
            if (rsd.w < epsilon || total_dist_marched > max_march_dist) break;
        }

        // --- Final Color Output ---
        vec3 final_col = 0.04 * accumulated_color; // Adjust overall brightness
        if (all(lessThan(final_col, vec3(0.05)))) final_col = vec3(0.0); // Clamp very dark to black
        
        gl_FragColor = vec4(clamp(final_col,0.0,1.0), 1.0); // Clamp final color and set alpha
    }
}
