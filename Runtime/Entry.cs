using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

namespace Celezt.SaveSystem
{
    internal struct Entry
    {
        internal List<Action<object?>> OnLoad => _onLoad;

        internal Func<object?>? OnSave
        {
            get => _onSave;
            set => _onSave = value;
		}

        internal object? CachedData => _cachedData;

		internal object? Save
        {
            get
            {
                if (!IsOnSaveAlive)
                    return _onSave = null;

				return _onSave?.Invoke();
			}
            set
            {
                if (value == null)
					_onSave = null;
				else if (value is Func<object> saveFunc)
                    _onSave = saveFunc;
                else
                    _onSave = () => value;
            }
        }

		internal bool IsOnSaveAlive =>
			_onSave != null && ((_onSave.Target is Component component && component != null)
			|| _onSave.Target is not Component);

		internal IEnumerable<Action<object>> AllAliveOnLoad
        {
            get
            {
                for (int i = 0; i < _onLoad.Count; i++)
                    if (IsOnLoadAlive(i))
                        yield return _onLoad[i];
            }
        }

		private List<Action<object?>> _onLoad;
		private Func<object?>? _onSave;
        private object? _cachedData;

        internal Entry(object? toSave, Action<object?> onLoad)
        {
            _onLoad = new();

            _onSave = () => toSave;
            _onLoad.Add(onLoad);
            _cachedData = null;
        }
        internal Entry(Func<object?> onSave, Action<object?> onLoad)
        {
            _onLoad = new();

            _onSave = onSave;
            _onLoad.Add(onLoad);
            _cachedData = null;
        }
        internal Entry(Func<object?> onSave)
        {
            _onLoad = new();

            _onSave = onSave;
            _cachedData = null;
        }
        internal Entry(Action<object?> onLoad)
        {
            _onLoad = new();

            _onSave = null;
            _onLoad.Add(onLoad);
            _cachedData = null;
        }
        internal Entry(object? toSave)
        {
            _onLoad = new();

            _onSave = () => toSave;
            _cachedData = null;
        }

        internal void InvokeLoad()
        {
            for (int i = _onLoad.Count - 1; i >= 0; i--)
            {
                if (IsOnLoadAlive(i))
                    _onLoad[i].Invoke(_cachedData);
                else
                    _onLoad.RemoveAt(i);
            }
        }

        internal bool IsOnLoadAlive(int index) => 
            (_onLoad[index].Target is Component component && component != null) || _onLoad[index].Target is not Component;

        internal static Entry CatchEntry(object data)
        {
            Entry entry = new Entry();
            entry._onLoad = new();
            entry.OnSave = () => data;
            entry._cachedData = data;
            return entry;
        }
	}
}
