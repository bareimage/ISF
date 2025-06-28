/*{
  "DESCRIPTION": "Truchet Pattern Generator with smooth transitions",
  "CREDIT": "Converted to ISF 2.0 with enhancements by dot2dot, original by @liu7d7 - Shadertoy",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR"],
  "INPUTS": [
    {
      "NAME": "speed",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 5.0,
      "LABEL": "Animation Speed"
    },
    {
      "NAME": "rotationSpeed",
      "TYPE": "float",
      "DEFAULT": 0.125,
      "MIN": -0.5,
      "MAX": 0.5,
      "LABEL": "Rotation Speed"
    },
    {
      "NAME": "scale",
      "TYPE": "float",
      "DEFAULT": 900.0,
      "MIN": 100.0,
      "MAX": 2000.0,
      "LABEL": "Pattern Scale"
    },
    {
      "NAME": "colorShift",
      "TYPE": "float",
      "DEFAULT": 0.5,
      "MIN": 0.1,
      "MAX": 2.0,
      "LABEL": "Color Shift Speed"
    },
    {
      "NAME": "colorA",
      "TYPE": "color",
      "DEFAULT": [0.5, 0.5, 0.5, 1.0],
      "LABEL": "Base Color A"
    },
    {
      "NAME": "colorB",
      "TYPE": "color",
      "DEFAULT": [0.5, 0.5, 0.5, 1.0],
      "LABEL": "Base Color B"
    },
    {
      "NAME": "colorC",
      "TYPE": "color",
      "DEFAULT": [0.8, 0.8, 0.5, 1.0],
      "LABEL": "Color Frequency"
    },
    {
      "NAME": "colorD",
      "TYPE": "color",
      "DEFAULT": [0.0, 0.2, 0.5, 1.0],
      "LABEL": "Color Phase"
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
      "TARGET": "finalOutput"
    }
  ]
}*/

#define PI 3.1415926
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

uint hash(uvec2 src) {
    const uint M = 0x5bd1e995u;
    uint h = 1190494759u;
    src *= M; src ^= src>>24u; src *= M;
    h *= M; h ^= src.x; h *= M; h ^= src.y;
    h ^= h>>13u; h *= M; h ^= h>>15u;
    return h;
}

float triangle(float t, float p, float a) {
    float m = mod(t, p), hp = p * 0.5;
    float s = step(hp, m);
    return (m * (1.0 - s) + (p - m) * s) / hp * a;
}

vec3 colorFunc(float t, vec3 a, vec3 b, vec3 c, vec3 d) {
    return a + b * cos(2.0 * PI * (c * t + d));
}

#define rot(t) mat2(cos(t), sin(t), -sin(t), cos(t))

vec4 truchet(vec2 p, float r, float w, float time, vec3 a, vec3 b, vec3 c, vec3 d, float colorShiftSpeed) {
    float hr = r / 2.0;
    float hw = w / 2.0;

    vec2 b_pos = floor(p / r) * r;
    vec2 c_pos = b_pos + vec2(hr);
    float h = float(hash(uvec2(abs(c_pos * 100.0))) % 4u);
    float i = 3.0 - h;
    
    vec2 a0 = b_pos + vec2(mod(h, 2.0) * r, floor(h / 2.0) * r);
    vec2 a1 = b_pos + vec2(mod(i, 2.0) * r, floor(i / 2.0) * r);
      
    float d1 = distance(p, a0);
    float d2 = distance(p, a1);
    
    float dist = min(d1, d2);
    
    vec3 colorVal = colorFunc(triangle(time * colorShiftSpeed, 3.0, 1.0), a, b, c, d);
    float alpha = (1.0 - (abs(dist - hr) - hw) / w) * 0.5;
    
    return vec4(1.0) * (1.0 - smoothstep(hw, hw + 1.414, abs(dist - hr))) + 
           vec4(colorVal, alpha) * (smoothstep(hw, hw + 1.414, abs(dist - hr)));
}

void main() {
    // Declare variables at the top level for use in all passes
    vec4 prevTimeData, prevParamData;
    float accumulatedTime;
    float currentRotSpeed, adjustedRotSpeed;
    float currentScale, adjustedScale;
    float currentColorShift, adjustedColorShift;
    
    if (PASSINDEX == 0) {
        // First pass: Update the accumulated time in the persistent buffer
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        
        // Extract previous accumulated time
        accumulatedTime = prevTimeData.r;
        
        // Calculate new accumulated time
        if (FRAMEINDEX == 0) {
            // Initialize time on first frame
            accumulatedTime = 0.0;
        } else {
            // Update time based on speed parameter
            accumulatedTime += TIMEDELTA * speed;
        }
        
        // Store the accumulated time
        gl_FragColor = vec4(accumulatedTime, 0.0, 0.0, 1.0);
    }
    else if (PASSINDEX == 1) {
        // Second pass: Update parameters with smooth transitions
        prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5, 0.5));
        
        if (FRAMEINDEX == 0) {
            // Initialize on first frame
            adjustedRotSpeed = rotationSpeed;
            adjustedScale = scale;
            adjustedColorShift = colorShift;
        } else {
            // Smoothly transition parameters
            currentRotSpeed = prevParamData.r;
            currentScale = prevParamData.g;
            currentColorShift = prevParamData.b;
            
            // Calculate the smoothed values
            adjustedRotSpeed = mix(currentRotSpeed, rotationSpeed, min(1.0, TIMEDELTA * transitionSpeed));
            adjustedScale = mix(currentScale, scale, min(1.0, TIMEDELTA * transitionSpeed));
            adjustedColorShift = mix(currentColorShift, colorShift, min(1.0, TIMEDELTA * transitionSpeed));
        }
        
        // Store the adjusted parameters
        gl_FragColor = vec4(adjustedRotSpeed, adjustedScale, adjustedColorShift, 1.0);
    }
    else {
        // Final pass: Render the shader using the accumulated values
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5, 0.5));
        
        // Get the accumulated values
        float effectiveTime = prevTimeData.r;
        float effectiveRotSpeed = prevParamData.r;
        float effectiveScale = prevParamData.g;
        float effectiveColorShift = prevParamData.b;
        
        // Extract color components
        vec3 a_color = colorA.rgb;
        vec3 b_color = colorB.rgb;
        vec3 c_color = colorC.rgb;
        vec3 d_color = colorD.rgb;
        
        // Main shader logic
        vec2 uv = (2.0 * gl_FragCoord.xy - RENDERSIZE.xy) / RENDERSIZE.y;
        uv *= effectiveScale / 2.0;
        
        uv = rot(effectiveTime * effectiveRotSpeed) * uv;
        
        vec3 final = vec3(0.0);
        for (float i = 3.0; i > -0.1; i--) {
            float t = mod(effectiveTime, 2.0) * 100.0;
            vec4 truc = truchet(
                (uv + vec2(sin(effectiveTime * 1.3) * (400.0 - 400.0/6.0 * i), 
                          cos(effectiveTime * 0.7) * (400.0 - 400.0/6.0 * i))), 
                80.0 - i * 20.0,  
                10.0 - i * 2.0,
                effectiveTime,
                a_color, b_color, c_color, d_color,
                effectiveColorShift
            ) * (4.0 - i * 0.75) * 0.25;
            
            final = final * (1.0 - truc.a) + clamp(truc.rgb, 0.0, 1.0) * truc.a;
        }
        
        gl_FragColor = vec4(final, 1.0);
    }
}
