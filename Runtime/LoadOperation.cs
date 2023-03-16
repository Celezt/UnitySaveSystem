using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Celezt.SaveSystem
{
    public readonly struct LoadOperation
    {
        public readonly LoadState State;
        public readonly object Result;

        internal LoadOperation(LoadState state, object result)
        {
            State = state;
            Result = result;
        }

        public enum LoadState
        {
            /// <summary>
            /// If loaded from load game callback.
            /// </summary>
            LoadGame,
            /// <summary>
            /// If loaded from previous save callback. 
            /// </summary>
            LoadPreviousSave,
        }

        public T GetResult<T>() => (T)Result;
    }
}
