using System;
using System.Collections;
using UnityEngine;

namespace Celezt.SaveSystem
{
	/// <summary>
	/// Register content to be saved by the save system. Must call 'RegisterSaveObject()' for it to work.<br/>
	/// By assigning 'partial' before 'class {ClassName}', the source generator will create a partial class that automatically adds all entries <br/>
	/// inside the class with 'SaveAttribute' to the save system.<br/>
	/// <br/>
	/// The class must be derived from <see cref="MonoBehaviour"/> or <see cref="IIdentifiable"/> (if both, will use <see cref="IIdentifiable"/>).
	/// <list type="table">
	/// <item><see cref="MonoBehaviour"/>: Uses 'GetComponentInParent' to find <see cref="IIdentifiable"/> or <see cref="SaveBehaviour"/>.</item>
	/// <item><see cref="IIdentifiable"/>: Requires the user to implement their own <see cref="Guid"/> ID.</item>
	/// </list>
	/// The name of a variable/property/method is used as ID by being converted to snake_case. 'Get' and 'Set' in front of a method name is ignored.<br/>
	/// There can only be one get and set per ID. Entries with the same ID (e.g. int intValue, int GetIntValue()) is overwritten based on priority:
	/// <list type="number">
	/// <item>Method</item>
	/// <item>Property</item>
	/// <item>Field</item>
	/// </list> 
	/// <b>User Code:</b>
	/// <code>
	/// public partial class Example : MonoBehaviour
	/// {
	///		[Save]
	///		private int _exampleValue;
	///		
	///		[Save]
	///		private void SetExampleValue(int value) => _exampleValue = value;
	///		
	///		private void Awake()
	///		{
	///			RegisterSaveObject();
	///		}
	/// }
	/// </code>
	/// <b>Source Generator:</b>
	/// <code>
	///	public partial class Example
	///	{
	///		/// ... ///
	///		protected void RegisterSaveObject()
	///		{
	///			global::Celezt.SaveSystem.SaveSystem.GetEntryKey(this)
	///				.SetSubEntry("example_value", 
	///					() => _exampleValue, 
	///					value => SetExampleValue((int)value));
	///		}
	///	}
	/// </code>
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public class SaveAttribute : Attribute
	{
		/// <summary>
		/// Converts to snake_case.
		/// </summary>
		public string? Identifier { get; set; }
		public SaveSetting Setting => _setting;

		private SaveSetting _setting;

		public SaveAttribute(SaveSetting setting = SaveSetting.Default)
		{
			_setting = setting;
		}
	}

	public enum SaveSetting
	{
		/// <summary>
		/// Removes if the instance owner is destroyed. 
		/// </summary>
		Default,
		/// <summary>
		/// Keeps the save alive even when the instance owner is destroyed.
		/// </summary>
		Persistent,
	}
}