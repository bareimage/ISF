/*{
    "DESCRIPTION": "Conversion of a complex feedback shader from twigl.app. Features smoothed animation speed and rotation control.",
    "CREDIT": "Original algorithm by @XorDev(https://x.com/XorDev/status/1727206969038213426) twigl.app version. ISF 2.0 Version with enhancements by dot2dot.",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "GENERATOR"
    ],
    "INPUTS": [
        {
            "NAME": "speed",
            "TYPE": "float",
            "DEFAULT": 4.0,
            "MIN": -10.0,
            "MAX": 10.0,
            "LABEL": "Animation Speed"
        },
        {
            "NAME": "rotationFactor",
            "TYPE": "float",
            "DEFAULT": 4.0,
            "MIN": 8.0,
            "MAX": 0.5,
            "LABEL": "Rotation Factor"
        },
        {
            "NAME": "colorTint",
            "TYPE": "color",
            "DEFAULT": [1.0, 1.0, 1.0, 1.0],
            "LABEL": "Color Tint"
        },
        {
            "NAME": "intensity",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Intensity"
        },
        {
            "NAME": "transitionSpeed",
            "TYPE": "float",
            "DEFAULT": 2.0,
            "MIN": 0.1,
            "MAX": 10.0,
            "LABEL": "Control Smoothness"
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

mat2 rotate2D(float a) {
    float s = sin(a), c = cos(a);
    return mat2(c, -s, s, c);
}

void main() {
    if (PASSINDEX == 0) {
        // PASS 0: TIME ACCUMULATOR
        // Updates and stores the accumulated time based on a smoothed speed value.
        // This replaces the global TIME uniform to prevent jumps when speed changes.
        vec4 prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        
        float accumulatedTime = prevTimeData.r;
        float currentSpeed = prevTimeData.g;
        float newTime;
        float adjustedSpeed;

        if (FRAMEINDEX == 0) {
            newTime = 0.0;
            adjustedSpeed = speed;
        } else {
            adjustedSpeed = mix(currentSpeed, speed, min(1.0, TIMEDELTA * transitionSpeed));
            newTime = accumulatedTime + adjustedSpeed * TIMEDELTA;
        }
        
        // Store accumulated time in .r and the current smoothed speed in .g
        gl_FragColor = vec4(newTime, adjustedSpeed, 0.0, 1.0);

    } else if (PASSINDEX == 1) {
        // PASS 1: ROTATION PHASE ACCUMULATOR
        // Calculates a smooth, continuous rotation angle (phase) by integrating the
        // rotation speed over time. This avoids phase jumps when changing rotationFactor.
        
        // Read data from previous buffers
        vec4 timeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        vec4 prevRotData = IMG_NORM_PIXEL(rotationBuffer, vec2(0.5));

        // Get the smoothed speed from the time buffer
        float adjustedSpeed = timeData.g;

        float accumulatedPhase;
        float adjustedFactor;

        if (FRAMEINDEX == 0) {
            accumulatedPhase = 0.0;
            adjustedFactor = rotationFactor;
        } else {
            float prevPhase = prevRotData.r;
            float prevFactor = prevRotData.g;
            
            // Smooth the rotationFactor input
            adjustedFactor = mix(prevFactor, rotationFactor, min(1.0, TIMEDELTA * transitionSpeed));
            
            // The "frequency" of rotation is the animation speed divided by the rotation factor.
            // We integrate this frequency over time to get a smooth phase.
            float rotationFrequency = adjustedSpeed / adjustedFactor;
            accumulatedPhase = prevPhase + rotationFrequency * TIMEDELTA;
        }
        
        // Store accumulated phase in .r and the smoothed rotation factor in .g
        gl_FragColor = vec4(accumulatedPhase, adjustedFactor, 0.0, 1.0);

    } else {
        // PASS 2: FINAL RENDER
        // Renders the final visual using the smoothed time and phase from the persistent buffers.
        
        // Read the smooth, continuous values calculated in the previous passes
        float effectiveTime = IMG_NORM_PIXEL(timeBuffer, vec2(0.5)).r;
        float effectivePhase = IMG_NORM_PIXEL(rotationBuffer, vec2(0.5)).r;

        vec4 outColor = vec4(1.0, 1.0, 1.0, 1.0);
        vec4 h;
        vec2 u;

        for (float A, l, a, i = 0.6; i > 0.1; i -= 0.1) {
            // Calculate the angle 'a' for rotation.
            // Instead of using `effectiveTime / rotationFactor`, which causes jumps,
            // we use our smoothly accumulated `effectivePhase`.
            // The `+ i` is added to preserve the original visual's per-loop variation.
            a = effectivePhase + i;
            
            // Apply the same non-linear transformation from the original shader to the new angle
            a -= sin(a - sin(a));
            
            u = (isf_FragNormCoord * RENDERSIZE * 2. - RENDERSIZE) / RENDERSIZE.y;
            
            // Perform the feedback rotation using the smoothly calculated angle 'a'.
            // Note that there is no longer a division by rotationFactor here.
            l = max(length(u -= rotate2D(a) * clamp(u * rotate2D(a), -i, +i)), 0.1);
            
            A = min((l - 0.1) * RENDERSIZE.y * 0.2, 1.0);
            
            h = sin(i / 0.1 + a + vec4(1, 3, 5, 0)) * 0.2 + 0.7;
            outColor = mix(h, outColor, A) * mix(h / h, h + 0.5 * A * u.y / l, 0.1 / l);
        }

        gl_FragColor = outColor * colorTint * intensity;
    }
}
