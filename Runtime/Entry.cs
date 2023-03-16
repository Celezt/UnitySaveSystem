using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Celezt.SaveSystem
{
    internal struct Entry
    {
        internal List<Action<LoadOperation>> Load => _onLoad;
        internal object Save
        {
            get => InvokeSave();
            set
            {
                if (value is Func<object> saveFunc)
                    _onSave = saveFunc;
                else
                    _onSave = () => value;
            }
        }
        internal object LoadedSave
        {
            get => _loadedSave;
            set => _loadedSave = value;
        }

        private Func<object> _onSave;
        private List<Action<LoadOperation>> _onLoad;
        private object _loadedSave;

        internal Entry(object toSave, Action<LoadOperation> onLoad)
        {
            _onLoad = new();

            _onSave = () => toSave;
            _onLoad.Add(onLoad);
            _loadedSave = null;
        }
        internal Entry(Func<object> onSave, Action<LoadOperation> onLoad)
        {
            _onLoad = new();

            _onSave = onSave;
            _onLoad.Add(onLoad);
            _loadedSave = null;
        }
        internal Entry(Func<object> onSave)
        {
            _onLoad = new();

            _onSave = onSave;
            _loadedSave = null;
        }
        internal Entry(Action<LoadOperation> onLoad)
        {
            _onLoad = new();

            _onSave = null;
            _onLoad.Add(onLoad);
            _loadedSave = null;
        }
        internal Entry(object toSave)
        {
            _onLoad = new();

            _onSave = () => toSave;
            _loadedSave = null;
        }

        internal object InvokeSave()
        {
            if (_onSave != null && _onSave.Target is Component component && component == null)
                return _onSave = null;

            return _onSave?.Invoke();
        }

        internal void InvokeLoad(object data)
        {
            if (_onSave == null)
                _onSave = () => data;

            for (int i = _onLoad.Count - 1; i >= 0; i--)
            {
                if (_onLoad[i].Target is Component component && component == null)
                {
                    _onLoad.RemoveAt(i);
                }
                else
                {
                    _onLoad[i].Invoke(new LoadOperation(LoadOperation.LoadState.LoadGame, data));
                }
            }
        }

        internal void Clear()
        {
            _onLoad = new();
            _onSave = null;
        }
    }
}
