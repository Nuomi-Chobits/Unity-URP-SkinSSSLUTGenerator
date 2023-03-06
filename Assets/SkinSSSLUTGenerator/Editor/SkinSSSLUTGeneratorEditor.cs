using UnityEngine;
using UnityEditor;

public class SkinSSSLUTGeneratorEditor : EditorWindow
{
    [MenuItem("Tools/SkinSSSLUTGenerator", false, 1000)]
    private static void MenuGeneratorLUT()
    {
        GetWindow<SkinSSSLUTGeneratorEditor>("Skin SSS LUT Generator");
    }

    private bool useTonemap = true;
    private ComputeShader IntegratorShader = null;
    private RenderTexture SSSLUT = null;
    private int Resolution;
    public enum IntegralInterval
    {
        _Fully = 2,
        _Half = 1,
    }

    public enum TextureSize
    {
        _64x64 = 64,
        _128x128 = 128,
        _256x256 = 256,
        _512x512 = 512,
    }
    IntegralInterval mIntegralInterval = IntegralInterval._Half;
    TextureSize mTextureSize = TextureSize._64x64;
    private void OnEnable()
    {
        IntegratorShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/SkinSSSLUTGenerator/Shaders/PreIntegratedSkinLUT.compute");
    }
    private void OnDestroy()
    {
        if (SSSLUT != null)
        {
            RenderTexture.ReleaseTemporary(SSSLUT);
        }
    }
    void OnGUI()
    {
        GUILayout.Label("Base Settings", EditorStyles.boldLabel);

        mIntegralInterval = (IntegralInterval)EditorGUILayout.EnumPopup("Integral Interval",mIntegralInterval);
        mTextureSize = (TextureSize)EditorGUILayout.EnumPopup("Texture Size",mTextureSize);
        useTonemap = EditorGUILayout.Toggle("Use Tonemap", useTonemap);
        
        EditorGUILayout.BeginHorizontal();

        if(GUILayout.Button("Bake") && IntegratorShader != null)
        {
            if (SSSLUT != null)
            {
                RenderTexture.ReleaseTemporary(SSSLUT);
            }
            Resolution = (int)mTextureSize * 8;
            SSSLUT = RenderTexture.GetTemporary(Resolution, Resolution, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default);
            SSSLUT.enableRandomWrite = true;
            if (useTonemap)
            {
                IntegratorShader.EnableKeyword("USE_TONEMAP");
            }
            else
            {
                IntegratorShader.DisableKeyword("USE_TONEMAP");
            }
            IntegratorShader.SetTexture(0, "_SSSLUT", SSSLUT);
            IntegratorShader.SetFloat("_Resoultion", (float)Resolution);
            IntegratorShader.SetFloat("_IntegralInterval", (float)mIntegralInterval);
            IntegratorShader.Dispatch(0, (int)mTextureSize, (int)mTextureSize, 1);
        }
        if (GUILayout.Button("Save(make sure baked Texture first)") && SSSLUT != null)
        {   
            var rt = RenderTexture.GetTemporary(Resolution, Resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(SSSLUT, rt);

            RenderTexture.active = rt;
            var tex = new Texture2D(SSSLUT.width, SSSLUT.height, TextureFormat.RGB24, false, true);
            tex.ReadPixels(new Rect(0, 0, SSSLUT.width, SSSLUT.height), 0, 0);
            RenderTexture.active = null;

            RenderTexture.ReleaseTemporary(rt);

            var path = "Assets/SkinSSSLUTGenerator/Resources/SkinSSSLUT.TGA";
            System.IO.File.WriteAllBytes(path, tex.EncodeToTGA());
            AssetDatabase.ImportAsset(path);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.sRGBTexture = false;
            importer.maxTextureSize = (int)mTextureSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
        var rect = EditorGUILayout.GetControlRect(true, position.height - 110);
        if (SSSLUT != null)
        {
            EditorGUI.DrawPreviewTexture(rect, SSSLUT);
        }
        else
        {
            EditorGUI.DrawRect(rect, Color.black);
        }
    }
}
