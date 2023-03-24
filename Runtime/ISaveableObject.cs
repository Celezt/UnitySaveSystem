using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Celezt.SaveSystem
{
    public interface ISaveableObject
    {
        public Guid Guid { get; }
		public EntryKey EntryKey { get; }
	}
}
