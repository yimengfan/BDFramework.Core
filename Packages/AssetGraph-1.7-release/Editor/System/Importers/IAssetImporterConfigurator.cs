using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

using Model = UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph
{

    public struct ConfigurationOption
    {
        //TextureImporterOptions
        public bool overwritePackingTag;
        public bool overwriteSpriteSheet;
        public SerializableMultiTargetString customPackingTagTemplate;
        
        //ModelImporterOptions
        public bool overwriteAnimationClipSettings;
        public bool overwriteHumanDescriptions;
    }

    /// <summary>
    /// IImportSettingsConfigurator is an interface to configure import settings for assets.
    /// Subclass of IImportSettingsConfigurator must have CustomImportSettingsConfigurator attribute.
    /// </summary>
    public interface IAssetImporterConfigurator
    {

        void Initialize (ConfigurationOption legacyOption);

        bool IsModified (AssetImporter referenceImporter, AssetImporter importer, BuildTarget target, string group);

        void Configure (AssetImporter referenceImporter, AssetImporter importer, BuildTarget target, string group);

        void OnInspectorGUI (AssetImporter referenceImporter, BuildTargetGroup target, Action onValueChanged);
    }

    /// <summary>
    /// CustomImportSettingsConfigurator attribute is to declare the class is used as a ImportSettingsConfigurator
    /// for given specific type of asset.
    /// </summary>
    [AttributeUsage (AttributeTargets.Class)] 
    public class CustomAssetImporterConfigurator : Attribute
    {

        private Type m_For;
        private string m_guiName;
        private string m_templateFileName;

        public Type For {
            get {
                return m_For;
            }
        }

        public string GUIName {
            get {
                return m_guiName;
            }
        }

        public string TemplateFileName {
            get {
                return m_templateFileName;
            }
        }

        public CustomAssetImporterConfigurator (Type configureFor, string guiName, string templateFileName)
        {
            m_For = configureFor;
            m_guiName = guiName;
            m_templateFileName = templateFileName;
        }
    }

    public class ImporterConfiguratorUtility
    {
        private static Dictionary<Type, Type> s_importerConfiguratorTypeMap;
        private static Dictionary<string, Type> s_importerConfiguratorGuiNameTypeMap;

        public static Dictionary<string, Type> GetImporterConfiguratorGuiNameTypeMap ()
        {
            if (s_importerConfiguratorGuiNameTypeMap == null) {
                var map = new Dictionary<string, Type> ();

                var allConfigurators = new List<Type> ();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    var types = assembly.GetTypes ()
                        .Where (t => t != typeof(IAssetImporterConfigurator))
                        .Where (t => typeof(IAssetImporterConfigurator).IsAssignableFrom (t));
                    allConfigurators.AddRange (types);
                }

                foreach (var type in allConfigurators) {
                    CustomAssetImporterConfigurator attr = 
                        type.GetCustomAttributes (typeof(CustomAssetImporterConfigurator), false).FirstOrDefault () as CustomAssetImporterConfigurator;

                    if (attr != null) {
                        if (!map.ContainsKey (attr.GUIName)) {
                            map [attr.GUIName] = attr.For;
                        } else {
                            LogUtility.Logger.LogWarning (LogUtility.kTag,
                                $"Multiple CustomImporterConfigurator for {attr.For.Name} found. Ignoring {type.Name}");
                        }
                    }
                }
                s_importerConfiguratorGuiNameTypeMap = map;
            }

            return s_importerConfiguratorGuiNameTypeMap;
        }

        private static Dictionary<Type, Type> GetImporterConfiguratorTypeMap ()
        {
            if (s_importerConfiguratorTypeMap == null) {
                var map = new Dictionary<Type, Type> ();

                var allConfigurators = new List<Type> ();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    var types = assembly.GetTypes ()
                        .Where (t => t != typeof(IAssetImporterConfigurator))
                        .Where (t => typeof(IAssetImporterConfigurator).IsAssignableFrom (t));
                    allConfigurators.AddRange (types);
                }

                foreach (var type in allConfigurators) {
                    CustomAssetImporterConfigurator attr = 
                        type.GetCustomAttributes (typeof(CustomAssetImporterConfigurator), false).FirstOrDefault () as CustomAssetImporterConfigurator;

                    if (attr != null) {
                        if (!map.ContainsKey (attr.For)) {
                            map [attr.For] = type;
                        } else {
                            LogUtility.Logger.LogWarning (LogUtility.kTag,
                                $"Multiple CustomImporterConfigurator for {attr.For.Name} found. Ignoring {type.Name}.");
                        }
                    }
                }
                s_importerConfiguratorTypeMap = map;
            }

            return s_importerConfiguratorTypeMap;
        }

        public static Type GetConfiguratorTypeFor (Type importerType)
        {
            var map = GetImporterConfiguratorTypeMap ();
            if (map.ContainsKey (importerType)) {
                return map [importerType];
            }
            return null;
        }

        public static IAssetImporterConfigurator CreateConfigurator (Type importerType) {
            
            var type = GetConfiguratorTypeFor (importerType);
            if (type != null) {
                return (IAssetImporterConfigurator)type.Assembly.CreateInstance (type.FullName);
            }
            return null;
        }

        private static string GetSettingTemplateFileName(Type importerType) {

            Type configuratorType = GetConfiguratorTypeFor (importerType);

            CustomAssetImporterConfigurator attr = 
                configuratorType.GetCustomAttributes (typeof(CustomAssetImporterConfigurator), false).FirstOrDefault () as CustomAssetImporterConfigurator;

            if (attr != null) {
                return attr.TemplateFileName;
            }

            return null;
        }

        public static string FindSettingTemplateFileGUID(Type importerType) {
            var fileName = GetSettingTemplateFileName(importerType);
            if (string.IsNullOrEmpty (fileName)) {
                return null;
            }

            var guids = AssetDatabase.FindAssets(Model.Settings.SETTING_TEMPLATE_DIR_SEARCH_CONDITION);

            foreach(var guid in guids) {
                string templateDirPath = AssetDatabase.GUIDToAssetPath(guid);

                var files = FileUtility.GetAllFilePathsInFolder (templateDirPath);

                var settingFilePath = files.Find (f => System.IO.Path.GetFileName (f) == fileName);
                if (!string.IsNullOrEmpty (settingFilePath)) {
                    return AssetDatabase.AssetPathToGUID (settingFilePath);
                }
            }

            return null;
        }
    }
}
