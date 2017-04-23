﻿using System;
using Ultraviolet.Core;

namespace TwistedLogik.Gluon
{
    public static unsafe partial class gl
    {
        [MonoNativeFunctionWrapper]
        private delegate void glVertexAttribLPointerDelegate(uint index, int size, uint type, int stride, IntPtr pointer);
        [Require(MinVersion = "4.1")]
        private static readonly glVertexAttribLPointerDelegate glVertexAttribLPointer = null;

        public static void VertexAttribLPointer(uint index, int size, uint type, int stride, void* pointer) { glVertexAttribLPointer(index, size, type, stride, (IntPtr)pointer); }        
    }
}
