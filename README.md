# Unity Save System

Unity Save System is an all-in-one save system for Unity 2021+. The current usage is exclusive to scripts. It is designed to be easy to use for less experienced C# users and with minimal boilerplate code and works out of the box without any setup more than what data to save. It saves to a binary file using [Odin Serializer](https://github.com/TeamSirenix/odin-serializer), which allows the user to serialize almost any type.

The current version does not support custom save location. Where it saves depends on the platform:
* **Windows Standalone Player:** Special folder. OneDrive support. *\Documents\My Games*
* **Other:** Persistent data path.
  * **Windows Store Apps:** *%userprofile%\AppData\Local\Packages\{productname}\LocalState*
  * **WebGL:** */idbfs/{md5 hash of data path}*
  * **Linux:** *$XDG_CONFIG_HOME/unity3d or $HOME/.config/unity3d*
  * **Mac:** *~/Library/Application Support/{companyname}/{productname}*
  * **IOS:** */var/mobile/Containers/Data/Application/{guid}/Documents*
  * **Android:** */storage/emulated/0/Android/data/{packagename}/files*
