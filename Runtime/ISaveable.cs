using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Celezt.SaveSystem
{
    public interface ISaveable
    {
        public Guid Guid { get; }
    }
}
