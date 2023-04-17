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
	private static readonly Serializer<AssetReference> _assetReferenceSerializer = Serializer.Get<AssetReference>();

	protected override void DeserializeImplementation(ref Instance value, IDataReader reader)
	{
		value = new Instance
					(
						instanceGuid: _guidSerializer.ReadValue(reader),
						assetReference: _assetReferenceSerializer.ReadValue(reader),
						sceneIndex: _intSerializer.ReadValue(reader)
					);

	}

	protected override void SerializeImplementation(ref Instance value, IDataWriter writer)
	{
		_guidSerializer.WriteValue(value.InstanceGuid, writer);
		_assetReferenceSerializer.WriteValue(value.AssetReference, writer);
		_intSerializer.WriteValue(value.SceneIndex, writer);
	}
}
