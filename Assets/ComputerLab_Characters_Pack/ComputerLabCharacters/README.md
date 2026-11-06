# Computer Lab Characters – Unity import guide

Two separate, rigged, textured and animated characters for a Unity computer-lab scene.
Built in Blender 5.2 with MPFB2 (MakeHuman, CC0 assets) and exported as FBX.

| | Male_Teacher | Male_Student |
|---|---|---|
| Height | 1.75 m | 1.70 m |
| Look | Light-blue formal shirt, charcoal trousers, belt, black leather shoes, ID card on lanyard | Navy T-shirt, blue jeans, sneakers, backpack |
| Triangles | ~42.6k | ~32.5k |
| Meshes | Body, Eyes, Eyebrows, Eyelashes, Teeth, Tongue, Hair, Outfit, Shoes, IDLanyard | Body, Eyes, Eyebrows, Eyelashes, Teeth, Tongue, Hair, Outfit, Shoes, Backpack |
| Skeleton | 64 bones, Mixamo naming (`mixamorig:*`), T-pose rest, incl. jaw, eyes and eyelids | same |
| Blendshapes | `Blink`, `MouthOpen`, `Smile` (Body, Eyelashes, Eyebrows) | same |

## Folder contents

```
ComputerLabCharacters/            <- copy this whole folder into your project's Assets/
  Male_Teacher/
    Male_Teacher.fbx              model + skeleton + all 10 animation clips
    Textures/                     *_BaseColor, *_Normal, *_MetallicSmoothness, *_Occlusion (PNG)
    Animations/Male_Teacher@<Clip>.fbx   the same clips as separate animation-only FBX files
  Male_Student/                   same layout
  Scripts/Editor/LabCharacterImporter.cs   automatic import setup (Editor only)
  Scripts/Runtime/LabCharacterFace.cs      blinking, smile, talking lips
  Scripts/Runtime/LabSeatController.cs     sit / stand at a desk, keyboard and mouse hand IK
```

The Blender source files (`Male_Teacher.blend`, `Male_Student.blend`, with textures packed inside)
and the pipeline scripts are in `../BlenderSource`. **Keep the `.blend` files out of `Assets/`.**
Otherwise Unity imports them a second time through Blender.

## Animation clips (30 fps)

| Clip | Length | Loop | Notes |
|---|---|---|---|
| Idle | 4.0 s | yes | standing, breathing, small weight shift |
| Walk | 1.0 s | yes | root motion forward, about 0.87 m/s |
| Walk_InPlace | 1.0 s | yes | same walk without forward motion (move the character from script at 0.87 m/s) |
| Talk | 6.0 s | yes | standing lecture gestures, head nods, jaw movement |
| Sit_Down | 2.2 s | no | stand → seated on a 0.46 m chair |
| Sit_Idle | 4.0 s | yes | seated, hands on thighs |
| Typing | 4.0 s | yes | seated, both hands on the keyboard, finger taps |
| Mouse | 4.0 s | yes | seated, right hand on mouse with clicks, left hand at keyboard |
| Talk_Seated | 6.0 s | yes | seated talking / asking a question |
| Stand_Up | 2.0 s | no | seated → standing |

## Import steps

1. Copy the `ComputerLabCharacters` folder into `Assets/` (anywhere is fine).
2. Wait for the import to finish. `LabCharacterImporter.cs` runs automatically and:
   - sets **Rig → Animation Type: Humanoid**, **Avatar Definition: Create From This Model**,
     **Bake Axis Conversion**, scale 1 (the characters import at real height);
   - names the clips, sets loop time and root settings (see below);
   - marks normal maps as *Normal map* and mask textures as linear (non-sRGB);
   - creates `Materials/*.mat` for your render pipeline (Built-in Standard, URP Lit, or HDRP Lit)
     and remaps the FBX materials to them;
   - creates `Male_Teacher.controller` / `Male_Student.controller`;
   - creates `Male_Teacher.prefab` / `Male_Student.prefab` with Animator, `LabCharacterFace` and
     `LabSeatController` already attached.
3. Drag a prefab into the scene.

To re-run step 2 (for example after switching render pipeline), use
**Tools → Computer Lab Characters → Rebuild Materials, Animator and Prefabs**.

### Checking the Humanoid rig manually

Select the FBX → **Rig** tab → **Configure…**. Every required bone should be green. Unity maps the
`mixamorig:*` names automatically:

| Unity | Bone | Unity | Bone |
|---|---|---|---|
| Hips | mixamorig:Hips | Neck / Head | mixamorig:Neck / Head |
| Spine / Chest / UpperChest | Spine / Spine1 / Spine2 | Jaw | mixamorig:Jaw |
| Left/Right Shoulder | LeftShoulder / RightShoulder | Left/Right Eye | LeftEye / RightEye |
| Upper Arm / Lower Arm / Hand | LeftArm / LeftForeArm / LeftHand | Fingers | LeftHandIndex1-3 … Thumb1-3 |
| Upper Leg / Lower Leg | LeftUpLeg / LeftLeg | Foot / Toes | LeftFoot / LeftToeBase |

The rest pose is already a T-pose, so Unity's "Character not in T-Pose" warning should not appear.
Breast, buttock and eyelid helper bones are extra (non-humanoid) bones and stay unmapped. That's expected.

### Manual import settings (if you don't use the editor script)

- **Model:** Scale Factor 1, *Convert Units* on, **Bake Axis Conversion** on, Import BlendShapes on.
- **Rig:** Humanoid, Create From This Model. For the `@` animation files you can use
  *Copy From Other Avatar* and pick the main FBX's avatar.
- **Animation:** for each clip, tick *Loop Time* as in the table above. *Root Transform Rotation*,
  *Position (Y)* and *Position (XZ)*: **Bake Into Pose**, Based Upon **Original**. Only exception:
  `Walk` leaves *Position (XZ)* un-baked so it carries root motion.
- **Materials:** use the PNGs in `Textures/`: BaseColor → Albedo/Base Map,
  Normal → Normal Map, MetallicSmoothness → Metallic map (smoothness in its alpha), Occlusion → Occlusion.
  Use *Cutout* (alpha clip 0.4) for Hair, Eyebrows and Eyelashes.

## Animator parameters

| Parameter | Type | Effect |
|---|---|---|
| `Speed` | float | > 0.1 walks (Idle ↔ Walk) |
| `Talking` | bool | standing Idle ↔ Talk |
| `Seated` | bool | true → Sit_Down → Sit_Idle, false → Stand_Up → Idle |
| `Activity` | int | while seated: 0 Sit_Idle, 1 Typing, 2 Mouse, 3 Talk_Seated |

## Sitting at a desk

The seated clips are authored for a standard lab desk. Distances are relative to the **chair seat centre**
with the chair tucked in at the desk:

```
            monitor
   ┌──────────────────────┐  desk top 0.75 m
   │   [ keyboard ]  (m)  │  keyboard centre 0.41 m in front of the seat centre,
   └──────────────────────┘  mouse 0.30 m to the right of it
   ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─   desk front edge 0.26 m in front of the seat centre
          ┌──────┐
          │ seat │           seat height 0.46 m
          └──────┘
```

Set up `LabSeatController` on the character:

1. **Chair:** your chair transform. Its blue (+Z) axis must point toward the desk. Set
   *Chair Pivot Height* / *Seat Center Offset* if the pivot isn't on the floor below the seat centre.
2. Optional **Keyboard** and **Mouse** transforms make the hands follow your real props through
   Animator IK. The generated controller has the IK pass enabled. Without them the authored
   positions above are used.
3. Call it from your own code:

```csharp
var seat = teacher.GetComponent<ComputerLab.Characters.LabSeatController>();
// walk the character to seat.StandPoint first (NavMeshAgent, script, etc.), then:
seat.SitDown();                                     // pulls chair out, sits, shuffles in to the desk
seat.SetActivity(ComputerLab.Characters.LabActivity.Typing);   // Typing, Mouse, Talking, None
seat.StandUp();                                     // shuffles out, stands up
```

Tick **Start Seated** to spawn students already sitting at their desks.

The student's backpack is a separate mesh (`Male_Student_Backpack`). `LabSeatController` hides it
while seated so it doesn't clip through the chair back. Disable that GameObject to remove it
entirely.

## Face

`LabCharacterFace` blinks every 2–5.5 s and holds a gentle smile (`Smile` 0–100). It also moves the
lips (`MouthOpen`) whenever the Animator's `Talking` is true or `Activity` is 3, or when you tick
*Talking*. The Talk clips also animate the jaw bone.

## Walking

- With **Apply Root Motion** on (the prefab default), `Walk` moves the character forward by itself.
  Steer it by rotating the character and set `Speed` > 0.1.
- If a NavMeshAgent or CharacterController moves the character, replace the Walk state's motion
  with `Walk_InPlace` and move at ~0.87 m/s.
- `LabSeatController` turns root motion off while seated and back on after standing.

## Changing the characters

Everything is generated by the scripts in `../BlenderSource/pipeline_scripts`:

- `build_character.py`: body shape, height, skin tint, hair, clothing and colours (edit the
  `CONFIGS` dictionary at the top).
- `animate_export.py`: T-pose conversion, all animation clips (layout constants at the top) and FBX
  export.
- `run_all.sh`: rebuilds both characters end to end (needs Blender with the MPFB extension and
  the MakeHuman system asset pack).

## Credits and licence

Base meshes, skins, hair, eyes and clothing come from MakeHuman / MPFB2 system assets, released
under **CC0**. The lanyard, ID card, backpack, recoloured textures, generated normal and smoothness
maps, and all animations were made for this pack.
