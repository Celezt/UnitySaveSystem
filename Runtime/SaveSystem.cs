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

            var toSerialize = new (Guid guid, object data)[_entries.Count + 1];
            int i = 0;

            toSerialize[i++] = (Guid.Empty, new SaveInfo(VERSION, SceneManager.GetActiveScene().buildIndex));   // First index is for save info.

            foreach (var stream in _entries)
            {
                Guid guid = stream.Key;
                Entry entry = stream.Value;

                toSerialize[i++] = (guid, entry.Save);
            }

            try
            {
                string filePath = SavePath + fileName + SAVE_FILE_TYPE;

                if (File.Exists(fileName))  // Overwrite existing file with the same path.
                {
                    string tempFilePath = filePath + ".tmp";
                    string oldFilePath = filePath + ".old";

                    byte[] bytes = SerializationUtility.SerializeValue(toSerialize, DataFormat.Binary);
                    File.WriteAllBytes(tempFilePath, bytes);

                    File.Move(filePath, oldFilePath);
                    File.Move(tempFilePath, filePath);
                    File.Delete(oldFilePath);
                }
                else
                {
                    byte[] bytes = SerializationUtility.SerializeValue(toSerialize, DataFormat.Binary);
                    File.WriteAllBytes(filePath, bytes);
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
                    byte[] bytes = File.ReadAllBytes(correctFilePath);
                    var toDeserialize = ((Guid guid, object data)[])SerializationUtility.DeserializeValueWeak(bytes, DataFormat.Binary);

                    if (toDeserialize[0].data is not SaveInfo saveInfo)
                        throw new Exception("Save info is missing.");

                    if (saveInfo.Version != VERSION)
                        throw new Exception($"The save file, version: {saveInfo.Version} is not supported. The current supported version is {VERSION}");

					AsyncOperation operation = SceneManager.LoadSceneAsync(saveInfo.SceneIndex);
					operation.allowSceneActivation = false;

					while (!operation.isDone)   // When loading is done. It will not be true until allowSceneActivation is true.
					{
						OnLoading.Invoke(operation);

						if (operation.progress >= 0.9f) // Will not progress past 0.9 until allowSceneActivation is true.
						{
							Guid[] toRemove = (from x in _entries.Keys
											   where !_persistentEntries.Contains(x)
											   select x).ToArray();

							for (int i = 0; i < toRemove.Length; i++) // Remove all non persistent entries.
								RemoveEntry(toRemove[i]);

							for (int j = 0; j < _instancesByScene.Length; j++)  // Clear all instance data.
								_instancesByScene[j].Clear();

							for (int i = 1; i < toDeserialize.Length; i++)  // Skip SaveInfo.
							{
								Guid guid = toDeserialize[i].guid;
								object data = toDeserialize[i].data;

								if (data is Instance instance)  // Get all instances to be instanced to a scene.
								{
									_instancesByScene[instance.SceneIndex].Add(guid);
								}

								if (_entries.TryGetValue(guid, out Entry outEntry))
								{
									outEntry.LoadedSave = data;  // Set last saved data from file.
									_entries[guid] = outEntry;
								}
								else
								{
									Entry newEntry = new Entry(data);
									newEntry.LoadedSave = data;
									_entries[guid] = newEntry;
								}

							}

							OnBeforeLoad.Invoke();
							operation.allowSceneActivation = true;
						}

						await UniTask.Yield();
					}

					for (int i = 1; i < toDeserialize.Length; i++)  // Deserialize and load data from file.
					{
						Guid guid = toDeserialize[i].guid;
						object data = toDeserialize[i].data;

						if (_entries.TryGetValue(guid, out var outEntry))
							outEntry.InvokeLoad(data);
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
        /// Get or add <see cref="EntryKey"/>. Used to add sub entries. Get from existing <see cref="SaveBehaviour"/>. If none exist, returns null.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <returns><see cref="EntryKey"/>.</returns>
        public static EntryKey GetEntryKey(GameObject gameObject)
        {
            SaveBehaviour saveBehaviour = gameObject.GetComponentInParent<SaveBehaviour>();

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
        /// Try get the last loaded save from existing entry.
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="outData">Data.</param>
        /// <returns>If loaded save exist.</returns>
        public static bool TryGetLoadedSave(string id, out object outData) => TryGetLoadedSave(GuidExtension.Generate(id), out outData);
        /// <summary>
        /// Try get the last loaded save from existing entry.
        /// </summary>
        /// <param name="guid">Identifier</param>
        /// <param name="outData">Data.</param>
        /// <returns>If loaded save exist.</returns>
        public static bool TryGetLoadedSave(Guid guid, out object outData)
        {
            bool exist = _entries.TryGetValue(guid, out Entry outEntry) && outEntry.LoadedSave != null;
            outData = exist ? outEntry.LoadedSave : null;
            return exist;
        }
        /// <summary>
        /// Try get the last loaded save from existing entry.
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <param name="outData">Data.</param>
        /// <returns>If loaded save exist.</returns>
        public static bool TryGetLoadedSave<T>(string id, out T outData) => TryGetLoadedSave(GuidExtension.Generate(id), out outData);
        /// <summary>
        /// Try get the last loaded save from existing entry.
        /// </summary>
        /// <param name="guid">Identifier</param>
        /// <param name="outData">Data.</param>
        /// <returns>If loaded save exist.</returns>
        public static bool TryGetLoadedSave<T>(Guid guid, out T outData)
        {
            bool exist = _entries.TryGetValue(guid, out Entry outEntry) && outEntry.LoadedSave != null;
            outData = exist ? (T)outEntry.LoadedSave : default(T);
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
            bool exist = _entries.TryGetValue(guid, out Entry outEntry) && outEntry.Save != null;
            outData = exist ? outEntry.Save : null;
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
            bool exist = _entries.TryGetValue(guid, out Entry outEntry) && outEntry.Save != null;
            outData = exist ? (T)outEntry.Save : default(T);
            return exist;
        }

        /// <summary>
        /// Subscribe to an entry.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="onLoad">Get value when loading.</param>
        /// <returns>If it exist.</returns>
        public static bool SubscribeEntry(string id, Action<LoadOperation> onLoad) => SubscribeEntry(GuidExtension.Generate(id), onLoad);
        /// <summary>
        /// Subscribe to an entry.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <param name="onLoad">Get value when loading.</param>
        /// <returns>If it exist.</returns>
        public static bool SubscribeEntry(Guid guid, Action<LoadOperation> onLoad)
        {
            if (_entries.TryGetValue(guid, out Entry outEntry))
            {
                int loadCount = outEntry.Load.Count;
                outEntry.Load.Add(onLoad);

                object save = outEntry.Save;
                if (save != null)   // Call latest if any save previously existed.
                    outEntry.Load[loadCount].Invoke(new LoadOperation(LoadOperation.LoadState.LoadPreviousSave, save));

                return true;
            }

            _entries[guid] = new Entry(onLoad);
            return false;
        }

        /// <summary>
        /// Unsubscribe an action from an entry.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="onLoad">Unsubscribed action.</param>
        /// <returns>If it exist.</returns>
        public static bool UnsubscribeEntry(string id, Action<LoadOperation> onLoad) => UnsubscribeEntry(GuidExtension.Generate(id), onLoad);
        /// <summary>
        /// Unsubscribe an action from an entry.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <param name="onLoad">Unsubscribed action.</param>
        /// <returns>If it exist.</returns>
        public static bool UnsubscribeEntry(Guid guid, Action<LoadOperation> onLoad)
        {
            if (_entries.TryGetValue(guid, out Entry outEntry))
            {
                outEntry.Load.Remove(onLoad);

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
        public static void SetPersistentEntry(string id, Func<object> onSave, Action<LoadOperation> onLoad) => SetPersistentEntry(GuidExtension.Generate(id), onSave, onLoad);
        /// <summary>
        /// Add or set persistent entry. Does not refresh on load.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <param name="onSave">Set value when saving.</param>
        /// <param name="onLoad">Get value when loading.</param>
        public static void SetPersistentEntry(Guid guid, Func<object> onSave, Action<LoadOperation> onLoad)
        {
            _persistentEntries.Add(guid);
            SetEntry(guid, onSave, onLoad);
        }
        /// <summary>
        /// Add or set persistent entry. Does not refresh on load.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="toSave">Set value.</param>
        /// <param name="onLoad">Get value when loading.</param>
        public static void SetPersistentEntry(string id, object toSave, Action<LoadOperation> onLoad) => SetPersistentEntry(GuidExtension.Generate(id), toSave, onLoad);
        /// <summary>
        /// Add or set persistent entry. Does not refresh on load.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <param name="toSave">Set value.</param>
        /// <param name="onLoad">Get value when loading.</param>
        public static void SetPersistentEntry(Guid guid, object toSave, Action<LoadOperation> onLoad)
        {
            _persistentEntries.Add(guid);
            SetEntry(guid, toSave, onLoad);
        }
        /// <summary>
        /// Add or set persistent entry. Does not refresh on load.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="onSave">Set value when saving.</param>
        public static void SetPersistentEntry(string id, Func<object> onSave) => SetPersistentEntry(GuidExtension.Generate(id), onSave);
        /// <summary>
        /// Add or set persistent entry. Does not refresh on load.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <param name="onSave">Set value when saving.</param>
        public static void SetPersistentEntry(Guid guid, Func<object> onSave)
        {
            _persistentEntries.Add(guid);
            SetEntry(guid, onSave);
        }
        /// <summary>
        /// Add or set persistent entry. Does not refresh on load.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="toSave">Set value.</param>
        public static void SetPersistentEntry(string id, object toSave) => SetPersistentEntry(GuidExtension.Generate(id), toSave);
        /// <summary>
        /// Add or set persistent entry. Does not refresh on load.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <param name="toSave">Set value.</param>
        public static void SetPersistentEntry(Guid guid, object toSave)
        {
            _persistentEntries.Add(guid);
            SetEntry(guid, toSave);
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
        public static void SetEntry(string id, Func<object> onSave, Action<LoadOperation> onLoad) => SetEntry(GuidExtension.Generate(id), onSave, onLoad);
        /// <summary>
        /// Add or set entry.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <param name="onSave">Set value when saving.</param>
        /// <param name="onLoad">Get value when loading.</param>
        public static void SetEntry(Guid guid, Func<object> onSave, Action<LoadOperation> onLoad)
        {
            if (_entries.TryGetValue(guid, out Entry outEntry))
            {
                int loadCount = outEntry.Load.Count;
                outEntry.Load.Add(onLoad);

                object save = outEntry.Save;
                if (save != null)   // Call latest if any save previously existed.
                    outEntry.Load[loadCount].Invoke(new LoadOperation(LoadOperation.LoadState.LoadPreviousSave, save));

                outEntry.Save = onSave;
                _entries[guid] = outEntry;
            }
            else
                _entries[guid] = new Entry(onSave, onLoad);
        }
        /// <summary>
        /// Add or set entry.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="toSave">Set value.</param>
        /// <param name="onLoad">Get value when loading.</param>
        public static void SetEntry(string id, object toSave, Action<LoadOperation> onLoad) => SetEntry(GuidExtension.Generate(id), toSave, onLoad);
        /// <summary>
        /// Add or set entry.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <param name="toSave">Set value.</param>
        /// <param name="onLoad">Get value when loading.</param>
        public static void SetEntry(Guid guid, object toSave, Action<LoadOperation> onLoad)
        {
            if (_entries.TryGetValue(guid, out Entry outEntry))
            {
                int loadCount = outEntry.Load.Count;
                outEntry.Load.Add(onLoad);

                object save = outEntry.Save;
                if (save != null)   // Call latest if any save previously existed.
                    outEntry.Load[loadCount].Invoke(new LoadOperation(LoadOperation.LoadState.LoadPreviousSave, save));

                outEntry.Save = toSave;
                _entries[guid] = outEntry;
            }
            else
                _entries[guid] = new Entry(toSave, onLoad);
        }
        /// <summary>
        /// Add or set entry.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="onSave">Set value when saving.</param>
        public static void SetEntry(string id, Func<object> onSave) => SetEntry(GuidExtension.Generate(id), onSave);
        /// <summary>
        /// Add or set entry.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <param name="onSave">Set value when saving.</param>
        public static void SetEntry(Guid guid, Func<object> onSave)
        {
            if (_entries.TryGetValue(guid, out Entry outEntry))
            {
                outEntry.Save = onSave;
                _entries[guid] = outEntry;
            }
            else
                _entries[guid] = new Entry(onSave);
        }
        /// <summary>
        /// Add or set entry.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="toSave">Set value.</param>
        public static void SetEntry(string id, object toSave) => SetEntry(GuidExtension.Generate(id), toSave);
        /// <summary>
        /// Add or set entry.
        /// </summary>
        /// <param name="guid">Identifier.</param>
        /// <param name="toSave">Set value.</param>
        public static void SetEntry(Guid guid, object toSave)
        {
            if (_entries.TryGetValue(guid, out Entry outEntry))
            {
                outEntry.Save = toSave;
                _entries[guid] = outEntry;
            }
            else
            {
                _entries[guid] = new Entry(toSave);
            }
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

            if (SaveSystem.TryGetLoadedSave("key", out bool boolValue))
            {
                Debug.Log(boolValue);
            }

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
