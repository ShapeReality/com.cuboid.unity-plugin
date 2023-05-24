using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace Cuboid.UnityPlugin.Editor
{
    /// <summary>
    /// Builds a RealityAssetCollection to a zip that can be
    /// put on the AR / VR headset. 
    /// </summary>
    public static class BuildUtils
    {
        // Stores the last selected directory path in EditorPrefs so that it persists between editor reloads. 
        private const string k_LastSelectedDirectoryPathKey = "BuildUtils_lastSelectedDirectoryPath";
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

        private static void OnCancelBuild(RealityAssetCollection collection)
        {
            Debug.LogWarning($"Asset Collection \"{collection.name}\" Build Cancelled");
        }

        public static void Build(RealityAssetCollection collection)
        {
            if (collection == null) { throw new System.Exception("Collection is null"); }
            if (collection.Assets == null || collection.Assets.Count == 0) { throw new System.Exception("Collection does not contain any Assets"); }

            // Step 1: Get the output path

            bool validPath = false;
            string targetPath = null;

            while (!validPath)
            {
                string directoryPath = EditorUtility.OpenFolderPanel("Select the folder where the Asset Collection will be built to", LastSelectedDirectoryPath, LastSelectedDirectoryPath);

                if (directoryPath == "")
                {
                    // this means we should stop the operation because the user has canceled
                    OnCancelBuild(collection); return;
                }

                if (!Directory.Exists(directoryPath))
                {
                    throw new DirectoryNotFoundException();
                }

                LastSelectedDirectoryPath = directoryPath;

                string fileName = collection.name + Constants.k_AssetCollectionFileExtension;
                targetPath = Path.Combine(directoryPath, fileName);

                if (File.Exists(targetPath))
                {
                    // 0 = ok, 1 = cancel, 2 = alt
                    int choice = EditorUtility.DisplayDialogComplex($"An Asset Collection with name {fileName} already exists at {directoryPath}.",
                        "Do you want to overwrite this Asset Collection? This cannot be undone. ",
                        "Overwrite", "Cancel", "Save As");

                    if (choice == 1) { OnCancelBuild(collection); return; }
                    if (choice == 0)
                    {
                        File.Delete(targetPath);
                        validPath = true;
                    }
                }
                else
                {
                    validPath = true;
                }
            }

            Debug.Log($"Writing collection at {targetPath}");

            // Step 2: Filter any duplicate or null objects out of the assets
            List<GameObject> assets = FilterAssets(collection.Assets);

            // Step 2: Make SpriteAtlas
            SpriteAtlas spriteAtlas = new SpriteAtlas();

            foreach (GameObject asset in assets)
            {
                // get the thumbnail
                Texture thumbnailTexture = ThumbnailProvider.GetThumbnail(asset);

            }

            // We build the asset bundle to a temporary directory
            string tempPath = FileUtil.GetUniqueTempPathInProject();
            Directory.CreateDirectory(tempPath);

            string assetBundlePath = tempPath;//Path.Combine(tempPath, Constants.k_AssetCollectionAssetBundleName);

            BuildAssetBundleOptions options = BuildAssetBundleOptions.None;
            BuildTarget buildTarget = BuildTarget.Android;
            AssetBundleBuild assetBundleBuild = new AssetBundleBuild()
            {
                assetBundleName = collection.name,
                assetNames = GetAssetNames(collection.Assets)
            };
            BuildPipeline.BuildAssetBundles(assetBundlePath, new AssetBundleBuild[] { assetBundleBuild },options, buildTarget);

            File.WriteAllText(targetPath, "Dingetjes");
        }

        private static string[] GetAssetNames(List<GameObject> assets)
        {
            string[] assetNames = new string[assets.Count];
            for (int i = 0; i < assets.Count; i++)
            {
                GameObject asset = assets[i];
                string path = AssetDatabase.GetAssetPath(asset);
                assetNames[i] = path;
            }
            return assetNames;
        }

        private static List<GameObject> FilterAssets(List<GameObject> assets)
        {
            List<GameObject> filtered = assets.ToHashSet().ToList(); // removes duplicates
            filtered.RemoveAll(asset => asset == null); // removes all null assets
            return filtered;
        }
    }
}
