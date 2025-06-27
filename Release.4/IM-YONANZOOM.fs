/*{
  "DESCRIPTION": "Hypnotic fractal tunnel with logarithmic polar coordinates and temporal feedback. Includes smoothed speed transitions and color control.",
  "CREDIT": "ISF translation of twigl shader by @zozuar (https://x.com/zozuar), converted by @dot2dot. Speed smoothing and color control added.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR"],
  "INPUTS": [
    {
      "NAME": "timeSpeed",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": -3.0,
      "MAX": 3.0,
      "LABEL": "Time Speed"
    },
    {
        "NAME": "transitionSpeed",
        "TYPE": "float",
        "DEFAULT": 2.0,
        "MIN": 0.1,
        "MAX": 10.0,
        "LABEL": "Transition Smoothness"
    },
    {
        "NAME": "baseColor",
        "TYPE": "color",
        "DEFAULT": [1.0, 0.4, 0.2, 1.0],
        "LABEL": "Base Color"
    },
    {
      "NAME": "rotationAmount",
      "TYPE": "float",
      "DEFAULT": 0.7,
      "MIN": 0.0,
      "MAX": 2.0,
      "LABEL": "Rotation Amount"
    },
    {
      "NAME": "scrollSpeed",
      "TYPE": "float",
      "DEFAULT": 3.0,
      "MIN": 0.1,
      "MAX": 10.0,
      "LABEL": "Scroll Speed"  
    },
    {
      "NAME": "feedbackAmount",
      "TYPE": "float",
      "DEFAULT": 0.85,
      "MIN": 0.0,
      "MAX": 0.98,
      "LABEL": "Feedback Amount"
    },
    {
      "NAME": "feedbackDecay",
      "TYPE": "float",
      "DEFAULT": 0.97,
      "MIN": 0.8,
      "MAX": 1.0,
      "LABEL": "Feedback Decay"
    },
    {
      "NAME": "brightness",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.1,
      "MAX": 3.0,
      "LABEL": "Brightness"
    },
    {
      "NAME": "centerOffset",
      "TYPE": "point2D",
      "DEFAULT": [0.5, 0.5],
      "MIN": [0.0, 0.0],
      "MAX": [1.0, 1.0],
      "LABEL": "Center Position"
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
      "TARGET": "feedbackBuffer",
      "PERSISTENT": true,
      "FLOAT": true
    },
    {}
  ]
}*/

#define PI 3.14159265359

//I am not sure regarding the license intention of the original artist. 
//To be on the safe side I assume that it is released under CC lisence
//I am waiting for the responce from the original author

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

// 2D rotation matrix
mat2 rotate2D(float angle){
    float s = sin(angle);
    float c = cos(angle);
    return mat2(c, -s, s, c);
}

void main() {
    if (PASSINDEX == 0) {
        // PASS 0: Update the accumulated time in the persistent buffer
        vec4 prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        
        // Extract previous accumulated time and speed
        float accumulatedTime = prevTimeData.r;
        float currentSpeed = prevTimeData.g;
        
        float newTime;
        float adjustedSpeed;
        
        // Calculate new accumulated time
        if (FRAMEINDEX == 0) {
            // Initialize time on first frame
            newTime = 0.0;
            adjustedSpeed = timeSpeed;
        } else {
            // Smoothly transition to target speed
            adjustedSpeed = mix(currentSpeed, timeSpeed, min(1.0, TIMEDELTA * transitionSpeed));
            newTime = accumulatedTime + adjustedSpeed * TIMEDELTA;
        }
        
        // Store the new accumulated time and current speed
        gl_FragColor = vec4(newTime, adjustedSpeed, 0.0, 1.0);

    } else if (PASSINDEX == 1) {
        // PASS 1: Render the fractal with feedback accumulation
        
        // Read the smoothed, accumulated time from our buffer
        vec4 prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        float effectiveTime = prevTimeData.r;

        // Get fragment coordinates in pixel space
        vec2 fragCoord = gl_FragCoord.xy;
        vec2 resolution = RENDERSIZE;
        
        // Time is now the effectiveTime from our buffer
        float t = effectiveTime;
        
        // Initialize working variables
        float y = 0.0;
        float i = 0.0;
        float e = 0.0;
        float R = 0.0;
        float a = 0.0;
        
        // Initialize ray position
        vec3 q = vec3(0.0);
        vec3 p = vec3(0.0);
        
        // Main raymarching loop
        for(i = 0.0; i < 100.0; i += 1.0) {
            vec3 coordVec = vec3(fragCoord.y, fragCoord.x, fragCoord.x);
            coordVec.xy -= resolution * centerOffset;
            coordVec /= resolution.y;
            vec3 rayStep = (0.6 - coordVec) * max(-y, R) / 4.0;
            
            q += rayStep;
            p = q;
            
            p.y -= 1.0;
            p.yx *= rotate2D(cos(t) * rotationAmount);
            
            y = p.y;
            e = atan(p.x, p.z) + t;
            
            R = length(p);
            if (R < 0.0001) R = 0.0001;
            
            a = y / R;
            p = vec3(
                log(R) - t / PI * scrollSpeed + e / PI,
                a * sin(5.0 * e),
                a
            ) + 0.5;
            
            for(int j = 0; j < 12; j++) {
                p -= round(p);
                a = dot(p, p) + 0.26;
                R *= a;
                p /= a;
            }
        }
        
        // Calculate base color using the user-defined color input
        vec3 color = baseColor.rgb * 5.0 / 50000.0 / log(R + 1.0);
        color *= brightness;
        
        vec4 previousFrame = IMG_NORM_PIXEL(feedbackBuffer, isf_FragNormCoord);
        vec3 feedbackColor = previousFrame.rgb * feedbackDecay;
        
        vec3 finalColor;
        if (FRAMEINDEX < 3) {
            finalColor = color;
        } else {
            finalColor = mix(color, feedbackColor, feedbackAmount) + color * 0.1;
        }
        
        finalColor = clamp(finalColor, 0.0, 1.0);
        
        gl_FragColor = vec4(finalColor, 1.0);
        
    } else { // PASSINDEX == 2
        // PASS 2: Output the accumulated result
        vec4 accumulatedColor = IMG_NORM_PIXEL(feedbackBuffer, isf_FragNormCoord);
        
        vec3 outputColor = accumulatedColor.rgb;
        
        vec2 uv = isf_FragNormCoord * 2.0 - 1.0;
        float vignette = 1.0 - dot(uv * 0.5, uv * 0.5);
        outputColor *= vignette;
        
        gl_FragColor = vec4(outputColor, 1.0);
    }
}
