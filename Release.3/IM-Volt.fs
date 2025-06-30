/*{
  "DESCRIPTION": "ISF implementation of the 'Volt' algorithm. Features perspective-like projection with Y-symmetry fix for spherical appearance, time accumulation, X, Y, Z rotation controls, zoom, internal swirling, and a vignette effect.",
  "CREDIT": "Volt Algorithm: By @XorDev, ISF Version by @dot2dot (bareimage)",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR", "ABSTRACT", "PATTERNS"],
  "INPUTS": [
    { "NAME": "speed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0, "LABEL": "Animation Speed" },
    { "NAME": "rotationX", "TYPE": "float", "DEFAULT": 0.0, "MIN": -3.14159, "MAX": 3.14159, "LABEL": "Rotation X" },
    { "NAME": "rotationY", "TYPE": "float", "DEFAULT": 0.0, "MIN": -3.14159, "MAX": 3.14159, "LABEL": "Rotation Y" },
    { "NAME": "rotationZ", "TYPE": "float", "DEFAULT": 0.0, "MIN": -3.14159, "MAX": 3.14159, "LABEL": "Rotation Z" },
    { "NAME": "zoom", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 5.0, "LABEL": "Zoom (FOV)" },
    { "NAME": "transitionSpeed", "TYPE": "float", "DEFAULT": 2.0, "MIN": 0.1, "MAX": 10.0, "LABEL": "Parameter Transition Smoothness" }
  ],
  "PASSES": [
    { "TARGET": "timeBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "rotXBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "rotYBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "rotZBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "zoomBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "finalOutput" }
  ]
}*/

precision highp float;
const float pi = 3.14159265359;

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

// Helper function to create an X-axis rotation matrix
mat3 rotationXMatrix(float angle) {
    float s = sin(angle);
    float c = cos(angle);
    return mat3(
        1.0, 0.0, 0.0,
        0.0, c,   -s,
        0.0, s,   c
    );
}

// Helper function to create a Y-axis rotation matrix
mat3 rotationYMatrix(float angle) {
    float s = sin(angle);
    float c = cos(angle);
    return mat3(
        c,   0.0, s,
        0.0, 1.0, 0.0,
        -s,  0.0, c
    );
}

// Helper function to create a Z-axis rotation matrix
mat3 rotationZMatrix(float angle) {
    float s = sin(angle);
    float c = cos(angle);
    return mat3(
        c,   -s,  0.0,
        s,   c,   0.0,
        0.0, 0.0, 1.0
    );
}

void main() {
    // Pass 0: Time and main speed smoothing
    if(PASSINDEX == 0) {
        vec4 prevData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        float smoothedSpeed = (FRAMEINDEX == 0) ? speed : mix(prevData.g, speed, min(1.0, TIMEDELTA * transitionSpeed));
        float accumulatedTime = (FRAMEINDEX == 0) ? 0.0 : prevData.r + smoothedSpeed * TIMEDELTA;
        gl_FragColor = vec4(accumulatedTime, smoothedSpeed, 0.0, 1.0);
        return;
    }

    // Pass 1: Rotation X smoothing
    if(PASSINDEX == 1) {
        vec4 prevData = IMG_NORM_PIXEL(rotXBuffer, vec2(0.5));
        float smoothedRotX = (FRAMEINDEX == 0) ? rotationX : mix(prevData.r, rotationX, min(1.0, TIMEDELTA * transitionSpeed));
        gl_FragColor = vec4(smoothedRotX, 0.0, 0.0, 1.0);
        return;
    }

    // Pass 2: Rotation Y smoothing
    if(PASSINDEX == 2) {
        vec4 prevData = IMG_NORM_PIXEL(rotYBuffer, vec2(0.5));
        float smoothedRotY = (FRAMEINDEX == 0) ? rotationY : mix(prevData.r, rotationY, min(1.0, TIMEDELTA * transitionSpeed));
        gl_FragColor = vec4(smoothedRotY, 0.0, 0.0, 1.0);
        return;
    }

    // Pass 3: Rotation Z smoothing
    if(PASSINDEX == 3) {
        vec4 prevData = IMG_NORM_PIXEL(rotZBuffer, vec2(0.5));
        float smoothedRotZ = (FRAMEINDEX == 0) ? rotationZ : mix(prevData.r, rotationZ, min(1.0, TIMEDELTA * transitionSpeed));
        gl_FragColor = vec4(smoothedRotZ, 0.0, 0.0, 1.0);
        return;
    }

    // Pass 4: Zoom smoothing
    if(PASSINDEX == 4) {
        vec4 prevData = IMG_NORM_PIXEL(zoomBuffer, vec2(0.5));
        float smoothedZoom = (FRAMEINDEX == 0) ? zoom : mix(prevData.r, zoom, min(1.0, TIMEDELTA * transitionSpeed));
        gl_FragColor = vec4(smoothedZoom, 0.0, 0.0, 1.0);
        return;
    }

    // Pass 5: Render "Volt" Algorithm to finalOutput
    if(PASSINDEX == 5) {
        vec4 timeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        float t = timeData.r;

        vec4 rotXData = IMG_NORM_PIXEL(rotXBuffer, vec2(0.5));
        float currentRotationX = rotXData.r;

        vec4 rotYData = IMG_NORM_PIXEL(rotYBuffer, vec2(0.5));
        float currentRotationY = rotYData.r;

        vec4 rotZData = IMG_NORM_PIXEL(rotZBuffer, vec2(0.5));
        float currentRotationZ = rotZData.r;

        vec4 zoomData = IMG_NORM_PIXEL(zoomBuffer, vec2(0.5));
        float currentZoom = max(0.01, zoomData.r); // Ensure zoom is not zero

        vec4 o = vec4(0.0);
        float i_loop_volt = 0.0;
        float d_volt = 0.0;
        float z_volt = 0.0;

        vec2 uv_centered = isf_FragNormCoord.xy * 2.0 - 1.0;
        uv_centered.x *= RENDERSIZE.x / RENDERSIZE.y;

        vec3 base_ray_dir = normalize(vec3(uv_centered.xy, currentZoom));

        mat3 rotXMat = rotationXMatrix(currentRotationX);
        mat3 rotYMat = rotationYMatrix(currentRotationY);
        mat3 rotZMat = rotationZMatrix(currentRotationZ);
        mat3 finalRotationMatrix = rotZMat * rotYMat * rotXMat;
        
        vec3 actual_initial_ray_dir = finalRotationMatrix * base_ray_dir;

        for(i_loop_volt = 0.0; i_loop_volt < 90.0; i_loop_volt++) {
            vec3 p_volt = z_volt * actual_initial_ray_dir;
            
            float internalRotAngle = cos(0.1 * z_volt + t * 0.2);
            mat2 internalRotMat = mat2(cos(internalRotAngle), -sin(internalRotAngle), sin(internalRotAngle), cos(internalRotAngle));
            p_volt.xy = internalRotMat * p_volt.xy;
            
            for(float d_inner_volt = 0.8; d_inner_volt < 30.0; d_inner_volt += d_inner_volt) {
                 p_volt += 0.5 * sin(p_volt * d_inner_volt + z_volt).yzx / d_inner_volt;
            }

            // MODIFIED LINE: Use abs(p_volt.y) for symmetry
            d_volt = 0.3 * length(vec3(cos(p_volt.xz) + 1.0, sin(p_volt.x * 0.8 - abs(p_volt.y) / 1000.0 - p_volt.z * 0.7 + t)));
            z_volt += d_volt;

            if (abs(d_volt) > 0.0001) {
                 o += (cos(sin(z_volt * 0.3) + vec4(1.0, 3.0, 4.0, 1.0)) + 1.4) / d_volt;
            }
            if(z_volt > 100.0 || d_volt < 0.001) break;
        }

        o = tanh(o / 3000.0);
        vec3 finalVoltColor = o.rgb;

        vec2 uv_vignette = isf_FragNormCoord.xy;
        float vignette = 1.0 - smoothstep(0.5, 1.5, length(uv_vignette - 0.5) * 1.5);
        vec3 resWithVignette = finalVoltColor.rgb * vignette;

        gl_FragColor = vec4(resWithVignette, 1.0);
        return;
    }
}
