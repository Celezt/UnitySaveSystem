using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Celezt.SaveSystem
{
    [Serializable]
    internal readonly struct Instance
    {
        public readonly Guid InstanceGuid;
        public readonly AssetReference AssetReference;
        public readonly int SceneIndex;


        public Instance(Guid instanceGuid, AssetReference assetReference, int sceneIndex)
        {
            InstanceGuid = instanceGuid;
            AssetReference = assetReference;
            SceneIndex = sceneIndex;
        }
    }
}
