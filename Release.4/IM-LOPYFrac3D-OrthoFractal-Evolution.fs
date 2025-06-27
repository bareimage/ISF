/*{
    "DESCRIPTION": "A user-controllable fractal shader featuring manual evolution control to isolate specific visual states. Includes volumetric depth and smoothed parameters. This is very fun shader, I encoroge you to play arround with it",
    "CREDIT": "Original algorith @YoheiNishitsuji (https://twigl.app/?ol=true&ss=-ORucwaeIgR3O6O9Rg5e). Re-architected with manual evolution control by @dot2dot. ISF 2.0 Version @dot2dot",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "Generator",
        "Fractal"
    ],
    "INPUTS": [
        {
            "NAME": "evolutionLocation",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": 0.0,
            "MAX": 8.0,
            "LABEL": "Evolution Location"
        },
        {
            "NAME": "evolutionMod1",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": 0.0,
            "MAX": 8.0,
            "LABEL": "Evolution Mod"
        },
        {
            "NAME": "rotationSpeedX",
            "TYPE": "float",
            "DEFAULT": 0.1,
            "MIN": -2.0,
            "MAX": 2.0,
            "LABEL": "Rotation Speed X"
        },
        {
            "NAME": "rotationSpeedY",
            "TYPE": "float",
            "DEFAULT": 0.1,
            "MIN": -2.0,
            "MAX": 2.0,
            "LABEL": "Rotation Speed Y"
        },
        {
            "NAME": "rotationSpeedZ",
            "TYPE": "float",
            "DEFAULT": 0.1,
            "MIN": -2.0,
            "MAX": 2.0,
            "LABEL": "Rotation Speed Z"
        },
        {
            "NAME": "cameraX",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -5.0,
            "MAX": 5.0,
            "LABEL": "Camera X"
        },
        {
            "NAME": "cameraY",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -50.0,
            "MAX": 50.0,
            "LABEL": "Camera Y"
        },
        {
            "NAME": "cameraZ",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -5.0,
            "MAX": 5.0,
            "LABEL": "Camera Z"
        },
        {
            "NAME": "scale",
            "TYPE": "float",
            "DEFAULT": 2.5,
            "MIN": 0.5,
            "MAX": 10.0
        },
        {
            "NAME": "brightness",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.01,
            "MAX": 1.0
        },
        {
            "NAME": "hue_shift",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": 0.0,
            "MAX": 1.0
        },
        {
            "NAME": "fractalIterations",
            "TYPE": "float",
            "DEFAULT": 5,
            "MIN": 5,
            "MAX": 25,
            "LABEL": "Fractal Detail"
        },
        {
            "NAME": "raymarchSteps",
            "TYPE": "float",
            "DEFAULT": 10,
            "MIN": 1,
            "MAX": 40,
            "LABEL": "Render Depth"
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
            "TARGET": "rotSpeedBuffer",
            "PERSISTENT": true,
            "FLOAT": true,
            "WIDTH": 1,
            "HEIGHT": 1
        },
        {
            "TARGET": "rotationBuffer",
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

// Define mathematical constants
#define PI 3.14159265359
#define TWO_PI 6.28318530718

// Helper function to convert HSV color to RGB
vec3 hsv(float h, float s, float v) {
    vec4 t = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(vec3(h) + t.xyz) * 6.0 - vec3(t.w));
    return v * mix(vec3(t.x), clamp(p - vec3(t.x), 0.0, 1.0), s);
}

// Helper functions for 3D rotation
mat3 rotationX(float angle) {
    float s = sin(angle);
    float c = cos(angle);
    return mat3(1.0, 0.0, 0.0, 0.0, c, -s, 0.0, s, c);
}

mat3 rotationY(float angle) {
    float s = sin(angle);
    float c = cos(angle);
    return mat3(c, 0.0, s, 0.0, 1.0, 0.0, -s, 0.0, c);
}

mat3 rotationZ(float angle) {
    float s = sin(angle);
    float c = cos(angle);
    return mat3(c, -s, 0.0, s, c, 0.0, 0.0, 0.0, 1.0);
}


void main() {
    // Variable declarations for all passes
    vec4 prevAngleData, prevSpeedData;
    vec3 prevAngles, newAngles, effectiveAngles, stabilizedAngles;
    vec3 currentRotSpeed, targetRotSpeed, adjustedRotSpeed, smoothedSpeeds;
    mat3 rotMat;
    float i, g, e, s;
    vec3 p, cameraPos;
    vec2 screenPos;
    float distanceSquared;

    if (PASSINDEX == 0) {
        // --- Pass 0: Rotation SPEED smoothing ---
        prevSpeedData = IMG_NORM_PIXEL(rotSpeedBuffer, vec2(0.5, 0.5));
        currentRotSpeed = prevSpeedData.rgb;
        targetRotSpeed = vec3(rotationSpeedX, rotationSpeedY, rotationSpeedZ);
        if (FRAMEINDEX == 0) {
            adjustedRotSpeed = targetRotSpeed;
        } else {
            adjustedRotSpeed = mix(currentRotSpeed, targetRotSpeed, min(1.0, TIMEDELTA * transitionSpeed));
        }
        gl_FragColor = vec4(adjustedRotSpeed, 1.0);

    } else if (PASSINDEX == 1) {
        // --- Pass 1: Rotation ANGLE accumulation with stabilization ---
        prevAngleData = IMG_NORM_PIXEL(rotationBuffer, vec2(0.5, 0.5));
        prevAngles = prevAngleData.rgb;
        smoothedSpeeds = IMG_NORM_PIXEL(rotSpeedBuffer, vec2(0.5, 0.5)).rgb;
        newAngles = prevAngles + smoothedSpeeds * TIMEDELTA;
        stabilizedAngles = mod(newAngles + PI, TWO_PI) - PI;
        gl_FragColor = vec4(stabilizedAngles, 1.0);

    } else {
        // --- Pass 2: Final Render with Manual Evolution Control ---
        effectiveAngles = IMG_NORM_PIXEL(rotationBuffer, vec2(0.5, 0.5)).rgb;
        
        g = 0.0;
        gl_FragColor = vec4(0.0, 0.0, 0.0, 1.0);

        rotMat = rotationZ(effectiveAngles.z) * rotationY(effectiveAngles.y) * rotationX(effectiveAngles.x);
        cameraPos = vec3(cameraX, cameraY, cameraZ);
        
        // --- MANUAL EVOLUTION CONTROL ---
        // The animation is now driven directly by the 'evolutionLocation' slider.
        float manualTime = evolutionLocation;
        
        // Main rendering loop
        for (int step = 0; step < raymarchSteps; step++) {
            i = float(step + 1);
            
            screenPos = (gl_FragCoord.xy - 0.5 * RENDERSIZE) / RENDERSIZE.x;
            
            p = vec3(screenPos * scale, g - 1.5);

            p += cameraPos;
            p = rotMat * p;
            
            s = 1.0;
            
            // Inner fractal loop
            for (int fractal_step = 0; fractal_step < fractalIterations; fractal_step++) {
                distanceSquared = dot(p, p);
                distanceSquared = max(distanceSquared, 0.0001);
                e = max(1.0, 11.0 / distanceSquared);
                s *= e;
                
                // Animation is now driven by the manual time value
                vec3 offset = vec3(3.0, 2.1, 3.0 + sin(manualTime * 0.5));
                p = vec3(0.0, 3.6, 3.0) - abs(abs(p) * e - offset);
            }
            
            // Cyclical geometry logic
            g = fract(g + mod(length(p.yy), max(p.y, 0.001)) / max(s, 0.001) * 0.48);
            
            float hue = g + hue_shift;
            float value = clamp(s / 2500.0 * brightness, 0.0, 1.0);
            
            gl_FragColor.rgb += hsv(hue, 0.8, value);
        }
        
        gl_FragColor.rgb = tanh(gl_FragColor.rgb);
    }
}
