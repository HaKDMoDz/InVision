﻿using System;
using System.Runtime.InteropServices;
using InVision.Native.Ext;

namespace InVision.OIS.Native
{
    [CppValueObject]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct MouseStateDescriptor
    {
        public Handle Self;
        public int* Width;
        public int* Height;
        public int* Buttons;
        public AxisDescriptor X;
        public AxisDescriptor Y;
        public AxisDescriptor Z;
    }
}