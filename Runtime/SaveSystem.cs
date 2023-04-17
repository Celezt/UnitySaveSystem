#define SAVE_SYSTEM

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Sirenix.Serialization;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Celezt.SaveSystem.Utilities;

namespace Celezt.SaveSystem
{
    /// <summary>
    /// Global save system.
    /// </summary>
    public static class SaveSystem
    {
        public const int VERSION = 1;
        public const string SAVES_PATH = "/Saves/";
        public const string SAVE_FILE_TYPE = ".sav";

        public static event Action OnBeforeSave = delegate {};
        public static event Action OnAfterSave = delegate { };
        public static event Action OnBeforeLoad = delegate { };
        public static event Action OnAfterLoad = delegate { };
        /// <summary>
        /// When loading save. Gives access to the <see cref="AsyncOperation"/>.
        /// </summary>
        public static event Action<AsyncOperation> OnLoading = delegate { };

        /// <summary>
        /// The last saved file. Returns <see cref="string.Empty"/> if no last save exist.
        /// </summary>
        public static string LastSaveName
        {
            get => _lastSaveName;
            set => _lastSaveName = value;
        }

        private static string _lastSaveName = string.Empty;

        private static HashSet<Guid> _persistentEntries = new();
        private static Dictionary<Guid, EntryKey> _entryKeys = new();
        private static Dictionary<Guid, Entry> _entries = new();
        private static HashSet<Guid>[] _instancesByScene; // Set of instances connected to a scene by index.

        /// <summary>
        /// Get full path to the save directory. Different depending on the platform.
        /// <list type="bullet"><b>Windows Standalone Player:</b> Special folder. OneDrive support. <para>\Documents\My Games</para></list>
        /// <list type="bullet"><b>Other:</b> Persistent data path.
        /// <list type="bullet"><b>Windows Store Apps:</b> %userprofile%\AppData\Local\Packages\{productname}\LocalState</list>
        /// <list type="bullet"><b>WebGL:</b> /idbfs/{md5 hash of data path}</list>
        /// <list type="bullet"><b>Linux:</b> $XDG_CONFIG_HOME/unity3d or $HOME/.config/unity3d</list>
        /// <list type="bullet"><b>Mac:</b> ~/Library/Application Support/company name/{productname}</list>
        /// <list type="bullet"><b>IOS:</b> /var/mobile/Containers/Data/Application/{guid}/Documents</list>
        /// <list type="bullet"><b>Android:</b> /storage/emulated/0/Android/data/{packagename}/files</list>
        /// <list type="bullet"><b>tvOS:</b> is not supported and returns an empty string</list>
        /// </list>
        /// </summary>
        public static string SavePath
        {
            get
            {
#if UNITY_STANDALONE_WIN
                try
                {
                    string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).Replace("\\", "/") + "/My Games/" + Application.productName + SAVES_PATH;
                    Directory.CreateDirectory(path);
                    return path;
                }
                catch (Exception e)
                {
                    throw e;
                }
#else
                try
                {
                    string path = Application.persistentDataPath + SAVES_PATH;
                    Directory.CreateDirectory(path);
                    return path;
                }
                catch (Exception e)
                {
                    throw e;
                }
#endif
            }
        }

		/// <summary>
		/// Save game to the last saved file.
		/// </summary>
		public static void SaveGame()
        {
            if (string.IsNullOrEmpty(_lastSaveName))
            {
                Debug.LogError("No last save exist.");
                return;
            }

            SaveGame(_lastSaveName);
        }
        /// <summary>
        /// Save game to specified file.
        /// </summary>
        /// <param name="fileName"></param>
        public static void SaveGame(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogError("File name cannot be empty.");
                return;
            }

            OnBeforeSave.Invoke();

			try
            {
				SaveData saveData = new SaveData(VERSION, SceneManager.GetActiveScene().buildIndex, _entries);

				string filePath = SavePath + fileName + SAVE_FILE_TYPE;

                if (File.Exists(fileName))  // Overwrite existing file with the same path.
                {
                    string tempFilePath = filePath + ".tmp";
                    string oldFilePath = filePath + ".old";

                    using FileStream stream = File.OpenWrite(tempFilePath);
                    
					SerializationUtility.SerializeValue(saveData, stream, DataFormat.Binary);

                    File.Move(filePath, oldFilePath);
                    File.Move(tempFilePath, filePath);
                    File.Delete(oldFilePath);
                }
                else
                {
					using FileStream stream = File.OpenWrite(filePath);
					SerializationUtility.SerializeValue(saveData, stream, DataFormat.Binary);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            _lastSaveName = fileName;

            OnAfterSave.Invoke();
        }

        /// <summary>
        /// Load the last saved file.
        /// </summary>
        public static async UniTask LoadGame()
        {
            if (string.IsNullOrEmpty(_lastSaveName))
            {
                Debug.LogError("No last save exist.");
                return;
            }

            await LoadGame(_lastSaveName);
        }
        /// <summary>
        /// Load game from specified file.
        /// </summary>
        /// <param name="fileName"></param>
        public static async UniTask LoadGame(string fileName)
        {
            async UniTask Load(string correctFilePath)
            {
                try
                {
					using FileStream stream = File.OpenRead(correctFilePath);   
                    var toDeserialize = SerializationUtility.DeserializeValue<SaveData>(stream, DataFormat.Binary);

                    if (toDeserialize.Version != VERSION)
                        throw new Exception($"The save file, version: {toDeserialize.Version} is not supported. The current supported version is {VERSION}");

					AsyncOperation operation = SceneManager.LoadSceneAsync(toDeserialize.SpawnSceneIndex);
					operation.allowSceneActivation = false;

					while (!operation.isDone)   // When loading is done. It will not be true until allowSceneActivation is true.
					{
						OnLoading.Invoke(operation);
                        
						if (operation.progress >= 0.9f) // Will not progress past 0.9 until allowSceneActivation is true.
						{
							for (int j = 0; j < _instancesByScene.Length; j++)  // Clear all instance data.
								_instancesByScene[j].Clear();

                            var oldEntries = _entries;
                            _entries = toDeserialize.Entries;   // Replace the old entries with the new.

                            foreach (Guid guid in _persistentEntries)                   // Update with old content for all persistent entries.
                                if (_entries.TryGetValue(guid, out Entry outEntry))
                                {
                                    if (oldEntries.TryGetValue(guid, out Entry outOldEntry))
                                    {
										outEntry.OnLoad.AddRange(outOldEntry.AllAliveOnLoad); // Add all old on load when persistent and still alive.

                                        if (outOldEntry.IsOnSaveAlive)                  // Use old on save when persistent and still alive.
											outEntry.OnSave = outOldEntry.OnSave;           
                                    }
								}

                            foreach (var pair in _entries)
                            {
								if (pair.Value.CachedData is Instance instance)  // Get all instances to be instanced to a scene.
									_instancesByScene[instance.SceneIndex].Add(pair.Key);
							}

							OnBeforeLoad.Invoke();
							operation.allowSceneActivation = true;
						}

						await UniTask.Yield();
					}

					foreach (var pair in _entries)
					{
						if (_entries.TryGetValue(pair.Key, out var outEntry))
							outEntry.InvokeLoad();
					}
				}
                catch(Exception e)
                {
                    Debug.LogException(e);
                }
            }

            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogError("File name cannot be empty.");
                return;
            }

            string filePath = SavePath + fileName + SAVE_FILE_TYPE;
            string tempFilePath = filePath + ".tmp";
            string oldFilePath = filePath + ".old";

            bool currentExist = File.Exists(filePath);
            bool tempExist = File.Exists(tempFilePath);
            bool oldExist = File.Exists(oldFilePath);

            if (currentExist && tempExist)  // Both current and temp exist. Use the current in that case.
            {
                Debug.LogWarning("Something went wrong last time saved. The temp version still exist and the current save will be used instead.");
                await Load(filePath);
            }
            else if (tempExist && oldExist) // Both temp and old exist. Use the old in that case.
            {
                Debug.LogWarning("Something went wrong last time saved. The temp and old version still exist and the old save will be used instead.");
                await Load(oldFilePath);
            }
            else if (currentExist && oldExist) // Both current and old exist. Use the old in that case.
            {
                Debug.LogWarning("Something went wrong last time saved. The old version still exist and will instead be used.");
                await Load(oldFilePath);
            }
            else if (currentExist)
            {
                await Load(filePath);
            }
            else // if failed to load
            {
                Debug.LogWarning($"Could not load. No save file with the file path: {filePath} exist.");
            }

            _lastSaveName = fileName;

            OnAfterLoad.Invoke();
        }

        /// <summary>
        /// Get all saves from destined folder.
        /// </summary>
        /// <returns></returns>
        public static string[] GetSavedGames()
        {
            return Directory.GetFiles(SavePath, string.Format("*{0}", SAVE_FILE_TYPE));
        }
        public static bool SaveExists(string fileName)
        {
            return File.Exists(SavePath + fileName + SAVE_FILE_TYPE);
        }

        /// <summary>
        /// Deletes specified save file if it exist.
        /// </summary>
        public static void DeleteSaveGame(string fileName)
        {
            try
            {
                File.Delete(SavePath + fileName + SAVE_FILE_TYPE);
            }
            catch (IOException e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Opens the save folder automatically for the user.
        /// </summary>
        public static void OpenSaveGameFolder()
        {
            try
            {
                string savePath = SavePath;
                if (Directory.Exists(savePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = savePath,
                        UseShellExecute = true,
                        Verb = "open",
                    });
                }
                else
                    Debug.LogError($"{savePath} Directory does not exist");
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Get or add <see cref="EntryKey"/>. Used to add sub entries.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <returns><see cref="EntryKey"/>.</returns>
        public static EntryKey GetEntryKey(string id) => GetEntryKey(GuidExtension.Generate(id));
        /// <summary>
        /// Get or add <see cref="EntryKey"/>. Used to add sub entries.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <returns><see cref="EntryKey"/>.</returns>
        public static EntryKey GetEntryKey(Guid guid)
        {
            if (_entryKeys.TryGetValue(guid, out EntryKey existingSaveObject))
                return existingSaveObject;

            EntryKey newSaveObject = new EntryKey(guid);
            _entryKeys.Add(guid, newSaveObject);

            return newSaveObject;
        }
		/// <summary>
		/// Get or add <see cref="EntryKey"/>. Used to add sub entries. Get from existing <see cref="IIdentifiable"/> or <see cref="SaveBehaviour"/>.
		/// </summary>
		/// <param name="gameObject">GetComponentInParent for <see cref="IIdentifiable"/> or <see cref="SaveBehaviour"/>.</param>
		/// <returns><see cref="EntryKey"/>.</returns>
		public static EntryKey GetEntryKey(GameObject gameObject)
        {
            IIdentifiable identifiable = gameObject.GetComponentInParent<IIdentifiable>();

            if (identifiable == null)
                throw new NullReferenceException($"Could not find {nameof(IIdentifiable)} in parent.");

            return GetEntryKey(identifiable.Guid);
        }
		/// <summary>
		/// Get or add <see cref="EntryKey"/>. Used to add sub entries. Get from existing <see cref="SaveBehaviour"/>. If none exist, returns null.
		/// </summary>
		/// <param name="monoBehaviour">GetComponentInParent for <see cref="SaveBehaviour"/>.</param>
		/// <returns><see cref="EntryKey"/>.</returns>
		public static EntryKey GetEntryKey(MonoBehaviour monoBehaviour)
		{
			SaveBehaviour saveBehaviour = monoBehaviour.GetComponentInParent<SaveBehaviour>();

			return saveBehaviour.EntryKey;
		}

		/// <summary>
		/// Remove entry if it exist.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <returns>If it exist.</returns>
		public static bool RemoveEntryKey(string id) => RemoveEntryKey(GuidExtension.Generate(id));
        /// <summary>
        /// Remove entry if it exist.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <returns>If it exist.</returns>
        public static bool RemoveEntryKey(Guid guid)
        {
            if (_entryKeys.TryGetValue(guid, out var entryKey))
                entryKey.Clear();
            
            return _entryKeys.Remove(guid);
        }

		/// <summary>
		/// Try get cached data from existing entry.
		/// </summary>
		/// <param name="id">Identifier</param>
		/// <param name="outData">Data.</param>
		/// <returns>If cached data exist.</returns>
		public static bool TryGetCachedData(string id, out object outData) => TryGetCachedData(GuidExtension.Generate(id), out outData);
		/// <summary>
		/// Try get cached data from existing entry.
		/// </summary>
		/// <param name="guid">Identifier</param>
		/// <param name="outData">Data.</param>
		/// <returns>If cached data exist.</returns>
		public static bool TryGetCachedData(Guid guid, out object outData)
        {
            bool exist = _entries.TryGetValue(guid, out Entry outEntry) && outEntry.CachedData != null;
            outData = exist ? outEntry.CachedData : null;
            return exist;
        }
		/// <summary>
		/// Try get cached data from existing entry.
		/// </summary>
		/// <param name="id">Identifier</param>
		/// <param name="outData">Data.</param>
		/// <returns>If cached data exist.</returns>
		public static bool TryGetCachedData<T>(string id, out T outData) => TryGetCachedData(GuidExtension.Generate(id), out outData);
		/// <summary>
		/// Try get cached data from existing entry.
		/// </summary>
		/// <param name="guid">Identifier</param>
		/// <param name="outData">Data.</param>
		/// <returns>If cached data exist.</returns>
		public static bool TryGetCachedData<T>(Guid guid, out T outData)
        {
            bool exist = _entries.TryGetValue(guid, out Entry outEntry) && outEntry.CachedData != null;
            outData = exist ? (T)outEntry.CachedData : default(T);
            return exist;
        }

        /// <summary>
        /// Try get save from existing entry. 
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="outData">Data.</param>
        /// <returns>If any save exist.</returns>
        public static bool TryGetSave(string id, out object outData) => TryGetSave(GuidExtension.Generate(id), out outData);
        /// <summary>
        /// Try get save from existing entry. 
        /// </summary>
        /// <param name="guid">Identifier</param>
        /// <param name="outData">Data.</param>
        /// <returns>If any save exist.</returns>
        public static bool TryGetSave(Guid guid, out object outData)
        {
            bool exist = _entries.TryGetValue(guid, out Entry outEntry);
			object data = outEntry.Save;
			outData = exist && data != null ? data : null;
            return exist;
        }
        /// <summary>
        /// Try get save from existing entry. 
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="outData">Data.</param>
        /// <returns>If any save exist.</returns>
        public static bool TryGetSave<T>(string id, out T outData) => TryGetSave(GuidExtension.Generate(id), out outData);
        /// <summary>
        /// Try get save from existing entry. 
        /// </summary>
        /// <param name="guid">Identifier</param>
        /// <param name="outData">Data.</param>
        /// <returns>If any save exist.</returns>
        public static bool TryGetSave<T>(Guid guid, out T outData)
        {
			bool exist = _entries.TryGetValue(guid, out Entry outEntry);
            object data = outEntry.Save;
            outData = exist && data != null ? (T)data : default(T);
            return exist;
        }

        /// <summary>
        /// Subscribe to an entry.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="onLoad">Get value when loading.</param>
        /// <param name="loadPreviousSave">Call onLoad if a save exist.</param>
        /// <returns>If it exist.</returns>
        public static bool AddListener(string id, Action<object> onLoad, bool loadPreviousSave = true) 
            => AddListener(GuidExtension.Generate(id), onLoad, loadPreviousSave);
        /// <summary>
        /// Subscribe to an entry.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <param name="onLoad">Get value when loading.</param>
        /// <param name="loadPreviousSave">Call onLoad if a save exist.</param>
        /// <returns>If it exist.</returns>
        public static bool AddListener(Guid guid, Action<object> onLoad, bool loadPreviousSave = true) => SetEntry(guid, onLoad, loadPreviousSave);

        /// <summary>
        /// Unsubscribe an action from an entry.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="onLoad">Unsubscribed action.</param>
        /// <returns>If it exist.</returns>
        public static bool RemoveListener(string id, Action<object> onLoad) => RemoveListener(GuidExtension.Generate(id), onLoad);
        /// <summary>
        /// Unsubscribe an action from an entry.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <param name="onLoad">Unsubscribed action.</param>
        /// <returns>If it exist.</returns>
        public static bool RemoveListener(Guid guid, Action<object> onLoad)
        {
            if (_entries.TryGetValue(guid, out Entry outEntry))
            {
                outEntry.OnLoad.Remove(onLoad);

                return true;
            }

            return false;
        }

		/// <summary>
		/// Add or set persistent entry. Does not refresh on load.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <param name="onSave">Set value when saving.</param>
		/// <param name="onLoad">Get value when loading.</param>
		/// <param name="loadPreviousSave">Call onLoad if a save exist.</param>
		/// <returns>If entry already exist.</returns>
		public static bool SetPersistentEntry(string id, Func<object> onSave, Action<object> onLoad, bool loadPreviousSave = true) 
            => SetPersistentEntry(GuidExtension.Generate(id), onSave, onLoad, loadPreviousSave);
		/// <summary>
		/// Add or set persistent entry. Does not refresh on load.
		/// </summary>
		/// <param name="guid">Identifier.</param>
		/// <param name="onSave">Set value when saving.</param>
		/// <param name="onLoad">Get value when loading.</param>
		/// <param name="loadPreviousSave">Call onLoad if a save exist.</param>
		/// <returns>If entry already exist.</returns>
		public static bool SetPersistentEntry(Guid guid, Func<object> onSave, Action<object> onLoad, bool loadPreviousSave = true)
        {
            _persistentEntries.Add(guid);
            return SetEntry(guid, onSave, onLoad, loadPreviousSave);
        }
		/// <summary>
		/// Add or set persistent entry. Does not refresh on load.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <param name="toSave">Set value.</param>
		/// <param name="onLoad">Get value when loading.</param>
		/// <param name="loadPreviousSave">Call onLoad if a save exist.</param>
		/// <returns>If entry already exist.</returns>
		public static bool SetPersistentEntry(string id, object toSave, Action<object> onLoad, bool loadPreviousSave = true) 
            => SetPersistentEntry(GuidExtension.Generate(id), toSave, onLoad, loadPreviousSave);
		/// <summary>
		/// Add or set persistent entry. Does not refresh on load.
		/// </summary>
		/// <param name="guid">Identifier.</param>
		/// <param name="toSave">Set value.</param>
		/// <param name="onLoad">Get value when loading.</param>
		/// <param name="loadPreviousSave">Call onLoad if a save exist.</param>
		/// <returns>If entry already exist.</returns>
		public static bool SetPersistentEntry(Guid guid, object toSave, Action<object> onLoad, bool loadPreviousSave = true)
        {
            _persistentEntries.Add(guid);
            return SetEntry(guid, toSave, onLoad, loadPreviousSave);
        }
		/// <summary>
		/// Add or set persistent entry. Does not refresh on load.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <param name="onLoad">Get value when loading.</param>
		/// <returns>If entry already exist.</returns>
		public static bool SetPersistentEntry(string id, Action<object> onLoad, bool loadPreviousSave = true) => SetPersistentEntry(GuidExtension.Generate(id), onLoad, loadPreviousSave);
		/// <summary>
		/// Add or set persistent entry. Does not refresh on load.
		/// </summary>
		/// <param name="guid">Identifier.</param>
		/// <param name="onLoad">Get value when loading.</param>
		/// <returns>If entry already exist.</returns>
		public static bool SetPersistentEntry(Guid guid, Action<object> onLoad, bool loadPreviousSave = true)
        {
            _persistentEntries.Add(guid);
            return SetEntry(guid, onLoad, loadPreviousSave);
        }
		/// <summary>
		/// Add or set persistent entry. Does not refresh on load.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <param name="onSave">Set value when saving.</param>
		/// <returns>If entry already exist.</returns>
		public static bool SetPersistentEntry(string id, Func<object> onSave) => SetPersistentEntry(GuidExtension.Generate(id), onSave);
		/// <summary>
		/// Add or set persistent entry. Does not refresh on load.
		/// </summary>
		/// <param name="guid">Identifier.</param>
		/// <param name="onSave">Set value when saving.</param>
		/// <returns>If entry already exist.</returns>
		public static bool SetPersistentEntry(Guid guid, Func<object> onSave)
        {
            _persistentEntries.Add(guid);
            return SetEntry(guid, onSave);
        }
		/// <summary>
		/// Add or set persistent entry. Does not refresh on load.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <param name="toSave">Set value.</param>
		/// <returns>If entry already exist.</returns>
		public static bool SetPersistentEntry(string id, object toSave) => SetPersistentEntry(GuidExtension.Generate(id), toSave);
		/// <summary>
		/// Add or set persistent entry. Does not refresh on load.
		/// </summary>
		/// <param name="guid">Identifier.</param>
		/// <param name="toSave">Set value.</param>
		/// <returns>If entry already exist.</returns>
		public static bool SetPersistentEntry(Guid guid, object toSave)
        {
            _persistentEntries.Add(guid);
            return SetEntry(guid, toSave);
        }

        /// <summary>
        /// Downgrade persistent entry to a refreshable entry. Will refresh when loading a save.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        public static void DowngradeEntry(Guid guid)
        {
            _persistentEntries.Remove(guid);
        }

        /// <summary>
        /// Convert existing entry to a persistent entry. Prevents it from being refreshed when loading a save.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        public static void ConvertToPersistentEntry(Guid guid)
        {
            _persistentEntries.Add(guid);
        }

		/// <summary>
		/// Add or set entry.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <param name="onSave">Set value when saving.</param>
		/// <param name="onLoad">Get value when loading.</param>
		/// <param name="loadPreviousSave">Call onLoad if a save exist.</param>
		/// /// <returns>If entry already exist.</returns>
		public static bool SetEntry(string id, Func<object> onSave, Action<object> onLoad, bool loadPreviousSave = true) => SetEntry(GuidExtension.Generate(id), onSave, onLoad, loadPreviousSave);
		/// <summary>
		/// Add or set entry.
		/// </summary>
		/// <param name="guid">Identifier.</param>
		/// <param name="onSave">Set value when saving.</param>
		/// <param name="onLoad">Get value when loading.</param>
		/// <param name="loadPreviousSave">Call onLoad if a save exist.</param>
		/// <returns>If entry already exist.</returns>
		public static bool SetEntry(Guid guid, Func<object> onSave, Action<object> onLoad, bool loadPreviousSave = true)
        {
            if (_entries.TryGetValue(guid, out Entry outEntry))
            {
                outEntry.OnLoad.Add(onLoad);

                if (loadPreviousSave)
                {
					object save = outEntry.Save;
					if (save != null)   // Call latest if any save previously existed.
						onLoad.Invoke(save);
				}

                outEntry.Save = onSave;
                _entries[guid] = outEntry;

                return true;
            }

            _entries[guid] = new Entry(onSave, onLoad);

            return false;
        }
		/// <summary>
		/// Add or set entry.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <param name="toSave">Set value.</param>
		/// <param name="onLoad">Get value when loading.</param>
		/// <param name="loadPreviousSave">Call onLoad if a save exist.</param>
		/// <returns>If entry already exist.</returns>
		public static bool SetEntry(string id, object toSave, Action<object> onLoad, bool loadPreviousSave = true) => SetEntry(GuidExtension.Generate(id), toSave, onLoad, loadPreviousSave);
		/// <summary>
		/// Add or set entry.
		/// </summary>
		/// <param name="guid">Identifier.</param>
		/// <param name="toSave">Set value.</param>
		/// <param name="onLoad">Get value when loading.</param>
		/// <param name="loadPreviousSave">Call onLoad if a save exist.</param>
		/// <returns>If entry already exist.</returns>
		public static bool SetEntry(Guid guid, object toSave, Action<object> onLoad, bool loadPreviousSave = true)
        {
            if (_entries.TryGetValue(guid, out Entry outEntry))
            {
                outEntry.OnLoad.Add(onLoad);

                if (loadPreviousSave)
                {
					object save = outEntry.Save;
					if (save != null)   // Call latest if any save previously existed.
						onLoad.Invoke(save);
				}

                outEntry.Save = toSave;
                _entries[guid] = outEntry;

                return true;
            }

            _entries[guid] = new Entry(toSave, onLoad);

            return false;
        }
		/// <summary>
		/// Add or set entry.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <param name="onLoad">Get value when loading.</param>
		/// <returns>If entry already exist.</returns>
		public static bool SetEntry(string id, Action<object> onLoad, bool loadPreviousSave = true) => SetEntry(GuidExtension.Generate(id), onLoad, loadPreviousSave);
		/// <summary>
		/// Add or set entry.
		/// </summary>
		/// <param name="guid">Identifier.</param>
		/// <param name="onLoad">Get value when loading.</param>
		/// <returns>If entry already exist.</returns>
		public static bool SetEntry(Guid guid, Action<object> onLoad, bool loadPreviousSave = true)
		{
			if (_entries.TryGetValue(guid, out Entry outEntry))
			{
				outEntry.OnLoad.Add(onLoad);

				if (loadPreviousSave)
				{
					object save = outEntry.Save;
					if (save != null)   // Call latest if any save previously existed.
						onLoad.Invoke(save);
				}

				return true;
			}

			_entries[guid] = new Entry(onLoad);

			return false;
		}
		/// <summary>
		/// Add or set entry.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <param name="onSave">Set value when saving.</param>
		/// <returns>If entry already exist.</returns>
		public static bool SetEntry(string id, Func<object> onSave) => SetEntry(GuidExtension.Generate(id), onSave);
		/// <summary>
		/// Add or set entry.
		/// </summary>
		/// <param name="guid">Identifier.</param>
		/// <param name="onSave">Set value when saving.</param>
		/// <returns>If entry already exist.</returns>
		public static bool SetEntry(Guid guid, Func<object> onSave)
        {
            if (_entries.TryGetValue(guid, out Entry outEntry))
            {
                outEntry.Save = onSave;
                _entries[guid] = outEntry;

                return true;
            }

            _entries[guid] = new Entry(onSave);

            return false;
        }
		/// <summary>
		/// Add or set entry.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <param name="toSave">Set value.</param>
		/// <returns>If entry already exist.</returns>
		public static bool SetEntry(string id, object toSave) => SetEntry(GuidExtension.Generate(id), toSave);
		/// <summary>
		/// Add or set entry.
		/// </summary>
		/// <param name="guid">Identifier.</param>
		/// <param name="toSave">Set value.</param>
		/// <returns>If entry already exist.</returns>
		public static bool SetEntry(Guid guid, object toSave)
        {
            if (_entries.TryGetValue(guid, out Entry outEntry))
            {
                outEntry.Save = toSave;
                _entries[guid] = outEntry;

                return true;
            }

			_entries[guid] = new Entry(toSave);

			return false;
        }

        /// <summary>
        /// Remove entry if it exist.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <returns>If it exist.</returns>
        public static bool RemoveEntry(string id) => RemoveEntry(GuidExtension.Generate(id));
        /// <summary>
        /// Remove entry if it exist.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <returns>If it exist.</returns>
        public static bool RemoveEntry(Guid guid)
        {
            return _entries.Remove(guid);
        }

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            _instancesByScene = new HashSet<Guid>[SceneManager.sceneCountInBuildSettings + 1];
            for (int i = 0; i < _instancesByScene.Length; i++)
                _instancesByScene[i] = new HashSet<Guid>();

            SceneManager.sceneLoaded += (scene, mode) =>
            {
				int sceneIndex = scene.buildIndex;

                foreach (Guid guid in _instancesByScene[sceneIndex])
                {
                    if (TryGetSave(guid, out Instance outInstance))
                    {
                        Addressables.InstantiateAsync(outInstance.AssetReference).Completed += operation => 
                        {
                            GameObject gameObject = operation.Result;

                            if (gameObject.TryGetComponent(out SaveBehaviour saveBehaviour))
                            {
                                saveBehaviour.Guid = outInstance.InstanceGuid;
                            }

                        };
                    }
                }
            };
        }
    }
}
