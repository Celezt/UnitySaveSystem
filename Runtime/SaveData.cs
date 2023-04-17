using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Celezt.SaveSystem
{
	[Serializable]
	internal readonly struct SaveData
	{
		public readonly int Version;
		public readonly int SpawnSceneIndex;
		public readonly Dictionary<Guid, Entry> Entries;

		internal SaveData(int version, int spawnSceneIndex, Dictionary<Guid, Entry> entries)
		{
			Version = version;
			SpawnSceneIndex = spawnSceneIndex;
			Entries = entries;
		}
	}
}
