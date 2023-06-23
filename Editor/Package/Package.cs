using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.UnityPlugin.Editor
{
    public static class Package
    {
        public const string k_PackagePath = "Packages/com.cuboid.unity-plugin";

        public static string GetPath(string path)
        {
            return Path.Combine(k_PackagePath, path);
        }
    }
}
