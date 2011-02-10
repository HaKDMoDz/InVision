﻿using System;
using System.Runtime.InteropServices;

namespace InVision.Ogre3D.Native
{
	internal sealed class NativeSceneManager : PlatformInvoke
	{
		[DllImport(Library, EntryPoint = "ScnMngrDelete")]
		public static extern void Delete(IntPtr pSceneManager);

		[DllImport(Library, EntryPoint = "ScnMngrCreateCamera")]
		public static extern IntPtr _CreateCamera(IntPtr pSceneManager, string name);

		/// <summary>
		/// 	Creates the camera.
		/// </summary>
		/// <param name = "pSceneManager">The p scene manager.</param>
		/// <param name = "name">The name.</param>
		/// <returns></returns>
		public static Camera CreateCamera(IntPtr pSceneManager, string name)
		{
			return _CreateCamera(pSceneManager, name).
				AsHandle(pCamera => new Camera(pCamera));
		}
	}
}