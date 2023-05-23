using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cuboid.UnityPlugin
{
    /// <summary>
    /// The <see cref="ThumbnailProvider"/> allows the <see cref="AssetCollectionsWindow "/> to call
    /// methods to retrieve the rendered thumbnail.
    ///
    /// The <see cref="ThumbnailProvider"/> will return them.
    ///
    /// They will be serialized in the project 
    /// </summary>
    public static class ThumbnailProvider
    {
        /// <summary>
        /// Cache directory relative to the Assets directory of the project. 
        /// </summary>
        private const string k_ThumbnailCacheDirectory = "Assets/Plugins/Cuboid/Cache/Thumbnails";

        //private static Dictionary<string, Texture> _

        /// <summary>
        /// Empties the cache, so that the 
        /// </summary>
        private static void EmptyCache()
        {

        }

        private static void EnsureThumbnailCacheDirectoryExists()
        {
            if (!Directory.Exists(k_ThumbnailCacheDirectory))
            {
                Directory.CreateDirectory(k_ThumbnailCacheDirectory);
                AssetDatabase.Refresh();
                DefaultAsset folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(k_ThumbnailCacheDirectory);
                folderAsset.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameObject"></param>
        public static Texture GetThumbnail(GameObject gameObject)
        {
            EnsureThumbnailCacheDirectoryExists();

            // get the directory

            ThumbnailRenderer.BackgroundColor = Color.clear;
            ThumbnailRenderer.UseLocalBounds = true;
            ThumbnailRenderer.OrthographicMode = true;
            return ThumbnailRenderer.GenerateModelPreview(gameObject, 512, 512);
        }
    }
}
