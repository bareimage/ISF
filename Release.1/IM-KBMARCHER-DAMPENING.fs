/*{
  "DESCRIPTION": "Converted Shadertoy shader with smoothed speed, rotation transitions, and zoom controls",
  "CREDIT": "Converted to ISF 2.0 with enhancements by dot2dot, original code by @ufffd (https://www.shadertoy.com/view/lcfXD8)",
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
      "NAME": "rotationSmoothness",
      "TYPE": "float",
      "DEFAULT": 5.0,
      "MIN": 0.5,
      "MAX": 20.0,
      "LABEL": "Rotation Smoothness"
    },
    {
      "NAME": "transitionSpeed",
      "TYPE": "float",
      "DEFAULT": 2.0,
      "MIN": 0.1,
      "MAX": 10.0,
      "LABEL": "Speed Transition Smoothness"
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
      "NAME": "zoomSmoothness",
      "TYPE": "float",
      "DEFAULT": 3.0,
      "MIN": 0.1,
      "MAX": 10.0,
      "LABEL": "Zoom Smoothness"
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

#define SS(a,b,c) smoothstep(a-b,a+b,c)
#define gyr(p) dot(sin(p.xyz),cos(p.zxy))

float map(in vec3 p, float T) {
    return (1. + .2*sin(p.y*600.)) * 
    gyr(( p*(10.) + .8*gyr(( p*8. )) )) *
    (1.+sin(T+length(p.xy)*10.)) + 
    .3 * sin(T*.15 + p.z * 5. + p.y) *
    (2.+gyr(( p*(sin(T*.2+p.z*3.)*350.+250.) )));
}

vec3 norm(in vec3 p, float T) {
    float m = map(p, T);
    vec2 d = vec2(.06+.06*sin(p.z),0.);
    return map(p, T)-vec3(
        map(p-d.xyy, T),map(p-d.yxy, T),map(p-d.yyx, T)
    );
}

void main() {
    // Declare variables at the top level for use in all passes
    vec4 prevTimeData, prevRotData, prevZoomData;
    float accumulatedTime, currentSpeed, adjustedSpeed, newTime;
    float currentRotDir, prevRotDir, effectiveRotDir;
    float currentZoom, prevZoom, effectiveZoom;
    float effectiveTime;
    vec2 R, uv, uvc, zoomedUvc;
    vec3 p, rd, n;
    float d, dd, bw;
    
    if (PASSINDEX == 0) {
        // First pass: Update the accumulated time in the persistent buffer
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        
        // Extract previous accumulated time
        accumulatedTime = prevTimeData.r;
        currentSpeed = prevTimeData.g;
        
        // Calculate new accumulated time
        if (FRAMEINDEX == 0) {
            // Initialize time on first frame
            newTime = 0.0;
            adjustedSpeed = speed;
        } else {
            // Smoothly transition to target speed
            adjustedSpeed = mix(currentSpeed, speed, min(1.0, TIMEDELTA * transitionSpeed));
            
            newTime = accumulatedTime + adjustedSpeed * TIMEDELTA;
        }
        
        // Store the accumulated time and current speed
        gl_FragColor = vec4(newTime, adjustedSpeed, 0.0, 1.0);
    }
    else if (PASSINDEX == 1) {
        // Second pass: Update the rotation direction with more aggressive smooth transition
        prevRotData = IMG_NORM_PIXEL(rotationBuffer, vec2(0.5, 0.5));
        
        if (FRAMEINDEX == 0) {
            // Initialize on first frame
            currentRotDir = rotationDirection;
        } else {
            // More aggressive smoothing for rotation
            prevRotDir = prevRotData.r;
            currentRotDir = mix(prevRotDir, rotationDirection, min(1.0, TIMEDELTA * rotationSmoothness));
        }
        
        gl_FragColor = vec4(currentRotDir, 0.0, 0.0, 1.0);
    }
    else if (PASSINDEX == 2) {
        // Third pass: Update the zoom level with smooth transition
        prevZoomData = IMG_NORM_PIXEL(zoomBuffer, vec2(0.5, 0.5));
        
        if (FRAMEINDEX == 0) {
            // Initialize on first frame
            currentZoom = zoom;
        } else {
            // Smoothly transition to target zoom
            prevZoom = prevZoomData.r;
            currentZoom = mix(prevZoom, zoom, min(1.0, TIMEDELTA * zoomSmoothness));
        }
        
        gl_FragColor = vec4(currentZoom, 0.0, 0.0, 1.0);
    }
    else {
        // Final pass: Render the shader using the accumulated values
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        prevRotData = IMG_NORM_PIXEL(rotationBuffer, vec2(0.5, 0.5));
        prevZoomData = IMG_NORM_PIXEL(zoomBuffer, vec2(0.5, 0.5));
        
        // Get the accumulated values
        effectiveTime = prevTimeData.r;
        effectiveRotDir = prevRotData.r;
        effectiveZoom = prevZoomData.r;
        
        R = RENDERSIZE.xy;
        uv = isf_FragNormCoord.xy * R;
        
        // Center the coordinates for proper zooming
        uvc = (uv - R/2.0) / R.y;
        
        // Apply zoom effect - divide by zoom factor to zoom in
        zoomedUvc = uvc / effectiveZoom;
        
        // Apply rotation to the zoomed coordinates
        float angle = effectiveRotDir * effectiveTime * 0.2;
        mat2 rotation = mat2(cos(angle), -sin(angle), sin(angle), cos(angle));
        zoomedUvc = rotation * zoomedUvc;
        
        d = 0.;
        dd = 1.;
        p = vec3(0., 0., effectiveTime/4.);
        rd = normalize(vec3(zoomedUvc.xy, 1.));
        
        // Ray marching loop
        for (float i = 0.; i < 90. && dd > .001 && d < 2.; i++) {
            d += dd;
            p += rd * d;
            dd = map(p, effectiveTime) * .02;
        }
        
        n = norm(p, effectiveTime);
        bw = n.x + n.y;
        bw *= SS(.9, .15, 1./d);
        
        // Apply color tinting with zoom-based intensity
        float colorIntensity = 0.3 + 0.2 * sin(effectiveTime * 0.2) + 0.1 * (effectiveZoom - 1.0);
        vec3 finalColor = mix(vec3(bw), colorControl.rgb, clamp(colorIntensity, 0.0, 0.7));
        
        gl_FragColor = vec4(finalColor, 1.0);
    }
}
