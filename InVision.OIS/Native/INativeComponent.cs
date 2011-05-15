using System;
using System.Runtime.InteropServices;
using InVision.Native.Ext;
using InVision.OIS.Components;

namespace InVision.OIS.Native
{
	[GeneratorType, ValueObject]
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct TmpComponentDescriptor
	{
		private readonly Handle _handle;
		private readonly ComponentType* _ctype;

		/// <summary>
		/// Gets the handle.
		/// </summary>
		/// <value>The handle.</value>
		public Handle Handle
		{
			get { return _handle; }
		}

		/// <summary>
		/// Gets the type of the component.
		/// </summary>
		/// <value>The type of the component.</value>
		public ComponentType ComponentType
		{
			get { return *_ctype; }
		}
	}

	[GeneratorType, FunctionProvider, TargetCppType("Component", Namespace = "OIS")]
	internal interface INativeComponent
	{
		[Constructor]
		TmpComponentDescriptor New();

		[Constructor]
		TmpComponentDescriptor New(ComponentType ctype);

		[Destructor]
		void Delete(Handle handle);
	}
}