using UnityEditor;

using System;
using System.IO;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
    [System.Serializable]
    [CustomAssetGenerator("Texture Scaler", "v1.0", 1)]
    public class TextureScaler : IAssetGenerator {

        public enum TextureOutputType
        {
            PNG,
            JPG,
            EXR,
        }

        public enum TextureFilterType
        {
            Point,
            Bilinear,
        }


        [SerializeField] private TextureOutputType m_outputType;
        [SerializeField] private TextureFilterType m_filterType;
        [SerializeField] private float m_scale = 1.0f;
        [SerializeField] private int m_jpgQuality = 100;
        [SerializeField] private Texture2D.EXRFlags m_exrFlags = Texture2D.EXRFlags.CompressZIP;

        public void OnValidate () {
        }

        public string GetAssetExtension (AssetReference asset) {
            switch (m_outputType) {
            case TextureOutputType.PNG:
                return ".png";
            case TextureOutputType.JPG:
                return ".jpg";
            case TextureOutputType.EXR:
                return ".exr";
            }
            return "";
        }

        public Type GetAssetType(AssetReference asset) {
            return typeof(TextureImporter);
        }

        public bool CanGenerateAsset (AssetReference asset) {

            if (asset.importerType != typeof(TextureImporter)) {
                throw new NodeException ("Texture Scaler needs texture for source asset.",
                    $"Remove {asset.fileNameAndExtension} from input.");
            }

            var importer = AssetImporter.GetAtPath (asset.importFrom) as TextureImporter;

            return importer != null;
        }

        public bool GenerateAsset (AssetReference asset, string generateAssetPath) {

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(asset.importFrom);
            if (tex == null) {
                return false;
            }

            Texture2D output = null;

            switch (m_filterType) {
            case TextureFilterType.Bilinear:
                output = CreateScaledTextureBL (tex);
                break;
            case TextureFilterType.Point:
                output = CreateScaledTexturePT (tex);
                break;
            }

            Resources.UnloadAsset(tex);

            // Encode texture into the EXR
            byte[] bytes = null;

            switch (m_outputType) {
            case TextureOutputType.JPG:
                bytes = output.EncodeToJPG();
                break;
            case TextureOutputType.PNG:
                bytes = output.EncodeToPNG();
                break;
            case TextureOutputType.EXR:
                bytes = output.EncodeToEXR(m_exrFlags);
                break;
            }

            string fullPath = FileUtility.PathCombine (Directory.GetParent(Application.dataPath).ToString(), generateAssetPath);
            File.WriteAllBytes(fullPath, bytes);

            UnityEngine.Object.DestroyImmediate(output);

            return true;
        }

        private Texture2D CreateDstTexture(Texture2D src) {
            int width  = (int)(src.width * m_scale);
            int height = (int)(src.height * m_scale);
            if (m_outputType == TextureOutputType.EXR) {
                return new Texture2D (width, height, TextureFormat.RGBAHalf, false);
            } else {
                return new Texture2D (width, height);
            }
        }

        private Texture2D CreateScaledTexturePT(Texture2D src){
            var dst = CreateDstTexture (src);
            var dstPix = new Color[dst.width * dst.height];
            int y = 0;
            while (y < dst.height) {
                int x = 0;
                while (x < dst.width) {
                    int srcX = Mathf.FloorToInt(x / m_scale);
                    int srcY = Mathf.FloorToInt(y / m_scale);
                    dstPix[y * dst.width + x] = src.GetPixel((int)srcX, (int)srcY);
                    ++x;
                }
                ++y;
            }
            dst.SetPixels(dstPix);
            dst.Apply();

            return dst;
        }

        private Texture2D CreateScaledTextureBL(Texture2D src){
            var dst = CreateDstTexture (src);
            var dstPix = new Color[dst.width * dst.height];
            int y = 0;
            while (y < dst.height) {
                int x = 0;
                while (x < dst.width) {
                    float xFrac = x * 1.0F / (dst.width - 1);
                    float yFrac = y * 1.0F / (dst.height - 1);
                    dstPix[y * dst.width + x] = src.GetPixelBilinear(xFrac, yFrac);
                    ++x;
                }
                ++y;
            }
            dst.SetPixels(dstPix);
            dst.Apply();

            return dst;
        }

        public void OnInspectorGUI (Action onValueChanged) {

            var newOutputType = (TextureOutputType)EditorGUILayout.EnumPopup ("Output Format", m_outputType);
            if (newOutputType != m_outputType) {
                m_outputType = newOutputType;
                onValueChanged();
            }

            if (m_outputType == TextureOutputType.JPG) {
                var newQuality = EditorGUILayout.IntSlider ("JPG Quality", m_jpgQuality, 1, 100);
                if (newQuality != m_jpgQuality) {
                    m_jpgQuality = newQuality;
                    onValueChanged ();
                }
            }

            if (m_outputType == TextureOutputType.EXR) {
                var exrOpt = (Texture2D.EXRFlags)EditorGUILayout.EnumPopup ("EXR Option", m_exrFlags);
                if (exrOpt != m_exrFlags) {
                    m_exrFlags = exrOpt;
                    onValueChanged ();
                }
            }

            var newScaleType = (TextureFilterType)EditorGUILayout.EnumPopup ("Filter Mode", m_filterType);
            if (newScaleType != m_filterType) {
                m_filterType = newScaleType;
                onValueChanged();
            }

            var newScale = EditorGUILayout.Slider ("Scale(%)", m_scale * 100f, 1.0f, 100.0f);
            newScale = newScale / 100f;
            if (m_scale != newScale) {
                m_scale = newScale;
                onValueChanged();
            }
        }

    }
}