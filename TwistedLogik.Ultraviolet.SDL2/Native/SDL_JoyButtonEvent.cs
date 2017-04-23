﻿using System;
using System.Runtime.InteropServices;
using Ultraviolet.Core;
using SDL_JoystickID = System.Int32;

#pragma warning disable 1591

namespace TwistedLogik.Ultraviolet.SDL2.Native
{
    [Preserve]
    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_JoyButtonEvent
    {
        public UInt32 type;
        public UInt32 timestamp;
        public SDL_JoystickID which;
        public Byte button;
        public Byte state;
        public Byte padding1;
        public Byte padding2;
    }
}
