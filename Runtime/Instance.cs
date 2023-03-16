using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Celezt.SaveSystem
{
    [Serializable]
    internal struct Instance
    {
        public Guid InstanceGuid;
        public AssetReference AssetReference;
        public int SceneIndex;
    }
}
