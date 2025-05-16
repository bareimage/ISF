/*{
  "DESCRIPTION": "Converted Shadertoy shader with smoothed speed and rotation transitions, this shader must be rendered in desktop app ",
  "CREDIT": "Converted to ISF 2.0 with enhancements by dot2dot, original code by SnoopethDuckDuck (https://www.shadertoy.com/view/3fdGD7)",
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

#define C(U) cos(cos(U*i + t/i) + cos(U.yx*i) + o.x*i*i + t*i)/i/9.

void main() {
    // Declare variables at the top level for use in all passes
    vec4 prevTimeData, prevRotData;
    float accumulatedTime, currentSpeed, adjustedSpeed, newTime;
    float currentRotDir, prevRotDir;
    float effectiveTime, effectiveRotDir;
    vec2 R, u, v;
    vec4 o;
    float t, i, angle1, angle2, a, b, c, d;
    mat2 transform;
    
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
        // Second pass: Update the rotation direction with smooth transition
        prevRotData = IMG_NORM_PIXEL(rotationBuffer, vec2(0.5, 0.5));
        
        if (FRAMEINDEX == 0) {
            // Initialize on first frame
            currentRotDir = rotationDirection;
        } else {
            // Smoothly transition to target rotation direction
            prevRotDir = prevRotData.r;
            currentRotDir = mix(prevRotDir, rotationDirection, min(1.0, TIMEDELTA * transitionSpeed));
        }
        
        gl_FragColor = vec4(currentRotDir, 0.0, 0.0, 1.0);
    }
    else {
        // Final pass: Render the shader using the accumulated values
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        prevRotData = IMG_NORM_PIXEL(rotationBuffer, vec2(0.5, 0.5));
        
        // Get the accumulated values
        effectiveTime = prevTimeData.r;
        effectiveRotDir = prevRotData.r;
        
        R = RENDERSIZE.xy;
        u = isf_FragNormCoord.xy * R;
        v = u = 4.*(u+u-R)/R.y;
        u /= 1. + .013*dot(u,u);
        o = vec4(colorControl.rgb, 0.0);
        
        t = effectiveTime / 2.0;
        
        for (i = 1.0; i <= 19.0; i++) {
            o += cos(u.x + i + o.y*9. + t/i)/4./i;
            u += C(u) + C(u.yx);
            
            angle1 = effectiveRotDir * (i + length(u)*.3/i - t/2./i);
            angle2 = angle1 + 33.0;
            
            a = cos(angle1 + 0.0);
            b = cos(angle1 + 11.0);
            c = cos(angle1 + 33.0);
            d = cos(angle1 + 0.0);
            
            transform = 1.17 * mat2(a, b, c, d);
            
            u *= transform;
        }
             
        o = 1. + cos(o*3. + vec4(8,2,1.8,0));
        o = 1.1 - exp(-1.3*o*sqrt(o))
          + dot(v,v)*min(.02, 4e-6*exp(-.2*u.y));
        
        gl_FragColor = o;
    }
}
