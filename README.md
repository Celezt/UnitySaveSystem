# Unity Save System

Unity Save System is an all-in-one save system for Unity 2021+. The current usage is exclusive to scripts. It is designed to be easy to use for less experienced C# users and with minimal boilerplate code and works out of the box without any setup more than what data to save. It saves to a binary file using [Odin Serializer](https://github.com/TeamSirenix/odin-serializer), which allows the user to serialize almost any type.

## How to Use
### Save Attribute

The easiest way to save something is to use the 'Save' attribute. It takes advantage of [Roslyn Source Generation](https://github.com/dotnet/roslyn) to generate boilerplate code that otherwise the user would need to implement themself. Alongside the source generation, it includes analysis to help the user implement everything correctly. All of this exists separately in another [repository](https://github.com/Celezt/UnitySaveSystemSourceGenerator). It is separate because Roslyn is limited to the .NET standard, which exists as a .dll in Unity Save System.

```cs
public partial class Example : MonoBehaviour
{
     [Save]
     public const string EXAMPLE_CONST = "This is a const string";

     [Save]
     public Guid ExampleGuid { get; set; } = Guid.NewGuid();

     [Save]
     private int _exampleValue;

     [Save]
     private void SetExampleValue(int value) => _exampleValue = value;

     private void Awake()
     {
          RegisterSaveObject();
     }
}

// Auto generated code.
public partial class Example
{
     /// ... ///
     protected void RegisterSaveObject()
     {
          global::Celezt.SaveSystem.SaveSystem.GetEntryKey(this)
               .SetSubEntry("example_const", () => EXAMPLE_CONST)
               .SetSubEntry("example_guid", () => ExampleGuid, value => ExampleGuid = (Guid)value)
               .SetSubEntry("example_value", () => _exampleValue, value => SetExampleValue((int)value));
     }
}
```
The 'Save' attribute is powerful and useful in different scenarios. It supports fields, properties and methods. The example above displays how the 'Save' attribute can save data. These are only a few examples of how to use it. They use their name as an identifier by default (set the property 'Identifier' to use a custom name) in snake_case. 'Get' and 'Set' in front of a method name is ignored. There can only be one set and get of an identifier and will otherwise overwrite the existing one, seen with 'SetExampleValue' overwrites '_exampleValue' set. There is a priority, with the first being the highest: method, property and field.

### Source Generation

For the source generator to work, it requires a 'partial' class and to call 'RegisterSaveObject' (Recommended to call from Awake()). The use of the 'Save' attribute also requires a unique identifier. For example, there might be multiple instances of a game object with the same behaviour, but to be able to save the data, the data need to be somehow distinguishable. For the behaviour to be unique, it can derive from 'IIdentifiable'. Only one 'IIdentifiable' is required, and all other behaviours connected to that instance, such as a prefab, can refer to it. To implicitly refer to 'IIdentifiable', you only need to derive your class from 'MonoBehaviour'. It searches with 'GetComponentInParent', meaning it only works if there is an IIdentifiable to find. Because all instances, such as prefabs, will have a unique value, the identifier for the actual data only needs to be unique locally. The unique and local identifiers are combined and used as a key in a flat dictionary.

### Set Entry
     
```cs
SaveSystem.SetEntry("key", 123);
SaveSystem.SetEntry("key", () => _fieldValue);
SaveSystem.SetEntry("key", () => _fieldValue, value => _fieldValue = (int)value);

if (SaveSystem.TryGetSave("key", out int value))  // E.g. calls '() => _fieldValue'.
     _fieldValue = value;

if (SaveSystem.TryGetCachedData("key", out int outData)     // 
     _fieldValue = outData;

EntryKey key = SaveSystem.GetEntryKey("entry_key");
key.SetSubEntry("key", 123);
```
It is also possible to manually set save data. The above shows a few variants of how to use the save system directly. Set and get can be done with a string or Guid as the key. 'SetEntry' is useful when using a singleton and does not need to worry about multiple instances. 'GetEntryKey' is convenient because only the entry key needs to be unique.

Setting an entry can be done directly or by calling a delegate when the game saves or loads. When the game loads, it caches the data; the data loads by the load delegate the first time when registering a save. There is no time limit for when to add a saved entry, which means loading can be asynchronous.

### Save Behaviour

![Save Behaviour](https://media.discordapp.net/attachments/985960740030656592/999837026964754522/unknown.png)

Using purely 'IIdentifiable' is possible, especially for complete control over how to save and be identified, but for most scenarios, using 'SaveBehaviour' is sufficient. There should only be one 'SaveBehaviour' per prefab. Depending on the need, adding it to a game object is enough for it to work. It supports scene-instanced, prefab, and runtime-instanced objects. It assigns a unique Guid and matches it with existing 'SaveBehaviour' to prevent duplicates. It gives a Guid to the prefab when instanced. By default, it will remember the transform, the scene and if destroyed. If instanced at runtime, for it to instantiate on load after saving, it is possible to assign an asset, usually of the same prefab. Instantiating on load handles by [Unity Addressable](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/index.html). There is no need to use Addressable if it already exists in the scene at build.

## Install

To install the plugin, clone it and import it into the project as an asset or package, or use Unity Package Manager and click "Add package from git URL..." and add https://github.com/Celezt/UnitySaveSystem.git.

## Save Location

The current version does not support custom save location. Where it saves depends on the platform:
* **Windows Standalone Player:** Special folder. OneDrive support. *\Documents\My Games*
* **Other:** Persistent data path.
  * **Windows Store Apps:** *%userprofile%\AppData\Local\Packages\{productname}\LocalState*
  * **WebGL:** */idbfs/{md5 hash of data path}*
  * **Linux:** *$XDG_CONFIG_HOME/unity3d or $HOME/.config/unity3d*
  * **Mac:** *~/Library/Application Support/{companyname}/{productname}*
  * **IOS:** */var/mobile/Containers/Data/Application/{guid}/Documents*
  * **Android:** */storage/emulated/0/Android/data/{packagename}/files*
