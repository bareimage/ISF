/*{
    "DESCRIPTION": "Original EmoCube, not very engaging, but I wanted to include it for reference. Shader features tumbling cube with a different carved emotion on each face. Features smoothed, time-independent animation speed and advanced materials.",
    "CREDIT": "Original by @dot2dot (bareimage), rendering pipeline by @mrange. ISF 2.0 Conversion by @dot2dot (bareimage)",
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
            "MAX": 5.0,
            "LABEL": "Rotation Speed"
        },
        {
            "NAME": "transitionSpeed",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 5.0,
            "LABEL": "Speed Transition Smoothness"
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

// --- SDF Primitives ---

// SDF for a 2D box (rectangle).
float sdBox2D(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

// SDF for a 3D box.
float sdBox(vec3 p, vec3 b) {
    vec3 q = abs(p) - b;
    return length(max(q, vec3(0.0))) + min(max(q.x, max(q.y, q.z)), 0.0);
}

// --- Face Carving SDFs for Different Emotions ---

// Neutral Face (+Z)
float getCarvingSDF_neutral(vec2 p) {
    float d_eyes = min(sdBox2D(p - vec2(-3.0, 3.0), vec2(1.5, 0.5)), sdBox2D(p - vec2(3.0, 3.0), vec2(1.5, 0.5)));
    float d_nose = min(sdBox2D(p - vec2(0.0, 2.45), vec2(0.5, 1.05)), sdBox2D(p - vec2(0.0, 1.0), vec2(1.5, 0.5)));
    float d_mouth = max(sdBox2D(p - vec2(0.0, -2.0), vec2(3.5, 1.5)), -sdBox2D(p - vec2(0.0, -2.0), vec2(1.5, 0.5)));
    return min(min(d_eyes, d_nose), d_mouth);
}

// Happy Face (+X)
float getCarvingSDF_happy(vec2 p) {
    float d_eyes = min(sdBox2D(p - vec2(-3.0, 3.0), vec2(1.5, 0.5)), sdBox2D(p - vec2(3.0, 3.0), vec2(1.5, 0.5)));
    // Up-turned mouth
    float d_mouth = min(sdBox2D(p - vec2(0.0, -2.0), vec2(3.0, 0.5)), sdBox2D(p - vec2(0.0, -2.5), vec2(2.0, 0.5)));
    d_mouth = min(d_mouth, sdBox2D(p-vec2(-2.5,-1.5),vec2(0.5)));
    d_mouth = min(d_mouth, sdBox2D(p-vec2(2.5,-1.5),vec2(0.5)));
    return min(d_eyes, d_mouth);
}

// Sad Face (-Z)
float getCarvingSDF_sad(vec2 p) {
    float d_eyes = min(sdBox2D(p - vec2(-3.0, 3.0), vec2(1.5, 0.5)), sdBox2D(p - vec2(3.0, 3.0), vec2(1.5, 0.5)));
    // Down-turned mouth
    float d_mouth = min(sdBox2D(p - vec2(0.0, -2.5), vec2(3.0, 0.5)), sdBox2D(p - vec2(0.0, -2.0), vec2(2.0, 0.5)));
    d_mouth = min(d_mouth, sdBox2D(p-vec2(-2.5,-3.0),vec2(0.5)));
    d_mouth = min(d_mouth, sdBox2D(p-vec2(2.5,-3.0),vec2(0.5)));
    return min(d_eyes, d_mouth);
}

// Angry Face (-X)
float getCarvingSDF_angry(vec2 p) {
    // Angled eyebrows
    mat2 R1 = mat2(cos(0.5), -sin(0.5), sin(0.5), cos(0.5));
    mat2 R2 = mat2(cos(-0.5), -sin(-0.5), sin(-0.5), cos(-0.5));
    float d_eye1 = sdBox2D((p - vec2(-3.0, 3.5)) * R1, vec2(1.8, 0.4));
    float d_eye2 = sdBox2D((p - vec2(3.0, 3.5)) * R2, vec2(1.8, 0.4));
    float d_mouth = sdBox2D(p-vec2(0.0,-2.5), vec2(3.0,0.5));
    return min(min(d_eye1, d_eye2), d_mouth);
}

// Surprised Face (+Y)
float getCarvingSDF_surprised(vec2 p) {
    // Rounder eyes and mouth
    float d_eyes = min(sdBox2D(p - vec2(-3.0, 3.0), vec2(1.0, 1.0)), sdBox2D(p - vec2(3.0, 3.0), vec2(1.0, 1.0)));
    float d_mouth = sdBox2D(p - vec2(0.0, -2.0), vec2(1.5, 1.5));
    return min(d_eyes, d_mouth);
}

// Wink Face (-Y)
float getCarvingSDF_wink(vec2 p) {
    float d_eye1 = sdBox2D(p - vec2(-3.0, 3.0), vec2(1.5, 0.5));
    // Closed eye
    float d_eye2 = sdBox2D(p - vec2(3.0, 3.5), vec2(1.5, 0.2));
    float d_mouth = max(sdBox2D(p - vec2(0.0, -2.0), vec2(3.5, 1.5)), -sdBox2D(p - vec2(0.0, -2.0), vec2(1.5, 0.5)));
    return min(min(d_eye1, d_eye2), d_mouth);
}


// The master SDF for the entire scene.
float map(vec3 p) {
    float d_cube = sdBox(p, vec3(6.5));
    vec3 p_abs = abs(p);

    float d_carving_2d;
    vec2 face_uv;
    float carving_depth_axis;

    // Determine which face we are on and select the correct SDF and UV coordinates.
    if (p_abs.z > p_abs.x && p_abs.z > p_abs.y) { // Z faces
        face_uv = p.xy;
        carving_depth_axis = p.z;
        d_carving_2d = p.z > 0.0 ? getCarvingSDF_neutral(face_uv) : getCarvingSDF_sad(face_uv);
    } else if (p_abs.x > p_abs.y) { // X faces
        face_uv = p.zy; // Use zy for UVs on the X face
        carving_depth_axis = p.x;
        d_carving_2d = p.x > 0.0 ? getCarvingSDF_happy(face_uv) : getCarvingSDF_angry(face_uv);
    } else { // Y faces
        face_uv = p.xz; // Use xz for UVs on the Y face
        carving_depth_axis = p.y;
        d_carving_2d = p.y > 0.0 ? getCarvingSDF_surprised(face_uv) : getCarvingSDF_wink(face_uv);
    }

    // Extrude the 2D carving SDF into a 3D tool and subtract it from the cube.
    float d_z_axis = abs(abs(carving_depth_axis) - 6.005) - 0.505;
    vec2 w = vec2(d_carving_2d, d_z_axis);
    float d_carving_tool = min(max(w.x, w.y), 0.0) + length(max(w, 0.0));
    
    return max(d_cube, -d_carving_tool);
}


void main() {
    // Pass 0: Manage and smooth time accumulation for animation.
    if (PASSINDEX == 0) {
        vec4 prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        
        float accumulatedTime = prevTimeData.r;
        float currentSpeed = prevTimeData.g;
        
        float newTime;
        float adjustedSpeed;

        if (FRAMEINDEX == 0) {
            newTime = 0.0;
            adjustedSpeed = speed;
        } else {
            adjustedSpeed = mix(currentSpeed, speed, min(1.0, TIMEDELTA * transitionSpeed));
            newTime = accumulatedTime + adjustedSpeed * TIMEDELTA;
        }
        
        gl_FragColor = vec4(newTime, adjustedSpeed, 0.0, 1.0);
    } 
    // Pass 1: Render the final scene
    else {
        vec4 timeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        float effectiveTime = timeData.r;

        // Setup normalized screen coordinates
        // FIX 1: Use gl_FragCoord.xy instead of isf_FragCoord
        vec2 uv = (gl_FragCoord.xy - 0.5 * RENDERSIZE.xy) / RENDERSIZE.y;
        
        // This is the quirky rotation matrix from the original shader
        vec4 cos_args = effectiveTime + vec4(0.0, 11.0, 33.0, 0.0);
        mat2 R = mat2(cos(cos_args.x), cos(cos_args.y), cos(cos_args.z), cos(cos_args.w));

        // Raymarching loop with tumbling motion
        float t = 0.0;
        float i = 0.0; // Step counter
        const int MAX_STEPS = 100;

        // The rendering pipeline now EXACTLY matches the original shader
        // FIX 2: Add curly braces to correctly define the loop's scope
        for (int j = 0; j < MAX_STEPS; j++) {
            i += 1.0;
            
            // 1. Calculate point on ray from origin
            vec3 pos = normalize(vec3(uv, 1.0)) * t; 
            
            // 2. Translate the point (moves camera back)
            pos.z -= 20.0; 
            
            // 3. Apply the tumbling rotations
            pos.xz *= R;
            pos.yz *= R;

            // 4. Get distance and step forward
            float d = map(pos);
            if (d < 0.001) break;
            
            t += d * 0.5; // Step forward
            if (t > 100.0) break;
        }

        // Use the original aesthetic coloring formula
        vec3 col;
        if (t < 100.0) { // If we hit something
            vec4 color_vec = 1.0 + sin(vec4(0.0, 0.5, 1.0, 0.0) - i / 33.0 + 2.0 * (uv.x + uv.y));
            col = color_vec.rgb * 0.5;
        } else { // If we missed
            col = vec3(0.0);
        }
        
        gl_FragColor = vec4(col, 1.0);
    }
}