using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Cuboid.UnityPlugin.Editor.Tests
{
    public class Tests
    {
        [Test]
        public void ContainsSameItemsAs()
        {
            RealityAssetCollection object1 = ScriptableObject.CreateInstance<RealityAssetCollection>(); object1.name = "object1";
            RealityAssetCollection object2 = ScriptableObject.CreateInstance<RealityAssetCollection>(); object2.name = "object2";
            RealityAssetCollection object3 = ScriptableObject.CreateInstance<RealityAssetCollection>(); object3.name = "object3";

            List<RealityAssetCollection> objects = new List<RealityAssetCollection>() { object1, object2 };
            List<RealityAssetCollection> objects2 = new List<RealityAssetCollection>() { object1, object2 };

            Assert.That(Utils.ContainsSameItemsAs(objects, objects2));

            objects2.Add(object3);

            Assert.That(!Utils.ContainsSameItemsAs(objects, objects2));

            ScriptableObject.DestroyImmediate(object1);
            ScriptableObject.DestroyImmediate(object2);
            ScriptableObject.DestroyImmediate(object3);
        }
    }
}
