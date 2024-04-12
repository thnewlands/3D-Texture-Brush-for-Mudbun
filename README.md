**SDF Texture Solid Brush** 

<img src="https://github.com/thnewlands/3D-Texture-Brush-for-Mudbun/assets/4378629/088ddd5b-7b44-4ffd-942c-ff4dc253e2c4" width="250" height="250">

This repository contains code for extending Long Bunny Labs's [MudBun](https://assetstore.unity.com/packages/tools/particles-effects/mudbun-volumetric-vfx-modeling-177891) to support 3D SDF Textures. It currently supports up to 64 R16 or R32 SFloat 128x128x128 textures at a time by packing them into one R32 512x512x512 texture.

**How to Install**

1. Drag and drop contents of this repo into Mudbun's subfolder: `Assets/Mudbun/Customization`

2. Add the brush function to CustomBrush.cginc

Inside of CustomBrush.cginc, inside of the switch statement for `brush.type` add:

```
case kSDFTextureSolid:
{
   res = sdf_texture3D_brush(brush, pRel);
  break;
}
```

and just above the sdf_custom_brush function add:

```
#include "SDFTextureSolidBrush.cginc"
```

It should look something like this:

```

#include "SDFTextureSolidBrush.cginc"

// returns custom SDF value, the signed distance from solid surface
float sdf_custom_brush
(
  float res,      // current SDF result before brush is applied
  inout float3 p, // sample position in world space (distortion brushes modify this)
  float3 pRel,    // sample position in brush space (relative to brush transform)
  SdfBrush brush  // brush data (see BrushDefs.cginc for data layout)
)
{
  float3 h = 0.5f * brush.size;

  // add/modify custom brushes in this switch statement
  switch (brush.type)
  {
    case kCustomSolid:
    {
      // box
      res = sdf_box(pRel, h, brush.radius);
      break;
    }
    case kSDFTextureSolid:
    {
        res = sdf_texture3D_brush(brush, pRel);
        break;
    }
...
...
...
```


3. For ease of creation, add a dropdown item to CustomCreationMenu.

Inside of CustomCreationMenu.cs add:

```
  [MenuItem("GameObject/MudBun/Custom/SDF Texture Solid", priority = 4)]
  public static GameObject CreateSDFTextureSolid()
  {
    var go = CreateGameObject("Mud SDF Texture Solid");
    go.AddComponent<SDFTextureSolidBrush>();

    return OnBrushCreated(go);
  }
```

4. At this point you should be able to drag and drop a 3D Texture into the scene!

Now, when you click the dropdown item "GameObject -> MudBun -> Custom -> SDF Texture Solid" you should see Suzzane appear in your scene. If she doesn't, you probably are missing .meta files from this repo. 

<img src="https://github.com/thnewlands/3D-Texture-Brush-for-Mudbun/assets/4378629/d292c4a1-5ab7-4b12-b26d-03d4abf1e920" width="100" height="100">
<img src="https://github.com/thnewlands/3D-Texture-Brush-for-Mudbun/assets/4378629/38108378-4577-47bd-a695-a09402c3960b" width="100" height="100">
<img src="https://github.com/thnewlands/3D-Texture-Brush-for-Mudbun/assets/4378629/088ddd5b-7b44-4ffd-942c-ff4dc253e2c4" width="100" height="100">

1. Suzzane facing down.
2. Suzzane facing up with a few more voxels in resolution.
3. Clumpy SDF blending with multiple 3D Textures.

**Making Textures**

[Unity's SDF Bake Tool](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.0/manual/sdf-bake-tool-window.html), built into their VFX Graph Package, works great for this. There are two things to remember: 
1. Remember to make cube shaped textures with uniform scaling using fit cube to mesh.

![image](https://github.com/thnewlands/3D-Texture-Brush-for-Mudbun/assets/4378629/c99a0a2c-ff1a-4386-b38c-bbd57604b3f7)

2. Make sure that SDFs are 128x128x128 in resolution.

Once created you can drag the SDF into SDFTextureSolidBrush's Sdf Texture slot and if you did everything right it should just work! 

Tested with Unity 2022.3.21f and Mudbun 1.5.47

When figuring out blending at a distance with bounded SDF textures I referenced EmmetOT's [Isomesh](https://github.com/EmmetOT/IsoMesh) and Kosmonaut's [very informative post](https://kosmonautblog.wordpress.com/2017/05/09/signed-distance-field-rendering-journey-pt-2/) on the subject. I also highly recommend giving [Mudbun](https://assetstore.unity.com/packages/tools/particles-effects/mudbun-volumetric-vfx-modeling-177891) a shot if you have the resources. 
