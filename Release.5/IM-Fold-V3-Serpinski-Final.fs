/*{
    "DESCRIPTION": "A 3D 'folding' fractal with tetrahedral symmetry, now featuring an expanded set of controls and 17 advanced color palettes. Based on work by nimitz and dot2dot.",
    "CREDIT": "Original by @dot2dot (bareimage). ISF 2.0 Conversion by @dot2dot (bareimage)",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "GENERATOR"
    ],
    "INPUTS": [
        {
            "NAME": "speed",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 10.0,
            "LABEL": "Animation Speed"
        },
        {
            "NAME": "colorAnimSpeed",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 5.0,
            "LABEL": "Color Animation Speed"
        },
        {
            "NAME": "palette",
            "TYPE": "long",
            "DEFAULT": 2,
            "VALUES": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16],
            "LABELS": ["Plasma Ball", "Accretion Field", "Glitch Matrix", "Stargate", "Original Pop", "Ectoplasm", "Circuit Board", "Warp Drive", "Kaleidoscope", "Nebula", "Dot Matrix", "Interference", "Psychedelic Tunnels", "Turing Spots", "Labyrinth", "Coral Growth", "Cellular"],
            "LABEL": "Color Palette"
        },
        {
            "NAME": "iterations",
            "TYPE": "float",
            "DEFAULT": 16.5,
            "MIN": 1.0,
            "MAX": 34.0,
            "STEP": 1.0,
            "LABEL": "Fractal Iterations"
        },
        {
            "NAME": "fractalScale",
            "TYPE": "float",
            "DEFAULT": 2.5,
            "MIN": 1.5,
            "MAX": 3.0,
            "LABEL": "Fractal Scale"
        },
        {
            "NAME": "fractalOffset",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.5,
            "MAX": 1.5,
            "LABEL": "Fractal Offset"
        },
        {
            "NAME": "rotSpeedX",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -2.0,
            "MAX": 2.0,
            "LABEL": "Rotation Speed X"
        },
        {
            "NAME": "rotSpeedY",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": -2.0,
            "MAX": 2.0,
            "LABEL": "Rotation Speed Y"
        },
        {
            "NAME": "rotSpeedZ",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": -2.0,
            "MAX": 2.0,
            "LABEL": "Rotation Speed Z"
        },
        {
            "NAME": "haloSoftness",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 0.5,
            "LABEL": "Halo Softness"
        },
        {
            "NAME": "haloStrength",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 0.5,
            "LABEL": "Halo Strength"
        },
        {
            "NAME": "ambientStrength",
            "TYPE": "float",
            "DEFAULT": 0.2,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Ambient Light"
        },
        {
            "NAME": "colorBrightness",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 5.0,
            "LABEL": "Color Brightness"
        },
        {
            "NAME": "patternScale",
            "TYPE": "float",
            "DEFAULT": 1.0,
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
            "NAME": "transitionSpeed",
            "TYPE": "float",
            "DEFAULT": 2.0,
            "MIN": 0.1,
            "MAX": 10.0,
            "LABEL": "Parameter Smoothing"
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
            "TARGET": "rotationBuffer",
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

// MIT License (with additional attribution requirement)
// See original ShaderToy for full license details.
// Proper acknowledgment must be maintained for both the original authors
// and any substantial contributors to derivative works.

// --- Helper Functions ---
vec3 hsv2rgb(vec3 c) { vec4 K=vec4(1.,2./3.,1./3.,3.); vec3 p=abs(fract(c.xxx+K.xyz)*6.-K.www); return c.z*mix(K.xxx,clamp(p-K.xxx,0.,1.),c.y); }
float hash11(float p) { return fract(sin(p*727.1)*435.545); }
vec2 hash2(vec2 p) { return fract(sin(vec2(dot(p,vec2(127.1,311.7)),dot(p,vec2(269.5,183.3))))*43758.5453); }
float noise(vec3 x) {
    vec3 p=floor(x), f=fract(x); f=f*f*(3.-2.*f); float n=p.x+p.y*157.+113.*p.z;
    return mix(mix(mix(hash11(n),hash11(n+1.),f.x),mix(hash11(n+157.),hash11(n+158.),f.x),f.y),
             mix(mix(hash11(n+113.),hash11(n+114.),f.x),mix(hash11(n+270.),hash11(n+271.),f.x),f.y),f.z);
}
vec3 aces_approx(vec3 v) {
    v=max(v,0.)*0.6; float a=2.51,b=0.03,c=2.43,d=0.59,e=0.14;
    return clamp((v*(a*v+b))/(v*(c*v+d)+e),0.,1.);
}


// --- Raymarching & Fractal Settings ---
#define MAX_STEPS 30
#define MAX_DIST 60.0
#define SURF_DIST 0.01

// The Distance Estimator (DE) function.
// MODIFIED: Combines tetrahedral symmetry with a chaotic, time-based internal fold.
float getDistance(vec3 p, float time, float its, float scale, float offsetVal, vec3 rotationAngles) {
    float angleX = rotationAngles.x;
    float angleY = rotationAngles.y;
    float angleZ = rotationAngles.z;
    
    float sX = sin(angleX); float cX = cos(angleX);
    float sY = sin(angleY); float cY = cos(angleY);
    float sZ = sin(angleZ); float cZ = cos(angleZ);
    
    mat3 rotX = mat3(1.0, 0.0, 0.0, 0.0, cX, -sX, 0.0, sX, cX);
    mat3 rotY = mat3(cY, 0.0, sY, 0.0, 1.0, 0.0, -sY, 0.0, cY);
    mat3 rotZ = mat3(cZ, -sZ, 0.0, sZ, cZ, 0.0, 0.0, 0.0, 1.0);
    
    p *= rotY * rotX * rotZ;
    
    float foldTime = time * 0.4;
    float s_fold = sin(foldTime);
    float c_fold = cos(foldTime);
    mat2 rot_fold = mat2(c_fold, -s_fold, s_fold, c_fold);
    
    vec3 offset = vec3(offsetVal);
    
    for (int i = 0; i < int(its); i++) {
        if(p.x+p.y < 0.0) p.xy = -p.yx;
        if(p.x+p.z < 0.0) p.xz = -p.zx;
        if(p.y+p.z < 0.0) p.yz = -p.zy;
        
        p -= offset;
        
        p.xy *= rot_fold;
        p.yz *= rot_fold;
        p.zx *= rot_fold;
        
        p *= scale;
    }
    
    return length(p) / pow(scale, its);
}

// Calculates the surface normal at a point `p`.
vec3 getNormal(vec3 p, float time, float its, float scale, float offsetVal, vec3 rotationAngles) {
    float d = getDistance(p, time, its, scale, offsetVal, rotationAngles);
    vec2 e = vec2(0.0001, 0); 
    vec3 n = d - vec3(
        getDistance(p - e.xyy, time, its, scale, offsetVal, rotationAngles),
        getDistance(p - e.yxy, time, its, scale, offsetVal, rotationAngles),
        getDistance(p - e.yyx, time, its, scale, offsetVal, rotationAngles)
    );
    return normalize(n);
}

// --- High-Quality Animated Color Palettes ---
// This function is modified to use a UV mapping approach for color generation.
// The `p` parameter now acts as the UV coordinate for the texture lookup.
vec3 getColor(int p_int, float time, vec3 p, vec3 n, float t, vec2 fragUV, vec4 controls, vec3 rd) {
    // Determine the primary axis of the normal to project UVs
    vec2 uv;
    if (abs(n.x) > abs(n.y) && abs(n.x) > abs(n.z)) {
        uv = p.yz; // Project onto YZ plane if X is dominant
    } else if (abs(n.y) > abs(n.x) && abs(n.y) > abs(n.z)) {
        uv = p.xz; // Project onto XZ plane if Y is dominant
    } else {
        uv = p.xy; // Project onto XY plane if Z is dominant (or equal)
    }

    uv *= controls.z; // Apply Pattern Scale
    uv += time * 0.1; // Add some time-based panning for animation

    // Now, apply the color logic using the calculated uv
    if(p_int==0){float plasma=sin(length(uv)*1.5-time*2.);float hue=fract(0.6+0.1*plasma+controls.w*0.5);return hsv2rgb(vec3(hue,1.,1.+plasma));}
    if(p_int==1){vec3 p_turb=vec3(uv, p.z)*0.5;for(float i=1.;i<2.+5.*controls.w;i++){p_turb+=sin(p_turb.yzx*i+time*0.5+0.3*t)/i*0.8;}return((1.5+cos(p_turb.x+t*0.4+vec4(6,1,2,0)))*1.).rgb;}
    if(p_int==2){vec2 p_anim=uv; p_anim.x*=cos(2.+t*0.01+controls.w*0.5); p_anim.y*=sin(2.+t*0.01+controls.w*0.5); return((1.+sin(0.5*p.x+length(p_anim)+vec4(0,4,3,6)))*(0.8+0.2*sin(p.y*(1.+controls.w*20.)))).rgb;} // Modified to use UV and p.x/p.y
    if(p_int==3){vec3 col=vec3(0.);for(float i=1.;i<3.+10.*controls.w;i++){float a=atan(uv.y,uv.x)*ceil(i*2.5)+time*2.*sin(i*i)+i*i;col+=25./(abs(length(uv)*6.-i*1.5)+40.)*clamp(cos(a),0.,0.8)*(cos(a-i+vec4(0,1,2,0))+1.).rgb;}return col;}
    if(p_int==4){return(1.+sin(vec4(0.,0.5,1.,0.)-t/3.3+(2.+controls.w*5.)*(uv.x+uv.y))).rgb*1.2;}
    if(p_int==5){vec3 N1=vec3(noise(vec3(uv,p.z)*0.5+time*0.2));vec3 N2=vec3(noise(vec3(uv,p.z)*(1.5+controls.w*3.)-time*0.1));return mix(vec3(N2.x,N1.y,N2.z),vec3(N1.x,N2.y,N1.z),controls.w)*vec3(0.5,2.,1.2);} // Uses p.z for depth
    if(p_int==6){vec3 grid=abs(fract(vec3(uv,p.z)*0.5)-0.5)/(abs(n)+0.1);float lines=pow(min(min(grid.x,grid.y),grid.z),0.1+controls.w*0.4);float pulse=sin(uv.x*(1.+controls.w*5.)-time)*0.5+0.5;return vec3(lines*pulse*2.,lines*1.5,lines*4.);} // Uses p.z for grid
    if(p_int==7){float stretch=fract((p.z+n.x)*0.2+time);stretch=pow(stretch,5.*(1.+controls.w*3.));return hsv2rgb(vec3(fract(p.z*0.05),1.,stretch*5.));}
    if(p_int==8){vec2 current_uv = uv; for(int i=0;i<int(controls.w*5.);i++)current_uv=abs(current_uv);current_uv=vec2(atan(current_uv.y,current_uv.x),length(current_uv));current_uv.x+=sin(current_uv.y*2.-time)*0.5;return hsv2rgb(vec3(fract(current_uv.x/6.283),1.,pow(current_uv.y*0.1,0.5)));}
    if(p_int==9){float f=0.;mat2 m=mat2(1.6,1.2,-1.2,1.6);vec3 p_noise=vec3(uv,p.z)*0.5;for(int i=0;i<3+int(controls.w*4.);i++){f+=noise(p_noise)*pow(0.5,float(i));p_noise.xy*=m;}return hsv2rgb(vec3(f+time*0.05,0.8,1.));} // Uses p.z for depth
    if(p_int==10){vec3 d=vec3(0.);vec3 p_effect = vec3(uv, p.z);for(float i=1.;i<4.+8.*controls.w;i+=1.){d.x+=sin(length(p_effect+vec3(i*0.5,0,0))-time*2.0);d.y+=cos(length(p_effect+vec3(0,i*0.5,0))-time*2.0);d.z+=sin(length(p_effect-vec3(0,0,i*0.5))-time*2.0);}return normalize(d)*0.5+0.5;} // Uses p.z for depth
    if(p_int==11){float v=0.;float dot_size=mix(0.4,0.4,controls.w);vec3 p_dots=vec3(uv,p.z);for(int i=1;i<4;++i){vec3 g=floor(p_dots*float(i)*0.5);float id=g.x+g.y*157.+113.*g.z;vec3 f=fract(p_dots*float(i)*0.5);vec3 rand_pos=vec3(hash2(g.xy+g.z),hash11(id*3.14));v+=smoothstep(dot_size,dot_size-0.1,length(f-rand_pos-vec3(0,sin(time+id),0)));}return vec3(v)*hsv2rgb(vec3(fract(p.z*0.1+time*0.1),1.,1.));} // Uses p.z for depth
    if(p_int==12){vec2 p_polar=vec2(atan(uv.y,uv.x),log(length(uv)));float k=1.+controls.w*10.;float a=p_polar.x+time*0.5;a=floor(a*k)/k;p_polar=vec2(p_polar.y*cos(a)-a*sin(a),p_polar.y*sin(a)+a*cos(a));return hsv2rgb(vec3(fract(p_polar.x*.2+time*.1),1.,smoothstep(0.,1.,fract(p_polar.y*5.))));}
    if(p_int==13){vec2 g=floor(uv*3.),f=fract(uv*3.)-.5;float d=1e9;vec3 c;for(int i=-1;i<=1;i++)for(int j=-1;j<=1;j++){vec2 o=hash2(g+vec2(i,j));float D=length(f-o+.5);if(D<d){d=D;c=hsv2rgb(vec3(hash11(g.x+g.y*157.+time*.1),.7,.9));}}return(1.-smoothstep(0.,.8,d))*c;}
    if(p_int==14){float n=noise(vec3(uv,p.z)*0.5);float pattern=sin(n*15.+time*2.+sin(uv.y+n)*2.);pattern=smoothstep(0.,1.,pattern);return hsv2rgb(vec3(fract(n+time*0.1),0.8,pattern));} // Uses p.z for depth
    if(p_int==15){vec2 p_warp=uv;for(int i=0;i<3+int(controls.w*5.);i++){p_warp=abs(p_warp)/dot(p_warp,p_warp)-.8+sin(time*0.2)*0.1;}return hsv2rgb(vec3(fract(p_warp.x*.2),1.,1.));}
    if(p_int==16){vec2 st=floor(uv/10.*(5.+controls.w*20.));float h=hash11(st.x+st.y*157.);float on=step(0.5,fract(h*10.+time*h));return vec3(on)*hsv2rgb(vec3(fract(h*3.),1.,1.));}
    return vec3(0.8);
}


void main() {
    // PASS 0: Update and store accumulated time values.
    if (PASSINDEX == 0) {
        vec4 prevData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        float newTime = prevData.r;
        float currentSpeed = prevData.g;
        float newTexTime = prevData.b;
        float currentTexSpeed = prevData.a;

        if (FRAMEINDEX == 0) {
            newTime = 0.0;
            currentSpeed = speed;
            newTexTime = 0.0;
            currentTexSpeed = colorAnimSpeed;
        }
        
        float smoothing = min(1.0, TIMEDELTA * transitionSpeed);
        currentSpeed = mix(currentSpeed, speed, smoothing);
        newTime += currentSpeed * TIMEDELTA;
        currentTexSpeed = mix(currentTexSpeed, colorAnimSpeed, smoothing);
        newTexTime += currentTexSpeed * TIMEDELTA;
        
        gl_FragColor = vec4(newTime, currentSpeed, newTexTime, currentTexSpeed);
    }
    // PASS 1: Update and store smoothed fractal parameters.
    else if (PASSINDEX == 1) {
        vec4 prevData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5));
        float currentFractalScale = prevData.r;
        float currentFractalOffset = prevData.g;
        float currentHaloSoftness = prevData.b;
        float currentAmbient = prevData.a;

        if (FRAMEINDEX == 0) {
            currentFractalScale = fractalScale;
            currentFractalOffset = fractalOffset;
            currentHaloSoftness = haloSoftness;
            currentAmbient = ambientStrength;
        }

        float smoothing = min(1.0, TIMEDELTA * transitionSpeed);
        currentFractalScale = mix(currentFractalScale, fractalScale, smoothing);
        currentFractalOffset = mix(currentFractalOffset, fractalOffset, smoothing);
        currentHaloSoftness = mix(currentHaloSoftness, haloSoftness, smoothing);
        currentAmbient = mix(currentAmbient, ambientStrength, smoothing);

        gl_FragColor = vec4(currentFractalScale, currentFractalOffset, currentHaloSoftness, currentAmbient);
    }
    // PASS 2: Update and store accumulated rotation angles.
    else if (PASSINDEX == 2) {
        vec4 prevData = IMG_NORM_PIXEL(rotationBuffer, vec2(0.5));
        float newAngleX = prevData.r;
        float newAngleY = prevData.g;
        float newAngleZ = prevData.b;

        if (FRAMEINDEX == 0) {
            newAngleX = 0.0;
            newAngleY = 0.0;
            newAngleZ = 0.0;
        }
        
        newAngleX += rotSpeedX * TIMEDELTA;
        newAngleY += rotSpeedY * TIMEDELTA;
        newAngleZ += rotSpeedZ * TIMEDELTA;
        
        gl_FragColor = vec4(newAngleX, newAngleY, newAngleZ, 1.0);
    }
    // PASS 3: Update and store smoothed color/pattern controls.
    else if (PASSINDEX == 3) {
        vec4 prevData = IMG_NORM_PIXEL(controlsBuffer, vec2(0.5));
        vec4 targetData = vec4(0.0, colorBrightness, patternScale, patternComplexity);
        
        if (FRAMEINDEX == 0) {
             gl_FragColor = targetData;
        } else {
             float smoothing = min(1.0, TIMEDELTA * transitionSpeed);
             gl_FragColor = mix(prevData, targetData, smoothing);
        }
    }
    // PASS 4: Main render pass.
    else {
        // --- 1. Retrieve Smoothed & Accumulated Parameters ---
        vec4 timeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        float effectiveTime = timeData.r;
        float effectiveTextureTime = timeData.b;

        vec4 paramData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5));
        float effectiveFractalScale = paramData.r;
        float effectiveFractalOffset = paramData.g;
        float effectiveHaloSoftness = paramData.b;
        float effectiveAmbientStrength = paramData.a;
        
        vec4 controls = IMG_NORM_PIXEL(controlsBuffer, vec2(0.5));
        float effectiveBrightness = controls.y;
        
        float tempIteration = round(iterations);
        float local_iterations = tempIteration + haloStrength;

        vec4 rotationData = IMG_NORM_PIXEL(rotationBuffer, vec2(0.5));
        vec3 effectiveRotation = rotationData.xyz;

        // --- 2. Setup Coordinate System & Camera ---
        vec2 uv = (2.0 * isf_FragNormCoord.xy - 1.0) * vec2(RENDERSIZE.x/RENDERSIZE.y, 1.0);
        vec3 ro = vec3(0, 0, -4.0);
        vec3 rd = normalize(vec3(uv, 1.0));
        
        // --- 3. Raymarching Loop ---
        float dO = 0.0;
        vec3 p;
        float dS = 0.0;
        for (int i = 0; i < MAX_STEPS; i++) {
            p = ro + rd * dO;
            dS = getDistance(p, effectiveTime, local_iterations, effectiveFractalScale, effectiveFractalOffset, effectiveRotation);
            dO += dS;
            if (dO > MAX_DIST || dS < SURF_DIST) break;
        }

        // --- 4. Shading & Coloring ---
        vec3 col = vec3(0.0);
        if (dO < MAX_DIST) {
            p = ro + rd * dO;
            vec3 normal = getNormal(p, effectiveTime, local_iterations, effectiveFractalScale, effectiveFractalOffset, effectiveRotation);
            
            vec3 lightPos = vec3(2, 3, -3);
            vec3 lightDir = normalize(lightPos - p);
            float diffuse = max(0.0, dot(normal, lightDir));

            float ao = 0.0;
            float stepSize = 0.05;
            for(int j=1; j<=5; j++){
                float dist = getDistance(p + normal * float(j) * stepSize, effectiveTime, local_iterations, effectiveFractalScale, effectiveFractalOffset, effectiveRotation);
                ao += (float(j) * stepSize - dist);
            }
            ao = 1.0 - clamp(ao * 1.5, 0.0, 1.0);

            // Pass fragUV to getColor as well, in case a palette wants to use screen UVs.
            vec3 materialColor = getColor(palette, effectiveTextureTime, p, normal, dO, uv, controls, rd);
            col = materialColor * (effectiveAmbientStrength + diffuse) * effectiveBrightness * ao;

            float confidence = smoothstep(SURF_DIST + effectiveHaloSoftness, SURF_DIST, dS);
            col *= confidence;
        }

        col = aces_approx(col);
        gl_FragColor = vec4(col, 1.0);
    }
}