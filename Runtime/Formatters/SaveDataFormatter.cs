using Celezt.SaveSystem;
using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;

[assembly: RegisterFormatter(typeof(SaveDataFormatter))]
internal class SaveDataFormatter : BaseFormatter<SaveData>
{
	private static readonly Serializer<int> _intSerializer = Serializer.Get<int>();
	private static readonly Serializer<Guid> _guidSerializer = Serializer.Get<Guid>();
	private static readonly Serializer<object> _objectSerializer = Serializer.Get<object>();

	protected override void DeserializeImplementation(ref SaveData value, IDataReader reader)
	{
		int version = _intSerializer.ReadValue(reader);
		int spawnSceneIndex = _intSerializer.ReadValue(reader);

		Dictionary<Guid, Entry> entries = null;
		if (reader.PeekEntry(out _) == EntryType.StartOfArray)
		{
			try
			{
				reader.EnterArray(out var length);
				entries = new Dictionary<Guid, Entry>((int)length);
				for (int i = 0; i < length; i++)
				{
					if (reader.PeekEntry(out _) == EntryType.EndOfArray)
					{
						reader.Context.Config.DebugContext.LogError("Reached end of array after " + i + " elements, when " + length + " elements were expected.");
						break;
					}

					bool flag = true;
					try
					{
						reader.EnterNode(out var _);
						var guid = _guidSerializer.ReadValue(reader);
						var obj = _objectSerializer.ReadValue(reader);

						entries[guid] = Entry.CatchEntry(obj);
						goto IL_016d;
					}
					catch (SerializationAbortException ex)
					{
						flag = false;
						throw ex;
					}
					catch (Exception exception)
					{
						reader.Context.Config.DebugContext.LogException(exception);
						goto IL_016d;
					}
					finally
					{
						if (flag)
						{
							reader.ExitNode();
						}
					}

				IL_016d:
					if (!reader.IsInArrayNode)
					{
						reader.Context.Config.DebugContext.LogError("Reading array went wrong. Data dump: " + reader.GetDataDump());
						break;
					}
				}
			}
			finally
			{
				reader.ExitArray();
			}
		}
		else
		{
			reader.SkipEntry();
		}

		value = new SaveData(version, spawnSceneIndex, entries);
	}

	protected override void SerializeImplementation(ref SaveData value, IDataWriter writer)
	{
		_intSerializer.WriteValue(value.Version, writer);
		_intSerializer.WriteValue(value.SpawnSceneIndex, writer);

		try
		{
			writer.BeginArrayNode(value.Entries.Count);
			foreach (KeyValuePair<Guid, Entry> item in value.Entries)
			{
				bool flag = true;
				try
				{
					var guid = item.Key;
					var obj = item.Value.Save;

					writer.BeginStructNode(null, null);
					_guidSerializer.WriteValue(guid, writer);
					_objectSerializer.WriteValue(obj, writer);
				}
				catch (SerializationAbortException ex)
				{
					flag = false;
					throw ex;
				}
				catch (Exception exception)
				{
					writer.Context.Config.DebugContext.LogException(exception);
				}
				finally
				{
					if (flag)
					{
						writer.EndNode(null);
					}
				}
			}
		}
		finally
		{
			writer.EndArrayNode();
		}
	}
}
