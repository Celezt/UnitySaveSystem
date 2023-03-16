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
	internal static class GuidExtension
	{
		public static Guid Xor(this Guid a, Guid b)
		{
			Span<long> spanA = MemoryMarshal.CreateSpan(ref UnsafeUtility.As<Guid, long>(ref a), 2);
			Span<long> spanB = MemoryMarshal.CreateSpan(ref UnsafeUtility.As<Guid, long>(ref b), 2);

			spanB[0] ^= spanA[0];
			spanB[1] ^= spanA[1];

			return b;
		}

		public static Guid Generate(string input)
		{
			using (MD5 md5 = MD5.Create())
			{
				Span<byte> hash = stackalloc byte[16];
				md5.TryComputeHash(Encoding.UTF8.GetBytes(input), hash, out _);
				return new Guid(hash);
			}
		}
	}
}
