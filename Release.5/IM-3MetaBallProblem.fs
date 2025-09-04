/*{
    "DESCRIPTION": "3D metaball shader. Funny story about this shader. I always misread metaballs and read the name as meatballs. Imagine my surprise when some one corrected me, it was light a lightbulb flashed in my brain, for 15+ years I had wrong idea of what a metaball realy was. Ok about this shader, it features a dynamic 'twists and turns' background, 25 animated color palettes (this is prety neat, allows you to have multiple looks of the same shader), and smooth, time-independent parameter transitions.",
    "CREDIT": "Original by @dot2dot (bareimage), ISF 2.0 Conversion by @dot2dot (bareimage)",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "3D",
        "Raymarching",
        "GENERATOR"
    ],
    "INPUTS": [
        {
            "NAME": "ColorTemplates",
            "TYPE": "long",
            "LABEL": "Color Template",
            "VALUES": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24],
            "LABELS": [
                "Psychedelic", "Ember", "Abyss", "Monochrome", "Forest",
                "Oceanic", "Sunset", "Nebula", "Electric", "Pastel Dream",
                "Volcanic", "Glacial", "Desert Mirage", "Jungle Haze", "Cyberpunk",
                "Autumn", "Coral Reef", "Synthwave", "Starlight", "Iridescent",
                "Earth Tones", "Candy Pop", "Deep Space", "Golden Hour", "Emerald City"
            ],
            "DEFAULT": 0
        },
        {
            "NAME": "speed",
            "TYPE": "float",
            "LABEL": "Animation Speed",
            "MIN": -5.0,
            "MAX": 5.0,
            "DEFAULT": 1.0
        },
        {
            "NAME": "LineDensity",
            "TYPE": "float",
            "LABEL": "BG Pattern Density",
            "MIN": 5.0,
            "MAX": 100.0,
            "DEFAULT": 20.0
        },
        {
            "NAME": "LineWidth",
            "TYPE": "float",
            "LABEL": "BG Pattern Power",
            "MIN": 0.1,
            "MAX": 20.0,
            "DEFAULT": 10.0
        },
        {
            "NAME": "cameraPan",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -180.0,
            "MAX": 180.0,
            "LABEL": "Camera Pan"
        },
        {
            "NAME": "cameraTilt",
            "TYPE": "float",
            "DEFAULT": 15.0,
            "MIN": -89.0,
            "MAX": 89.0,
            "LABEL": "Camera Tilt"
        },
        {
            "NAME": "cameraHeight",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -10.0,
            "MAX": 10.0,
            "LABEL": "Camera Height"
        },
        {
            "NAME": "cameraDistance",
            "TYPE": "float",
            "DEFAULT": 3.0,
            "MIN": 1.5,
            "MAX": 10.0,
            "LABEL": "Camera Distance"
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
        { "TARGET": "timeBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
        { "TARGET": "paramBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
        { "TARGET": "cameraBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 },
        { "TARGET": "finalOutput" }
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
precision highp float;
const float PI = 3.14159265359;
const float EPS = 1e-4;
const int ITR = 64;
const vec3 sunDir = normalize(vec3(0.0, 1.0, 5.0));

// --- Forward Declarations ---
float map(vec3 p);
vec3 render_background(vec3 ro, vec3 rd);
vec3 shade(vec3 pos, vec3 normal, vec3 rayDir);
vec3 renderRay(vec3 rayPos, vec3 rayDir);
vec3 effect(vec2 p, vec4 cam_params, float time, float line_density, float line_width, int color_template_index);

// --- Globals for Final Pass ---
float g_time;
float g_lineDensity;
float g_lineWidth;
int   g_colorTemplate;

// --- Utility Functions ---
float noise3D(vec3 p) {
    vec3 i = floor(p); vec3 f = fract(p); vec3 u = f * f * (3.0 - 2.0 * f);
    float a = fract(sin(dot(i + vec3(0,0,0), vec3(12.9898, 78.233, 37.719))) * 43758.5453);
    float b = fract(sin(dot(i + vec3(1,0,0), vec3(12.9898, 78.233, 37.719))) * 43758.5453);
    float c = fract(sin(dot(i + vec3(0,1,0), vec3(12.9898, 78.233, 37.719))) * 43758.5453);
    float d = fract(sin(dot(i + vec3(1,1,0), vec3(12.9898, 78.233, 37.719))) * 43758.5453);
    float e = fract(sin(dot(i + vec3(0,0,1), vec3(12.9898, 78.233, 37.719))) * 43758.5453);
    float f_ = fract(sin(dot(i + vec3(1,0,1), vec3(12.9898, 78.233, 37.719))) * 43758.5453);
    float g = fract(sin(dot(i + vec3(0,1,1), vec3(12.9898, 78.233, 37.719))) * 43758.5453);
    float h = fract(sin(dot(i + vec3(1,1,1), vec3(12.9898, 78.233, 37.719))) * 43758.5453);
    return mix(mix(mix(a,b,u.x),mix(c,d,u.x),u.y),mix(mix(e,f_,u.x),mix(g,h,u.x),u.y),u.z);
}

vec3 aces_approx(vec3 v) {
    v = max(v, 0.0); v *= 0.6;
    float a = 2.51, b = 0.03, c = 2.43, d = 0.59, e = 0.14;
    return clamp((v*(a*v+b))/(v*(c*v+d)+e), 0.0, 1.0);
}

float smin(float a, float b, float k) {
    float h = max(k - abs(a - b), 0.0) / k;
    return min(a, b) - h * h * h * k * (1.0/6.0);
}

float sdSphere(vec3 p, float s) { return length(p) - s; }
mat3 rotY(float a) { float s=sin(a), c=cos(a); return mat3(c,0,s,0,1,0,-s,0,c); }
mat3 rotX(float a) { float s=sin(a), c=cos(a); return mat3(1,0,0,0,c,-s,0,s,c); }
mat3 lookAt(vec3 f, vec3 u) { f=normalize(f); vec3 r=normalize(cross(f,normalize(u))); return mat3(r,cross(r,f),f); }

// --- Color Palette Function ---
vec4 getPaletteColor(float z, vec3 p, int ColorTemplatesIndex) {
    vec4 color;
    float t = g_time * 0.2;
    if (ColorTemplatesIndex == 0) color = cos(z + sin(p.z) + vec4(6,1,2,0) + t) + 1.2;
    else if (ColorTemplatesIndex == 1) color = cos(z*0.5+cos(p.z)*sin(t)+vec4(1.5,0.8,0.5,0)) + 1.0;
    else if (ColorTemplatesIndex == 2) color = cos(z + sin(p.y) + vec4(0.2,0.5,1.5,0) - t) + 1.2;
    else if (ColorTemplatesIndex == 3) { float grey=cos(z+sin(p.z))+1.2; color=vec4(grey,grey,grey,0); }
    else if (ColorTemplatesIndex == 4) color = cos(z*0.8+p.x*2.0+vec4(0.2,1.0,0.3,0) + t) + 1.1;
    else if (ColorTemplatesIndex == 5) color = cos(z*0.7+p.y*2.5+vec4(0.1,0.4,2.0,0) - t) + 1.2;
    else if (ColorTemplatesIndex == 6) color = cos(z + sin(p.z*0.5)+vec4(1.8,0.7,0.4,0) + t) + 1.0;
    else if (ColorTemplatesIndex == 7) color = cos(z*0.3+sin(length(p.xy))+vec4(1,0.3,1.2,0)-t) + 1.3;
    else if (ColorTemplatesIndex == 8) color = 0.5+0.5*sin(z*2.0+p.y*3.0+vec4(1.5,1.5,0,0)+t);
    else if (ColorTemplatesIndex == 9) color = 0.8+0.4*cos(z+p.z+vec4(0.9,0.7,0.8,0)-t);
    else if (ColorTemplatesIndex == 10) color = vec4(1.5,0.5,0.1,0)*(1.0+0.5*sin(z*1.5+length(p.yz)+t));
    else if (ColorTemplatesIndex == 11) color = 0.9+0.3*cos(z*1.2+p.x+vec4(0.8,0.9,1,0)+t);
    else if (ColorTemplatesIndex == 12) color = cos(z*0.4+sin(p.y*1.5)+vec4(1.2,0.9,0.5,0)-t)+1.1;
    else if (ColorTemplatesIndex == 13) color = cos(z+sin(p.x*2.0)+vec4(0.4,0.8,0.2,0)+t)+1.2;
    else if (ColorTemplatesIndex == 14) color = 0.5+0.5*sin(z*3.0+vec4(1,0.1,1,0)+t);
    else if (ColorTemplatesIndex == 15) color = cos(z*0.6+p.z+vec4(1,0.5,0.2,0)-t)+1.0;
    else if (ColorTemplatesIndex == 16) color = cos(z*1.5+sin(p.y*2.0)+vec4(1.2,0.6,0.8,0)+t)+1.2;
    else if (ColorTemplatesIndex == 17) color = 0.5+0.5*cos(z+vec4(2,0,1,0)+t*2.0);
    else if (ColorTemplatesIndex == 18) { float grey=0.8+0.2*sin(z*5.0+p.z*10.); color=vec4(grey,grey,grey*1.2,0); }
    else if (ColorTemplatesIndex == 19) color = 0.5+0.5*sin(z*0.5+length(p)*0.5+vec4(0,1,2,0)+t);
    else if (ColorTemplatesIndex == 20) color = cos(z*0.2+p.x+vec4(0.6,0.4,0.2,0)-t)+1.0;
    else if (ColorTemplatesIndex == 21) color = 0.7+0.3*sin(z*2.0+vec4(1.2,0.8,1.0,0)+t);
    else if (ColorTemplatesIndex == 22) color = vec4(0.1,0.1,0.3,0)+0.4*cos(z*0.3+vec4(1,1.5,2,0)-t);
    else if (ColorTemplatesIndex == 23) color = cos(z+sin(p.z)+vec4(1.5,1.0,0.2,0)+t)+1.0;
    else color = cos(z*0.9+p.y*1.5+vec4(0.1,1.0,0.4,0)-t)+1.1;
    return color;
}

// --- Scene Definition ---
float map(vec3 p) {
    float time1 = g_time * 0.8; float time2 = g_time * 1.2; float time3 = g_time * 0.6;
    float baseRadius=0.5, radiusVariation=0.15, k=0.6;
    vec3 pos1 = vec3(cos(time1)*0.8, sin(time1)*0.8, sin(time1*1.5)*0.5);
    float radius1 = baseRadius + sin(time1*2.0)*radiusVariation;
    vec3 pos2 = vec3(cos(time2+PI*2./3.)*0.7, cos(time2*0.7)*0.6, sin(time2+PI*2./3.)*0.9);
    float radius2 = baseRadius + cos(time2*1.8)*radiusVariation;
    vec3 pos3 = vec3(sin(time3+PI*4./3.)*0.6, cos(time3*1.3+PI*4./3.)*0.5, cos(time3*0.9)*0.7);
    float radius3 = baseRadius + sin(time3*2.5)*radiusVariation;
    mat3 globalRot = rotY(g_time*0.2)*rotX(sin(g_time*0.15)*0.4);
    pos1 = globalRot * pos1; pos2 = globalRot * pos2; pos3 = globalRot * pos3;
    float sphere1 = sdSphere(p - pos1, radius1);
    float sphere2 = sdSphere(p - pos2, radius2);
    float sphere3 = sdSphere(p - pos3, radius3);
    float d = smin(sphere1, sphere2, k);
    return smin(d, sphere3, k * 0.95);
}

vec3 generateNormal(vec3 p) {
    return normalize(vec3(
        map(p+vec3(EPS,0,0))-map(p-vec3(EPS,0,0)),
        map(p+vec3(0,EPS,0))-map(p-vec3(0,EPS,0)),
        map(p+vec3(0,0,EPS))-map(p-vec3(0,0,EPS))
    ));
}

// --- Background Rendering ---
vec3 render_background(vec3 ro, vec3 rd) {
    vec3 p = ro + rd * 25.0; // Point on a virtual sky sphere
    vec3 q = p * 0.2 + vec3(0.0, 0.0, g_time * 0.1);

    // Create twisting patterns with multiple layers of noise
    float n1 = noise3D(q);
    vec3  q2 = q + n1 * 0.5;
    float n2 = noise3D(q2);

    float pattern = sin((p.x+p.y)*0.01*g_lineDensity + n1*5.0) +
                    cos((p.y-p.z)*0.01*g_lineDensity + n2*5.0 + g_time*0.2);

    float line_intensity = pow(abs(sin(pattern * 4.0)), g_lineWidth);

    vec4 palette = getPaletteColor(length(p.xy)*0.2 + n2, p, g_colorTemplate);
    vec3 col = palette.rgb * line_intensity * 1.2;
    col += palette.rgb * 0.05; // Ambient

    vec3 sunCol = vec3(1.0, 0.8, 0.6) * 0.5;
    col += sunCol * pow(max(dot(rd, sunDir), 0.0), 4.0);
    col += sunCol * 0.5 * pow(max(dot(rd, sunDir), 0.0), 32.0);
    return aces_approx(col);
}

// --- Shading and Raymarching ---
vec3 shade(vec3 pos, vec3 normal, vec3 rayDir) {
    vec3 reflectionNormal = normalize(normal + (noise3D(pos*0.25)-0.5)*2.0);
    vec3 reflectDir = reflect(rayDir, reflectionNormal);
    vec3 refractDir = refract(rayDir, normal, 1.0/1.5);
    float fresnel = 0.7 + 0.3 * pow(1.0+dot(rayDir, normal), 5.0);
    vec3 reflectionColor = render_background(pos, reflectDir);
    vec3 refractionColor = vec3(0.0);
    if (dot(refractDir, refractDir) > 0.0) {
        vec3 beerCol = -vec3(0.15, 0.45, 0.9) * 2.0;
        float absorptionFactor = pow(1.0-abs(dot(rayDir,normal)), 2.0)*2.0;
        refractionColor = vec3(0.4, 0.2, 0.05) * exp(beerCol * absorptionFactor);
    }
    vec3 finalColor = mix(refractionColor, reflectionColor, fresnel);
    vec3 viewDir = -rayDir;
    vec3 halfwayDir = normalize(sunDir + viewDir);
    float spec = pow(max(dot(normal, halfwayDir), 0.0), 64.0);
    finalColor += vec3(1.0,0.8,0.6)*0.5 * spec * 2.5;
    return finalColor;
}

vec3 renderRay(vec3 rayPos, vec3 rayDir) {
    float t = 0.0;
    for (int i=0; i<ITR; ++i) {
        float d = map(rayPos + rayDir * t);
        if (d < EPS) {
            vec3 hitPos = rayPos + rayDir*t;
            return shade(hitPos, generateNormal(hitPos), rayDir);
        }
        t += d * 0.9;
        if (t > 20.0) break;
    }
    return render_background(rayPos, rayDir);
}

// --- Main Effect Function (Final Pass) ---
vec3 effect(vec2 p, vec4 cam_params, float time, float line_density, float line_width, int color_template_index) {
    // Set globals for this render frame
    g_time = time;
    g_lineDensity = line_density;
    g_lineWidth = line_width;
    g_colorTemplate = color_template_index;

    // --- CORRECTED Camera Setup ---
    float cam_pan_angle = cam_params.x;
    float cam_tilt_angle = cam_params.y;
    float cam_height = cam_params.z;
    float cam_dist = cam_params.w;
    
    // The look-at target's height is now controlled by cameraHeight.
    vec3 target = vec3(0.0, cam_height, 0.0);

    float pan_rad = cam_pan_angle * PI / 180.0;
    float tilt_rad = cam_tilt_angle * PI / 180.0;
    
    // Calculate the camera's offset direction from the target.
    vec3 ro_offset;
    ro_offset.x = cos(tilt_rad) * sin(pan_rad);
    ro_offset.y = sin(tilt_rad);
    ro_offset.z = cos(tilt_rad) * cos(pan_rad);

    // Position the camera around the new height-adjusted target.
    vec3 ro = target + ro_offset * cam_dist;

    vec3 forward = normalize(target - ro);
    mat3 camMatrix = lookAt(forward, vec3(0.0, 1.0, 0.0));
    vec3 rd = normalize(camMatrix * vec3(p, 2.0));

    // --- Render and Post-Process ---
    vec3 finalColor = renderRay(ro, rd);
    finalColor = finalColor / (1.0 + finalColor * 0.8);
    finalColor = pow(finalColor, vec3(1.0/2.2));
    return finalColor;
}

// --- Main Function (Multi-Pass Router) ---
void main() {
    float smoothingFactor = min(1.0, TIMEDELTA * transitionSpeed);

    // --- Pass 0: Time Buffer ---
    if (PASSINDEX == 0) {
        vec4 prevData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        float newTime, adjustedSpeed;
        if (FRAMEINDEX == 0) {
            newTime = 0.0;
            adjustedSpeed = speed;
        } else {
            float accumulatedTime = prevData.r;
            float currentSpeed = prevData.g;
            adjustedSpeed = mix(currentSpeed, speed, smoothingFactor);
            newTime = accumulatedTime + adjustedSpeed * TIMEDELTA;
        }
        gl_FragColor = vec4(newTime, adjustedSpeed, 0.0, 1.0);
    }
    // --- Pass 1: General Parameters Buffer ---
    else if (PASSINDEX == 1) {
        vec4 prevParamData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5));
        vec4 currentParams;
        if (FRAMEINDEX == 0) {
            currentParams = vec4(LineDensity, LineWidth, 0.0, 0.0);
        } else {
            vec4 targetParams = vec4(LineDensity, LineWidth, 0.0, 0.0);
            currentParams = mix(prevParamData, targetParams, smoothingFactor);
        }
        gl_FragColor = currentParams;
    }
    // --- Pass 2: Camera Parameters Buffer ---
    else if (PASSINDEX == 2) {
        vec4 prevCamData = IMG_NORM_PIXEL(cameraBuffer, vec2(0.5));
        vec4 currentCamData;
        if (FRAMEINDEX == 0) {
            currentCamData = vec4(cameraPan, cameraTilt, cameraHeight, cameraDistance);
        } else {
            vec4 targetCamData = vec4(cameraPan, cameraTilt, cameraHeight, cameraDistance);
            currentCamData = mix(prevCamData, targetCamData, smoothingFactor);
        }
        gl_FragColor = currentCamData;
    }
    // --- Pass 3: Final Render ---
    else {
        vec4 timeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        vec4 paramData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5));
        vec4 cameraData = IMG_NORM_PIXEL(cameraBuffer, vec2(0.5));

        vec2 p = -1.0 + 2.0 * isf_FragNormCoord;
        p.x *= RENDERSIZE.x / RENDERSIZE.y;

        vec3 col = effect(p, cameraData, timeData.r, paramData.r, paramData.g, int(ColorTemplates));

        gl_FragColor = vec4(col, 1.0);
    }
}