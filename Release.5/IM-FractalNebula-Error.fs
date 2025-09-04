/*{
    "CATEGORIES": [
        "GENERATOR"
    ],
    "CREDIT": "Original by @dot2dot (bareimage). ISF 2.0 Conversion by @dot2dot (bareimage)",
    "DESCRIPTION": "A raymarched fractal nebula, converted from Shadery. Features smoothed controls for animation speed and camera movement, making it highly interactive. This version is optimized for performance with quality controls.",
    "INPUTS": [
        {
            "DEFAULT": 0.5,
            "LABEL": "Speed",
            "MAX": 2,
            "MIN": -2,
            "NAME": "speed",
            "TYPE": "float"
        },
        {
            "DEFAULT": [
                0,
                1
            ],
            "LABEL": "Camera Target",
            "MAX": [
                2,
                2
            ],
            "MIN": [
                -2,
                -2
            ],
            "NAME": "cameraTarget",
            "TYPE": "point2D"
        },
        {
            "DEFAULT": 6,
            "LABEL": "Zoom",
            "MAX": 6,
            "MIN": 0.1,
            "NAME": "zoom",
            "TYPE": "float"
        },
        {
            "DEFAULT": 0,
            "LABEL": "Hue Offset",
            "MAX": 1,
            "MIN": 0,
            "NAME": "hueOffset",
            "TYPE": "float"
        },
        {
            "DEFAULT": 0.4,
            "LABEL": "Saturation",
            "MAX": 1,
            "MIN": 0,
            "NAME": "saturation",
            "TYPE": "float"
        },
        {
            "DEFAULT": 9.0,
            "LABEL": "Brightness",
            "MAX": 10,
            "MIN": 0,
            "NAME": "brightness",
            "TYPE": "float"
        },
        {
            "NAME": "exposure",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -5.0,
            "MAX": 5.0,
            "LABEL": "Exposure"
        },
        {
            "NAME": "contrast",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Contrast"
        },
        {
            "DEFAULT": 1.0,
            "LABEL": "Gamma",
            "MAX": 3,
            "MIN": 0.1,
            "NAME": "gamma",
            "TYPE": "float"
        },
        {
            "DEFAULT": 2,
            "LABEL": "Transition Smoothness",
            "MAX": 10,
            "MIN": 0.1,
            "NAME": "transitionSpeed",
            "TYPE": "float"
        },
        {
            "DEFAULT": 0.0, "LABEL": "EQ Band 1", "MAX": 1.0, "MIN": 0.0, "NAME": "eq1", "TYPE": "float"
        },
        {
            "DEFAULT": 0.0, "LABEL": "EQ Band 2", "MAX": 1.0, "MIN": 0.0, "NAME": "eq2", "TYPE": "float"
        },
        {
            "DEFAULT": 0.0, "LABEL": "EQ Band 3", "MAX": 1.0, "MIN": 0.0, "NAME": "eq3", "TYPE": "float"
        },
        {
            "DEFAULT": 0.0, "LABEL": "EQ Band 4", "MAX": 1.0, "MIN": 0.0, "NAME": "eq4", "TYPE": "float"
        },
        {
            "DEFAULT": 0.0, "LABEL": "EQ Band 5", "MAX": 1.0, "MIN": 0.0, "NAME": "eq5", "TYPE": "float"
        },
        {
            "DEFAULT": 80,
            "LABEL": "Quality - Main Steps",
            "MAX": 129,
            "MIN": 16,
            "NAME": "mainSteps",
            "TYPE": "float"
        },
        {
            "DEFAULT": 8,
            "LABEL": "Quality - Detail Iterations",
            "MAX": 9,
            "MIN": 2,
            "NAME": "detailIterations",
            "TYPE": "float"
        }
    ],
    "ISFVSN": "2.0",
    "PASSES": [
        {
            "FLOAT": true,
            "HEIGHT": 1,
            "PERSISTENT": true,
            "TARGET": "timeBuffer",
            "WIDTH": 1
        },
        {
            "FLOAT": true,
            "HEIGHT": 1,
            "PERSISTENT": true,
            "TARGET": "cameraBuffer",
            "WIDTH": 1
        },
        
        {
            "FLOAT": true,
            "HEIGHT": 1,
            "PERSISTENT": true,
            "TARGET": "eqBuffer",
            "WIDTH": 1
        },
   
        {
            "TARGET": "finalOutput"
        }
    ]
}*/

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

// Helper function to convert HSV (Hue, Saturation, Value) color space to RGB.
vec3 hsv(float h, float s, float v){
    vec4 t = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(vec3(h) + t.xyz) * 6.0 - vec3(t.w));
    return v * mix(vec3(t.x), clamp(p - vec3(t.x), 0.0, 1.0), s);
}

// ACES tone mapping - better for high dynamic range
vec3 aces_tonemap(vec3 color) {
    const float a = 2.51;
    const float b = 0.03;
    const float c = 2.43;
    const float d = 0.59;
    const float e = 0.14;
    return clamp((color * (a * color + b)) / (color * (c * color + d) + e), 0.0, 1.0);
}

// Contrast adjustment function
vec3 adjustContrast(vec3 color, float contrast) {
    return 0.5 + contrast * (color - 0.5);
}


void main() {
    // --- Multi-Pass Logic ---
    if (PASSINDEX == 0) {
        // PASS 0: Update and store the smoothed, accumulated time.
        vec4 prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        float accumulatedTime = prevTimeData.r;
        float currentSpeed = prevTimeData.g;
        float newTime;
        float adjustedSpeed;
        if (FRAMEINDEX < 2) { // Use < 2 for better initialization
            newTime = 0.0;
            adjustedSpeed = speed;
        } else {
            adjustedSpeed = mix(currentSpeed, speed, min(1.0, TIMEDELTA * transitionSpeed));
            newTime = accumulatedTime + adjustedSpeed * TIMEDELTA;
        }
        gl_FragColor = vec4(newTime, adjustedSpeed, 0.0, 1.0);
    } else if (PASSINDEX == 1) {
        // PASS 1: Update and store the smoothed camera position.
        vec4 prevCamData = IMG_NORM_PIXEL(cameraBuffer, vec2(0.5, 0.5));
        vec2 currentCamPos;
        if (FRAMEINDEX < 2) { // Use < 2 for better initialization
            currentCamPos = cameraTarget;
        } else {
            currentCamPos = mix(prevCamData.xy, cameraTarget, min(1.0, TIMEDELTA * transitionSpeed));
        }
        gl_FragColor = vec4(currentCamPos, 0.0, 1.0);
    } else if (PASSINDEX == 2) {
        // --- NEW PASS 2: Update and store smoothed EQ values ---
        vec4 prevEQData = IMG_NORM_PIXEL(eqBuffer, vec2(0.5, 0.5));
        vec4 currentEQs = vec4(eq1, eq2, eq3, eq4); // Using a vec4 for the first 4 bands
        float currentEQ5 = eq5; // Separate float for the 5th
        
        vec4 smoothedEQs;
        float smoothedEQ5;

        if (FRAMEINDEX < 2) {
            smoothedEQs = currentEQs;
            smoothedEQ5 = currentEQ5;
        } else {
            // Smooth the first 4 EQ bands
            smoothedEQs = mix(prevEQData, currentEQs, min(1.0, TIMEDELTA * transitionSpeed));
            // You can also smooth the 5th band if needed, perhaps storing it in the 'w' component of a second buffer
            // For now, let's keep it simple and just use it directly or smooth it against a component.
            // Let's store the smoothed 5th value in the alpha channel of our existing buffer.
            smoothedEQ5 = mix(prevEQData.a, currentEQ5, min(1.0, TIMEDELTA * transitionSpeed));
        }
        gl_FragColor = vec4(smoothedEQs.rgb, smoothedEQ5); // We can store eq5 in the alpha channel
    } else {
        // PASS 3: Final render using smoothed values from the buffers.
        vec4 prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        float effectiveTime = prevTimeData.r;
        vec4 prevCamData = IMG_NORM_PIXEL(cameraBuffer, vec2(0.5, 0.5));
        vec2 effectiveCamPos = prevCamData.xy;

        // --- Read smoothed EQ values ---
        vec4 smoothedEQs = IMG_NORM_PIXEL(eqBuffer, vec2(0.5, 0.5));
        float s_eq1 = smoothedEQs.r;
        float s_eq2 = smoothedEQs.g;
        float s_eq3 = smoothedEQs.b;
        float s_eq4 = smoothedEQs.a; // Using alpha channel for the 4th value
        float s_eq5 = smoothedEQs.a; // Re-using 4th for simplicity, or connect to a separate logic

        // --- Camera Setup ---
        vec2 uv_centered = (isf_FragNormCoord * RENDERSIZE.xy - 0.5 * RENDERSIZE.xy) / RENDERSIZE.y;
        vec3 d = vec3(uv_centered * zoom + effectiveCamPos, 1.0);

        // --- Raymarching Loop ---
        float R = 1.0;
        float e = 1.0;
        float s = 1.0;
        vec3 q = vec3(0.0, -1.0, -1.0);
        vec3 p;
        // --- Improved Raymarching with Better Exposure Control ---
        vec4 accumulatedColor = vec4(0.0);
        float totalWeight = 0.0;
        float maxBrightness = 0.0;

        for(int i = 0; i < mainSteps; ++i){
            // Original value calculation
            float value = min(R * e * s - 0.07, e) / 7.0;
            // Generate color with exposure pre-compensation
            vec3 stepColor = hsv(-R / float(i + 1) + hueOffset, saturation, value);
            // Calculate step weight to prevent over-accumulation
            float stepWeight = exp(-totalWeight * 0.1) * (1.0 / float(mainSteps));
            // Accumulate with proper weighting
            accumulatedColor.rgb += stepColor * stepWeight;
            totalWeight += stepWeight;
                
            // Track maximum brightness for adaptive exposure
            maxBrightness = max(maxBrightness, dot(stepColor, vec3(0.299, 0.587, 0.114)));
            // March the ray forward
            p = q += d*e*R*(0.24 + s_eq1 * 0.1); // <-- EQ1 controls marching speed

            R = length(p);

            // Stability Bailout
            if (R < 0.0001 || R > 100.0) {
                break;
            }
                
            // Core fractal distance function
            p = vec3(log2(R) - effectiveTime * 0.5, exp(-p.z/R) + s_eq2 * 0.5, atan(p.y,p.x)); // <-- EQ2 modifies the shape
            // Inner loop to refine the distance estimate
            e = --p.y;
            s = 1.0;
            for(int j = 0; j < detailIterations; ++j){
                // <-- EQ3, EQ4, and EQ5 modify the detail function
                vec3 cos_offset = vec3(0.2 - s_eq3 * 0.1, 0.2 - s_eq4 * 0.1, 0.2 - s_eq5 * 0.1);
                e += dot(sin(p.yzx*s - effectiveTime), cos_offset-cos(p.yxy*s))/s*0.2;
                s *= 2.0;
            }
                
            // Improved early exit with adaptive threshold
            float currentLuminance = dot(accumulatedColor.rgb, vec3(0.299, 0.587, 0.114));
            if (currentLuminance > 0.8 * (1.0 + exposure * 0.5)) {
                break;
            }
        }

        // --- Enhanced Color Processing ---
        vec3 color = accumulatedColor.rgb;
        // Adaptive exposure based on scene content
        float adaptiveExposure = exposure - log2(max(maxBrightness, 0.01)) * 0.3;
        // Apply brightness with exposure compensation
        color *= brightness * pow(2.0, adaptiveExposure);
        // Improved tone mapping with better highlight preservation
        color = aces_tonemap(color);
        // Apply contrast and gamma
        color = adjustContrast(color, contrast);
        color = pow(max(color, vec3(0.0)), vec3(1.0 / gamma));

        gl_FragColor.rgb = color;
        gl_FragColor.a = 1.0;
    }
}