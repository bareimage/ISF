/*{
  "DESCRIPTION": "Cosmic shader with true circular discs, full 3D rotation, and smooth parameter transitions",
  "CREDIT": "Original by @XorDev, converted to ISF 2.0 with enhancements by dot2dot",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR"],
  "INPUTS": [
    {
      "NAME": "speed",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 10.0,
      "LABEL": "Animation Speed"
    },
    {
      "NAME": "intensity",
      "TYPE": "float",
      "DEFAULT": 0.2,
      "MIN": 0.01,
      "MAX": 1.0,
      "LABEL": "Glow Intensity"
    },
    {
      "NAME": "iterations",
      "TYPE": "float",
      "DEFAULT": 30.0,
      "MIN": 5.0,
      "MAX": 60.0,
      "LABEL": "Detail Level"
    },
    {
      "NAME": "discSize",
      "TYPE": "float",
      "DEFAULT": 2.0,
      "MIN": 0.1,
      "MAX": 10.0,
      "LABEL": "Disc Size (Diameter)"
    },
    {
      "NAME": "rotationX",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": -1.57079632679,
      "MAX": 1.57079632679,
      "LABEL": "X Rotation"
    },
    {
      "NAME": "rotationY",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": -1.57079632679,
      "MAX": 1.57079632679,
      "LABEL": "Y Rotation"
    },
    {
      "NAME": "rotationZ",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": -1.57079632679,
      "MAX": 1.57079632679,
      "LABEL": "Z Rotation"
    },
    {
      "NAME": "colorShift",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 5.0,
      "LABEL": "Color Shift"
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
      "TARGET": "paramBuffer",
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

// Rotation matrices
mat3 rotateX(float angle) {
    float s = sin(angle);
    float c = cos(angle);
    return mat3(
        1.0, 0.0, 0.0,
        0.0, c, -s,
        0.0, s, c
    );
}

mat3 rotateY(float angle) {
    float s = sin(angle);
    float c = cos(angle);
    return mat3(
        c, 0.0, s,
        0.0, 1.0, 0.0,
        -s, 0.0, c
    );
}

mat3 rotateZ(float angle) {
    float s = sin(angle);
    float c = cos(angle);
    return mat3(
        c, -s, 0.0,
        s, c, 0.0,
        0.0, 0.0, 1.0
    );
}

void main() {
    vec4 prevTimeData, prevParamData, prevRotData;
    float accumulatedTime, currentSpeed, adjustedSpeed, newTime;
    float currentIntensity, currentIterations, currentColorShift, currentDiscSize;
    vec3 currentRotation;

    if (PASSINDEX == 0) {
        // Time accumulation
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        accumulatedTime = prevTimeData.r;
        currentSpeed = prevTimeData.g;

        if (FRAMEINDEX == 0) {
            newTime = 0.0;
            adjustedSpeed = speed;
        } else {
            adjustedSpeed = mix(currentSpeed, speed, min(1.0, TIMEDELTA * transitionSpeed));
            newTime = accumulatedTime + adjustedSpeed * TIMEDELTA;
        }
        gl_FragColor = vec4(newTime, adjustedSpeed, 0.0, 1.0);
    }
    else if (PASSINDEX == 1) {
        // Parameters smoothing
        prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5, 0.5));
        if (FRAMEINDEX == 0) {
            currentIntensity = intensity;
            currentIterations = iterations;
            currentColorShift = colorShift;
            currentDiscSize = discSize;
        } else {
            currentIntensity = mix(prevParamData.r, intensity, min(1.0, TIMEDELTA * transitionSpeed));
            currentIterations = mix(prevParamData.g, iterations, min(1.0, TIMEDELTA * transitionSpeed));
            currentColorShift = mix(prevParamData.b, colorShift, min(1.0, TIMEDELTA * transitionSpeed));
            currentDiscSize = mix(prevParamData.a, discSize, min(1.0, TIMEDELTA * transitionSpeed));
        }
        gl_FragColor = vec4(currentIntensity, currentIterations, currentColorShift, currentDiscSize);
    }
    else if (PASSINDEX == 2) {
        // Rotation smoothing
        prevRotData = IMG_NORM_PIXEL(rotationBuffer, vec2(0.5, 0.5));
        if (FRAMEINDEX == 0) {
            currentRotation = vec3(rotationX, rotationY, rotationZ);
        } else {
            currentRotation.x = mix(prevRotData.r, rotationX, min(1.0, TIMEDELTA * transitionSpeed));
            currentRotation.y = mix(prevRotData.g, rotationY, min(1.0, TIMEDELTA * transitionSpeed));
            currentRotation.z = mix(prevRotData.b, rotationZ, min(1.0, TIMEDELTA * transitionSpeed));
        }
        gl_FragColor = vec4(currentRotation, 1.0);
    }
    else {
        // Final rendering
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5, 0.5));
        prevRotData = IMG_NORM_PIXEL(rotationBuffer, vec2(0.5, 0.5));

        float effectiveTime = prevTimeData.r;
        float effectiveIntensity = prevParamData.r;
        float effectiveIterations = prevParamData.g;
        float effectiveColorShift = prevParamData.b;
        float effectiveDiscSize = prevParamData.a;
        vec3 effectiveRotation = vec3(prevRotData.rgb);

        vec2 r = RENDERSIZE.xy;
        float aspectRatio = r.x / r.y;

        // Normalized device coordinates
        vec2 uv = (gl_FragCoord.xy / r - 0.5) * 2.0;
        uv.x *= aspectRatio;

        // Create a ray from camera through pixel
        vec3 rayDir = normalize(vec3(uv, -1.0));

        // Rotation matrix
        mat3 rotMat = rotateX(effectiveRotation.x) * rotateY(effectiveRotation.y) * rotateZ(effectiveRotation.z);

        // Rotate the ray direction
        vec3 dir = rotMat * rayDir;

        // Intersect with plane z=0 in local space
        float t = -dir.z / 1.0; // plane normal (0,0,1), plane at z=0
        vec3 hitPoint = t * dir;

        // Convert to disc coordinates
        vec2 discUV = hitPoint.xy / effectiveDiscSize;

        // Maintain circular shape
        // No aspect correction here; aspect handled in uv
        // Loop for the effect
        float i = 0.0;
        float a;
        vec4 O = vec4(0.0);
        for (; i < effectiveIterations; i++) {
            vec2 I = discUV;
            O += effectiveIntensity / (abs(length(I) * 8e1 - i) + 4e1 / r.y) *
                 clamp(cos(a = atan(I.y, I.x) * ceil(i * 0.1) + effectiveTime * sin(i * i) + i * i), 0.0, 0.6) *
                 (cos(a - i * effectiveColorShift + vec4(0,1,2,0)) + 1.0);
        }
        gl_FragColor = O;
    }
}
