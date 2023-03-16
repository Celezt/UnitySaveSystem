using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Celezt.SaveSystem.Utilities
{
	/// <summary>
	/// <see cref="https://forum.unity.com/threads/how-to-assign-matrix4x4-to-transform.121966/"/>
	/// </summary>
	internal static class MatrixExtension
	{
		/// <summary>
		/// Identity quaternion.
		/// </summary>
		/// <remarks>
		/// <para>It is faster to access this variation than <c>Quaternion.identity</c>.</para>
		/// </remarks>
		public static readonly Quaternion IdentityQuaternion = Quaternion.identity;
		/// <summary>
		/// Identity matrix.
		/// </summary>
		/// <remarks>
		/// <para>It is faster to access this variation than <c>Matrix4x4.identity</c>.</para>
		/// </remarks>
		public static readonly Matrix4x4 IdentityMatrix = Matrix4x4.identity;

		/// <summary>
		/// Extract translation from transform matrix.
		/// </summary>
		/// <param name="matrix">Transform matrix. This parameter is passed by reference
		/// to improve performance; no changes will be made to it.</param>
		/// <returns>
		/// Translation offset.
		/// </returns>
		public static Vector3 GetTranslation(this ref Matrix4x4 matrix) =>
			new Vector3
			{
				z = matrix.m23,
				y = matrix.m13,
				x = matrix.m03,
			};

		/// <summary>
		/// Extract rotation quaternion from transform matrix.
		/// </summary>
		/// <param name="matrix">Transform matrix. This parameter is passed by reference
		/// to improve performance; no changes will be made to it.</param>
		/// <returns>
		/// Quaternion representation of rotation transform.
		/// </returns>
		public static Quaternion GetRotation(this ref Matrix4x4 matrix)
		{
			Vector3 forward = new Vector3
			{
				x = matrix.m02,
				y = matrix.m12,
				z = matrix.m22,
			};

			Vector3 upwards = new Vector3
			{
				x = matrix.m01,
				y = matrix.m11,
				z = matrix.m21,
			};

			return Quaternion.LookRotation(forward, upwards);
		}

		/// <summary>
		/// Extract scale from transform matrix.
		/// </summary>
		/// <param name="matrix">Transform matrix. This parameter is passed by reference
		/// to improve performance; no changes will be made to it.</param>
		/// <returns>
		/// Scale vector.
		/// </returns>
		public static Vector3 GetScale(this ref Matrix4x4 matrix) =>
			new Vector3
			{
				x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude,
				y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude,
				z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude,
			};

		/// <summary>
		/// Extract position, rotation and scale from TRS matrix.
		/// </summary>
		/// <param name="matrix">Transform matrix. This parameter is passed by reference
		/// to improve performance; no changes will be made to it.</param>
		/// <param name="localPosition">Output position.</param>
		/// <param name="localRotation">Output rotation.</param>
		/// <param name="localScale">Output scale.</param>
		public static void Decompose(this ref Matrix4x4 matrix, out Vector3 localPosition, out Quaternion localRotation, out Vector3 localScale)
		{
			localPosition = GetTranslation(ref matrix);
			localRotation = GetRotation(ref matrix);
			localScale = GetScale(ref matrix);
		}

		/// <summary>
		/// Set transform component from TRS matrix.
		/// </summary>
		/// <param name="transform">Transform component.</param>
		/// <param name="matrix">Transform matrix. This parameter is passed by reference
		/// to improve performance; no changes will be made to it.</param>
		public static void SetFromMatrix(this Transform transform, ref Matrix4x4 matrix)
		{
			transform.localPosition = GetTranslation(ref matrix);
			transform.localRotation = GetRotation(ref matrix);
			transform.localScale = GetScale(ref matrix);
		}

		/// <summary>
		/// Set transform component from TRS matrix.
		/// </summary>
		/// <param name="transform">Transform component.</param>
		/// <param name="matrix">Transform matrix. This parameter is passed by reference
		/// to improve performance; no changes will be made to it.</param>
		public static void SetFromMatrix(this Transform transform, Matrix4x4 matrix)
		{
			transform.localPosition = GetTranslation(ref matrix);
			transform.localRotation = GetRotation(ref matrix);
			transform.localScale = GetScale(ref matrix);
		}

		/// <summary>
		/// Get translation matrix.
		/// </summary>
		/// <param name="offset">Translation offset.</param>
		/// <returns>
		/// The translation transform matrix.
		/// </returns>
		public static Matrix4x4 Translation(this Vector3 offset)
		{
			Matrix4x4 matrix = IdentityMatrix;
			matrix.m03 = offset.x;
			matrix.m13 = offset.y;
			matrix.m23 = offset.z;
			return matrix;
		}
	}
}
