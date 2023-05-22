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
    public class ThumbnailProvider
    {
        private const string k_ThumbnailCacheDirectory = ".cuboid/cache/thumbnails";

        /// <summary>
        /// Empties the cache, so that the 
        /// </summary>
        private void EmptyCache()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameObject"></param>
        public void GetThumbnail(GameObject gameObject)
        {
            
        }
    }
}
