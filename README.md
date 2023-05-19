# Unity Save System

Unity Save System is an all-in-one save system for Unity 2021+. The current usage is exclusive to scripts. It is designed to be easy to use for less experienced C# users and with minimal boilerplate code and works out of the box without any setup more than what data to save. It saves to a binary file using [Odin Serializer](https://github.com/TeamSirenix/odin-serializer), which allows the user to serialize almost any type.

## How to Use

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

For the source generator to work, it requires a 'partial' class and to call 'RegisterSaveObject' (Recommended to call from Awake()). The use of the 'Save' attribute also requires a unique identifier. For example, there might be multiple instances of a game object with the same behaviour, but to be able to save the data, the data need to be somehow distinguishable. For the behaviour to be unique, it can derive from 'IIdentifiable'. Only one 'IIdentifiable' is required, and all other behaviours connected to that instance, such as a prefab, can refer to it. To implicitly refer to 'IIdentifiable', you only need to derive your class from 'MonoBehaviour'. It searches with 'GetComponentInParent<IIdentifiable>', meaning it only works if there is an IIdentifiable to find. Because all instances, such as prefabs, will have a unique value, the identifier for the actual data only needs to be unique locally. The unique and local identifiers are combined and used as a key in a flat dictionary.
     
```cs
     SaveSystem.SetEntry("key", 123);
     SaveSystem.SetEntry("key", () => _fieldValue);
     SaveSystem.SetEntry("key", () => _fieldValue, value => _fieldValue = (int)value);
     
     if (SaveSystem.TryGetSave("key", out int value))
          _fieldValue = value;
     
     EntryKey key = SaveSystem.GetEntryKey("entry_key");
     key.SetSubEntry("key", 123);
```

## Install

You can clone it and import it into the project as an asset or package or use Unity Package Manager and click **"Add package from git URL..."** and add https://github.com/Celezt/UnitySaveSystem.git.

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
