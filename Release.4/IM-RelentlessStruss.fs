/*{
  "DESCRIPTION": "Raytraced geometric scene with customizable colors and independent, smoothed animation controls for gates (rings) and streams. Features include adjustable gate rotation, gate animation/scaling, and stream speed.",
  "CREDIT": "Shadertoy Shader @srtuss, 2013 - Converted to ISF 2.0, enhanced by dot2dot",
  "ISFVSN": "2.0",
  "CATEGORIES": [
    "Raytracing",
    "Geometric",
    "Abstract",
    "GENERATOR"
  ],
  "INPUTS": [
    {
      "NAME": "speed",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 50.0,
      "LABEL": "Master Speed"
    },
    {
      "NAME": "transitionSpeed",
      "TYPE": "float",
      "DEFAULT": 2.0,
      "MIN": 0.1,
      "MAX": 10.0,
      "LABEL": "Master Speed Smoothness"
    },
    {
      "NAME": "gateColor",
      "TYPE": "color",
      "DEFAULT": [1.0, 0.7, 0.2, 1.0],
      "LABEL": "Gate Color"
    },
    {
      "NAME": "floorColor",
      "TYPE": "color",
      "DEFAULT": [0.8, 0.2, 0.3, 1.0],
      "LABEL": "Floor Tile Color"
    },
    {
      "NAME": "streamColor",
      "TYPE": "color",
      "DEFAULT": [0.5, 0.8, 1.0, 1.0],
      "LABEL": "Stream Color"
    },
    {
      "NAME": "backgroundColor",
      "TYPE": "color",
      "DEFAULT": [0.1, 0.1, 0.2, 1.0],
      "LABEL": "Background Sky Color"
    },
    {
      "NAME": "gateRotationSpeedFactor",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": -5.0,
      "MAX": 5.0,
      "LABEL": "Gate Rotation Speed Factor"
    },
    {
      "NAME": "gateRotationTransition",
      "TYPE": "float",
      "DEFAULT": 2.0,
      "MIN": 0.1,
      "MAX": 10.0,
      "LABEL": "Gate Rotation Smoothness"
    },
    {
      "NAME": "gateAnimationValue",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": -1.0,
      "MAX": 1.0,
      "LABEL": "Gate Animation (e.g. Scale)"
    },
    {
      "NAME": "gateAnimationTransition",
      "TYPE": "float",
      "DEFAULT": 2.0,
      "MIN": 0.1,
      "MAX": 10.0,
      "LABEL": "Gate Animation Smoothness"
    },
    {
      "NAME": "streamSpeedMultiplier",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 5.0,
      "LABEL": "Stream Speed Multiplier"
    },
    {
      "NAME": "streamSpeedTransition",
      "TYPE": "float",
      "DEFAULT": 2.0,
      "MIN": 0.1,
      "MAX": 10.0,
      "LABEL": "Stream Speed Smoothness"
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
      "TARGET": "gateRotationBuffer",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": 1,
      "HEIGHT": 1
    },
    {
      "TARGET": "gateAnimationBuffer",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": 1,
      "HEIGHT": 1
    },
    {
      "TARGET": "streamSpeedBuffer",
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

////////////////////////////////////////////////////////////////////////////////
// Core Utility Functions
////////////////////////////////////////////////////////////////////////////////
vec2 rotate(vec2 p, float a) {
  return vec2(p.x * cos(a) - p.y * sin(a), p.x * sin(a) + p.y * cos(a));
}
float box(vec2 p, vec2 b, float r) {
  return length(max(abs(p) - b, 0.0)) - r;
}
vec3 intersectPlane(in vec3 o, in vec3 d, vec3 c, vec3 u, vec3 v) {
  vec3 q = o - c;
  float denominator = dot(cross(v, u), d);
  if (abs(denominator) < 0.00001) return vec3(-1.0);
  return vec3(
    dot(cross(u, v), q),
    dot(cross(q, u), d),
    dot(cross(v, q), d)) / denominator;
}
float rand11(float p) {
  return fract(sin(p * 591.32) * 43758.5357);
}
float rand12(vec2 p) {
  return fract(sin(dot(p.xy, vec2(12.9898, 78.233))) * 43758.5357);
}
vec2 rand21(float p) {
  return fract(vec2(sin(p * 591.32), cos(p * 391.32)));
}
vec2 rand22(in vec2 p) {
  return fract(vec2(sin(p.x * 591.32 + p.y * 154.077), cos(p.x * 391.32 + p.y * 49.077)));
}
float noise11(float p) {
  float fl = floor(p);
  return mix(rand11(fl), rand11(fl + 1.0), fract(p));
}
vec3 noise31(float p) {
  return vec3(noise11(p), noise11(p + 18.952), noise11(p - 11.372)) * 2.0 - 1.0;
}
vec3 voronoiPattern(in vec2 x) {
  vec2 n = floor(x);
  vec2 f = fract(x);
  vec2 mg;
  vec2 mr;
  float md = 8.0, md2 = 8.0;
  for (int j = -1; j <= 1; j++) {
    for (int i = -1; i <= 1; i++) {
      vec2 g = vec2(float(i), float(j));
      vec2 o = rand22(n + g);
      vec2 r = g + o - f;
      float d = max(abs(r.x), abs(r.y));
      if (d < md) {
        md2 = md; md = d; mr = r; mg = g;
      } else if (d < md2) {
        md2 = d;
      }
    }
  }
  return vec3(n + mg, md2 - md);
}
#define A2V(a) vec2(sin((a) * 6.28318531 / 100.0), cos((a) * 6.28318531 / 100.0))

////////////////////////////////////////////////////////////////////////////////
// Scene Element SDFs and Shading Functions
////////////////////////////////////////////////////////////////////////////////

float sdfGateDetails(vec2 p, float timeParam, float pRotationSpeedFactor, float pAnimationValue) {
  p *= (1.0 - pAnimationValue * 0.25); 

  float v, w, l, c;
  vec2 pp;
  l = length(p);

  pp = rotate(p, timeParam * 3.0 * pRotationSpeedFactor);
  c = max(dot(pp, normalize(vec2(-0.2, 0.5))), -dot(pp, normalize(vec2(0.2, 0.5))));
  c = min(c, max(dot(pp, normalize(vec2(0.5, -0.5))), -dot(pp, normalize(vec2(0.2, -0.5)))));
  c = min(c, max(dot(pp, normalize(vec2(0.3, 0.5))), -dot(pp, normalize(vec2(0.2, 0.5)))));

  v = abs(l - 0.5) - 0.03;
  v = max(v, -c);
  v = min(v, abs(l - 0.54) - 0.02);
  v = min(v, abs(l - 0.64) - 0.05);

  pp = rotate(p, timeParam * -1.333 * pRotationSpeedFactor);
  c = max(dot(pp, A2V(-5.0)), -dot(pp, A2V(5.0)));
  c = min(c, max(dot(pp, A2V(25.0 - 5.0)), -dot(pp, A2V(25.0 + 5.0))));
  c = min(c, max(dot(pp, A2V(50.0 - 5.0)), -dot(pp, A2V(50.0 + 5.0))));
  c = min(c, max(dot(pp, A2V(75.0 - 5.0)), -dot(pp, A2V(75.0 + 5.0))));

  w = abs(l - 0.83) - 0.09;
  v = min(v, max(w, c));

  return v;
}

float shadeGate(float d) {
  float v = 1.0 - smoothstep(0.0, 0.012, d);
  float g = exp(d * -20.0);
  return v + g * 0.5;
}

////////////////////////////////////////////////////////////////////////////////
// Camera Setup Function
////////////////////////////////////////////////////////////////////////////////
void initializeCamera(vec2 uv, float timeParam, out vec3 rayOrigin, out vec3 rayDirection) {
  vec3 ro = 0.7 * vec3(cos(0.2 * timeParam), 0.0, sin(0.2 * timeParam));
  ro.y = cos(0.6 * timeParam) * 0.3 + 0.65;
  vec3 ta = vec3(0.0, 0.2, 0.0);
  float shake = clamp(3.0 * (1.0 - length(ro.yz)), 0.3, 1.0);
  float st = mod(timeParam, 10.0) * 143.0;
  vec3 ww = normalize(ta - ro + noise31(st) * shake * 0.01);
  vec3 uu = normalize(cross(ww, normalize(vec3(0.0, 1.0, 0.2 * sin(timeParam)))));
  vec3 vv = normalize(cross(uu, ww));
  rayDirection = normalize(uv.x * uu + uv.y * vv + 1.0 * ww);
  ro += noise31(-st) * shake * 0.015;
  ro.x += timeParam * 2.0;
  rayOrigin = ro;
}

////////////////////////////////////////////////////////////////////////////////
// Scene Component Rendering Functions
////////////////////////////////////////////////////////////////////////////////
float getSkyPattern(vec3 p, float timeParam) {
  float a = atan(p.x, p.z);
  float timeOffset = timeParam * 0.1;
  float v = rand11(floor(a * 4.0 + timeOffset)) * 0.5 +
            rand11(floor(a * 8.0 - timeOffset)) * 0.25 +
            rand11(floor(a * 16.0 + timeOffset)) * 0.125;
  return v;
}

float renderBackground(vec3 rd, float timeParam) {
  float intensity = 0.0;
  float skyDot = dot(rd, vec3(0.0, 1.0, 0.0));
  intensity = pow(1.0 - abs(skyDot), 20.0);
  if (rd.y > 0.0) {
    intensity += pow(getSkyPattern(rd, timeParam), 5.0) * 0.2;
  }
  return intensity;
}

float renderVoronoiFloors(vec3 ro, vec3 rd, float timeParam) {
  float totalIntensity = 0.0;
  vec3 its;
  for (int i = 0; i < 4; i++) {
    float layer = float(i);
    its = intersectPlane(ro, rd, vec3(0.0, -5.0 - layer * 5.0, 0.0), vec3(1.0, 0.0, 0.0), vec3(0.0, 0.0, 1.0));
    if (its.x > 0.0) {
      vec3 vo = voronoiPattern((its.yz) * 0.05 + 8.0 * rand21(layer));
      float v = exp(-100.0 * (vo.z - 0.02));
      float fx = 0.0;
      if (i == 3) {
        float fxi = cos(vo.x * 0.2 + timeParam * 1.5);
        fx = clamp(smoothstep(0.9, 1.0, fxi), 0.0, 0.9) * 1.0 * rand12(vo.xy);
        fx *= exp(-3.0 * vo.z) * 2.0;
      }
      totalIntensity += v * 0.1 + fx;
    }
  }
  return totalIntensity;
}

float renderGates(vec3 ro, vec3 rd, float timeParam, float pRotationSpeedFactor, float pAnimationValue) {
  float totalIntensity = 0.0;
  vec3 its;
  float gateBaseX = floor(ro.x / 8.0 + 0.5) * 8.0 + 4.0;
  float gateOffset = -16.0;
  for (int i = 0; i < 4; i++) {
    its = intersectPlane(ro, rd, vec3(gateBaseX + gateOffset, 0.0, 0.0), vec3(0.0, 1.0, 0.0), vec3(0.0, 0.0, 1.0));
    if (dot(its.yz, its.yz) < 4.0 && its.x > 0.0) {
      float v = sdfGateDetails(its.yz, timeParam, pRotationSpeedFactor, pAnimationValue);
      totalIntensity += shadeGate(v);
    }
    gateOffset += 8.0;
  }
  return totalIntensity;
}

float renderStreams(vec3 ro, vec3 rd, float timeParam, float pStreamSpeedMultiplier) {
  float totalIntensity = 0.0;
  vec3 its;
  for (int j = 0; j < 20; j++) {
    float id = float(j);
    vec3 streamPlaneOrigin = vec3(0.0, (rand11(id) * 2.0 - 1.0) * 0.25, 0.0);
    its = intersectPlane(ro, rd, streamPlaneOrigin, vec3(1.0, 0.0, 0.0), vec3(0.0, 0.0, 1.0));
    if (its.x > 0.0) {
      vec2 particlePos = its.yz;
      float baseParticleSpeed = (1.0 + rand11(id) * 3.0) * 2.5;
      particlePos.y += timeParam * baseParticleSpeed * pStreamSpeedMultiplier;
      particlePos += (rand21(id) * 2.0 - 1.0) * vec2(0.3, 1.0);
      float repeatLength = rand11(id) + 1.5;
      particlePos.y = mod(particlePos.y, repeatLength * 2.0) - repeatLength;
      float distToBox = box(particlePos, vec2(0.02, 0.3), 0.1);
      float v_shade = 1.0 - smoothstep(0.0, 0.03, abs(distToBox) - 0.001);
      float g_shade = min(exp(distToBox * -20.0), 2.0);
      totalIntensity += (v_shade + g_shade * 0.7) * 0.5;
    }
  }
  return totalIntensity;
}

////////////////////////////////////////////////////////////////////////////////
// Main Function
////////////////////////////////////////////////////////////////////////////////
void main() {
  if (PASSINDEX == 0) { // Master Time and Speed
    vec4 prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
    float accumulatedTime = prevTimeData.r;
    float currentMasterSpeed = prevTimeData.g;
    float newTimeVal;
    float adjustedMasterSpeed;
    if (FRAMEINDEX == 0) {
      newTimeVal = 0.0;
      adjustedMasterSpeed = speed;
    } else {
      adjustedMasterSpeed = mix(currentMasterSpeed, speed, min(1.0, TIMEDELTA * transitionSpeed));
      newTimeVal = accumulatedTime + adjustedMasterSpeed * TIMEDELTA;
    }
    gl_FragColor = vec4(newTimeVal, adjustedMasterSpeed, 0.0, 1.0);
  } 
  else if (PASSINDEX == 1) { // Gate Rotation Speed Factor
    vec4 prevData = IMG_NORM_PIXEL(gateRotationBuffer, vec2(0.5, 0.5));
    float currentValue = prevData.r;
    float smoothedValue;
    if (FRAMEINDEX == 0) {
      smoothedValue = gateRotationSpeedFactor;
    } else {
      smoothedValue = mix(currentValue, gateRotationSpeedFactor, min(1.0, TIMEDELTA * gateRotationTransition));
    }
    gl_FragColor = vec4(smoothedValue, 0.0, 0.0, 1.0);
  }
  else if (PASSINDEX == 2) { // Gate Animation Value
    vec4 prevData = IMG_NORM_PIXEL(gateAnimationBuffer, vec2(0.5, 0.5));
    float currentValue = prevData.r;
    float smoothedValue;
    if (FRAMEINDEX == 0) {
      smoothedValue = gateAnimationValue;
    } else {
      smoothedValue = mix(currentValue, gateAnimationValue, min(1.0, TIMEDELTA * gateAnimationTransition));
    }
    gl_FragColor = vec4(smoothedValue, 0.0, 0.0, 1.0);
  }
  else if (PASSINDEX == 3) { // Stream Speed Multiplier
    vec4 prevData = IMG_NORM_PIXEL(streamSpeedBuffer, vec2(0.5, 0.5));
    float currentValue = prevData.r;
    float smoothedValue;
    if (FRAMEINDEX == 0) {
      smoothedValue = streamSpeedMultiplier;
    } else {
      smoothedValue = mix(currentValue, streamSpeedMultiplier, min(1.0, TIMEDELTA * streamSpeedTransition));
    }
    gl_FragColor = vec4(smoothedValue, 0.0, 0.0, 1.0);
  }
  else { // PASSINDEX == 4 (Final Render Pass)
    vec4 timeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
    float effectiveTime = timeData.r;

    vec4 gateRotData = IMG_NORM_PIXEL(gateRotationBuffer, vec2(0.5, 0.5));
    float effectiveGateRotationSpeed = gateRotData.r;

    vec4 gateAnimData = IMG_NORM_PIXEL(gateAnimationBuffer, vec2(0.5, 0.5));
    float effectiveGateAnimationValue = gateAnimData.r;

    vec4 streamSpeedData = IMG_NORM_PIXEL(streamSpeedBuffer, vec2(0.5, 0.5));
    float effectiveStreamSpeedMultiplier = streamSpeedData.r;

    vec2 uv = isf_FragNormCoord;
    uv = uv * 2.0 - 1.0;
    uv.x *= RENDERSIZE.x / RENDERSIZE.y;

    vec3 rayOrigin, rayDirection;
    initializeCamera(uv, effectiveTime, rayOrigin, rayDirection);

    vec3 finalColorAccumulator = vec3(0.0);

    float bgIntensity = renderBackground(rayDirection, effectiveTime);
    finalColorAccumulator += bgIntensity * backgroundColor.rgb;

    float floorIntensity = renderVoronoiFloors(rayOrigin, rayDirection, effectiveTime);
    finalColorAccumulator += floorIntensity * floorColor.rgb;
    
    float gateIntensity = renderGates(rayOrigin, rayDirection, effectiveTime, effectiveGateRotationSpeed, effectiveGateAnimationValue);
    finalColorAccumulator += gateIntensity * gateColor.rgb;

    float streamIntensity = renderStreams(rayOrigin, rayDirection, effectiveTime, effectiveStreamSpeedMultiplier);
    finalColorAccumulator += streamIntensity * streamColor.rgb;

    finalColorAccumulator *= (0.4 + (sin(effectiveTime * 0.5) * 0.5 + 0.5) * 0.6); 

    gl_FragColor = vec4(finalColorAccumulator, 1.0);
  }
}