using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.UnityPlugin
{
    public static class Constants
    {
        public const int k_ThumbnailSize = 256;

        public const string k_ThumbnailEntryName = "thumbnail.png";

        public const string k_DocumentFileExtension = ".cuboid"; // with dot
        public const string k_DocumentEntryName = "document.json";

        public const string k_AssetCollectionFileExtension = ".zip";
        public const string k_AssetCollectionEntryName = "collection.json";
        public const string k_AssetCollectionAssetBundleName = "bundle";

        public const string k_AssetCollectionSpriteAtlasName = "thumbnails.spriteatlas";
    }
}
