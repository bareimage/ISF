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
