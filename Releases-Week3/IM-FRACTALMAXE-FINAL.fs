/*{
    "DESCRIPTION": "ISF 2.0 Version 'GENERATORS REDUX' by Kali. Features smoothed speed, vibration intensity, and camera orientation (pitch/yaw/roll) transitions. Vibration frequency is independent of animation speed. ",
    "CREDIT": "Original Shadertoy by @Kali. ISF 2.0 Version by @dot2dot (bareimage).",
    "ISFVSN": "2.0",
    "CATEGORIES": ["GENERATOR", "FRACTAL", "3D"],
    "INPUTS": [
        {
            "NAME": "speed",
            "TYPE": "float",
            "DEFAULT": 0.25,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Animation Speed"
        },
        {
            "NAME": "transitionSpeed",
            "TYPE": "float",
            "DEFAULT": 2.0,
            "MIN": 0.1,
            "MAX": 10.0,
            "LABEL": "Transition Smoothness (All Controls)"
        },
        {
            "NAME": "vibrationIntensity",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": 0.0,
            "MAX": 10.0,
            "LABEL": "Vibration Intensity Target"
        },
        {
            "NAME": "pitch",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -180.0,
            "MAX": 180.0,
            "LABEL": "Camera Pitch Target"
        },
        {
            "NAME": "yaw",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -180.0,
            "MAX": 180.0,
            "LABEL": "Camera Yaw Target"
        },
        {
            "NAME": "roll",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -180.0,
            "MAX": 180.0,
            "LABEL": "Camera Roll Target"
        },
        {
            "NAME": "gamma_param",
            "TYPE": "float",
            "DEFAULT": 1.3,
            "MIN": 0.1,
            "MAX": 3.0,
            "LABEL": "Gamma"
        },
        {
            "NAME": "brightness_param",
            "TYPE": "float",
            "DEFAULT": 0.9,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Brightness"
        },
        {
            "NAME": "saturation_param",
            "TYPE": "float",
            "DEFAULT": 0.85,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Saturation"
        },
        {
            "NAME": "enableHardShadows",
            "TYPE": "bool",
            "DEFAULT": true,
            "LABEL": "Enable Hard Shadows"
        },
        {
            "NAME": "lightColorInput",
            "TYPE": "color",
            "DEFAULT": [0.85, 0.9, 1.0, 1.0],
            "LABEL": "Light Color"
        },
        {
            "NAME": "ambientColorInput",
            "TYPE": "color",
            "DEFAULT": [0.8, 0.83, 1.0, 1.0],
            "LABEL": "Ambient Color"
        },
        {
            "NAME": "floorColorInput",
            "TYPE": "color",
            "DEFAULT": [1.0, 0.7, 0.9, 1.0],
            "LABEL": "Floor Color"
        },
        {
            "NAME": "energyBaseColorInput",
            "TYPE": "color",
            "DEFAULT": [1.0, 0.7, 0.4, 1.0],
            "LABEL": "Energy Base Color"
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
            "TARGET": "vibrationBuffer",
            "PERSISTENT": true,
            "FLOAT": true,
            "WIDTH": 1,
            "HEIGHT": 1
        },
        {
            "TARGET": "orientationBuffer",
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

// Original ShaderToy constants
#define RAY_STEPS 70
#define SHADOW_STEPS 50
#define DETAIL_CONST .00005
#define PI 3.14159265359

// Scene constants
const vec3 LIGHT_DIR_CONST = normalize(vec3(0.5,-0.3,-1.));
const vec3 AMBIENT_DIR_CONST = normalize(vec3(0.,0.,1.));
const vec3 ORIGIN_CONST = vec3(0.,3.11,0.);


// Helper Functions
mat2 rot(float a) {
    return mat2(cos(a),sin(a),-sin(a),cos(a));
}

vec3 calculatePathPoint(float ti) {
    return vec3(sin(ti),.3-sin(ti*.632)*.3,cos(ti*.5))*.5;
}

struct PathInfo {
    vec3 position;
    mat2 rotationXZ; // For camera path's yaw
    mat2 rotationYZ; // For camera path's pitch
};

PathInfo calculateCameraPath(float currentTime_param) {
    vec3 go = calculatePathPoint(currentTime_param);
    vec3 adv = calculatePathPoint(currentTime_param + 0.7);
    vec3 advec = normalize(adv - go);
    float an_path_yaw = atan(advec.x, advec.z);
    mat2 rotPathYaw = mat2(cos(an_path_yaw), sin(an_path_yaw), -sin(an_path_yaw), cos(an_path_yaw));
    float an_path_pitch = advec.y * 1.7; 
    mat2 rotPathPitch = mat2(cos(an_path_pitch), sin(an_path_pitch), -sin(an_path_pitch), cos(an_path_pitch));
    return PathInfo(go, rotPathYaw, rotPathPitch);
}

// Sphere intersection
float intersectSphere(vec3 rayOrigin, vec3 rayDir, float radius, vec3 sphereCenter_param){
    vec3 p_intersect = rayOrigin - sphereCenter_param;
    float b = dot(-p_intersect, rayDir);
    float inner = b * b - dot(p_intersect, p_intersect) + radius * radius;
    if(inner < 0.0) return -1.0;
    return b - sqrt(inner);
}

// Distance Estimator
vec2 de(vec3 pos, float current_vibration_amplitude) {
    float hid = 0.;
    vec3 tpos = pos;
    tpos.xz = abs(.5 - mod(tpos.xz, 1.));
    vec4 p_de = vec4(tpos, 1.);
    float y_factor = max(0., .35 - abs(pos.y - 3.35)) / .35;

    for (int i = 0; i < 7; i++) {
        p_de.xyz = abs(p_de.xyz) - vec3(-0.02, 1.98, -0.02);
        p_de = p_de * (2.0 + current_vibration_amplitude * y_factor) / clamp(dot(p_de.xyz, p_de.xyz), .4, 1.) - vec4(0.5, 1., 0.4, 0.);
        p_de.xz *= mat2(-0.416, -0.91, 0.91, -0.416);
    }

    float fl = pos.y - 3.013;
    float fr = (length(max(abs(p_de.xyz) - vec3(0.1, 5.0, 0.1), vec3(0.0))) - 0.05) / p_de.w;
    float d = min(fl, fr);
    d = min(d, -pos.y + 3.95);
    if (abs(d - fl) < .001) hid = 1.;
    return vec2(d, hid);
}

// Normal Calculation
vec3 calculateNormal(vec3 p_norm, float raymarchDet_param, float current_vibration_amplitude) {
    vec3 e_norm = vec3(0.0, raymarchDet_param, 0.0);
    return normalize(vec3(
        de(p_norm + e_norm.yxx, current_vibration_amplitude).x - de(p_norm - e_norm.yxx, current_vibration_amplitude).x,
        de(p_norm + e_norm.xyx, current_vibration_amplitude).x - de(p_norm - e_norm.xyx, current_vibration_amplitude).x,
        de(p_norm + e_norm.xxy, current_vibration_amplitude).x - de(p_norm - e_norm.xxy, current_vibration_amplitude).x
    ));
}

// Shadow Calculation
float calculateShadow(vec3 pos_shadow, vec3 lightDir_param, float raymarchDet_param, vec3 pth1_param, float current_vibration_amplitude) {
    float sh = 1.0;
    float totalDist_shadow = 2.0 * raymarchDet_param;
    float dist_shadow = 10.;

    float t_sphere_shadow = intersectSphere(pos_shadow - 0.005 * lightDir_param, -lightDir_param, 0.015, pth1_param);
    if (t_sphere_shadow > 0. && t_sphere_shadow < 0.5) {
        vec3 hitPointOnSphereRay = pos_shadow - t_sphere_shadow * lightDir_param;
        vec3 sphereGlowNorm = normalize(hitPointOnSphereRay - pth1_param);
        sh = 1. - pow(max(.0, dot(sphereGlowNorm, lightDir_param)) * 1.2, 3.);
    }

    for (int steps = 0; steps < SHADOW_STEPS; steps++) {
        if (totalDist_shadow < 0.6 && dist_shadow > DETAIL_CONST) {
            vec3 p_s = pos_shadow - totalDist_shadow * lightDir_param;
            dist_shadow = de(p_s, current_vibration_amplitude).x;
            sh = min(sh, max(50. * dist_shadow / totalDist_shadow, 0.0));
            totalDist_shadow += max(.01, dist_shadow);
        }
    }
    return clamp(sh, 0.1, 1.0);
}

// Ambient Occlusion Calculation
float calculateAO(vec3 pos_ao, vec3 nor_ao, float current_vibration_amplitude) {
    float ao_detail_base = DETAIL_CONST * 40.;
    float totalAO = 0.0;
    float scale_ao = 14.0;
    for(int aoi = 0; aoi < 5; aoi++) {
        float hr_ao = ao_detail_base * float(aoi * aoi);
        vec3 aoPos_val = nor_ao * hr_ao + pos_ao;
        float dd_ao = de(aoPos_val, current_vibration_amplitude).x;
        totalAO += -(dd_ao - hr_ao) * scale_ao;
        scale_ao *= 0.7;
    }
    return clamp(1.0 - 5.0 * totalAO, 0., 1.0);
}

// Texture Pattern for Surfaces
float getTexturePattern(vec3 p_tex) {
    p_tex = abs(.5 - fract(p_tex * 10.));
    vec3 c_tex = vec3(3.);
    float es_tex = 0., l_tex = 0.;
    for (int i = 0; i < 10; i++) {
        p_tex = abs(p_tex + c_tex) - abs(p_tex - c_tex) - p_tex;
        p_tex /= clamp(dot(p_tex, p_tex), .0, 1.);
        p_tex = p_tex * -1.5 + c_tex;
        if (mod(float(i), 2.) < 1.) {
            float pl_tex = l_tex;
            l_tex = length(p_tex);
            es_tex += exp(-1. / abs(l_tex - pl_tex));
        }
    }
    return es_tex;
}

// Lighting Calculation
vec3 calculateLighting(vec3 p_light, vec3 viewDir, vec3 normal_param, float materialHid,
                               float raymarchDet_param, vec3 pth1_param, float current_vibration_amplitude,
                               vec3 currentEnergy_param, bool currentEnableHardShadows,
                               vec3 currentLightColor, vec3 currentAmbientColor, vec3 currentFloorColor) {
    float shadowFactor;
    if (currentEnableHardShadows) {
        shadowFactor = calculateShadow(p_light, LIGHT_DIR_CONST, raymarchDet_param, pth1_param, current_vibration_amplitude);
    } else {
        shadowFactor = calculateAO(p_light, -2.5 * LIGHT_DIR_CONST, current_vibration_amplitude);
    }

    float aoFactor = calculateAO(p_light, normal_param, current_vibration_amplitude);
    float diffuseFactor = max(0., dot(LIGHT_DIR_CONST, -normal_param)) * shadowFactor;
    float y_dist_from_energy = 3.35 - p_light.y;
    vec3 ambientLighting = max(.5, dot(viewDir, -normal_param)) * .5 * currentAmbientColor;

    if (materialHid < .5) { 
        float dist_y_norm = abs(3.0 - p_light.y) / 0.2; 
        float floor_y_influence_factor = pow(1.0 - smoothstep(0.0, 1.0, dist_y_norm), 1.5); 
        ambientLighting += max(0.2, dot(vec3(0., 1., 0.), -normal_param)) * currentFloorColor * floor_y_influence_factor * 2.0;
    }

    vec3 reflectedLightDir = reflect(LIGHT_DIR_CONST, normal_param);
    float specularFactor = pow(max(0., dot(viewDir, -reflectedLightDir)) * shadowFactor, 10.);
    vec3 surfaceColor;
    float energySourceGlow = pow(max(0., .04 - abs(y_dist_from_energy)) / .04, 4.) * 2.;

    if (materialHid > 1.5) { 
        surfaceColor = vec3(1.); 
        specularFactor = specularFactor * specularFactor; 
    } else { 
        float texVal = getTexturePattern(p_light) * .23 + .2;
        texVal = min(texVal, 1.5 - energySourceGlow); 
        surfaceColor = mix(vec3(texVal, texVal * texVal, texVal * texVal * texVal), vec3(texVal), .3);
        if (abs(materialHid - 1.0) < .001) { 
            surfaceColor *= currentFloorColor * 1.3;
        }
    }

    surfaceColor = surfaceColor * (ambientLighting + diffuseFactor * currentLightColor) + specularFactor * currentLightColor;
    if (materialHid < .5) { 
        surfaceColor = max(surfaceColor, currentEnergy_param * 2. * energySourceGlow);
    }
    surfaceColor *= min(1., aoFactor + length(currentEnergy_param) * .5 * max(0., .1 - abs(y_dist_from_energy)) / .1);
    return surfaceColor;
}

// Raymarching
struct RaymarchResult {
    vec3 color;
    float accumulatedGlow;
    float accumulatedEnergyGlow;
    float totalDistance;
    vec3 hitPoint;
    vec2 deResult;
    bool hit;
};

RaymarchResult raymarchScene(vec3 initialRayOrigin, vec3 initialRayDir,
                             float currentTime_param, vec3 pth1_world_param, float current_vibration_amplitude,
                             bool currentEnableHardShadows, vec3 currentLightColor, vec3 currentAmbientColor,
                             vec3 currentFloorColor, vec3 currentEnergyBaseColor) {
    float energyGlowCycle = mod(currentTime_param * .5, 1.);
    float accumulatedGlow = 0.;
    float accumulatedEnergyGlow = 0.;
    float reflectionFactor = 0.;
    float sphereHitDistance = 0.;
    vec3 currentRayOrigin = initialRayOrigin;
    vec3 currentRayDir = initialRayDir;
    vec3 sphereNormal = vec3(0.);

    vec3 wob_offset_val = cos(currentRayDir * 500.0 * length(currentRayOrigin - pth1_world_param) +
                              (currentRayOrigin - pth1_world_param) * 250. + currentTime_param * 100.) * 0.0005;
    vec3 sphere_effective_center = pth1_world_param - wob_offset_val;

    float sphereHit_t = intersectSphere(currentRayOrigin, currentRayDir, 0.015, sphere_effective_center);
    float sphereGlow_t = intersectSphere(currentRayOrigin, currentRayDir, 0.02, sphere_effective_center);

    if (sphereHit_t > 0.) {
        reflectionFactor = 1.0;
        vec3 hitP_sphere = currentRayOrigin + sphereHit_t * currentRayDir;
        currentRayOrigin = hitP_sphere;
        sphereHitDistance = sphereHit_t;
        sphereNormal = normalize(hitP_sphere - sphere_effective_center);
        currentRayDir = reflect(currentRayDir, sphereNormal);
    } else if (sphereGlow_t > 0.) {
        vec3 hitPointForGlow = initialRayOrigin + sphereGlow_t * initialRayDir;
        vec3 sphereGlowNorm = normalize(hitPointForGlow - sphere_effective_center);
        accumulatedGlow += pow(max(0., dot(sphereGlowNorm, -initialRayDir)), 5.);
    }

    float totalDistanceTraveled = 0.;
    vec2 de_res = vec2(1., 0.); 
    vec3 hitP_scene = currentRayOrigin;
    bool wasHit_scene = false;

    for (int i = 0; i < RAY_STEPS; i++) {
        float currentRaymarchDet = DETAIL_CONST * (1. + totalDistanceTraveled * 60.) * (1. + reflectionFactor * 5.);
        if (de_res.x > currentRaymarchDet && totalDistanceTraveled < 3.0) {
            hitP_scene = currentRayOrigin + totalDistanceTraveled * currentRayDir;
            de_res = de(hitP_scene, current_vibration_amplitude);
            totalDistanceTraveled += de_res.x;
            if (de_res.x < 0.015) {
                accumulatedGlow += max(0., .015 - de_res.x) * exp(-totalDistanceTraveled);
            }
            if (de_res.y < .5 && de_res.x < 0.03) { 
                float distToEnergyBand1 = abs(3.35 - hitP_scene.y - energyGlowCycle);
                float distToEnergyBand2 = abs(3.35 - hitP_scene.y + energyGlowCycle);
                float glow_y = min(distToEnergyBand1, distToEnergyBand2); 
                accumulatedEnergyGlow += max(0., .03 - de_res.x) / .03 *
                                         (pow(max(0., .05 - glow_y) / .05, 2.0f) +         
                                          pow(max(0., .15 - abs(3.35 - hitP_scene.y)) / .15, 3.0f)) * 1.5; 
            }
        } else {
            break;
        }
    }

    vec3 finalColor_val;
    float lightAngleFactor = pow(max(0., dot(normalize(-currentRayDir.xz), normalize(LIGHT_DIR_CONST.xz))), 2.);
    lightAngleFactor *= max(0.2, dot(-currentRayDir, LIGHT_DIR_CONST));
    vec3 backgroundColor = .5 * (1.2 - lightAngleFactor) + currentLightColor * lightAngleFactor * .7;
    backgroundColor *= currentAmbientColor;

    if (de_res.x <= DETAIL_CONST * (1. + totalDistanceTraveled * 60.)*(1. + reflectionFactor * 5.) ) {
        wasHit_scene = true;
        vec3 preciseHitPoint = hitP_scene - abs(de_res.x - DETAIL_CONST * (1. + totalDistanceTraveled * 60.)*(1. + reflectionFactor * 5.)) * currentRayDir;
        float currentRaymarchDet = DETAIL_CONST * (1. + totalDistanceTraveled * 60.) * (1. + reflectionFactor * 5.);
        vec3 surfaceNormal = calculateNormal(preciseHitPoint, currentRaymarchDet, current_vibration_amplitude);
        vec3 energyAtHitPoint = currentEnergyBaseColor * (1.5 + sin(currentTime_param * 20. + preciseHitPoint.z * 10.)) * .25; 
        finalColor_val = calculateLighting(preciseHitPoint, currentRayDir, surfaceNormal, de_res.y, 
                                         currentRaymarchDet, pth1_world_param, current_vibration_amplitude,
                                         energyAtHitPoint, currentEnableHardShadows, currentLightColor, currentAmbientColor, currentFloorColor);
        finalColor_val *= exp(-.2 * totalDistanceTraveled * totalDistanceTraveled);
        finalColor_val = mix(finalColor_val, backgroundColor, 1.0 - exp(-1. * pow(totalDistanceTraveled, 1.5)));
    } else {
        wasHit_scene = false;
        finalColor_val = backgroundColor;
    }

    vec3 lightShaftGlow = currentLightColor * pow(lightAngleFactor, 30.) * .5;
    finalColor_val += accumulatedGlow * (backgroundColor + lightShaftGlow) * 1.3;
    vec3 genericEnergyColorForGlow = currentEnergyBaseColor * (1.5 + sin(currentTime_param * 20.)) * .25;
    finalColor_val += pow(accumulatedEnergyGlow, 2.) * genericEnergyColorForGlow * .015;
    finalColor_val += lightShaftGlow * min(1., totalDistanceTraveled * totalDistanceTraveled * .3);

    if (reflectionFactor > 0.5) { 
        vec3 sphereHitSurfacePoint = initialRayOrigin + sphereHitDistance * initialRayDir;
        vec3 energyAtSphereSurface = currentEnergyBaseColor * (1.5 + sin(currentTime_param * 20. + sphereHitSurfacePoint.z * 10.)) * .25; 
        float sphereRaymarchDet = DETAIL_CONST * (1. + sphereHitDistance * 60.);
        vec3 sphereLighting = calculateLighting(sphereHitSurfacePoint, initialRayDir, sphereNormal, 2.0f, 
                                                sphereRaymarchDet, sphere_effective_center, current_vibration_amplitude,
                                                energyAtSphereSurface, currentEnableHardShadows, currentLightColor, currentAmbientColor, currentFloorColor);
        finalColor_val = mix(finalColor_val * .3 + sphereLighting * .7, backgroundColor, 1.0 - exp(-1. * pow(sphereHitDistance, 1.5)));
    }
    return RaymarchResult(finalColor_val, accumulatedGlow, accumulatedEnergyGlow, totalDistanceTraveled, hitP_scene, de_res, wasHit_scene);
}

// Main ISF function
// Variables for managing smoothed values, shared across passes
vec4 prevTimeData_isf;
float accumulatedTime_isf;      // .r of timeBuffer
float currentActualSpeed_isf;   // .g of timeBuffer

vec4 prevVibrationData_isf;
float smoothedVibrationIntensity_isf; // .r of vibrationBuffer

vec4 prevOrientationData_isf;
float smoothedPitch_isf;        // .r of orientationBuffer
float smoothedYaw_isf;          // .g of orientationBuffer
float smoothedRoll_isf;         // .b of orientationBuffer


void main() {
    if (PASSINDEX == 0) {
        // --- Pass 0: Update Time Buffer (for animation speed) ---
        prevTimeData_isf = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        float prevAccumulatedTime = prevTimeData_isf.r;
        float prevActualSpeed = prevTimeData_isf.g;

        float targetSpeed = speed; // Input: Animation Speed
        float newActualSpeed;
        float newAccumulatedTime;

        if (FRAMEINDEX == 0) {
            newActualSpeed = targetSpeed;
            newAccumulatedTime = 0.0;
        } else {
            newActualSpeed = mix(prevActualSpeed, targetSpeed, min(1.0, TIMEDELTA * transitionSpeed));
            newAccumulatedTime = prevAccumulatedTime + newActualSpeed * TIMEDELTA;
        }
        gl_FragColor = vec4(newAccumulatedTime, newActualSpeed, 0.0, 1.0);

    } else if (PASSINDEX == 1) {
        // --- Pass 1: Update Vibration Buffer (for vibration intensity) ---
        prevVibrationData_isf = IMG_NORM_PIXEL(vibrationBuffer, vec2(0.5, 0.5));
        float prevSmoothedVibIntensity = prevVibrationData_isf.r;
        
        float targetVibrationIntensity = vibrationIntensity; // Input: Vibration Intensity Target
        float newSmoothedVibIntensity;

        if (FRAMEINDEX == 0) {
            newSmoothedVibIntensity = targetVibrationIntensity;
        } else {
            newSmoothedVibIntensity = mix(prevSmoothedVibIntensity, targetVibrationIntensity, min(1.0, TIMEDELTA * transitionSpeed));
        }
        gl_FragColor = vec4(newSmoothedVibIntensity, 0.0, 0.0, 1.0);

    } else if (PASSINDEX == 2) {
        // --- Pass 2: Update Orientation Buffer (for pitch, yaw, roll) ---
        prevOrientationData_isf = IMG_NORM_PIXEL(orientationBuffer, vec2(0.5, 0.5));
        float prevSmoothedPitch = prevOrientationData_isf.r;
        float prevSmoothedYaw = prevOrientationData_isf.g;
        float prevSmoothedRoll = prevOrientationData_isf.b;

        float targetPitch = pitch; // Input: Camera Pitch Target
        float targetYaw = yaw;     // Input: Camera Yaw Target
        float targetRoll = roll;   // Input: Camera Roll Target

        float newSmoothedPitch, newSmoothedYaw, newSmoothedRoll;

        if (FRAMEINDEX == 0) {
            newSmoothedPitch = targetPitch;
            newSmoothedYaw = targetYaw;
            newSmoothedRoll = targetRoll;
        } else {
            float smoothingFactor = min(1.0, TIMEDELTA * transitionSpeed);
            newSmoothedPitch = mix(prevSmoothedPitch, targetPitch, smoothingFactor);
            newSmoothedYaw = mix(prevSmoothedYaw, targetYaw, smoothingFactor);
            newSmoothedRoll = mix(prevSmoothedRoll, targetRoll, smoothingFactor);
        }
        gl_FragColor = vec4(newSmoothedPitch, newSmoothedYaw, newSmoothedRoll, 1.0);

    } else if (PASSINDEX == 3) {
        // --- Pass 3: Main Rendering Logic ---
        
        // Read smoothed values from buffers
        vec4 timeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5, 0.5));
        float t_current_render = timeData.r; // Smoothed accumulated time for animation

        vec4 vibData = IMG_NORM_PIXEL(vibrationBuffer, vec2(0.5, 0.5));
        float currentSmoothedVibIntensity = vibData.r; // Smoothed vibration intensity

        vec4 orientData = IMG_NORM_PIXEL(orientationBuffer, vec2(0.5, 0.5));
        float currentSmoothedPitch_deg = orientData.r; // Smoothed pitch in degrees
        float currentSmoothedYaw_deg = orientData.g;   // Smoothed yaw in degrees
        float currentSmoothedRoll_deg = orientData.b;  // Smoothed roll in degrees

        // Calculate actual vibration effect using TIME for frequency and smoothed intensity for amplitude
        float current_vibration_amplitude = 0.0;
        if (currentSmoothedVibIntensity > 0.0001) {
            // Vibration frequency uses TIME directly, decoupled from animation speed
            // 0.0013 is the base amplitude, scaled by smoothed intensity
            current_vibration_amplitude = sin(TIME * 60.0) * 0.0013 * currentSmoothedVibIntensity;
        }

        // Automated camera path calculation (uses t_current_render for its own animation)
        PathInfo camPath = calculateCameraPath(t_current_render);
        vec3 rayOrigin = ORIGIN_CONST + camPath.position;
        vec3 pth1_current_world = calculatePathPoint(t_current_render + .3) + ORIGIN_CONST + vec3(0., .01, 0.);

        // Screen setup
        vec2 R_render = RENDERSIZE;
        vec2 uv_isf = isf_FragNormCoord * 2.0 - 1.0;
        uv_isf.y *= R_render.y / R_render.x; // Aspect ratio correction

        // Convert smoothed orientation degrees to radians for rotation matrices
        float pitch_rad = currentSmoothedPitch_deg * PI / 180.0;
        float yaw_rad = currentSmoothedYaw_deg * PI / 180.0;
        float roll_rad = currentSmoothedRoll_deg * PI / 180.0;

        // Initial ray direction (FOV encoded in .8)
        vec3 rayDir = normalize(vec3(uv_isf * .8, 1.)); 

        // Apply user-controlled smoothed Pitch (around X-axis)
        rayDir.yz *= rot(pitch_rad);
        // Apply user-controlled smoothed Yaw (around Y-axis)
        rayDir.xz *= rot(yaw_rad);
        
        // Apply automated camera path rotations (these are additional to user controls)
        rayDir.yz *= camPath.rotationYZ; // Path's pitch component
        rayDir.xz *= camPath.rotationXZ; // Path's yaw component

        // Apply user-controlled smoothed Roll (around Z-axis) - applied last
        rayDir.xy *= rot(roll_rad);

        // Get current colors from inputs
        vec3 currentLightColor = lightColorInput.rgb;
        vec3 currentAmbientColor = ambientColorInput.rgb;
        vec3 currentFloorColor = floorColorInput.rgb;
        vec3 currentEnergyBaseColor = energyBaseColorInput.rgb;

        // Perform raymarching with all current values
        RaymarchResult result = raymarchScene(rayOrigin, rayDir, t_current_render, pth1_current_world, current_vibration_amplitude,
                                              enableHardShadows, currentLightColor, currentAmbientColor, currentFloorColor, currentEnergyBaseColor);
        vec3 color = result.color;

        // Final color adjustments
        color = clamp(color, vec3(.0), vec3(1.));
        color = pow(color, vec3(gamma_param)) * brightness_param;
        color = mix(vec3(length(color)), color, saturation_param);
        
        gl_FragColor = vec4(color, 1.);
    }
}
