/*{
    "CREDIT": "Created by @dot2dot",
    "DESCRIPTION": "An optical illusion with pulsating red and orange diamonds connected by static green dots, creating a sense of motion and warping.",
    "CATEGORIES": [
        "Generator",
        "Optical Illusion"
    ],
    "INPUTS": [
        {
            "NAME": "box_base_size",
            "TYPE": "float",
            "LABEL": "Box Size",
            "DEFAULT": 0.2,
            "MIN": 0.05,
            "MAX": 0.4
        },
        {
            "NAME": "pulsation_amount",
            "TYPE": "float",
            "LABEL": "Pulsation Amount",
            "DEFAULT": 0.25,
            "MIN": 0.0,
            "MAX": 0.45
        },
        {
            "NAME": "rotation_speed",
            "TYPE": "float",
            "LABEL": "Rotation Speed",
            "DEFAULT": 0.05,
            "MIN": -0.5,
            "MAX": 0.5
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

// --- ISF-Compliant GLSL Code ---

// Color definitions from the original shader
const vec3 RED    = vec3(1.0, 0.1, 0.1);
const vec3 ORANGE = vec3(1.0, 0.5, 0.0);
const vec3 GREEN  = vec3(0.0, 0.7, 0.1);
const vec3 WHITE  = vec3(1.0);

// Helper function to create a rounded box, used for the diamond shapes.
// This is a Signed Distance Function (SDF).
// p: current coordinate, b: box size, r: corner radius
float roundedBox(vec2 p, vec2 b, float r) {
    vec2 q = abs(p) - b;
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
}

void main() {
    // --- Coordinate Setup ---
    // Convert ISF's normalized coordinates to the centered,
    // aspect-corrected coordinates used in the original shader logic.
    // isf_FragNormCoord is equivalent to fragCoord.xy / RENDERSIZE
    vec2 uv = (isf_FragNormCoord * RENDERSIZE * 2.0 - RENDERSIZE) / RENDERSIZE.y;

    // Add a subtle rotation over time, controlled by the new input
    float angle = TIME * rotation_speed;
    float s = sin(angle);
    float c = cos(angle);
    uv *= mat2(c, -s, s, c);

    // --- Grid Calculation ---
    // The scale determines the size of the grid cells.
    float scale = 20.0;
    vec2 grid_uv = uv * scale;

    // Get integer and fractional parts of the grid coordinates.
    // grid_id is the unique ID for each cell (e.g., (0,0), (0,1), (1,1)).
    // grid_fract is the coordinate within the cell (-0.5 to 0.5).
    vec2 grid_id = floor(grid_uv);
    vec2 grid_fract = fract(grid_uv) - 0.5;

    // --- Pulsating Box Animation ---
    // Animate the size of the boxes based on their grid position and time.
    // A sine wave creates a smooth pulsating effect.
    float size_modulator = sin(TIME * 2.5 + grid_id.x * 0.5 + grid_id.y * 0.3) * 0.5 + 0.5; // Varies 0.0 to 1.0

    // The box size is now a combination of the base size and the pulsation amount.
    float box_size = box_base_size + size_modulator * pulsation_amount;

    // Calculate the distance to the diamond shape using the SDF.
    float diamond_dist = roundedBox(grid_fract, vec2(box_size), 0.2);

    // --- Color Assignment ---
    // Start with a white background.
    vec3 color = WHITE;

    // If the pixel is inside the diamond, color it red or orange.
    if (diamond_dist < 0.0) {
        // Use the grid ID to create a checkerboard pattern.
        if (mod(grid_id.x + grid_id.y, 2.0) == 0.0) {
            color = RED;
        } else {
            color = ORANGE;
        }
    }

    // --- Green Dot Creation ---
    // This part is unchanged from the original. The dots are fixed to the
    // grid corners, which preserves the "connection" illusion.
    // We shift the grid UVs by half a cell to find the corners.
    vec2 corner_uv = fract(grid_uv + 0.5) - 0.5;

    // Calculate distance to the center of this new "corner grid".
    float dot_dist = length(corner_uv);

    // If we are close enough to the corner's center, draw a green dot.
    if (dot_dist < 0.1) {
        color = GREEN;
    }

    // --- Final Output ---
    gl_FragColor = vec4(color, 1.0);
}
