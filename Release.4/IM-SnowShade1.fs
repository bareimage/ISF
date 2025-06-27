/*{
  "DESCRIPTION": "Conversion of a complex fractal shader. Features dampened controls for 3D rotation, scale, multiple colors, and detail.",
  "CREDIT": "Concept by @YoheiNishitsuji (https://x.com/YoheiNishitsuji/status/1923362809569837131). Converted to ISF 2.0 with enhancements by @dot2dot.",
  "ISFVSN": "2.0",
  "CATEGORIES": [
    "GENERATOR"
  ],
  "INPUTS": [
    {
      "NAME": "rotationX",
      "TYPE": "float",
      "DEFAULT": -90.0,
      "MIN": -180.0,
      "MAX": 180.0,
      "LABEL": "Rotation X"
    },
    {
      "NAME": "rotationY",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": -180.0,
      "MAX": 180.0,
      "LABEL": "Rotation Y"
    },
    {
      "NAME": "rotationZ",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": -180.0,
      "MAX": 180.0,
      "LABEL": "Rotation Z"
    },
    {
      "NAME": "rotationSpeed",
      "TYPE": "float",
      "DEFAULT": 0.1,
      "MIN": -1.0,
      "MAX": 1.0,
      "LABEL": "Secondary Rotation Speed"
    },
    {
        "NAME": "scale",
        "TYPE": "float",
        "DEFAULT": 1.0,
        "MIN": 0.1,
        "MAX": 5.0,
        "LABEL": "Scale"
    },
    {
        "NAME": "brightness",
        "TYPE": "float",
        "DEFAULT": 2.5,
        "MIN": 0.0,
        "MAX": 15.0,
        "LABEL": "Brightness"
    },
    {
      "NAME": "color1",
      "TYPE": "color",
      "DEFAULT": [0.5, 0.1, 1.0, 1.0],
      "LABEL": "Color 1 (Center)"
    },
    {
      "NAME": "color2",
      "TYPE": "color",
      "DEFAULT": [1.0, 0.8, 0.2, 1.0],
      "LABEL": "Color 2 (Edges)"
    },
    {
        "NAME": "detail",
        "TYPE": "float",
        "DEFAULT": 99.0,
        "MIN": 1.0,
        "MAX": 150.0,
        "LABEL": "Outer Loop Detail"
    },
    {
        "NAME": "complexity",
        "TYPE": "float",
        "DEFAULT": 12.0,
        "MIN": 1.0,
        "MAX": 30.0,
        "LABEL": "Inner Loop Complexity"
    },
    {
      "NAME": "transitionSpeed",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.1,
      "MAX": 10.0,
      "LABEL": "Transition Smoothness"
    }
  ],
  "PASSES": [
    {
      "TARGET": "bufferA",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": 1,
      "HEIGHT": 1
    },
    {
      "TARGET": "bufferB",
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

// Utility functions
mat2 rotate2D(float angle){
    return mat2(cos(angle), -sin(angle),
                sin(angle), cos(angle));
}

// Creates a 3D rotation matrix from Euler angles (in radians)
mat3 rotationMatrixXYZ(vec3 angles) {
    vec3 c = cos(angles);
    vec3 s = sin(angles);
    mat3 rotX = mat3(1.0, 0.0, 0.0, 0.0, c.x, -s.x, 0.0, s.x, c.x);
    mat3 rotY = mat3(c.y, 0.0, s.y, 0.0, 1.0, 0.0, -s.y, 0.0, c.y);
    mat3 rotZ = mat3(c.z, -s.z, 0.0, s.z, c.z, 0.0, 0.0, 0.0, 1.0);
    return rotZ * rotY * rotX;
}


void main() {
    if (PASSINDEX == 0) {
        // First Pass (Buffer A): Update and smooth rotation parameters.
        vec4 prevParams = IMG_NORM_PIXEL(bufferA, vec2(0.5, 0.5));
        
        // .r: rotX, .g: rotY, .b: rotZ, .a: rotationSpeed
        vec4 smoothedVals;
        if (FRAMEINDEX == 0) {
            smoothedVals = vec4(rotationX, rotationY, rotationZ, rotationSpeed);
        } else {
            float smoothness = min(1.0, TIMEDELTA * transitionSpeed);
            smoothedVals.r = mix(prevParams.r, rotationX, smoothness);
            smoothedVals.g = mix(prevParams.g, rotationY, smoothness);
            smoothedVals.b = mix(prevParams.b, rotationZ, smoothness);
            smoothedVals.a = mix(prevParams.a, rotationSpeed, smoothness);
        }
        gl_FragColor = smoothedVals;

    } else if (PASSINDEX == 1) {
        // Second Pass (Buffer B): Update and smooth scale and detail parameters.
        vec4 prevParams = IMG_NORM_PIXEL(bufferB, vec2(0.5, 0.5));
        
        // .r: detail, .g: complexity, .b: scale
        vec4 smoothedVals;
        if (FRAMEINDEX == 0) {
            smoothedVals = vec4(detail, complexity, scale, 0.0);
        } else {
            float smoothness = min(1.0, TIMEDELTA * transitionSpeed);
            smoothedVals.r = mix(prevParams.r, detail, smoothness);
            smoothedVals.g = mix(prevParams.g, complexity, smoothness);
            smoothedVals.b = mix(prevParams.b, scale, smoothness);
        }
        gl_FragColor = smoothedVals;

    } else {
        // Final Pass: Render the visual using the smoothed parameters.
        vec4 rots       = IMG_NORM_PIXEL(bufferA, vec2(0.5, 0.5));
        vec4 other      = IMG_NORM_PIXEL(bufferB, vec2(0.5, 0.5));
        
        vec3  effectiveEulerAngles = rots.xyz;
        float effectiveRotationSpeed = rots.a;
        float effectiveDetail = other.r;
        float effectiveComplexity = other.g;
        float effectiveScale = other.b;
        
        // Initialize variables
        vec4 outColor = vec4(0.0, 0.0, 0.0, 1.0);
        float g = 0.0, e = 0.0, s = 0.0;
        
        // Create main rotation matrix from smoothed Euler angles
        mat3 mainRotation = rotationMatrixXYZ(radians(effectiveEulerAngles));

        // Main rendering loop adapted from the original shader
        for(float i = 0.0; i < effectiveDetail; ++i){
            // Apply the main rotation and scale
            vec3 p = mainRotation * vec3((isf_FragNormCoord.xy * RENDERSIZE.xy * 2.0 - RENDERSIZE.xy) / RENDERSIZE.y * 2.0, g - 6.0) * effectiveScale;
            
            // Apply the secondary rotation
            p.xz = rotate2D(TIME * effectiveRotationSpeed) * p.xz;
            
            s = 6.0;

            // Inner loop
            for(float j = 0.0; j < effectiveComplexity; ++j) {
                s *= e = 7.5 / dot(p, p * 0.47);
                p = vec3(0, 4.03, -1) - abs(abs(p) * e - vec3(3, 4, 3));
            }
            
            g += p.y * p.y / s * 0.3;
            s = log2(s) - g * 0.8;
            
            // --- COLOR CALCULATION ---
            // Calculate the mix factor based on the main loop iterator to create a gradient from center to edge.
            float mixFactor = i / effectiveDetail; 
            // Mix between color1 and color2 based on the factor.
            vec3 finalColor = mix(color1.rgb, color2.rgb, mixFactor);

            // Apply final color with brightness adjustment
            outColor.rgb += finalColor * (s / 7e2) * brightness;
        }

        gl_FragColor = outColor;
    }
}
