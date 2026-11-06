// Unity (URP) - one-click material setup for TeacherStation.fbx
// Menu: Tools > Lab Assets > Setup TeacherStation Materials
// Place this file in an "Editor" folder. Keep the FBX and the Textures folder together.
using System.IO;
using UnityEditor;
using UnityEngine;

public static class TeacherStationMaterialSetup
{
    struct Def { public string name; public Color color; public float metal, smooth; public string albedo, normal; }

    static readonly Def[] Defs =
    {
        new Def{name="M_Wood_Light",     color=new Color(0.80f,0.64f,0.45f), metal=0f,  smooth=0.35f, albedo="Wood_Light_Albedo.png", normal="Wood_Light_Normal.png"},
        new Def{name="M_Metal_Charcoal", color=Color.white,                  metal=0.8f,smooth=0.45f, albedo="Metal_Charcoal_Albedo.png"},
        new Def{name="M_Metal_Steel",    color=Color.white,                  metal=1f,  smooth=0.60f, albedo="Metal_Steel_Brushed_Albedo.png"},
        new Def{name="M_Laminate_Grey",  color=Color.white,                  metal=0f,  smooth=0.40f, albedo="Laminate_Grey_Albedo.png"},
        new Def{name="M_Plastic_White",  color=new Color(0.92f,0.92f,0.90f), metal=0f,  smooth=0.50f},
        new Def{name="M_Plastic_Black",  color=new Color(0.03f,0.03f,0.03f), metal=0f,  smooth=0.35f},
        new Def{name="M_Lens_Glass",     color=new Color(0.02f,0.03f,0.05f), metal=0.2f,smooth=0.95f},
        new Def{name="M_Board_Surface",  color=new Color(0.96f,0.97f,0.97f), metal=0f,  smooth=0.55f},
        new Def{name="M_Board_Frame",    color=new Color(0.05f,0.05f,0.055f),metal=0f,  smooth=0.40f},
    };

    [MenuItem("Tools/Lab Assets/Setup TeacherStation Materials")]
    public static void Run()
    {
        string[] guids = AssetDatabase.FindAssets("TeacherStation t:Model");
        if (guids.Length == 0) { Debug.LogError("TeacherStation.fbx not found in project."); return; }
        string fbxPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        string dir = Path.GetDirectoryName(fbxPath).Replace('\\', '/');
        string matDir = dir + "/Materials";
        if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder(dir, "Materials");

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) { Debug.LogError("URP Lit shader not found - is URP installed/active?"); return; }

        var importer = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
        importer.globalScale = 1f;            // FBX is authored in cm; Unity applies 0.01 file scale => metres
        importer.useFileScale = true;
        importer.importNormals = ModelImporterNormals.Import;
        importer.generateSecondaryUV = true;  // lightmap UVs for static baking
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.SaveAndReimport();

        foreach (var d in Defs)
        {
            string p = $"{matDir}/{d.name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, p); }
            m.shader = shader;
            m.SetColor("_BaseColor", d.color);
            m.SetFloat("_Metallic", d.metal);
            m.SetFloat("_Smoothness", d.smooth);
            if (!string.IsNullOrEmpty(d.albedo))
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/Textures/{d.albedo}"));
            if (!string.IsNullOrEmpty(d.normal))
            {
                string np = $"{dir}/Textures/{d.normal}";
                var ti = (TextureImporter)AssetImporter.GetAtPath(np);
                if (ti != null && ti.textureType != TextureImporterType.NormalMap)
                { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
                m.EnableKeyword("_NORMALMAP");
                m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(np));
            }
            if (d.name == "M_Lens_Glass") m.SetFloat("_Smoothness", 0.95f);
            EditorUtility.SetDirty(m);
        }
        AssetDatabase.SaveAssets();

        // Remap FBX material slots -> project materials (by name)
        importer = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        foreach (var d in Defs)
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), d.name),
                              AssetDatabase.LoadAssetAtPath<Material>($"{matDir}/{d.name}.mat"));
        importer.SaveAndReimport();
        Debug.Log("TeacherStation materials created and assigned.");
    }
}
