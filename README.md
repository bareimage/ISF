# ISF Shaders

ISF, short for Interactive Shader Format, is a file format for creating video generators and FX plugins that can run across desktop, mobile, and WebGL platforms. Built on top of GLSL (OpenGL Shading Language), ISF provides a standardized way to create hardware-accelerated visual effects that can be shared and reused across different software applications without requiring environment-specific modifications.

## What are ISF Shaders

ISF shaders are essentially GLSL shaders with additional metadata that describes how to execute and interact with them. While GLSL is extremely powerful and flexible, it's a very low-level language designed for writing small snippets of code that often require additional work to be used within host applications. ISF extends GLSL by providing:

- A standardized way for shaders to provide information about adjustable properties
- Useful metadata for host applications to work with the shader in a standardized way
- Conventions for creating effects filters that process incoming image data
- Support for multiple rendering passes and persistent buffers

The core concept behind ISF is that each visual effect is designed as a small, self-contained file that can run independently or be combined with other ISF compositions within various host environments. This allows artists and developers to focus on creating specific visual effects without having to write entire applications around them.

## How are ISF Shaders used in realtime generative applications

ISF shaders are widely supported in nearly a dozen popular media servers, real-time video applications, and creative coding environments. They serve multiple purposes in these applications:

- **Visual Generators**: Creating standalone visual content from scratch
- **Effects/Filters**: Processing and transforming input images
- **Transitions**: Creating smooth transitions between different visual sources

In practice, ISF shaders are used in applications like VDMX, CoGe, and other VJ software; Apple Motion and Final Cut Pro (via plugins); MadMapper and other projection mapping tools; Smode, Videosync, and other media servers; and various creative coding environments through addons.

Artists and developers can create ISF compositions using dedicated tools like the ISF Editor, which provides a live preview environment. These shaders can then be installed globally on a system (in directories like `/Library/Graphics/ISF` on macOS) to be available to all compatible applications, or in application-specific locations.

## What are the challenges with ISF Shaders

One of the main challenges with traditional ISF shaders is their lack of memory between frames. Each time a shader renders a new frame, it starts completely fresh without any knowledge of what happened in previous frames. This creates several significant issues:

1. **Speed-dependent animations**: When you adjust the speed of an animation, it affects not just how fast the animation runs but also the position or state of the animation. This makes it difficult to create smooth, controllable animations where speed can be adjusted independently.

2. **No frame-to-frame memory**: Traditional shaders lack the ability to accumulate changes over time or remember previous states, which limits the types of effects that can be created.

3. **Attribute management**: Any adjustments to shader attributes impact the entire shader simultaneously, making it difficult to create gradual transitions or effects that build up over time.

This limitation is similar to "reincarnation" - each frame begins anew without recalling what happened before, making certain types of animations and effects extremely difficult to implement elegantly.

## What are persistent buffers

Persistent buffers are one of the most powerful features that ISF adds to GLSL. They are images (GL textures) that stay with the ISF file for as long as it exists, allowing shaders to retain information between render passes.

Persistent buffers solve the memory limitation by enabling shaders to:
- "Build up" an image over time
- Accumulate changes across multiple frames
- Store calculations for later evaluation
- Create feedback effects and other time-dependent visuals

### How to use persistent buffers for animation

To implement persistent buffers for animation:

1. **Define the buffer in the JSON metadata**:
```
"PASSES": [
  {
    "TARGET": "bufferName",
    "PERSISTENT": true,
    "FLOAT": true
  }
]
```

2. **Access the previous frame's data** in your shader code:
```
vec4 previousFrameData = IMG_THIS_PIXEL(bufferName);
```

3. **Mix current and previous frame data** to create smooth transitions:
```
vec4 freshPixel = IMG_THIS_PIXEL(inputImage);
vec4 stalePixel = IMG_THIS_PIXEL(bufferName);
gl_FragColor = mix(freshPixel, stalePixel, blurAmount);
```

The optional `FLOAT` attribute can be included to create a 32-bit buffer, which uses more memory but stores information more accurately between render passes.

With persistent buffers, you can create:
- Motion blur and feedback effects
- Smooth transitions between parameter changes
- Animations that accumulate over time
- Complex simulations like Conway's Game of Life

## About this Repository

This repository is created by interactive artist Igor Molochevski. The goal of this repo is to establish a stable foundation for high-quality ISF Shaders that incorporate persistent buffers to enable smooth and stable animations.

By leveraging persistent buffers, these shaders overcome the traditional limitations of frame-independent rendering, allowing for:
- Smooth transitions between parameter changes
- Speed adjustments that don't affect position or state
- Better attribute management across time
- More sophisticated visual effects that build up over multiple frames

The repository serves as a resource for VJs, motion designers, and interactive artists looking to create more sophisticated real-time visual experiences with ISF shaders.

## How were shaders tested

The shaders in this repository were tested on macOS using an M1 MacBook Pro with VDMX. 

M1 Macs have shown excellent performance with ISF shaders, even at high resolutions (4K) and with multiple layers running simultaneously. The Metal rendering pipeline on Apple Silicon provides efficient hardware acceleration for shader-based effects, making it an ideal platform for real-time visual performance.

VDMX is a professional VJ application that offers robust support for ISF shaders, including the latest features like persistent buffers. It provides a real-world performance environment to ensure the shaders work reliably in live performance scenarios.

## Learning Resources for ISF

- Official ISF Specification: https://github.com/mrRay/ISF_Spec
- ISF Editor: https://isf.video/
- VDMX ISF Tutorial: https://docs.vidvox.net/vdmx_video_generators_isf.html
- ISF Examples: https://editor.isf.video/
- Book of Shaders: https://thebookofshaders.com/

---

