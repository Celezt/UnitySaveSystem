using Celezt.SaveSystem;
using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

[assembly: RegisterFormatter(typeof(SaveDataFormatter))]
internal class SaveDataFormatter : BaseFormatter<SaveData>
{
	private static readonly Serializer<int> _intSerializer = Serializer.Get<int>();
	private static readonly Serializer<Dictionary<Guid, Entry>> _entriesSerializer = Serializer.Get<Dictionary<Guid, Entry>>();

	protected override void DeserializeImplementation(ref SaveData value, IDataReader reader)
	{
		value = new SaveData
					(
						version: _intSerializer.ReadValue(reader),
						spawnSceneIndex: _intSerializer.ReadValue(reader),
						entries: _entriesSerializer.ReadValue(reader)
					);
	}

	protected override void SerializeImplementation(ref SaveData value, IDataWriter writer)
	{
		_intSerializer.WriteValue(value.Version, writer);
		_intSerializer.WriteValue(value.SpawnSceneIndex, writer);
		_entriesSerializer.WriteValue(value.Entries, writer);
	}
}
