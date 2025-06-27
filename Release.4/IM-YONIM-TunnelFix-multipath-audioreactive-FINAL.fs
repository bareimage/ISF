/*{
    "CATEGORIES": [
        "GENERATOR",
        "AUDIO-REACTIVE"
    ],
    "CREDIT": "Based on Twigl shader by @zozuar (https://x.com/zozuar). Redesigned by @dot2dot, ISF 2.0 by @dot2dot",
    "DESCRIPTION": "Audio-reactive fractal tunnel with smoothed speed, shape, zoom, and modulation. Based on original code from twigl shader by @zozuar, this shader will not work in Milumin, as Milumin cant feed the AudioSignal.",
    "INPUTS": [
        {
            "DEFAULT": 1,
            "LABEL": "Speed",
            "MAX": 5,
            "MIN": -5,
            "NAME": "speed",
            "TYPE": "float"
        },
        {
            "DEFAULT": 2,
            "LABEL": "Time Transition Smoothness",
            "MAX": 10,
            "MIN": 0.1,
            "NAME": "transitionSpeed",
            "TYPE": "float"
        },
        {
            "DEFAULT": 4,
            "LABEL": "Fractal Shape",
            "MAX": 10,
            "MIN": -10,
            "NAME": "fractalShape",
            "TYPE": "float"
        },
        {
            "DEFAULT": 1,
            "LABEL": "Fractal Mod",
            "MAX": 2,
            "MIN": -0.2,
            "NAME": "fractalMod",
            "TYPE": "float"
        },
        {
            "DEFAULT": 2,
            "LABEL": "Mod Transition Speed",
            "MAX": 10,
            "MIN": 0.1,
            "NAME": "modTransition",
            "TYPE": "float"
        },
        {
            "DEFAULT": [
                0.796078431372549,
                0.1803921568627451,
                0.058823529411764705,
                1
            ],
            "LABEL": "Highlight Color",
            "NAME": "highlightColor",
            "TYPE": "color"
        },
        {
            "DEFAULT": [
                0.1,
                0.3,
                0.8,
                1
            ],
            "LABEL": "Base Color",
            "NAME": "baseColor",
            "TYPE": "color"
        },
        {
            "DEFAULT": true,
            "LABEL": "Use Audio Reactivity",
            "NAME": "useAudio",
            "TYPE": "bool"
        },
        {
            "LABEL": "Audio Input",
            "NAME": "audio",
            "TYPE": "audioFFT"
        },
        {
            "DEFAULT": 2,
            "LABEL": "Audio Reactivity",
            "MAX": 20,
            "MIN": 0,
            "NAME": "audioReactivity",
            "TYPE": "float"
        },
        {
            "DEFAULT": 1.5,
            "LABEL": "Audio Smoothness",
            "MAX": 1.5,
            "MIN": 10,
            "NAME": "audioSmoothness",
            "TYPE": "float"
        }
    ],
    "ISFVSN": "2",
    "PASSES": [
        {
            "FLOAT": true,
            "HEIGHT": 1,
            "PERSISTENT": true,
            "TARGET": "controlBuffer",
            "WIDTH": 1
        },
        {
            "FLOAT": true,
            "HEIGHT": 1,
            "PERSISTENT": true,
            "TARGET": "audioBuffer",
            "WIDTH": 1
        },
        {
        }
    ]
}
*/

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

// Helper noise functions (unchanged)
float hash(vec2 p) {
  return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

float snoise2D(vec2 v) {
  const vec4 C = vec4(0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439);
  vec2 i  = floor(v + dot(v, C.yy));
  vec2 x0 = v - i + dot(i, C.xx);
  vec2 i1 = (x0.x > x0.y) ? vec2(1.0, 0.0) : vec2(0.0, 1.0);
  vec2 x1 = x0.xy + C.xx - i1;
  vec2 x2 = x0.xy + C.zz;
  i = mod(i, 289.0);
  vec3 p = mod(((i.y + vec3(0.0, i1.y, 1.0)) + i.x + vec3(0.0, i1.x, 1.0)) * 34.0 + 1.0, 289.0);
  vec3 m = max(0.5 - vec3(dot(x0,x0), dot(x1,x1), dot(x2,x2)), 0.0);
  m = m*m; m = m*m;
  vec3 x = 2.0 * fract(p * C.www) - 1.0;
  vec3 h = abs(x) - 0.5;
  vec3 ox = floor(x + 0.5);
  vec3 a0 = x - ox;
  m *= 1.79284291400159 - 0.85373472095314 * (a0*a0 + h*h);
  vec3 g;
  g.x  = a0.x  * x0.x  + h.x  * x0.y;
  g.yz = a0.yz * vec2(x1.x,x2.x) + h.yz * vec2(x1.y,x2.y);
  return 130.0 * dot(m, g);
}

#define A p=abs(p)

// Modified render function to return a color value, using the new simplified parameters
vec4 render(in vec2 pixelCoord, in float t, in vec2 R, in float shape, in float mod, in vec4 _base, in vec4 _highlight) {
  vec3 p, q;
  float e, S;
  vec4 o = vec4(0.0);

  q = vec3(0.0, 1.0, t);
  e = 0.0;

  for (int i = 0; i < 99; i++) {
    S = 2.0;
    q += -vec3(abs(2.0 * pixelCoord.x - R.x), 2.0 * pixelCoord.y - R.y, -R.x) / R.y * e;
    p = q;
    p.z -= ceil(p.z);
    A; p -= 0.5;

    for (int j = 0; j < 2; j++) {
      e = max(min(dot(A - mod, p), 2.0) / 8.0, 0.001);
      S /= e;
      A; p = p / e - shape;
      p.z += shape - q.y;
    }
    
    A; p = p / S;
    e = 0.4 * min(q.y - 0.01 - p.x, p.z + p.x - 0.09);

    // --- FIX IS HERE ---
    // The term (2.0 - q.y) can explode when raymarching is unstable. Clamping it prevents overexposure.
    float base_brightness = clamp(2.0 - q.y, 0.0, 4.0);
    
    o += (0.02 / (1.0 + exp(max(e * 400.0, -10.0)))) * _highlight +
         (base_brightness / (1.0 + exp(min(p.z * 80.0, 20.0)))) * _base * 0.1;
  }

  return tanh(o);
}


void main() {
  if (PASSINDEX == 0) {
    // Pass 0: Update smoothed control values in the persistent buffer (Time, Speed, Shape)
    vec4 prevData = IMG_NORM_PIXEL(controlBuffer, vec2(0.5, 0.5));
    
    float accumulatedTime = prevData.r;
    float prevSpeed = prevData.g;
    float prevShape = prevData.b;
    
    float newTime;
    float smoothedSpeed, smoothedShape;
    
    if (FRAMEINDEX == 0) {
      newTime = 0.0;
      smoothedSpeed = speed;
      smoothedShape = fractalShape;
    } else {
      // Smooth Speed
      smoothedSpeed = mix(prevSpeed, speed, min(1.0, TIMEDELTA * transitionSpeed));
      newTime = accumulatedTime + smoothedSpeed * TIMEDELTA;

      // Smooth Shape
      smoothedShape = mix(prevShape, fractalShape, min(1.0, TIMEDELTA * modTransition));
    }
    
    // Output smoothed time, speed, and shape. Alpha is unused.
    gl_FragColor = vec4(newTime, smoothedSpeed, smoothedShape, 1.0);
  }
  else if (PASSINDEX == 1) {
    // Pass 1: Update the smoothed audio level and the smoothed FractalMod
    vec4 prevAudioData = IMG_NORM_PIXEL(audioBuffer, vec2(0.5, 0.5));
    float prevAudioLevel = prevAudioData.r;
    float prevMod = prevAudioData.g;
    
    float currentAudioLevel = texture(audio, vec2(0.1, 0.5)).r;
    float smoothedAudio;
    float smoothedMod;

    if(FRAMEINDEX == 0) {
        smoothedAudio = 0.0;
        smoothedMod = fractalMod;
    } else {
        smoothedAudio = mix(prevAudioLevel, currentAudioLevel, min(1.0, TIMEDELTA * audioSmoothness));
        smoothedMod = mix(prevMod, fractalMod, min(1.0, TIMEDELTA * modTransition));
    }
    // Output smoothed audio and the smoothed FractalMod value.
    gl_FragColor = vec4(smoothedAudio, smoothedMod, 0.0, 1.0);
  }
  else { // PASSINDEX == 2
    // Pass 2: Main render, colorization, and audio-reactivity
    vec2 R = RENDERSIZE;
    vec2 pixelCoord = isf_FragNormCoord * R;
    
    // Retrieve smoothed data from buffers
    vec4 controlData = IMG_NORM_PIXEL(controlBuffer, vec2(0.5, 0.5));
    vec4 audioData = IMG_NORM_PIXEL(audioBuffer, vec2(0.5, 0.5));

    float effectiveTime = controlData.r;
    float effectiveShape = controlData.b;
    
    float smoothedAudio = audioData.r;
    float effectiveMod = audioData.g;
    
    if (useAudio) {
        effectiveShape += smoothedAudio * audioReactivity;
    }
    
    // Call the render function with the final, smoothed values
    vec4 currentColor = render(pixelCoord, effectiveTime, R, effectiveShape, effectiveMod, baseColor, highlightColor);
    
    gl_FragColor = currentColor;
  }
}
