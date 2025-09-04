
## Interactive Shaders Release 5 is Here!

Hey everyone, @dot2dot (bareimage) here! I'm thrilled to announce the launch of **Release 5**, a brand new collection of interactive shaders designed to push the boundaries of real-time visuals. This release is packed with deep customization, complex geometry, and a couple of major innovations I've been working on that I can't wait for you all to use.

One of the biggest breakthroughs in this release is a new, modular approach to voxel design, which you'll see in shaders like **`EmoCube-Complex`** and **`VohelHead-Icosahedron`**. The pattern on each side of the voxel cube is now defined by a simple array in the code. This means you're no longer stuck with my designs; you can create your own intricate, pixel-perfect art with incredible freedom. To make this process as easy as possible, I've built a small web utility to help you generate these arrays: **[Voxel Array Builder](https://github.com/bareimage/ISF/blob/main/Misc/VoxelArrayBuilder.html)**.

Another major step forward is the built-in **material template engine** you'll find in several of this release's shaders. By sharing a consistent set of generative color palettes, this engine allows artists to achieve a predictable and powerful look across different visuals. This is a game-changer for live performances, ensuring your aesthetic remains coherent as you transition between shaders.

As always, the creative coding community is all about collaboration. While all the shaders in this release were built by me, I want to give a huge shout-out to **@mrange**, whose brilliant graphics pipeline I've integrated into two of the shaders. Additionally, many of the core utility functions and color palettes are based on the fantastic designs of **@XorDev**.

Now, let's dive into the shaders!

---

### IM-NoiseThinng
This shader renders complex, animated toroidal structures made of interwoven cubes. It features interactive mouse control for the camera and a dynamic background that reacts to the main object, with several styles to choose from. This shader has a unique development story. It started its life not in GLSL, but as a LISP/Janet prototype in Bauble Studio. Prototyping in a LISP environment is incredibly powerful; it helps you build amazing shaders without worrying about writing complex SDF functions from scratch. Once the concept was solid, I converted it to GLSL for this release. You can read more about this fascinating approach [here](https://ianthehenry.com/posts/bauble/building-bauble/).

---

### EmoCube1
This is the one that started it all! `EmoCube1` is the original, featuring a tumbling cube with a different emotion carved into each face using Signed Distance Functions (SDFs). While it's simpler than its successors, it lays the foundation for the voxel-based rendering techniques explored in this release. The powerful rendering engine for this shader is taken from an original work by **@mrange**. It’s included here for reference and as a piece of history.

---

### EmoCube-Complex & EmoCube-Complex+
This is where the voxel array system truly shines. **`EmoCube-Complex`** takes the original concept and rebuilds it with fully customizable, array-defined faces and a powerful engine of 17 generative color palettes. For those who want to push things even further, **`EmoCube-Complex+`** adds independent XYZ rotation controls and a wild "Material Twisting" feature that warps the texture patterns across the surface of the cube as it tumbles.

---

### VohelHead-Icosahedron
This shader places an animated, array-defined voxel cube inside a raymarched icosahedron shell, set against a dynamic Voronoi-patterned floor. You have full control over the object's position and rotation, as well as the camera, allowing for dramatic, sweeping shots of the scene. The interplay between the glowing inner cube and the refractive outer shell creates a stunning sense of depth and energy. The foundation of this shader's rendering pipeline was built upon one of **@mrange's** incredible shaders, which I adapted for this scene.

---

### PixelTunnel
Take a journey down an infinite, twisting tunnel made of voxel faces. `PixelTunnel` creates a mesmerizing effect with a fluid, physics-based motion that causes the camera's focus to wander organically around the screen. Featuring procedurally generated patterns and deep control over color, twisting, and movement, this shader is perfect for creating hypnotic, ever-evolving visuals.

---

### 3MetaBallProblem
Funny story about this one—for over 15 years, I misread "metaballs" as "meatballs"! This shader is my tribute to that lightbulb moment. It features three glowing metaballs that merge and separate in a fluid, organic dance, set against a dynamic "twists and turns" background. It also includes a massive library of 25 animated color palettes, giving you an incredible range of looks right out of the box.

---

### Fold-V3-Final & Fold-V3-Serpinski-Final
These two shaders explore the beauty of 3D folding fractals. **`Fold-V3-Final`** is a classic folding cube fractal, but with a fun, glitchy halo effect and the new material template engine. For a different geometric flavor, **`Fold-V3-Serpinski-Final`** modifies the core algorithm to produce a fractal with beautiful tetrahedral symmetry, reminiscent of a Sierpinski pyramid, offering a more intricate and crystalline structure.

---

### AteraField-Candid1
This shader creates a unique 2.5D effect by rendering flowing layers of animated 2D cross-sections of a 3D icosahedron field. It gives the impression of flying through a celestial field of complex, crystalline objects. It integrates object and rotation controls and includes a post-process radial blur to add a sense of speed and motion.

---

### KaleidoKnot
`KaleidoKnot` generates a seamless, warping kaleidoscope effect that is both intricate and infinitely mesmerizing. It features the new material template engine and provides independent, smoothed animation controls for the geometry and colors, allowing you to create everything from gentle, flowing patterns to chaotic, high-energy visuals with just a few slider adjustments.

---

I can't wait to see what you all create with Release 5. Dive in, experiment, and have fun!

-@dot2dot (bareimage)
