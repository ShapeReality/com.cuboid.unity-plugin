using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor.U2D;
using Newtonsoft.Json;

namespace Cuboid.UnityPlugin.Editor
{
    /// <summary>
    /// Builds a RealityAssetCollection to a zip that can be
    /// put on the AR / VR headset. 
    /// </summary>
    public static class BuildUtils
    {
        private const string k_TemporaryThumbnailSpritesDirectory = "__cuboid_temp";
        private const string k_SpriteAtlasFileName = "__spriteatlas.spriteatlas";

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

            // Step 3: Create asset bundle build that contains asset names and addressable names,
            // these can then be used to name the thumbnails. 
            AssetBundleBuild assetBundleBuild = GetAssetBundlebuild(collection);

            // Step 4: Make SpriteAtlas
            string thumbnailsFolderGuid = AssetDatabase.CreateFolder("Assets", k_TemporaryThumbnailSpritesDirectory);
            string thumbnailsFolder = AssetDatabase.GUIDToAssetPath(thumbnailsFolderGuid);

            SpriteAtlas spriteAtlas = new SpriteAtlas();
            spriteAtlas.SetPackingSettings(new SpriteAtlasPackingSettings()
            {
                enableRotation = false,
                enableTightPacking = false
            });
            string spriteAtlasPath = Path.Combine(thumbnailsFolder, k_SpriteAtlasFileName);
            AssetDatabase.CreateAsset(spriteAtlas, spriteAtlasPath);

            UnityEngine.Object[] sprites = new UnityEngine.Object[assets.Count];
            for (int i = 0; i < assets.Count; i++)
            {
                GameObject asset = assets[i];

                // get the thumbnail
                Texture2D thumbnailTexture = ThumbnailProvider.GetThumbnail(asset);

                // make pngs and add them to the AssetDatabase
                byte[] wee = thumbnailTexture.GetRawTextureData();
                byte[] bytes = ImageConversion.EncodeArrayToPNG(
                    wee, thumbnailTexture.graphicsFormat, (uint)thumbnailTexture.width, (uint)thumbnailTexture.height);

                string thumbnailName = assetBundleBuild.addressableNames[i];
                thumbnailName = thumbnailName.Replace(Path.DirectorySeparatorChar, '_');
                string thumbnailPath = Path.Combine(thumbnailsFolder, thumbnailName + ".png");
                File.WriteAllBytes(thumbnailPath, bytes);

                AssetDatabase.ImportAsset(thumbnailPath);
                TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(thumbnailPath);
                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.alphaIsTransparency = true;
                textureImporter.alphaSource = TextureImporterAlphaSource.FromInput;

                EditorUtility.SetDirty(textureImporter);
                textureImporter.SaveAndReimport();

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(thumbnailPath);
                sprites[i] = sprite;
            }

            spriteAtlas.Add(sprites);

            AssetDatabase.SaveAssets();

            // Step 5: Add the Sprite Atlas to the asset bundle build
            assetBundleBuild.Add(spriteAtlasPath, Constants.k_AssetCollectionSpriteAtlasName);

            // Step 4: Build the asset bundle to a temporary directory
            string tempPath = FileUtil.GetUniqueTempPathInProject();
            Directory.CreateDirectory(tempPath);

            string assetBundlePath = Path.Combine(tempPath, Constants.k_AssetCollectionAssetBundleName);
            Directory.CreateDirectory(assetBundlePath);

            BuildAssetBundleOptions options = BuildAssetBundleOptions.None;
            BuildTarget buildTarget = BuildTarget.Android;
            BuildPipeline.BuildAssetBundles(assetBundlePath, new AssetBundleBuild[] { assetBundleBuild },options, buildTarget);

            // create the serialized collection
            SerializedRealityAssetCollection serializedCollection = new SerializedRealityAssetCollection()
            {
                AddressableNames = assetBundleBuild.addressableNames.ToList(),
                Author = collection.Author,
                CreationDate = DateTime.Now
            };

            string json = JsonConvert.SerializeObject(serializedCollection, Formatting.Indented, SerializationSettings.RealityAssetCollectionJsonSerializationSettings);
            string jsonPath = Path.Combine(tempPath, Constants.k_AssetCollectionEntryName);
            File.WriteAllText(jsonPath, json);

            File.WriteAllText(targetPath, "Dingetjes");

            // Finally: Remove all thumbnail sprites from assets

            // Clear up temporary files
        }

        private static void Add(this ref AssetBundleBuild bundle, string path, string addressableName = null)
        {
            string[] addressableNames = bundle.addressableNames;
            string[] assetNames = bundle.assetNames;

            Array.Resize(ref addressableNames, addressableNames.Length + 1);
            Array.Resize(ref assetNames, assetNames.Length + 1);

            int index = addressableNames.Length - 1;

            assetNames[index] = path;
            addressableNames[index] = addressableName != null ? addressableName : path;

            bundle.assetNames = assetNames;
            bundle.addressableNames = addressableNames;
        }

        private static string GetAddressableName(string assetName)
        {
            return Path.GetFileNameWithoutExtension(assetName);
        }

        /// <summary>
        /// Returns an asset bundle build object with unique addressable names that
        /// are as short as possible, but include folder names when there are multiple assets
        /// with the same name in different folders. 
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        private static AssetBundleBuild GetAssetBundlebuild(RealityAssetCollection collection)
        {
            if (collection == null || collection.Assets == null || collection.Assets.Count == 0)
            {
                throw new System.Exception("Invalid collection");
            }

            List<GameObject> assets = collection.Assets;

            string[] addressableNames = new string[assets.Count];
            string[] assetNames = new string[assets.Count];

            Dictionary<string, HashSet<int>> collisions = new();

            for (int i = 0; i < assets.Count; i++)
            {
                GameObject asset = assets[i];
                string path = AssetDatabase.GetAssetPath(asset);
                assetNames[i] = path;

                // make sure the name doesn't already exist (add folder distinction)
                string addressableName = GetAddressableName(path);
                
                // check all previous added names
                for (int j = 0; j < i; j++)
                {
                    // this means a naming collision, we should specify both of them with the folder name
                    if (GetAddressableName(assetNames[j]) == addressableName)
                    {
                        collisions.TryAdd(addressableName, new());
                        collisions[addressableName].Add(j);
                        collisions[addressableName].Add(i);
                    }
                }
                addressableNames[i] = addressableName;
            };

            foreach (KeyValuePair<string, HashSet<int>> collision in collisions)
            {
                Debug.Log(collision.Key);
                HashSet<int> collisionIndices = collision.Value;

                if (collisionIndices.Count < 2) { continue; } // collisions can only occur when two or more collisions were found

                string[] paths = new string[collisionIndices.Count];
                int k = 0;
                foreach (int collisionIndex in collisionIndices)
                {
                    paths[k] = assetNames[collisionIndex];
                    k++;
                }

                // get common prefix
                string commonPrefix = GetCommonPrefix(paths);

                // remove the trailing
                int directorySeparatorIndex = commonPrefix.LastIndexOf(Path.DirectorySeparatorChar);
                if (directorySeparatorIndex != -1)
                {
                    commonPrefix = commonPrefix.Substring(0, directorySeparatorIndex);
                }
                Debug.Log(commonPrefix);

                // now, remove this prefix from the asset names and set that as the addressable names
                foreach (int collisionIndex in collisionIndices)
                {
                    string path = assetNames[collisionIndex];
                    path = path.Substring(commonPrefix.Length);
                    path = path.TrimStart(Path.DirectorySeparatorChar);
                    path = path.Substring(0, path.LastIndexOf('.'));

                    addressableNames[collisionIndex] = path;
                }
            }

            for (int i = 0; i < addressableNames.Length; i++)
            {
                Debug.Log(addressableNames[i]);
            }

            return new AssetBundleBuild()
            {
                assetBundleName = collection.name,
                assetNames = assetNames,
                addressableNames = addressableNames
            };
        }

        private static string GetCommonPrefix(string[] strings)
        {            
            if (strings == null || strings.Length == 0) { return ""; }
            if (strings.Length == 1) { return strings[0]; }

            // first get the minLength
            int minLength = int.MaxValue;
            for (int i = 0; i < strings.Length; i++)
            {
                int length = strings[i].Length;
                if (length < minLength)
                {
                    minLength = length;
                }
            }

            // loop through all of the characters
            for (int j = 0; j < minLength; j++)
            {
                char c = strings[0][j];
                // loop through each string
                for (int k = 1; k < strings.Length; k++)
                {
                    char newChar = strings[k][j];
                    if (newChar != c)
                    {
                        return strings[0].Substring(0, j);
                    }
                }
            }

            return strings[0].Substring(0, minLength);
        }

        private static List<GameObject> FilterAssets(List<GameObject> assets)
        {
            List<GameObject> filtered = assets.ToHashSet().ToList(); // removes duplicates
            filtered.RemoveAll(asset => asset == null); // removes all null assets
            return filtered;
        }
    }
}
