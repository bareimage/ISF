/*{
    "DESCRIPTION": "A raymarched icosahedron containing an animated voxel cube, with a Voronoi-patterned floor. Features smooth, time-independent animation speed and parameter transitions. Controllable position and rotation. For helping with  array fillup, use this webapp https://github.com/bareimage/ISF/blob/main/Misc/VoxelArrayBuilder.html",
    "CREDIT": "Original by @dot2dot (bareimage). ISF 2.0 Conversion by @dot2dot (bareimage)",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "GENERATOR"
    ],
    "INPUTS": [
        {
            "NAME": "speed",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 50.0,
            "LABEL": "Animation Speed"
        },
        {
            "NAME": "polyhedronSize",
            "TYPE": "float",
            "DEFAULT": 3.5,
            "MIN": 1.0,
            "MAX": 10.0,
            "LABEL": "Icosahedron Size"
        },
        {
            "NAME": "objectY",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -5.0,
            "MAX": 10.0,
            "LABEL": "Object Y Position"
        },
        {
            "NAME": "voxelWorldSize",
            "TYPE": "float",
            "DEFAULT": 2.5,
            "MIN": 0.5,
            "MAX": 5.0,
            "LABEL": "Voxel Cube Size"
        },
        {
            "NAME": "colorModifier",
            "TYPE": "float",
            "DEFAULT": 0.2,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Voxel Color Pulsation"
        },
        {
            "NAME": "glowIntensity",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 5.0,
            "LABEL": "Outer Glow Intensity"
        },
        {
            "NAME": "rotX",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -3.14159,
            "MAX": 3.14159,
            "LABEL": "Icosahedron Rotation X"
        },
        {
            "NAME": "rotY",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -3.14159,
            "MAX": 3.14159,
            "LABEL": "Icosahedron Rotation Y"
        },
        {
            "NAME": "rotZ",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -3.14159,
            "MAX": 3.14159,
            "LABEL": "Icosahedron Rotation Z"
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
            "DEFAULT": 20.0,
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
            "DEFAULT": 15.0,
            "MIN": 5.0,
            "MAX": 40.0,
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
            "TARGET": "transformBuffer",
            "PERSISTENT": true,
            "FLOAT": true,
            "WIDTH": 1,
            "HEIGHT": 1
        },
        {
            "TARGET": "cameraBuffer",
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

// --- Constants and Macros ---
#define PI 3.14159265359
#define TAU (2.0*PI)
#define ROT(a) mat2(cos(a), sin(a), -sin(a), cos(a))
#define EPS 0.001

// --- Voxel Cube Data (13x13 Faces) ---
// Corrected arrays with proper size and GLSL syntax
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

// --- Shared Globals (for passing data between functions) ---
mat3 g_rot; // Voxel cube's animation rotation
mat3 g_icosahedronRot;
mat3 g_icosahedronRot_inv;
vec3 g_icosahedronPos; 
vec2 g_glowDistanceShapes;
float g_polyhedronSize;
float g_voxelWorldSize;
float g_colorModifier;

// --- Raymarching Constants ---
const int maxRayMarchesShapes = 70;
const float toleranceShapes = .001;
const float maxRayLengthShapes = 80.0;
const float normalEpsilonShapes= 0.01;
const int maxBouncesInsides = 5;

// --- Forward Declarations ---
float dfShapes(vec3 p);
vec3 renderWorld(vec3 ro, vec3 rd, float time);
vec3 effect(vec2 p, vec2 pp, float time, float glow, vec4 camera_params);

// --- Utility Functions ---
vec3 hsv2rgb_approx(vec3 hsv) {
    return (cos(hsv.x*TAU+vec3(0.,4.,2.))*hsv.y+2.-hsv.y)*hsv.z/2.;
}

vec3 aces_approx(vec3 v) {
    v = max(v, 0.0);
    v *= 0.6;
    float a = 2.51, b = 0.03, c = 2.43, d = 0.59, e = 0.14;
    return clamp((v*(a*v+b))/(v*(c*v+d)+e), 0.0, 1.0);
}

mat3 animatedRotationMatrix(float time) {
    float angle1 = time*0.5, angle2 = time*0.707, angle3 = time*0.33;
    float c1=cos(angle1), s1=sin(angle1), c2=cos(angle2), s2=sin(angle2), c3=cos(angle3), s3=sin(angle3);
    return mat3(c1*c2, c1*s2*s3-c3*s1, s1*s3+c1*c3*s2, c2*s1, c1*c3+s1*s2*s3, c3*s1*s2-c1*s3, -s2, c2*s3, c2*c3);
}

mat3 rotationMatrixXYZ(vec3 angles) {
    vec3 c = cos(angles);
    vec3 s = sin(angles);
    mat3 rotX = mat3(1.0, 0.0, 0.0, 0.0, c.x, -s.x, 0.0, s.x, c.x);
    mat3 rotY = mat3(c.y, 0.0, s.y, 0.0, 1.0, 0.0, -s.y, 0.0, c.y);
    mat3 rotZ = mat3(c.z, -s.z, 0.0, s.z, c.z, 0.0, 0.0, 0.0, 1.0);
    return rotZ * rotY * rotX;
}

vec2 hash2( vec2 p ) {
    return fract(sin(vec2(dot(p, vec2(127.1,311.7)), dot(p, vec2(269.5,183.3))))*43758.5453);
}

// --- Voxel Cube Rendering Logic ---
vec3 GetColor(float v) {
    return vec3(0.5) + vec3(0.5)*cos( 6.28318*(vec3(0.6, 0.4, 0.3)*v+vec3(0.6, 0.4, 0.3)) );
}

bool map(in vec3 p, out float v, float time) {
    float CUBE_SIZE = 13.0;
    float last = CUBE_SIZE - 1.0;
    vec2 uv;
    int index = -1;
    float cellValue = 0.0;
    v = sin(time) * g_colorModifier;
    if (p.z > -EPS && p.z < EPS) { uv = p.xy; if (all(greaterThanEqual(uv, vec2(0.0))) && all(lessThan(uv, vec2(CUBE_SIZE)))) { index = int(uv.y) * int(CUBE_SIZE) + int(uv.x); cellValue = faceFront[index]; }
    } else if (p.z > last - EPS && p.z < last + EPS) { uv = p.xy; if (all(greaterThanEqual(uv, vec2(0.0))) && all(lessThan(uv, vec2(CUBE_SIZE)))) { index = int(uv.y) * int(CUBE_SIZE) + int(uv.x); cellValue = faceBack[index]; }
    } else if (p.y > -EPS && p.y < EPS) { uv = p.xz; if (all(greaterThanEqual(uv, vec2(0.0))) && all(lessThan(uv, vec2(CUBE_SIZE)))) { index = int(uv.y) * int(CUBE_SIZE) + int(uv.x); cellValue = faceBottom[index]; }
    } else if (p.y > last - EPS && p.y < last + EPS) { uv = p.xz; if (all(greaterThanEqual(uv, vec2(0.0))) && all(lessThan(uv, vec2(CUBE_SIZE)))) { index = int(uv.y) * int(CUBE_SIZE) + int(uv.x); cellValue = faceTop[index]; }
    } else if (p.x > -EPS && p.x < EPS) { uv = p.zy; if (all(greaterThanEqual(uv, vec2(0.0))) && all(lessThan(uv, vec2(CUBE_SIZE)))) { index = int(uv.y) * int(CUBE_SIZE) + int(uv.x); cellValue = faceLeft[index]; }
    } else if (p.x > last - EPS && p.x < last + EPS) { uv = p.zy; if (all(greaterThanEqual(uv, vec2(0.0))) && all(lessThan(uv, vec2(CUBE_SIZE)))) { index = int(uv.y) * int(CUBE_SIZE) + int(uv.x); cellValue = faceRight[index]; }
    } return cellValue > 0.5;
}

bool IRayAABox(in vec3 ro, in vec3 rd, in vec3 invrd, in vec3 bmin, in vec3 bmax, out vec3 p0, out vec3 p1) {
    vec3 t0 = (bmin - ro) * invrd; vec3 t1 = (bmax - ro) * invrd;
    vec3 tmin = min(t0, t1); vec3 tmax = max(t0, t1);
    float fmin = max(max(tmin.x, tmin.y), tmin.z); float fmax = min(min(tmax.x, tmax.y), tmax.z);
    p0 = ro + rd*fmin; p1 = ro + rd*fmax;
    return fmax >= fmin;
}

vec3 AABoxNormal(vec3 bmin, vec3 bmax, vec3 p) {
    vec3 c = (bmin + bmax) * 0.5; vec3 d = p - c; vec3 ad = abs(d);
    if (ad.x > ad.y && ad.x > ad.z) return vec3(sign(d.x), 0, 0);
    if (ad.y > ad.z) return vec3(0, sign(d.y), 0);
    return vec3(0, 0, sign(d.z));
}

bool traceVoxelGrid(in vec3 initial_ro, in vec3 rd, in vec3 invrd, in vec3 bmin, in vec3 bmax, out vec3 n, out vec3 p, out float v, float time) {
    n=vec3(0.); p=vec3(0.); v=0.; vec3 current_ro; vec3 exit_point;
    if (!IRayAABox(initial_ro, rd, invrd, bmin, bmax, current_ro, exit_point)) return false;
    if (dot(exit_point - current_ro, rd) < EPS) return false;
    vec3 ep = floor(current_ro + rd*EPS); bool ret = false;
    for (int i = 0; i < 64; ++i) {
        if (map(ep, v, time)) { ret = true; break; }
        vec3 dummy_p0; IRayAABox(current_ro - rd*2.0, rd, invrd, ep, ep+1.0, dummy_p0, current_ro);
        ep = floor(current_ro + rd*EPS);
        if (dot(exit_point - current_ro, rd) < EPS) { ret = false; break; }
    }
    if (ret) { n = AABoxNormal(ep, ep+1.0, current_ro); p = current_ro; }
    return ret;
}

float intersectVoxelCube(in vec3 ro, in vec3 rd, out vec3 p_hit, out vec3 n_hit, out float v_hit, float time) {
    float CUBE_SIZE = 13.0;
    vec3 bmin_world = -vec3(g_voxelWorldSize * 0.5); vec3 bmax_world = vec3(g_voxelWorldSize * 0.5);
    vec3 invrd = 1.0/rd; vec3 p_entry, p_exit;
    if (!IRayAABox(ro, rd, invrd, bmin_world, bmax_world, p_entry, p_exit)) return 1e10;
    vec3 ro_start = p_entry;
    if (dot(p_entry - ro, rd) < 0.0) ro_start = ro;
    vec3 ro_vox = (ro_start - bmin_world) / g_voxelWorldSize * CUBE_SIZE;
    vec3 rd_vox = rd / g_voxelWorldSize * CUBE_SIZE;
    vec3 invrd_vox = 1.0/rd_vox; vec3 n_vox, p_vox;
    vec3 bmin_vox = vec3(0.0); vec3 bmax_vox = vec3(CUBE_SIZE);
    if (traceVoxelGrid(ro_vox, rd_vox, invrd_vox, bmin_vox, bmax_vox, n_vox, p_vox, v_hit, time)) {
        p_hit = bmin_world + (p_vox / CUBE_SIZE) * g_voxelWorldSize;
        n_hit = normalize(n_vox);
        if(distance(ro, p_hit) > distance(ro, p_exit) + EPS) return 1e10;
        return distance(ro, p_hit);
    }
    return 1e10;
}

// --- Main Scene Rendering ---
vec3 voronoi( in vec2 x, float time ) {
    vec2 ip = floor(x); vec2 fp = fract(x);
    vec2 mg, mr; float md = 8.0;
    for( int j=-1; j<=1; j++ ) for( int i=-1; i<=1; i++ ) {
        vec2 g = vec2(float(i),float(j));
        vec2 o = hash2( ip + g );
        o = 0.5 + 0.5*sin( time + TAU*o );
        vec2 r = g + o - fp; float d = dot(r,r);
        if( d<md ) { md = d; mr = r; mg = g; }
    }
    md = 8.0;
    for( int j=-2; j<=2; j++ ) for( int i=-2; i<=2; i++ ) {
        vec2 g = mg + vec2(float(i),float(j));
        vec2 o = hash2( ip + g );
        o = 0.5 + 0.5*sin( time + TAU*o );
        vec2 r = g + o - fp;
        if( dot(mr-r,mr-r)>0.00001 ) md = min( md, dot( 0.5*(mr+r), normalize(r-mr) ) );
    }
    return vec3( md, mr );
}

vec3 renderWorld(vec3 ro, vec3 rd, float time) {
    float bottom = -g_polyhedronSize - 0.5;
    vec3 col = hsv2rgb_approx(vec3(0.6, clamp(0.3+0.9*rd.y,0.0, 1.0), 2.*clamp(2.0-2.*rd.y*rd.y, 0.0, 2.)));
    float bt = -(ro.y-bottom)/(rd.y);
    if (bt > 0.) {
        vec3 bp = ro + rd*bt;
        vec3 v = voronoi(bp.xz * 0.4, time);
        
        vec3 groundCol = hsv2rgb_approx(vec3(0.7, 0.2, 1.5));
        vec3 cellColor = groundCol;
        vec3 borderColor = groundCol * 0.15;
        vec3 bcol = mix(borderColor, cellColor, smoothstep(0.01, 0.015, v.x));
        bcol *= (0.9 + 0.1 * sin(v.x * 50.0));
        
        float bfade = mix(1.,0.2, exp(-0.3*max(bt-15., 0.)));
        bcol *= bfade;
        
        col = mix(col, bcol, exp(-0.008*bt));
    }
    return col;
}

float intersectContainerExit(vec3 ro, vec3 rd) {
    float t = 0.01;
    for(int i=0; i<32; i++) {
        vec3 p = ro + rd * t; float d = dfShapes(p);
        if(d > -toleranceShapes) { return t; }
        t -= d * 0.8;
        if(t > maxRayLengthShapes) break;
    }
    return maxRayLengthShapes;
}

vec3 renderInsides(vec3 ro, vec3 rd, float time) {
    vec3 agg = vec3(0.0); float ragg = 1.0; float tagg = 0.0;
    g_rot = animatedRotationMatrix(sqrt(0.5)*0.5*time);
    mat3 g_rot_inv = transpose(g_rot);

    float beerHue = 0.75;
    vec3 beerFactor = -hsv2rgb_approx(vec3(beerHue+0.5, 0.75, 1.0));

    for (int bounce = 0; bounce < maxBouncesInsides; ++bounce) {
        if (ragg < 0.1) break;
        
        vec3 ro_ico_space = g_icosahedronRot_inv * (ro - g_icosahedronPos);
        vec3 rd_ico_space = g_icosahedronRot_inv * rd;
        
        vec3 ro_local = g_rot_inv * ro_ico_space;
        vec3 rd_local = g_rot_inv * rd_ico_space;

        vec3 p_voxel, n_voxel;
        float v_voxel;
        float t_voxel = intersectVoxelCube(ro_local, rd_local, p_voxel, n_voxel, v_voxel, time);
        float t_container = intersectContainerExit(ro, rd);
        vec3 beer = ragg * exp(0.1 * beerFactor * tagg);

        if (t_voxel < t_container) {
            tagg += t_voxel;
            vec3 ip = ro + rd * t_voxel;
            
            vec3 in_ = normalize( g_icosahedronRot * (g_rot * n_voxel) );
            vec3 ir = reflect(rd, in_);
            
            float ifre = 1.0 + dot(in_, rd); ifre *= ifre;
            ifre = mix(0.8, 1.0, ifre) * 0.8;
            
            vec3 voxel_color = GetColor(v_voxel);
            agg += voxel_color * beer;
            ragg *= ifre * 0.7;
            
            ro = ip + ir * 0.01;
            rd = ir;
        } else {
            tagg += t_container;
            vec3 ip = ro + rd * t_container;
            
            agg += renderWorld(ip, rd, time) * beer;
            break;
        }
    }
    return agg;
}

float sdIcosahedron(vec3 p, float r) {
    const float phi = (1.0+sqrt(5.0))*0.5; const float invphi = 1.0/phi;
    p = abs(p);
    
    vec3 n1 = normalize(vec3(1.0, 1.0, 1.0));
    vec3 n2 = normalize(vec3(0.0, invphi, phi));

    float d = dot(p, n1);
    d = max(d, dot(p, n2));
    d = max(d, dot(p, n2.zxy));
    d = max(d, dot(p, n2.yzx));
    
    return d - r;
}

float dfShapes(vec3 p) {
    vec3 p_transformed = g_icosahedronRot_inv * (p - g_icosahedronPos);
    float d = sdIcosahedron(p_transformed, g_polyhedronSize);
    g_glowDistanceShapes = vec2(d);
    return d;
}

float rayMarchShapes(vec3 ro, vec3 rd) {
    float t = 0.0;
    for (int i = 0; i < maxRayMarchesShapes; ++i) {
        float d = dfShapes(ro + rd*t);
        if (d < toleranceShapes || t > maxRayLengthShapes) break;
        t += d;
    }
    return t;
}

vec3 normalShapes(vec3 pos) {
    const vec2 eps = vec2(normalEpsilonShapes, 0.0);
    return normalize(vec3(
        dfShapes(pos+eps.xyy)-dfShapes(pos-eps.xyy),
        dfShapes(pos+eps.yxy)-dfShapes(pos-eps.yxy),
        dfShapes(pos+eps.yyx)-dfShapes(pos-eps.yyx))
    );
}

vec3 renderShapes(vec3 ro, vec3 rd, float time, float glow) {
    float bottom = -g_polyhedronSize - 0.5;
    vec3 col = renderWorld(ro, rd, time);
    float bt = -(ro.y-bottom)/(rd.y);
    vec3 bp = ro+rd*bt;
    float bd = dfShapes(bp);
    g_glowDistanceShapes = vec2(1E3);
    float st = rayMarchShapes(ro, rd);
    float sglowDistance = g_glowDistanceShapes.x;

    if (st < maxRayLengthShapes && (bt < 0.0 || st < bt)) {
        vec3 sp = ro+rd*st;
        vec3 sn = normalShapes(sp);
        float sfre = 1.0 + dot(rd, sn); sfre *= sfre;
        sfre = mix(0.05, 1.0, sfre);
        
        const float eta = 1.0 / 1.8;
        vec3 srr = refract(rd, sn, eta);
        
        float beerHue = 0.75;
        vec3 reflCol = hsv2rgb_approx(vec3(beerHue, 0.33, 0.33));
        vec3 reflected_color = renderWorld(sp, reflect(rd, sn), time) * reflCol;
        
        if (dot(srr, srr) < 0.001) { // Total Internal Reflection
            col = reflected_color;
        } else {
            vec3 refracted_color = renderInsides(sp, srr, time);
            col = mix(refracted_color, reflected_color, sfre);
        }
    } else if (bt > 0.0) {
        col *= mix(1.0, 0.125, exp(-bd));
    }
    
    vec3 outerGlowCol = hsv2rgb_approx(vec3(0.16,0.5,.0002));
    col += outerGlowCol/max(sglowDistance, toleranceShapes) * glow;
    return col;
}

vec3 effect(vec2 p, vec2 pp, float time, float glow, vec4 camera_params) {
    // --- New Camera System ---
    float cam_pan_angle = (time * 4.0) + camera_params.x; // pan, degrees
    float cam_tilt_angle = camera_params.y; // tilt, degrees
    float cam_height = camera_params.z;
    float cam_dist = camera_params.w;

    vec3 la = g_icosahedronPos; // Look at point is now the object's center

    // Calculate camera position using spherical coordinates
    float pan_rad = cam_pan_angle * PI / 180.0;
    float tilt_rad = cam_tilt_angle * PI / 180.0;
    vec3 ro_offset;
    ro_offset.x = cos(tilt_rad) * sin(pan_rad);
    ro_offset.y = sin(tilt_rad);
    ro_offset.z = cos(tilt_rad) * cos(pan_rad);

    vec3 ro = la + ro_offset * cam_dist;
    ro.y += cam_height; // Apply height offset
    
    // Clamp camera to prevent going under floor
    ro.y = max(ro.y, la.y - g_polyhedronSize - 0.4);

    // Standard camera matrix setup
    vec3 ww = normalize(la - ro);
    vec3 uu = normalize(cross(vec3(0.0, 1.0, 0.0), ww ));
    vec3 vv = (cross(ww,uu));
    
    vec3 rd = normalize(-p.x*uu + p.y*vv + 2.0*ww);
    vec3 col = renderShapes(ro, rd, time, glow);

    // Vignette and color grading
    col -= 0.03*vec3(2.0,3.0,1.0)*(length(p)+0.25);
    col *= smoothstep(1.7, 0.8, length(pp));
    col = aces_approx(col);
    col = sqrt(col);
    return col;
}

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
            currentParams = vec4(polyhedronSize, voxelWorldSize, colorModifier, glowIntensity);
        } else {
            vec4 targetParams = vec4(polyhedronSize, voxelWorldSize, colorModifier, glowIntensity);
            currentParams = mix(prevParamData, targetParams, smoothingFactor);
        }
        gl_FragColor = currentParams;
    }
    // --- Pass 2: Object Transform Buffer ---
    else if (PASSINDEX == 2) {
        vec4 prevTransformData = IMG_NORM_PIXEL(transformBuffer, vec2(0.5));
        vec4 currentTransforms;
        if (FRAMEINDEX == 0) {
             currentTransforms = vec4(rotX, rotY, rotZ, objectY);
        } else {
            vec4 targetTransforms = vec4(rotX, rotY, rotZ, objectY);
            currentTransforms = mix(prevTransformData, targetTransforms, smoothingFactor);
        }
        gl_FragColor = currentTransforms;
    }
    // --- Pass 3: Camera Parameters Buffer ---
    else if (PASSINDEX == 3) {
        vec4 prevCamData = IMG_NORM_PIXEL(cameraBuffer, vec2(0.5));
        vec4 currentCamData;
        if(FRAMEINDEX == 0) {
            currentCamData = vec4(cameraPan, cameraTilt, cameraHeight, cameraDistance);
        } else {
            vec4 targetCamData = vec4(cameraPan, cameraTilt, cameraHeight, cameraDistance);
            currentCamData = mix(prevCamData, targetCamData, smoothingFactor);
        }
        gl_FragColor = currentCamData;
    }
    // --- Pass 4: Final Render ---
    else {
        // Retrieve smoothed values from all buffers
        vec4 timeData = IMG_NORM_PIXEL(timeBuffer, vec2(0.5));
        vec4 paramData = IMG_NORM_PIXEL(paramBuffer, vec2(0.5));
        vec4 transformData = IMG_NORM_PIXEL(transformBuffer, vec2(0.5));
        vec4 cameraData = IMG_NORM_PIXEL(cameraBuffer, vec2(0.5));

        // Set global object parameters
        g_polyhedronSize = paramData.r;
        g_voxelWorldSize = paramData.g;
        g_colorModifier = paramData.b;
        float effectiveGlow = paramData.a;
        
        // Set global object transformation
        float smoothedObjectY = transformData.w;
        
        // Clamp the object's Y position to prevent it from going through the floor.
        // The floor is at y = -polyhedronSize - 0.5. The object's lowest point is at y - polyhedronSize.
        // So, y - polyhedronSize must be >= -polyhedronSize - 0.5, which simplifies to y >= -0.5.
        smoothedObjectY = max(smoothedObjectY, -0.5);

        g_icosahedronPos = vec3(0.0, smoothedObjectY, 0.0);
        vec3 effectiveRot = transformData.xyz;
        g_icosahedronRot = rotationMatrixXYZ(effectiveRot);
        g_icosahedronRot_inv = transpose(g_icosahedronRot);

        // Standard coordinate setup
        vec2 q = isf_FragNormCoord.xy;
        vec2 p = -1. + 2. * q;
        vec2 pp = p; // Store original p for vignette
        p.x *= RENDERSIZE.x/RENDERSIZE.y;
        
        // Render the final image
        float effectiveTime = timeData.r;
        vec3 col = effect(p, pp, effectiveTime, effectiveGlow, cameraData);
        
        gl_FragColor = vec4(col, 1.0);
    }
}
