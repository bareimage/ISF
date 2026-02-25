/*{
    "DESCRIPTION": "A tumbling voxel cube with shiny metallic gold surface textured by a live Gray-Scott reaction-diffusion Turing pattern. Background trails past renders of the cube, enlarging fluidly. Deliberate edge-of-function errors at voxel boundaries create digital artifacts.",
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
            "NAME": "turingFeed",
            "TYPE": "float",
            "DEFAULT": 0.037,
            "MIN": 0.01,
            "MAX": 0.08,
            "LABEL": "Turing Feed Rate"
        },
        {
            "NAME": "turingKill",
            "TYPE": "float",
            "DEFAULT": 0.06,
            "MIN": 0.04,
            "MAX": 0.07,
            "LABEL": "Turing Kill Rate"
        },
        {
            "NAME": "bumpStrength",
            "TYPE": "float",
            "DEFAULT": 0.4,
            "MIN": 0.0,
            "MAX": 1.5,
            "LABEL": "Turing Bump Depth"
        },
        {
            "NAME": "trailIntensity",
            "TYPE": "float",
            "DEFAULT": 0.85,
            "MIN": 0.0,
            "MAX": 0.99,
            "LABEL": "Trail Persistence"
        },
        {
            "NAME": "trailZoom",
            "TYPE": "float",
            "DEFAULT": 0.006,
            "MIN": 0.0,
            "MAX": 0.03,
            "LABEL": "Trail Zoom"
        },
        {
            "NAME": "edgeGlitch",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Edge Glitch"
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
            "TARGET": "turingBuffer",
            "PERSISTENT": true,
            "FLOAT": true,
            "WIDTH": 256,
            "HEIGHT": 256
        },
        {
            "TARGET": "sceneBuffer",
            "FLOAT": true
        },
        {
            "TARGET": "trailBuffer",
            "PERSISTENT": true,
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

// --- Constants ---
#define PI 3.14159265359
#define CUBE_SIZE 13.0
#define EPS 0.001
#define TURING_RES 256.0

// --- Utility Functions ---
vec3 hsv2rgb(vec3 c) { vec4 K=vec4(1.,2./3.,1./3.,3.); vec3 p=abs(fract(c.xxx+K.xyz)*6.-K.www); return c.z*mix(K.xxx,clamp(p-K.xxx,0.,1.),c.y); }
float hash11(float p) { return fract(sin(p*727.1)*435.545); }
float hash2d(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
vec3 aces_approx(vec3 v) {
    v=max(v,0.)*0.8; float a=2.51,b=0.03,c=2.43,d=0.59,e=0.14;
    return clamp((v*(a*v+b))/(v*(c*v+d)+e),0.,1.);
}

// --- Voxel Cube Data (13x13 face arrays) ---
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

// --- Voxel Cube Rendering Infrastructure ---
bool voxelMap(in vec3 p,out float v,float t){float last=CUBE_SIZE-1.;vec2 uv;int i=-1;float cV=0.;v=0.;if(p.z>-EPS&&p.z<EPS){uv=p.xy;if(all(greaterThanEqual(uv,vec2(0)))&&all(lessThan(uv,vec2(CUBE_SIZE)))){i=int(uv.y)*int(CUBE_SIZE)+int(uv.x);cV=faceFront[i];}}else if(p.z>last-EPS&&p.z<last+EPS){uv=p.xy;if(all(greaterThanEqual(uv,vec2(0)))&&all(lessThan(uv,vec2(CUBE_SIZE)))){i=int(uv.y)*int(CUBE_SIZE)+int(uv.x);cV=faceBack[i];}}else if(p.y>-EPS&&p.y<EPS){uv=p.xz;if(all(greaterThanEqual(uv,vec2(0)))&&all(lessThan(uv,vec2(CUBE_SIZE)))){i=int(uv.y)*int(CUBE_SIZE)+int(uv.x);cV=faceBottom[i];}}else if(p.y>last-EPS&&p.y<last+EPS){uv=p.xz;if(all(greaterThanEqual(uv,vec2(0)))&&all(lessThan(uv,vec2(CUBE_SIZE)))){i=int(uv.y)*int(CUBE_SIZE)+int(uv.x);cV=faceTop[i];}}else if(p.x>-EPS&&p.x<EPS){uv=p.zy;if(all(greaterThanEqual(uv,vec2(0)))&&all(lessThan(uv,vec2(CUBE_SIZE)))){i=int(uv.y)*int(CUBE_SIZE)+int(uv.x);cV=faceLeft[i];}}else if(p.x>last-EPS&&p.x<last+EPS){uv=p.zy;if(all(greaterThanEqual(uv,vec2(0)))&&all(lessThan(uv,vec2(CUBE_SIZE)))){i=int(uv.y)*int(CUBE_SIZE)+int(uv.x);cV=faceRight[i];}}return cV>0.5;}
bool IRayAABox(in vec3 ro,in vec3 rd,in vec3 invrd,in vec3 bmin,in vec3 bmax,out vec3 p0,out vec3 p1){vec3 t0=(bmin-ro)*invrd,t1=(bmax-ro)*invrd;vec3 tmin=min(t0,t1),tmax=max(t0,t1);float fmin=max(max(tmin.x,tmin.y),tmin.z),fmax=min(min(tmax.x,tmax.y),tmax.z);p0=ro+rd*fmin;p1=ro+rd*fmax;return fmax>=fmin;}
vec3 AABoxNormal(vec3 bmin,vec3 bmax,vec3 p){vec3 c=(bmin+bmax)*0.5,d=p-c,ad=abs(d);if(ad.x>ad.y&&ad.x>ad.z)return vec3(sign(d.x),0,0);if(ad.y>ad.z)return vec3(0,sign(d.y),0);return vec3(0,0,sign(d.z));}
bool traceVoxelGrid(in vec3 i_ro,in vec3 rd,in vec3 invrd,in vec3 bmin,in vec3 bmax,out vec3 n,out vec3 p,out float v,float t){n=vec3(0);p=vec3(0);v=0;vec3 ro,e;if(!IRayAABox(i_ro,rd,invrd,bmin,bmax,ro,e))return false;if(dot(e-ro,rd)<EPS)return false;vec3 ep=floor(ro+rd*EPS);bool ret=false;for(int i=0;i<64;++i){if(voxelMap(ep,v,t)){ret=true;break;}vec3 d0;IRayAABox(ro-rd*2.,rd,invrd,ep,ep+1.,d0,ro);ep=floor(ro+rd*EPS);if(dot(e-ro,rd)<EPS){ret=false;break;}}if(ret){n=AABoxNormal(ep,ep+1.,ro);p=ro;}return ret;}
float intersectVoxelCube(in vec3 ro,in vec3 rd,out vec3 p_hit,out vec3 n_hit,out float v_hit,float t){const float WSIZE=CUBE_SIZE;vec3 bmin_w=-vec3(WSIZE*0.5),bmax_w=vec3(WSIZE*0.5);vec3 invrd=1./rd,p_entry,p_exit;if(!IRayAABox(ro,rd,invrd,bmin_w,bmax_w,p_entry,p_exit))return-1.;vec3 ro_s=p_entry;if(dot(p_entry-ro,rd)<0.)ro_s=ro;vec3 ro_v=(ro_s-bmin_w)/WSIZE*CUBE_SIZE,rd_v=rd/WSIZE*CUBE_SIZE,invrd_v=1./rd_v;vec3 n_v,p_v;if(traceVoxelGrid(ro_v,rd_v,invrd_v,vec3(0),vec3(CUBE_SIZE),n_v,p_v,v_hit,t)){p_hit=bmin_w+(p_v/CUBE_SIZE)*WSIZE;n_hit=normalize(n_v);if(distance(ro,p_hit)>distance(ro,p_exit)+EPS)return-1.;return distance(ro,p_hit);}return-1.;}

// --- Gold Material with Turing Texture and Edge Errors ---
vec3 goldRender(vec3 p_world, vec3 n_hit, vec3 rd, float bumpStr, float eGlitch) {
    // Voxel-space position for face UV extraction
    vec3 p_vox = p_world + vec3(CUBE_SIZE * 0.5);

    // Determine face UV from surface normal
    vec2 faceUV;
    if (abs(n_hit.z) > 0.9)      faceUV = p_vox.xy;
    else if (abs(n_hit.y) > 0.9) faceUV = p_vox.xz;
    else                         faceUV = p_vox.zy;

    // Normalize to 0-1 for Turing buffer sampling
    vec2 tUV = clamp(faceUV / CUBE_SIZE, 0.0, 1.0);
    vec2 tEps = vec2(1.0 / TURING_RES, 0.0);

    // --- Edge Detection (voxel cell boundaries) ---
    vec2 cellFrac = fract(faceUV);
    vec2 edgeDist = min(cellFrac, 1.0 - cellFrac);
    float nearEdge = 1.0 - smoothstep(0.0, 0.15, min(edgeDist.x, edgeDist.y));

    // --- Turing Pattern Sampling with Chromatic Aberration at Edges ---
    float caShift = nearEdge * eGlitch * 0.04;
    float tR = IMG_NORM_PIXEL(turingBuffer, tUV + vec2(caShift, 0.0)).g;
    float tG = IMG_NORM_PIXEL(turingBuffer, tUV).g;
    float tB = IMG_NORM_PIXEL(turingBuffer, tUV - vec2(0.0, caShift)).g;
    float tCenter = tG;

    // --- Bump Mapping from Turing Pattern ---
    float tRight = IMG_NORM_PIXEL(turingBuffer, tUV + tEps.xy).g;
    float tUp    = IMG_NORM_PIXEL(turingBuffer, tUV + tEps.yx).g;
    vec3 bn = n_hit;
    bn.x += (tCenter - tRight) * bumpStr * 4.0;
    bn.y += (tCenter - tUp) * bumpStr * 4.0;
    bn = normalize(bn);

    // --- Gold Base Color modulated by Turing Pattern ---
    vec3 goldLight = vec3(1.0, 0.84, 0.0);
    vec3 goldDark  = vec3(0.55, 0.35, 0.05);
    vec3 goldSpec  = vec3(1.0, 0.95, 0.7);

    // Mix Turing chromatic aberration into gold tones
    vec3 turingTint = vec3(tR, tG, tB);
    vec3 baseCol = mix(goldDark, goldLight, tCenter);
    baseCol = mix(baseCol, turingTint * goldLight, nearEdge * eGlitch * 0.4);

    // --- Two-Light Setup ---
    vec3 L1 = normalize(vec3(0.5, 0.8, -0.6));
    vec3 L2 = normalize(vec3(-0.3, 0.4, 0.7));
    float diff1 = max(dot(bn, L1), 0.0) * 0.7 + 0.3;
    float diff2 = max(dot(bn, L2), 0.0) * 0.25;

    // --- Specular (Blinn-Phong, high power for metallic sheen) ---
    vec3 H1 = normalize(L1 - rd);
    vec3 H2 = normalize(L2 - rd);
    float spec1 = pow(max(dot(bn, H1), 0.0), 128.0);
    float spec2 = pow(max(dot(bn, H2), 0.0), 64.0) * 0.4;

    // --- Fresnel (Schlick approximation for metallic gold) ---
    float NdotV = max(dot(bn, -rd), 0.0);
    float fresnel = pow(1.0 - NdotV, 5.0);
    fresnel = mix(0.1, 1.0, fresnel);

    // --- Environment Reflection (procedural gradient) ---
    vec3 reflDir = reflect(rd, bn);
    vec3 envCol = mix(vec3(0.15, 0.08, 0.02), vec3(0.6, 0.4, 0.15),
                      reflDir.y * 0.5 + 0.5);
    envCol += vec3(0.2, 0.15, 0.05) * max(0.0, reflDir.z);

    // --- Compose Material ---
    vec3 col = baseCol * (diff1 + diff2);
    col += goldSpec * (spec1 + spec2);
    col += envCol * fresnel * 0.5;

    // --- Edge-of-Function Errors ---
    // atan discontinuity lines (wraps at +/- PI)
    float atanErr = abs(fract(atan(p_world.y, p_world.x) * 3.0 / PI) - 0.5);
    float funcError = smoothstep(0.02, 0.0, atanErr) * nearEdge * eGlitch;

    // fract precision spike at cell boundaries
    float fractErr = smoothstep(0.01, 0.0, min(edgeDist.x, edgeDist.y));
    fractErr *= eGlitch * 0.8;

    // Apply edge artifacts
    col += goldSpec * funcError * 0.6;
    col += vec3(1.0, 0.9, 0.4) * fractErr * nearEdge;
    col += goldLight * nearEdge * eGlitch * 0.15;

    return col;
}

// --- FXAA by @XorDev ---
vec4 fxaa(sampler2D tex,vec2 uv,vec2 r){const float m=8.,n=1./128.,o=1./32.;const vec3 l=vec3(0.299,0.587,0.114);vec3 c=texture(tex,uv).rgb,p=texture(tex,uv+vec2(-.5,-.5)*r).rgb,q=texture(tex,uv+vec2(.5,-.5)*r).rgb,s=texture(tex,uv+vec2(-.5,.5)*r).rgb,v=texture(tex,uv+vec2(.5,.5)*r).rgb;float d=dot(c,l),e=dot(p,l),f=dot(q,l),g=dot(s,l),h=dot(v,l);vec2 j=vec2((g+h)-(e+f),(e+g)-(f+h));float k=max((e+f+g+h)*o,n),a=1./(min(abs(j.x),abs(j.y))+k);j=clamp(j*a,-m,m)*r;vec4 A=.5*(texture(tex,uv-j*(1./6.))+texture(tex,uv+j*(1./6.))),B=A*.5+.25*(texture(tex,uv-j*.5)+texture(tex,uv+j*.5));float b=min(d,min(min(e,f),min(g,h))),i=max(d,max(max(e,f),max(g,h)));return dot(B.rgb,l)<b||dot(B.rgb,l)>i?A:B;}

// --- Main Multi-Pass Pipeline ---
void main() {
    float sf = min(1.0, TIMEDELTA * transitionSpeed);

    // ==========================================
    // Pass 0: Time Buffer (accTime, smSpeed, smFeed, smKill)
    // ==========================================
    if (PASSINDEX == 0) {
        vec4 prev = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        float accTime, smSpeed, smFeed, smKill;
        if (FRAMEINDEX == 0) {
            accTime = 0.0;
            smSpeed = speed;
            smFeed = turingFeed;
            smKill = turingKill;
        } else {
            smSpeed = mix(prev.g, speed, sf);
            smFeed  = mix(prev.b, turingFeed, sf);
            smKill  = mix(prev.a, turingKill, sf);
            accTime = prev.r + smSpeed * TIMEDELTA;
        }
        gl_FragColor = vec4(accTime, smSpeed, smFeed, smKill);
    }

    // ==========================================
    // Pass 1: Param Buffer (smTrail, smZoom, smGlitch, smBump)
    // ==========================================
    else if (PASSINDEX == 1) {
        vec4 prev = IMG_NORM_PIXEL(paramBuffer, vec2(0.5));
        vec4 target = vec4(trailIntensity, trailZoom, edgeGlitch, bumpStrength);
        if (FRAMEINDEX == 0) gl_FragColor = target;
        else gl_FragColor = mix(prev, target, sf);
    }

    // ==========================================
    // Pass 2: Turing Buffer (Gray-Scott Reaction-Diffusion)
    // ==========================================
    else if (PASSINDEX == 2) {
        vec2 tSize = vec2(TURING_RES);
        vec2 texel = 1.0 / tSize;
        vec2 uv = gl_FragCoord.xy / tSize;

        // Retrieve smoothed feed/kill rates
        vec4 tData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        float f = tData.b;
        float k = tData.a;

        if (FRAMEINDEX < 5) {
            // Initialization: U=1 everywhere, V seeded with random spots
            float U = 1.0;
            float V = 0.0;
            vec2 pos = gl_FragCoord.xy;

            // Dense random seeding for fast pattern emergence
            float h = hash2d(floor(pos / 3.0));
            if (h > 0.7) V = hash2d(pos * 0.1) * 0.8;

            // Structured seed rings for organic nucleation
            float dist = length(pos - tSize * 0.5);
            if (dist > 25.0 && dist < 30.0) V = 1.0;
            if (dist > 55.0 && dist < 58.0) V = 0.9;
            if (dist > 85.0 && dist < 88.0) V = 0.8;

            gl_FragColor = vec4(U, V, 0.0, 1.0);
        } else {
            // Gray-Scott evolution step
            vec4 cC = IMG_NORM_PIXEL(turingBuffer, uv);
            vec4 cL = IMG_NORM_PIXEL(turingBuffer, uv + vec2(-texel.x, 0.0));
            vec4 cR = IMG_NORM_PIXEL(turingBuffer, uv + vec2( texel.x, 0.0));
            vec4 cU = IMG_NORM_PIXEL(turingBuffer, uv + vec2(0.0,  texel.y));
            vec4 cD = IMG_NORM_PIXEL(turingBuffer, uv + vec2(0.0, -texel.y));

            float lapU = cL.r + cR.r + cU.r + cD.r - 4.0 * cC.r;
            float lapV = cL.g + cR.g + cU.g + cD.g - 4.0 * cC.g;

            float Du = 0.21;
            float Dv = 0.105;
            float UVV = cC.r * cC.g * cC.g;

            float newU = cC.r + Du * lapU - UVV + f * (1.0 - cC.r);
            float newV = cC.g + Dv * lapV + UVV - (f + k) * cC.g;

            gl_FragColor = vec4(clamp(newU, 0.0, 1.0), clamp(newV, 0.0, 1.0), 0.0, 1.0);
        }
    }

    // ==========================================
    // Pass 3: Scene Buffer (Cube Render)
    // ==========================================
    else if (PASSINDEX == 3) {
        vec4 tData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        vec4 pData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5));
        float rotTime = tData.r;
        float eGlitch = pData.b;
        float bumpStr = pData.a;

        vec2 uv = (gl_FragCoord.xy - 0.5 * RENDERSIZE.xy) / RENDERSIZE.y;
        vec3 ro = vec3(0.0, 0.0, -20.0);
        vec3 rd = normalize(vec3(uv, 1.0));

        // Rotation (same matrix approach as EmoCube)
        vec4 cA = rotTime + vec4(0, 11, 33, 0);
        mat2 R = mat2(cos(cA.x), cos(cA.y), cos(cA.z), cos(cA.w));
        mat2 R_inv = (1.0 / (R[0][0]*R[1][1] - R[0][1]*R[1][0]))
                     * mat2(R[1][1], -R[0][1], -R[1][0], R[0][0]);

        vec3 rot_ro = ro; rot_ro.yz *= R_inv; rot_ro.xz *= R_inv;
        vec3 rot_rd = rd; rot_rd.yz *= R_inv; rot_rd.xz *= R_inv;

        vec3 p_hit, n_hit;
        float v_hit;
        float t = intersectVoxelCube(rot_ro, rot_rd, p_hit, n_hit, v_hit, rotTime);

        vec3 col = vec3(0.0);
        if (t > 0.0) {
            col = goldRender(p_hit, n_hit, rot_rd, bumpStr, eGlitch);
        }

        gl_FragColor = vec4(aces_approx(col), 1.0);
    }

    // ==========================================
    // Pass 4: Trail Buffer (Feedback Zoom Composite)
    // ==========================================
    else if (PASSINDEX == 4) {
        vec4 pData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5));
        float trailDecay = pData.r;
        float zoomAmt = pData.g;

        vec2 uv = isf_FragNormCoord;

        // Read previous trail, zoomed toward center (enlarging effect)
        vec2 zoomedUV = (uv - 0.5) * (1.0 - zoomAmt) + 0.5;
        vec3 trail = IMG_NORM_PIXEL(trailBuffer, zoomedUV).rgb;

        // Fade previous trail
        trail *= trailDecay;

        // Slight color shift on the trail for fluid feel
        trail.rg *= vec2(0.998, 0.995);

        // Read current scene
        vec3 scene = IMG_NORM_PIXEL(sceneBuffer, uv).rgb;
        float sceneLum = max(max(scene.r, scene.g), scene.b);

        // Composite: scene overwrites where cube is, trail shows in background
        float mask = smoothstep(0.0, 0.02, sceneLum);
        vec3 result = trail * (1.0 - mask * 0.7) + scene;

        gl_FragColor = vec4(result, 1.0);
    }

    // ==========================================
    // Pass 5: Final Output (FXAA from trail buffer)
    // ==========================================
    else {
        gl_FragColor = fxaa(trailBuffer, isf_FragNormCoord, sqrt(2.0) / RENDERSIZE.xy);
    }
}