﻿using System;
using System.Runtime.InteropServices;

namespace InVision.Input
{
	[StructLayout(LayoutKind.Sequential)]
	public struct ButtonComponent : IButtonComponent, IHandleHolder
	{
		private readonly IntPtr handle;
		private readonly ComponentType componentType;
		private readonly bool isPushed;

		/// <summary>
		/// 	Initializes a new instance of the <see cref = "ButtonComponent" /> struct.
		/// </summary>
		/// <param name = "handle">The handle.</param>
		/// <param name = "componentType">Type of the component.</param>
		/// <param name = "isPushed">if set to <c>true</c> [is pushed].</param>
		internal ButtonComponent(IntPtr handle, ComponentType componentType, bool isPushed)
		{
			this.handle = handle;
			this.componentType = componentType;
			this.isPushed = isPushed;
		}

		/// <summary>
		/// 	Initializes a new instance of the <see cref = "ButtonComponent" /> struct.
		/// </summary>
		/// <param name = "isPushed">if set to <c>true</c> [is pushed].</param>
		/// <param name = "componentType">Type of the component.</param>
		public ButtonComponent(ComponentType componentType, bool isPushed)
		{
			handle = IntPtr.Zero;
			this.componentType = componentType;
			this.isPushed = isPushed;
		}

		#region IButton Members

		/// <summary>
		/// 	Gets the type of the component.
		/// </summary>
		/// <value>The type of the component.</value>
		public ComponentType ComponentType
		{
			get { return componentType; }
		}

		/// <summary>
		/// 	Gets a value indicating whether this instance is pushed.
		/// </summary>
		/// <value><c>true</c> if this instance is pushed; otherwise, <c>false</c>.</value>
		public bool IsPushed
		{
			get { return isPushed; }
		}

		#endregion

		#region IHandleHolder Members

		/// <summary>
		/// 	Gets the handle.
		/// </summary>
		/// <value>The handle.</value>
		IntPtr IHandleHolder.Handle
		{
			get { return handle; }
		}

		#endregion
	}
}