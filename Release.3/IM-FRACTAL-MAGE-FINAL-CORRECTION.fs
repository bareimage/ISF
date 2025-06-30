/*{
  "ISFVSN": "2.0",
  "CREDIT": "Original shader by @dot2dot, ISF conversion and corrections for smooth animation and Metal compatibility.",
  "DESCRIPTION": "A kaleidoscopic DMT trip. Tunnel effect with smoothed controls, kaleidoscope, distortion, and fadeout. Corrected for smooth animation.",
  "CATEGORIES": ["GENERATOR"],
  "INPUTS": [
    {"NAME": "segments", "LABEL": "Floral Pattern Segments", "TYPE": "float", "MIN": 4.0, "MAX": 48.0, "DEFAULT": 26.0},
    {"NAME": "zoom", "LABEL": "Tunnel FOV (Zoom)", "TYPE": "float", "MIN": 0.25, "MAX": 3.0, "DEFAULT": 1.0},
    {"NAME": "tunnelSpeed", "LABEL": "Tunnel Speed", "TYPE": "float", "MIN": -2.0, "MAX": 2.0, "DEFAULT": 0.2},
    {"NAME": "tunnelRotationSpeed", "LABEL": "Tunnel Rotation Speed", "TYPE": "float", "MIN": -1.0, "MAX": 1.0, "DEFAULT": 0.1},
    {"NAME": "kaleidoscopeWedges", "LABEL": "Kaleidoscope Wedges", "TYPE": "float", "MIN": 1.0, "MAX": 12.0, "DEFAULT": 3.0},
    {"NAME": "distortionAmount", "LABEL": "Distortion Amount", "TYPE": "float", "MIN": 0.0, "MAX": 0.5, "DEFAULT": 0.0},
    {"NAME": "distortionFrequency", "LABEL": "Distortion Frequency", "TYPE": "float", "MIN": 1.0, "MAX": 50.0, "DEFAULT": 10.0},
    {"NAME": "distortionSpeed", "LABEL": "Distortion Animation Speed", "TYPE": "float", "MIN": 0.0, "MAX": 2.0, "DEFAULT": 0.5},
    {"NAME": "farFadeStart", "LABEL": "Far Fade Start (Radius)", "TYPE": "float", "MIN": 0.01, "MAX": 1.0, "DEFAULT": 0.15},
    {"NAME": "farFadeEnd", "LABEL": "Far Fade End (Radius)", "TYPE": "float", "MIN": 0.0, "MAX": 0.5, "DEFAULT": 0.05},
    {"NAME": "color1", "LABEL": "Color 1", "TYPE": "color", "DEFAULT": [0.5, 0.2, 0.9, 1.0]},
    {"NAME": "color2", "LABEL": "Color 2", "TYPE": "color", "DEFAULT": [1.0, 0.6, 0.9, 1.0]},
    {"NAME": "color3", "LABEL": "Color 3", "TYPE": "color", "DEFAULT": [0.1, 0.9, 0.9, 1.0]},
    {"NAME": "color4", "LABEL": "Color 4", "TYPE": "color", "DEFAULT": [0.7, 1.0, 0.9, 1.0]},
    {"NAME": "colorBg", "LABEL": "Background Color", "TYPE": "color", "DEFAULT": [0.0, 0.0, 0.0, 1.0]},
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
      "TARGET": "tunnelTimeBuffer",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": 1,
      "HEIGHT": 1
    },
    {
      "TARGET": "tunnelRotationBuffer",
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

precision highp float;
#define PI 3.14159265359

void main() {
    // Variables for speed smoothing
    vec4 prevTunnelTimeData, prevTunnelRotationData;
    float accumulatedTunnelTime, currentSmoothedTunnelSpeed, adjustedTunnelSpeed, newTunnelTime;
    float accumulatedRotationTime, currentSmoothedRotationSpeed, adjustedRotationSpeed, newRotationTime;
    float effectiveTunnelTime, effectiveRotationTime;

    // Original shader variables (some will be replaced by smoothed versions)
    vec2 uv, duv;
    float angle, radius, tunnelCoord, tunnelPhase, kAngle;
    float wedges, wedgeAngle, angleMod, symAngle;
    vec3 finalColor;
    float actualFarFadeEnd, patternVis;
    float dTime; // For distortion, remains tied to TIME

    if (PASSINDEX == 0) {
        // Pass 0: Update Tunnel Speed and Accumulated Tunnel Time
        prevTunnelTimeData = IMG_NORM_PIXEL(tunnelTimeBuffer, vec2(0.5, 0.5));
        accumulatedTunnelTime = prevTunnelTimeData.r;
        currentSmoothedTunnelSpeed = prevTunnelTimeData.g;

        if (FRAMEINDEX == 0) {
            newTunnelTime = 0.0;
            adjustedTunnelSpeed = tunnelSpeed;
        } else {
            adjustedTunnelSpeed = mix(currentSmoothedTunnelSpeed, tunnelSpeed, min(1.0, TIMEDELTA * transitionSpeed));
            newTunnelTime = accumulatedTunnelTime + adjustedTunnelSpeed * TIMEDELTA;
        }
        gl_FragColor = vec4(newTunnelTime, adjustedTunnelSpeed, 0.0, 1.0);

    } else if (PASSINDEX == 1) {
        // Pass 1: Update Tunnel Rotation Speed and Accumulated Rotation Time
        prevTunnelRotationData = IMG_NORM_PIXEL(tunnelRotationBuffer, vec2(0.5, 0.5));
        accumulatedRotationTime = prevTunnelRotationData.r;
        currentSmoothedRotationSpeed = prevTunnelRotationData.g;

        if (FRAMEINDEX == 0) {
            newRotationTime = 0.0;
            adjustedRotationSpeed = tunnelRotationSpeed;
        } else {
            adjustedRotationSpeed = mix(currentSmoothedRotationSpeed, tunnelRotationSpeed, min(1.0, TIMEDELTA * transitionSpeed));
            newRotationTime = accumulatedRotationTime + adjustedRotationSpeed * TIMEDELTA;
        }
        gl_FragColor = vec4(newRotationTime, adjustedRotationSpeed, 0.0, 1.0);

    } else {
        // Final Render Pass
        prevTunnelTimeData = IMG_NORM_PIXEL(tunnelTimeBuffer, vec2(0.5, 0.5));
        prevTunnelRotationData = IMG_NORM_PIXEL(tunnelRotationBuffer, vec2(0.5, 0.5));

        effectiveTunnelTime = prevTunnelTimeData.r;
        effectiveRotationTime = prevTunnelRotationData.r;

        // Normalize coordinates and aspect
        uv = vv_FragNormCoord * 2.0 - 1.0; // Using vv_FragNormCoord as in the original
        uv.x *= RENDERSIZE.x / RENDERSIZE.y;
        uv *= zoom;

        // Animate tunnel and rotation using SMOOTHED accumulated times
        // float tunnelTime = TIME * tunnelSpeed; // Old
        // float rotationTime = TIME * tunnelRotationSpeed; // Old
        float currentTunnelTime = effectiveTunnelTime;
        float currentRotationTime = effectiveRotationTime;

        // Optional distortion (keeps original TIME * distortionSpeed behavior)
        duv = uv;
        if (distortionAmount > 0.0) {
            dTime = TIME * distortionSpeed;
            duv.x += sin(uv.y * distortionFrequency + dTime) * distortionAmount;
            duv.y += cos(uv.x * distortionFrequency - dTime) * distortionAmount;
        }

        angle = atan(duv.y, duv.x) + currentRotationTime;
        radius = length(duv);

        // Kaleidoscope
        wedges = max(1.0, kaleidoscopeWedges);
        wedgeAngle = PI / wedges;
        angleMod = mod(angle, wedgeAngle * 2.0);
        symAngle = abs(angleMod - wedgeAngle);
        kAngle = (symAngle / wedgeAngle) * (2.0 * PI);

        // Tunnel effect
        float safeRadius = max(radius, 0.05); // Ensure radius is not zero before division
        tunnelCoord = 1.0 / safeRadius;
        tunnelPhase = fract(tunnelCoord + currentTunnelTime);


        // Pattern logic (unchanged from original, uses tunnelPhase and kAngle)
        finalColor = colorBg.rgb;
        if (tunnelPhase < 0.1) {
            finalColor = mix(color1.rgb, color2.rgb, smoothstep(0.0, 0.1, tunnelPhase));
        } else if (tunnelPhase < 0.2) {
            float petals = 16.0;
            float pa = mod(kAngle, 2.0 * PI / petals);
            float shape = smoothstep(0.05, 0.04, abs(pa - PI / petals));
            shape *= smoothstep(0.1, 0.11, tunnelPhase) * (1.0 - smoothstep(0.19, 0.2, tunnelPhase));
            finalColor = mix(colorBg.rgb, color1.rgb, shape);
        } else if (tunnelPhase < 0.3) {
            float spikes = 32.0;
            float sf = cos(kAngle * spikes);
            finalColor = mix(color3.rgb, colorBg.rgb, smoothstep(0.8, 0.9, sf) * 0.8);
        } else if (tunnelPhase < 0.45) {
            float sa = mod(kAngle, 2.0 * PI / segments);
            float shape = smoothstep(0.2, 0.1, abs(sa - PI / segments));
            shape -= smoothstep(0.4, 0.39, tunnelPhase) * 0.5; // This was an interesting subtraction
            vec3 rc = mix(color1.rgb, color2.rgb, smoothstep(0.3, 0.45, tunnelPhase));
            finalColor = mix(colorBg.rgb, rc, clamp(shape, 0.0, 1.0));
        } else if (tunnelPhase < 0.55) {
            float wf = sin(kAngle * segments); //segments should be float, it is.
            finalColor = mix(color3.rgb, colorBg.rgb, smoothstep(0.1, 0.2, wf));
        } else if (tunnelPhase < 0.9) {
            float a = kAngle + PI / segments;
            float sa = mod(a, 2.0 * PI / segments) - PI / segments;
            vec2 cuv = vec2(sa * tunnelPhase, tunnelPhase); // tunnelPhase can be up to 0.9 here
            vec2 size = vec2(0.1, 0.35);
            vec2 d_val = abs(cuv) - size + 0.05; // d is a keyword in some GLSL versions, renamed to d_val
            float capsule = smoothstep(0.01, 0.0, length(max(d_val, 0.0)));
            if (capsule > 0.5) {
                vec3 cc = mix(colorBg.rgb, color2.rgb, smoothstep(0.02, 0.0, abs(cuv.x)));
                cc = mix(cc, color4.rgb, smoothstep(0.8, 0.82, cuv.y)); // cuv.y is tunnelPhase
                float eye = smoothstep(0.01, 0.0, length(vec2(cuv.x, cuv.y - 0.85)));
                finalColor = mix(cc, colorBg.rgb, eye * 0.8);
            }
        }

        // Far region fade
        actualFarFadeEnd = min(farFadeStart - 0.001, farFadeEnd); // ensure start is greater than end
        patternVis = smoothstep(actualFarFadeEnd, farFadeStart, radius);
        finalColor = mix(colorBg.rgb, finalColor, patternVis);

        gl_FragColor = vec4(finalColor, 1.0);
    }
}
