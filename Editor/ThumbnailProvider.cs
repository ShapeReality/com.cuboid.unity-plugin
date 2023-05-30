using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cuboid.UnityPlugin.Editor
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
        public const int k_ThumbnailSize = 256;

        private static Dictionary<string, Texture2D> _thumbnailsCache = new Dictionary<string, Texture2D>();

        private static Texture2D _emptyTexture = null;
        public static Texture2D EmptyTexture
        {
            get
            {
                if (_emptyTexture == null)
                {
                    _emptyTexture = new Texture2D(k_ThumbnailSize, k_ThumbnailSize);
                }
                return _emptyTexture;
            }
        }

        /// <summary>
        /// Empties the cache
        /// </summary>
        public static void EmptyCache()
        {
            _thumbnailsCache.Clear();
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
        /// Simply uses the thumbnail of the first item in the Asset Collection
        /// </summary>
        public static Texture2D GetCollectionThumbnail(RealityAssetCollection collection)
        {
            return GetThumbnail((collection != null && collection.Assets.Count > 0) ? collection.Assets[0] : null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameObject"></param>
        public static Texture2D GetThumbnail(GameObject gameObject)
        {
            Debug.Assert(_thumbnailsCache != null);

            if (gameObject == null) { return null; }

            // Try to get the thumbnail from the cache
            string assetPath = AssetDatabase.GetAssetPath(gameObject);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (_thumbnailsCache.TryGetValue(guid, out Texture2D value))
            {
                if (value != null)
                {
                    return value;
                }
                _thumbnailsCache.Remove(guid); // remove if value is null
            }

            ThumbnailRenderer.BackgroundColor = Color.clear;
            ThumbnailRenderer.UseLocalBounds = true;
            ThumbnailRenderer.OrthographicMode = true;
            Texture2D thumbnail = ThumbnailRenderer.GenerateModelPreview(gameObject, k_ThumbnailSize, k_ThumbnailSize);

            // Store the thumbnail in the cache
            if (thumbnail != null)
            {
                _thumbnailsCache[guid] = thumbnail;
            }

            return thumbnail;
        }
    }
}
