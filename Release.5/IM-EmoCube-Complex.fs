/*{
    "DESCRIPTION": "A tumbling voxel cube with 17 generative color palettes and advanced animation controls. Features smoothed, time-independent animation and parameter transitions with dedicated damping controls for rotation, scale, and twist. I am very proud of this shader, each of the side of the shader is defined an array that can be modified to create unlimited numbers of voxel like designs, and if this is not enough, it has build in color template engine.",    
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
            "MAX": 5.0,
            "LABEL": "Rotation Speed"
        },
        {
            "NAME": "startTime",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": 0.0,
            "MAX": 1000.0,
            "LABEL": "Start Time Offset"
        },
        {
            "NAME": "paramSmoothness",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 5.0,
            "LABEL": "Parameter Smoothness"
        },
        {
            "NAME": "colorPalette",
            "TYPE": "long",
            "DEFAULT": 3,
            "VALUES": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16],
            "LABELS": ["Plasma Ball", "Accretion Field", "Glitch Matrix", "Stargate", "Original Pop", "Ectoplasm", "Circuit Board", "Warp Drive", "Kaleidoscope", "Nebula", "Dot Matrix", "Interference", "Psychedelic Tunnels", "Turing Spots", "Labyrinth", "Coral Growth", "Cellular"],
            "LABEL": "Color Palette"
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
            "TARGET": "sceneBuffer",
            "FLOAT": true
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

// --- Utility & Noise Functions ---
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

// --- Voxel Cube Data (Correctly Formatted) ---
const float faceFront[169] = float[169](
1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0,
1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 1.0,
1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 1.0,
1.0, 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 1.0, 0.0, 1.0,
1.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 1.0, 0.0, 1.0,
1.0, 0.0, 1.0, 0.0, 1.0, 1.0, 1.0, 0.0, 1.0, 0.0, 1.0, 0.0, 1.0,
1.0, 0.0, 1.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 1.0, 0.0, 1.0,
1.0, 0.0, 1.0, 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 1.0, 0.0, 1.0,
1.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 1.0,
1.0, 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 1.0,
1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0,
1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0
);
const float faceBack[169] = float[169](
1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
1.0, 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0, 1.0,
1.0, 1.0, 0.0, 1.0, 0.0, 1.0, 0.0, 1.0, 0.0, 1.0, 0.0, 1.0, 1.0,
1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
1.0, 0.0, 0.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 0.0, 0.0, 1.0,
1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 0.0, 0.0, 1.0, 1.0, 1.0, 1.0, 1.0,
1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0,
1.0, 1.0, 1.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0, 1.0, 1.0, 1.0,
1.0, 1.0, 0.0, 1.0, 0.0, 1.0, 1.0, 1.0, 0.0, 1.0, 0.0, 1.0, 1.0,
1.0, 1.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 1.0, 1.0,
1.0, 0.0, 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 0.0, 1.0,
1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0
);
const float faceTop[169] = float[169](
1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0,
1.0, 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0, 1.0,
1.0, 1.0, 1.0, 1.0, 0.0, 1.0, 0.0, 1.0, 0.0, 1.0, 1.0, 1.0, 1.0,
1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0,
1.0, 0.0, 0.0, 1.0, 1.0, 0.0, 0.0, 0.0, 1.0, 1.0, 0.0, 0.0, 1.0,
1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
1.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0, 1.0, 1.0,
1.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 1.0,
1.0, 0.0, 1.0, 1.0, 1.0, 0.0, 0.0, 0.0, 1.0, 1.0, 1.0, 0.0, 1.0,
1.0, 0.0, 0.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 0.0, 0.0, 1.0,
1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0
);
const float faceBottom[169] = float[169](
1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0,
1.0, 1.0, 1.0, 1.0, 0.0, 1.0, 0.0, 1.0, 0.0, 1.0, 1.0, 1.0, 1.0,
1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
1.0, 0.0, 0.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 0.0, 0.0, 1.0,
1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 0.0, 0.0, 1.0, 1.0, 1.0, 1.0, 1.0,
1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0,
1.0, 1.0, 0.0, 1.0, 1.0, 0.0, 0.0, 0.0, 1.0, 1.0, 0.0, 1.0, 1.0,
1.0, 1.0, 0.0, 1.0, 1.0, 0.0, 1.0, 0.0, 1.0, 1.0, 0.0, 1.0, 1.0,
1.0, 1.0, 0.0, 1.0, 1.0, 0.0, 1.0, 0.0, 1.0, 1.0, 0.0, 1.0, 1.0,
1.0, 0.0, 0.0, 1.0, 1.0, 0.0, 0.0, 0.0, 1.0, 1.0, 0.0, 0.0, 1.0,
1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0
);
const float faceLeft[169] = float[169](
1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
1.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 1.0,
1.0, 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0, 1.0,
1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0,
1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
1.0, 0.0, 0.0, 0.0, 1.0, 1.0, 0.0, 1.0, 1.0, 0.0, 0.0, 0.0, 1.0,
1.0, 1.0, 1.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 1.0, 1.0, 1.0,
1.0, 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 1.0,
1.0, 0.0, 1.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 1.0, 0.0, 1.0,
1.0, 0.0, 0.0, 1.0, 1.0, 0.0, 0.0, 0.0, 1.0, 1.0, 0.0, 0.0, 1.0,
1.0, 1.0, 1.0, 1.0, 0.0, 0.0, 1.0, 0.0, 0.0, 1.0, 1.0, 1.0, 1.0,
1.0, 1.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0, 1.0, 1.0,
1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0
);
const float faceRight[169] = float[169](
1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
1.0, 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0, 1.0,
1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0,
1.0, 1.0, 0.0, 0.0, 1.0, 1.0, 0.0, 1.0, 1.0, 0.0, 0.0, 1.0, 1.0,
1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
1.0, 1.0, 1.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 1.0, 1.0, 1.0,
1.0, 1.0, 1.0, 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 1.0, 1.0, 1.0,
1.0, 0.0, 1.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 1.0, 0.0, 1.0,
1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0,
1.0, 0.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0.0, 1.0,
1.0, 1.0, 1.0, 0.0, 0.0, 1.0, 0.0, 1.0, 0.0, 0.0, 1.0, 1.0, 1.0,
1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0
);

// --- Voxel Cube Rendering Logic ---
const float EPS=0.001;
const float CUBE_SIZE = 13.0; // Global constant for cube dimensions

bool voxelMap(in vec3 p,out float v,float t){float last=CUBE_SIZE-1.;vec2 uv;int i=-1;float cV=0.;v=0.;if(p.z>-EPS&&p.z<EPS){uv=p.xy;if(all(greaterThanEqual(uv,vec2(0)))&&all(lessThan(uv,vec2(CUBE_SIZE)))){i=int(uv.y)*int(CUBE_SIZE)+int(uv.x);cV=faceFront[i];}}else if(p.z>last-EPS&&p.z<last+EPS){uv=p.xy;if(all(greaterThanEqual(uv,vec2(0)))&&all(lessThan(uv,vec2(CUBE_SIZE)))){i=int(uv.y)*int(CUBE_SIZE)+int(uv.x);cV=faceBack[i];}}else if(p.y>-EPS&&p.y<EPS){uv=p.xz;if(all(greaterThanEqual(uv,vec2(0)))&&all(lessThan(uv,vec2(CUBE_SIZE)))){i=int(uv.y)*int(CUBE_SIZE)+int(uv.x);cV=faceBottom[i];}}else if(p.y>last-EPS&&p.y<last+EPS){uv=p.xz;if(all(greaterThanEqual(uv,vec2(0)))&&all(lessThan(uv,vec2(CUBE_SIZE)))){i=int(uv.y)*int(CUBE_SIZE)+int(uv.x);cV=faceTop[i];}}else if(p.x>-EPS&&p.x<EPS){uv=p.zy;if(all(greaterThanEqual(uv,vec2(0)))&&all(lessThan(uv,vec2(CUBE_SIZE)))){i=int(uv.y)*int(CUBE_SIZE)+int(uv.x);cV=faceLeft[i];}}else if(p.x>last-EPS&&p.x<last+EPS){uv=p.zy;if(all(greaterThanEqual(uv,vec2(0)))&&all(lessThan(uv,vec2(CUBE_SIZE)))){i=int(uv.y)*int(CUBE_SIZE)+int(uv.x);cV=faceRight[i];}}return cV>0.5;}
bool IRayAABox(in vec3 ro,in vec3 rd,in vec3 invrd,in vec3 bmin,in vec3 bmax,out vec3 p0,out vec3 p1){vec3 t0=(bmin-ro)*invrd,t1=(bmax-ro)*invrd;vec3 tmin=min(t0,t1),tmax=max(t0,t1);float fmin=max(max(tmin.x,tmin.y),tmin.z),fmax=min(min(tmax.x,tmax.y),tmax.z);p0=ro+rd*fmin;p1=ro+rd*fmax;return fmax>=fmin;}
vec3 AABoxNormal(vec3 bmin,vec3 bmax,vec3 p){vec3 c=(bmin+bmax)*0.5,d=p-c,ad=abs(d);if(ad.x>ad.y&&ad.x>ad.z)return vec3(sign(d.x),0,0);if(ad.y>ad.z)return vec3(0,sign(d.y),0);return vec3(0,0,sign(d.z));}
bool traceVoxelGrid(in vec3 i_ro,in vec3 rd,in vec3 invrd,in vec3 bmin,in vec3 bmax,out vec3 n,out vec3 p,out float v,float t){n=vec3(0);p=vec3(0);v=0;vec3 ro,e;if(!IRayAABox(i_ro,rd,invrd,bmin,bmax,ro,e))return false;if(dot(e-ro,rd)<EPS)return false;vec3 ep=floor(ro+rd*EPS);bool ret=false;for(int i=0;i<64;++i){if(voxelMap(ep,v,t)){ret=true;break;}vec3 d0;IRayAABox(ro-rd*2.,rd,invrd,ep,ep+1.,d0,ro);ep=floor(ro+rd*EPS);if(dot(e-ro,rd)<EPS){ret=false;break;}}if(ret){n=AABoxNormal(ep,ep+1.,ro);p=ro;}return ret;}
float intersectVoxelCube(in vec3 ro,in vec3 rd,out vec3 p_hit,out vec3 n_hit,out float v_hit,float t){const float WSIZE=CUBE_SIZE;vec3 bmin_w=-vec3(WSIZE*0.5),bmax_w=vec3(WSIZE*0.5);vec3 invrd=1./rd,p_entry,p_exit;if(!IRayAABox(ro,rd,invrd,bmin_w,bmax_w,p_entry,p_exit))return-1.;vec3 ro_s=p_entry;if(dot(p_entry-ro,rd)<0.)ro_s=ro;vec3 ro_v=(ro_s-bmin_w)/WSIZE*CUBE_SIZE,rd_v=rd/WSIZE*CUBE_SIZE,invrd_v=1./rd_v;vec3 n_v,p_v;if(traceVoxelGrid(ro_v,rd_v,invrd_v,vec3(0),vec3(CUBE_SIZE),n_v,p_v,v_hit,t)){p_hit=bmin_w+(p_v/CUBE_SIZE)*WSIZE;n_hit=normalize(n_v);if(distance(ro,p_hit)>distance(ro,p_exit)+EPS)return-1.;return distance(ro,p_hit);}return-1.;}

// --- High-Quality Animated Color Palettes ---
vec3 getColor(int p_int, float time, vec3 p, vec3 n, float t, vec2 uv, vec4 controls, vec3 rd) {
    p *= controls.z; // Apply Pattern Scale
    vec2 face_uv; if(abs(n.x)>0.9)face_uv=p.yz;else if(abs(n.y)>0.9)face_uv=p.xz;else face_uv=p.xy;

    if(p_int==0){float plasma=sin(length(p)*1.5-time*2.);float hue=fract(0.6+0.1*plasma+controls.w*0.5);return hsv2rgb(vec3(hue,1.,1.+plasma));}
    if(p_int==1){vec3 p_turb=p*0.5;for(float i=1.;i<2.+5.*controls.w;i++){p_turb+=sin(p_turb.yzx*i+time*0.5+0.3*t)/i*0.8;}return((1.5+cos(p_turb.x+t*0.4+vec4(6,1,2,0)))*1.).rgb;}
    if(p_int==2){p.xy*=mat2(cos(2.+t*0.01+vec4(0,11,33,0)));p.xy*=mat2(cos(p.z*0.1+time*0.5+vec4(0,11,33,0)));return((1.+sin(0.5*p.z+length(p)+vec4(0,4,3,6)))*(0.8+0.2*sin(p.z*(1.+controls.w*20.)))).rgb;}
    if(p_int==3){vec3 col=vec3(0.);for(float i=1.;i<3.+10.*controls.w;i++){float a=atan(face_uv.y,face_uv.x)*ceil(i*2.5)+time*2.*sin(i*i)+i*i;col+=25./(abs(length(face_uv)*6.-i*1.5)+40.)*clamp(cos(a),0.,0.8)*(cos(a-i+vec4(0,1,2,0))+1.).rgb;}return col;}
    if(p_int==4){return(1.+sin(vec4(0.,0.5,1.,0.)-t/3.3+(2.+controls.w*5.)*(uv.x+uv.y))).rgb*1.2;}
    if(p_int==5){vec3 N1=vec3(noise(p*0.5+time*0.2));vec3 N2=vec3(noise(p*(1.5+controls.w*3.)-time*0.1));return mix(vec3(N2.x,N1.y,N2.z),vec3(N1.x,N2.y,N1.z),controls.w)*vec3(0.5,2.,1.2);}
    if(p_int==6){vec3 grid=abs(fract(p*0.5)-0.5)/(abs(n)+0.1);float lines=pow(min(min(grid.x,grid.y),grid.z),0.1+controls.w*0.4);float pulse=sin(p.x*(1.+controls.w*5.)-time)*0.5+0.5;return vec3(lines*pulse*2.,lines*1.5,lines*4.);}
    if(p_int==7){float stretch=fract((p.z+n.x)*0.2+time);stretch=pow(stretch,5.*(1.+controls.w*3.));return hsv2rgb(vec3(fract(p.z*0.05),1.,stretch*5.));}
    if(p_int==8){for(int i=0;i<int(controls.w*5.);i++)face_uv=abs(face_uv);face_uv=vec2(atan(face_uv.y,face_uv.x),length(face_uv));face_uv.x+=sin(face_uv.y*2.-time)*0.5;return hsv2rgb(vec3(fract(face_uv.x/6.283),1.,pow(face_uv.y*0.1,0.5)));}
    if(p_int==9){float f=0.;mat2 m=mat2(1.6,1.2,-1.2,1.6);p*=0.5;for(int i=0;i<3+int(controls.w*4.);i++){f+=noise(p)*pow(0.5,float(i));p.xy*=m;}return hsv2rgb(vec3(f+time*0.05,0.8,1.));}
    if(p_int==10){vec3 d=vec3(0.);for(float i=1.;i<4.+8.*controls.w;i+=1.){d.x+=sin(length(p+vec3(i*0.5,0,0))-time*2.0);d.y+=cos(length(p+vec3(0,i*0.5,0))-time*2.0);d.z+=sin(length(p-vec3(0,0,i*0.5))-time*2.0);}return normalize(d)*0.5+0.5;}
    if(p_int==11){float v=0.;float dot_size=mix(0.4,0.4,controls.w);for(int i=1;i<4;++i){vec3 g=floor(p*float(i)*0.5);float id=g.x+g.y*157.+113.*g.z;vec3 f=fract(p*float(i)*0.5);vec3 rand_pos=vec3(hash2(g.xy+g.z),hash11(id*3.14));v+=smoothstep(dot_size,dot_size-0.1,length(f-rand_pos-vec3(0,sin(time+id),0)));}return vec3(v)*hsv2rgb(vec3(fract(p.z*0.1+time*0.1),1.,1.));}
    if(p_int==12){vec2 p_polar=vec2(atan(face_uv.y,face_uv.x),log(length(face_uv)));float k=1.+controls.w*10.;float a=p_polar.x+time*0.5;a=floor(a*k)/k;p_polar=vec2(p_polar.y*cos(a)-a*sin(a),p_polar.y*sin(a)+a*cos(a));return hsv2rgb(vec3(fract(p_polar.x*.2+time*.1),1.,smoothstep(0.,1.,fract(p_polar.y*5.))));}
    if(p_int==13){vec2 g=floor(face_uv*3.),f=fract(face_uv*3.)-.5;float d=1e9;vec3 c;for(int i=-1;i<=1;i++)for(int j=-1;j<=1;j++){vec2 o=hash2(g+vec2(i,j));float D=length(f-o+.5);if(D<d){d=D;c=hsv2rgb(vec3(hash11(g.x+g.y*157.+time*.1),.7,.9));}}return(1.-smoothstep(0.,.8,d))*c;}
    if(p_int==14){float n=noise(p*0.5);float pattern=sin(n*15.+time*2.+sin(p.y+n)*2.);pattern=smoothstep(0.,1.,pattern);return hsv2rgb(vec3(fract(n+time*0.1),0.8,pattern));}
    if(p_int==15){vec2 p_warp=face_uv;for(int i=0;i<3+int(controls.w*5.);i++){p_warp=abs(p_warp)/dot(p_warp,p_warp)-.8+sin(time*0.2)*0.1;}return hsv2rgb(vec3(fract(p_warp.x*.2),1.,1.));}
    if(p_int==16){vec2 st=floor(face_uv/10.*(5.+controls.w*20.));float h=hash11(st.x+st.y*157.);float on=step(0.5,fract(h*10.+time*h));return vec3(on)*hsv2rgb(vec3(fract(h*3.),1.,1.));}
    
    return vec3(0.8);
}

// --- FXAA Implementation by @XorDev ---
vec4 fxaa(sampler2D tex,vec2 uv,vec2 r){const float m=8.,n=1./128.,o=1./32.;const vec3 l=vec3(0.299,0.587,0.114);vec3 c=texture(tex,uv).rgb,p=texture(tex,uv+vec2(-.5,-.5)*r).rgb,q=texture(tex,uv+vec2(.5,-.5)*r).rgb,s=texture(tex,uv+vec2(-.5,.5)*r).rgb,v=texture(tex,uv+vec2(.5,.5)*r).rgb;float d=dot(c,l),e=dot(p,l),f=dot(q,l),g=dot(s,l),h=dot(v,l);vec2 j=vec2((g+h)-(e+f),(e+g)-(f+h));float k=max((e+f+g+h)*o,n),a=1./(min(abs(j.x),abs(j.y))+k);j=clamp(j*a,-m,m)*r;vec4 A=.5*(texture(tex,uv-j*(1./6.))+texture(tex,uv+j*(1./6.))),B=A*.5+.25*(texture(tex,uv-j*.5)+texture(tex,uv+j*.5));float b=min(d,min(min(e,f),min(g,h))),i=max(d,max(max(e,f),max(g,h)));return dot(B.rgb,l)<b||dot(B.rgb,l)>i?A:B;}

void main() {
    if (PASSINDEX == 0) { // Time & Speed Buffer
        vec4 pD=IMG_NORM_PIXEL(timeBuffer,vec2(0.5));
        float aT=pD.r, cS=pD.g, aCT=pD.b, cAS=pD.a;
        float nS=mix(cS,speed,min(1.,TIMEDELTA*paramSmoothness));
        float nAS=mix(cAS,colorAnimSpeed,min(1.,TIMEDELTA*paramSmoothness));
        float nT=aT+nS*TIMEDELTA;
        float nCT=aCT+nAS*TIMEDELTA;
        // On the first frame, use the startTime offset
        if(FRAMEINDEX==0){
            nT=startTime;
            nS=speed;
            nCT=startTime;
            nAS=colorAnimSpeed;
        }
        gl_FragColor=vec4(nT,nS,nCT,nAS);
    }
    else if (PASSINDEX == 1) { // Color Palette Buffer
        // Switch is now instantaneous
        gl_FragColor=vec4(float(colorPalette),0,0,1);
    }
    else if (PASSINDEX == 2) { // Controls Buffer
        vec4 pV=IMG_NORM_PIXEL(controlsBuffer,vec2(0.5));
        vec4 tV=vec4(0., colorBrightness, patternScale, patternComplexity);
        
        // On the first frame, set the values directly to avoid accumulation
        if (FRAMEINDEX == 0) {
            gl_FragColor = tV;
        } 
        // On all other frames, smoothly transition between values
        else {
            gl_FragColor=mix(pV,tV,min(1.,TIMEDELTA*paramSmoothness));
        }
    }
    else if (PASSINDEX == 3) { // Scene Render
        vec4 tD=IMG_NORM_PIXEL(timeBuffer,vec2(0.5)),cD=IMG_NORM_PIXEL(colorBuffer,vec2(0.5)),ctrl=IMG_NORM_PIXEL(controlsBuffer,vec2(0.5));
        float rotTime=tD.r;
        float colorTime=tD.b;
        float pI=cD.r;
        vec2 uv=(gl_FragCoord.xy-0.5*RENDERSIZE.xy)/RENDERSIZE.y;
        vec3 ro=vec3(0,0,-20),rd=normalize(vec3(uv,1));
        vec4 cA=rotTime+vec4(0,11,33,0);
        mat2 R=mat2(cos(cA.x),cos(cA.y),cos(cA.z),cos(cA.w)),R_inv=(1./(R[0][0]*R[1][1]-R[0][1]*R[1][0]))*mat2(R[1][1],-R[0][1],-R[1][0],R[0][0]);
        vec3 rot_ro=ro;rot_ro.yz*=R_inv;rot_ro.xz*=R_inv;
        vec3 rot_rd=rd;rot_rd.yz*=R_inv;rot_rd.xz*=R_inv;
        vec3 p_hit,n_hit;float v_hit,t=intersectVoxelCube(rot_ro,rot_rd,p_hit,n_hit,v_hit,rotTime);
        vec3 col=vec3(0);
        if(t>0.){
            vec3 raw_col=getColor(int(pI+0.5),colorTime,p_hit,n_hit,t,uv,ctrl, rot_rd);
            float diffuse=max(0.,dot(n_hit,normalize(vec3(0.5,0.8,-1.))))*0.7+0.3;
            col=raw_col*diffuse*ctrl.y; // Apply brightness control
        }
        gl_FragColor=vec4(aces_approx(col),1.);
    }
    else { // Final Pass with FXAA
        gl_FragColor=fxaa(sceneBuffer,isf_FragNormCoord,sqrt(2.)/RENDERSIZE.xy);
    }
}
