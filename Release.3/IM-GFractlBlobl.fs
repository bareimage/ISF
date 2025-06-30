/*{
  "DESCRIPTION": "3D Fractal Blob Effect with Gaussian Blur and Smooth Parameter Transitions. Optimized to use 2 custom buffers.",
  "CREDIT": "Original by @philip.bertani@gmail.com, ISF Version by @dot2dot",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR", "3D", "BLUR", "FRACTAL"],
  "INPUTS": [
    { "NAME": "animationSpeed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 5.0, "LABEL": "Animation Speed" },
    { "NAME": "rotationSpeed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 3.0, "LABEL": "Rotation Speed" },
    { "NAME": "blobIntensity", "TYPE": "float", "DEFAULT": 0.79, "MIN": 0.1, "MAX": 1.0, "LABEL": "Blob Intensity" },
    { "NAME": "scaleAmount", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.1, "MAX": 2.0, "LABEL": "Scale Amount" },
    { "NAME": "blurStrength", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 3.0, "LABEL": "Blur Strength" },
    { "NAME": "colorShift", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 5.0, "LABEL": "Color Shift" },
    { "NAME": "transitionSpeed", "TYPE": "float", "DEFAULT": 2.0, "MIN": 0.1, "MAX": 10.0, "LABEL": "Transition Smoothness" }
  ],
  "PASSES": [
    { "TARGET": "stateBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 2, "HEIGHT": 1 },
    { "TARGET": "fractalBuffer", "PERSISTENT": false, "FLOAT": true },
    { "TARGET": "finalOutput" }
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

precision highp float;

// Built-in variables provided by ISF host:
// uniform float TIME; // Time in seconds since composition started
// uniform float TIMEDELTA; // Time in seconds since last frame
// uniform int FRAMEINDEX; // Index of the current frame
// uniform vec2 RENDERSIZE; // Size of the rendering buffer in pixels
// uniform sampler2D stateBuffer; // Sampler for our persistent state buffer (previous frame)
// uniform sampler2D fractalBuffer; // Sampler for the rendered fractal
// varying vec2 isf_FragNormCoord; // Normalized fragment coordinates (0-1) for final pass

const float pi = 3.14159265359;

// Rodrigues-Euler axis angle rotation
vec3 ROT(vec3 p, vec3 axis, float t) {
    return mix(axis * dot(p, axis), p, cos(t)) + sin(t) * cross(p, axis);
}

// Color formula
vec3 H(float h, vec4 id) {
    // Using id to add some variation, simple hash-like approach
    float idFactor = fract(sin(dot(id.xy, vec2(12.9898, 78.233))) * 43758.5453);
    return cos(h + vec3(10.0, 3.0, 2.0) + idFactor * 0.5) * 0.7 + 0.2;
}

// Mapping scale factor
float M(float c) {
    return log(c);
}

void main() {
    float smoothFactor = min(1.0, TIMEDELTA * transitionSpeed);

    // PASS 0: Update stateBuffer
    // This pass runs for each pixel of stateBuffer (2x1).
    // It calculates all smoothed parameters and writes them into stateBuffer.
    if (PASSINDEX == 0) {
        // Read all previous values from the 2 pixels of stateBuffer (from the previous frame)
        // Normalized coordinates for sampling centers of a 2x1 texture:
        // Pixel 0: (0.5 / 2.0, 0.5 / 1.0) = (0.25, 0.5)
        // Pixel 1: (1.5 / 2.0, 0.5 / 1.0) = (0.75, 0.5)
        vec4 prev_px0_val = texture(stateBuffer, vec2(0.25, 0.5)); // Changed texture2D to texture
        vec4 prev_px1_val = texture(stateBuffer, vec2(0.75, 0.5)); // Changed texture2D to texture

        // Unpack previous values
        float prev_accumTime_fractal      = prev_px0_val.r;
        float prev_smoothedAnimSpeed      = prev_px0_val.g;
        float prev_smoothedBlobIntensity  = prev_px0_val.b;
        float prev_smoothedScaleAmount    = prev_px0_val.a;

        float prev_smoothedBlurStrength   = prev_px1_val.r;
        float prev_smoothedColorShift     = prev_px1_val.g;
        float prev_smoothedRotationSpeed  = prev_px1_val.b;
        float prev_accumulatedRotationAngle = prev_px1_val.a;

        // Calculate current smoothed values using mix for smooth transitions
        // If FRAMEINDEX is 0, initialize directly with the input uniform.
        float sAnimSpeed     = (FRAMEINDEX == 0) ? animationSpeed : mix(prev_smoothedAnimSpeed, animationSpeed, smoothFactor);
        float sBlobIntensity = (FRAMEINDEX == 0) ? blobIntensity  : mix(prev_smoothedBlobIntensity, blobIntensity, smoothFactor);
        float sScaleAmount   = (FRAMEINDEX == 0) ? scaleAmount    : mix(prev_smoothedScaleAmount, scaleAmount, smoothFactor);
        float sBlurStrength  = (FRAMEINDEX == 0) ? blurStrength   : mix(prev_smoothedBlurStrength, blurStrength, smoothFactor);
        float sColorShift    = (FRAMEINDEX == 0) ? colorShift     : mix(prev_smoothedColorShift, colorShift, smoothFactor);
        float sRotSpeed      = (FRAMEINDEX == 0) ? rotationSpeed  : mix(prev_smoothedRotationSpeed, rotationSpeed, smoothFactor);

        // Calculate current accumulated values
        // On FRAMEINDEX 0, accumulated time/angle starts from 0 then adds the first delta.
        float accumTime_fractal = (FRAMEINDEX == 0) ? 0.0 : prev_accumTime_fractal;
        accumTime_fractal += sAnimSpeed * TIMEDELTA;

        float accumRotAngle = (FRAMEINDEX == 0) ? 0.0 : prev_accumulatedRotationAngle;
        accumRotAngle += sRotSpeed * TIMEDELTA;
        
        // RENDERSIZE for this pass is vec2(2.0, 1.0) as defined in PASSES.
        // gl_FragCoord.x will be 0.5 for the first pixel, 1.5 for the second.
        if (gl_FragCoord.x < 1.0) { // Writing to the first pixel of stateBuffer
            gl_FragColor = vec4(accumTime_fractal, sAnimSpeed, sBlobIntensity, sScaleAmount);
        } else { // Writing to the second pixel of stateBuffer
            gl_FragColor = vec4(sBlurStrength, sColorShift, sRotSpeed, accumRotAngle);
        }
        return;
    }
    
    // PASS 1: Render fractal scene to fractalBuffer
    // This pass uses the values from the updated stateBuffer.
    if (PASSINDEX == 1) {
        // Read all smoothed/accumulated values from the current frame's stateBuffer
        vec4 px0_val = texture(stateBuffer, vec2(0.25, 0.5)); // Changed texture2D to texture
        vec4 px1_val = texture(stateBuffer, vec2(0.75, 0.5)); // Changed texture2D to texture

        float currentFractalTime          = px0_val.r; // Used for blob/scale dynamics
        // float currentSmoothedAnimSpeed = px0_val.g; // Available if needed
        float currentBlobIntensity        = px0_val.b;
        float currentScaleAmount          = px0_val.a;

        // float currentBlurStrength      = px1_val.r; // Not used in this pass
        float currentColorShift           = px1_val.g;
        // float currentSmoothedRotSpeed  = px1_val.b; // Available if needed
        float currentAccumulatedRotationAngle = px1_val.a; // Used for object rotation

        float universalTime = TIME; // Global time for stable rotation axis evolution

        vec2 U = gl_FragCoord.xy; // Pixel coordinates for fractalBuffer
        vec2 R = RENDERSIZE;      // Dimensions of fractalBuffer (usually main output size)
        
        vec3 c = vec3(0.0); // Final color for the pixel
        // Ray direction, scaled; R.y is often used for FOV adjustment
        vec3 rd = normalize(vec3(U - 0.5 * R.xy, R.y)) * 32.0; 
        
        float sc, dotp, totdist = 0.0;
        float t_fractal = currentFractalTime / 2.0; // Time variable for fractal animations
        
        // Raymarching loop
        for(float i = 0.0; i < 150.0; i++) { 
            vec4 p = vec4(rd * totdist, 0.0); // Current point along the ray
            
            p.xyz += vec3(0.0, 0.0, -100.0); // Initial translation to move camera back
            sc = 1.0; // Scale factor for distance estimation
            
            // Rotation axis evolves based on universalTime (stable)
            vec3 rotAxis = normalize(vec3(sin(universalTime / 5.0), cos(universalTime / 3.0), sin(universalTime / 7.0)*0.5 + 0.5)); // Added some z-axis evolution
            // Rotate point p around the axis by the accumulated rotation angle
            p.xyz = ROT(p.xyz, rotAxis, currentAccumulatedRotationAngle); 
            
            vec4 id = round(p / 4.0); // Cell ID for color variation based on spatial location
            
            // Fractal iteration
            for(float j = 0.0; j < 7.0; j++) {
                // Blob dynamics influenced by fractalTime and blobIntensity
                float blobs = currentBlobIntensity + 0.03 * abs(sin(t_fractal * 1.2 + j * 0.1)); 
                p = log(blobs + abs(p)); // Apply log and absolute value transformation
                
                dotp = max(1.0 / dot(p, p), 0.2); // Inverse squared length, clamped
                // Scale factor modulation by fractalTime and scaleAmount
                sc *= dotp * (currentScaleAmount + 0.15 * abs(sin(t_fractal * 1.2 + pi / 2.0 + j * 0.1))); 
                
                p *= dotp - currentScaleAmount; // Further transformation of p
            }
            
            // Distance estimator for the fractal surface
            float dist = abs(length(p) - 0.6) / sc; 
            float stepsize = dist / 4.0 + 1e-4; // Adaptive step size
            totdist += stepsize; // Accumulate total distance marched
            
            // Accumulate color contribution based on scale factor and iteration depth
            // Exponential falloff for contribution to avoid overly bright spots from distant steps
            c += mix(vec3(1.0), H(M(sc) * currentColorShift, id), 0.7) * 0.015 * exp(-i * i * stepsize * stepsize * 0.01);
            
            if (totdist > 200.0 || stepsize < 1e-3) break; // Early exit conditions
        }
        
        c = clamp(c, 0.0, 1.0); // Clamp color to valid range
        c = c * c; // Gamma correction / contrast enhancement
        
        gl_FragColor = vec4(c, 1.0);
        return;
    }
    
    // Pass 2: Apply Gaussian blur and output to finalOutput (screen)
    // This pass reads from fractalBuffer and uses blurStrength from stateBuffer.
    if (PASSINDEX == 2) {
        // Read currentBlurStrength from stateBuffer
        vec4 px1_val = texture(stateBuffer, vec2(0.75, 0.5)); // .r component of second pixel. Changed texture2D to texture
        float currentBlurStrength = px1_val.r;
        
        vec2 uv = isf_FragNormCoord.xy; // Normalized coordinates for the final output
        vec4 O = vec4(0.0); // Final output color
        
        // Predefined 5x5 Gaussian kernel weights
        // Sum of these weights is approximately 1.0
        float gk1s[25];
        gk1s[0] = 0.003765; gk1s[1] = 0.015019; gk1s[2] = 0.023792; gk1s[3] = 0.015019; gk1s[4] = 0.003765;
        gk1s[5] = 0.015019; gk1s[6] = 0.059912; gk1s[7] = 0.094907; gk1s[8] = 0.059912; gk1s[9] = 0.015019;
        gk1s[10] = 0.023792; gk1s[11] = 0.094907; gk1s[12] = 0.150342; gk1s[13] = 0.094907; gk1s[14] = 0.023792;
        gk1s[15] = 0.015019; gk1s[16] = 0.059912; gk1s[17] = 0.094907; gk1s[18] = 0.059912; gk1s[19] = 0.015019;
        gk1s[20] = 0.003765; gk1s[21] = 0.015019; gk1s[22] = 0.023792; gk1s[23] = 0.015019; gk1s[24] = 0.003765;
        
        if (currentBlurStrength <= 0.001) { // If blur is negligible, just output the fractalBuffer content
            O = texture(fractalBuffer, uv); // Changed texture2D to texture
        } else { // Apply Gaussian blur
            for (int k = 0; k < 25; k++) {
                // Calculate offset for each sample in the kernel
                // Offset is scaled by blurStrength and pixel size (1.0 / RENDERSIZE)
                // RENDERSIZE here refers to the dimensions of finalOutput.
                // Assuming fractalBuffer has the same dimensions.
                vec2 offset = (vec2(float(k % 5), float(k / 5)) - 2.0) * (1.0 / RENDERSIZE) * currentBlurStrength;
                O += gk1s[k] * texture(fractalBuffer, uv + offset); // Changed texture2D to texture
            }
        }
        
        gl_FragColor = O;
        return;
    }
}
