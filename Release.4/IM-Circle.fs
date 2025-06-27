/*{
  "DESCRIPTION": "ISF conversion of a 3D raymarched fractal torus. Features a looping orbital camera and customizable fractal parameters.",
  "CREDIT": "Original twigl.app shader by @YoheiNishitsuji (https://x.com/YoheiNishitsuji?ref_src=twsrc%5Egoogle%7Ctwcamp%5Eserp%7Ctwgr%5Eauthor), ISF V2.0 Version by @dot2dot",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR"],
  "INPUTS": [
    {
      "NAME": "speed",
      "TYPE": "float",
      "DEFAULT": 0.5,
      "MIN": -5.0,
      "MAX": 5.0,
      "LABEL": "Animation Speed"
    },
    {
        "NAME": "cameraOrbitSpeed",
        "TYPE": "float",
        "DEFAULT": 0.2,
        "MIN": 0.0,
        "MAX": 2.0,
        "LABEL": "Camera Orbit Speed"
    },
    {
        "NAME": "cameraOrbitRadius",
        "TYPE": "float",
        "DEFAULT": 2.5,
        "MIN": 0.0,
        "MAX": 10.0,
        "LABEL": "Camera Orbit Radius"
    },
    {
      "NAME": "colorControl",
      "TYPE": "color",
      "DEFAULT": [0.8, 0.9, 1.0, 1.0],
      "LABEL": "Base Color"
    },
    {
        "NAME": "foldOffset",
        "TYPE": "float",
        "DEFAULT": 0.8,
        "MIN": 0.0,
        "MAX": 2.0,
        "LABEL": "Fractal Fold Offset"
    },
    {
        "NAME": "torusMajorRadius",
        "TYPE": "float",
        "DEFAULT": 1.0,
        "MIN": 0.0,
        "MAX": 3.0,
        "LABEL": "Torus Major Radius"
    },
    {
        "NAME": "torusMinorRadius",
        "TYPE": "float",
        "DEFAULT": 0.3,
        "MIN": 0.01,
        "MAX": 2.0,
        "LABEL": "Torus Minor Radius"
    },
    {
        "NAME": "glowIntensity",
        "TYPE": "float",
        "DEFAULT": 0.02,
        "MIN": 0.0,
        "MAX": 0.1,
        "LABEL": "Glow Intensity"
    },
    {
        "NAME": "glowFalloff",
        "TYPE": "float",
        "DEFAULT": 99.0,
        "MIN": 1.0,
        "MAX": 200.0,
        "LABEL": "Glow Falloff"
    },
    {
      "NAME": "transitionSpeed",
      "TYPE": "float",
      "DEFAULT": 1.0,
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

void main() {
  if (PASSINDEX == 0) {
    // First pass: Update the accumulated time in the persistent buffer
    vec4 prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));

    // Extract previous accumulated time and speed
    float accumulatedTime = prevTimeData.r;
    float currentSpeed = prevTimeData.g;
    float newTime;
    float adjustedSpeed;

    // Calculate new accumulated time
    if (FRAMEINDEX == 0) {
      // Initialize time on the first frame
      newTime = 0.0;
      adjustedSpeed = speed;
    } else {
      // Smoothly transition to the target speed
      adjustedSpeed = mix(currentSpeed, speed, min(1.0, TIMEDELTA * transitionSpeed));
      newTime = accumulatedTime + adjustedSpeed * TIMEDELTA;
    }

    // Store the new accumulated time and current smoothed speed
    gl_FragColor = vec4(newTime, adjustedSpeed, 0.0, 1.0);

  } else {
    // Final pass: Render the shader using the accumulated values from the buffer
    vec4 prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
    float effectiveTime = prevTimeData.r;

    // --- Procedural Camera Setup ---
    // Create a looping time variable for animation.
    float animTime = effectiveTime * cameraOrbitSpeed;
    // Calculate a camera position (Ray Origin) that orbits the fractal in a circle.
    // This ensures the camera never flies away from the scene.
    vec3 rayOrigin = vec3(cos(animTime) * cameraOrbitRadius, 0.0, sin(animTime) * cameraOrbitRadius);

    // --- Raymarching setup ---
    float d = 0.0; // Distance to the surface
    float a = 0.0; // Accumulated distance traveled along the ray
    vec4 accumulatedColor = vec4(0.0);
    
    vec2 fragCoord = isf_FragNormCoord * RENDERSIZE;

    // Main raymarching loop
    for(float i = 0.0; i < 159.0; i++) {

      // 1. Calculate the ray direction and current point 'p'
      vec3 rayDir = (vec3(fragCoord.x, 0.0, fragCoord.y) * 2.0 - RENDERSIZE.xxy) / RENDERSIZE.x;

      a += d; // Advance total distance by the result from the last step.
      
      // Calculate world-space position of the point: p = camera_pos + camera_dir * distance
      vec3 p = rayOrigin + rayDir * a;

      // 2. Apply fractal folding
      vec3 foldConst = vec3(foldOffset);
      for(int k = 0; k < 30; k++) {
        p = abs(p) - foldConst;
      }

      // 3. Estimate distance 'd' to the surface
      d = abs(length(vec2(length(p.xz) - torusMajorRadius, p.y)) - torusMinorRadius) + 0.0008;

      // 4. Accumulate color
      accumulatedColor.rgb += (0.008 - glowIntensity / exp(glowFalloff * d)) * colorControl.rgb;
    }

    accumulatedColor.a = 1.0;
    gl_FragColor = accumulatedColor;
  }
}