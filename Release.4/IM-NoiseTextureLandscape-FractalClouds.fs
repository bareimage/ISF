/*{
  "DESCRIPTION": "An optimized raymarched terrain with clouds, featuring consolidated buffers, seed control, and smooth startup. The original shader relied on external chanell for noise. I rebuild this shader to have internal seeded noise funtion. I also changed things up to have more fun psyhodelic sky",
  "CREDIT": "Based on Shadertoy by @ztri (https://www.shadertoy.com/view/Msf3zX), ISF 2.0 Version @dot2dot.",
  "ISFVSN": "2.0",
  "CATEGORIES": [
    "Generator"
  ],
  "INPUTS": [
    {
      "NAME": "speed",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 50.0
    },
    {
      "NAME": "transitionSpeed",
      "TYPE": "float",
      "DEFAULT": 2.0,
      "MIN": 0.1,
      "MAX": 10.0
    },
    {
      "NAME": "mouse_control",
      "TYPE": "point2D",
      "DEFAULT": [0.5, 0.5],
      "MIN": [0.0, 0.0],
      "MAX": [1.0, 1.0],
      "LABEL": "Mouse Control"
    },
    {
      "NAME": "noiseSeed",
      "TYPE": "float",
      "DEFAULT": 43758.5453123,
      "MIN": 43758.5453123,
      "MAX": 443758.5453123,
      "LABEL": "Base Noise Seed"
    },
    {
      "NAME": "elevationAmplitude",
      "TYPE": "float",
      "DEFAULT": 1200.0,
      "MIN": 200.0,
      "MAX": 3000.0
    },
    {
      "NAME": "terrainDetailFactor",
      "TYPE": "float",
      "DEFAULT": 40.0,
      "MIN": 0.0,
      "MAX": 150.0
    },
    {
      "NAME": "cloudDensity",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 5.0
    },
    {
      "NAME": "cloudAltitudeOffset",
      "TYPE": "float",
      "DEFAULT": 100.0,
      "MIN": -200.0,
      "MAX": 800.0
    },
    {
      "NAME": "groundColorBase",
      "TYPE": "color",
      "DEFAULT": [0.30, 0.25, 0.20, 1.0]
    },
    {
      "NAME": "cloudColorBase",
      "TYPE": "color",
      "DEFAULT": [0.85, 0.88, 0.90, 1.0]
    },
    {
      "NAME": "skyColorBase",
      "TYPE": "color",
      "DEFAULT": [0.4, 0.6, 0.85, 1.0]
    }
  ],
  "PASSES": [
    {
      "TARGET": "paramBufferA",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": 1,
      "HEIGHT": 1
    },
    {
      "TARGET": "paramBufferB",
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

precision mediump float; // Use lower precision for performance

// Simplified hash function using the noiseSeed uniform
float hash_2d_to_1d(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * noiseSeed);
}

// Consistent 2D texture pattern
float consistent_texture_noise(vec2 uv_tex) {
  vec2 scaled_uv = uv_tex * 100.0;
  vec2 grid = floor(scaled_uv);
  vec2 frac_uv = fract(scaled_uv);
  float a_val = hash_2d_to_1d(grid);
  float b_val = hash_2d_to_1d(grid + vec2(1.0, 0.0));
  float c_val = hash_2d_to_1d(grid + vec2(0.0, 1.0));
  float d_val = hash_2d_to_1d(grid + vec2(1.0, 1.0));
  float mix_x1 = mix(a_val, b_val, frac_uv.x);
  float mix_x2 = mix(c_val, d_val, frac_uv.x);
  return mix(mix_x1, mix_x2, frac_uv.y);
}

vec3 rotate(vec3 r, float v){
  return vec3(r.x*cos(v)+r.z*sin(v), r.y, r.z*cos(v)-r.x*sin(v));
}

// Base noise function
float noise(in vec3 x) {
  float z = x.z * 64.0;
  vec2 offz = vec2(0.317, 0.123);
  vec2 uv1_noise = x.xy + offz * floor(z);
  vec2 uv2_noise = uv1_noise + offz;
  return mix(consistent_texture_noise(uv1_noise), consistent_texture_noise(uv2_noise), fract(z)) - 0.5;
}

// Optimized Fractal noise (3 octaves)
float noises(in vec3 p_noise) {
  float a_noise = 0.0;
  vec3 current_p = p_noise;
  for(float i_noise = 1.0; i_noise < 4.0; i_noise++) { // Reduced from 6.0
    a_noise += noise(current_p) / i_noise;
    current_p = current_p * 2.0 + vec3(0.0, a_noise * 0.001 / i_noise, a_noise * 0.0001 / i_noise);
  }
  return a_noise;
}

// --- Core Logic Functions ---
float base(in vec3 p_base, float current_elevation_amplitude) {
  return noise(p_base * 0.00002) * current_elevation_amplitude;
}

float ground(in vec3 p_ground, float current_elevation_amplitude, float current_terrain_detail_factor) {
  float base_val = base(p_ground, current_elevation_amplitude);
  return base_val + noises(p_ground.zxy * 0.00005 + 10.0) * current_terrain_detail_factor * (0.0 - p_ground.y * 0.01) + p_ground.y;
}

float clouds(in vec3 p_clouds,
           float local_time_param,
           vec2 current_mouse_param,
           float current_elevation_amplitude,
           float current_cloud_density,
           float current_cloud_altitude_offset) {
  float b_val_for_clouds = base(p_clouds, current_elevation_amplitude);
  vec3 p_clouds_mod = p_clouds;
  p_clouds_mod.y += b_val_for_clouds * 0.5 / abs(p_clouds_mod.y) + current_cloud_altitude_offset;
  float noise_val = noises(vec3(p_clouds_mod.x * 0.3 + ((local_time_param + current_mouse_param.y * RENDERSIZE.y) * 30.0), p_clouds_mod.y, p_clouds_mod.z) * 0.00002);
  return noise_val * current_cloud_density - max(p_clouds_mod.y, 0.0) * 0.00009;
}


void main() {
  // --- PASS 0: Packed Time and Mouse Buffer ---
  if (PASSINDEX == 0) {
    float newTime, adjustedSpeed;
    vec2 currentSmoothedMouse;

    if (FRAMEINDEX == 0) {
      // Initialize on the first frame to prevent sudden jumps
      newTime = 0.0;
      adjustedSpeed = speed;
      currentSmoothedMouse = mouse_control;
    } else {
      // Read previous frame's data and smooth towards new values
      vec4 prevData = IMG_PIXEL(paramBufferA, ivec2(0));
      float prevTime = prevData.r;
      float prevSpeed = prevData.g;
      vec2 prevMouse = prevData.ba;

      adjustedSpeed = mix(prevSpeed, speed, min(1.0, TIMEDELTA * transitionSpeed));
      newTime = prevTime + adjustedSpeed * TIMEDELTA;
      currentSmoothedMouse = mix(prevMouse, mouse_control, min(1.0, TIMEDELTA * transitionSpeed));
    }
    
    // Pack into a single vec4
    gl_FragColor = vec4(newTime, adjustedSpeed, currentSmoothedMouse.x, currentSmoothedMouse.y);
  }
  // --- PASS 1: Packed Terrain and Cloud Parameters Buffer ---
  else if (PASSINDEX == 1) {
    float smoothedElevation, smoothedDetail, smoothedDensity, smoothedAltitude;

    if (FRAMEINDEX == 0) {
      // Initialize on the first frame
      smoothedElevation = elevationAmplitude;
      smoothedDetail = terrainDetailFactor;
      smoothedDensity = cloudDensity;
      smoothedAltitude = cloudAltitudeOffset;
    } else {
      // Read previous frame's data and smooth towards new values
      vec4 prevData = IMG_PIXEL(paramBufferB, ivec2(0));
      smoothedElevation = mix(prevData.r, elevationAmplitude, min(1.0, TIMEDELTA * transitionSpeed));
      smoothedDetail = mix(prevData.g, terrainDetailFactor, min(1.0, TIMEDELTA * transitionSpeed));
      smoothedDensity = mix(prevData.b, cloudDensity, min(1.0, TIMEDELTA * transitionSpeed));
      smoothedAltitude = mix(prevData.a, cloudAltitudeOffset, min(1.0, TIMEDELTA * transitionSpeed));
    }
    
    // Pack into a single vec4
    gl_FragColor = vec4(smoothedElevation, smoothedDetail, smoothedDensity, smoothedAltitude);
  }
  // --- PASS 2: Final Render ---
  else {
    // Unpack data from buffers
    vec4 bufferA = IMG_NORM_PIXEL(paramBufferA, vec2(0.5));
    vec4 bufferB = IMG_NORM_PIXEL(paramBufferB, vec2(0.5));

    float effectiveTime = bufferA.r;
    vec2 effectiveMouseControl = bufferA.ba;
    float effectiveElevationAmplitude = bufferB.r;
    float effectiveTerrainDetailFactor = bufferB.g;
    float effectiveCloudDensity = bufferB.b;
    float effectiveCloudAltitudeOffset = bufferB.a;

    // Pre-calculate base colors
    vec3 baseGroundColor = groundColorBase.rgb;
    vec3 baseCloudColor = cloudColorBase.rgb;
    vec3 baseSkyColor = skyColorBase.rgb;

    float local_time_var = effectiveTime * 5.0 + floor(effectiveTime * 0.1) * 150.0;
    vec2 uv_frag = (isf_FragNormCoord.xy * 2.0 - 1.0) * vec2(RENDERSIZE.x / RENDERSIZE.y, 1.0);

    vec3 initial_campos_for_base = vec3(30.0, 500.0, local_time_var * 8.0);
    vec3 campos = vec3(initial_campos_for_base.x, 500.0 - base(initial_campos_for_base, effectiveElevationAmplitude), initial_campos_for_base.z);

    vec3 ray = rotate(
      normalize(vec3(uv_frag.x, uv_frag.y - sin(local_time_var * 0.05) * 0.2 - 0.1, 1.0)),
      local_time_var * 0.01 + effectiveMouseControl.x * 2.0 * 3.14159
    );

    vec3 pos = campos + ray;
    vec3 sun = vec3(0.0, 0.6, -0.4);
    float test_val = 0.0;
    float fog_val = 0.0;
    float dist_val = 0.0;
    vec3 p1_march = pos;

    // Optimized Raymarching Loop
    for(float i_march = 1.0; i_march < 25.0; i_march++) { // Reduced iterations
      // LOD Calculation
      float lod = min(dist_val / 10000.0, 1.0);
      float detailFactor = mix(1.0, 0.3, lod); // Reduce detail with distance
      
      test_val = ground(p1_march, effectiveElevationAmplitude, effectiveTerrainDetailFactor * detailFactor);
      
      fog_val += max(test_val * clouds(p1_march, local_time_var, effectiveMouseControl, effectiveElevationAmplitude, effectiveCloudDensity, effectiveCloudAltitudeOffset), fog_val * 0.02);
      
      // Adaptive step sizing
      float stepSize = max(abs(test_val) * 0.5, 1.0);
      p1_march += ray * stepSize;
      dist_val += stepSize;
      
      // More aggressive early termination
      if(abs(test_val) < 5.0 || dist_val > 20000.0 || fog_val > 1000.0) break;
    }

    float l_light = sin(dot(ray, sun));
    vec3 light_eff = vec3(l_light, 0.0, -l_light) + ray.y * 0.2;

    float amb_eff = smoothstep(-100.0, 100.0, ground(p1_march + vec3(0.0, 30.0, 0.0) + sun * 10.0, effectiveElevationAmplitude, effectiveTerrainDetailFactor)) -
                      smoothstep(1000.0, -0.0, p1_march.y) * 0.7;
    
    vec3 ground_color_val = baseGroundColor + sin(p1_march * 0.001) * 0.01 +
                          noise(vec3(p1_march * 0.002)) * 0.1 + amb_eff * 0.7 + light_eff * 0.01;

    float f_fog_mix = smoothstep(0.0, 800.0, fog_val);
    vec3 cloud_color_val = baseCloudColor + light_eff * 0.05 + sin(fog_val * 0.0002) * 0.2 + noise(p1_march) * 0.05;

    float h_dist_mix = smoothstep(10000.0, 40000.0, dist_val);
    vec3 sky_color_val = baseSkyColor + light_eff * 0.02 + ray.y * 0.1 - 0.02;

    gl_FragColor = vec4(
      sqrt(smoothstep(0.2, 1.0,
        mix(mix(ground_color_val, sky_color_val, h_dist_mix), cloud_color_val, f_fog_mix) - dot(uv_frag, uv_frag) * 0.1
      )),
      1.0
    );
  }
}