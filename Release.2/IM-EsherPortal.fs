/*{
  "DESCRIPTION": "Portal Terrain Effect with Escher-like distortions and smooth parameter transitions. Terrain noise calculated directly. Detail morphing controlled by inputs. Droste effect always on. Decoupled camera orbit speed.",
  "CREDIT": "Original Shadertoy by @tmst (https://www.shadertoy.com/view/tl3GW2), ISF 2.0 Version by @dot2dot (bareimage)",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR", "3D"],
  "INPUTS": [
    { "NAME": "animSpeed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 30.0, "LABEL": "Animation Speed" },
    { "NAME": "cameraOrbitSpeed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0, "LABEL": "Camera Orbit Speed" },
    { "NAME": "portalSize", "TYPE": "float", "DEFAULT": 0.25, "MIN": 0.04, "MAX": 0.6, "LABEL": "Portal Size" },
    { "NAME": "terrainDetailMorph", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 10.0, "LABEL": "Terrain Detail Morph" },
    { "NAME": "cloudDetailMorph", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 100.0, "LABEL": "Cloud Detail Morph" },
    { "NAME": "enableTwist", "TYPE": "bool", "DEFAULT": true, "LABEL": "Enable Twist Effect" },
    { "NAME": "transitionSpeed", "TYPE": "float", "DEFAULT": 2.0, "MIN": 0.1, "MAX": 10.0, "LABEL": "Parameter Smoothing" }
  ],
  "PASSES": [
    { "TARGET": "speedBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "sizeBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "morphBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
    { "TARGET": "finalOutput" }
  ]
}*/

#define TWOPI 6.283185307179586
#define IMAGE_ASPECT_WIDTH_OVER_HEIGHT 1.0
#define FIXED_UP vec3(0.0, 1.0, 0.0)
#define TAN_HALF_FOVY 0.7673269879789604
#define CAM_Z_NEAR 0.1
#define CAM_Z_FAR 50.0
#define MIN_DIST 0.005
#define MAX_DIST 50.0
#define RAY_STEPS 30
#define RAY_STEPS_SHADOW 10
#define POM_QUALITY 100
#define POM_QUALITY_REFL 40
#define TEX_SCALE 40.0
#define BUMP_TEX_DEPTH 0.12
#define BOUNDARY_RADIUS 0.2
#define PLANE_DEPTH 3.0
#define NV_PLANE_N vec3(0.0, 1.0, 0.0)
#define TWIST_EXPONENT 1.0

precision highp float;

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

// Noise and terrain generation functions
float rand(in vec2 p) {
    return fract(sin(dot(p,vec2(12.9898,78.233))) * 43758.5453);
}

float noise(in vec2 p) {
    vec2 pi = floor(p);
    vec2 pf = fract(p);
    float r00 = rand(vec2(pi.x    ,pi.y    ));
    float r10 = rand(vec2(pi.x+1.0,pi.y    ));
    float r01 = rand(vec2(pi.x    ,pi.y+1.0));
    float r11 = rand(vec2(pi.x+1.0,pi.y+1.0));
    return mix(mix(r00, r10, pf.x), mix(r01, r11, pf.x), pf.y);
}

// Modified fbm to accept a detailMorph parameter for subtle pattern variations
float fbm(vec2 uv, float detailMorph) { // detailMorph is 0-1
    vec2 p = uv*256.0;

    vec2 morphVecBase = vec2(0.47, 0.31); 
    float v = noise(p + detailMorph * morphVecBase * 1.5); 

    float scale_factor = 0.3; 

    p *= scale_factor;
    vec2 morphVecOctave2 = vec2(0.29, 0.53);
    v = mix(v, noise(p + detailMorph * morphVecOctave2 * 1.0), 0.8);

    p *= scale_factor;
    vec2 morphVecOctave3 = vec2(0.61, 0.37);
    v = mix(v, noise(p + detailMorph * morphVecOctave3 * 0.5), 0.8); 

    p *= scale_factor;
    vec2 morphVecOctave4 = vec2(0.43, 0.23);
    v = mix(v, noise(p + detailMorph * morphVecOctave4 * 0.25), 0.8); 

    return v;
}

float nearInt(float x) {
    return pow(0.5 + 0.5*cos(x*6.28),10.0);
}

// Modified fbmWithBorder to accept and pass detailMorph
float fbmWithBorder(vec2 uv, float detailMorph) {
    return mix(1.0, fbm(uv, detailMorph), (1.0-nearInt(uv.x))*(1.0-nearInt(uv.y)) );
}

float unmix(float a, float b, float x) {
    return (x - a)/(b - a);
}

// Utility functions for portal geometry (unchanged)
float lensq(vec3 p, vec3 q) { vec3 pq = q - p; return dot(pq, pq); }
float hitPlane(vec3 planePoint, vec3 nvPlaneN, vec3 p, vec3 v) { return dot(planePoint - p, nvPlaneN) / dot(v, nvPlaneN); }
mat4 getClipToWorld(float aspectWoverH, vec3 nvCamFw) { mat4 clipToEye = mat4( aspectWoverH * TAN_HALF_FOVY, 0.0, 0.0, 0.0, 0.0, TAN_HALF_FOVY, 0.0, 0.0, 0.0, 0.0,  0.0, (CAM_Z_NEAR - CAM_Z_FAR)/(2.0 * CAM_Z_NEAR * CAM_Z_FAR), 0.0, 0.0, -1.0, (CAM_Z_NEAR + CAM_Z_FAR)/(2.0 * CAM_Z_NEAR * CAM_Z_FAR) ); vec3 nvCamRt = normalize(cross(nvCamFw, FIXED_UP)); vec3 nvCamUp = cross(nvCamRt, nvCamFw); mat4 eyeToWorld = mat4( nvCamRt, 0.0, nvCamUp, 0.0, -nvCamFw, 0.0, 0.0, 0.0, 0.0, 1.0 ); return eyeToWorld * clipToEye; }
vec3 nvDirFromClip(mat4 clipToWorld, vec2 clip) { vec4 world = clipToWorld * vec4(clip, 1.0, 1.0); return normalize(world.xyz / world.w); }

// Closest point functions for portal geometry (unchanged)
vec3 cpPlane(vec3 planePoint, vec3 nvPlaneN, vec3 p) { float t = dot(p - planePoint, nvPlaneN); return p - t*nvPlaneN; }
vec3 cpSeg(vec3 q0, vec3 q1, vec3 p) { vec3 vEdge = q1 - q0; float t = dot(p - q0, vEdge) / dot(vEdge, vEdge); return q0 + clamp(t, 0.0, 1.0)*vEdge; }
vec3 cpTuple2(vec3 q0, vec3 q1, vec3 p) { vec3 q = q0; return mix(q, q1, step( lensq(p,q1), lensq(p,q) )); }
vec3 cpTuple3(vec3 q0, vec3 q1, vec3 q2, vec3 p) { vec3 q = cpTuple2(q0,q1, p); return mix(q, q2, step( lensq(p,q2), lensq(p,q) )); }
vec3 cpTuple4(vec3 q0, vec3 q1, vec3 q2, vec3 q3, vec3 p) { vec3 q = cpTuple3(q0,q1,q2, p); return mix(q, q3, step( lensq(p,q3), lensq(p,q) )); }
vec3 cpTriBoundary(vec3 q0, vec3 q1, vec3 q2, vec3 p) { return cpTuple3(cpSeg(q0,q1, p), cpSeg(q1,q2, p), cpSeg(q2,q0, p), p); }
vec3 cpQuadBoundary(vec3 q0, vec3 q1, vec3 q2, vec3 q3, vec3 p) { return cpTuple4( cpSeg(q0,q1, p), cpSeg(q1,q2, p), cpSeg(q2,q3, p), cpSeg(q3,q0, p), p ); }
float pointInTri(vec3 q0, vec3 q1, vec3 q2, vec3 p) { vec3 v01 = cross(q1-q0, p-q0); vec3 v12 = cross(q2-q1, p-q1); vec3 v20 = cross(q0-q2, p-q2); return step(0.0, dot(v01,v12)) * step(0.0, dot(v01,v20)); }
vec3 cpTri(vec3 q0, vec3 q1, vec3 q2, vec3 p) { vec3 nvPlaneN = normalize(cross(q1-q0, q2-q0)); vec3 xp = cpPlane(q0, nvPlaneN, p); return mix(cpTriBoundary(q0,q1,q2, p), xp, pointInTri(q0,q1,q2, xp)); }
vec3 cpQuad(vec3 q0, vec3 q1, vec3 q2, vec3 q3, vec3 p) { return cpTuple2(cpTri(q0,q1,q2, p), cpTri(q0,q2,q3, p), p); }
float sdQuadBoundary(vec3 q0, vec3 q1, vec3 q2, vec3 q3, float r, vec3 p) { vec3 x = cpQuadBoundary(q0,q1,q2,q3, p); return distance(x, p) - r; }
vec3 normalQuadBoundary(vec3 q0, vec3 q1, vec3 q2, vec3 q3, vec3 p) { vec3 x = cpQuadBoundary(q0,q1,q2,q3, p); return normalize(p - x); }
float sdQuad(vec3 q0, vec3 q1, vec3 q2, vec3 q3, float r, vec3 p) { vec3 x = cpQuad(q0,q1,q2,q3, p); return distance(x, p) - r; }


// Terrain and sky rendering functions - MODIFIED for direct calculation and detailMorph
vec4 bumpTex_directCalc(vec2 uv, float ter_detail_morph) {
    float r_val = fbmWithBorder(uv, ter_detail_morph);
    float ang = (r_val + fract(uv.x) + fract(uv.y)) * TWOPI;
    vec2 q = uv + 0.1*vec2( cos(ang), sin(ang) );
    float height_val = fbmWithBorder(q, ter_detail_morph);
    vec3 color = mix(vec3(0.0), vec3(1.0, 0.2, 0.1), height_val);
    return vec4(color, height_val);
}

vec2 skyTex_directCalc(vec2 uv, float currentTime, float cld_detail_morph) {
    float starVal = noise(uv*1024.0); 
    float starIntensity = unmix(0.95, 1.0, starVal) * step(0.95, starVal);
    
    float g_val_for_ang = fbmWithBorder(uv, cld_detail_morph); 
    float ang = (g_val_for_ang + currentTime*0.1) * TWOPI; // currentTime here is for cloud animation, not orbit
    vec2 q_sky = uv + 0.05*vec2( cos(ang), sin(ang) );
    float cloudIntensity = fbmWithBorder(q_sky, cld_detail_morph);
    
    return vec2(cloudIntensity, starIntensity);
}

vec3 bumpTexNormal(vec2 uv, float ter_detail_morph) { 
    vec2 uvPixel = 1.0 / RENDERSIZE.xy;
    float hSA = bumpTex_directCalc(uv + vec2(-uvPixel.s, 0.0), ter_detail_morph).a;
    float hSB = bumpTex_directCalc(uv + vec2( uvPixel.s, 0.0), ter_detail_morph).a;
    float hTA = bumpTex_directCalc(uv + vec2(0.0,-uvPixel.t), ter_detail_morph).a;
    float hTB = bumpTex_directCalc(uv + vec2(0.0, uvPixel.t), ter_detail_morph).a;

    vec2 dhdt = vec2(hSB-hSA, hTB-hTA) / (2.0 * uvPixel);
    vec2 gradh = BUMP_TEX_DEPTH * dhdt;
    return normalize(vec3( -gradh, 1.0 ));
}

vec3 skyColor(vec3 nvDir, float currentTime, float cld_detail_morph) { // currentTime for cloud animation
    float yy = clamp(nvDir.y+0.1, 0.0, 1.0);
    float horiz0 = pow(1.0 - yy, 30.0);
    float horiz1 = pow(1.0 - yy, 5.0);
    
    vec3 sv = nvDir - vec3(0.0, -1.0, 0.0);
    vec2 uvCloud = 0.25*(sv.xz / sv.y) + vec2(0.5);
    vec2 skyTexVal = skyTex_directCalc(uvCloud, currentTime, cld_detail_morph); 

    float cloudIntensity = pow(skyTexVal.x, 2.0);
    float starIntensity = pow(skyTexVal.y, 2.0);

    vec3 c = vec3(0.0);
    c = mix(c, vec3(0.2, 0.0, 0.5), horiz1);
    c = mix(c, vec3(1.0), horiz0);
    c = mix(c, vec3(0.45, 0.5, 0.48), (1.0-horiz0)*cloudIntensity);
    c = mix(c, vec3(1.0), (1.0-horiz1)*starIntensity);
    return c;
}

void computeLighting(
    in float diffuseCoefficient, in float specularCoefficient, in float specularExponent,
    in vec3 lightColor, in vec3 texColor, in vec3 nvNormal,
    in vec3 nvFragToLight, in vec3 nvFragToCam,
    out vec3 diffuse, out vec3 specular
) {
    float valDiffuse = max(0.0, dot(nvNormal, nvFragToLight)) * diffuseCoefficient;
    vec3 blinnH = normalize(nvFragToLight + nvFragToCam);
    float valSpecular = pow(max(0.0, dot(nvNormal, blinnH)), specularExponent) * specularCoefficient;
    diffuse = valDiffuse * texColor * lightColor;
    specular = valSpecular * lightColor;
}

// Ray marching functions (unchanged)
void hitObject(in vec3 startPos, in vec3 nvRayDir, in vec3 q00, in vec3 q10, in vec3 q11, in vec3 q01, out float didHit, out vec3 hitPos){ didHit = 0.0; float travel = 0.0; vec3 curPos = startPos; for (int k = 0; k < RAY_STEPS; k++) { float sdCur = sdQuadBoundary(q00,q10,q11,q01, BOUNDARY_RADIUS, curPos); if (sdCur < MIN_DIST) { didHit = 1.0; break; } curPos += sdCur * nvRayDir; travel += sdCur; if (travel > MAX_DIST) break; } hitPos = curPos; }
void hitShadow(in vec3 startPos, in vec3 nvRayDir, in vec3 q00, in vec3 q10, in vec3 q11, in vec3 q01, out float lightPercent){ lightPercent = 1.0; float travel = 0.0; vec3 curPos = startPos; for (int k = 0; k < RAY_STEPS_SHADOW; k++) { float sdCur = sdQuad(q00,q10,q11,q01, BOUNDARY_RADIUS, curPos); float curLightPercent = abs(sdCur)/(0.02*travel); lightPercent = min(lightPercent, curLightPercent); if (sdCur < MIN_DIST) { lightPercent = 0.0; break; } curPos += sdCur * nvRayDir; travel += sdCur; if (travel > MAX_DIST) break; } }

// Parallax mapping functions
void getParallaxMaxOffsets(
    in vec3 tangentS, in vec3 tangentT, in vec3 nvNormal, in vec3 camToFrag, in float depthMax,
    out vec2 maxTexOffset, out vec3 maxPosOffset
){
    float camDist = -dot(camToFrag, nvNormal);
    maxPosOffset = (depthMax / camDist) * camToFrag;
    float dss = dot(tangentS, tangentS); float dst = dot(tangentS, tangentT); float dtt = dot(tangentT, tangentT);
    float dcs = dot(maxPosOffset, tangentS); float dct = dot(maxPosOffset, tangentT);
    float invDet = 1.0 / (dss * dtt - dst * dst);
    maxTexOffset = invDet * vec2(dtt*dcs - dst*dct, -dst*dcs + dss*dct);
}

float getParallaxDepthFactor(vec2 uvInitial, vec2 maxTexOffset, int steps, float ter_detail_morph) {
    vec2 uvMax = uvInitial + maxTexOffset;
    float dt = 1.0 / float(steps);
    float tOld = 0.0, depthOld = 0.0;
    float tCur = 0.0, depthCur = 0.0;

    for(int i=0; i<=steps; ++i){
        tOld = tCur; tCur = float(i)*dt;
        depthOld = depthCur;
        depthCur = 1.0 - bumpTex_directCalc(mix(uvInitial, uvMax, tCur), ter_detail_morph).a; 
        if(tCur > depthCur){
            tCur = mix(tOld, tCur, unmix(depthOld-tOld, depthCur-tCur, 0.0));
            break;
        }
    }
    return tCur;
}

void terrainAndSky(
    in vec3 startPos, in vec3 nvRayDir, in vec3 lightPos, in int pomSteps,
    in vec3 q00, in vec3 q10, in vec3 q11, in vec3 q01,
    float generalAnimTime, float ter_detail_morph, float cld_detail_morph, 
    out vec3 hitColor
) {
    float tPlane = hitPlane(vec3(0.0, -PLANE_DEPTH, 0.0), NV_PLANE_N, startPos, nvRayDir);
    float didHitPlane = step(0.0, tPlane);

    if (didHitPlane > 0.5) {
        vec3 hitPos = startPos + tPlane*nvRayDir;
        vec2 hitTex = hitPos.xz / TEX_SCALE;

        vec2 maxTexOffset; vec3 maxPosOffset;
        getParallaxMaxOffsets( vec3(TEX_SCALE,0.0,0.0), vec3(0.0,0.0,TEX_SCALE), vec3(0.0,1.0,0.0),
                               hitPos - startPos, BUMP_TEX_DEPTH * TEX_SCALE, maxTexOffset, maxPosOffset );
        float depthPct = getParallaxDepthFactor(hitTex, maxTexOffset, pomSteps, ter_detail_morph); 
        vec2 hitTexBump = hitTex + depthPct*maxTexOffset;
        vec3 hitPosBump = hitPos + depthPct*maxPosOffset;

        vec3 bumpColor = bumpTex_directCalc(hitTexBump, ter_detail_morph).rgb; 
        vec3 nvNormal = bumpTexNormal(hitTexBump, ter_detail_morph); 
        vec3 nvBumpNormal = normalize(vec3(nvNormal.x, 1.0, nvNormal.y));

        if (distance(startPos, hitPosBump) < MAX_DIST) {
            vec3 nvBumpPosToLight = normalize(lightPos - hitPosBump);
            vec3 vBumpPosToStart = startPos - hitPosBump;
            float dHit = length(vBumpPosToStart);
            vec3 nvBumpPosToStart = vBumpPosToStart / dHit;

            float lightPercent = 1.0;
            hitShadow(hitPosBump, nvBumpPosToLight, q00,q10,q11,q01, lightPercent);
            lightPercent *= mix(1.0, 0.0, depthPct);

            vec3 diffuse, specular;
            computeLighting( 0.8,0.3,5.0, vec3(1.0),bumpColor, nvBumpNormal,nvBumpPosToLight,nvBumpPosToStart, diffuse,specular);
            vec3 matColor = 0.1 * bumpColor + lightPercent*(diffuse + specular);

            float fogT = mix(0.0, 0.8, unmix(0.0, MAX_DIST, dHit) );
            hitColor = mix(matColor, vec3(0.9, 0.8, 1.0), pow(fogT, 1.5));
        } else {
            hitColor = skyColor(nvRayDir, generalAnimTime, cld_detail_morph); 
        }
    } else {
        hitColor = skyColor(nvRayDir, generalAnimTime, cld_detail_morph); 
    }
}

// Escher effect helpers
vec2 cmul(vec2 a, vec2 b) { return vec2(a.x*b.x - a.y*b.y, a.x*b.y + a.y*b.x); }
vec2 clog(vec2 a) { return vec2(0.5*log(dot(a,a)), atan(a.y, a.x)); }
vec2 cexp(vec2 a) { return exp(a.x)*vec2(cos(a.y), sin(a.y)); }

vec2 twist(vec2 p, float portal_size, bool twist_enabled) {
    if (!twist_enabled) return p;
    vec2 r = vec2(TWIST_EXPONENT, log(portal_size)/TWOPI);
    return cexp(cmul(r, clog(p)));
}

// Droste effect: always on
vec2 droste(vec2 p, float portal_size, float general_anim_time) {
    float apow = fract(general_anim_time*0.2); 
    p *= pow(portal_size, apow);
    vec2 log_a = log(abs(p)) / log(portal_size);
    float adjust = min(floor(log_a.x), floor(log_a.y));
    log_a -= vec2(adjust);
    return sign(p) * pow(vec2(portal_size), log_a);
}

// Main scene rendering
vec4 scene(
    vec2 p, float generalAnimTime, float cameraOrbitTime, float currentSize, 
    float currentTerrainDetailMorph, float currentCloudDetailMorph 
) {
    // Camera: uses cameraOrbitTime for orbital motion, generalAnimTime for other animations
    vec3 camPos = 4.0 * vec3(cos(cameraOrbitTime * 0.2), 0.0, sin(cameraOrbitTime * 0.2)); // Orbit uses cameraOrbitTime
    camPos += vec3(0.0, 0.75 + 0.5*cos(generalAnimTime*0.5), 0.0); // Vertical bob uses generalAnimTime
    vec3 lookTarget = vec3(0.0);
    vec3 movement = vec3(2.0, 0.0, -generalAnimTime*2.0); // Forward movement uses generalAnimTime
    camPos += movement;
    lookTarget += movement;
    vec3 nvCamFw = normalize(lookTarget - camPos);
    mat4 clipToWorld = getClipToWorld(IMAGE_ASPECT_WIDTH_OVER_HEIGHT, nvCamFw);
    vec3 nvCamDir = nvDirFromClip(clipToWorld, p);

    // Portal geometry
    float a = currentSize;
    vec3 nv00=nvDirFromClip(clipToWorld,vec2(-a,-a)); vec3 nv10=nvDirFromClip(clipToWorld,vec2(a,-a));
    vec3 nv01=nvDirFromClip(clipToWorld,vec2(-a,a)); vec3 nv11=nvDirFromClip(clipToWorld,vec2(a,a));
    float minY = -PLANE_DEPTH+2.5*BOUNDARY_RADIUS;
    float tL=hitPlane(vec3(0.,minY,0.),NV_PLANE_N,camPos,nv00); float tU=hitPlane(vec3(0.,-minY,0.),-NV_PLANE_N,camPos,nv01);
    float tPortal=min(12.,min(mix(MAX_DIST,tL,step(0.,tL)),mix(MAX_DIST,tU,step(0.,tU))));
    vec3 q00=camPos+tPortal*nv00; vec3 q10=camPos+tPortal*nv10; vec3 q01=camPos+tPortal*nv01; vec3 q11=camPos+tPortal*nv11;
    vec3 portalVX=normalize(q10-q00); vec3 portalVY=normalize(q01-q00); vec3 portalVZ=cross(portalVX,portalVY);
    q00+=BOUNDARY_RADIUS*(-portalVX-portalVY); q10+=BOUNDARY_RADIUS*(portalVX-portalVY);
    q01+=BOUNDARY_RADIUS*(-portalVX+portalVY); q11+=BOUNDARY_RADIUS*(portalVX+portalVY);

    // Light placement
    vec3 lightPos = 0.5*(q01 + q11) + 1.0*portalVY + 5.0*portalVZ;

    // Render scene
    vec3 sceneColorVal = vec3(0.0); 
    float didHitPortal; vec3 hitPos;
    hitObject(camPos, nvCamDir, q00,q10,q11,q01, didHitPortal, hitPos);

    if (didHitPortal > 0.5) {
        vec3 n = normalQuadBoundary(q00,q10,q11,q01, hitPos);
        vec3 nvRefl = normalize(reflect( hitPos-camPos, n ));
        vec3 diffuse, specular;
        computeLighting(0.2,0.8,20.0, vec3(1.0),0.4*vec3(1.0,0.5,1.0), n,normalize(lightPos-hitPos),normalize(camPos-hitPos), diffuse,specular);
        vec3 matColor = diffuse + specular;
        vec3 terrainColor;
        terrainAndSky(hitPos,nvRefl,lightPos,POM_QUALITY_REFL, q00,q10,q11,q01, generalAnimTime, currentTerrainDetailMorph, currentCloudDetailMorph, terrainColor);
        sceneColorVal = matColor + 0.8*terrainColor;
    } else {
        terrainAndSky(camPos,nvCamDir,lightPos,POM_QUALITY, q00,q10,q11,q01, generalAnimTime, currentTerrainDetailMorph, currentCloudDetailMorph, sceneColorVal);
    }
    return vec4(clamp(sceneColorVal, 0.0, 1.0), 1.0);
}

void main() {
    // Pass 0: Speed parameter smoothing and time accumulation
    if(PASSINDEX == 0) {
        vec4 prevSpeedData = IMG_NORM_PIXEL(speedBuffer, vec2(0.5));
        // Smooth animSpeed and cameraOrbitSpeed
        float smoothedAnimSpeed = (FRAMEINDEX == 0) ? animSpeed : mix(prevSpeedData.g, animSpeed, min(1.0, TIMEDELTA * transitionSpeed));
        float smoothedCameraOrbitSpeed = (FRAMEINDEX == 0) ? cameraOrbitSpeed : mix(prevSpeedData.b, cameraOrbitSpeed, min(1.0, TIMEDELTA * transitionSpeed));
        
        // Accumulate general animation time (driven by animSpeed)
        float accumulatedGeneralAnimTime = (FRAMEINDEX == 0) ? 0.0 : prevSpeedData.r + smoothedAnimSpeed * TIMEDELTA;
        // Accumulate camera orbit time (driven by cameraOrbitSpeed)
        float accumulatedCameraOrbitTime = (FRAMEINDEX == 0) ? 0.0 : prevSpeedData.a + smoothedCameraOrbitSpeed * TIMEDELTA;
        
        gl_FragColor = vec4(accumulatedGeneralAnimTime, smoothedAnimSpeed, smoothedCameraOrbitSpeed, accumulatedCameraOrbitTime);
        return;
    }
    
    // Pass 1: Portal size smoothing
    if(PASSINDEX == 1) {
        vec4 prevData = IMG_NORM_PIXEL(sizeBuffer, vec2(0.5));
        float smoothedSize = (FRAMEINDEX == 0) ? portalSize : mix(prevData.r, portalSize, min(1.0, TIMEDELTA * transitionSpeed));
        gl_FragColor = vec4(smoothedSize, 0.0, 0.0, 1.0);
        return;
    }
    
    // Pass 2: Detail Morph parameters smoothing
    if(PASSINDEX == 2) {
        vec4 prevData = IMG_NORM_PIXEL(morphBuffer, vec2(0.5)); 
        float smoothedTerrainDetailMorph = (FRAMEINDEX == 0) ? terrainDetailMorph : mix(prevData.x, terrainDetailMorph, min(1.0, TIMEDELTA * transitionSpeed));
        float smoothedCloudDetailMorph = (FRAMEINDEX == 0) ? cloudDetailMorph : mix(prevData.y, cloudDetailMorph, min(1.0, TIMEDELTA * transitionSpeed));
        gl_FragColor = vec4(smoothedTerrainDetailMorph, smoothedCloudDetailMorph, 0.0, 1.0);
        return;
    }
        
    // Final Pass: Main rendering - PASSINDEX == 3
    if(PASSINDEX == 3) { 
        vec2 uv = isf_FragNormCoord.xy;
        
        vec4 speedData = IMG_NORM_PIXEL(speedBuffer, vec2(0.5));
        vec4 sizeData = IMG_NORM_PIXEL(sizeBuffer, vec2(0.5));
        vec4 morphData = IMG_NORM_PIXEL(morphBuffer, vec2(0.5)); 
        
        float currentGeneralAnimTime = speedData.r;
        // float currentSmoothedAnimSpeed = speedData.g; // Not directly used in scene, but available
        // float currentSmoothedOrbitSpeed = speedData.b; // Not directly used in scene, but available
        float currentCameraOrbitTime = speedData.a; // Use dedicated orbit time

        float currentSize = sizeData.r;
        float currentTerrainDetailMorph = morphData.x;
        float currentCloudDetailMorph = morphData.y;
        
        vec2 p = (2.0*uv - 1.0);
        p.x *= (RENDERSIZE.x / RENDERSIZE.y) / IMAGE_ASPECT_WIDTH_OVER_HEIGHT;

        vec2 radv = uv - vec2(0.5, 0.5);
        float dCorner = length(radv);
        float vignetteFactor = 1.0 - mix(0.0, 0.3, smoothstep(0.2, 0.707, dCorner));

        // Droste effect is always on, enableTwist is still a parameter. Pass general animation time to Droste.
        vec2 transformedP = droste(twist(p, currentSize, enableTwist), currentSize, currentGeneralAnimTime); 
        
        gl_FragColor = vignetteFactor * scene(transformedP, currentGeneralAnimTime, currentCameraOrbitTime, currentSize, currentTerrainDetailMorph, currentCloudDetailMorph);
        return;
    }
}
