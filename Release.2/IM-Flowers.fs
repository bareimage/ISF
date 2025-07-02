/*{
  "DESCRIPTION": "Organic 3D Pattern (taste of noise 7, ISF 2.0 conversion, with smooth parameter transitions and temporal feedback)",
  "CREDIT": "Original by @leon_denise, ISF 2.0 Version by @dot2dot (bareimage)",
  "ISFVSN": "2.0",
  "CATEGORIES": ["GENERATOR", "3D", "ORGANIC", "FEEDBACK"],
  "INPUTS": [
    { "NAME": "speed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 5.0, "LABEL": "Animation Speed" },
    { "NAME": "transitionSpeed", "TYPE": "float", "DEFAULT": 2.0, "MIN": 0.1, "MAX": 10.0, "LABEL": "Transition Smoothness" },
    { "NAME": "zoom", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.5, "MAX": 2.0, "LABEL": "Camera Zoom" },
    { "NAME": "sphereSize", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.5, "MAX": 2.0, "LABEL": "Sphere Size" },
    { "NAME": "ambient", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 1.0, "LABEL": "Ambient Light" },
    { "NAME": "feedbackDecay", "TYPE": "float", "DEFAULT": 0.01, "MIN": 0.0, "MAX": 0.1, "LABEL": "Feedback Decay" }
  ],
  "PASSES": [
    { "TARGET": "paramBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 }, 
    { "TARGET": "feedbackParamBuffer", "PERSISTENT": true, "FLOAT": true, "WIDTH": 1, "HEIGHT": 1 }, 
    { "TARGET": "feedbackBuffer", "PERSISTENT": true, "FLOAT": true }, 
    { "TARGET": "finalOutput" }
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
// and indicate if changes were made. You may do so in any reasonable manner,
// but not in any way that suggests the licensor endorses you or your use.
// - NonCommercial: You may not use the material for commercial purposes.
// - ShareAlike: If you remix, transform, or build upon the material, you must
// distribute your contributions under the same license as the original.
//
// No additional restrictions: You may not apply legal terms or technological
// measures that legally restrict others from doing anything the license permits.
//
// DISCLAIMER: This work is provided "AS IS" without warranty of any kind, express
// or implied. The licensor makes no warranties regarding this work and disclaims
// liability for damages resulting from its use to the fullest extent possible

precision highp float;

const float pi = 3.14159265359;

// Global variables for the current pixel's state during map evaluation
// These are reset for each pixel before raymarching.
float _pixel_material_id; // Stores the material ID from the SDF
float _pixel_rng;         // Stores the random number for the current pixel

// Hash function (Dave Hoskins)
float hash13(vec3 p3)
{
    p3  = fract(p3 * .1031);
    p3 += dot(p3, p3.zyx + 31.32);
    return fract((p3.x + p3.y) * p3.z);
}

// Smooth minimum (Inigo Quilez)
float smin(float d1, float d2, float k) {
    float h = clamp(0.5 + 0.5*(d2-d1)/k, 0.0, 1.0);
    return mix(d2, d1, h) - k*h*(1.0-h);
}
float smoothing(float d1, float d2, float k) {
    return clamp(0.5 + 0.5*(d2-d1)/k, 0.0, 1.0);
}

// 2D rotation
mat2 rot(float a) {
    float c = cos(a), s = sin(a);
    return mat2(c, -s, s, c);
}

// Domain repetition
vec3 repeat(vec3 p, float r) {
    return mod(p, r) - r*0.5; // Corrected from r/2.0 to r*0.5 for clarity
}

// SDF map (core scene)
// Updates _pixel_material_id as a side effect. Uses _pixel_rng.
float map_sdf(vec3 p, float time, float sphereSizeParam)
{
    float t = time; // Current animation time
    float grid = 5.0; // Scale of the repeating grid
    vec3 cell = floor(p / grid); // ID of the current grid cell
    p = repeat(p, grid); // Repeat p within the grid cell

    float dp = length(p); // Distance from the center of the cell

    // Angle for rotations, varies with position and cell
    vec3 angle = vec3(0.1, -0.5, 0.1) + dp * 0.5 + p * 0.1 + cell;
    // Size of spheres, modulated by rng and input parameter
    float size = sin(_pixel_rng * pi) * sphereSizeParam;
    // Wave modulation for sphere stretching/kaleidoscope folding
    float wave = sin(-dp * 1.0 + t + hash13(cell) * 6.2831853) * 0.5;

    const int count = 4; // Number of kaleidoscope iterations (fixed from original)
    float a = 1.0; // Scaling factor for iterations
    float scene_dist = 1000.0; // Current minimum distance to scene
    float shape_dist;     // Distance to current shape

    // _pixel_material_id is reset before the raymarch for each pixel.
    // It's accumulated here through the iterations.
    float current_iter_material = _pixel_material_id;

    for (int index = 0; index < count; ++index)
    {
        // Kaleidoscopic folding and translation
        p.xz = abs(p.xz) - (0.5 + wave) * a;
        
        // Rotations
        p.xz *= rot(angle.y / a);
        p.yz *= rot(angle.x / a);
        p.yx *= rot(angle.z / a);

        // Sphere SDF
        shape_dist = length(p) - 0.2 * a * size;

        // Blend material ID based on proximity
        current_iter_material = mix(current_iter_material, float(index), smoothing(shape_dist, scene_dist, 0.3 * a));
        
        // Smooth minimum to combine with the scene
        scene_dist = smin(scene_dist, shape_dist, 1.0 * a);

        // Attenuate scale for next iteration
        a /= 1.9;
    }
    _pixel_material_id = current_iter_material; // Store final material for this map evaluation
    return scene_dist;
}

// Camera and ray setup
void setupCamera(
    in vec2 uv_aspect_corrected, // UV already corrected for aspect ratio
    in float zoom_param,
    out vec3 eye_pos,
    out vec3 ray_dir,
    out vec3 look_at_pos
) {
    eye_pos = vec3(1.0, 1.0, 1.0) * zoom_param; // Camera position, scaled by zoom
    look_at_pos = vec3(0.0, 0.0, 0.0); // Camera looks at origin
    
    // Camera basis vectors
    vec3 cam_forward = normalize(look_at_pos - eye_pos);
    vec3 cam_right = normalize(cross(cam_forward, vec3(0.0, 1.0, 0.0)));
    vec3 cam_up = cross(cam_right, cam_forward);
    
    // Calculate ray direction
    ray_dir = normalize(uv_aspect_corrected.x * cam_right + uv_aspect_corrected.y * cam_up + cam_forward);
}

// Normal calculation using central differences
vec3 calcNormal(vec3 pos, float time, float sphereSizeParam) {
    vec2 off = vec2(0.001, 0.0);
    // Store original _pixel_material_id and _pixel_rng as map_sdf modifies them
    float original_material = _pixel_material_id;
    float original_rng = _pixel_rng;

    float d_base = map_sdf(pos, time, sphereSizeParam);
    vec3 n = vec3(
        d_base - map_sdf(pos - off.xyy, time, sphereSizeParam),
        d_base - map_sdf(pos - off.yxy, time, sphereSizeParam),
        d_base - map_sdf(pos - off.yyx, time, sphereSizeParam)
    );
    
    // Restore original values if they are important for subsequent operations
    // For normal calculation, the material change isn't an issue here, but good practice.
    _pixel_material_id = original_material;
    _pixel_rng = original_rng;

    return normalize(n);
}


void main()
{
    // --- PASS 0: Parameter smoothing for main visual params ---
    if (PASSINDEX == 0) {
        vec4 prev = IMG_NORM_PIXEL(paramBuffer, vec2(0.5));
        float smoothedSpeed = (FRAMEINDEX == 0) ? speed : mix(prev.r, speed, min(1.0, TIMEDELTA * transitionSpeed));
        float smoothedZoom = (FRAMEINDEX == 0) ? zoom : mix(prev.g, zoom, min(1.0, TIMEDELTA * transitionSpeed));
        float smoothedSphereSize = (FRAMEINDEX == 0) ? sphereSize : mix(prev.b, sphereSize, min(1.0, TIMEDELTA * transitionSpeed));
        float smoothedAmbient = (FRAMEINDEX == 0) ? ambient : mix(prev.a, ambient, min(1.0, TIMEDELTA * transitionSpeed));
        gl_FragColor = vec4(smoothedSpeed, smoothedZoom, smoothedSphereSize, smoothedAmbient);
        return;
    }

    // --- PASS 1: Parameter smoothing for feedback params ---
    if (PASSINDEX == 1) {
        vec4 prevFeedback = IMG_NORM_PIXEL(feedbackParamBuffer, vec2(0.5));
        float smoothedFeedbackDecay = (FRAMEINDEX == 0) ? feedbackDecay : mix(prevFeedback.r, feedbackDecay, min(1.0, TIMEDELTA * transitionSpeed));
        gl_FragColor = vec4(smoothedFeedbackDecay, 0.0, 0.0, 1.0); // Store in .r
        return;
    }

    // --- PASS 2: Render scene to feedbackBuffer (Main rendering pass with temporal feedback) ---
    if (PASSINDEX == 2) {
        // Retrieve smoothed parameters
        vec4 params = IMG_NORM_PIXEL(paramBuffer, vec2(0.5));
        float currentSpeed = params.r;
        float currentZoom = params.g;
        float currentSphereSize = params.b;
        float currentAmbient = params.a;

        vec4 feedbackParams = IMG_NORM_PIXEL(feedbackParamBuffer, vec2(0.5));
        float currentFeedbackDecay = feedbackParams.r;

        // Animation time
        float time = TIME * currentSpeed;

        // Normalized coordinates, aspect corrected
        vec2 uv_norm = isf_FragNormCoord.xy; // 0-1 range
        vec2 uv_centered = uv_norm * 2.0 - 1.0; // -1 to 1 range
        uv_centered.x *= RENDERSIZE.x / RENDERSIZE.y; // Aspect correction

        // Mouse control (from original ShaderToy, normalized 0-1 from ISF)
        vec2 m = vec2(0.0); // Default no mouse movement
        #ifdef MOUSE_DOWN // Check if mouse is down, isf_MOUSE is available if so
            if (isf_MOUSE.z > 0.0 || isf_MOUSE.w > 0.0) { // z or w for click/drag
                 m = (isf_MOUSE.xy / RENDERSIZE.xy - 0.5) * 2.0 * pi; // Convert to -pi to pi range
            }
        #else // Fallback if MOUSE_DOWN is not defined by host, or use a direct input
            // If you have a point2D input for mouse, use it here.
            // For now, assumes no mouse if MOUSE_DOWN is not active.
        #endif


        // Camera setup
        vec3 eye, ray, at_target;
        setupCamera(uv_centered, currentZoom, eye, ray, at_target);

        // Rotate camera with mouse
        float mx = m.x;
        float my = m.y;
        ray.xz *= rot(mx); eye.xz *= rot(mx);
        ray.xy *= rot(my); eye.xy *= rot(my);

        // Initialize per-pixel state variables
        vec3 seed_rng = vec3(uv_norm * RENDERSIZE.xy, time); // Seed for RNG
        _pixel_rng = hash13(seed_rng); // Generate RNG for this pixel
        _pixel_material_id = 0.0;      // Reset material ID for this pixel's raymarch

        // Raymarch
        vec3 pos = eye;
        const float max_steps = 30.0;
        float steps_taken = max_steps; // Start with max, count down to 0 if hit early
        float dist_to_surface = 0.0;
        float hit_material_id = 0.0; // Store the material ID at the actual hit point

        for (float i = 0.0; i < max_steps; ++i) {
            // map_sdf uses and updates _pixel_material_id and uses _pixel_rng
            dist_to_surface = map_sdf(pos, time, currentSphereSize);
            if (dist_to_surface < 0.01) {
                steps_taken = i; // Record how many steps it took
                hit_material_id = _pixel_material_id; // Capture material at this hit point
                break;
            }
            // Apply dithering to step distance
            dist_to_surface *= (0.9 + 0.1 * _pixel_rng);
            pos += ray * dist_to_surface;

            // Safety break if ray goes too far
            if (length(pos - eye) > 100.0) {
                dist_to_surface = 100.0; // Indicate far away / no hit
                break;
            }
        }

        vec3 final_calc_color = vec3(0.0); // Default to black / background

        if (dist_to_surface < 0.01) { // If we hit something
            // Ambient occlusion based on steps taken (more steps = further away = more occluded)
            // Original: float shade = index / steps; (index was loop counter from steps down to 0)
            // Here, steps_taken is 0 to max_steps. So, closer hit = less steps_taken.
            // We want shade to be 1 for close hits, 0 for far.
            float shade = 1.0 - (steps_taken / max_steps);
            shade = clamp(shade, 0.0, 1.0);


            // Normal calculation at hit point
            // calcNormal will call map_sdf, which uses _pixel_rng and modifies _pixel_material_id.
            // We've already stored hit_material_id. _pixel_rng is fine to be reused.
            vec3 normal = calcNormal(pos, time, currentSphereSize);

            // Color palette based on material at hit point
            vec3 tint = 0.5 + 0.5 * cos(vec3(3.0, 2.0, 1.0) + hit_material_id * 0.5 + length(pos) * 0.5);

            // Lighting
            float NdotL1 = dot(reflect(ray, normal), normalize(vec3(0.0, 1.0, 0.0))) * 0.5 + 0.5; // Light from above
            vec3 light_contrib = vec3(1.0, 0.502, 0.502) * sqrt(max(0.0, NdotL1));
            float NdotL2 = dot(reflect(ray, normal), normalize(vec3(0.0, 0.0, -1.0))) * 0.5 + 0.5; // Light from behind-ish
            light_contrib += vec3(0.4, 0.714, 0.145) * sqrt(max(0.0, NdotL2)) * 0.5;
            
            // Modified final color calculation
            // Original was: (tint + light) * (shade * currentAmbient + 0.3);
            // The "+ 0.3" made it too bright.
            // Let's try: (tint + light_contrib) * shade * currentAmbient;
            // This makes currentAmbient a master brightness control over the shaded result.
            final_calc_color = (tint + light_contrib) * shade * currentAmbient;

        } else {
            // If no hit, color is black (or could be a fog color)
            final_calc_color = vec3(0.0);
        }

        // Temporal Feedback
        vec4 previous_frame_color = IMG_NORM_PIXEL(feedbackBuffer, uv_norm);
        vec4 current_frame_color = vec4(final_calc_color, 1.0);
        
        // Blend with previous frame
        gl_FragColor = max(current_frame_color, previous_frame_color - currentFeedbackDecay);
        return;
    }

    // --- PASS 3: Output final image from feedbackBuffer ---
    if (PASSINDEX == 3) {
        gl_FragColor = IMG_NORM_PIXEL(feedbackBuffer, isf_FragNormCoord.xy);
        return;
    }
}
