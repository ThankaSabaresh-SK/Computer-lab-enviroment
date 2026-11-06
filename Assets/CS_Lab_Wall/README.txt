CS_Lab_Wall  -  standalone wall asset (modern college computer lab)
====================================================================

FILES
  CS_Lab_Wall.fbx      Primary asset (ASCII FBX 7.4, 8 materials, textures linked by relative path)
  CS_Lab_Wall.obj/.mtl Fallback (same geometry, UVs, materials)
  Textures/            All PNG maps. Keep the "Textures" folder next to the .fbx

DIMENSIONS & ORIENTATION   (units: metres, Y-up)
  Wall body (slab)      6.00 W (X)  x  3.20 H (Y)  x  0.20 D (Z)
  Overall bounds        X -3.000 .. +3.000   Y 0.000 .. 3.200   Z -0.114 .. +0.142   (6.00 x 3.20 x 0.256)
  Pivot / origin        (0,0,0) = bottom edge, horizontal centre, middle of slab thickness
  Front (detailed) face +Z      Rear face -Z (plain painted grey, with simple rear skirting)
  Wall runs along X. Top edge at Y = 3.20. No rotation or scale baked in (Lcl TRS = identity).

LAYOUT (front face, left to right, X in metres)
  Bays: 1.30 white | 0.70 blue | 2.00 white | 0.70 blue | 1.30 white
  Brushed-aluminium vertical strips at X = -1.70, -1.00, +1.00, +1.70 (+ slim end caps)
  Lower zone  Y 0.14-1.10  light-grey panels with 8 mm reveal gaps
  Chair rail  Y 1.10-1.14  warm white, with aluminium accent line above
  Upper zone  Y 1.14-3.00  warm-white and blue acoustic-fabric panels
  Crown       Y 3.00-3.20  aluminium line + two-step warm-white cornice
  Skirting    Y 0.00-0.14  graphite satin, stepped profile
  Displays    Left: CPU/circuit print 0.8x1.0 m | Centre: code + binary tree print 1.6x0.9 m | Right: network print 0.8x1.0 m
              (graphite frames, prints recessed 18 mm)

GEOMETRY
  718 triangles, 452 welded vertices, 8 materials / 8 submeshes. Hard-edged normals, 3-6 mm chamfers
  on panel edges for realistic highlights. Hidden back faces removed.

UVs
  One UV set. Architectural surfaces use planar world-space UVs at constant texel density (no stretching):
    paint / grey / graphite / aluminium: 1.00 m per tile (1024 px  = 1 px/mm)
    blue fabric: 0.25 m per tile (1024 px, ~1.9 mm weave thread)
  Artwork prints use 0-1 UVs on a single quad each (1280x1600 and 2560x1440 px, ~1.6 px/mm). UV V=1 is image top.

MATERIALS / TEXTURES (Textures/<name>_<map>.png)
  MAT_PaintWarmWhite    paint_warm_white      rough 0.80
  MAT_PanelLightGrey    panel_light_grey      rough 0.50
  MAT_FabricBlue        fabric_blue           rough 0.93
  MAT_BrushedAluminium  brushed_aluminium     metallic ~0.96, rough ~0.33 (brushing runs vertically on strips)
  MAT_GraphiteSatin     graphite_satin        rough 0.42
  MAT_Art_Circuit / MAT_Art_Network / MAT_Art_Code   art_circuit / art_network / art_code  (smoothness ~0.78)
  Maps per material:
    _albedo.png               sRGB colour
    _normal.png               tangent-space normal, OpenGL (+Y up) = Unity convention
    _roughness.png            linear greyscale
    _metallic_smoothness.png  RGB = metallic, A = smoothness (1 - roughness): Unity Standard / URP-Lit metallic map
  The FBX links albedo (DiffuseColor) and normal (NormalMap) automatically. If Unity does not auto-assign the
  metallic/smoothness map, drop *_metallic_smoothness.png into the material's Metallic slot (Source: Metallic Alpha).
  Set *_normal.png import type to "Normal map" if Unity does not prompt you.

VALIDATION
  FBX re-imported with an independent importer (Assimp 5.3): 718 faces, 8 materials, textures resolved, bounds as above.
  Not opened in Unity in this environment, so material auto-linking in Unity is untested.
