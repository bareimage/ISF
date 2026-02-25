/*{
    "DESCRIPTION": "An infinite procedural labyrinth with evolving corridors, atmospheric 3D lighting, and flow-based coloring. The camera drifts and rotates smoothly over an endless morphing maze built from Truchet-tile patterns.",
    "CREDIT": "Original by @dot2dot (bareimage). ISF 2.0 Conversion by @dot2dot (bareimage)",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "GENERATOR"
    ],
    "INPUTS": [
        {
            "NAME": "speed",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 3.0,
            "LABEL": "Camera Speed"
        },
        {
            "NAME": "zoom",
            "TYPE": "float",
            "DEFAULT": 6.0,
            "MIN": 1.0,
            "MAX": 20.0,
            "LABEL": "Maze Scale"
        },
        {
            "NAME": "corridorWidth",
            "TYPE": "float",
            "DEFAULT": 0.12,
            "MIN": 0.02,
            "MAX": 0.4,
            "LABEL": "Corridor Width"
        },
        {
            "NAME": "morphSpeed",
            "TYPE": "float",
            "DEFAULT": 0.15,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Morph Speed"
        },
        {
            "NAME": "rotationSpeed",
            "TYPE": "float",
            "DEFAULT": 0.05,
            "MIN": -0.5,
            "MAX": 0.5,
            "LABEL": "Rotation Speed"
        },
        {
            "NAME": "wallHeight",
            "TYPE": "float",
            "DEFAULT": 0.6,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Wall Depth"
        },
        {
            "NAME": "glowIntensity",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Path Glow"
        },
        {
            "NAME": "color1",
            "TYPE": "color",
            "DEFAULT": [0.08, 0.06, 0.16, 1.0],
            "LABEL": "Wall Color"
        },
        {
            "NAME": "color2",
            "TYPE": "color",
            "DEFAULT": [0.2, 0.75, 0.85, 1.0],
            "LABEL": "Path Color"
        },
        {
            "NAME": "color3",
            "TYPE": "color",
            "DEFAULT": [1.0, 0.4, 0.1, 1.0],
            "LABEL": "Glow Color"
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
            "TARGET": "morphBuffer",
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

// --- Constants ---
#define PI 3.14159265359

// --- Utility Functions ---
float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

mat2 rot2(float a) {
    float c = cos(a), s = sin(a);
    return mat2(c, -s, s, c);
}

vec3 aces(vec3 v) {
    v = max(v, 0.0);
    float a = 2.51, b = 0.03, c = 2.43, d = 0.59, e = 0.14;
    return clamp((v * (a * v + b)) / (v * (c * v + d) + e), 0.0, 1.0);
}

// --- Truchet-Tile Labyrinth ---
// Each grid cell randomly chooses a diagonal orientation,
// creating quarter-circle arc pairs that connect to form corridors.
float labyrinth(vec2 p, float seed) {
    vec2 id = floor(p);
    vec2 f = fract(p);

    // Hash determines tile orientation
    float h = hash(id + seed * 0.317);
    if (h > 0.5) f.x = 1.0 - f.x;

    // Quarter-circle arcs from opposite corners
    float d1 = abs(length(f) - 1.0);
    float d2 = abs(length(f - 1.0) - 1.0);

    return min(d1, d2);
}

// Smoothly morphing labyrinth between two hash states
float labyrinthMorph(vec2 p, float morphTime) {
    float cycleDuration = 6.0;
    float cycle = morphTime / cycleDuration;
    float idx = floor(cycle);
    float t = fract(cycle);

    // Snap transition: spend most time in stable states
    t = smoothstep(0.25, 0.75, t);

    float d1 = labyrinth(p, idx);
    float d2 = labyrinth(p, idx + 1.0);

    return mix(d1, d2, t);
}

// --- Main Render ---
vec3 renderLabyrinth(vec2 fragCoord, vec2 renderSize,
    float camTime, float morphTime, float rotAngle,
    float zoomVal, float cwVal, float whVal, float glowVal,
    vec3 wallCol, vec3 pathCol, vec3 glowCol) {

    // Centered UV
    vec2 uv = (2.0 * fragCoord - renderSize) / renderSize.y;

    // Camera rotation
    uv = rot2(rotAngle) * uv;

    // Smooth camera path (Lissajous drift + linear advance)
    vec2 camPos = vec2(
        camTime * 0.6 + sin(camTime * 0.41) * 2.5,
        camTime * 0.4 + cos(camTime * 0.29) * 2.5
    );

    // Final maze coordinates
    vec2 p = uv * zoomVal + camPos;

    // Get morphing distance field
    float d = labyrinthMorph(p, morphTime);

    // --- Corridor / Wall Classification ---
    float aa = 1.5 / renderSize.y * zoomVal;
    float corridor = smoothstep(cwVal + aa, cwVal - aa, d);

    // --- Pseudo-3D Lighting ---
    // Compute gradient of the distance field for normal estimation
    vec2 eps = vec2(0.005, 0.0);
    float d_r = labyrinthMorph(p + eps.xy, morphTime);
    float d_u = labyrinthMorph(p + eps.yx, morphTime);
    float dx = d_r - d;
    float dy = d_u - d;

    // Surface normal (as if walls are extruded upward)
    vec3 N = normalize(vec3(-dx * whVal * 20.0, 1.0, -dy * whVal * 20.0));

    // Directional light from upper-left
    vec3 L = normalize(vec3(-0.4, 1.0, -0.3));
    float diffuse = max(dot(N, L), 0.0);
    float ambient = 0.25;
    float light = ambient + (1.0 - ambient) * diffuse;

    // --- Shadow Offset ---
    // Walls cast shadows in the light direction
    vec2 shadowOffset = vec2(0.04, 0.04) * whVal;
    float dShadow = labyrinthMorph(p + shadowOffset, morphTime);
    float shadowCorridor = smoothstep(cwVal + aa, cwVal - aa, dShadow);
    float shadow = mix(1.0, 0.3 + 0.7 * shadowCorridor, whVal);

    // --- Ambient Occlusion ---
    // Corridors near walls are darker
    float ao = smoothstep(0.0, cwVal * 2.0, d);
    ao = 0.5 + 0.5 * ao;

    // --- Path Glow ---
    float glow = exp(-d * 10.0) * glowVal;

    // Secondary glow layer at finer scale for depth
    float d2 = labyrinthMorph(p * 3.0 + 50.0, morphTime * 0.7);
    float glowDetail = exp(-d2 * 15.0) * glowVal * 0.3;

    // --- Flow Coloring ---
    // Color variation along corridors based on gradient direction
    float flowAngle = atan(dy, dx);
    float flow = sin(flowAngle * 3.0 + morphTime * 0.8) * 0.5 + 0.5;

    // Position-based color variation for paths
    float posColor = sin(p.x * 0.3 + p.y * 0.7 + camTime * 0.3) * 0.5 + 0.5;

    // --- Compose Final Color ---
    // Wall color with lighting
    vec3 wCol = wallCol * light;

    // Path color with flow variation and shadow
    vec3 pCol = mix(pathCol * 0.7, pathCol * 1.3, flow * 0.5 + posColor * 0.5);
    pCol *= shadow * ao;

    // Mix wall and corridor
    vec3 col = mix(wCol, pCol, corridor);

    // Add glow layers
    col += glowCol * glow;
    col += glowCol * 0.5 * glowDetail;

    // Subtle edge highlight on corridor/wall boundary
    float edge = smoothstep(cwVal + aa * 3.0, cwVal, d) * smoothstep(cwVal - aa * 3.0, cwVal, d);
    col += pathCol * edge * 0.3;

    // --- Vignette ---
    vec2 vUV = fragCoord / renderSize - 0.5;
    float vig = 1.0 - dot(vUV, vUV) * 0.6;
    col *= vig;

    // --- Tonemap ---
    col = aces(col);

    return col;
}

void main() {
    float sf = min(1.0, TIMEDELTA * transitionSpeed);

    // Pass 0: Time Buffer (camera time + rotation)
    if (PASSINDEX == 0) {
        vec4 prev = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        float accTime, smSpeed, accRot, smRot;

        if (FRAMEINDEX == 0) {
            accTime = 0.0;
            smSpeed = speed;
            accRot = 0.0;
            smRot = rotationSpeed;
        } else {
            smSpeed = mix(prev.g, speed, sf);
            smRot = mix(prev.a, rotationSpeed, sf);
            accTime = prev.r + smSpeed * TIMEDELTA;
            accRot = prev.b + smRot * TIMEDELTA;
        }

        gl_FragColor = vec4(accTime, smSpeed, accRot, smRot);
    }
    // Pass 1: Morph Buffer (morph time accumulation)
    else if (PASSINDEX == 1) {
        vec4 prev = IMG_NORM_PIXEL(morphBuffer, vec2(0.5));
        float accMorph, smMorph;

        if (FRAMEINDEX == 0) {
            accMorph = 0.0;
            smMorph = morphSpeed;
        } else {
            smMorph = mix(prev.g, morphSpeed, sf);
            accMorph = prev.r + smMorph * TIMEDELTA;
        }

        gl_FragColor = vec4(accMorph, smMorph, 0.0, 1.0);
    }
    // Pass 2: Param Buffer (zoom, corridorWidth, wallHeight, glow)
    else if (PASSINDEX == 2) {
        vec4 prev = IMG_NORM_PIXEL(paramBuffer, vec2(0.5));
        vec4 target = vec4(zoom, corridorWidth, wallHeight, glowIntensity);

        if (FRAMEINDEX == 0) {
            gl_FragColor = target;
        } else {
            gl_FragColor = mix(prev, target, sf);
        }
    }
    // Pass 3: Color Buffer (3 smoothed colors)
    else if (PASSINDEX == 3) {
        vec2 coord = gl_FragCoord.xy;
        vec4 prev = IMG_PIXEL(colorBuffer, coord);
        vec4 target;

        if (coord.x < 1.0)       target = vec4(color1.rgb, 1.0);
        else if (coord.x < 2.0)  target = vec4(color2.rgb, 1.0);
        else                     target = vec4(color3.rgb, 1.0);

        if (FRAMEINDEX == 0) gl_FragColor = target;
        else gl_FragColor = mix(prev, target, sf);
    }
    // Pass 4: Final Render
    else {
        // Retrieve all smoothed values
        vec4 timeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        vec4 morphData = IMG_NORM_PIXEL(morphBuffer, vec2(0.5));
        vec4 params = IMG_NORM_PIXEL(paramBuffer, vec2(0.5));

        vec3 c1 = IMG_NORM_PIXEL(colorBuffer, vec2(1.0/6.0, 0.5)).rgb;
        vec3 c2 = IMG_NORM_PIXEL(colorBuffer, vec2(3.0/6.0, 0.5)).rgb;
        vec3 c3 = IMG_NORM_PIXEL(colorBuffer, vec2(5.0/6.0, 0.5)).rgb;

        vec3 col = renderLabyrinth(
            gl_FragCoord.xy, RENDERSIZE,
            timeData.r, morphData.r, timeData.b,
            params.r, params.g, params.b, params.a,
            c1, c2, c3
        );

        gl_FragColor = vec4(col, 1.0);
    }
}