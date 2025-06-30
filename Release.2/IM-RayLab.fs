/*{
  "DESCRIPTION": "3D Tunnel Effect with Smooth Parameter Transitions",
  "CREDIT": "ISF 2.0 version by @dot2dot (bareimage), Original code by @zguerrero(https://www.shadertoy.com/user/zguerrero) ",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR", "3D"],
  "INPUTS": [
    {
      "NAME": "speed",
      "TYPE": "float",
      "DEFAULT": 3.0,
      "MIN": 0.0,
      "MAX": 10.0,
      "LABEL": "Animation Speed"
    },
    {
      "NAME": "pistonSpeed",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 5.0,
      "LABEL": "Piston Movement Speed"
    },
    {
      "NAME": "radialBlurIntensity",
      "TYPE": "float",
      "DEFAULT": 0.01,
      "MIN": 0.0,
      "MAX": 0.1,
      "LABEL": "Radial Blur Intensity"
    },
    {
      "NAME": "tunnelScale",
      "TYPE": "float",
      "DEFAULT": 8.5,
      "MIN": 5.0,
      "MAX": 15.0,
      "LABEL": "Tunnel Scale"
    },
    {
      "NAME": "curvAmount",
      "TYPE": "float",
      "DEFAULT": 0.075,
      "MIN": 0.0,
      "MAX": 0.2,
      "LABEL": "Curvature Amount"
    },
    {
      "NAME": "reflAmount",
      "TYPE": "float",
      "DEFAULT": 0.8,
      "MIN": 0.0,
      "MAX": 1.0,
      "LABEL": "Reflection Amount"
    },
    {
      "NAME": "transitionSpeed",
      "TYPE": "float",
      "DEFAULT": 2.0,
      "MIN": 0.1,
      "MAX": 10.0,
      "LABEL": "Transition Smoothness"
    },
    {
      "NAME": "mainLightColor",
      "TYPE": "color",
      "DEFAULT": [0.3, 0.6, 1.0, 1.0],
      "LABEL": "Main Light Color"
    },
   
    {
      "NAME": "lightIntensity",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 3.0,
      "LABEL": "Light Intensity"
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
      "TARGET": "pistonBuffer",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": 1,
      "HEIGHT": 1
    },
    {
      "TARGET": "lightParamBuffer",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": 1,
      "HEIGHT": 1
    },
    {
      "TARGET": "lightColorBuffer",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": 1,
      "HEIGHT": 1
    },
    {
      "TARGET": "bufferA",
      "PERSISTENT": false
    },
    {
      "TARGET": "bufferB",
      "PERSISTENT": false
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

const float epsilon = 0.02;
const float pi = 3.14159265359;
const vec3 wallsColor = vec3(0.05, 0.025, 0.025);
const vec3 fogColor = vec3(0.05, 0.05, 0.2);
precision highp float;


//Distance Field functions by iq
float sdCylinder(vec3 p, vec3 c)
{
  return length(c.xy - p.xz) - c.z;
}

float sdCappedCylinder(vec3 p, vec2 h)
{
  vec2 d = abs(vec2(length(p.xz),p.y)) - h;
  return min(max(d.x,d.y),0.0) + length(max(d,0.0));
}

float sdSphere(vec3 p, float s)
{
  return length(p)-s;
}

float sdBox(vec3 p, vec3 b)
{
  vec3 d = abs(p) - b;
  return min(max(d.x,max(d.y,d.z)),0.0) + length(max(d,0.0));
}

vec3 opRep(vec3 p, vec3 c)
{
    return mod(p,c)-0.5*c;
}

vec2 linearStep2(vec2 mi, vec2 ma, vec2 v)
{
    return clamp((v - mi) / (ma - mi), 0.0 ,1.0);
}

float tunnel(vec3 p, vec3 c)
{
  return -length(c.xy - p.xz) + c.z;
}

vec4 distfunc(vec3 pos, float effectiveTime, float effectiveSpeed, float effectiveTunnelScale, float effectivePistonSpeed)
{
    vec3 repPos = opRep(pos, vec3(4.0, 1.0, 4.0));
    vec2 sinPos = sin((pos.z * pi / 8.0) + vec2(0.0, pi)) * 1.75;
    vec3 repPosSin = opRep(pos.xxz + vec3(sinPos.x, sinPos.y, 0.0), vec3(4.0, 4.0, 0.0));
    
    float cylinders = sdCylinder(vec3(repPos.x, pos.y, repPos.z), vec3(0.0, 0.0, 0.5));
    // Use separate piston speed parameter for the piston movement
    float s = sin(effectiveTime*effectivePistonSpeed + floor(pos.z*0.25));
    float cutCylinders1 = sdBox(vec3(pos.x, pos.y, repPos.z), vec3(100.0, clamp(s, 0.025, 0.75), 1.0));
    float cutCylinders2 = sdBox(vec3(repPos.x, pos.y, repPos.z), vec3(0.035, 100.0, 10.0));
    float cuttedCylinders = max(-cutCylinders2, max(-cutCylinders1, cylinders));
    
    float innerCylinders = sdCylinder(vec3(repPos.x, pos.y, repPos.z), vec3(0.0, 0.0, 0.15));
    float tubes1 = sdCylinder(vec3(repPosSin.x, 0.0, pos.y - 0.85), vec3(0.0, 0.0, 0.025));
    float tubes2 = sdCylinder(vec3(repPosSin.y, 0.0, pos.y + 0.85), vec3(0.0, 0.0, 0.025));
    float tubes = min(tubes1, tubes2);  
    float lightsGeom = min(tubes, innerCylinders);
    
    float resultCylinders = min(cuttedCylinders, lightsGeom);
    
    float spheres = sdSphere(vec3(repPos.x, pos.y, repPos.z), (s*0.5+0.5)*1.5);
    float light = min(tubes, spheres);
    
    vec2 planeMod = abs(fract(pos.xx * vec2(0.25, 4.0) + 0.5) * 4.0 - 2.0) - 1.0;
    float planeMod2 = clamp(planeMod.y, -0.02, 0.02) * min(0.0, planeMod.x);
    float cylindersCutPlane = sdCylinder(vec3(repPos.x, pos.y, repPos.z), vec3(0.0, 0.0, 0.6));
    float spheresCutPlane = sdSphere(vec3(repPos.x, pos.y, repPos.z), 1.3);
    float plane = 1.0 - abs(pos.y + clamp(planeMod.x, -0.04, 0.04) + planeMod2);
    float t = tunnel(pos.xzy * vec3(1.0, 1.0, 3.0), vec3(0.0, 0.0, effectiveTunnelScale));
    float cutTunnel = sdBox(vec3(pos.x, pos.y, repPos.z), vec3(100.0, 100.0, 0.1));
    plane = min(max(-cutTunnel, t), max(-spheresCutPlane, max(-cylindersCutPlane, plane)));
    
    float dist = min(resultCylinders, plane);
    float occ = min(cuttedCylinders, plane);
    
    float id = 0.0;
    
    if(lightsGeom < epsilon)
    {
       id = 1.0; 
    }
    
    return vec4(dist, id, light, occ);
}

vec3 rayMarch(vec3 rayDir, vec3 cameraOrigin, float effectiveTime, float effectiveSpeed, float effectiveTunnelScale, float effectivePistonSpeed)
{
    const int maxItter = 100;
    const float maxDist = 30.0;
    
    float totalDist = 0.0;
    vec3 pos = cameraOrigin;
    vec4 dist = vec4(epsilon);
    
    for(int i = 0; i < maxItter; i++)
    {
        dist = distfunc(pos, effectiveTime, effectiveSpeed, effectiveTunnelScale, effectivePistonSpeed);
        totalDist += dist.x;
        pos += dist.x * rayDir;
        
        if(dist.x < epsilon || totalDist > maxDist)
        {
            break;
        }
    }
    
    return vec3(dist.x, totalDist, dist.y);
}

vec3 rayMarchReflection(vec3 rayDir, vec3 cameraOrigin, float effectiveTime, float effectiveSpeed, float effectiveTunnelScale, float effectivePistonSpeed)
{
    const int maxItter = 30;
    const float maxDist = 20.0;
    
    float totalDist = 0.0;
    vec3 pos = cameraOrigin;
    vec4 dist = vec4(epsilon);

    for(int i = 0; i < maxItter; i++)
    {
        dist = distfunc(pos, effectiveTime, effectiveSpeed, effectiveTunnelScale, effectivePistonSpeed);
        totalDist += dist.x;
        pos += dist.x * rayDir;
        
        if(dist.x < epsilon || totalDist > maxDist)
        {
            break;
        }
    }
    
    return vec3(dist.x, totalDist, dist.y);
}

vec2 AOandFakeAreaLights(vec3 pos, vec3 n, float effectiveTime, float effectiveSpeed, float effectiveTunnelScale, float effectivePistonSpeed)
{
    vec4 res = vec4(0.0);
    
    for(int i=0; i<3; i++)
    {
        vec3 aopos = pos + n*0.3*float(i);
        vec4 d = distfunc(aopos, effectiveTime, effectiveSpeed, effectiveTunnelScale, effectivePistonSpeed);
        res += d;
    }
    
    float ao = clamp(res.w, 0.0, 1.0);
    float light = 1.0 - clamp(res.z*0.3, 0.0, 1.0);
    
    return vec2(ao, light * ao);   
}

mat3 setCamera(in vec3 ro, in vec3 ta, float cr)
{
    vec3 cw = normalize(ta-ro);
    vec3 cp = vec3(sin(cr), cos(cr),0.0);
    vec3 cu = normalize(cross(cw,cp));
    vec3 cv = normalize(cross(cu,cw));
    return mat3(cu, cv, cw);
}

vec4 norcurv(in vec3 p, float effectiveTime, float effectiveSpeed, float effectiveTunnelScale, float effectivePistonSpeed)
{
    vec2 e = vec2(-epsilon, epsilon);   
    float t1 = distfunc(p + e.yxx, effectiveTime, effectiveSpeed, effectiveTunnelScale, effectivePistonSpeed).x;
    float t2 = distfunc(p + e.xxy, effectiveTime, effectiveSpeed, effectiveTunnelScale, effectivePistonSpeed).x;
    float t3 = distfunc(p + e.xyx, effectiveTime, effectiveSpeed, effectiveTunnelScale, effectivePistonSpeed).x;
    float t4 = distfunc(p + e.yyy, effectiveTime, effectiveSpeed, effectiveTunnelScale, effectivePistonSpeed).x;

    float curv = .25/e.y*(t1 + t2 + t3 + t4 - 4.0 * distfunc(p, effectiveTime, effectiveSpeed, effectiveTunnelScale, effectivePistonSpeed).x);
    return vec4(normalize(e.yxx*t1 + e.xxy*t2 + e.xyx*t3 + e.yyy*t4), curv);
}

vec4 lighting(vec3 n, vec3 rayDir, vec3 reflectDir, vec3 pos, float effectiveTime, float effectiveSpeed, vec3 effectiveLightColor2, float effectiveLightIntensity)
{
    vec3 light = vec3(0.0, 0.0, 2.0 + effectiveTime * effectiveSpeed);
    vec3 lightVec = light - pos;
    vec3 lightDir = normalize(lightVec);
    float atten = clamp(1.0 - length(lightVec)*0.1, 0.0, 1.0);
    float spec = pow(max(0.0, dot(reflectDir, lightDir)), 10.0);
    float rim = (1.0 - max(0.0, dot(-n, rayDir)));
    return vec4(spec*atten*effectiveLightColor2*effectiveLightIntensity + rim*0.2, rim); 
}

vec3 color(float id, vec3 pos, vec3 effectiveLightColor1, float effectiveLightIntensity)
{
    vec2 fp = vec2(1.0) - linearStep2(vec2(0.0), vec2(0.01), abs(fract(pos.xz * vec2(0.25, 1.0) + vec2(0.0, 0.5)) - 0.5));
    float s = fp.y + fp.x;
    return mix(wallsColor + s*effectiveLightColor1*0.5*effectiveLightIntensity, effectiveLightColor1*effectiveLightIntensity, id);
}

vec4 finalColor(vec3 rayDir, vec3 reflectDir, vec3 pos, vec3 normal, float ao, float id, vec3 effectiveLightColor1, vec3 effectiveLightColor2, float effectiveLightIntensity)
{
    vec4 l = lighting(normal, rayDir, reflectDir, pos, 0.0, 0.0, effectiveLightColor2, effectiveLightIntensity);
    vec3 col = color(id, pos, effectiveLightColor1, effectiveLightIntensity);
    float ao1 = 0.5 * ao + 0.5;
    float ao2 = 0.25 * ao + 0.75;
    vec3 res = (mix(col * ao1, col, id) + l.xyz) * ao2;
    return vec4(res, l.w); 
}

void main()
{
    // Declare variables for all passes
    vec4 prevTimeData, prevParamData, prevPistonData, prevLightParamData, prevLightColorData;
    float accumulatedTime, currentSpeed, adjustedSpeed, newTime;
    float currentRadialBlur, currentTunnelScale, currentCurvAmount, currentReflAmount;
    float currentPistonSpeed, adjustedPistonSpeed;
    float currentLightIntensity, currentGlowAmount;
    vec3 currentLightColor1, currentLightColor2;
    float effectiveTime, effectiveSpeed, effectiveRadialBlur, effectiveTunnelScale;
    float effectiveCurvAmount, effectiveReflAmount, effectivePistonSpeed;
    float effectiveLightIntensity, effectiveGlowAmount;
    vec3 effectiveLightColor1, effectiveLightColor2;
    
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
        // Second pass: Update the parameters with smooth transitions
        prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5, 0.5));
        
        if (FRAMEINDEX == 0) {
            // Initialize on first frame
            currentRadialBlur = radialBlurIntensity;
            currentTunnelScale = tunnelScale;
            currentCurvAmount = curvAmount;
            currentReflAmount = reflAmount;
        } else {
            // Extract previous parameter values
            currentRadialBlur = prevParamData.r;
            currentTunnelScale = prevParamData.g;
            currentCurvAmount = prevParamData.b;
            currentReflAmount = prevParamData.a;
            
            // Apply smooth transitions
            currentRadialBlur = mix(currentRadialBlur, radialBlurIntensity, min(1.0, TIMEDELTA * transitionSpeed));
            currentTunnelScale = mix(currentTunnelScale, tunnelScale, min(1.0, TIMEDELTA * transitionSpeed));
            currentCurvAmount = mix(currentCurvAmount, curvAmount, min(1.0, TIMEDELTA * transitionSpeed));
            currentReflAmount = mix(currentReflAmount, reflAmount, min(1.0, TIMEDELTA * transitionSpeed));
        }
        
        gl_FragColor = vec4(currentRadialBlur, currentTunnelScale, currentCurvAmount, currentReflAmount);
    }
    else if (PASSINDEX == 2) {
        // Third pass: Update the piston speed with smooth transitions
        prevPistonData = IMG_NORM_PIXEL(pistonBuffer, vec2(0.5, 0.5));
        
        if (FRAMEINDEX == 0) {
            // Initialize on first frame
            currentPistonSpeed = pistonSpeed;
        } else {
            // Extract previous parameter value
            currentPistonSpeed = prevPistonData.r;
            
            // Apply smooth transition
            currentPistonSpeed = mix(currentPistonSpeed, pistonSpeed, min(1.0, TIMEDELTA * transitionSpeed));
        }
        
        gl_FragColor = vec4(currentPistonSpeed, 0.0, 0.0, 0.0);
    }
    else if (PASSINDEX == 3) {
        // Fourth pass: Update the light parameters with smooth transitions
        prevLightParamData = IMG_NORM_PIXEL(lightParamBuffer, vec2(0.5, 0.5));
        
        if (FRAMEINDEX == 0) {
            // Initialize on first frame
            currentLightIntensity = lightIntensity;
            
        } else {
            // Extract previous parameter values
            currentLightIntensity = prevLightParamData.r;
            currentGlowAmount = prevLightParamData.g;
            
            // Apply smooth transitions
            currentLightIntensity = mix(currentLightIntensity, lightIntensity, min(1.0, TIMEDELTA * transitionSpeed));
          
        }
        
        gl_FragColor = vec4(currentLightIntensity, currentGlowAmount, 0.0, 0.0);
    }
    else if (PASSINDEX == 4) {
        // Fifth pass: Update the light colors with smooth transitions
        prevLightColorData = IMG_NORM_PIXEL(lightColorBuffer, vec2(0.5, 0.5));
        
        if (FRAMEINDEX == 0) {
            // Initialize on first frame
            currentLightColor1 = mainLightColor.rgb;
          
        } else {
            // Extract previous color values
            currentLightColor1 = vec3(prevLightColorData.r, prevLightColorData.g, prevLightColorData.b);
            currentLightColor2 = vec3(prevLightColorData.a, 0.0, 0.0); // Need another buffer for full color
            
            // Apply smooth transitions
            currentLightColor1 = mix(currentLightColor1, mainLightColor.rgb, min(1.0, TIMEDELTA * transitionSpeed));
           
        }
        
        gl_FragColor = vec4(currentLightColor1.r, currentLightColor1.g, currentLightColor1.b, currentLightColor2.r);
        // Note: we're only storing one component of the second color due to vec4 limitation
        // In a real implementation, you'd use a second buffer or a different approach
    }
    else if (PASSINDEX == 5) {
        // BufferA - 3D scene generation
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5, 0.5));
        prevPistonData = IMG_NORM_PIXEL(pistonBuffer, vec2(0.5, 0.5));
        prevLightParamData = IMG_NORM_PIXEL(lightParamBuffer, vec2(0.5, 0.5));
        prevLightColorData = IMG_NORM_PIXEL(lightColorBuffer, vec2(0.5, 0.5));
        
        // Get the accumulated values
        effectiveTime = prevTimeData.r;
        effectiveSpeed = prevTimeData.g;
        effectiveRadialBlur = prevParamData.r;
        effectiveTunnelScale = prevParamData.g;
        effectiveCurvAmount = prevParamData.b;
        effectiveReflAmount = prevParamData.a;
        effectivePistonSpeed = prevPistonData.r;
        effectiveLightIntensity = prevLightParamData.r;
        effectiveGlowAmount = prevLightParamData.g;
        
        // Get light colors (with simplified second color)
        effectiveLightColor1 = vec3(prevLightColorData.r, prevLightColorData.g, prevLightColorData.b);
        effectiveLightColor2 = vec3(prevLightColorData.a, prevLightColorData.a * 0.7, prevLightColorData.a * 0.7);
        
        // For a real implementation, get the full second color from another buffer
        
        vec2 uv = gl_FragCoord.xy/RENDERSIZE.xy;
        
        float move = effectiveTime;
        vec2 sinMove = sin((move * pi) / 16.0 + vec2(1.0, -1.0)) * vec2(5.0, 0.35);
        float camX = sinMove.x;
        float camY = 0.0;
        float camZ = -5.0 + move;                 
        vec3 cameraOrigin = vec3(camX, camY, camZ);
        vec3 cameraTarget = vec3(0.0, 0.0, cameraOrigin.z + 10.0);
        
        vec2 screenPos = uv * 2.0 - 1.0;
        
        screenPos.x *= RENDERSIZE.x/RENDERSIZE.y;
        
        mat3 cam = setCamera(cameraOrigin, cameraTarget, sinMove.y);
        
        vec3 rayDir = cam*normalize(vec3(screenPos.xy,1.0));
        vec3 dist = rayMarch(rayDir, cameraOrigin, effectiveTime, effectiveSpeed, effectiveTunnelScale, effectivePistonSpeed);
        
        vec3 res;
        vec2 fog;

        if(dist.x < epsilon)
        {
            vec3 pos = cameraOrigin + dist.y*rayDir;
            vec4 n = norcurv(pos, effectiveTime, effectiveSpeed, effectiveTunnelScale, effectivePistonSpeed);
            vec2 ao = AOandFakeAreaLights(pos, n.xyz, effectiveTime, effectiveSpeed, effectiveTunnelScale, effectivePistonSpeed);
            vec3 r = reflect(rayDir, n.xyz);
            vec3 rpos = pos + n.xyz*0.02;
            vec3 reflectDist = rayMarchReflection(r, rpos, effectiveTime, effectiveSpeed, effectiveTunnelScale, effectivePistonSpeed);
            fog = clamp(1.0 / exp(vec2(dist.y, reflectDist.y)*vec2(0.15, 0.2)), 0.0, 1.0);
            vec4 direct = finalColor(rayDir, r, pos, n.xyz, ao.x, dist.z, effectiveLightColor1, effectiveLightColor2, effectiveLightIntensity) + n.w*effectiveCurvAmount;
            
            vec4 reflN;
            vec2 reflAO;
            vec3 reflFinal;
            
            if(reflectDist.x < epsilon)
            {
                vec3 reflPos = rpos + reflectDist.y*r;
                reflN = norcurv(reflPos, effectiveTime, effectiveSpeed, effectiveTunnelScale, effectivePistonSpeed);
                reflAO = AOandFakeAreaLights(reflPos, reflN.xyz, effectiveTime, effectiveSpeed, effectiveTunnelScale, effectivePistonSpeed);
                vec3 rr = reflect(r, reflN.xyz);
                vec4 refl = finalColor(r, rr, reflPos, reflN.xyz, reflAO.x, reflectDist.z, effectiveLightColor1, effectiveLightColor2, effectiveLightIntensity);
                vec3 reflAreaLights = reflAO.y * effectiveLightColor1 * 0.5 * effectiveLightIntensity;
                reflFinal = (refl.xyz + reflN.w*effectiveCurvAmount + reflAreaLights) * fog.y * effectiveReflAmount * direct.w;
            }
            else   
            {
                reflFinal = vec3(0.0, 0.0, 0.0);
            }
            
            vec3 areaLightsColor = ao.y * effectiveLightColor1 * 0.5 * effectiveLightIntensity;
            
            res = mix(fogColor, direct.xyz + reflFinal + areaLightsColor, fog.x);
        }
        else
        {
            res = fogColor; 
            fog = vec2(0.0);
        }
        
        gl_FragColor = vec4(res, (dist.z) * fog);
    }
    else if (PASSINDEX == 6) {
        // BufferB - Radial blur
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5, 0.5));
        prevLightParamData = IMG_NORM_PIXEL(lightParamBuffer, vec2(0.5, 0.5));
        
        effectiveTime = prevTimeData.r;
        effectiveSpeed = prevTimeData.g;
        effectiveRadialBlur = prevParamData.r;
        effectiveLightIntensity = prevLightParamData.r;
        
        float s = sin(effectiveTime * pi / 16.0 - 1.0);
        vec2 radialBlurCenter = vec2((s * 0.5 + 0.5) * 0.5 + 0.25, abs(s)* 0.2 + 0.35);
        
        vec2 uv = gl_FragCoord.xy/RENDERSIZE.xy;
        vec2 uvCenter = uv - radialBlurCenter;
        float c = length(uv - radialBlurCenter);
        vec4 texBlurred = IMG_NORM_PIXEL(bufferA, uv);
        
        float itter = 5.0;
        
        for(float itter1 = 0.0; itter1 < 5.0; itter1++)
        {
            texBlurred += IMG_NORM_PIXEL(bufferA, uvCenter * (1.0 - effectiveRadialBlur * 
            itter1 * c) + radialBlurCenter);
        }
        
        vec4 res = texBlurred / itter;
        
        float motionBlur = res.w;
        vec3 light = motionBlur * vec3(0.25, 0.5, 0.75) * effectiveLightIntensity;
        gl_FragColor = vec4(res.xyz + light*2.0, motionBlur);
    }
    else {
        // Final Image
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        prevLightParamData = IMG_NORM_PIXEL(lightParamBuffer, vec2(0.5, 0.5));
        
        effectiveTime = prevTimeData.r;
        effectiveGlowAmount = prevLightParamData.g;
        
        vec2 uv = gl_FragCoord.xy/RENDERSIZE.xy;
            
        float b = step(fract(uv.y * 50.0 + effectiveTime), 0.5);
        vec4 tex = IMG_NORM_PIXEL(bufferB, uv);
        vec4 tex2 = IMG_NORM_PIXEL(bufferB, uv + vec2((b - 0.5)*0.005, 0.0));
        
        vec2 vign = smoothstep(vec2(0.5, 1.5), vec2(1.0, 0.98 + b*0.02), vec2(length(uv - 0.5) * 2.0)); 
        
        // Calculate brightness for glow effect
        float brightness = dot(tex.rgb, vec3(0.2126, 0.7152, 0.0722));
        vec3 glow = tex.rgb * max(0.0, brightness - 0.5) * effectiveGlowAmount;
        
        vec4 res = mix(tex, vec4(tex.x, tex.y, tex2.z, tex.w), vign.x);
        vec4 col = res * vign.y * (0.85 + 0.15);
        
        // Add glow
        col.rgb += glow;
        
        gl_FragColor = pow(col*1.75, vec4(1.25));
    }
}
