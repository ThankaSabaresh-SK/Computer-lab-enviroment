// Computer Lab Characters - automatic Unity import setup.
// Applies Humanoid rig, scale, clip loop/root-motion settings, texture types, builds materials,
// an Animator Controller and a ready-to-use prefab for Male_Teacher.fbx and Male_Student.fbx.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

namespace ComputerLab.Characters.Editor
{
    public class LabCharacterImporter : AssetPostprocessor
    {
        static readonly string[] Characters = { "Male_Teacher", "Male_Student" };
        static readonly HashSet<string> LoopClips = new HashSet<string>
            { "Idle", "Walk", "Walk_InPlace", "Talk", "Sit_Idle", "Typing", "Mouse", "Talk_Seated" };

        static bool IsCharacterFbx(string path)
        {
            string file = Path.GetFileName(path);
            return path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase) && Characters.Any(file.StartsWith);
        }

        static bool IsAnimationOnly(string path) => Path.GetFileName(path).Contains("@");

        void OnPreprocessModel()
        {
            if (!IsCharacterFbx(assetPath)) return;
            var mi = (ModelImporter)assetImporter;
            mi.globalScale = 1f;
            mi.useFileScale = true;
            mi.bakeAxisConversion = true;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importVisibility = false;
            mi.importBlendShapes = !IsAnimationOnly(assetPath);
            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.optimizeGameObjects = false;
            mi.importAnimation = true;
            mi.resampleCurves = true;
            mi.animationCompression = ModelImporterAnimationCompression.Optimal;
            mi.materialImportMode = IsAnimationOnly(assetPath)
                ? ModelImporterMaterialImportMode.None
                : ModelImporterMaterialImportMode.ImportStandard;
            mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
            mi.importTangents = ModelImporterTangents.CalculateMikk;
        }

        void OnPreprocessAnimation()
        {
            if (!IsCharacterFbx(assetPath)) return;
            var mi = (ModelImporter)assetImporter;
            var clips = mi.defaultClipAnimations;
            foreach (var c in clips)
            {
                string n = c.name;
                if (IsAnimationOnly(assetPath))
                    n = Path.GetFileNameWithoutExtension(assetPath).Split('@')[1];
                else if (n.Contains("|"))
                    n = n.Substring(n.LastIndexOf('|') + 1);
                c.name = n;
                c.loopTime = LoopClips.Contains(n);
                // Root transform: everything baked into the pose so the character stays on its spot,
                // except "Walk", which carries forward root motion (about 0.87 m/s).
                c.lockRootRotation = true;
                c.keepOriginalOrientation = true;
                c.lockRootHeightY = true;
                c.keepOriginalPositionY = true;
                c.heightFromFeet = false;
                c.lockRootPositionXZ = n != "Walk";
                c.keepOriginalPositionXZ = true;
            }
            mi.clipAnimations = clips;
        }

        void OnPreprocessTexture()
        {
            if (!Characters.Any(c => assetPath.Contains("/" + c + "/Textures/"))) return;
            var ti = (TextureImporter)assetImporter;
            string f = Path.GetFileNameWithoutExtension(assetPath);
            ti.mipmapEnabled = true;
            ti.maxTextureSize = 2048;
            if (f.EndsWith("_Normal"))
                ti.textureType = TextureImporterType.NormalMap;
            else if (f.EndsWith("_MetallicSmoothness") || f.EndsWith("_Occlusion"))
                ti.sRGBTexture = false;
            else if (f.EndsWith("_BaseColor"))
                ti.alphaIsTransparency = f.Contains("_Hair_") || f.Contains("_Eyebrows_") || f.Contains("_Eyelashes_");
        }

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var p in imported.Where(p => IsCharacterFbx(p) && !IsAnimationOnly(p)))
            {
                string path = p;
                EditorApplication.delayCall += () => LabCharacterSetup.Setup(path, false);
            }
        }
    }

    public static class LabCharacterSetup
    {
        static readonly Dictionary<string, float> Smoothness = new Dictionary<string, float>
        {
            { "Body", 0.38f }, { "Eyes", 0.85f }, { "Eyebrows", 0.15f }, { "Eyelashes", 0.15f }, { "Teeth", 0.6f },
            { "Tongue", 0.5f }, { "Hair", 0.35f }, { "Outfit", 0.18f }, { "Shoes", 0.35f }, { "Lanyard", 0.3f },
            { "Backpack", 0.22f },
        };
        static readonly HashSet<string> Cutout = new HashSet<string> { "Hair", "Eyebrows", "Eyelashes" };

        [MenuItem("Tools/Computer Lab Characters/Rebuild Materials, Animator and Prefabs")]
        static void RebuildAll()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                string f = Path.GetFileName(p);
                if ((f == "Male_Teacher.fbx" || f == "Male_Student.fbx")) Setup(p, true);
            }
        }

        public static void Setup(string fbxPath, bool force)
        {
            if (!(AssetImporter.GetAtPath(fbxPath) is ModelImporter)) return;
            BuildMaterials(fbxPath, force);
            var controller = BuildController(fbxPath, force);
            BuildPrefab(fbxPath, controller, force);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                Debug.LogWarning($"[ComputerLab] {fbxPath}: Humanoid avatar is not valid - open Rig > Configure to check the bone mapping.");
        }

        static string Dir(string fbxPath) => Path.GetDirectoryName(fbxPath).Replace('\\', '/');

        static void BuildMaterials(string fbxPath, bool force)
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
            string dir = Dir(fbxPath);
            string charName = Path.GetFileNameWithoutExtension(fbxPath);
            string matDir = dir + "/Materials";
            if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder(dir, "Materials");
            var existing = mi.GetExternalObjectMap();
            bool changed = false;

            var rp = GraphicsSettings.currentRenderPipeline;
            string rpName = rp != null ? rp.GetType().Name : "";
            bool urp = rpName.Contains("Universal");
            bool hdrp = rpName.Contains("HDRenderPipeline");
            Shader shader = Shader.Find(urp ? "Universal Render Pipeline/Lit" : hdrp ? "HDRP/Lit" : "Standard");

            foreach (var src in AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Material>())
            {
                string matName = src.name;
                var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), matName);
                if (!force && existing.ContainsKey(id) && existing[id] != null) continue;
                string part = matName.StartsWith(charName + "_") ? matName.Substring(charName.Length + 1) : matName;
                string matPath = $"{matDir}/{matName}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    mat = new Material(shader) { name = matName };
                    AssetDatabase.CreateAsset(mat, matPath);
                }
                else mat.shader = shader;

                Texture2D T(string kind) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/Textures/{matName}_{kind}.png");
                var baseColor = T("BaseColor");
                var normal = T("Normal");
                var metalSmooth = T("MetallicSmoothness");
                var occlusion = T("Occlusion");
                float smooth = Smoothness.TryGetValue(part, out var s) ? s : 0.3f;
                bool cutout = Cutout.Contains(part);

                if (urp)
                {
                    mat.SetTexture("_BaseMap", baseColor);
                    mat.SetColor("_BaseColor", Color.white);
                    if (normal) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); }
                    if (metalSmooth) { mat.SetTexture("_MetallicGlossMap", metalSmooth); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); mat.SetFloat("_Smoothness", 1f); }
                    else mat.SetFloat("_Smoothness", smooth);
                    if (occlusion) { mat.SetTexture("_OcclusionMap", occlusion); mat.EnableKeyword("_OCCLUSIONMAP"); }
                    if (cutout)
                    {
                        mat.SetFloat("_AlphaClip", 1f); mat.SetFloat("_Cutoff", 0.4f); mat.EnableKeyword("_ALPHATEST_ON");
                        mat.SetFloat("_Cull", 0f);
                        mat.SetOverrideTag("RenderType", "TransparentCutout");
                        mat.renderQueue = (int)RenderQueue.AlphaTest;
                    }
                }
                else if (hdrp)
                {
                    mat.SetTexture("_BaseColorMap", baseColor);
                    if (normal) { mat.SetTexture("_NormalMap", normal); mat.EnableKeyword("_NORMALMAP"); mat.EnableKeyword("_NORMALMAP_TANGENT_SPACE"); }
                    mat.SetFloat("_Metallic", 0f);
                    mat.SetFloat("_Smoothness", smooth);
                    if (cutout)
                    {
                        mat.SetFloat("_AlphaCutoffEnable", 1f); mat.SetFloat("_AlphaCutoff", 0.4f); mat.EnableKeyword("_ALPHATEST_ON");
                        mat.SetFloat("_DoubleSidedEnable", 1f);
                    }
                }
                else
                {
                    mat.SetTexture("_MainTex", baseColor);
                    if (normal) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); }
                    if (metalSmooth) { mat.SetTexture("_MetallicGlossMap", metalSmooth); mat.EnableKeyword("_METALLICGLOSSMAP"); mat.SetFloat("_GlossMapScale", 1f); }
                    else mat.SetFloat("_Glossiness", smooth);
                    if (occlusion) mat.SetTexture("_OcclusionMap", occlusion);
                    if (cutout)
                    {
                        mat.SetFloat("_Mode", 1f); mat.SetFloat("_Cutoff", 0.4f); mat.EnableKeyword("_ALPHATEST_ON");
                        mat.SetOverrideTag("RenderType", "TransparentCutout");
                        mat.renderQueue = (int)RenderQueue.AlphaTest;
                    }
                }
                EditorUtility.SetDirty(mat);
                mi.AddRemap(id, mat);
                changed = true;
            }
            AssetDatabase.SaveAssets();
            if (changed) mi.SaveAndReimport();
        }

        static AnimatorController BuildController(string fbxPath, bool force)
        {
            string dir = Dir(fbxPath);
            string charName = Path.GetFileNameWithoutExtension(fbxPath);
            string path = $"{dir}/{charName}.controller";
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ac != null && !force) return ac;
            if (ac != null) AssetDatabase.DeleteAsset(path);

            var clips = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).GroupBy(c => c.name).ToDictionary(g => g.Key, g => g.First());
            ac = AnimatorController.CreateAnimatorControllerAtPath(path);
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.AddParameter("Talking", AnimatorControllerParameterType.Bool);
            ac.AddParameter("Seated", AnimatorControllerParameterType.Bool);
            ac.AddParameter("Activity", AnimatorControllerParameterType.Int);

            var layers = ac.layers;
            layers[0].iKPass = true;      // lets LabSeatController place hands on the real keyboard/mouse
            ac.layers = layers;
            var sm = ac.layers[0].stateMachine;

            AnimatorState S(string clip, float x, float y)
            {
                var st = sm.AddState(clip, new Vector3(x, y, 0));
                if (clips.TryGetValue(clip, out var c)) st.motion = c;
                else Debug.LogWarning($"[ComputerLab] clip {clip} not found in {fbxPath}");
                return st;
            }
            AnimatorStateTransition T(AnimatorState a, AnimatorState b, float dur, float exitTime = -1f)
            {
                var t = a.AddTransition(b);
                t.hasExitTime = exitTime >= 0f;
                if (exitTime >= 0f) t.exitTime = exitTime;
                t.duration = dur;
                t.hasFixedDuration = true;
                return t;
            }

            var idle = S("Idle", 250, 0);
            var walk = S("Walk", 250, -120);
            var talk = S("Talk", 520, -120);
            var sitDown = S("Sit_Down", 520, 80);
            var sitIdle = S("Sit_Idle", 520, 200);
            var typing = S("Typing", 800, 120);
            var mouse = S("Mouse", 800, 220);
            var talkSeated = S("Talk_Seated", 800, 320);
            var standUp = S("Stand_Up", 250, 200);
            sm.defaultState = idle;

            T(idle, walk, 0.2f).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            T(walk, idle, 0.25f).AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            T(idle, talk, 0.3f).AddCondition(AnimatorConditionMode.If, 0, "Talking");
            T(talk, idle, 0.3f).AddCondition(AnimatorConditionMode.IfNot, 0, "Talking");
            T(talk, walk, 0.25f).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            T(idle, sitDown, 0.2f).AddCondition(AnimatorConditionMode.If, 0, "Seated");
            T(talk, sitDown, 0.25f).AddCondition(AnimatorConditionMode.If, 0, "Seated");
            T(sitDown, sitIdle, 0.2f, 0.92f);
            T(standUp, idle, 0.25f, 0.92f);

            var seatedStates = new[] { sitIdle, typing, mouse, talkSeated };   // index == Activity value
            for (int i = 0; i < seatedStates.Length; i++)
            {
                var from = seatedStates[i];
                for (int j = 0; j < seatedStates.Length; j++)
                {
                    if (i == j) continue;
                    var t = T(from, seatedStates[j], 0.35f);
                    t.AddCondition(AnimatorConditionMode.Equals, j, "Activity");
                    t.AddCondition(AnimatorConditionMode.If, 0, "Seated");
                }
                T(from, standUp, 0.3f).AddCondition(AnimatorConditionMode.IfNot, 0, "Seated");
            }
            AssetDatabase.SaveAssets();
            return ac;
        }

        static void BuildPrefab(string fbxPath, AnimatorController controller, bool force)
        {
            string dir = Dir(fbxPath);
            string charName = Path.GetFileNameWithoutExtension(fbxPath);
            string prefabPath = $"{dir}/{charName}.prefab";
            if (!force && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (model == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            go.name = charName;
            var anim = go.GetComponent<Animator>();
            if (anim == null) anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = true;
            anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                smr.updateWhenOffscreen = false;
            if (go.GetComponent<LabCharacterFace>() == null) go.AddComponent<LabCharacterFace>();
            if (go.GetComponent<LabSeatController>() == null) go.AddComponent<LabSeatController>();
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[ComputerLab] Created {prefabPath}");
        }
    }
}
