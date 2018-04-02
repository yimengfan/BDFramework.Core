using UnityEngine;
using UnityEditor;
using System.IO;

public class Editor_2ChangeTextureImporter : EditorWindow
{
    
    // ----------------------------------------------------------------------------  
    TextureImporterType textureType = TextureImporterType.Sprite;
    TextureImporterFormat textureFormat = TextureImporterFormat.Automatic;
    TextureImporterCompression textureCompression = TextureImporterCompression.Uncompressed;

    bool ifAllowsAlphaSplitting = true;
    bool ifMipmapEnabled = false;
   
    TextureImporterPlatformSettings changetexture = new TextureImporterPlatformSettings();
    int num;
     void OnGUI()
    {
        string datapath = Application.dataPath;
        string path = datapath + "\\Art";
        string path_1 = path.Replace("Assets\\Art", "");
        //文件路径
        GUILayout.Label(string.Format("当前资源路径:{0}", path));
        EditorGUILayout.Space();
        GUILayout.Label(string.Format("文件相对路径:{0}", path_1));
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "1.注意确认资源路径是否正确         2.文件相对路径用于查找图片信息位置,请确认是否正确后再进行图片处理", MessageType.Warning);
        EditorGUILayout.Space();

        //平台
        string BuildTarget = GetBuildTarget(changetexture);
        
        //
        textureType = (TextureImporterType)EditorGUILayout.EnumPopup("图片类型:", textureType);
        textureFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("图片格式:", textureFormat);
        textureCompression = (TextureImporterCompression)EditorGUILayout.EnumPopup("压缩质量:", textureCompression);

        EditorGUILayout.Space();
        ifAllowsAlphaSplitting = EditorGUILayout.Toggle("是否允许透明分离:", ifAllowsAlphaSplitting);
        ifMipmapEnabled = EditorGUILayout.Toggle("是否允许Mipmap:", ifMipmapEnabled);

        EditorGUILayout.Space();
        GUILayout.Label(string.Format("当前平台:{0}", BuildTarget));

        EditorGUILayout.Space();

        if (GUILayout.Button("开始压缩"))
        {
            string keyword = "*.BMP|*.JPG|*.GIF|*.PNG";
            var keywords = keyword.Split('|');
      
            for (int i = 0; i < keywords.Length; i++)
            {
                var files = Directory.GetFiles(@path, keywords[i], SearchOption.AllDirectories);
                foreach (var item in files)
                {
                    var newpath = item.Replace(path_1, "");
                    var newitempath = newpath.Replace("\\", "/");
     
                    TextureImporter textureImporter = AssetImporter.GetAtPath(newitempath) as TextureImporter;
                    //类型
                    textureImporter.textureType = textureType;
                    //是否从写
                    changetexture.overridden = true;
                    //压缩
                    changetexture.textureCompression = textureCompression;
                    //透明分离值
                    changetexture.allowsAlphaSplitting = ifAllowsAlphaSplitting;
                    //最大尺寸
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(newitempath);
                    changetexture.maxTextureSize = GetMaxSize(texture);
                    //格式
                    changetexture.format = textureFormat;

                    textureImporter.SetPlatformTextureSettings(changetexture);
             
                    AssetDatabase.ImportAsset(newitempath);
                }
            }
        }
    }
    //
    string GetBuildTarget(TextureImporterPlatformSettings importer)
    {
#if UNITY_ANDROID
        importer.name = "Android";
#endif

#if UNITY_IPHONE 
        importer.name = "iPhone";
#endif

#if UNITY_STANDALONE_WIN        
        importer.name = "Standalone";
#endif
        return importer.name;
    }
    //计算
    int GetMaxSize(Texture2D texture)
    {
        float width = texture.width;
        float height = texture.height;

        if (width > height)
        {
            num = 0;
            while (width / 2 >= 1)
            {
                width = width / 2;
                num++;
            }
        }
        else
        {
            num = 0;
            while (height / 2 >= 1)
            {
                height = height / 2;
                num++;
            }
        }
        var maxsize = Mathf.Pow(2, num);
        return (int)maxsize;
    }

}

