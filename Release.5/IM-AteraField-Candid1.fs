/*{
    "DESCRIPTION": "Flowing 2.5D layers displaying animated 2D cross-sections of a 3D icosahedron field. Integrates object properties and rotation controls from ChunderFPV's 'Stellated Dodecahedron' shader, including automatic rotation and a post-process radial blur.",
    "CREDIT": "Original by @dot2dot (bareimage). ISF 2.0 Conversion by @dot2dot (bareimage)",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "GENERATOR",
        "3D",
        "BLUR"
    ],
    "INPUTS": [
        {
            "NAME": "zoom",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.1,
            "MAX": 4.0,
            "LABEL": "Zoom"
        },
        {
            "NAME": "fov",
            "TYPE": "float",
            "DEFAULT": 0.45,
            "MIN": 0.1,
            "MAX": 1.0,
            "LABEL": "Field of View (Perspective)"
        },
        {
            "NAME": "objectDensity",
            "TYPE": "float",
            "DEFAULT": 4.0,
            "MIN": 0.0,
            "MAX": 10.0,
            "LABEL": "Object Density"
        },
        {
            "NAME": "rotationSpeed",
            "TYPE": "float",
            "DEFAULT": 0.2,
            "MIN": 2.0,
            "MAX": -2.0,
            "LABEL": "Rotation Speed"
        },
        {
            "NAME": "movementSpeed",
            "TYPE": "float",
            "DEFAULT": 0.4,
            "MIN": -2.0,
            "MAX": 2.0,
            "LABEL": "Movement Speed"
        },
        {
            "NAME": "depthSeparation",
            "TYPE": "float",
            "DEFAULT": 0.25,
            "MIN": 0.1,
            "MAX": 0.5,
            "LABEL": "Layer Depth Separation"
        },
        {
            "NAME": "waveIntensity",
            "TYPE": "float",
            "DEFAULT": 0.02,
            "MIN": 0.0,
            "MAX": 0.1,
            "LABEL": "Wave Distortion Intensity"
        },
        {
            "NAME": "color1",
            "TYPE": "color",
            "DEFAULT": [0.8, 0.2, 0.2, 1.0],
            "LABEL": "Color 1"
        },
        {
            "NAME": "color2",
            "TYPE": "color",
            "DEFAULT": [0.2, 0.8, 0.2, 1.0],
            "LABEL": "Color 2"
        },
        {
            "NAME": "color3",
            "TYPE": "color",
            "DEFAULT": [0.2, 0.2, 0.8, 1.0],
            "LABEL": "Color 3"
        },
        {
            "NAME": "color4",
            "TYPE": "color",
            "DEFAULT": [0.8, 0.8, 0.2, 1.0],
            "LABEL": "Color 4"
        },
        {
            "NAME": "paletteTimeScale",
            "TYPE": "float",
            "DEFAULT": 0.2,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Palette Cycle Speed"
        },
        {
            "NAME": "lightColor",
            "TYPE": "color",
            "DEFAULT": [1.0, 1.0, 0.9, 1.0],
            "LABEL": "Light Color"
        },
        {
            "NAME": "ambientColor",
            "TYPE": "color",
            "DEFAULT": [0.5, 0.5, 0.6, 1.0],
            "LABEL": "Ambient Color"
        },
        {
            "NAME": "fogColor",
            "TYPE": "color",
            "DEFAULT": [0.2, 0.25, 0.35, 1.0],
            "LABEL": "Fog Color"
        },
        {
            "NAME": "blurAmount",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": 0.0,
            "MAX": 60.0,
            "LABEL": "Radial Blur Amount"
        },
        {
            "NAME": "saturation",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 3.0,
            "LABEL": "Saturation"
        },
        {
            "NAME": "contrast",
            "TYPE": "float",
            "DEFAULT": 1.1,
            "MIN": 0.0,
            "MAX": 3.0,
            "LABEL": "Contrast"
        },
        {
            "NAME": "brightness",
            "TYPE": "float",
            "DEFAULT": 0.1,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Brightness"
        },
        {
            "NAME": "transitionSpeed",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 5.0,
            "LABEL": "Parameter Smoothing"
        }
    ],
    "PASSES": [
        { "TARGET": "timeBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
        { "TARGET": "paramBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
        { "TARGET": "colorControlBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 4, "HEIGHT": 1 },
        { "TARGET": "densityBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
        { "TARGET": "sceneBuffer" },
        { "TARGET": "finalOutput" }
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

// --- Helper Functions ---

mat2 rot(float a) {
    float s = sin(a); float c = cos(a);
    return mat2(c, -s, s, c);
}

vec2 perspective(vec2 uv, float depth, float fov) {
    float z = depth * 2.0 + 0.1;
    return uv / (z * fov + 1.0);
}

vec2 wave_distortion(vec2 uv, float time, float intensity) {
    float wave1 = sin(uv.x * 8.0 + time * 2.0) * sin(uv.y * 6.0 + time * 1.5);
    float wave2 = cos(uv.x * 5.0 - time * 1.8) * cos(uv.y * 7.0 + time * 2.2);
    return uv + vec2(wave1, wave2) * intensity;
}

// --- Object Slicing Engine (Using Icosahedron) ---

// Signed Distance Function for an Icosahedron (by Inigo Quilez)
// Visually complex like a dodecahedron, but more robust for this method.
float sdObject(vec3 p) {
    vec3 q = abs(p);
    float phi = (1.0 + sqrt(5.0)) * 0.5; // Golden ratio
    float a = dot(q, normalize(vec3(phi, 1.0, 0.0)));
    vec3 v1 = vec3(1.0, phi, 0.0);
    float b = dot(q.xyz, normalize(v1).xzy);
    float c = dot(q.yzx, normalize(v1).xzy);
    return max(max(a, b), c) - 1.0;
}

float hash3D(vec3 p) {
    p = fract(p * 0.1031);
    p += dot(p, p.yzx + 33.33);
    return fract((p.x + p.y) * p.z);
}

vec2 sceneSDF_for_slice(vec3 p, float time, float effectiveDensity) {
    float grid_size = 14.0 / (effectiveDensity + 1.0);
    float object_size = 1.8;

    vec3 cell_id = floor(p / grid_size);
    p = mod(p, grid_size) - 0.5 * grid_size;

    float h = hash3D(cell_id);

    float density_threshold = effectiveDensity / 10.0;
    float softness = 0.2 / (effectiveDensity + 1.0);
    float alpha = smoothstep(density_threshold + softness, density_threshold - softness, h);

    if (alpha <= 0.0) {
        return vec2(100.0, 0.0);
    }

    float rot_angle = time * (1.0 + h) + h * 6.28;
    p.xy *= rot(rot_angle);
    p.yz *= rot(rot_angle * 0.7);

    float d = sdObject(p) * object_size;
    return vec2(d, alpha);
}

vec2 object_slice(vec2 uv, float scale, float time, float depth, float effectiveDensity) {
    vec2 p2d = uv * scale;
    float z_slice = sin(time * 0.3 + depth * 2.5) * 2.5;
    vec3 p3d = vec3(p2d.x, p2d.y, z_slice);
    return sceneSDF_for_slice(p3d, time, effectiveDensity);
}

// --- Lighting, Color & Post-FX ---

vec3 calculate_lighting(vec3 base_color, float depth, vec3 normal, float time, vec3 light_col, vec3 ambient_col) {
    vec3 light_pos = vec3(cos(time * 0.3) * 2.0, sin(time * 0.4) * 1.5, 2.0);
    vec3 light_dir = normalize(light_pos - vec3(0.0, 0.0, depth));
    float diffuse = max(dot(normal, light_dir), 0.0);
    float specular = pow(max(dot(reflect(-light_dir, normal), vec3(0.0, 0.0, 1.0)), 0.0), 16.0);
    return base_color * (ambient_col + diffuse * light_col * 0.7) + specular * light_col * 0.3;
}

vec3 apply_atmosphere(vec3 color, float depth, vec3 fog_col) {
    float fog_factor = exp(-depth * 0.4);
    return mix(fog_col, color, fog_factor);
}

vec3 getPaletteColor(float t, vec3 c1, vec3 c2, vec3 c3, vec3 c4) {
    t = fract(t);
    if (t < 0.25) return mix(c1, c2, t / 0.25);
    if (t < 0.5) return mix(c2, c3, (t - 0.25) / 0.25);
    if (t < 0.75) return mix(c3, c4, (t - 0.5) / 0.25);
    return mix(c4, c1, (t - 0.75) / 0.25);
}

// --- Functions for Blur Pass (from second shader) ---
float hash12(vec2 u) {
    vec3 p = fract(u.xyx * .1031);
    p += dot(p, p.yzx + 33.33);
    return fract((p.x + p.y) * p.z);
}
vec3 H(float a) {
    return cos(radians(vec3(0.0, 60.0, 120.0)) + (a)*6.2832) * 0.5 + 0.5;
}
float tanh_compat(float x) {
    float e1 = exp(x);
    float e2 = exp(-x);
    return (e1 - e2) / (e1 + e2);
}


void main() {
    // --- PASSES 0-3: Parameter Smoothing ---
    if (PASSINDEX < 4) {
        float s = min(1.0, TIMEDELTA * transitionSpeed);
        if (PASSINDEX == 0) { // Time, Speeds & Rotation Angle
            vec4 prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
            float newTime, adjustedMovementSpeed, adjustedRotationSpeed, newRotationAngle;
            if (FRAMEINDEX == 0) {
                newTime = 0.1;
                adjustedMovementSpeed = movementSpeed;
                adjustedRotationSpeed = rotationSpeed;
                newRotationAngle = 0.0;
            } else {
                adjustedMovementSpeed = mix(prevTimeData.g, movementSpeed, s);
                adjustedRotationSpeed = mix(prevTimeData.b, rotationSpeed, s);
                newTime = prevTimeData.r + adjustedMovementSpeed * TIMEDELTA;
                newRotationAngle = prevTimeData.a + adjustedRotationSpeed * TIMEDELTA;
            }
            gl_FragColor = vec4(newTime, adjustedMovementSpeed, adjustedRotationSpeed, newRotationAngle);
        }
        if (PASSINDEX == 1) { // General Params
            vec4 prev = IMG_NORM_PIXEL(paramBuffer, vec2(0.5));
            vec4 current;
            if (FRAMEINDEX == 0) { current = vec4(zoom, fov, waveIntensity, depthSeparation); }
            else { current = mix(prev, vec4(zoom, fov, waveIntensity, depthSeparation), s); }
            gl_FragColor = current;
        }
        if (PASSINDEX == 2) { // Colors & PostFX
            float x = isf_FragNormCoord.x; vec4 outColor;
            if (FRAMEINDEX == 0) {
                if (x<0.25) outColor=vec4(color1.rgb,saturation); else if (x<0.5) outColor=vec4(color2.rgb,contrast);
                else if (x<0.75) outColor=vec4(color3.rgb,brightness); else outColor=vec4(color4.rgb,paletteTimeScale);
            } else {
                if (x<0.25) {vec4 p=IMG_NORM_PIXEL(colorControlBuffer,vec2(0.125,0.5)); outColor=vec4(mix(p.rgb,color1.rgb,s),mix(p.a,saturation,s));}
                else if (x<0.5) {vec4 p=IMG_NORM_PIXEL(colorControlBuffer,vec2(0.375,0.5)); outColor=vec4(mix(p.rgb,color2.rgb,s),mix(p.a,contrast,s));}
                else if (x<0.75) {vec4 p=IMG_NORM_PIXEL(colorControlBuffer,vec2(0.625,0.5)); outColor=vec4(mix(p.rgb,color3.rgb,s),mix(p.a,brightness,s));}
                else {vec4 p=IMG_NORM_PIXEL(colorControlBuffer,vec2(0.875,0.5)); outColor=vec4(mix(p.rgb,color4.rgb,s),mix(p.a,paletteTimeScale,s));}
            }
            gl_FragColor = outColor;
        }
        if (PASSINDEX == 3) { // Density
            float prevDensity = (FRAMEINDEX == 0) ? objectDensity : IMG_NORM_PIXEL(densityBuffer, vec2(0.5)).r;
            float newDensity = mix(prevDensity, objectDensity, min(1.0, s * 0.5));
            gl_FragColor = vec4(newDensity, 0.0, 0.0, 1.0);
        }
        return;
    }

    // --- PASS 4: Main Scene Render ---
    if (PASSINDEX == 4) {
        // Fetch smoothed data
        vec4 timeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        float effectiveTime = timeData.r;
        float accumulatedRotation = timeData.a; // Use accumulated angle from buffer

        vec4 paramData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5));
        float effectiveDensity = IMG_NORM_PIXEL(densityBuffer, vec2(0.5)).r;
        vec4 c1_sat = IMG_NORM_PIXEL(colorControlBuffer, vec2(0.125, 0.5));
        vec4 c2_con = IMG_NORM_PIXEL(colorControlBuffer, vec2(0.375, 0.5));
        vec4 c3_bri = IMG_NORM_PIXEL(colorControlBuffer, vec2(0.625, 0.5));
        vec4 c4_pts = IMG_NORM_PIXEL(colorControlBuffer, vec2(0.875, 0.5));
        
        // Unpack parameters
        float effectiveZoom = paramData.r;
        float effectiveFov = paramData.g;
        float effectiveWaveIntensity = paramData.b;
        float effectiveDepthSeparation = paramData.a;
        vec3 pcol1 = c1_sat.rgb; float eff_sat = c1_sat.a;
        vec3 pcol2 = c2_con.rgb; float eff_con = c2_con.a;
        vec3 pcol3 = c3_bri.rgb; float eff_bri = c3_bri.a;
        vec3 pcol4 = c4_pts.rgb; float eff_pts = c4_pts.a;

        vec2 uv = (2.0 * isf_FragNormCoord.xy - 1.0);
        uv.x *= RENDERSIZE.x / RENDERSIZE.y;
        
        // Automated rotation logic
        // Negative sign added to match: Left -> Counter-Clockwise, Right -> Clockwise
        float finalAngleX = -accumulatedRotation;
        float finalAngleY = -accumulatedRotation * 0.7;
        uv *= rot(finalAngleX);

        uv /= effectiveZoom;
        
        vec3 col = vec3(0.0);
        
        const int NUM_LAYERS = 12;
        for (int i = NUM_LAYERS - 1; i >= 0; i--) {
            float layer_index = float(i);
            float depth = layer_index * effectiveDepthSeparation;
            
            vec2 layer_uv = perspective(uv, depth, effectiveFov);
            layer_uv *= rot(effectiveTime * 0.03 * (1.0 + layer_index * 0.15) + layer_index * 0.5 + finalAngleY);
            layer_uv = wave_distortion(layer_uv, effectiveTime, effectiveWaveIntensity / (depth + 1.0));
            
            float scale = 4.0 + depth * 2.0;
            vec2 res = object_slice(layer_uv, scale, effectiveTime, depth, effectiveDensity);
            float d = res.x;
            float object_alpha = res.y;
            
            float aa_width = 0.02 / (depth + 1.0);
            float mask = smoothstep(aa_width, -aa_width, d);
            
            if (mask * object_alpha > 0.01) {
                vec3 base_color = getPaletteColor(effectiveTime * c4_pts.a + layer_index * 0.1, pcol1, pcol2, pcol3, pcol4);
                
                vec2 eps = vec2(0.01, 0.0);
                float d_dx = object_slice(layer_uv + eps.xy, scale, effectiveTime, depth, effectiveDensity).x - d;
                float d_dy = object_slice(layer_uv + eps.yx, scale, effectiveTime, depth, effectiveDensity).x - d;
                vec3 normal = normalize(vec3(-d_dx, -d_dy, 0.1));
                
                vec3 lit_color = calculate_lighting(base_color, depth, normal, effectiveTime, lightColor.rgb, ambientColor.rgb);
                
                col = mix(col, lit_color, mask * object_alpha);
            }
        }
        
        col = apply_atmosphere(col, length(uv * effectiveZoom * 0.1), fogColor.rgb);
        
        // Post-processing
        vec3 gray = vec3(dot(col, vec3(0.299, 0.587, 0.114)));
        col = mix(gray, col, eff_sat);
        col = (col - 0.5) * eff_con + 0.5;
        col += eff_bri;
        
        vec2 vignette_uv = (isf_FragNormCoord.xy - 0.5) * 2.0;
        vignette_uv.x *= RENDERSIZE.x / RENDERSIZE.y;
        float vignette = 1.0 - smoothstep(0.4, 1.5, length(vignette_uv));
        col *= vignette * 0.5 + 0.5;

        gl_FragColor = vec4(clamp(col, 0.0, 1.0), 1.0);
        return;
    }

    // --- PASS 5: Radial Blur ---
    if (PASSINDEX == 5) {
        if (blurAmount <= 0.0) {
            gl_FragColor = IMG_NORM_PIXEL(sceneBuffer, isf_FragNormCoord);
            return;
        }

        vec2 u = isf_FragNormCoord;
        vec2 m = vec2(0.5);
        vec3 col = IMG_NORM_PIXEL(sceneBuffer, u).rgb * 0.7;

        float l = blurAmount;
        float j = hash12(gl_FragCoord.xy + TIME);
        float v = 0.0;

        for (float i = 0.0; i < 60.0; i += 1.0) {
            if (i >= l) break;
            float d = 1.0 - i / l;
            float blend = (v + j) / l;
            vec2 offset = mix(u, m, blend);
            vec3 blurOut = IMG_NORM_PIXEL(sceneBuffer, offset).rgb;
            col += blurOut * H(d) * 0.2;
            v += 1.0;
        }

        vec3 processed;
        processed.r = tanh_compat(col.r * col.r);
        processed.g = tanh_compat(col.g * col.g);
        processed.b = tanh_compat(col.b * col.b);

        gl_FragColor = vec4(processed, 1.0);
    }
}