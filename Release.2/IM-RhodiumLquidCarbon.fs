/*{
  "DESCRIPTION": "Alcatraz / Rhodium liquid carbon effect by Virgill, converted to ISF with parameter smoothing and DoF. Original ShaderToy: https://www.youtube.com/watch?v=YK7fbtQw3ZU (Related to pouet.net/prod.php?which=68239)",
  "CREDIT": "Original ShaderToy by Jochen 'Virgill' Feldkötter (https://www.shadertoy.com/view/llK3Dy). ISF Version by @dot2dot.",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR", "3D"],
  "INPUTS": [
    { "NAME": "overallSpeed", "TYPE": "float", "DEFAULT": 0.2, "MIN": 0.0, "MAX": 2.0, "LABEL": "Overall Speed"},
    { "NAME": "bounceFactor", "TYPE": "float", "DEFAULT": 20.0, "MIN": 0.0, "MAX": 50.0, "LABEL": "Bounce Factor"},
    { "NAME": "bounceTimeScale", "TYPE": "float", "DEFAULT": 0.05, "MIN": 0.01, "MAX": 0.5, "LABEL": "Bounce Time Scale"},
    { "NAME": "wobbleIntensity", "TYPE": "float", "DEFAULT": 0.1, "MIN": 0.0, "MAX": 0.5, "LABEL": "Wobble Intensity"},
    { "NAME": "wobbleTimeScale", "TYPE": "float", "DEFAULT": 0.1, "MIN": 0.01, "MAX": 0.5, "LABEL": "Wobble Time Scale"},
    { "NAME": "wobbleSinFrequency", "TYPE": "float", "DEFAULT": 30.0, "MIN": 1.0, "MAX": 100.0, "LABEL": "Wobble Sine Frequency"},
    { "NAME": "glowAnimSpeed", "TYPE": "float", "DEFAULT": 0.209, "MIN": 0.0, "MAX": 1.0, "LABEL": "Glow Animation Speed"},
    { "NAME": "glowBaseLevel", "TYPE": "float", "DEFAULT": 0.05, "MIN": 0.0, "MAX": 0.2, "LABEL": "Glow Base Level"},
    { "NAME": "glowSinAmplitude", "TYPE": "float", "DEFAULT": 0.1, "MIN": 0.0, "MAX": 0.5, "LABEL": "Glow Sine Amplitude"},
    { "NAME": "dofStrength", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 5.0, "LABEL": "Depth of Field Strength"},
    { "NAME": "transitionSpeed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.1, "MAX": 10.0, "LABEL": "Parameter Transition Smoothness" }
  ],
  "PASSES": [
    { "TARGET": "timeSpeedBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "paramsBufferA", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "paramsBufferB", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "paramsBufferC", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "sceneBuffer", "PERSISTENT": false, "FLOAT": true },
    { "TARGET": "finalOutput" }
  ]
}*/

precision highp float;

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

// --- Global smoothed parameters (populated in respective passes) ---
// For Pass 4 (Scene Rendering)
float currentTime_pass4;
float currentBounceFactor_pass4;
float currentBounceTimeScale_pass4;
float currentWobbleIntensity_pass4;
float currentWobbleTimeScale_pass4;
float currentWobbleSinFrequency_pass4;
float currentGlowAnimSpeed_pass4;
float currentGlowBaseLevel_pass4;
float currentGlowSinAmplitude_pass4;
float bounce_val_pass4; // Derived from above

// For Pass 5 (DoF)
float currentDofStrength_pass5;


// --- ShaderToy helper functions (prefixed with st_) ---

// Signed box distance function
float st_sdBox(vec3 p, vec3 b) {
  vec3 d = abs(p) - b;
  return min(max(d.x, max(d.y, d.z)), 0.0) + length(max(d, 0.0));
}

// 2D rotation
void st_pR(inout vec2 p, float a) {
  p = cos(a) * p + sin(a) * vec2(p.y, -p.x);
}

// 3D noise function (IQ's)
float st_noise(vec3 p) {
  vec3 ip = floor(p);
  p -= ip;
  vec3 s = vec3(7, 157, 113);
  vec4 h = vec4(0., s.yz, s.y + s.z) + dot(ip, s);
  p = p * p * (3. - 2. * p);
  h = mix(fract(sin(h) * 43758.5), fract(sin(h + s.x) * 43758.5), p.x);
  h.xy = mix(h.xz, h.yw, p.y);
  return mix(h.x, h.y, p.z);
}

// --- Main scene mapping and raymarching (BufferA logic) ---

float st_map(vec3 p) {
  // Uses globals: bounce_val_pass4, currentTime_pass4
  p.z -= 1.0;
  p *= 0.9;
  st_pR(p.yz, bounce_val_pass4 * 1.0 + 0.4 * p.x);
  return st_sdBox(p + vec3(0, sin(1.6 * currentTime_pass4), 0), vec3(20.0, 0.05, 1.2)) - 0.4 * st_noise(8. * p + 3. * bounce_val_pass4);
}

vec3 st_calcNormal(vec3 pos) {
  // Uses st_map, which uses globals
  float eps = 0.0001;
  float d = st_map(pos);
  return normalize(vec3(
      st_map(pos + vec3(eps, 0, 0)) - d,
      st_map(pos + vec3(0, eps, 0)) - d,
      st_map(pos + vec3(0, 0, eps)) - d
  ));
}

float st_castRayx(vec3 ro, vec3 rd) {
  // Uses st_map
  float function_sign = (st_map(ro) < 0.0) ? -1.0 : 1.0;
  float precis = .0001;
  float h = precis * 2.0;
  float t = 0.0;
  for (int i = 0; i < 120; i++) {
    if (abs(h) < precis || t > 12.0) break;
    h = function_sign * st_map(ro + rd * t);
    t += h;
  }
  return t;
}

float st_refr(vec3 pos, vec3 lig, vec3 dir, vec3 nor, float angle, out float t2, out vec3 nor2) {
  // Uses st_map, st_calcNormal
  float h = 0.0; // Initialize h
  t2 = 2.0; // Initialize t2
  vec3 dir2 = refract(dir, nor, angle);
  for (int i = 0; i < 50; i++) {
    // Before 'abs(h)>3.', h needs a value. It gets it from st_map.
    h = st_map(pos + dir2 * t2);
    if (abs(h) > 3.0 && i > 0) break; // Check h after it's computed, and not on first iteration if t2 is large
    if (abs(h) < 0.0001 && t2 > 0.01) break; 
    t2 -= h; // Iterate t2 based on map distance
    if (t2 > 10.0 || t2 < -10.0) break; // Prevent excessive t2
  }
  nor2 = st_calcNormal(pos + dir2 * t2);
  return (0.5 * clamp(dot(-lig, nor2), 0.0, 1.0) + pow(max(dot(reflect(dir2, nor2), lig), 0.0), 8.0));
}

float st_softshadow(vec3 ro, vec3 rd) {
  // Uses st_map
  float sh = 1.0;
  float t = .02;
  float h = .0;
  for (int i = 0; i < 22; i++) {
    if (t > 20.0) break; // Changed from continue to break for clarity
    h = st_map(ro + rd * t);
    sh = min(sh, 4. * h / t);
    t += h;
    if (h < 0.001 && t < 20.0) t+=0.02; // Nudge if stuck
  }
  return clamp(sh,0.0,1.0); // Ensure shadow is clamped
}

void st_bufferA_mainImage(out vec4 fragColor, vec2 fragCoord_normalized_uv) {
  // Uses globals: currentTime_pass4, bounce_val_pass4, currentWobble*, currentGlow*
  vec2 uv = fragCoord_normalized_uv;

  float wobble_time_factor = currentTime_pass4 - 1.0;
  float wobble = (fract(currentWobbleTimeScale_pass4 * wobble_time_factor) >= 0.9) ?
                 fract(-currentTime_pass4) * currentWobbleIntensity_pass4 * sin(currentWobbleSinFrequency_pass4 * currentTime_pass4) : 0.0;

  vec2 screen_ndc = (uv * 2.0 - 1.0);
  vec3 dir = normalize(vec3(screen_ndc.x * RENDERSIZE.x, screen_ndc.y * RENDERSIZE.y, RENDERSIZE.y));
  vec3 org = vec3(0, 2.0 * wobble, -3.0);

  vec3 color = vec3(0.0);
  vec3 color2 = vec3(0.0);
  float t = st_castRayx(org, dir);
  vec3 pos = org + dir * t;
  vec3 nor = st_calcNormal(pos);

  vec3 lig = normalize(vec3(.2, 6., .5));
  float depth = clamp((1.0 - 0.09 * t), 0.0, 1.0);

  vec3 refr_nor2 = vec3(0.0); // Initialize to avoid uninitialized use
  float t2_refr = 0.0;      // Initialize

  if (t < 12.0) {
    color2 = vec3(max(dot(lig, nor), 0.0) + pow(max(dot(reflect(dir, nor), lig), 0.0), 16.0));
    color2 *= clamp(st_softshadow(pos, lig), 0.0, 1.0);
    color2.rgb += st_refr(pos, lig, dir, nor, 0.9, t2_refr, refr_nor2) * depth;
    color2 -= clamp(0.1 * t2_refr, 0.0, 1.0);
  }

  float tmp_glow = 0.0;
  float T_glow = 1.0;
  float intensity_glow = currentGlowSinAmplitude_pass4 * -sin(currentGlowAnimSpeed_pass4 * currentTime_pass4 + 1.0) + currentGlowBaseLevel_pass4;
  
  vec3 org_glow_loop = org;
  vec3 normal_for_density_calc = (t < 12.0 && length(refr_nor2) > 0.1) ? refr_nor2 : nor;
  if (length(normal_for_density_calc) < 0.1) normal_for_density_calc = vec3(0,0,1); // Fallback if initial normal is zero

  for (int i = 0; i < 128; i++) {
    float nebula = st_noise(org_glow_loop + bounce_val_pass4); // Use bounce_val_pass4
    float density = intensity_glow - st_map(org_glow_loop + 0.5 * normal_for_density_calc) * nebula; // st_map uses globals implicitly

    if (density > 0.0) {
      tmp_glow = density / 128.0;
      T_glow *= 1.0 - tmp_glow * 100.0;
      if (T_glow <= 0.0) break;
    }
    org_glow_loop += dir * 0.078;
  }
  vec3 basecol = vec3(1.0, 1.0 / 4.0, 1.0 / 16.0);
  T_glow = clamp(T_glow, 0.0, 1.5);
  color += basecol * exp(4.0 * (0.5 - T_glow) - 0.8);
  color2 *= depth;
  color2 += (1.0 - depth) * st_noise(6.0 * dir + 0.3 * currentTime_pass4) * 0.1;

  fragColor = vec4(vec3(1.0 * color + 0.8 * color2) * 1.3, abs(0.67 - depth) * 2.0 + 4.0 * wobble);
}


// --- Depth of Field (Image shader logic) ---
const float st_GA = 2.399;
const mat2 st_rot = mat2(cos(st_GA), sin(st_GA), -sin(st_GA), cos(st_GA));
const int DOF_SAMPLES = 80; // Kept original sample count

vec3 st_dof(sampler2D tex, vec2 uv, float alpha_depth_info) {
  // Uses global: currentDofStrength_pass5
  vec3 acc = vec3(0.0);
  // Pixel offset scales with resolution and DoF strength
  vec2 pixel_offset_base = vec2(0.002 * RENDERSIZE.y / RENDERSIZE.x, 0.002); // Original base factor
  vec2 pixel = pixel_offset_base; // Not scaling pixel by dofStrength, instead scaling blur_radius directly
                                  
  float blur_radius = alpha_depth_info * currentDofStrength_pass5;
  vec2 angle = vec2(0.0, blur_radius); // blur_radius comes from alpha, scaled by DoF strength
  float rad_spiral = 1.0;

  for (int j = 0; j < DOF_SAMPLES; j++) {
    rad_spiral += 1.0 / rad_spiral; // Spiral outwards
    angle *= st_rot; // Rotate sample vector
    vec2 sample_uv = uv + pixel * (rad_spiral - 1.0) * angle;
    vec4 col = texture(tex, clamp(sample_uv, 0.0, 1.0));
    acc += col.rgb;
  }
  return acc / float(DOF_SAMPLES);
}

void st_image_mainImage(out vec4 fragColor, vec2 fragCoord_normalized_uv, sampler2D channel0_sampler) {
  vec2 uv = fragCoord_normalized_uv;
  float scene_alpha = texture(channel0_sampler, uv).w; // Depth info from sceneBuffer's alpha
  vec3 dof_result = st_dof(channel0_sampler, uv, scene_alpha);
  fragColor = vec4(dof_result, 1.0);
}


// --- ISF Main Function ---
void main() {
  // Pass 0: Time and overall speed smoothing
  if (PASSINDEX == 0) {
    vec4 prevData = IMG_NORM_PIXEL(timeSpeedBuffer, vec2(0.5));
    float smoothedOverallSpeed = (FRAMEINDEX == 0) ? overallSpeed : mix(prevData.g, overallSpeed, min(1.0, TIMEDELTA * transitionSpeed));
    float accumulatedTime = (FRAMEINDEX == 0) ? 0.0 : prevData.r + smoothedOverallSpeed * TIMEDELTA;
    gl_FragColor = vec4(accumulatedTime, smoothedOverallSpeed, 0.0, 1.0);
    return;
  }
  // Pass 1: Parameter Set A smoothing (bounceFactor, bounceTimeScale, wobbleIntensity)
  if (PASSINDEX == 1) {
    vec4 prevData = IMG_NORM_PIXEL(paramsBufferA, vec2(0.5));
    float smBounceFactor = (FRAMEINDEX == 0) ? bounceFactor : mix(prevData.r, bounceFactor, min(1.0, TIMEDELTA * transitionSpeed));
    float smBounceTimeScale = (FRAMEINDEX == 0) ? bounceTimeScale : mix(prevData.g, bounceTimeScale, min(1.0, TIMEDELTA * transitionSpeed));
    float smWobbleIntensity = (FRAMEINDEX == 0) ? wobbleIntensity : mix(prevData.b, wobbleIntensity, min(1.0, TIMEDELTA * transitionSpeed));
    gl_FragColor = vec4(smBounceFactor, smBounceTimeScale, smWobbleIntensity, 1.0);
    return;
  }
  // Pass 2: Parameter Set B smoothing (wobbleTimeScale, wobbleSinFrequency, glowAnimSpeed)
  if (PASSINDEX == 2) {
    vec4 prevData = IMG_NORM_PIXEL(paramsBufferB, vec2(0.5));
    float smWobbleTimeScale = (FRAMEINDEX == 0) ? wobbleTimeScale : mix(prevData.r, wobbleTimeScale, min(1.0, TIMEDELTA * transitionSpeed));
    float smWobbleSinFrequency = (FRAMEINDEX == 0) ? wobbleSinFrequency : mix(prevData.g, wobbleSinFrequency, min(1.0, TIMEDELTA * transitionSpeed));
    float smGlowAnimSpeed = (FRAMEINDEX == 0) ? glowAnimSpeed : mix(prevData.b, glowAnimSpeed, min(1.0, TIMEDELTA * transitionSpeed));
    gl_FragColor = vec4(smWobbleTimeScale, smWobbleSinFrequency, smGlowAnimSpeed, 1.0);
    return;
  }
  // Pass 3: Parameter Set C smoothing (glowBaseLevel, glowSinAmplitude, dofStrength)
  if (PASSINDEX == 3) {
    vec4 prevData = IMG_NORM_PIXEL(paramsBufferC, vec2(0.5));
    float smGlowBaseLevel = (FRAMEINDEX == 0) ? glowBaseLevel : mix(prevData.r, glowBaseLevel, min(1.0, TIMEDELTA * transitionSpeed));
    float smGlowSinAmplitude = (FRAMEINDEX == 0) ? glowSinAmplitude : mix(prevData.g, glowSinAmplitude, min(1.0, TIMEDELTA * transitionSpeed));
    float smDofStrength = (FRAMEINDEX == 0) ? dofStrength : mix(prevData.b, dofStrength, min(1.0, TIMEDELTA * transitionSpeed));
    gl_FragColor = vec4(smGlowBaseLevel, smGlowSinAmplitude, smDofStrength, 1.0);
    return;
  }

  // Pass 4: Render 3D Scene (BufferA equivalent) to sceneBuffer
  if (PASSINDEX == 4) {
    vec4 timeSpeedData = IMG_NORM_PIXEL(timeSpeedBuffer, vec2(0.5));
    currentTime_pass4 = timeSpeedData.r;

    vec4 paramsAData = IMG_NORM_PIXEL(paramsBufferA, vec2(0.5));
    currentBounceFactor_pass4 = paramsAData.r;
    currentBounceTimeScale_pass4 = paramsAData.g;
    currentWobbleIntensity_pass4 = paramsAData.b;

    vec4 paramsBData = IMG_NORM_PIXEL(paramsBufferB, vec2(0.5));
    currentWobbleTimeScale_pass4 = paramsBData.r;
    currentWobbleSinFrequency_pass4 = paramsBData.g;
    currentGlowAnimSpeed_pass4 = paramsBData.b;
    
    vec4 paramsCData = IMG_NORM_PIXEL(paramsBufferC, vec2(0.5)); // For glow params from this buffer
    currentGlowBaseLevel_pass4 = paramsCData.r;
    currentGlowSinAmplitude_pass4 = paramsCData.g;
    // currentDofStrength is not used in this pass

    bounce_val_pass4 = abs(fract(currentBounceTimeScale_pass4 * currentTime_pass4) - 0.5) * currentBounceFactor_pass4;

    st_bufferA_mainImage(gl_FragColor, isf_FragNormCoord);
    return;
  }

  // Pass 5: Apply Depth of Field (Image shader equivalent) to finalOutput
  if (PASSINDEX == 5) {
    vec4 paramsCData = IMG_NORM_PIXEL(paramsBufferC, vec2(0.5)); // For DoF strength
    currentDofStrength_pass5 = paramsCData.b; // DoF strength is the .b component here

    st_image_mainImage(gl_FragColor, isf_FragNormCoord, sceneBuffer);
    return;
  }
}
