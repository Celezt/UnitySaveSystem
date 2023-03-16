using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using Celezt.SaveSystem.Utilities;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif

namespace Celezt.SaveSystem
{
    [ExecuteInEditMode, DisallowMultipleComponent]
    public class SaveBehaviour : MonoBehaviour, ISaveable, ISerializationCallbackReceiver
    {
        private static readonly Guid _transformGuid = GuidExtension.Generate("transform");
        private static readonly Guid _destroyGuid = GuidExtension.Generate("destroy");
        private static readonly Guid _instanceGuid = GuidExtension.Generate("instance");

        public bool IsInstancedAtRuntime => _isInstancedAtRuntime;

        public EntryKey EntryKey => SaveSystem.GetEntryKey(Guid);

        public Guid Guid
        {
            get
            {
                if (_guid == Guid.Empty && serializedGuid != null && serializedGuid.Length == 16)
                    _guid = new Guid(serializedGuid);
                return _guid;
            }
            set
            {
                _guid = value;
                serializedGuid = _guid.ToByteArray();
            }
        }

        public AssetReferenceGameObject AssetReference
        {
            get => _assetReference;
            set => _assetReference = value;
        }

        // System guid we use for comparison and generation.
        private Guid _guid = Guid.Empty;

        // Unity's serialization system doesn't know about System.Guid, so we convert to a byte array
        // Fun fact, we tried using strings at first, but that allocated memory and was twice as slow.
        [SerializeField] private byte[] serializedGuid;

        [SerializeField] private bool _isPositionSaved = true;
        [SerializeField] private bool _isRotationSaved = true;
        [SerializeField] private bool _isScaleSaved = true;
        [SerializeField] private bool _isDestroyedSaved = true;
        [SerializeField] private bool _isInstancedAtRuntime = true;

        [SerializeField] private AssetReferenceGameObject _assetReference;

        private EntryKey _key;

        public bool IsGuidAssigned() => _guid != Guid.Empty;

        // We cannot allow a GUID to be saved into a prefab, and we need to convert to byte[].
        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            // This lets us detect if we are a prefab instance or a prefab asset.
            // A prefab asset cannot contain a GUID since it would then be duplicated when instanced.
            if (IsAssetOnDisk())
            {
                serializedGuid = null;
                _guid = Guid.Empty;
            }
            else
#endif
            {
                if (_guid != Guid.Empty)
                {
                    serializedGuid = _guid.ToByteArray();
                }
            }
        }

        // On load, we can go head a restore our system guid for later use.
        public void OnAfterDeserialize()
        {
            if (serializedGuid != null && serializedGuid.Length == 16)
            {
                _guid = new Guid(serializedGuid);
            }
        }

        // Never return an invalid GUID.
        public Guid GetGuid()
        {
            if (_guid == Guid.Empty && serializedGuid != null && serializedGuid.Length == 16)
            {
                _guid = new Guid(serializedGuid);
            }
           
            return _guid;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Use only in editor scope.
        /// </summary>
        public bool IsAssetOnDisk()
        {
            return PrefabUtility.IsPartOfPrefabAsset(this) || IsEditingInPrefabMode();
        }
#endif

        // When de-serializing or creating this component, we want to either restore our serialized GUID.
        // or create a new one.
        private void CreateGuid()
        {
            // if our serialized data is invalid, then we are a new object and need a new GUID.
            if (serializedGuid == null || serializedGuid.Length != 16)
            {
#if UNITY_EDITOR
                // if in editor, make sure we aren't a prefab of some kind.
                if (IsAssetOnDisk())
                {
                    return;
                }

                Undo.RecordObject(this, "Added GUID");
#endif
                _guid = Guid.NewGuid();
                serializedGuid = _guid.ToByteArray();

#if UNITY_EDITOR
                // If we are creating a new GUID for a prefab instance of a prefab, but we have somehow lost our prefab connection
                // force a save of the modified prefab instance properties.
                if (PrefabUtility.IsPartOfNonAssetPrefabInstance(this))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(this);
                }
#endif
            }
            else if (_guid == Guid.Empty)
            {
                // otherwise, we should set our system guid to our serialized guid.
                _guid = new Guid(serializedGuid);
            }

            // register with the GUID Manager so that other components can access this.
            if (_guid != Guid.Empty)
            {
                if (!GuidManager.Add(this))
                {
                    // if registration fails, we probably have a duplicate or invalid GUID, get us a new one.
                    serializedGuid = null;
                    _guid = Guid.Empty;
                    CreateGuid();
                }
            }
        }

#if UNITY_EDITOR
        private bool IsEditingInPrefabMode()
        {
            if (EditorUtility.IsPersistent(this))
            {
                // if the game object is stored on disk, it is a prefab of some kind, despite not returning true for IsPartOfPrefabAsset =/.
                return true;
            }
            else
            {
                // If the GameObject is not persistent let's determine which stage we are in first because getting Prefab info depends on it.
                var mainStage = StageUtility.GetMainStageHandle();
                var currentStage = StageUtility.GetStageHandle(gameObject);
                if (currentStage != mainStage)
                {
                    var prefabStage = PrefabStageUtility.GetPrefabStage(gameObject);
                    if (prefabStage != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
#endif
        private void Awake()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                CreateGuid();

                _isInstancedAtRuntime = false;
            }
#endif
        }

        private void Start()
        {
            CreateGuid();

            _key = SaveSystem.GetEntryKey(Guid);

            if (_isInstancedAtRuntime)      // Save asset reference and scene index when instanced at runtime.
            {
                _key.SetSubEntry(_instanceGuid, () => new Instance
                {
                    InstanceGuid = Guid,
                    AssetReference = _assetReference,
                    SceneIndex = gameObject.scene.buildIndex
                });
            }
            else if (_isDestroyedSaved) // If not instanced at runtime, save if scene object has been destroyed.
            {
                _key.SetSubEntry(_destroyGuid, false, operation =>
                {
                    bool isDestroyed = operation.GetResult<bool>();

                    if (isDestroyed)
                        Destroy(gameObject);
                });
            }

            if (_isPositionSaved || _isRotationSaved || _isScaleSaved)  // if any of them is enabled.
            {
                _key.SetSubEntry(_transformGuid, () => transform.localToWorldMatrix, operation =>
                {
                    var matrix = operation.GetResult<Matrix4x4>();

                    if (_isPositionSaved)
                        transform.position = matrix.GetPosition();

                    if (_isRotationSaved)
                        transform.rotation = matrix.GetRotation();

                    if (_isScaleSaved)
                        transform.localScale = matrix.GetScale();
                });
            }
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            // similar to on Serialize, but gets called on Copying a Component or Applying a Prefab
            // at a time that lets us detect what we are.
            if (IsAssetOnDisk())
            {
                serializedGuid = null;
                _guid = Guid.Empty;
            }
            else
#endif
            {
                CreateGuid();
            }
        }

        private void OnDestroy()
        {
            // let the manager know we are gone, so other objects no longer find this.
            GuidManager.Remove(_guid);

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
#endif
                if (SceneManager.GetActiveScene().isLoaded)  // If not unloaded.
                {
                    if (_isInstancedAtRuntime) // Remove all entries if a runtime instanced is destroyed.
                    {
                        SaveSystem.RemoveEntryKey(Guid);
                    }
                    else
                    {
                        _key.RemoveSubEntry(_transformGuid);
                        _key.SetSubEntry(_destroyGuid, true);
                    }
                }
#if UNITY_EDITOR
            }
#endif
        }
    }
}