/*{
"DESCRIPTION": "Truchet + Kaleidoscope effect with smooth transitions",
"CREDIT": "Original by Concept by @Mrange (https://www.shadertoy.com/view/7lKSWW), Patern Generation Modifications and ISF 2.0 conversion by @dot2dot, Materials are based on @mAlk (https://www.shadertoy.com/view/4sBXRt),",
"ISFVSN": "2.0",
"CATEGORIES": ["GENERATOR"],
"INPUTS": [
    {
        "NAME": "speed",
        "TYPE": "float",
        "DEFAULT": 1.0,
        "MIN": 0.0,
        "MAX": 20.0,
        "LABEL": "Animation Speed"
    },
    {
        "NAME": "kaleidoscopeAmount",
        "TYPE": "float",
        "DEFAULT": 6.0,
        "MIN": 1.0,
        "MAX": 32.0,
        "LABEL": "Kaleidoscope Segments"
    },
    {
        "NAME": "kaleidoscopeSpeed",
        "TYPE": "float",
        "DEFAULT": 0.5,
        "MIN": 0.0,
        "MAX": 5.0,
        "LABEL": "Kaleidoscope Transition Speed"
    },
    {
        "NAME": "rotationSpeed",
        "TYPE": "float",
        "DEFAULT": 0.5,
        "MIN": 0.0,
        "MAX": 2.0,
        "LABEL": "Rotation Speed"
    },
    {
        "NAME": "patternDelta",
        "TYPE": "float",
        "DEFAULT": 0.3,
        "MIN": 0.1,
        "MAX": 1.0,
        "LABEL": "Pattern Delta"
    },
    {
        "NAME": "deltaScale",
        "TYPE": "float",
        "DEFAULT": 0.3,
        "MIN": 0.01,
        "MAX": 1.0,
        "LABEL": "Delta Scale"
    },
    {
        "NAME": "deltaSmoothing",
        "TYPE": "float",
        "DEFAULT": 0.05,
        "MIN": 0.001,
        "MAX": 0.5,
        "LABEL": "Delta Smoothing"
    },
    {
        "NAME": "transitionSpeed",
        "TYPE": "float",
        "DEFAULT": 2.0,
        "MIN": 0.01,
        "MAX": 5.0,
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
        "TARGET": "deltaBuffer",
        "PERSISTENT": true,
        "FLOAT": true,
        "WIDTH": 1,
        "HEIGHT": 1
    },
    {
        "TARGET": "finalOutput"
    }
]}*/

// Constants
#define PI 3.141592654
#define TAU (2.0*PI)
#define ROT(a) mat2(cos(a), sin(a), -sin(a), cos(a))
#define PCOS(x) (0.5+0.5*cos(x))
#define MIPs (8.5+RENDERSIZE.y/512.0)


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

//This particular Shader took me a week of work. I tried as much as I can to smooth out PaternDelta, but it can still
//be too ubrupt, I am open to advises of the people in the comunity.

// Utility functions
float hash(float co) {
    return fract(sin(co*12.9898) * 13758.5453);
}

float hash(vec2 p) {
    float a = dot(p, vec2(127.1, 311.7));
    return fract(sin(a)*43758.5453123);
}

float tanh_approx(float x) {
    float x2 = x*x;
    return clamp(x*(27.0 + x2)/(27.0+9.0*x2), -1.0, 1.0);
}

float pmin(float a, float b, float k) {
    float h = clamp(0.5+0.5*(b-a)/k, 0.0, 1.0);
    return mix(b, a, h) - k*h*(1.0-h);
}

float pmax(float a, float b, float k) {
    return -pmin(-a, -b, k);
}

float pabs(float a, float k) {
    return pmax(a, -a, k);
}

float modMirror1(inout float p, float size) {
    float halfsize = size*0.5;
    float c = floor((p + halfsize)/size);
    p = mod(p + halfsize,size) - halfsize;
    p *= mod(c, 2.0)*2.0 - 1.0;
    return c;
}

// Coordinate conversion functions
vec2 toPolar(vec2 p) {
    float r = length(p);
    float theta = atan(p.y, p.x);
    if (theta < 0.0) theta += TAU;
    return vec2(r, theta);
}

vec2 toRect(vec2 p) {
    return vec2(p.x*cos(p.y), p.x*sin(p.y));
}

// Blending functions
vec4 alphaBlend(vec4 back, vec4 front) {
    // Ensure minimum alpha threshold to prevent disappearing
    front.w = max(front.w, 0.01);
    float w = front.w + back.w*(1.0-front.w);
    vec3 xyz = (front.xyz*front.w + back.xyz*back.w*(1.0-front.w))/w;
    return w > 0.0 ? vec4(xyz, w) : vec4(0.0, 0.0, 0.0, 0.01);
}

vec3 alphaBlend(vec3 back, vec4 front) {
    return mix(back, front.xyz, front.w);
}

// Post-processing
vec3 postProcess(vec3 col, vec2 q) {
    col = clamp(col, 0.0, 1.0);
    col = pow(col, vec3(1.0/2.2));
    col = col*0.6+0.4*col*col*(3.0-2.0*col);
    col = mix(col, vec3(dot(col, vec3(0.33))), -0.4);
    col *=0.5+0.5*pow(19.0*q.x*q.y*(1.0-q.x)*(1.0-q.y),0.7);
    return col;
}

// Camera movement functions
vec3 offset(float z, float time) {
    float a = z;
    vec2 p = -0.075*(vec2(cos(a/time), sin(a*sqrt(0.1))) + vec2(cos(a*sqrt(0.75)), sin(a*sqrt(0.5))));
    return vec3(p, z);
}

vec3 doffset(float z, float time) {
    float eps = 0.1;
    return 0.5*(offset(z + eps, time) - offset(z - eps, time))/time*eps;
}

vec3 ddoffset(float z, float time, float frameRate) {
    float eps = 0.01; // Fixed small value instead of time
    return (doffset(z + eps, time) - doffset(z - eps, time))/eps;
}

// Kaleidoscope function
vec2 applyKaleidoscope(vec2 uv, float segments, float kaleidoTime) {
    // Convert to polar coordinates
    float radius = length(uv);
    float angle = atan(uv.y, uv.x);
    
    // Ensure angle is positive
    if (angle < 0.0) {
        angle += TAU;
    }
    
    // Add rotation based on time
    angle += kaleidoTime * 0.2;
    
    // Use a fixed integer number of segments (no fractional segments)
    float intSegments = floor(segments);
    
    // Calculate the segment angle
    float segmentAngle = TAU / intSegments;
    
    // Determine which segment we're in
    float segmentIndex = floor(angle / segmentAngle);
    
    // Calculate the local angle within the segment
    float localAngle = angle - segmentIndex * segmentAngle;
    
    // Mirror the second half of each segment
    // This ensures perfect alignment at segment boundaries
    if (localAngle > segmentAngle * 0.5) {
        localAngle = segmentAngle - localAngle;
    }
    
    // Convert back to cartesian coordinates
    return vec2(cos(localAngle), sin(localAngle)) * radius;
}

// Gold material functions
float Sphere(in vec2 Coord, in vec2 Position, in float Size) {
    return 1.0-clamp(dot(Coord/Size-Position, Coord/Size-Position), 0.0, 1.0);
}

float SelectMip(in float Roughness) {
    return MIPs-1.0-(3.0-1.15*log2(Roughness));
}

vec2 Reflection(in vec2 Coord, in vec2 Position, in float Size, in float NormalZ) {
    return (1.0-Size*(Coord/Size-Position)/NormalZ)/2.0;
}

// Pattern generation functions
vec2 cell_df(float r, vec2 np, vec2 mp, vec2 off, float time) {
    const vec2 n0 = normalize(vec2(1.0, 1.0));
    const vec2 n1 = normalize(vec2(1.0, -1.0));
    
    np += off;
    mp -= off;
    
    float hh = hash(np);
    float h0 = hh;
    
    vec2 p0 = mp;
    p0 = abs(p0);
    p0 -= 0.5;
    float d0 = length(p0);
    float d1 = abs(d0-r);
    
    float dot0 = dot(n0, mp);
    float dot1 = dot(n1, mp);
    
    float d2 = abs(dot0);
    float t2 = dot1;
    d2 = abs(t2) > sqrt(0.5) ? d0 : d2;
    
    float d3 = abs(dot1);
    float t3 = dot0;
    d3 = abs(t3) > sqrt(0.5) ? d0 : d3;
    
    float d = d0;
    d = min(d, d1);
    
    if (h0 > .85) {
        d = min(d, d2);
        d = min(d, d3);
    }
    else if(h0 > 0.5) {
        d = min(d, d2);
    }
    else if(h0 > 0.15) {
        d = min(d, d3);
    }
    
    return vec2(d, d0-r);
}

vec2 truchet_df(float r, vec2 p, float time, float patternDeltaVal) {
    // Apply exponential smoothing to patternDeltaVal to dampen rapid changes
    // This creates a more gradual response to audio input
    float smoothedDelta = patternDeltaVal * deltaScale; // Use the parameter instead of hardcoded value
    
    // Base level
    vec2 np1 = floor(p + sin(smoothedDelta));
    vec2 mp1 = fract(p + sin(smoothedDelta)) - cos(smoothedDelta);
    vec2 d1 = cell_df(r, np1, mp1, vec2(0.0), time);
    
    // Second level (smaller scale)
    vec2 p2 = p * 3.0;
    vec2 np2 = floor(p2 + cos(smoothedDelta * 1.2)); // Reduced multiplier from 1.7 to 1.2
    vec2 mp2 = fract(p2 + cos(smoothedDelta * 1.2)) - sin(smoothedDelta * 1.2);
    vec2 d2 = cell_df(r * 0.33, np2, mp2, vec2(0.0), time * 1.5);
    
    // Third level (even smaller)
    vec2 p3 = p * 7.0;
    vec2 np3 = floor(p3 + sin(smoothedDelta * 0.3)); // Reduced multiplier from 0.5 to 0.3
    vec2 mp3 = fract(p3 + sin(smoothedDelta * 0.3)) - cos(smoothedDelta * 0.3);
    vec2 d3 = cell_df(r * 0.14, np3, mp3, vec2(0.0), time * 0.8);
    
    // Combine all levels
    float d = d1.x;
    d = min(d, d2.x + 0.03);
    d = min(d, d3.x + 0.06);
    
    return vec2(d, d1.y);
}

// Material calculation functions
vec3 calculateGoldMaterial(vec2 p, vec3 baseColor, float roughness, vec3 normal, float time) {
    // Create a procedural environment map
    vec3 envMap = vec3(
        0.8 + 0.2 * sin(p.x * 10.0 + time * 0.1), 
        0.8 + 0.2 * cos(p.y * 8.0 + time * 0.2),
        0.7 + 0.3 * sin((p.x + p.y) * 12.0 + time * 0.15)
    );
    
    // Light direction (animated slightly)
    vec3 lightDir = normalize(vec3(0.5 + 0.2 * sin(time * 0.3), 0.5 + 0.2 * cos(time * 0.2), 1.0));
    
    // View direction
    vec3 viewDir = vec3(0.0, 0.0, 1.0);
    
    // Calculate Fresnel term
    float fresnel = 1.0 - pow(clamp(dot(normal, viewDir), 0.0, 1.0), 1.0);
    
    // Simulate environment reflection
    vec3 environment = (1.0 + fresnel) * envMap;
    
    // Basic shading
    float diffuse = 0.75 + 0.25 * clamp(dot(normal, lightDir), 0.0, 1.0);
    
    // Specular highlight
    vec3 halfVec = normalize(lightDir + viewDir);
    float specPower = 1.0 / (pow(roughness + 0.1, 4.0));
    float spec = (1.0 - roughness) * pow(clamp(dot(normal, halfVec), 0.0, 1.0), specPower);
    
    // Combine for final gold color
    return mix(diffuse * baseColor * environment, 
               mix(normalize(baseColor) * 2.5, vec3(1.5), 0.4), 
               vec3(spec));
}

// Sky background
vec3 skyColor(vec3 ro, vec3 rd) {
    float d = pow(max(dot(rd, vec3(0.0, 0.0, 1.0)), 0.0), 20.0);
    return vec3(d);
}

// Plane rendering function
vec4 plane(vec3 ro, vec3 rd, vec3 pp, vec3 off, float aa, float n, float time, float kaleidoAmount, float rotSpeed, float kaleidoTime) {
    float h_ = hash(n);
    float h0 = fract(1777.0*h_);
    float h1 = fract(2087.0*h_);
    float h2 = fract(2687.0*h_);
    float h3 = fract(3167.0*h_);
    float h4 = fract(3499.0*h_);
    float l = length(pp - ro);
    vec3 hn;
    vec2 p = (pp-off*vec3(1.0, 1.0, 0.0)).xy;
    
    // Apply initial rotation
    p *= ROT(0.5*(h4 - 0.5)*time*rotSpeed);
    
    // Use fixed segment count - no variation to avoid morphing artifacts
    float segments = 2.0 * kaleidoAmount;
    
    // Apply the kaleidoscope effect using the improved function
    p = applyKaleidoscope(p, segments, kaleidoTime);
    
    // Apply additional rotation after kaleidoscope
    p *= ROT(TAU*h0+0.025*time*rotSpeed);
    
    float z = mix(0.2, 0.4, h3);
    p /= z;
    p+=0.5+floor(h1*1000.0);
    
    float tl = tanh_approx(0.33*l);
    float r = mix(0.30, 0.45, PCOS(0.1*n));
    
    // Use stable time value for each plane
    float stableTime = floor(time * 10.0) / 10.0 + n * 0.1;
    
    vec2 d2 = truchet_df(r, p, stableTime, patternDelta);
    d2 *= z;
    
    float d = d2.x;
    float lw = 0.025*z;
    d -= lw;
    
    // Calculate normal from the distance field for gold material
    vec2 eps = vec2(0.001, 0.0);
    vec3 normal = normalize(vec3(
        truchet_df(r, p + eps.xy, stableTime, patternDelta).x - 
        truchet_df(r, p - eps.xy, stableTime, patternDelta).x,
        truchet_df(r, p + eps.yx, stableTime, patternDelta).x - 
        truchet_df(r, p - eps.yx, stableTime, patternDelta).x,
        0.02
    ));
    
    // Choose gold color variation based on hash
    vec3 baseColor = mix(
        vec3(1.0, 0.76, 0.33),  // Yellow gold
        vec3(0.83, 0.68, 0.21), // Darker gold
        h2
    );
    
    // Vary roughness based on pattern
    float roughness = mix(0.2, 0.4, h3);
    
    // Calculate gold material
    vec3 goldColor = calculateGoldMaterial(p, baseColor, roughness, normal, time);
    
    // Apply pattern masking
    vec3 col = mix(vec3(0.0), goldColor, smoothstep(aa, -aa, d));
    
    // Add thin line highlights
    col = mix(col, vec3(1.0, 0.9, 0.5) * 1.5, 
              smoothstep(mix(1.0, -0.5, tl), 1.0, sin(PI*100.0*d)) * 0.5);
    
    // Apply pattern cutouts
    col = mix(col, vec3(0.0), step(d2.y, 0.0));
    float t = smoothstep(aa, -aa, -d2.y-3.0*lw)*mix(0.5, 1.0, smoothstep(aa, -aa, -d2.y-lw));
    
    return vec4(col, t);
}

// Main color calculation function
vec3 color(vec3 ww, vec3 uu, vec3 vv, vec3 ro, vec2 p, float time, float kaleidoAmount, float patternScale, float rotSpeed, float kaleidoTime) {
    float lp = length(p);
    vec2 np = p + 1.0/RENDERSIZE.xy;
    float rdd = (2.0+1.0*tanh_approx(lp));
    vec3 rd = normalize(p.x*uu + p.y*vv + rdd*ww);
    vec3 nrd = normalize(np.x*uu + np.y*vv + rdd*ww);
    
    const float planeDist = 1.0-0.25;
    const int furthest = 6;
    const int fadeFrom = max(furthest-5, 0);
    const float fadeDist = planeDist*float(furthest - fadeFrom);
    
    float nz = floor(ro.z / planeDist);
    
    vec3 skyCol = skyColor(ro, rd);
    vec4 acol = vec4(0.0);
    const float cutOff = 0.95;
    bool cutOut = false;
    
    // Improved plane rendering loop with better z-fighting prevention
    for (int i = 1; i <= furthest; ++i) {
        float pz = planeDist*nz + planeDist*float(i);
        
        // Improve z-fighting prevention with a better bias
        float pd = (pz - ro.z)/rd.z + 0.001 * float(i) * (1.0 + 0.1 * sin(time * 0.5));
        
        if (pd > 0.0 && acol.w < cutOff) {
            vec3 pp = ro + rd*pd;
            vec3 npp = ro + nrd*pd;
            
            // Increase antialiasing at edges
            float aa = 3.0*length(pp - npp);
            
            vec3 off = offset(pp.z, time);
            
            // Use a consistent time value for each plane with slight variation
            float stableTime = floor(time * 10.0) / 10.0 + float(i) * 0.1;
            
            vec4 pcol = plane(ro, rd, pp, off, aa, nz+float(i), stableTime, kaleidoAmount, rotSpeed, kaleidoTime);
            
            // Improve blending between planes
            float nz = pp.z-ro.z;
            float fadeIn = smoothstep(planeDist*float(furthest), planeDist*float(fadeFrom), nz);
            float fadeOut = smoothstep(0.0, planeDist*0.1, nz);
            
            pcol.xyz = mix(skyCol, pcol.xyz, fadeIn);
            pcol.w *= fadeOut;
            
            // Ensure minimum alpha to prevent disappearing but allow transparency at edges
            pcol.w = max(pcol.w, 0.01);
            pcol = clamp(pcol, 0.0, 1.0);
            
            acol = alphaBlend(pcol, acol);
        } else {
            cutOut = true;
            break;
        }
    }
    
    vec3 col = alphaBlend(skyCol, acol);
    return col;
}

// Main function
void main() {
    // First pass: Update the accumulated time in the persistent buffer
    if (PASSINDEX == 0) {
        vec4 prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        
        float accumulatedTime = prevTimeData.r;
        float currentSpeed = prevTimeData.g;
        float accumulatedKaleidoTime = prevTimeData.b;
        float currentKaleidoSpeed = prevTimeData.a;
        
        // Calculate new accumulated time
        float newTime;
        float adjustedSpeed;
        float newKaleidoTime;
        float adjustedKaleidoSpeed;
        
        if (FRAMEINDEX == 0) {
            // Initialize time on first frame
            newTime = 4.0;
            adjustedSpeed = speed;
            newKaleidoTime = 0.0;
            adjustedKaleidoSpeed = kaleidoscopeSpeed;
        } else {
            // Smoothly transition to target speed
            adjustedSpeed = mix(currentSpeed, speed, min(1.0, TIMEDELTA * transitionSpeed));
            newTime = accumulatedTime + adjustedSpeed * TIMEDELTA;
            
            // No transition for kaleidoscope speed - use it directly
            adjustedKaleidoSpeed = kaleidoscopeSpeed;
            newKaleidoTime = accumulatedKaleidoTime + adjustedKaleidoSpeed * TIMEDELTA;
        }
        
        // Store the accumulated time, current speed, kaleidoscope time and speed
        gl_FragColor = vec4(newTime, adjustedSpeed, newKaleidoTime, adjustedKaleidoSpeed);
    }
    // Second pass: Update the parameters with smooth transitions
    else if (PASSINDEX == 1) {
        vec4 prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5, 0.5));
        
        float currentKaleidoAmount, currentRotSpeed;
        
        if (FRAMEINDEX == 0) {
            // Initialize on first frame
            currentKaleidoAmount = kaleidoscopeAmount;
            currentRotSpeed = rotationSpeed;
        } else {
            // Extract previous parameter values
            currentKaleidoAmount = prevParamData.r;
            currentRotSpeed = prevParamData.b;
            
            // Apply smooth transitions
            currentKaleidoAmount = mix(currentKaleidoAmount, kaleidoscopeAmount, min(1.0, TIMEDELTA * transitionSpeed));
            currentRotSpeed = mix(currentRotSpeed, rotationSpeed, min(1.0, TIMEDELTA * transitionSpeed));
        }
        
        // Store the parameters (patternDelta is now handled in pass 2)
        gl_FragColor = vec4(currentKaleidoAmount, 0.0, currentRotSpeed, 1.0);
    }
    // Third pass: Handle patternDelta with extra smoothing
    else if (PASSINDEX == 2) {
        vec4 prevDeltaData = IMG_NORM_PIXEL(deltaBuffer, vec2(0.5, 0.5));
        
        float smoothedDelta;
        
        if (FRAMEINDEX == 0) {
            // Initialize on first frame
            smoothedDelta = patternDelta;
        } else {
            // Extract previous delta value
            smoothedDelta = prevDeltaData.r;
            
            // Apply extra strong smoothing for pattern delta
            // Use the dedicated deltaSmoothing parameter for fine control
            smoothedDelta = mix(smoothedDelta, patternDelta, min(1.0, TIMEDELTA * deltaSmoothing));
        }
        
        // Store the smoothed delta value
        gl_FragColor = vec4(smoothedDelta, 0.0, 0.0, 1.0);
    }
    // Final pass: Render the shader
    else {
        vec4 prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        vec4 prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5, 0.5));
        vec4 prevDeltaData = IMG_NORM_PIXEL(deltaBuffer, vec2(0.5, 0.5));
        
        // Get the accumulated values
        float effectiveTime = prevTimeData.r;
        float effectiveKaleidoTime = prevTimeData.b;
        float effectiveKaleidoAmount = prevParamData.r;
        float effectivePatternDelta = prevDeltaData.r; // Use the heavily smoothed delta
        float effectiveRotSpeed = prevParamData.b;
        
        vec2 q = isf_FragNormCoord.xy;
        vec2 p = -1. + 2. * q;
        p.x *= RENDERSIZE.x/RENDERSIZE.y;
        
        float tm = effectiveTime*0.25;
        vec3 ro = offset(tm, effectiveTime);
        vec3 dro = doffset(tm, effectiveTime);
        
        // Improved ddoffset call with fixed epsilon
        vec3 ddro = ddoffset(tm, effectiveTime, 60.0);
        
        vec3 ww = normalize(dro);
        vec3 uu = normalize(cross(normalize(vec3(0.0,1.0,0.0)+ddro), ww));
        vec3 vv = normalize(cross(ww, uu));
        
        vec3 col = color(ww, uu, vv, ro, p, effectiveTime, effectiveKaleidoAmount, effectivePatternDelta, effectiveRotSpeed, effectiveKaleidoTime);
        
        col *= smoothstep(0.0, 4.0, effectiveTime);
        col = postProcess(col, q);
        
        gl_FragColor = vec4(col, 1.0);
    }
}
