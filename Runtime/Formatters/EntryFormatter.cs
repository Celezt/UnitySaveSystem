using Celezt.SaveSystem;
using Sirenix.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[assembly: RegisterFormatter(typeof(EntryFormatter))]
internal class EntryFormatter : BaseFormatter<Entry>
{
	private static readonly Serializer<object> _objectSerializer = Serializer.Get<object>();

	protected override void DeserializeImplementation(ref Entry value, IDataReader reader)
	{
		value = Entry.CatchEntry(_objectSerializer.ReadValue(reader));
	}

	protected override void SerializeImplementation(ref Entry value, IDataWriter writer)
	{
		_objectSerializer.WriteValue(value.Save, writer);
	}
}
