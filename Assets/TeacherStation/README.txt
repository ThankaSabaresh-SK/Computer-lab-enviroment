TEACHER WORKSTATION - Modern College Computer Lab (Unity URP / VR)
=================================================================
Import
 1. Copy the TeacherStation folder (FBX + Textures + Editor) into Assets/.
 2. Run  Tools > Lab Assets > Setup TeacherStation Materials  (creates URP/Lit materials, assigns textures, remaps slots).
 3. Drag TeacherStation.fbx into the scene. Root = LabTeacherStation_Root (pivot at floor, at the wall line).

Scale / axes
 - Authored in metres, written to FBX in cm (UnitScaleFactor 1) -> Unity file-scale 0.01 -> 1 unit = 1 m.
 - Y-up. Wall surface is the plane z = 0; the room extends toward +Z. Projector lens points toward the wall.
 - Reference sizes: desk 1.60 x 0.75 x 0.75 m | cabinet 0.80 x 0.45 x 1.80 m |
   display 2.14 x 1.22 m (bottom 0.90 m) | ceiling plane y = 2.90 m | projector 0.33 x 0.11 x 0.26 m.

Hierarchy (every group pivot sits at a sensible mounting point)
 LabTeacherStation_Root
  Desk_Teacher            (pivot: floor, centre of footprint)
    Desk_Top, Desk_Frame, Desk_FeetGlides, Desk_CableGrommet, Desk_Pedestal, Desk_DrawerFronts,
    Desk_DrawerHandles, Desk_DrawerLock, Desk_KeyboardTray, Desk_KeyboardTray_Slides
  Cabinet_Locked          (pivot: floor, centre of footprint)
    Cabinet_Body, Cabinet_Plinth, Cabinet_Door_Left, Cabinet_Door_Right, Cabinet_Handles
    Cabinet_Lock          (pivot: lock centre)  -> Cabinet_Lock_Barrel, Cabinet_Lock_Keyway
  Projector               (pivot: body centre)
    Projector_Body, Projector_Base, Projector_LensHousing, Projector_LensRing, Projector_LensGlass, Projector_Vents
  Projector_Mount         (pivot: ceiling plane)
    Mount_CeilingPlate, Mount_Pole, Mount_Head
  Board_Smart             (pivot: bottom-centre of frame, on wall)
    Board_Frame, Board_Surface, Board_BackPanel, Board_PenTray

Budget: 30 meshes, ~3.7k tris, 9 materials, 4 albedo textures + 1 normal map (1024 wood / 512 others).
Meshes are separate so doors/drawers/lock can be animated or given colliders individually.
Suggested: mark all static for baking; add BoxColliders to Desk_Top, Cabinet_Body and Board_Surface;
add a Rigidbody-free hinge pivot on the doors if you want them to open (door pivots are currently the group pivot).
