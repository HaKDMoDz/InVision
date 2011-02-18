﻿using System;

namespace InVision.Input
{
	internal interface IHandleHolder
	{
		/// <summary>
		/// 	Gets the handle.
		/// </summary>
		/// <value>The handle.</value>
		IntPtr Handle { get; }
	}
}