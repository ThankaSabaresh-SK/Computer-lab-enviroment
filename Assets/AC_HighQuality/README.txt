AC_HighQuality — Unity asset package
====================================

Contents
--------
- AC.fbx: original model copied unchanged.
- textures/ac_basecolor_hd.png: 2048 x 2048 resized version of the supplied 512 x 512 base-color image, using Lanczos resampling and mild sharpening.
- textures/ac_basecolor_original.png: original base-color image for comparison/reference.
- textures/internal_ground_ao_texture.jpeg: original AO texture, unchanged.

Important quality note
---------------------
The new 2048 x 2048 image has more pixels, but this is conventional upscaling, not AI reconstruction. It cannot recover genuine details that were absent from the 512 x 512 source. The original FBX's UV layout was not inspected here, so the texture's compatibility with the model must be verified in Unity. Do not assume the FBX automatically uses this new texture; assign it to the correct material after confirming the mapping.

Suggested Unity texture settings
-------------------------------
Base color (ac_basecolor_hd.png):
- Texture Type: Default
- sRGB (Color Texture): On
- Max Size: 2048
- Filter Mode: Trilinear
- Aniso Level: 8–16 (especially for surfaces viewed at a shallow angle)
- Generate Mip Maps: On
- Compression: None while testing; if performance/storage matters, try High Quality and compare artifacts.

AO (internal_ground_ao_texture.jpeg):
- sRGB (Color Texture): Off if this is truly an AO/data map.
- Max Size: 2048 (the source itself is 512 x 512, so raising this setting does not add detail).
- Filter Mode: Trilinear; Aniso Level: 8–16; Generate Mip Maps: On for a 3D surface.
- Assign to the material's Occlusion/AO slot only after verifying it is the correct map and UV layout.

Manual setup
------------
1. Copy/extract this folder into your Unity project's Assets folder, e.g. Assets/AC_HighQuality/.
2. Select ac_basecolor_hd.png and verify the settings above in the Inspector, then Apply.
3. Select the AC model and inspect its material(s). Assign ac_basecolor_hd.png to Base Map/Albedo only if it matches the model's UV layout.
4. Assign the AO image to Occlusion only if confirmed appropriate. Do not put AO in the base-color slot.
5. Compare with the original at the same camera distance and lighting. If it still looks blurry, inspect the model's UVs/material links and the Game view resolution/render scale; texture upscaling alone may not solve those causes.

The original AC.fbx was not edited. Its .meta file is intentionally not included to avoid a duplicate Unity GUID if the original model already exists in the project; Unity will generate a new .meta for this copy. The new texture has its own unique .meta.
