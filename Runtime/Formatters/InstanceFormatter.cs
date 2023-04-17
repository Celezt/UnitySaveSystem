using Celezt.SaveSystem;
using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[assembly: RegisterFormatter(typeof(InstanceFormatter))]
internal class InstanceFormatter : BaseFormatter<Instance>
{
	private static readonly Serializer<int> _intSerializer = Serializer.Get<int>();
	private static readonly Serializer<Guid> _guidSerializer = Serializer.Get<Guid>();
	private static readonly Serializer<string> _stringSerializer = Serializer.Get<string>();

	protected override void DeserializeImplementation(ref Instance value, IDataReader reader)
	{
		value = new Instance
					(
						instanceGuid: _guidSerializer.ReadValue(reader),
						assetReference: new AssetReference(_stringSerializer.ReadValue(reader)),
						sceneIndex: _intSerializer.ReadValue(reader)
					);
	}

	protected override void SerializeImplementation(ref Instance value, IDataWriter writer)
	{
		_guidSerializer.WriteValue(value.InstanceGuid, writer);
		_stringSerializer.WriteValue(value.AssetReference.AssetGUID, writer);
		_intSerializer.WriteValue(value.SceneIndex, writer);
	}
}
