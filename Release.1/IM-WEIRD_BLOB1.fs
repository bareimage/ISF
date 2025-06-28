/*{
  "DESCRIPTION": "3D Fractal Hourglass with Random Blobs",
  "CREDIT": "Created by dot2dot, based on total random shit",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR"],
  "INPUTS": [
    {
      "NAME": "speed",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 50.0,
      "LABEL": "Speed"
    },
    {
      "NAME": "colorControl",
      "TYPE": "color",
      "DEFAULT": [0.1, 0.4, 0.6, 1.0],
      "LABEL": "Base Color"
    },
    {
      "NAME": "rotationDirection",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": -1.0,
      "MAX": 1.0,
      "LABEL": "Rotation Direction (1 or -1)"
    },
    {
      "NAME": "zoom",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.1,
      "MAX": 5.0,
      "LABEL": "Zoom Level"
    },
    {
      "NAME": "transitionSpeed",
      "TYPE": "float",
      "DEFAULT": 5.0,
      "MIN": 0.1,
      "MAX": 20.0,
      "LABEL": "Transition Speed (higher=faster)"
    },
    {
      "NAME": "complexity",
      "TYPE": "float",
      "DEFAULT": 5.0,
      "MIN": 1.0,
      "MAX": 10.0,
      "LABEL": "Fractal Complexity"
    },
    {
      "NAME": "blobSize",
      "TYPE": "float",
      "DEFAULT": 0.5,
      "MIN": 0.1,
      "MAX": 2.0,
      "LABEL": "Blob Size"
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
      "TARGET": "rotationBuffer",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": 1,
      "HEIGHT": 1
    },
    {
      "TARGET": "zoomBuffer",
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

// Hash function for pseudo-random values
float hash(vec3 p) {
    p = fract(p * vec3(123.34, 234.34, 345.65));
    p += dot(p, p + 34.45);
    return fract(p.x * p.y * p.z);
}

// 3D Noise function
float noise(vec3 p) {
    vec3 i = floor(p);
    vec3 f = fract(p);
    
    // Smooth interpolation
    vec3 u = f * f * (3.0 - 2.0 * f);
    
    // Mix 8 corners of the cube
    float n000 = hash(i);
    float n001 = hash(i + vec3(0.0, 0.0, 1.0));
    float n010 = hash(i + vec3(0.0, 1.0, 0.0));
    float n011 = hash(i + vec3(0.0, 1.0, 1.0));
    float n100 = hash(i + vec3(1.0, 0.0, 0.0));
    float n101 = hash(i + vec3(1.0, 0.0, 1.0));
    float n110 = hash(i + vec3(1.0, 1.0, 0.0));
    float n111 = hash(i + vec3(1.0, 1.0, 1.0));
    
    // Interpolate
    return mix(mix(mix(n000, n100, u.x),
                  mix(n010, n110, u.x), u.y),
               mix(mix(n001, n101, u.x),
                  mix(n011, n111, u.x), u.y), u.z);
}

// Fractal Brownian Motion (FBM)
float fbm(vec3 p, float T) {
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;
    
    // Animate the noise
    p += vec3(0.0, 0.0, T * 0.1);
    
    // Add octaves of noise
    for (int i = 0; i < int(complexity); i++) {
        value += amplitude * noise(p * frequency);
        frequency *= 2.0;
        amplitude *= 0.5;
        
        // Rotate the domain for more interesting patterns
        float angle = T * 0.05 + float(i) * 0.1;
        mat3 rot = mat3(
            cos(angle), sin(angle), 0.0,
            -sin(angle), cos(angle), 0.0,
            0.0, 0.0, 1.0
        );
        p = rot * p;
    }
    
    return value;
}

// Hourglass shape function
float hourglass(vec3 p, float T) {
    // Basic hourglass shape
    float waist = 0.2 + 0.1 * sin(T * 0.3);
    float bulb = 1.0 + 0.2 * sin(T * 0.2);
    
    // Calculate hourglass profile
    float y = abs(p.y);
    float r = length(p.xz);
    
    // Hourglass equation: r = waist + (bulb-waist) * (y/height)^2
    float height = 1.5;
    float ideal_r = waist + (bulb - waist) * pow(y / height, 2.0);
    
    // Distance to the hourglass surface
    return r - ideal_r;
}

// Distance function for the fractal hourglass with blobs
float map(vec3 p, float T) {
    // Hourglass base shape
    float d_hourglass = hourglass(p, T);
    
    // Add fractal noise to create blobs
    float noise_scale = blobSize * (0.8 + 0.2 * sin(T * 0.4));
    float noise_value = fbm(p * 2.0, T) * noise_scale;
    
    // Combine base shape with noise
    return d_hourglass - noise_value;
}

// Calculate normal at point p
vec3 calcNormal(vec3 p, float T) {
    vec2 e = vec2(0.001, 0.0);
    return normalize(vec3(
        map(p + e.xyy, T) - map(p - e.xyy, T),
        map(p + e.yxy, T) - map(p - e.yxy, T),
        map(p + e.yyx, T) - map(p - e.yyx, T)
    ));
}

void main() {
    vec4 prevTimeData, prevRotData, prevZoomData;
    float accumulatedTime, currentSpeed, adjustedSpeed, newTime;
    float currentRotDir, prevRotDir, effectiveRotDir;
    float currentZoom, prevZoom, effectiveZoom;
    float effectiveTime;
    vec2 R, uv, uvc, zoomedUvc;
    vec3 p, rd, n;
    float d, dd;

    // Calculate smoothing factor: higher transitionSpeed = faster transition
    float smoothing = min(1.0, TIMEDELTA * transitionSpeed);

    if (PASSINDEX == 0) {
        // Time/speed smoothing
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        accumulatedTime = prevTimeData.r;
        currentSpeed = prevTimeData.g;
        if (FRAMEINDEX == 0) {
            newTime = 0.0;
            adjustedSpeed = speed;
        } else {
            adjustedSpeed = mix(currentSpeed, speed, smoothing);
            newTime = accumulatedTime + adjustedSpeed * TIMEDELTA;
        }
        gl_FragColor = vec4(newTime, adjustedSpeed, 0.0, 1.0);
    }
    else if (PASSINDEX == 1) {
        // Rotation smoothing
        prevRotData = IMG_NORM_PIXEL(rotationBuffer, vec2(0.5, 0.5));
        if (FRAMEINDEX == 0) {
            currentRotDir = rotationDirection;
        } else {
            prevRotDir = prevRotData.r;
            currentRotDir = mix(prevRotDir, rotationDirection, smoothing);
        }
        gl_FragColor = vec4(currentRotDir, 0.0, 0.0, 1.0);
    }
    else if (PASSINDEX == 2) {
        // Zoom smoothing
        prevZoomData = IMG_NORM_PIXEL(zoomBuffer, vec2(0.5, 0.5));
        if (FRAMEINDEX == 0) {
            currentZoom = zoom;
        } else {
            prevZoom = prevZoomData.r;
            currentZoom = mix(prevZoom, zoom, smoothing);
        }
        gl_FragColor = vec4(currentZoom, 0.0, 0.0, 1.0);
    }
    else {
        // Final render pass
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        prevRotData = IMG_NORM_PIXEL(rotationBuffer, vec2(0.5, 0.5));
        prevZoomData = IMG_NORM_PIXEL(zoomBuffer, vec2(0.5, 0.5));
        effectiveTime = prevTimeData.r;
        effectiveRotDir = prevRotData.r;
        effectiveZoom = prevZoomData.r;

        R = RENDERSIZE.xy;
        uv = isf_FragNormCoord.xy * R;
        uvc = (uv - R/2.0) / R.y;
        zoomedUvc = uvc / effectiveZoom;

        // Camera setup
        vec3 cameraPos = vec3(0.0, 0.0, -3.0);
        rd = normalize(vec3(zoomedUvc.xy, 1.0));
        
        // Apply rotation to ray direction
        float angle = effectiveRotDir * effectiveTime * 0.1;
        mat2 rotation = mat2(cos(angle), -sin(angle), sin(angle), cos(angle));
        rd.xy = rotation * rd.xy;
        
        // Ray marching setup
        p = cameraPos;
        d = 0.0;
        float maxDist = 10.0;
        float minDist = 0.001;
        bool hit = false;
        
        // Ray marching loop
        for (int i = 0; i < 100; i++) {
            vec3 pos = p + rd * d;
            dd = map(pos, effectiveTime);
            
            if (dd < minDist) {
                hit = true;
                break;
            }
            
            if (d > maxDist) break;
            
            d += dd;
        }
        
        if (hit) {
            // We hit the fractal - calculate position and normal
            vec3 pos = p + rd * d;
            n = calcNormal(pos, effectiveTime);
            
            // Enhanced lighting
            vec3 lightDir = normalize(vec3(1.0, 1.0, -1.0));
            float diff = max(dot(n, lightDir), 0.0);
            float amb = 0.3;
            float spec = pow(max(dot(reflect(-lightDir, n), -rd), 0.0), 16.0);
            
            // Add fractal coloring based on position and normal
            vec3 baseColor = colorControl.rgb;
            vec3 posColor = 0.5 + 0.5 * sin(pos * 0.5 + effectiveTime * 0.2);
            vec3 finalColor = mix(baseColor, posColor, 0.5) * (amb + diff) + vec3(0.5) * spec;
            
            // Add glow effect based on distance and noise
            float noise_glow = fbm(pos * 3.0, effectiveTime) * 0.5;
            float glow = 0.2 / (0.1 + d * d * 0.1) + noise_glow;
            finalColor += baseColor * glow;
            
            gl_FragColor = vec4(finalColor, 1.0);
        } else {
            // Background with subtle fractal fog
            float depth = length(uvc);
            float fog = fbm(vec3(uvc * 2.0, effectiveTime * 0.1), effectiveTime) * 0.3;
            vec3 bgColor = mix(vec3(0.05), colorControl.rgb * 0.3, depth + fog);
            gl_FragColor = vec4(bgColor, 1.0);
        }
    }
}
