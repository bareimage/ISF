/*{
  "DESCRIPTION": "Animated heart with smooth transitions",
  "CREDIT": "Original code by @arlo (https://www.shadertoy.com/view/WdK3Dz), ISF 2.0 version by @dot2dot (bareimage)",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR"],
  "INPUTS": [
    {
      "NAME": "speed",
      "TYPE": "float",
      "DEFAULT": 0.5,
      "MIN": 0.0,
      "MAX": 5.0,
      "LABEL": "Speed"
    },
    {
      "NAME": "intensity",
      "TYPE": "float",
      "DEFAULT": 1.3,
      "MIN": 0.1,
      "MAX": 3.0,
      "LABEL": "Glow Intensity"
    },
    {
      "NAME": "radius",
      "TYPE": "float",
      "DEFAULT": 0.015,
      "MIN": 0.001,
      "MAX": 0.05,
      "LABEL": "Glow Radius"
    },
    {
      "NAME": "color1",
      "TYPE": "color",
      "DEFAULT": [1.0, 0.05, 0.3, 1.0],
      "LABEL": "First Heart Color"
    },
    {
      "NAME": "color2",
      "TYPE": "color",
      "DEFAULT": [0.1, 0.4, 1.0, 1.0],
      "LABEL": "Second Heart Color"
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

#define POINT_COUNT 8

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

float sdBezier(vec2 pos, vec2 A, vec2 B, vec2 C) {
    vec2 a = B - A;
    vec2 b = A - 2.0*B + C;
    vec2 c = a * 2.0;
    vec2 d = A - pos;
    float kk = 1.0 / dot(b,b);
    float kx = kk * dot(a,b);
    float ky = kk * (2.0*dot(a,a)+dot(d,b)) / 3.0;
    float kz = kk * dot(d,a);      
    float res = 0.0;
    float p = ky - kx*kx;
    float p3 = p*p*p;
    float q = kx*(2.0*kx*kx - 3.0*ky) + kz;
    float h = q*q + 4.0*p3;
    if(h >= 0.0){ 
        h = sqrt(h);
        vec2 x = (vec2(h, -h) - q) / 2.0;
        vec2 uv = sign(x)*pow(abs(x), vec2(1.0/3.0));
        float t = uv.x + uv.y - kx;
        t = clamp(t, 0.0, 1.0);
        vec2 qos = d + (c + b*t)*t;
        res = length(qos);
    } else {
        float z = sqrt(-p);
        float v = acos(q/(p*z*2.0)) / 3.0;
        float m = cos(v);
        float n = sin(v)*1.732050808;
        vec3 t = vec3(m + m, -n - m, n - m) * z - kx;
        t = clamp(t, 0.0, 1.0);
        vec2 qos = d + (c + b*t.x)*t.x;
        float dis = dot(qos,qos);
        res = dis;
        qos = d + (c + b*t.y)*t.y;
        dis = dot(qos,qos);
        res = min(res,dis);
        qos = d + (c + b*t.z)*t.z;
        dis = dot(qos,qos);
        res = min(res,dis);
        res = sqrt(res);
    }
    return res;
}

vec2 getHeartPosition(float t) {
    return vec2(16.0 * sin(t) * sin(t) * sin(t),
                -(13.0 * cos(t) - 5.0 * cos(2.0*t)
                - 2.0 * cos(3.0*t) - cos(4.0*t)));
}

float getGlow(float dist, float radius, float intensity) {
    return pow(radius/dist, intensity);
}

float getSegment(float t, vec2 pos, float offset, float effectiveSpeed) {
    vec2 points[POINT_COUNT];
    for(int i = 0; i < POINT_COUNT; i++) {
        points[i] = getHeartPosition(offset + float(i)*0.25 + fract(effectiveSpeed * t) * 6.28);
    }
    vec2 c = (points[0] + points[1]) / 2.0;
    vec2 c_prev;
    float dist = 10000.0;
    for(int i = 0; i < POINT_COUNT-1; i++) {
        c_prev = c;
        c = (points[i] + points[i+1]) / 2.0;
        dist = min(dist, sdBezier(pos, 0.012 * c_prev, 0.012 * points[i], 0.012 * c));
    }
    return max(0.0, dist);
}

void main() {
    vec4 prevTimeData, prevParamData;
    float accumulatedTime, currentSpeed, adjustedSpeed, newTime;
    float currentIntensity, currentRadius;
    float effectiveTime, effectiveSpeed, effectiveIntensity, effectiveRadius;

    if (PASSINDEX == 0) {
        // Time and speed buffer
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        accumulatedTime = prevTimeData.r;
        currentSpeed = prevTimeData.g;

        if (FRAMEINDEX == 0) {
            newTime = 0.0;
            adjustedSpeed = 10.0; // Initial buildup
        } else {
            // Smooth speed transition
            adjustedSpeed = mix(currentSpeed, speed, min(1.0, TIMEDELTA * transitionSpeed));
            // Advance time by (smoothed) speed * TIMEDELTA
            newTime = accumulatedTime + adjustedSpeed * TIMEDELTA;
        }
        gl_FragColor = vec4(newTime, adjustedSpeed, 0.0, 1.0);
    }
    else if (PASSINDEX == 1) {
        // Parameter buffer
        prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5, 0.5));
        if (FRAMEINDEX == 0) {
            currentIntensity = intensity;
            currentRadius = radius;
        } else {
            currentIntensity = mix(prevParamData.r, intensity, min(1.0, TIMEDELTA * transitionSpeed));
            currentRadius = mix(prevParamData.g, radius, min(1.0, TIMEDELTA * transitionSpeed));
        }
        gl_FragColor = vec4(currentIntensity, currentRadius, 0.0, 1.0);
    }
    else {
        // Final render pass
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5, 0.5));
        effectiveTime = prevTimeData.r;
        effectiveSpeed = prevTimeData.g;
        effectiveIntensity = prevParamData.r;
        effectiveRadius = prevParamData.g;

        vec2 uv = isf_FragNormCoord.xy;
        float widthHeightRatio = RENDERSIZE.x/RENDERSIZE.y;
        vec2 centre = vec2(0.5, 0.5);
        vec2 pos = centre - uv;
        pos.y /= widthHeightRatio;
        pos.y += 0.03;

        float dist = getSegment(effectiveTime, pos, 0.0, -1.0); // <- FIX: always -1 to move forward
        float glow = getGlow(dist, effectiveRadius, effectiveIntensity);

        vec3 col = vec3(0.0);
        col += 10.0*vec3(smoothstep(0.006, 0.003, dist));
        col += glow * vec3(color1.rgb);

        dist = getSegment(effectiveTime, pos, 3.4, -1.0); // <- FIX: always -1 to move forward
        glow = getGlow(dist, effectiveRadius, effectiveIntensity);
        col += 10.0*vec3(smoothstep(0.006, 0.003, dist));
        col += glow * vec3(color2.rgb);

        col = 1.0 - exp(-col);
        col = pow(col, vec3(0.4545));
        gl_FragColor = vec4(col, 1.0);
    }
}
