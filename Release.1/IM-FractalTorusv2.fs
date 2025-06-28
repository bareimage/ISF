/*{
    "CATEGORIES": [
        "Generator",
        "Animation"
    ],
    "CREDIT": "Converted from isf.video by @dot2dot with added controls",
    "DESCRIPTION": "Fractal Torus with Neon Colors and Rotation Controls",
    "INPUTS": [
        {
            "DEFAULT": [
                1,
                0,
                0,
                1
            ],
            "NAME": "color1",
            "TYPE": "color"
        },
        {
            "DEFAULT": [
                0,
                1,
                0,
                1
            ],
            "NAME": "color2",
            "TYPE": "color"
        },
        {
            "DEFAULT": 0.5,
            "MAX": 3,
            "MIN": -3,
            "NAME": "rotateSpeedX",
            "TYPE": "float"
        },
        {
            "DEFAULT": 0.3,
            "MAX": 3,
            "MIN": -3,
            "NAME": "rotateSpeedY",
            "TYPE": "float"
        },
        {
            "DEFAULT": 0.4,
            "MAX": 3,
            "MIN": -3,
            "NAME": "rotateSpeedZ",
            "TYPE": "float"
        },
        {
            "DEFAULT": 1,
            "LABEL": "Animation Speed",
            "MAX": 10,
            "MIN": 0,
            "NAME": "speedControl",
            "TYPE": "float"
        }
    ],
    "ISFVSN": "2",
    "PASSES": [
        {
            "FLOAT": true,
            "HEIGHT": 1,
            "PERSISTENT": true,
            "TARGET": "stateBuffer",
            "WIDTH": 4
        },
        {
            "TARGET": "finalOutput"
        }
    ]
}
*/

#define PI 3.14159265359

mat2 rot(float a) {
    float s = sin(a), c = cos(a);
    return mat2(c, s, -s, c);
}

float sdTorus(vec3 p, vec2 t) {
    vec2 q = vec2(length(p.xz) - t.x, p.y);
    return length(q) - t.y;
}

float de(vec3 p, float effectiveTime, float effectiveRotX, float effectiveRotY, float effectiveRotZ) {
    float t = -sdTorus(p, vec2(2.3, 2.));
    p.y += 1.;
    float d = 100., s = 2.;
    p *= 0.5;
    
    // Modified rotation with XYZ controls using effective rotation values
    for(int i = 0; i < 6; i++) {
        p.xz *= rot(effectiveRotX);
        p.xy *= rot(effectiveRotY);
        p.yz *= rot(effectiveRotZ);
        p.xz = abs(p.xz);
        float inv = 1.0 / clamp(dot(p, p), 0.0, 1.0);
        p = p * inv - 1.0;
        s *= inv;
        d = min(d, length(p.xz) + fract(p.y * 0.05 + effectiveTime * 0.2) - 0.1);
    }
    return min(d / s, t);
}

float march(vec3 from, vec3 dir, float effectiveTime, float effectiveRotX, float effectiveRotY, float effectiveRotZ) {
    float td = 0.0, g = 0.0;
    vec3 p;
    for(int i = 0; i < 80; i++) { // Reduced from 100 to 80 iterations for better performance
        p = from + dir * td;
        float d = de(p, effectiveTime, effectiveRotX, effectiveRotY, effectiveRotZ);
        if(d < 0.002) break;
        g++;
        td += d;
    }
    float glow = exp(-0.07 * td * td) * sin(p.y * 10.0 + effectiveTime * 10.0);
    float pattern = smoothstep(0.3, 0.0, abs(0.5 - fract(p.y * 15.0)));
    return mix(pattern * glow, g * g * 0.00008, 0.3);
}

void main() {
    vec4 fragColor = vec4(0.0);
    
    if (PASSINDEX == 0) {
        // First pass: Update all state variables in one buffer
        vec4 prevData = IMG_NORM_PIXEL(stateBuffer, vec2(0.125, 0.5)); // Time at position 0
        vec4 prevRotX = IMG_NORM_PIXEL(stateBuffer, vec2(0.375, 0.5)); // RotX at position 1
        vec4 prevRotY = IMG_NORM_PIXEL(stateBuffer, vec2(0.625, 0.5)); // RotY at position 2
        vec4 prevRotZ = IMG_NORM_PIXEL(stateBuffer, vec2(0.875, 0.5)); // RotZ at position 3
        
        // Extract previous accumulated values
        float accumulatedTime = prevData.r;
        float accumulatedRotX = prevRotX.r;
        float accumulatedRotY = prevRotY.r;
        float accumulatedRotZ = prevRotZ.r;
        
        // Calculate new accumulated values
        float newTime, newRotX, newRotY, newRotZ;
        
        if (FRAMEINDEX == 0) {
            // Initialize on first frame
            newTime = 0.0;
            newRotX = 0.0;
            newRotY = 0.0;
            newRotZ = 0.0;
        } else {
            // Accumulate based on speed and frame delta
            newTime = accumulatedTime + speedControl * TIMEDELTA;
            newRotX = accumulatedRotX + rotateSpeedX * TIMEDELTA * speedControl;
            newRotY = accumulatedRotY + rotateSpeedY * TIMEDELTA * speedControl;
            newRotZ = accumulatedRotZ + rotateSpeedZ * TIMEDELTA * speedControl;
        }
        
        // Store the accumulated value based on the x position
        if (gl_FragCoord.x < 1.0) {
            fragColor = vec4(newTime, 0.0, 0.0, 1.0); // Time at position 0
        } else if (gl_FragCoord.x < 2.0) {
            fragColor = vec4(newRotX, 0.0, 0.0, 1.0); // RotX at position 1
        } else if (gl_FragCoord.x < 3.0) {
            fragColor = vec4(newRotY, 0.0, 0.0, 1.0); // RotY at position 2
        } else {
            fragColor = vec4(newRotZ, 0.0, 0.0, 1.0); // RotZ at position 3
        }
    }
    else if (PASSINDEX == 1) {
        // Final pass: Render the fractal using the accumulated values
        vec4 timeData = IMG_NORM_PIXEL(stateBuffer, vec2(0.125, 0.5));
        vec4 rotXData = IMG_NORM_PIXEL(stateBuffer, vec2(0.375, 0.5));
        vec4 rotYData = IMG_NORM_PIXEL(stateBuffer, vec2(0.625, 0.5));
        vec4 rotZData = IMG_NORM_PIXEL(stateBuffer, vec2(0.875, 0.5));
        
        // Get the accumulated values
        float effectiveTime = timeData.r;
        float effectiveRotX = rotXData.r;
        float effectiveRotY = rotYData.r;
        float effectiveRotZ = rotZData.r;

        vec2 uv = (gl_FragCoord.xy / RENDERSIZE.xy) - 0.5;
        uv.x *= RENDERSIZE.x / RENDERSIZE.y;
        
        float t = effectiveTime * 0.5;
        vec3 from = vec3(cos(t), 0.0, -3.3);
        vec3 dir = normalize(vec3(uv, 0.7));
        
        // Apply rotation controls to camera using effectiveRotX
        dir.xy *= rot(0.5 * sin(effectiveRotX));
        
        float intensity = march(from, dir, effectiveTime, effectiveRotX, effectiveRotY, effectiveRotZ);
        
        // Color blending with neon effect
        vec3 finalColor = mix(
            color1.rgb * intensity * 2.0,
            color2.rgb * (1.0 - intensity) * 1.5,
            smoothstep(0.2, 0.8, intensity)
        );
        
        // Add glow based on color2
        finalColor += color2.rgb * 0.5 * pow(intensity, 3.0);
        
        fragColor = vec4(finalColor, 1.0);
    }
    
    // Output the final color
    gl_FragColor = fragColor;
}
