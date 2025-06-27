/*{
  "DESCRIPTION": "Conversion of a TWIGL shader. Creates a hypnotic, rotating fractal tunnel with smoothed (dampened) parameter transitions.",
  "CREDIT": "Original GLSL code by @YoheiNishitsuji (https://x.com/YoheiNishitsuji/status/1918213871375892638) twigl.app. ISF 2.0 Version by @dot2dot.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR"],
  "INPUTS": [
    {
      "NAME": "speed",
      "TYPE": "float",
      "DEFAULT": 0.8,
      "MIN": -2.0,
      "MAX": 5.0,
      "LABEL": "Forward Speed"
    },
    {
      "NAME": "rotationSpeed",
      "TYPE": "float",
      "DEFAULT": 0.4,
      "MIN": -2.0,
      "MAX": 2.0,
      "LABEL": "Rotation Speed"
    },
    {
      "NAME": "hue",
      "TYPE": "float",
      "DEFAULT": 0.1,
      "MIN": 0.0,
      "MAX": 1.0,
      "LABEL": "Hue"
    },
    {
      "NAME": "saturation",
      "TYPE": "float",
      "DEFAULT": 0.15,
      "MIN": 0.0,
      "MAX": 1.0,
      "LABEL": "Saturation"
    },
    {
        "NAME": "brightness",
        "TYPE": "float",
        "DEFAULT": 1.0,
        "MIN": 0.0,
        "MAX": 5.0,
        "LABEL": "Brightness"
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
      "TARGET": "persistentBuffer",
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

// Helper function to convert HSV color space to RGB
vec3 hsv(float h, float s, float v){
    vec4 t = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(vec3(h) + t.xyz) * 6.0 - vec3(t.w));
    return v * mix(vec3(t.x), clamp(p - vec3(t.x), 0.0, 1.0), s);
}

void main() {
  if (PASSINDEX == 0) {
    // PASS 0: Update and store smoothed time and speed values in a persistent buffer.
    
    // Read the state from the previous frame
    vec4 prevState = IMG_NORM_PIXEL(persistentBuffer, vec2(0.5));
    float prevAccTimeFwd = prevState.r;
    float prevAccTimeRot = prevState.g;
    float prevSpeedFwd = prevState.b;
    float prevSpeedRot = prevState.a;

    float adjustedSpeedFwd;
    float adjustedSpeedRot;
    float newAccTimeFwd;
    float newAccTimeRot;

    if (FRAMEINDEX == 0) {
      // Initialize on the first frame
      adjustedSpeedFwd = speed;
      adjustedSpeedRot = rotationSpeed;
      newAccTimeFwd = 0.0;
      newAccTimeRot = 0.0;
    } else {
      // Smoothly interpolate from the previous speed to the target speed
      float mixFactor = min(1.0, TIMEDELTA * transitionSpeed);
      adjustedSpeedFwd = mix(prevSpeedFwd, speed, mixFactor);
      adjustedSpeedRot = mix(prevSpeedRot, rotationSpeed, mixFactor);

      // Update accumulated times with the new smoothed speeds
      newAccTimeFwd = prevAccTimeFwd + adjustedSpeedFwd * TIMEDELTA;
      newAccTimeRot = prevAccTimeRot + adjustedSpeedRot * TIMEDELTA;
    }

    // Store the new state (accumulated times, current speeds) into the buffer for the next frame
    gl_FragColor = vec4(newAccTimeFwd, newAccTimeRot, adjustedSpeedFwd, adjustedSpeedRot);

  } else {
    // PASS 1: Render the final visual using the smoothed values.

    // Read the smoothed accumulated time for forward motion and rotation
    vec4 smoothedVals = IMG_NORM_PIXEL(persistentBuffer, vec2(0.5));
    float timeFwd = smoothedVals.r;
    float timeRot = smoothedVals.g;

    // --- Start of ported twigl.app shader logic ---

    vec3 color = vec3(0.0);
    // Initialize variables used in the loop. These carry state between iterations.
    float i=0.0, e=0.0, R=0.0, s=1.0; 
    
    // Set initial ray position and direction
    vec3 q = vec3(0.0, -1.0, -1.0);
    vec3 d = vec3(isf_FragNormCoord.xy - vec2(0.5, -0.3), 0.8);

    // Main raymarching loop
    for(;i++<99.;){
        // 1. Add color based on the result of the previous iteration's calculations (e, s)
        // This creates a trail/feedback effect.
        color += hsv(hue, saturation, clamp(min(e * s, 0.7 - e) / 35.0, 0.0, 1.0));

        // 2. March the ray using the previous iteration's distance (e) and length (R)
        q += d * e * R * 0.2;
        vec3 p = q;

        // 3. Transform the current point in space to create the fractal tunnel effect
        R = length(p);
        if (R < 0.0001) R = 0.0001; // Avoid log(0)
        p = vec3(log(R) - timeFwd, exp(0.8 - p.z / R), atan(p.y, p.x) + timeRot);

        // 4. Calculate a new distance estimate 'e' and scale 's' for the current point.
        // These values will be used in the *next* iteration for coloring and raymarching.
        e = --p.y; // Initialize distance estimator by pre-decrementing p.y
        s = 1.0;   // Reset scale for the inner loop
        for(int k=0; k<9; k++) { // Inner loop iterates to refine the distance estimate
            e += dot(sin(p.yzz * s) - 0.5, 0.8 - sin(p.zxx * s)) / s * 0.3;
            s *= 2.0; // Double the scale
        }

        // 5. Break if the ray escapes
        if (R > 100.0) break;
    }

    // Apply brightness and set the final fragment color
    gl_FragColor = vec4(color * brightness, 1.0);
  }
}