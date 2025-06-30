/*{
  "DESCRIPTION": "Converted Shadertoy shader featuring abstract fractal patterns with smoothed speed, rotation, and parameter transitions. Original Shadertoy by unknown author, optimized by FabriceNeyret2. Includes brightness protection and twist perturbation.",
  "CREDIT": "Based on code @diatribes (https://www.shadertoy.com/view/tfc3DX), ISF 2.0 Version by @dot2dot (bareimage)",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR"],
  "INPUTS": [
    {
      "NAME": "speed",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 20.0,
      "LABEL": "Speed"
    },
    {
      "NAME": "colorControl",
      "TYPE": "color",
      "DEFAULT": [1.0, 1.0, 1.0, 1.0],
      "LABEL": "Final Tint Color"
    },
    {
      "NAME": "rotationDirection",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": -1.0,
      "MAX": 1.0,
      "LABEL": "Rotation Direction Multiplier"
    },
    {
      "NAME": "perturbation",
      "TYPE": "float",
      "DEFAULT": 0.2,
      "MIN": 0.0,
      "MAX": 2.0,
      "LABEL": "Twist Intensity"
    },
    {
      "NAME": "fractal_osc_amplitude",
      "TYPE": "float",
      "DEFAULT": 0.125,
      "MIN": 0.0,
      "MAX": 0.5,
      "LABEL": "Fractal Oscillation Amplitude"
    },
    {
      "NAME": "brightness",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.1,
      "MAX": 10.0,
      "LABEL": "Brightness"
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
      "TARGET": "paramBuffer",
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

void main() {
  // Declare variables for use in all passes
  vec4 prevParamData, prevRotData;
  float accumulatedTime, currentSpeedSmoothed, currentPerturbationSmoothed, currentFractalOscSmoothed;
  float newTime, adjustedSpeed, adjustedPerturbation, adjustedFractalOsc;
  float currentRotDirSmoothed, prevRotDir;

  float effectiveTime, effectiveRotDir, effectivePerturbation, effectiveFractalOsc;
  // Shadertoy specific variables for final pass
  vec2 R_res; // For iResolution
  vec2 u_st;  // For Shadertoy u coordinate
  vec4 o_st;  // For Shadertoy o (output color)
  vec3 p_st;
  float t_st, i_st, d_st, s_st, w_st, l_st;


  if (PASSINDEX == 0) {
    // First pass: Update accumulated time and other smoothed parameters
    prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5, 0.5));
    
    accumulatedTime = prevParamData.r;
    currentSpeedSmoothed = prevParamData.g;
    currentPerturbationSmoothed = prevParamData.b; 
    currentFractalOscSmoothed = prevParamData.a;   
    
    if (FRAMEINDEX == 0) {
      newTime = 0.0;
      adjustedSpeed = speed;
      adjustedPerturbation = perturbation; 
      adjustedFractalOsc = fractal_osc_amplitude; 
    } else {
      adjustedSpeed = mix(currentSpeedSmoothed, speed, min(1.0, TIMEDELTA * transitionSpeed));
      adjustedPerturbation = mix(currentPerturbationSmoothed, perturbation, min(1.0, TIMEDELTA * transitionSpeed));
      adjustedFractalOsc = mix(currentFractalOscSmoothed, fractal_osc_amplitude, min(1.0, TIMEDELTA * transitionSpeed));
      newTime = accumulatedTime + adjustedSpeed * TIMEDELTA;
    }
    
    gl_FragColor = vec4(newTime, adjustedSpeed, adjustedPerturbation, adjustedFractalOsc);
  }
  else if (PASSINDEX == 1) {
    // Second pass: Update the rotation direction with smooth transition
    prevRotData = IMG_NORM_PIXEL(rotationBuffer, vec2(0.5, 0.5));
    
    if (FRAMEINDEX == 0) {
      currentRotDirSmoothed = rotationDirection;
    } else {
      prevRotDir = prevRotData.r;
      currentRotDirSmoothed = mix(prevRotDir, rotationDirection, min(1.0, TIMEDELTA * transitionSpeed));
    }
    
    gl_FragColor = vec4(currentRotDirSmoothed, 0.0, 0.0, 1.0);
  }
  else { // PASSINDEX == 2 - Final render pass
    // Get the accumulated/smoothed values
    prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5, 0.5));
    prevRotData = IMG_NORM_PIXEL(rotationBuffer, vec2(0.5, 0.5));
    
    effectiveTime = prevParamData.r;
    effectivePerturbation = prevParamData.b; 
    effectiveFractalOsc = prevParamData.a;   
    effectiveRotDir = prevRotData.r;

    // ---- Shadertoy mainImage logic START ----
    R_res = RENDERSIZE.xy;
    u_st = (isf_FragNormCoord.xy * R_res - R_res/2.0) / R_res.y;

    // New Twist Perturbation:
    if (abs(effectivePerturbation) > 0.001) { // Apply only if perturbation is significant
        float twist_strength = effectivePerturbation * 0.5; // Max twist of 1.0 rad if perturbation input is 2.0
        float dist_from_center = length(u_st);
        // Angle depends on time, distance from center, and perturbation strength
        float angle = twist_strength * (sin(effectiveTime * 0.2 + dist_from_center * 1.5) + effectiveTime * 0.1); 
        mat2 twist_matrix = mat2(cos(angle), -sin(angle), sin(angle), cos(angle));
        u_st = twist_matrix * u_st;
    }
    // Old perturbation (removed):
    // u_st -= sin(effectiveTime * 0.3) * effectivePerturbation; 

    t_st = effectiveTime * 0.2;
    
    o_st = vec4(0.0); 
    d_st = 0.0;       

    for (i_st = 1.0; i_st <= 100.0; i_st++) { 
      p_st = 0.5 * vec3( u_st * d_st, d_st + t_st ); 
      s_st = 1.0 - length(p_st.xy + d_st * 2.0);
      p_st += cos(t_st + p_st.yzx) * 0.05; 
      
      float common_angle_base = cos(t_st * 0.3) * 3.0;
      float final_common_angle = common_angle_base * effectiveRotDir;
      
      vec4 rot_angle_offsets = vec4(0.0, 33.0, 11.0, 0.0); 
      mat2 rot_mat = mat2(
        cos(final_common_angle + rot_angle_offsets.x), cos(final_common_angle + rot_angle_offsets.y), 
        cos(final_common_angle + rot_angle_offsets.z), cos(final_common_angle + rot_angle_offsets.w)  
      );
      p_st.xy = p_st.xy * rot_mat; 
        
      w_st = 0.4;
      for (int j_loop = 0; j_loop < 6; j_loop++) {
        p_st = abs(sin(p_st)) - 1.05; 
        l_st = (1.5 + cos(t_st) * effectiveFractalOsc) / dot(p_st, p_st); 
        p_st *= l_st; 
        w_st *= l_st; 
      }
      s_st = max(length(p_st) / w_st, s_st); 
      d_st += s_st;
      
      // Brightness protection: ensure s_st is not too small to prevent division by zero/very small number
      o_st += (1.0 + cos(d_st + vec4(1.0, 3.0, 4.0, 1.0))) / max(s_st, 0.0001);
    }
    
    float scale_factor = brightness * (1.0 / 200000.0); 
    o_st = tanh(o_st * scale_factor * exp(-d_st / 8.0));
    // ---- Shadertoy mainImage logic END ----

    gl_FragColor = o_st * colorControl; 
  }
}
