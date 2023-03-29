using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Celezt.SaveSystem
{
    /// <summary>
    /// Identifier used to group entries together to be saved under a common key.
    /// </summary>
    public interface IIdentifiable
    {
        public Guid Guid { get; }
	}
}
