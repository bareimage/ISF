/*{
    "DESCRIPTION": "A twisting voxel face tunnel that wanders around the screen with a fluid, physics-based motion. Features procedurally generated patterns and fine-grained control over all parameters.",
    "CREDIT": "Original by @dot2dot (bareimage). ISF 2.0 Conversion by @dot2dot (bareimage)",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "GENERATOR"
    ],
    "INPUTS": [
        {
            "NAME": "speed",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 50.0,
            "LABEL": "Animation Speed"
        },
        {
            "NAME": "zoom",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 5.0,
            "LABEL": "Zoom"
        },
        {
            "NAME": "wanderAmount",
            "TYPE": "float",
            "DEFAULT": 0.4,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Wander Amount"
        },
        {
            "NAME": "wanderSpeed",
            "TYPE": "float",
            "DEFAULT": 0.2,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Wander Speed"
        },
        {
            "NAME": "faceScale",
            "TYPE": "float",
            "DEFAULT": 13.0,
            "MIN": 5.0,
            "MAX": 25.0,
            "LABEL": "Face Scale"
        },
        {
            "NAME": "twistAmount",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Twist Amount"
        },
        {
            "NAME": "twistFrequency",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 5.0,
            "LABEL": "Twist Frequency"
        },
        {
            "NAME": "color1",
            "TYPE": "color",
            "DEFAULT": [1.0, 0.9, 0.2, 1.0],
            "LABEL": "Color 1 (Yellow)"
        },
        {
            "NAME": "color2",
            "TYPE": "color",
            "DEFAULT": [0.2, 0.5, 1.0, 1.0],
            "LABEL": "Color 2 (Blue)"
        },
        {
            "NAME": "color3",
            "TYPE": "color",
            "DEFAULT": [0.8, 0.3, 1.0, 1.0],
            "LABEL": "Color 3 (Purple)"
        },
        {
            "NAME": "transitionSpeed",
            "TYPE": "float",
            "DEFAULT": 2.0,
            "MIN": 0.1,
            "MAX": 10.0,
            "LABEL": "Parameter Smoothing"
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
            "TARGET": "wanderStateBuffer",
            "PERSISTENT": true,
            "FLOAT": true,
            "WIDTH": 1,
            "HEIGHT": 1
        },
        {
            "TARGET": "paramBuffer",
            "PERSISTENT": true,
            "FLOAT": true,
            "WIDTH": 1,
            "HEIGHT": 1
        },
        {
            "TARGET": "colorBuffer",
            "PERSISTENT": true,
            "FLOAT": true,
            "WIDTH": 3,
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

// --- Constants and Macros ---
#define PI 3.14159265359

// --- Helper Functions ---
float sdBox(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

float random(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

float getCellValue(vec2 gridIndex, float time, float gridSize) {
    float cycleIndex = floor(time / 4.0);
    vec2 symmetricalIndex = gridIndex;
    if (symmetricalIndex.x > (gridSize - 1.0) / 2.0) {
        symmetricalIndex.x = (gridSize - 1.0) - symmetricalIndex.x;
    }
    float randValue = random(symmetricalIndex + cycleIndex);
    if (gridIndex.x < 1.0 || gridIndex.x >= (gridSize - 1.0) || gridIndex.y < 1.0 || gridIndex.y >= (gridSize - 1.0)) {
        return 1.0;
    }
    return step(0.5, randValue);
}

// --- Main Rendering Logic ---
vec3 renderTunnel(vec2 fragCoord, vec2 renderSize, float time, float zoom, float faceScale, float twistAmount, float twistFrequency, vec3 c1, vec3 c2, vec3 c3, vec2 centerOffset) {
    vec2 uv = ((2.0 * fragCoord - renderSize) / renderSize.y) - centerOffset;
    float tunnelRadius = length(uv) + 0.001;
    float tunnelAngle = atan(uv.y, uv.x);
    float z = (1.0 / tunnelRadius) * zoom;
    z += time; 
    float angle = tunnelAngle;
    angle += sin(z * twistFrequency) * twistAmount;
    vec2 tunnelUV = vec2(z, angle / PI);
    tunnelUV.y += 0.5;

    // --- Grid Logic with "Mirror Grow" ---
    // This new logic implements your suggestion to scale from the center of each face.
    vec2 tileCount = vec2(2.0, 3.0);
    vec2 tiled_uv = tunnelUV * tileCount;
    
    // 1. Get the integer and fractional parts of the tiled coordinates.
    vec2 uv_id = floor(tiled_uv);
    vec2 uv_fract = fract(tiled_uv);
    
    // 2. Center the fractional part (so it ranges from -0.5 to 0.5).
    vec2 uv_centered = uv_fract - 0.5;
    
    // 3. Scale this centered coordinate. This is the "mirror grow".
    vec2 uv_scaled = uv_centered * faceScale;
    
    // 4. Reconstruct the final grid coordinate from the scaled integer and fractional parts.
    vec2 gridUv = uv_id * faceScale + uv_scaled;

    vec2 gridId = floor(gridUv);
    vec2 wrappedGridIndex = mod(gridId, vec2(faceScale, faceScale));
    float layer = floor(tunnelUV.x);
    float cellValue = getCellValue(wrappedGridIndex, layer, faceScale);

    if (cellValue < 0.5) {
        return vec3(0.0);
    }

    // --- Voxel SDF Drawing ---
    vec2 cellUv = fract(gridUv) - 0.5;
    float aaWidth = (1.0 / renderSize.y) * tunnelRadius * 2.0;
    int patternIndex = int(mod(floor(layer / 4.0), 3.0));
    vec3 baseColor;
    if (patternIndex == 0)      baseColor = c1;
    else if (patternIndex == 1) baseColor = c2;
    else                        baseColor = c3;
    vec2 offset = vec2(0.07) * tunnelRadius;
    vec3 finalColor = vec3(0.0);
    float backDist = sdBox(cellUv - offset, vec2(0.45));
    float backAlpha = smoothstep(aaWidth, -aaWidth, backDist);
    finalColor = mix(finalColor, baseColor * 0.4, backAlpha);
    float frontDist = sdBox(cellUv, vec2(0.45));
    float frontAlpha = smoothstep(aaWidth, -aaWidth, frontDist);
    finalColor = mix(finalColor, baseColor, frontAlpha);

    // -- CORRECTED PART --
    // float fog = smoothstep(1.5, 0.1, tunnelRadius);
    // finalColor *= fog;
    
    return finalColor;
}

void main() {
    float smoothingFactor = min(1.0, TIMEDELTA * transitionSpeed);

    // Pass 0: Time Buffer
    if (PASSINDEX == 0) {
        vec4 prevData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        float newTime, adjustedSpeed;
        if (FRAMEINDEX == 0) {
            newTime = 0.0;
            adjustedSpeed = speed;
        } else {
            adjustedSpeed = mix(prevData.g, speed, smoothingFactor);
            newTime = prevData.r + adjustedSpeed * TIMEDELTA;
        }
        gl_FragColor = vec4(newTime, adjustedSpeed, 0.0, 1.0);
    }
    // Pass 1: Wander State Buffer
    else if (PASSINDEX == 1) {
        vec4 prevState = IMG_NORM_PIXEL(wanderStateBuffer, vec2(0.5));
        float newWanderTime, smoothedWanderSpeed, smoothedWanderAmount;
        if (FRAMEINDEX == 0) {
            newWanderTime = 0.0;
            smoothedWanderSpeed = wanderSpeed;
            smoothedWanderAmount = wanderAmount;
        } else {
            smoothedWanderSpeed = mix(prevState.y, wanderSpeed, smoothingFactor);
            smoothedWanderAmount = mix(prevState.z, wanderAmount, smoothingFactor);
            newWanderTime = prevState.x + smoothedWanderSpeed * TIMEDELTA;
        }
        gl_FragColor = vec4(newWanderTime, smoothedWanderSpeed, smoothedWanderAmount, 1.0);
    }
    // Pass 2: General Parameters Buffer
    else if (PASSINDEX == 2) {
        vec4 prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5));
        vec4 currentParams;
        if (FRAMEINDEX == 0) {
            currentParams = vec4(zoom, 0.0, 0.0, 0.0);
        } else {
            vec4 targetParams = vec4(zoom, 0.0, 0.0, 0.0);
            currentParams = mix(prevParamData, targetParams, smoothingFactor);
        }
        gl_FragColor = currentParams;
    }
    // Pass 3: Color Buffer
    else if (PASSINDEX == 3) {
        vec2 coord = gl_FragCoord.xy;
        vec4 prevColor = IMG_PIXEL(colorBuffer, coord);
        vec4 targetColor;
        if (coord.x < 1.0)       targetColor = vec4(color1.rgb, 1.0);
        else if (coord.x < 2.0)  targetColor = vec4(color2.rgb, 1.0);
        else                     targetColor = vec4(color3.rgb, 1.0);
        if(FRAMEINDEX == 0) gl_FragColor = targetColor;
        else gl_FragColor = mix(prevColor, targetColor, smoothingFactor);
    }
    // Pass 4: Final Render
    else {
        vec4 timeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        vec4 wanderState = IMG_NORM_PIXEL(wanderStateBuffer, vec2(0.5));
        vec4 paramData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5));
        vec3 c1 = (IMG_NORM_PIXEL(colorBuffer, vec2(1.0/6.0, 0.5))).rgb;
        vec3 c2 = (IMG_NORM_PIXEL(colorBuffer, vec2(3.0/6.0, 0.5))).rgb;
        vec3 c3 = (IMG_NORM_PIXEL(colorBuffer, vec2(5.0/6.0, 0.5))).rgb;
        
        float finalWanderTime = wanderState.x;
        float finalWanderAmount = wanderState.z;
        vec2 wanderPos = vec2(sin(finalWanderTime), cos(finalWanderTime * 0.7)) * finalWanderAmount;

        vec3 col = renderTunnel(gl_FragCoord.xy, RENDERSIZE, timeData.r, 
                                paramData.r,
                                faceScale,
                                twistAmount, twistFrequency,
                                c1, c2, c3,
                                wanderPos);
        
        gl_FragColor = vec4(col, 1.0);
    }
}