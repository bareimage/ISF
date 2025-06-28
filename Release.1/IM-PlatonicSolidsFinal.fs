/*{
  "DESCRIPTION": "Self Reflection with dampened manual rotations only",
  "CREDIT": "Converted to ISF 2.0 by dot2dot, adapted for persistent buffer usage by dot2dot, original code by @mrange (https://www.shadertoy.com/view/XfyXRV)",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR"],
  "INPUTS": [
    { "NAME": "rotationX", "TYPE": "float", "DEFAULT": 0.0, "MIN": -1.0, "MAX": 1.0, "LABEL": "X Rotation" },
    { "NAME": "rotationY", "TYPE": "float", "DEFAULT": 0.0, "MIN": -1.0, "MAX": 1.0, "LABEL": "Y Rotation" },
    { "NAME": "rotationZ", "TYPE": "float", "DEFAULT": 0.0, "MIN": -1.0, "MAX": 1.0, "LABEL": "Z Rotation" },
    { "NAME": "innerSphere", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.5, "MAX": 1.5, "LABEL": "Inner Sphere Size" },
    { "NAME": "polyZoom", "TYPE": "float", "DEFAULT": 2.0, "MIN": 1.0, "MAX": 4.0, "LABEL": "Poly Zoom" },
    { "NAME": "smoothingFactor", "TYPE": "float", "DEFAULT": 4.0, "MIN": 0.1, "MAX": 16.0, "LABEL": "Smoothing" }
  ],
  "PASSES": [
    { "TARGET": "rotationBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "finalOutput" }
  ]
}*/

#define PI 3.141592654
#define MAX_BOUNCES 6
#define TOLERANCE 0.0005
#define MAX_RAY_MARCHES 50
#define NORM_OFF 0.005
#define TOLERANCE3 0.0005
#define MAX_RAY_LENGTH 10.0
#define MAX_RAY_MARCHES3 90
#define NORM_OFF3 0.005

mat3 g_rot;
vec2 g_gd;

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

// Rotation matrices
mat3 rotX(float a) { float c=cos(a),s=sin(a); return mat3(1,0,0,0,c,-s,0,s,c); }
mat3 rotY(float a) { float c=cos(a),s=sin(a); return mat3(c,0,s,0,1,0,-s,0,c); }
mat3 rotZ(float a) { float c=cos(a),s=sin(a); return mat3(c,-s,0,s,c,0,0,0,1); }

// Helper functions
vec3 hsv2rgb(vec3 c) {
  vec4 K = vec4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
  vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
  return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

vec3 aces_approx(vec3 v) {
  v = max(v, 0.0);
  v *= 0.6;
  float a = 2.51;
  float b = 0.03;
  float c = 2.43;
  float d = 0.59;
  float e = 0.14;
  return clamp((v*(a*v+b))/(v*(c*v+d)+e), 0.0, 1.0);
}

float sphere(vec3 p, float r) {
  return length(p) - r;
}

float box(vec2 p, vec2 b) {
  vec2 d = abs(p)-b;
  return length(max(d,0.0)) + min(max(d.x,d.y),0.0);
}

void poly_fold(inout vec3 pos, int poly_type, vec3 poly_nc) {
  vec3 p = pos;
  for(int i = 0; i < 3; ++i) {
    p.xy = abs(p.xy);
    p -= 2.0*min(0.0, dot(p,poly_nc)) * poly_nc;
  }
  pos = p;
}

float poly_plane(vec3 pos, vec3 poly_pab, vec3 poly_pbc, vec3 poly_pca) {
  float d0 = dot(pos, poly_pab);
  float d1 = dot(pos, poly_pbc);
  float d2 = dot(pos, poly_pca);
  float d = d0;
  d = max(d, d1);
  d = max(d, d2);
  return d;
}

float poly_corner(vec3 pos) {
  float d = length(pos) - 0.0125;
  return d;
}

float dot2(vec3 p) {
  return dot(p, p);
}

float poly_edge(vec3 pos, vec3 poly_nc) {
  float dla = dot2(pos-min(0.0, pos.x)*vec3(1.0, 0.0, 0.0));
  float dlb = dot2(pos-min(0.0, pos.y)*vec3(0.0, 1.0, 0.0));
  float dlc = dot2(pos-min(0.0, dot(pos, poly_nc))*poly_nc);
  return sqrt(min(min(dla, dlb), dlc))-2E-3;
}

vec3 shape(vec3 pos, vec3 poly_p, vec3 poly_nc, vec3 poly_pab, vec3 poly_pbc, vec3 poly_pca, int poly_type) {
  pos *= g_rot;
  pos /= polyZoom;
  poly_fold(pos, poly_type, poly_nc);
  pos -= poly_p;
  return vec3(poly_plane(pos, poly_pab, poly_pbc, poly_pca), poly_edge(pos, poly_nc), poly_corner(pos))*polyZoom;
}

vec3 render0(vec3 ro, vec3 rd, vec3 sunDir, vec3 sunCol, vec3 bottomBoxCol, vec3 topBoxCol) {
  vec3 col = vec3(0.0);
  float srd = sign(rd.y);
  float tp = -(ro.y-6.0)/abs(rd.y);
  if (srd < 0.0) {
    col += bottomBoxCol*exp(-0.5*(length((ro + tp*rd).xz)));
  }
  if (srd > 0.0) {
    vec3 pos = ro + tp*rd;
    vec2 pp = pos.xz;
    float db = box(pp, vec2(5.0, 9.0))-3.0;
    col += topBoxCol*rd.y*rd.y*smoothstep(0.25, 0.0, db);
    col += 0.2*topBoxCol*exp(-0.5*max(db, 0.0));
    col += 0.05*sqrt(topBoxCol)*max(-db, 0.0);
  }
  col += sunCol/(1.001-dot(sunDir, rd));
  return col; 
}

float df2(vec3 p, vec3 poly_p, vec3 poly_nc, vec3 poly_pab, vec3 poly_pbc, vec3 poly_pca, int poly_type) {
  vec3 ds = shape(p, poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type);
  float d2 = ds.y-5E-3;
  float d0 = min(-ds.x, d2);
  float d1 = sphere(p, innerSphere);
  g_gd = min(g_gd, vec2(d2, d1));
  float d = (min(d0, d1));
  return d;
}

float rayMarch2(vec3 ro, vec3 rd, float tinit, vec3 poly_p, vec3 poly_nc, vec3 poly_pab, vec3 poly_pbc, vec3 poly_pca, int poly_type) {
  float t = tinit;
  vec2 dti = vec2(1e10,0.0);
  int i;
  for (i = 0; i < MAX_RAY_MARCHES; ++i) {
    float d = df2(ro + rd*t, poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type);
    if (d<dti.x) { dti=vec2(d,t); }
    if (d < TOLERANCE) {
      break;
    }
    t += d;
  }
  if(i==MAX_RAY_MARCHES) { t=dti.y; }
  return t;
}

vec3 normal2(vec3 pos, vec3 poly_p, vec3 poly_nc, vec3 poly_pab, vec3 poly_pbc, vec3 poly_pca, int poly_type) {
  vec2 eps = vec2(NORM_OFF,0.0);
  vec3 nor;
  nor.x = df2(pos+eps.xyy, poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type) - df2(pos-eps.xyy, poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type);
  nor.y = df2(pos+eps.yxy, poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type) - df2(pos-eps.yxy, poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type);
  nor.z = df2(pos+eps.yyx, poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type) - df2(pos-eps.yyx, poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type);
  return normalize(nor);
}

vec3 render2(vec3 ro, vec3 rd, float db, vec3 poly_p, vec3 poly_nc, vec3 poly_pab, vec3 poly_pbc, vec3 poly_pca, int poly_type, vec3 sunDir, vec3 sunCol, vec3 bottomBoxCol, vec3 topBoxCol, vec3 glowCol0, vec3 glowCol1, vec3 beerCol) {
  vec3 agg = vec3(0.0);
  float ragg = 1.0;
  float tagg = 0.0;
  float refr_index = 0.9;
  float rrefr_index = 1.0/refr_index;
  for (int bounce = 0; bounce < MAX_BOUNCES; ++bounce) {
    if (ragg < 0.1) break;
    g_gd = vec2(1E3);
    float t2 = rayMarch2(ro, rd, min(db+0.05, 0.3), poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type);
    vec2 gd2 = g_gd;
    tagg += t2;
    vec3 p2 = ro+rd*t2;
    vec3 n2 = normal2(p2, poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type);
    vec3 r2 = reflect(rd, n2);
    vec3 rr2 = refract(rd, n2, rrefr_index);
    float fre2 = 1.0+dot(n2,rd);
    vec3 beer = ragg*exp(0.2*beerCol*tagg);
    agg += glowCol1*beer*((1.0+tagg*tagg*4E-2)*6.0/max(gd2.x, 5E-4+tagg*tagg*2E-4/ragg));
    vec3 ocol = 0.2*beer*render0(p2, rr2, sunDir, sunCol, bottomBoxCol, topBoxCol);
    if (gd2.y <= TOLERANCE) {
      ragg *= 1.0-0.9*fre2;
    } else {
      agg += ocol;
      ragg *= 0.8;
    }
    ro = p2;
    rd = r2;
    db = gd2.x; 
  }
  return agg;
}

float df3(vec3 p, vec3 poly_p, vec3 poly_nc, vec3 poly_pab, vec3 poly_pbc, vec3 poly_pca, int poly_type) {
  vec3 ds = shape(p, poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type);
  g_gd = min(g_gd, ds.yz);
  float sw = 0.02;
  float d1 = min(ds.y, ds.z)-sw;
  float d0 = ds.x;
  d0 = min(d0, ds.y);
  d0 = min(d0, ds.z);
  return d0;
}

float rayMarch3(vec3 ro, vec3 rd, float tinit, out int iter, vec3 poly_p, vec3 poly_nc, vec3 poly_pab, vec3 poly_pbc, vec3 poly_pca, int poly_type) {
  float t = tinit;
  int i;
  for (i = 0; i < MAX_RAY_MARCHES3; ++i) {
    float d = df3(ro + rd*t, poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type);
    if (d < TOLERANCE3 || t > MAX_RAY_LENGTH) {
      break;
    }
    t += d;
  }
  iter = i;
  return t;
}

vec3 normal3(vec3 pos, vec3 poly_p, vec3 poly_nc, vec3 poly_pab, vec3 poly_pbc, vec3 poly_pca, int poly_type) {
  vec2 eps = vec2(NORM_OFF3,0.0);
  vec3 nor;
  nor.x = df3(pos+eps.xyy, poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type) - df3(pos-eps.xyy, poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type);
  nor.y = df3(pos+eps.yxy, poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type) - df3(pos-eps.yxy, poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type);
  nor.z = df3(pos+eps.yyx, poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type) - df3(pos-eps.yyx, poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type);
  return normalize(nor);
}

vec3 render3(vec3 ro, vec3 rd, vec3 poly_p, vec3 poly_nc, vec3 poly_pab, vec3 poly_pbc, vec3 poly_pca, int poly_type, vec3 sunDir, vec3 sunCol, vec3 bottomBoxCol, vec3 topBoxCol, vec3 glowCol0, vec3 glowCol1, vec3 beerCol) {
  int iter;
  vec3 skyCol = render0(ro, rd, sunDir, sunCol, bottomBoxCol, topBoxCol);
  vec3 col = skyCol;
  g_gd = vec2(1E3);
  float t1 = rayMarch3(ro, rd, 0.1, iter, poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type);
  vec2 gd1 = g_gd;
  vec3 p1 = ro+t1*rd;
  vec3 n1 = normal3(p1, poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type);
  vec3 r1 = reflect(rd, n1);
  float refr_index = 0.9;
  vec3 rr1 = refract(rd, n1, refr_index);
  float fre1 = 1.0+dot(rd, n1);
  fre1 *= fre1;
  float ifo = mix(0.5, 1.0, smoothstep(1.0, 0.9, float(iter)/float(MAX_RAY_MARCHES3)));
  if (t1 < MAX_RAY_LENGTH) {
    col = render0(p1, r1, sunDir, sunCol, bottomBoxCol, topBoxCol)*(0.5+0.5*fre1)*ifo;
    vec3 icol = render2(p1, rr1, gd1.x, poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type, sunDir, sunCol, bottomBoxCol, topBoxCol, glowCol0, glowCol1, beerCol); 
    if (gd1.x > TOLERANCE3 && gd1.y > TOLERANCE3 && rr1 != vec3(0.0)) {
      col += icol*(1.0-0.75*fre1)*ifo;
    }
  }
  col += (glowCol0+1.0*fre1*(glowCol0))/max(gd1.x, 3E-4);
  return col;
}

vec3 effect(vec2 p, vec2 pp, float time, vec3 poly_p, vec3 poly_nc, vec3 poly_pab, vec3 poly_pbc, vec3 poly_pca, int poly_type, vec3 sunDir, vec3 sunCol, vec3 bottomBoxCol, vec3 topBoxCol, vec3 glowCol0, vec3 glowCol1, vec3 beerCol) {
  float fov = 2.0;
  vec3 rayOrigin = vec3(0.0, 1.0, -5.0);
  vec3 up = vec3(0.0, 1.0, 0.0);
  vec3 la = vec3(0.0);
  vec3 ww = normalize(la-rayOrigin);
  vec3 uu = normalize(cross(up, ww));
  vec3 vv = cross(ww, uu);
  vec3 rd = normalize(-p.x*uu + p.y*vv + fov*ww);
  vec3 col = render3(rayOrigin, rd, poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type, sunDir, sunCol, bottomBoxCol, topBoxCol, glowCol0, glowCol1, beerCol);
  col -= 2E-2*vec3(2.0,3.0,1.0)*(length(p)+0.25);
  col = aces_approx(col);
  col = sqrt(col);
  return col;
}

void main() {
    vec4 prevRotData;
    vec2 R, p, pp;
    int poly_type = 3;
    float poly_U = 1.0;
    float poly_V = 0.5;
    float poly_W = 1.0;
    float poly_cospin = cos(PI/float(poly_type));
    float poly_scospin = sqrt(0.75-poly_cospin*poly_cospin);
    vec3 poly_nc = vec3(-0.5, -poly_cospin, poly_scospin);
    vec3 poly_pab = vec3(0.0, 0.0, 1.0);
    vec3 poly_pbc = normalize(vec3(poly_scospin,0.0,0.5));
    vec3 poly_pca = normalize(vec3(0.0,poly_scospin,poly_cospin));
    vec3 poly_p = normalize(poly_U*poly_pab + poly_V*poly_pbc + poly_W*poly_pca);
    vec3 sunDir = normalize(vec3(0.0,-1.0,5.0));
    vec3 sunCol = hsv2rgb(vec3(0.06,0.9,0.01));
    vec3 bottomBoxCol = hsv2rgb(vec3(0.66,0.8,0.5));
    vec3 topBoxCol = hsv2rgb(vec3(0.60,0.9,1.0));
    vec3 glowCol0 = hsv2rgb(vec3(0.05,0.7,0.001));
    vec3 glowCol1 = hsv2rgb(vec3(0.95,0.7,0.001));
    vec3 beerCol = -hsv2rgb(vec3(0.65,0.7,2.0));
    if (PASSINDEX == 0) {
        prevRotData = IMG_NORM_PIXEL(rotationBuffer, vec2(0.5));
        vec3 prevRot = prevRotData.rgb;
        vec3 targetRot = vec3(rotationX, rotationY, rotationZ);
        vec3 newRot = (FRAMEINDEX == 0)
            ? targetRot
            : prevRot + (targetRot - prevRot) * min(smoothingFactor * TIMEDELTA, 1.0);
        gl_FragColor = vec4(newRot, 1.0);
    }
    else {
        prevRotData = IMG_NORM_PIXEL(rotationBuffer, vec2(0.5));
        vec3 rot = prevRotData.rgb * PI; // Map [-1,1] to [-π,π]
        g_rot = rotZ(rot.z) * rotY(rot.y) * rotX(rot.x);
        R = RENDERSIZE.xy;
        p = isf_FragNormCoord.xy * 2.0 - 1.0;
        pp = p;
        p.x *= R.x/R.y;
        vec3 col = effect(p, pp, 0.0, poly_p, poly_nc, poly_pab, poly_pbc, poly_pca, poly_type, 
                         sunDir, sunCol, bottomBoxCol, topBoxCol, glowCol0, glowCol1, beerCol);
        gl_FragColor = vec4(col, 1.0);
    }
}
