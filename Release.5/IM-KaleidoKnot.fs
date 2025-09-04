/*{
    "DESCRIPTION": "Creates a seamless, warping kaleidoscope effect with generative color palettes. Features independent, smoothed animation controls for geometry and color.",
    "CREDIT": "Original by @dot2dot (bareimage). ISF 2.0 Conversion by @dot2dot (bareimage)",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "GENERATOR"
    ],
    "INPUTS": [
        {
            "NAME": "speed",
            "TYPE": "float",
            "DEFAULT": 4.0,
            "MIN": 0.0,
            "MAX": 5.0,
            "LABEL": "Distortion Anim Speed"
        },
        {
            "NAME": "transitionSpeed",
            "TYPE": "float",
            "DEFAULT": 2.0,
            "MIN": 0.1,
            "MAX": 10.0,
            "LABEL": "Parameter Smoothing"
        },
        {
            "NAME": "kaleidoscopeSegments",
            "TYPE": "float",
            "DEFAULT": 6.0,
            "MIN": 1.0,
            "MAX": 20.0,
            "LABEL": "Kaleidoscope Segments"
        },
        {
            "NAME": "rotationSpeed",
            "TYPE": "float",
            "DEFAULT": 0.2,
            "MIN": -2.0,
            "MAX": 2.0,
            "LABEL": "Rotation Speed"
        },
        {
            "NAME": "distortionAmount",
            "TYPE": "float",
            "DEFAULT": 0.2,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Distortion Amount"
        },
        {
            "NAME": "lineFrequency",
            "TYPE": "float",
            "DEFAULT": 50.0,
            "MIN": 5.0,
            "MAX": 100.0,
            "LABEL": "Line Frequency"
        },
        {
            "NAME": "pulsationSpeed",
            "TYPE": "float",
            "DEFAULT": 0.1,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Pulsation Speed"
        },
        {
            "NAME": "warpFactor",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 5.0,
            "LABEL": "Warp Factor"
        },
        {
            "NAME": "colorPalette",
            "TYPE": "long",
            "DEFAULT": 2,
            "VALUES": [0, 1, 2, 3, 4, 5, 6, 7],
            "LABELS": ["Stargate", "Circuit Board", "Nebula", "Psychedelic Tunnels", "Turing Spots", "Labyrinth", "Coral Growth", "Cellular"],
            "LABEL": "Color Palette"
        },
        {
            "NAME": "colorAnimSpeed",
            "TYPE": "float",
            "DEFAULT": 0.2,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Animation Speed"
        },
        {
            "NAME": "colorBrightness",
            "TYPE": "float",
            "DEFAULT": 1.5,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Brightness"
        },
        {
            "NAME": "patternScale",
            "TYPE": "float",
            "DEFAULT": 3.0,
            "MIN": 0.1,
            "MAX": 5.0,
            "LABEL": "Pattern Scale"
        },
        {
            "NAME": "patternComplexity",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Pattern Complexity"
        },
        {
            "NAME": "centerFadeRadius",
            "TYPE": "float",
            "DEFAULT": 0.1,
            "MIN": 0.0,
            "MAX": 0.5,
            "LABEL": "Center Fade Radius"
        },
        {
            "NAME": "centerFadeSharpness",
            "TYPE": "float",
            "DEFAULT": 10.0,
            "MIN": 1.0,
            "MAX": 50.0,
            "LABEL": "Center Fade Sharpness"
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
            "TARGET": "paramBufferA",
            "PERSISTENT": true,
            "FLOAT": true,
            "WIDTH": 1,
            "HEIGHT": 1
        },
        {
            "TARGET": "pulsationTimeBuffer",
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
            "TARGET": "colorBuffer",
            "PERSISTENT": true,
            "FLOAT": true,
            "WIDTH": 1,
            "HEIGHT": 1
        },
        {
            "TARGET": "controlsBuffer",
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

// MIT License
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// ADDITIONAL ATTRIBUTION REQUIREMENT:
// When using, modifying, or distributing this software, proper acknowledgment
// and credit must be maintained for both the original authors and any
// substantial contributors to derivative works.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// --- Mathematical Constants ---
#define PI 3.14159265359
#define TAU (2.0 * PI)

// --- Utility & Noise Functions (from Voxel Shader) ---
vec3 hsv2rgb(vec3 c) { vec4 K=vec4(1.,2./3.,1./3.,3.); vec3 p=abs(fract(c.xxx+K.xyz)*6.-K.www); return c.z*mix(K.xxx,clamp(p-K.xxx,0.,1.),c.y); }
float hash11(float p) { return fract(sin(p*727.1)*435.545); }
vec2 hash2(vec2 p) { return fract(sin(vec2(dot(p,vec2(127.1,311.7)),dot(p,vec2(269.5,183.3))))*43758.5453); }
float noise(vec3 x) {
    vec3 p=floor(x), f=fract(x); f=f*f*(3.-2.*f); float n=p.x+p.y*157.+113.*p.z;
    return mix(mix(mix(hash11(n),hash11(n+1.),f.x),mix(hash11(n+157.),hash11(n+158.),f.x),f.y),
               mix(mix(hash11(n+113.),hash11(n+114.),f.x),mix(hash11(n+270.),hash11(n+271.),f.x),f.y),f.z);
}
vec3 aces_approx(vec3 v) {
    v=max(v,0.); float a=2.51,b=0.03,c=2.43,d=0.59,e=0.14;
    return clamp((v*(a*v+b))/(v*(c*v+d)+e),0.,1.);
}

// --- Generative Color Palettes ---
// Adapted for 2D kaleidoscope context.
vec3 getColor(int p_int, float time, vec3 p, float t, vec4 controls) {
    p *= controls.z; // Apply Pattern Scale
    vec2 face_uv = p.xy; // In 2D, the face is always the xy plane.

    // Stargate
    if(p_int==0){vec3 col=vec3(0.);for(float i=1.;i<3.+10.*controls.w;i++){float a=atan(face_uv.y,face_uv.x)*ceil(i*2.5)+time*2.*sin(i*i)+i*i;col+=25./(abs(length(face_uv)*6.-i*1.5)+40.)*clamp(cos(a),0.,0.8)*(cos(a-i+vec4(0,1,2,0))+1.).rgb;}return col;}
    // Circuit Board
    if(p_int==1){vec3 grid=abs(fract(p*0.5)-0.5)/0.1;float lines=pow(min(min(grid.x,grid.y),grid.z),0.1+controls.w*0.4);float pulse=sin(p.x*(1.+controls.w*5.)-time)*0.5+0.5;return vec3(lines*pulse*2.,lines*1.5,lines*4.);}
    // Nebula
    if(p_int==2){float f=0.;mat2 m=mat2(1.6,1.2,-1.2,1.6);p*=0.5;for(int i=0;i<3+int(controls.w*4.);i++){f+=noise(p)*pow(0.5,float(i));p.xy*=m;}return hsv2rgb(vec3(f+time*0.05,0.8,1.));}
    // Psychedelic Tunnels
    if(p_int==3){vec2 p_polar=vec2(atan(face_uv.y,face_uv.x),log(length(face_uv)));float k=1.+controls.w*10.;float a=p_polar.x+time*0.5;a=floor(a*k)/k;p_polar=vec2(p_polar.y*cos(a)-a*sin(a),p_polar.y*sin(a)+a*cos(a));return hsv2rgb(vec3(fract(p_polar.x*.2+time*.1),1.,smoothstep(0.,1.,fract(p_polar.y*5.))));}
    // Turing Spots
    if(p_int==4){vec2 g=floor(face_uv*3.),f=fract(face_uv*3.)-.5;float d=1e9;vec3 c;for(int i=-1;i<=1;i++)for(int j=-1;j<=1;j++){vec2 o=hash2(g+vec2(i,j));float D=length(f-o+.5);if(D<d){d=D;c=hsv2rgb(vec3(hash11(g.x+g.y*157.+time*.1),.7,.9));}}return(1.-smoothstep(0.,.8,d))*c;}
    // Labyrinth
    if(p_int==5){float n=noise(p*0.5);float pattern=sin(n*15.+time*2.+sin(p.y+n)*2.);pattern=smoothstep(0.,1.,pattern);return hsv2rgb(vec3(fract(n+time*0.1),0.8,pattern));}
    // Coral Growth
    if(p_int==6){vec2 p_warp=face_uv;for(int i=0;i<3+int(controls.w*5.);i++){p_warp=abs(p_warp)/dot(p_warp,p_warp)-.8+sin(time*0.2)*0.1;}return hsv2rgb(vec3(fract(p_warp.x*.2),1.,1.));}
    // Cellular
    if(p_int==7){vec2 st=floor(face_uv/10.*(5.+controls.w*20.));float h=hash11(st.x+st.y*157.);float on=step(0.5,fract(h*10.+time*h));return vec3(on)*hsv2rgb(vec3(fract(h*3.),1.,1.));}
    
    return vec3(0.8);
}

// --- Geometry Functions ---
mat2 rot(float angle) { return mat2(cos(angle), -sin(angle), sin(angle), cos(angle)); }
vec2 kaleidoscope(vec2 uv, float segments) {
    float angle = atan(uv.y, uv.x);
    float radius = length(uv);
    float segmentAngle = PI / segments;
    angle = mod(angle, segmentAngle);
    if (angle > segmentAngle / 2.0) {
        angle = segmentAngle - angle;
    }
    return vec2(cos(angle), sin(angle)) * radius;
}

void main() {
    // Pass 0: Time & Speed Buffer (Main animation + Color animation)
    if (PASSINDEX == 0) {
        vec4 pD = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        float mainTime = pD.r, smSpeed = pD.g, colorTime = pD.b, smColorSpeed = pD.a;
        
        float n_smSpeed = mix(smSpeed, speed, min(1., TIMEDELTA * transitionSpeed));
        float n_smColorSpeed = mix(smColorSpeed, colorAnimSpeed, min(1., TIMEDELTA * transitionSpeed));
        float n_mainTime = mainTime + n_smSpeed * TIMEDELTA;
        float n_colorTime = colorTime + n_smColorSpeed * TIMEDELTA;
        
        if (FRAMEINDEX == 0) {
            n_mainTime = 0.; n_smSpeed = speed;
            n_colorTime = 0.; n_smColorSpeed = colorAnimSpeed;
        }
        gl_FragColor = vec4(n_mainTime, n_smSpeed, n_colorTime, n_smColorSpeed);
    }
    // Pass 1: Geometry Parameters
    else if (PASSINDEX == 1) {
        vec4 pP = IMG_NORM_PIXEL(paramBufferA, vec2(0.5));
        vec4 cP;
        if (FRAMEINDEX == 0) {
            cP = vec4(kaleidoscopeSegments, distortionAmount, lineFrequency, warpFactor);
        } else {
            cP.r = mix(pP.r, kaleidoscopeSegments, min(1., TIMEDELTA * transitionSpeed));
            cP.g = mix(pP.g, distortionAmount, min(1., TIMEDELTA * transitionSpeed));
            cP.b = mix(pP.b, lineFrequency, min(1., TIMEDELTA * transitionSpeed));
            cP.a = mix(pP.a, warpFactor, min(1., TIMEDELTA * transitionSpeed));
        }
        gl_FragColor = cP;
    }
    // Pass 2: Pulsation Time
    else if (PASSINDEX == 2) {
        vec4 pD = IMG_NORM_PIXEL(pulsationTimeBuffer, vec2(0.5));
        float pT = pD.r, pS = pD.g;
        float nS = mix(pS, pulsationSpeed, min(1., TIMEDELTA * transitionSpeed));
        float nT = pT + nS * TIMEDELTA;
        if(FRAMEINDEX == 0) { nT = 0.; nS = pulsationSpeed; }
        gl_FragColor = vec4(nT, nS, 0.0, 1.0);
    }
    // Pass 3: Rotation Angle
    else if (PASSINDEX == 3) {
        vec4 pD = IMG_NORM_PIXEL(rotationBuffer, vec2(0.5));
        float pA = pD.r, pS = pD.g;
        float nS = mix(pS, rotationSpeed, min(1., TIMEDELTA * transitionSpeed));
        float nA = pA + nS * TIMEDELTA;
        if(FRAMEINDEX == 0) { nA = 0.; nS = rotationSpeed; }
        gl_FragColor = vec4(nA, nS, 0.0, 1.0);
    }
    // Pass 4: Color Palette Selection
    else if (PASSINDEX == 4) {
        gl_FragColor = vec4(float(colorPalette), 0.0, 0.0, 1.0);
    }
    // Pass 5: Color Controls
    else if (PASSINDEX == 5) {
        vec4 pV = IMG_NORM_PIXEL(controlsBuffer,vec2(0.5));
        vec4 tV = vec4(colorBrightness, patternScale, patternComplexity, 0.0);
        gl_FragColor = mix(pV, tV, min(1.,TIMEDELTA * transitionSpeed));
    }
    // Final Render Pass
    else {
        // --- Retrieve all smoothed values ---
        vec4 timeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        vec4 paramsA = IMG_NORM_PIXEL(paramBufferA, vec2(0.5));
        vec4 pulsationData = IMG_NORM_PIXEL(pulsationTimeBuffer, vec2(0.5));
        vec4 rotationData = IMG_NORM_PIXEL(rotationBuffer, vec2(0.5));
        vec4 paletteData = IMG_NORM_PIXEL(colorBuffer, vec2(0.5));
        vec4 controlsData = IMG_NORM_PIXEL(controlsBuffer, vec2(0.5));
        
        float effectiveTime = timeData.r;
        float colorTime = timeData.b;
        
        float segments = paramsA.r;
        float distortionMax = paramsA.g;
        float lineFreq = paramsA.b;
        float warp = paramsA.a;
        
        float pulsationTime = pulsationData.r;
        float rotationAngle = rotationData.r;
        
        int paletteIndex = int(paletteData.r);
        vec4 colorControls = vec4(0.0, controlsData.x, controlsData.y, controlsData.z); // (Unused, Brightness, Scale, Complexity)

        // --- Geometry Calculation ---
        vec2 uv = (-1.0 + 2.0 * isf_FragNormCoord.xy);
        uv.x *= RENDERSIZE.x / RENDERSIZE.y;
        
        uv *= rot(rotationAngle);
        
        float original_radius = length(uv);
        
        // Apply kaleidoscope effect
        uv = kaleidoscope(uv, segments);
        
        float radius = length(uv);
        float angle = atan(uv.y * tan(warp), uv.x);
        
        float animatedDistortion = distortionMax * (0.5 + 0.5 * sin(effectiveTime));
        float distortion = sin(angle) * animatedDistortion;
        float distorted_radius = radius + distortion;
        
        float lines = sin(distorted_radius * sin(pulsationTime) * lineFreq);
        lines = smoothstep(-1.0, 1.0, lines / fwidth(lines));

        // --- Central Noise Fix ---
        // Create a falloff factor for the center
        float centerFalloff = smoothstep(centerFadeRadius, 0.0, original_radius * centerFadeSharpness);
        centerFalloff = clamp(centerFalloff, 0.0, 1.0);

        // --- Coloring ---
        vec3 p = vec3(uv.x, uv.y, effectiveTime * 0.2); // Use animated z for 3D noise
        vec3 rawColor = getColor(paletteIndex, colorTime, p, radius, colorControls);
        
        // Combine pattern and color, apply brightness, and tonemap
        vec3 finalColor = rawColor * lines * colorControls.y;

        // Blend out the distortion/lines near the center
        // By blending with a more uniform color (e.g., the raw color without lines)
        finalColor = mix(finalColor, rawColor * colorControls.y, centerFalloff);

        finalColor = aces_approx(finalColor);
        
        gl_FragColor = vec4(finalColor, 1.0);
    }
}