using Celezt.SaveSystem.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Celezt.SaveSystem
{
    public struct EntryKey
    {
        public Guid Owner => _owner;

        private HashSet<Guid> _subEntries;

        private Guid _owner;

        internal EntryKey(Guid owner)
        {
            _subEntries = new();
            _owner = owner;
        }

        public override string ToString() => _owner.ToString();
        public override int GetHashCode() => _owner.GetHashCode();
        public override bool Equals(object obj)
        {
            if (obj is Guid guid)
                return _owner.Equals(guid);
            else
                return false;
        }

        /// <summary>
        /// Try get the last loaded save from existing sub entry.
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="outData">Data.</param>
        /// <returns>If loaded save exist.</returns>
        public bool TryGetSubLoadedSave(string id, out object outData) => TryGetSubLoadedSave(GuidExtension.Generate(id), out outData);
        /// <summary>
        /// Try get the last loaded save from existing sub entry.
        /// </summary>
        /// <param name="guid">Identifier</param>
        /// <param name="outData">Data.</param>
        /// <returns>If loaded save exist.</returns>
        public bool TryGetSubLoadedSave(Guid guid, out object outData)
        {
            return SaveSystem.TryGetLoadedSave(_owner.Xor(guid), out outData);
        }
        /// <summary>
        /// Try get the last loaded save from existing sub entry.
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="outData">Data.</param>
        /// <returns>If loaded save exist.</returns>
        public bool TryGetSubLoadedSave<T>(string id, out T outData) => TryGetSubLoadedSave(GuidExtension.Generate(id), out outData);
        /// <summary>
        /// Try get the last loaded save from existing sub entry.
        /// </summary>
        /// <param name="guid">Identifier</param>
        /// <param name="outData">Data.</param>
        /// <returns>If loaded save exist.</returns>
        public bool TryGetSubLoadedSave<T>(Guid guid, out T outData)
        {
            return SaveSystem.TryGetLoadedSave(_owner.Xor(guid), out outData);
        }

        /// <summary>
        /// Try get save from existing sub entry. 
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="outData">Data.</param>
        /// <returns>If any save exist.</returns>
        public bool TryGetSubSave(string id, out object outData) => TryGetSubSave(GuidExtension.Generate(id), out outData);
        /// <summary>
        /// Try get save from existing sub entry. 
        /// </summary>
        /// <param name="guid">Identifier</param>
        /// <param name="outData">Data.</param>
        /// <returns>If any save exist.</returns>
        public bool TryGetSubSave(Guid guid, out object outData)
        {
            return SaveSystem.TryGetSave(_owner.Xor(guid), out outData);
        }
        /// <summary>
        /// Try get save from existing sub entry. 
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="outData">Data.</param>
        /// <returns>If any save exist.</returns>
        public bool TryGetSubSave<T>(string id, out T outData) => TryGetSubSave(GuidExtension.Generate(id), out outData);
        /// <summary>
        /// Try get save from existing sub entry. 
        /// </summary>
        /// <param name="guid">Identifier</param>
        /// <param name="outData">Data.</param>
        /// <returns>If any save exist.</returns>
        public bool TryGetSubSave<T>(Guid guid, out T outData)
        {
            return SaveSystem.TryGetSave(_owner.Xor(guid), out outData);
        }

        /// <summary>
        /// Subscribe to a sub entry.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="onLoad">Get value when loading.</param>
        /// <returns>If it exist.</returns>
        public bool SubscribeSubEntry(string id, Action<LoadOperation> onLoad) => SubscribeSubEntry(GuidExtension.Generate(id), onLoad);
        /// <summary>
        /// Subscribe to a sub entry.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <param name="onLoad">Get value when loading.</param>
        /// <returns>If it exist.</returns>
        public bool SubscribeSubEntry(Guid guid, Action<LoadOperation> onLoad)
        {
            Guid combinedGuid = _owner.Xor(guid);
            return SaveSystem.SubscribeEntry(combinedGuid, onLoad);
        }

        /// <summary>
        /// Unsubscribe an action from a sub entry.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="onLoad">Unsubscribed action.</param>
        /// <returns>If it exist.</returns>
        public bool UnsubscribeSubEntry(string id, Action<LoadOperation> onLoad) => UnsubscribeSubEntry(GuidExtension.Generate(id), onLoad);
        /// <summary>
        /// Unsubscribe an action from a sub entry.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <param name="onLoad">Unsubscribed action.</param>
        /// <returns>If it exist.</returns>
        public bool UnsubscribeSubEntry(Guid guid, Action<LoadOperation> onLoad)
        {
            Guid combinedGuid = _owner.Xor(guid);
            return SaveSystem.UnsubscribeEntry(combinedGuid, onLoad);
        }

        /// <summary>
        /// Add or set persistent sub entry. Does not refresh on load.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="onSave">Set value.</param>
        /// <param name="onLoad">Get value when loading.</param>
        public EntryKey SetPersistentSubEntry(string id, Func<object> onSave, Action<LoadOperation> onLoad) => SetPersistentSubEntry(GuidExtension.Generate(id), onSave, onLoad);
        /// <summary>
        /// Add or set persistent sub entry. Does not refresh on load.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <param name="onSave">Set value.</param>
        /// <param name="onLoad">Get value when loading.</param>
        public EntryKey SetPersistentSubEntry(Guid guid, Func<object> onSave, Action<LoadOperation> onLoad)
        {
            Guid combinedGuid = _owner.Xor(guid);
            SaveSystem.SetPersistentEntry(combinedGuid, onSave, onLoad);

            _subEntries.Add(guid);

            return this;
        }
        /// <summary>
        /// Add or set persistent sub entry. Does not refresh on load.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="toSave">Set value.</param>
        /// <param name="onLoad">Get value when loading.</param>
        public EntryKey SetPersistentSubEntry(string id, object toSave, Action<LoadOperation> onLoad) => SetPersistentSubEntry(GuidExtension.Generate(id), toSave, onLoad);
        /// <summary>
        /// Add or set persistent sub entry. Does not refresh on load.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <param name="toSave">Set value.</param>
        /// <param name="onLoad">Get value when loading.</param>
        public EntryKey SetPersistentSubEntry(Guid guid, object toSave, Action<LoadOperation> onLoad)
        {
            Guid combinedGuid = _owner.Xor(guid);
            SaveSystem.SetPersistentEntry(combinedGuid, toSave, onLoad);

            _subEntries.Add(guid);

            return this;
        }
        /// <summary>
        /// Add or set persistent sub entry. Does not refresh on load.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="onSave">Set value when saving.</param>
        public EntryKey SetPersistentSubEntry(string id, Func<object> onSave) => SetPersistentSubEntry(GuidExtension.Generate(id), onSave);
        /// <summary>
        /// Add or set persistent sub entry. Does not refresh on load.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <param name="onSave">Set value when saving.</param>
        public EntryKey SetPersistentSubEntry(Guid guid, Func<object> onSave)
        {
            Guid combinedGuid = _owner.Xor(guid);
            SaveSystem.SetPersistentEntry(combinedGuid, onSave);

            _subEntries.Add(guid);

            return this;
        }
        /// <summary>
        /// Add or set persistent sub entry. Does not refresh on load.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="toSave">Set value.</param>
        public EntryKey SetPersistentSubEntry(string id, object toSave) => SetPersistentSubEntry(GuidExtension.Generate(id), toSave);
        /// <summary>
        /// Add or set persistent sub entry. Does not refresh on load.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <param name="toSave">Set value.</param>
        public EntryKey SetPersistentSubEntry(Guid guid, object toSave)
        {
            Guid combinedGuid = _owner.Xor(guid);
            SaveSystem.SetPersistentEntry(combinedGuid, toSave);

            _subEntries.Add(guid);

            return this;
        }

        /// <summary>
        /// Downgrade persistent sub entry to a refreshable sub entry. Will refresh when loading a save.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        public void DowngradeSubEntry(Guid guid)
        {
            Guid combinedGuid = _owner.Xor(guid);
            SaveSystem.DowngradeEntry(combinedGuid);
        }

        /// <summary>
        /// Convert existing sub entry to a persistent sub entry. Prevents it from being refreshed when loading a save.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        public void ConvertToPersistentSubEntry(Guid guid)
        {
            Guid combinedGuid = _owner.Xor(guid);
            SaveSystem.ConvertToPersistentEntry(combinedGuid);
        }

        /// <summary>
        /// Add or set sub entry.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="onSave">Set value.</param>
        /// <param name="onLoad">Get value when loading.</param>
        public EntryKey SetSubEntry(string id, Func<object> onSave, Action<LoadOperation> onLoad) => SetSubEntry(GuidExtension.Generate(id), onSave, onLoad);
        /// <summary>
        /// Add or set sub entry.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <param name="onSave">Set value.</param>
        /// <param name="onLoad">Get value when loading.</param>
        public EntryKey SetSubEntry(Guid guid, Func<object> onSave, Action<LoadOperation> onLoad)
        {
            Guid combinedGuid = _owner.Xor(guid);
            SaveSystem.SetEntry(combinedGuid, onSave, onLoad);

            _subEntries.Add(guid);

            return this;
        }
        /// <summary>
        /// Add or set sub entry.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="toSave">Set value.</param>
        /// <param name="onLoad">Get value when loading.</param>
        public EntryKey SetSubEntry(string id, object toSave, Action<LoadOperation> onLoad) => SetSubEntry(GuidExtension.Generate(id), toSave, onLoad);
        /// <summary>
        /// Add or set sub entry.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <param name="toSave">Set value.</param>
        /// <param name="onLoad">Get value when loading.</param>
        public EntryKey SetSubEntry(Guid guid, object toSave, Action<LoadOperation> onLoad)
        {
            Guid combinedGuid = _owner.Xor(guid);
            SaveSystem.SetEntry(combinedGuid, toSave, onLoad);

            _subEntries.Add(guid);

            return this;
        }
        /// <summary>
        /// Add or set entry.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="onSave">Set value when saving.</param>
        public EntryKey SetSubEntry(string id, Func<object> onSave) => SetSubEntry(GuidExtension.Generate(id), onSave);
        /// <summary>
        /// Add or set entry.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <param name="onSave">Set value when saving.</param>
        public EntryKey SetSubEntry(Guid guid, Func<object> onSave)
        {
            Guid combinedGuid = _owner.Xor(guid);
            SaveSystem.SetEntry(combinedGuid, onSave);

            _subEntries.Add(guid);

            return this;
        }
        /// <summary>
        /// Add or set sub entry.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="toSave">Set value.</param>
        public EntryKey SetSubEntry(string id, object toSave) => SetSubEntry(GuidExtension.Generate(id), toSave);
        /// <summary>
        /// Add or set sub entry.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <param name="toSave">Set value.</param>
        public EntryKey SetSubEntry(Guid guid, object toSave)
        {
            Guid combinedGuid = _owner.Xor(guid);
            SaveSystem.SetEntry(combinedGuid, toSave);

            _subEntries.Add(guid);

            return this;
        }

        public bool RemoveSubEntry(string id) => RemoveSubEntry(GuidExtension.Generate(id));
        public bool RemoveSubEntry(Guid guid)
        {
            Guid combinedGuid = _owner.Xor(guid);
            _subEntries.Remove(combinedGuid);

            return SaveSystem.RemoveEntry(combinedGuid);
        }

        public void Clear()
        {
            foreach (var entryGuid in _subEntries)
            {
                SaveSystem.RemoveEntry(entryGuid.Xor(_owner));
            }

            _subEntries.Clear();
        }
    }
}
