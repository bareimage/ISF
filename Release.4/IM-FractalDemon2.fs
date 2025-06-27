/*{
  "DESCRIPTION": "A 3D folding fractal converted from a twigl.app shader. Features smoothed animation speed and intensity control.",
  "CREDIT": "Original by @YoheiNishitsuji twigl.app s. ISF 2.0 with parameter smoothing by dot2dot.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR", "FRACTAL"],
  "INPUTS": [
    { "NAME": "speed", "TYPE": "float", "DEFAULT": 0.4, "MIN": 0.0, "MAX": 3.0, "LABEL": "Animation Speed" },
    { "NAME": "transitionSpeed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 10.0, "LABEL": "Speed Smoothing" },
    { "NAME": "zoom", "TYPE": "float", "DEFAULT": 5.0, "MIN": 1.0, "MAX": 10.0, "LABEL": "Zoom" },
    { "NAME": "hue", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 1.0, "LABEL": "Color Hue" },
    { "NAME": "saturation", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 2.0, "LABEL": "Saturation" },
    { "NAME": "brightness", "TYPE": "float", "DEFAULT": 0.00025, "MIN": 0.0, "MAX": 0.001, "LABEL": "Brightness", "STEP": 0.00001 },
    { "NAME": "intensity", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0, "LABEL": "Fractal Intensity" },
    { "NAME": "rotAxisX", "TYPE": "float", "DEFAULT": 1.0, "MIN": -1.0, "MAX": 1.0, "LABEL": "Rotation Axis X" },
    { "NAME": "rotAxisY", "TYPE": "float", "DEFAULT": 1.0, "MIN": -1.0, "MAX": 1.0, "LABEL": "Rotation Axis Y" },
    { "NAME": "rotAxisZ", "TYPE": "float", "DEFAULT": 1.0, "MIN": -1.0, "MAX": 1.0, "LABEL": "Rotation Axis Z" }
  ],
  "PASSES": [
    { "TARGET": "timeBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "finalOutput" }
  ]
}*/

// Helper function for converting HSV color to RGB
vec3 hsv(float h, float s, float v){
    vec4 t = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(vec3(h) + t.xyz) * 6.0 - vec3(t.w));
    return v * mix(vec3(t.x), clamp(p - vec3(t.x), 0.0, 1.0), s);
}

// Helper function for 3D rotation
mat3 rotate3D(float angle, vec3 axis){
    float s = sin(angle);
    float c = cos(angle);
    float oc = 1.0 - c;
    // Normalize axis to ensure proper rotation, check for zero vector
    vec3 a = normalize(axis);
    if (length(axis) == 0.0) {
      a = vec3(0.0, 0.0, 1.0); // Default to Z-axis if input is zero
    }
    return mat3(
        oc * a.x * a.x + c,         oc * a.x * a.y - a.z * s,   oc * a.z * a.x + a.y * s,
        oc * a.x * a.y + a.z * s,   oc * a.y * a.y + c,         oc * a.y * a.z - a.x * s,
        oc * a.z * a.x - a.y * s,   oc * a.y * a.z + a.x * s,   oc * a.z * a.z + c
    );
}

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
        // --- Pass 0: Time Smoothing ---
        // This pass smooths the animation speed to prevent jerky movements when changing the 'speed' parameter.
        vec4 prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        float accumulatedTime = prevTimeData.r;
        float currentSpeed = prevTimeData.g;
        float adjustedSpeed;
        float newTime;
        
        if (FRAMEINDEX == 0) {
            newTime = 0.0;
            adjustedSpeed = speed;
        } else {
            // Mix between the last frame's speed and the target speed for a smooth transition.
            adjustedSpeed = mix(currentSpeed, speed, min(1.0, TIMEDELTA * transitionSpeed));
            newTime = accumulatedTime + adjustedSpeed * TIMEDELTA;
        }
        
        // Store the new time and speed in our 1x1 buffer for the next frame.
        gl_FragColor = vec4(newTime, adjustedSpeed, 0.1 , 1.0);
    } else {
        // --- Pass 1: Final Render ---
        vec4 prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        float effectiveTime = prevTimeData.r;

        vec4 outColor = vec4(0.0, 0.0, 0.0, 1.0);
        float g = 0.0;
        float e = 1.0;
        float s = 1.0;
        vec3 p;
        
        vec3 rotationAxis = vec3(rotAxisX, rotAxisY, rotAxisZ);

        // Quality loop: render the scene multiple times and accumulate the results for a richer effect.
        for(float i_qual = 1.0; i_qual <= 10.0; ++i_qual){
            // Initialize ray origin for this quality iteration.
            // Coordinates are set up based on screen position, zoom, and a time-based animation.
            // The 'g' variable is used in the z-component, creating feedback between iterations.
            p = vec3((isf_FragNormCoord.xy * RENDERSIZE.xy - 0.5 * RENDERSIZE.xy) / RENDERSIZE.x * (zoom - sin(effectiveTime * 0.3) * 2.0), g + 0.3)
                   * rotate3D(effectiveTime * 0.2, rotationAxis);
            s = 1.0;

            // **FIXED LOGIC**: Raymarching loop.
            // The order of operations has been corrected to match the original shader's logic,
            // where 'e' is calculated from 'p', then 's' is updated, and finally 'p' is updated using the new 'e'.
            // This ensures the correct feedback mechanism for the fractal generation.
            for(int i_ray = 0; i_ray < 45; ++i_ray){
                e = max(1.0, 10.0 / dot(p,p));
                s *= e;
                p = vec3(0.0, 3.1, 3.0) - abs(abs(p) * e - vec3(2.0, 2.8, 3.05));
            }
            
            // Accumulate geometry information into 'g'.
            g -= mod(length(p.yx + p.zy), p.y) / s * 0.6;
            
            // **ADDED INTENSITY**: Calculate final color.
            // The new 'intensity' uniform scales the geometric contribution ('g') to the saturation,
            // allowing for control over the color complexity and visual "intensity" of the fractal.
            outColor.rgb += hsv(hue, saturation * (g * i_qual * intensity - 0.4 * p.x), s * brightness);
        }
        
        gl_FragColor = outColor;
    }
}
