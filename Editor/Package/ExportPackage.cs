using System.IO;
using UnityEditor;
using UnityEngine;

namespace Cuboid.UnityPlugin.Editor
{
    public static class ExportPackage
    {
        // Stores the last selected directory path in EditorPrefs so that it persists between editor reloads. 
        private const string k_LastSelectedDirectoryPathKey = "ExportPackage_lastSelectedDirectoryPath";
        private static string _lastSelectedDirectoryPath = null;
        public static string LastSelectedDirectoryPath
        {
            get
            {
                if (_lastSelectedDirectoryPath == null)
                {
                    _lastSelectedDirectoryPath = EditorPrefs.GetString(k_LastSelectedDirectoryPathKey, "");
                }

                return _lastSelectedDirectoryPath;
            }
            private set
            {
                _lastSelectedDirectoryPath = value;
                EditorPrefs.SetString(k_LastSelectedDirectoryPathKey, _lastSelectedDirectoryPath);
            }
        }

        [MenuItem("Cuboid/Utils/Export Package")]
        public static void Export()
        {
            // returns GUIDs
            string[] assets = AssetDatabase.FindAssets("", new string[] { Package.k_PackagePath });

            string[] assetPaths = new string[assets.Length];
            for (int i = 0; i < assets.Length; i++)
            {
                assetPaths[i] = AssetDatabase.GUIDToAssetPath(assets[i]);
            }

            string directoryPath = EditorUtility.OpenFolderPanel("Select the folder where the .unitypackage will be exported to", LastSelectedDirectoryPath, LastSelectedDirectoryPath);
            
            if (directoryPath == "")
            {
                // this means we should stop the operation because the user has canceled
                return;
            }

            AssetDatabase.ExportPackage(assetPaths, Path.Combine(directoryPath, "cuboid-unity-plugin.unitypackage"));
        }
    }
}
