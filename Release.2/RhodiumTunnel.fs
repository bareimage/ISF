/*{
"DESCRIPTION": "Tunnel effect from Rhodium 4k Intro, converted to ISF 2.0",
"CREDIT": "Original by Jochen 'Virgill' Feldkötter (https://www.youtube.com/channel/UCruCDhw5mbPXh0Wqt5IRong), ISF 2.0 Version by @dot2dot (bareimage), ",
"ISFVSN": "2.0",
"CATEGORIES": ["GENERATOR"],
"INPUTS": [
    {
        "NAME": "speed",
        "TYPE": "float",
        "DEFAULT": 1.0,
        "MIN": 0.0,
        "MAX": 5.0,
        "LABEL": "Animation Speed"
    },
    {
        "NAME": "intensity",
        "TYPE": "float",
        "DEFAULT": 0.1,
        "MIN": 0.01,
        "MAX": 0.5,
        "LABEL": "Glow Intensity"
    },
    {
        "NAME": "wobbleAmount",
        "TYPE": "float",
        "DEFAULT": 0.3,
        "MIN": 0.0,
        "MAX": 1.0,
        "LABEL": "Camera Wobble"
    },
    {
        "NAME": "baseColor",
        "TYPE": "color",
        "DEFAULT": [1.0, 0.25, 0.0625, 1.0],
        "LABEL": "Base Color"
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

// Utility functions
float bounce = 0.;

// rotation
void pR(inout vec2 p, float a) 
{
    p = cos(a)*p + sin(a)*vec2(p.y, -p.x);
}

// 3D noise function (IQ)
float noise(vec3 p)
{
    vec3 ip = floor(p);
    p -= ip; 
    vec3 s = vec3(7, 157, 113);
    vec4 h = vec4(0., s.yz, s.y+s.z) + dot(ip, s);
    p = p*p*(3.-2.*p); 
    h = mix(fract(sin(h)*43758.5), fract(sin(h+s.x)*43758.5), p.x);
    h.xy = mix(h.xz, h.yw, p.y);
    return mix(h.x, h.y, p.z); 
}

float map(vec3 p, float effectiveTime, float bounce)
{   
    // tunnel    
    p.z += (3.-sin(0.314*effectiveTime+1.1));
    pR(p.zy, 1.57);
    return mix(length(p.xz)-.2, length(vec3(p.x, abs(p.y)-1.3, p.z))-.2, step(1.3, abs(p.y))) - 0.1*noise(8.*p+0.4*bounce);
}

// normal calculation
vec3 calcNormal(vec3 pos, float effectiveTime, float bounce)
{
    float eps = 0.0001;
    float d = map(pos, effectiveTime, bounce);
    return normalize(vec3(
        map(pos+vec3(eps, 0, 0), effectiveTime, bounce) - d,
        map(pos+vec3(0, eps, 0), effectiveTime, bounce) - d,
        map(pos+vec3(0, 0, eps), effectiveTime, bounce) - d
    ));
}

// standard sphere tracing inside and outside
float castRayx(vec3 ro, vec3 rd, float effectiveTime, float bounce) 
{
    float function_sign = (map(ro, effectiveTime, bounce) < 0.) ? -1. : 1.;
    float precis = .0001;
    float h = precis * 2.;
    float t = 0.;
    for(int i = 0; i < 120; i++) 
    {
        if(abs(h) < precis || t > 12.) break;
        h = function_sign * map(ro+rd*t, effectiveTime, bounce);
        t += h;
    }
    return t;
}

// refraction
float refr(vec3 pos, vec3 lig, vec3 dir, vec3 nor, float angle, out float t2, out vec3 nor2, float effectiveTime, float bounce)
{
    float h = 0.;
    t2 = 2.;
    vec3 dir2 = refract(dir, nor, angle);  
    for(int i = 0; i < 50; i++) 
    {
        if(abs(h) > 3.) break;
        h = map(pos+dir2*t2, effectiveTime, bounce);
        t2 -= h;
    }
    nor2 = calcNormal(pos+dir2*t2, effectiveTime, bounce);
    return (.5*clamp(dot(-lig, nor2), 0., 1.) + pow(max(dot(reflect(dir2, nor2), lig), 0.), 8.));
}

// softshadow 
float softshadow(vec3 ro, vec3 rd, float effectiveTime, float bounce) 
{
    float sh = 1.;
    float t = .02;
    float h = .0;
    for(int i = 0; i < 22; i++)  
    {
        if(t > 20.) continue;
        h = map(ro+rd*t, effectiveTime, bounce);
        sh = min(sh, 4.*h/t);
        t += h;
    }
    return sh;
}

void main()
{
    vec4 prevTimeData;
    float accumulatedTime, currentSpeed, adjustedSpeed, newTime;
    float effectiveTime;
    
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
            // Update time with current speed
            adjustedSpeed = speed;
            newTime = accumulatedTime + adjustedSpeed * TIMEDELTA;
        }
        
        // Store the accumulated time and current speed
        gl_FragColor = vec4(newTime, adjustedSpeed, 0.0, 1.0);
    }
    else {
        // Final rendering pass
        prevTimeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        effectiveTime = prevTimeData.r;
        
        // Calculate the bounce effect
        float bounce = abs(fract(0.05*effectiveTime)-.5)*20.; // triangle function
        
        vec2 fragCoord = isf_FragNormCoord.xy * RENDERSIZE.xy;
        vec2 uv = fragCoord.xy/RENDERSIZE.xy; 
        vec2 p = uv*2.-1.;
       
        // Bouncy cam every 10 seconds
        float wobble = (fract(.1*(effectiveTime-1.)) >= 0.9) ? 
                       fract(-effectiveTime)*0.1*sin(30.*effectiveTime) : 0.;
        wobble *= wobbleAmount;
        
        // Camera    
        vec3 dir = normalize(vec3(2.*fragCoord.xy - RENDERSIZE.xy, RENDERSIZE.y));
        vec3 org = vec3(0, 2.*wobble, -3.);  
        dir = normalize(vec3(dir.xy, sqrt(max(dir.z*dir.z - dot(dir.xy, dir.xy)*.2, 0.)))); // barrel
        vec2 m = sin(vec2(0, 1.57) + effectiveTime/8.);
        dir.xy = mat2(m.y, -m.x, m.x, m.y)*dir.xy;
        dir.xz = mat2(m.y, -m.x, m.x, m.y)*dir.xz;

        // Standard sphere tracing
        vec3 color = vec3(0.);
        vec3 color2 = vec3(0.);
        float t = castRayx(org, dir, effectiveTime, bounce);
        vec3 pos = org+dir*t;
        vec3 nor = calcNormal(pos, effectiveTime, bounce);

        // Lighting
        vec3 lig = normalize(-pos);
        float depth = clamp((1.-0.09*t), 0., 1.);
        
        vec3 nor2;
        if(t < 12.0)
        {
            color2 = vec3(max(dot(lig, nor), 0.) + pow(max(dot(reflect(dir, nor), lig), 0.), 16.));
            color2 *= clamp(softshadow(pos, lig, effectiveTime, bounce), 0., 1.);  // shadow               
            float t2;
            color2.r += refr(pos, lig, dir, nor, 0.91, t2, nor2, effectiveTime, bounce)*depth;
            color2.g += refr(pos, lig, dir, nor, 0.90, t2, nor2, effectiveTime, bounce)*depth;
            color2.b += refr(pos, lig, dir, nor, 0.89, t2, nor2, effectiveTime, bounce)*depth;
            color2 -= clamp(.1*t2, 0., 1.);             // inner intensity loss
        }      
        
        float tmp = 0.;
        float T = 1.;

        // Animation of glow intensity    
        float glowIntensity = intensity*-sin(.209*effectiveTime+1.)+intensity; 
        for(int i = 0; i < 128; i++)
        {
            float density = 0.; 
            float nebula = noise(org+bounce);
            density = glowIntensity-map(org+.5*nor2, effectiveTime, bounce)*nebula;
            if(density > 0.)
            {
                tmp = density / 128.;
                T *= 1. -tmp * 100.;
                if(T <= 0.) break;
            }
            org += dir*0.078;
        }    
        
        vec3 basecol = baseColor.rgb;
        T = clamp(T, 0., 1.5); 
        color += basecol * exp(4.*(0.5-T) - 0.8);
        color2 *= depth*depth;
        color2 += (1.-depth)*noise(6.*dir+0.3*effectiveTime)*.1; // subtle mist

        gl_FragColor = vec4(vec3(1.*color+0.8*color2)*1.3, 1.0);
    }
}
