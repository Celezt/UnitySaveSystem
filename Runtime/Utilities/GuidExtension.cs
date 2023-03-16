using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Celezt.SaveSystem.Utilities
{
	public static class GuidExtension
	{
		/// <summary>
		/// <see cref="https://stackoverflow.com/a/63719049/12707382"/>
		/// </summary>
		public static Guid Xor(this Guid a, Guid b)
		{
			Span<long> spanA = MemoryMarshal.CreateSpan(ref UnsafeUtility.As<Guid, long>(ref a), 2);
			Span<long> spanB = MemoryMarshal.CreateSpan(ref UnsafeUtility.As<Guid, long>(ref b), 2);

			spanB[0] ^= spanA[0];
			spanB[1] ^= spanA[1];

			return b;
		}

		/// <summary>
		/// <see cref="https://stackoverflow.com/a/6248764/12707382"/>
		/// </summary>
		public static Guid Generate(string input)
		{
			using (MD5 md5 = MD5.Create())
			{
				byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
				return new Guid(hash);
			}
		}
	}
}
