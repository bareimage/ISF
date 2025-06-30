/*{
  "DESCRIPTION": "ISF conversion of a raymarched fractal shader. Features smoothed zoom, dynamic multi-origin symmetry (two points on a circle 180 degrees apart, plus center), smoothed time, colors, view rotation, and fractal symmetry controls with blurred transitions. Speed primarily affects time progression. All primary inputs are buffered for smooth transitions.",
  "CREDIT": "Original ShaderToy @fractal; ISF Version by @dot2dot",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR"],
  "INPUTS": [
    {
      "NAME": "speed",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 5.0,
      "LABEL": "Overall Time Speed"
    },
    {
      "NAME": "baseColorVolumetricInput",
      "TYPE": "color",
      "DEFAULT": [0.1, 0.7, 0.7, 1.0],
      "LABEL": "Volumetric Base Color"
    },
    {
      "NAME": "emissiveHighlightColorInput",
      "TYPE": "color",
      "DEFAULT": [0.1, 1.0, 0.1, 1.0],
      "LABEL": "Emissive Highlight Color"
    },
    {
      "NAME": "viewRotationFactorInput",
      "TYPE": "float",
      "DEFAULT": 0.1,
      "MIN": -2.0,
      "MAX": 2.0,
      "LABEL": "View Rotation Rate"
    },
    {
      "NAME": "fractalSymmetryDivisionsInput",
      "TYPE": "float",
      "DEFAULT": 6.0,
      "MIN": 3.0,
      "MAX": 32.0,
      "LABEL": "Fractal Symmetry Divisions"
    },
    {
      "NAME": "dynamicOriginRadiusInput",
      "TYPE": "float",
      "DEFAULT": 0.5,
      "MIN": 0.0,
      "MAX": 2.0,
      "LABEL": "Dynamic Origins Radius"
    },
    {
      "NAME": "dynamicOriginAngleInput",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 6.283185307, 
      "LABEL": "Dynamic Origins Angle (Radians)"
    },
    {
      "NAME": "zoomLevelInput",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.1,
      "MAX": 10.0,
      "LABEL": "Zoom Level"
    },
    {
      "NAME": "transitionSpeed",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.1,
      "MAX": 10.0,
      "LABEL": "Parameter Transition Smoothness"
    }
  ],
  "PASSES": [
    {
      "TARGET": "timeAndRotationBuffer",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": 1, 
      "HEIGHT": 1
    },
    {
      "TARGET": "baseColorBuffer",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": 1,
      "HEIGHT": 1
    },
    {
      "TARGET": "emissiveColorBuffer",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": 1,
      "HEIGHT": 1
    },
    {
      "TARGET": "fractalParamsBuffer",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": 1,
      "HEIGHT": 1
    },
    {
      "TARGET": "originParamsBuffer",
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

// Global constant
#define PI 3.1415926535

// Variables to hold smoothed values, accessible in the final pass
float effective_iTime;
float effective_TK; 
float effective_accumulatedViewRotationAngle; 
vec3 effective_baseColorVolumetric;
vec3 effective_emissiveHighlightColor;
float effective_dynamicOriginRadius;
float effective_dynamicOriginAngle;
float effective_zoomLevel;


// --- Helper Functions (Pure Data Transformers) ---

vec2 rotate2D(vec2 p, float r) {
    mat2 rotationMatrix = mat2(cos(r), sin(r), -sin(r), cos(r));
    return rotationMatrix * p;
}

vec2 polarModulo(vec2 p, float n) {
    n = max(1.0, n); 
    float anglePerSector = 2.0 * PI / n;
    float currentAngle = atan(p.y, p.x); 
    float modifiedAngle = currentAngle - 0.5 * anglePerSector;
    modifiedAngle = mod(modifiedAngle, anglePerSector) - 0.5 * anglePerSector;
    return length(p) * vec2(cos(modifiedAngle), sin(modifiedAngle));
}

float sdfCube_original(vec3 p_to_cube, vec3 s_cube) {
    vec3 q_abs = abs(p_to_cube);
    vec3 m_dist_to_face_inside = max(s_cube - q_abs, 0.0);
    float dist_outside = length(max(q_abs - s_cube, 0.0));
    float dist_inside_component = min(min(m_dist_to_face_inside.x, m_dist_to_face_inside.y), m_dist_to_face_inside.z);
    return dist_outside - dist_inside_component;
}

// --- Scene Signed Distance Function (SDF) Calculation Helper ---
float calculateSdfForDivisions(vec3 queryPoint, float divisions, float current_iTime, float dynamic_radius, float dynamic_angle) {
    vec3 p_animated_z = vec3(queryPoint.x, queryPoint.y, queryPoint.z - 1.0 * current_iTime);
    vec3 p_twisted = vec3(rotate2D(p_animated_z.xy, 1.0 * p_animated_z.z), p_animated_z.z);
    
    vec2 current_point_xy = p_twisted.xy;
    
    vec2 origin1_xy = vec2(0.0, 0.0); 
    vec2 origin2_xy = dynamic_radius * vec2(cos(dynamic_angle), sin(dynamic_angle));
    vec2 origin3_xy = -origin2_xy; 

    float dist_to_origin1 = length(current_point_xy - origin1_xy);
    float dist_to_origin2 = length(current_point_xy - origin2_xy);
    float dist_to_origin3 = length(current_point_xy - origin3_xy);

    vec2 active_origin_offset = origin1_xy;
    float min_dist = dist_to_origin1;

    if (dist_to_origin2 < min_dist) {
        min_dist = dist_to_origin2;
        active_origin_offset = origin2_xy;
    }
    if (dist_to_origin3 < min_dist) {
        active_origin_offset = origin3_xy;
    }
    
    vec2 p_mod_xy = polarModulo(current_point_xy - active_origin_offset, divisions) + active_origin_offset;
    vec3 p_radial_sym = vec3(p_mod_xy, p_twisted.z);

    float repetitionScale = 0.7;
    float zCellId = floor(p_radial_sym.z * repetitionScale);
    vec3 p_cell_local = mod(p_radial_sym, repetitionScale) - 0.5 * repetitionScale;

    vec3 p_iterated_fractal = p_cell_local;
    for (int i = 0; i < 4; i++) {
        p_iterated_fractal = abs(p_iterated_fractal) - 0.3;
        float rotAngleXY = 1.0 + zCellId + 0.1 * current_iTime; 
        p_iterated_fractal.xy = rotate2D(p_iterated_fractal.xy, rotAngleXY);
        float rotAngleXZ = 1.0 + 4.7 * zCellId + 0.3 * current_iTime;
        p_iterated_fractal.xz = rotate2D(p_iterated_fractal.xz, rotAngleXZ);
    }

    float distToCube = sdfCube_original(p_iterated_fractal, vec3(0.3));
    float distToSphere = length(p_iterated_fractal) - 0.4;
    return min(distToCube, distToSphere);
}

// --- Main Scene Signed Distance Function (SDF) with Blending ---
float sceneDist(vec3 queryPoint, 
                float currentFrameDivisions, float prevFrameDivisions, float blendAlpha, 
                float current_iTime_param, float dyn_radius, float dyn_angle) {
    float dist_current = calculateSdfForDivisions(queryPoint, currentFrameDivisions, current_iTime_param, dyn_radius, dyn_angle);

    if (blendAlpha >= 0.999 || abs(currentFrameDivisions - prevFrameDivisions) < 0.001) {
        return dist_current;
    }

    float dist_previous = calculateSdfForDivisions(queryPoint, prevFrameDivisions, current_iTime_param, dyn_radius, dyn_angle);
    return mix(dist_previous, dist_current, blendAlpha);
}


// --- Main ISF Rendering Function ---
void main() {
    if (PASSINDEX == 0) {
        // --- Pass 0: Update timeAndRotationBuffer ---
        vec4 prevBufferData = IMG_NORM_PIXEL(timeAndRotationBuffer, vec2(0.5, 0.5));
        float previousAccumulated_iTime = prevBufferData.r;
        float previous_effective_TK = prevBufferData.g; 
        float previous_viewRotationRate = prevBufferData.b; 
        float previous_accumulatedViewRotationAngle = prevBufferData.a;
        
        float current_smoothed_TK; 
        float newAccumulated_iTime;
        float current_smoothed_viewRotationRate;
        float newAccumulated_viewRotationAngle;

        if (FRAMEINDEX == 0) {
            newAccumulated_iTime = 0.0;
            current_smoothed_TK = speed; 
            current_smoothed_viewRotationRate = viewRotationFactorInput;
            newAccumulated_viewRotationAngle = 0.0;
        } else {
            current_smoothed_TK = mix(previous_effective_TK, speed, min(1.0, TIMEDELTA * transitionSpeed));
            newAccumulated_iTime = previousAccumulated_iTime + current_smoothed_TK * TIMEDELTA;
            current_smoothed_viewRotationRate = mix(previous_viewRotationRate, viewRotationFactorInput, min(1.0, TIMEDELTA * transitionSpeed));
            newAccumulated_viewRotationAngle = previous_accumulatedViewRotationAngle + current_smoothed_viewRotationRate * TIMEDELTA;
        }
        gl_FragColor = vec4(newAccumulated_iTime, current_smoothed_TK, current_smoothed_viewRotationRate, newAccumulated_viewRotationAngle);

    } else if (PASSINDEX == 1) {
        // --- Pass 1: Update baseColorBuffer ---
        vec4 prevBufferData = IMG_NORM_PIXEL(baseColorBuffer, vec2(0.5, 0.5));
        vec3 previous_effective_baseColor = prevBufferData.rgb;
        vec3 current_effective_baseColor;
        if (FRAMEINDEX == 0) {
            current_effective_baseColor = baseColorVolumetricInput.rgb;
        } else {
            current_effective_baseColor = mix(previous_effective_baseColor, baseColorVolumetricInput.rgb, min(1.0, TIMEDELTA * transitionSpeed));
        }
        gl_FragColor = vec4(current_effective_baseColor, 1.0);

    } else if (PASSINDEX == 2) {
        // --- Pass 2: Update emissiveColorBuffer ---
        vec4 prevBufferData = IMG_NORM_PIXEL(emissiveColorBuffer, vec2(0.5, 0.5));
        vec3 previous_effective_emissiveColor = prevBufferData.rgb;
        vec3 current_effective_emissiveColor;
        if (FRAMEINDEX == 0) {
            current_effective_emissiveColor = emissiveHighlightColorInput.rgb;
        } else {
            current_effective_emissiveColor = mix(previous_effective_emissiveColor, emissiveHighlightColorInput.rgb, min(1.0, TIMEDELTA * transitionSpeed));
        }
        gl_FragColor = vec4(current_effective_emissiveColor, 1.0);

    } else if (PASSINDEX == 3) {
        // --- Pass 3: Update fractalParamsBuffer (fractalSymmetryDivisions current, previous, blend_alpha) ---
        vec4 prevBufferData = IMG_NORM_PIXEL(fractalParamsBuffer, vec2(0.5, 0.5));
        float previous_frame_effective_fractalSymmetryDivisions = prevBufferData.r;
        float current_smoothed_fractalSymmetryDivisions;
        float blend_alpha_for_symmetry;
        if (FRAMEINDEX == 0) {
            current_smoothed_fractalSymmetryDivisions = fractalSymmetryDivisionsInput;
            gl_FragColor = vec4(current_smoothed_fractalSymmetryDivisions, current_smoothed_fractalSymmetryDivisions, 1.0, 1.0);
        } else {
            blend_alpha_for_symmetry = min(1.0, TIMEDELTA * transitionSpeed);
            current_smoothed_fractalSymmetryDivisions = mix(previous_frame_effective_fractalSymmetryDivisions, fractalSymmetryDivisionsInput, blend_alpha_for_symmetry);
            gl_FragColor = vec4(current_smoothed_fractalSymmetryDivisions, previous_frame_effective_fractalSymmetryDivisions, blend_alpha_for_symmetry, 1.0);
        }

    } else if (PASSINDEX == 4) {
        // --- Pass 4: Update originParamsBuffer (dynamicOriginRadius, dynamicOriginAngle, zoomLevel) ---
        vec4 prevBufferData = IMG_NORM_PIXEL(originParamsBuffer, vec2(0.5, 0.5));
        float previous_radius = prevBufferData.r;
        float previous_angle = prevBufferData.g;
        float previous_zoom = prevBufferData.b; // Use .b for zoom

        float current_smoothed_radius;
        float current_smoothed_angle;
        float current_smoothed_zoom;

        if (FRAMEINDEX == 0) {
            current_smoothed_radius = dynamicOriginRadiusInput;
            current_smoothed_angle = dynamicOriginAngleInput;
            current_smoothed_zoom = zoomLevelInput;
        } else {
            float blend_alpha = min(1.0, TIMEDELTA * transitionSpeed);
            current_smoothed_radius = mix(previous_radius, dynamicOriginRadiusInput, blend_alpha);
            current_smoothed_angle = mix(previous_angle, dynamicOriginAngleInput, blend_alpha);
            current_smoothed_zoom = mix(previous_zoom, zoomLevelInput, blend_alpha);
        }
        // Store radius in .r, angle in .g, zoom in .b. .a is unused.
        gl_FragColor = vec4(current_smoothed_radius, current_smoothed_angle, current_smoothed_zoom, 0.0);


    } else if (PASSINDEX == 5) {
        // --- Pass 5: Main Raymarching and Rendering ---
        vec4 timeRotBufferData = IMG_NORM_PIXEL(timeAndRotationBuffer, vec2(0.5, 0.5));
        effective_iTime = timeRotBufferData.r;                
        effective_TK = timeRotBufferData.g;                   
        effective_accumulatedViewRotationAngle = timeRotBufferData.a;

        effective_baseColorVolumetric = IMG_NORM_PIXEL(baseColorBuffer, vec2(0.5, 0.5)).rgb;
        effective_emissiveHighlightColor = IMG_NORM_PIXEL(emissiveColorBuffer, vec2(0.5, 0.5)).rgb;
        
        vec4 fractalParamsBufferData = IMG_NORM_PIXEL(fractalParamsBuffer, vec2(0.5, 0.5));
        float current_effective_fractalSymmetryDivisions = fractalParamsBufferData.r;
        float previous_frame_fractalSymmetryDivisions_val = fractalParamsBufferData.g;
        float symmetry_blend_alpha_val = fractalParamsBufferData.b;

        vec4 originParamsData = IMG_NORM_PIXEL(originParamsBuffer, vec2(0.5, 0.5));
        effective_dynamicOriginRadius = originParamsData.r;
        effective_dynamicOriginAngle = originParamsData.g;
        effective_zoomLevel = max(0.01, originParamsData.b); // Ensure zoom is not zero or negative


        vec2 uv_centered = 2.0 * (isf_FragNormCoord - 0.5);
        vec2 uv_aspect_corrected = vec2(uv_centered.x * RENDERSIZE.x / RENDERSIZE.y, uv_centered.y); 
        // Apply zoom by scaling UV coordinates
        vec2 uv_zoomed = uv_aspect_corrected / effective_zoomLevel;
        vec2 uv_view_rotated = rotate2D(uv_zoomed, effective_accumulatedViewRotationAngle);    

        vec3 rayOrigin = vec3(0.0, 0.0, 0.1);
        vec3 rayDirection = normalize(vec3(uv_view_rotated, 0.0) - rayOrigin);

        float totalDistanceTraveled = 2.0;
        float accumulatedDensity = 0.0;

        int maxSteps = 66;
        for (int i = 0; i < maxSteps; i++) {
            vec3 currentRayPosition = rayOrigin + rayDirection * totalDistanceTraveled;
            float distSample = sceneDist(currentRayPosition, 
                                         current_effective_fractalSymmetryDivisions, 
                                         previous_frame_fractalSymmetryDivisions_val, 
                                         symmetry_blend_alpha_val,
                                         effective_iTime,
                                         effective_dynamicOriginRadius,
                                         effective_dynamicOriginAngle) * 0.2; 
            float stepDistance = max(0.00001, abs(distSample)); 
            totalDistanceTraveled += stepDistance;
            if (abs(distSample) < 0.001) { 
                accumulatedDensity += 0.1;
            }
            if (totalDistanceTraveled > 100.0) break; 
        }

        vec3 finalBaseColor = effective_baseColorVolumetric * 0.2 * accumulatedDensity;
        vec3 hitPoint = rayOrigin + rayDirection * totalDistanceTraveled;
        
        float emissionPatternScale = 0.5;
        vec3 pn_for_emission = hitPoint;
        pn_for_emission.z += -1.5 * effective_iTime; 
        pn_for_emission.z = mod(pn_for_emission.z, emissionPatternScale) - 0.5 * emissionPatternScale;

        float emissionStrength = clamp(0.01 / abs(pn_for_emission.z + 1e-5), 0.0, 100.0); 
        vec3 finalEmissiveColor = 3.0 * emissionStrength * effective_emissiveHighlightColor;
        vec3 finalPixelColor = finalBaseColor + finalEmissiveColor;

        finalPixelColor = clamp(finalPixelColor, 0.0, 1.0);
        gl_FragColor = vec4(finalPixelColor, 1.0);
    }
}
