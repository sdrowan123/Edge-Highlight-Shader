# Edge Highlight Shader
This is the first in a suite of shaders intended to easily and algorithmically paint models in a miniature-painting style. This shader is a work in progress but is intended to work on any 3D model.

![pic](https://user-images.githubusercontent.com/98781207/171671573-67ee6aee-972d-4e7a-80da-13c0725deb11.PNG)


## How to use:
In Unity, create two folders titled 'Uninitiated' and 'Initiated'. Create a folder called 'Resources' in the Uninitated folder. Installing the C# scripts included will add a tool to the Unity tools menu titled 'Initialize Prefabs'. When run, this tool will take any prefabs in the Unitiated/Resources folder, initialize it with highlight map, apply the shader to it, and move it to the Initiated folder.

## How it works:
The shader works off of a baked highlight map. The highlight map is baked based on relative angle between vertices and relative distance between vertices. There are a number of heuristically determined parameters which allow this to achieve the look I was looking for, the code and parameters for highlight map baking can be seen in PrefabInit/BakeHighlightMaps.cs. On top of that, there are a number of normalization functions which iterate over the highlight map.

Initially I was against baking a map per-pixel and wanted the map to be per-vertex. Unfortunately, this led to very jagged triangles in the final shader due to barycentric interpolation in the fragment shader. The approach I used instead was to bake a bilinear interpolation per pixel based on nearby vertices. This needs a lot of work and code optimization as it's very slow and still completely unthreaded, though it does render the look I was going for.
