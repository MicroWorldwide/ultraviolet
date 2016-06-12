﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using CoreGraphics;
using Foundation;
using TwistedLogik.Nucleus;
using TwistedLogik.Ultraviolet.Graphics;
using UIKit;

namespace TwistedLogik.Ultraviolet.iOS.Graphics
{
    /// <summary>
    /// Represents an implementation of the <see cref="SurfaceSource"/> class for the iOS platform.
    /// </summary>
    public sealed unsafe class iOSSurfaceSource : SurfaceSource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="iOSSurfaceSource"/> class.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> that contains the surface data.</param>
        public iOSSurfaceSource(Stream stream)
        {
            Contract.Require(stream, nameof(stream));

            using (var data = NSData.FromStream(stream))
            {
                using (var img = UIImage.LoadFromData(data))
                {
                    this.width = (Int32)img.Size.Width;
                    this.height = (Int32)img.Size.Height;
                    this.stride = (Int32)img.CGImage.BytesPerRow;

                    this.bmpData = Marshal.AllocHGlobal(stride * height);

                    using (var colorSpace = CGColorSpace.CreateDeviceRGB())
                    {
                        using (var bmp = new CGBitmapContext(bmpData, width, height, 8, stride, colorSpace, CGImageAlphaInfo.PremultipliedLast))
                        {
                            bmp.DrawImage(new CGRect(0, 0, width, height), img.CGImage);
                        }
                    }
                }
            }

            ReversePremultiplication();
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public override Color this[int x, int y]
        {
            get
            {
                Contract.EnsureNotDisposed(this, disposed);

                unsafe
                {
                    var pixel = ((Byte*)bmpData.ToPointer())[y * stride + (x * sizeof(UInt32))];
                    return Color.FromRgba(*(UInt32*)pixel);
                }
            }
        }

        /// <inheritdoc/>
        public override IntPtr Data
        {
            get
            {
                Contract.EnsureNotDisposed(this, disposed);

                return bmpData;
            }
        }

        /// <inheritdoc/>
        public override Int32 Stride
        {
            get
            {
                Contract.EnsureNotDisposed(this, disposed);

                return stride;
            }
        }

        /// <inheritdoc/>
        public override Int32 Width
        {
            get
            {
                Contract.EnsureNotDisposed(this, disposed);

                return width;
            }
        }

        /// <inheritdoc/>
        public override Int32 Height
        {
            get
            {
                Contract.EnsureNotDisposed(this, disposed);

                return height;
            }
        }

        /// <inheritdoc/>
        public override SurfaceSourceDataFormat DataFormat => SurfaceSourceDataFormat.RGBA;

        /// <summary>
        /// Reverses the premultiplication which is automatically applied by the iOS API's...
        /// ...so that Ultraviolet can re-premultiply it later. Yeah.
        /// </summary>
        private void ReversePremultiplication()
        {
            var pBmpData = (UInt32*)bmpData.ToPointer();
            for (int i = 0; i < width * height; i++)
            {
                var pixel = Color.FromRgba(*pBmpData);
                var a = pixel.A / 255f;
                var r = (Int32)(pixel.R / a);
                var g = (Int32)(pixel.G / a);
                var b = (Int32)(pixel.B / a);
                *pBmpData++ = new Color(r, g, b, a).PackedValue;
            }
        }

        /// <summary>
        /// Releases resources associated with the object.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> if the object is being disposed; <see langword="false"/> if the object is being finalized.</param>
        private void Dispose(Boolean disposing)
        {
            if (disposed)
                return;

            if (bmpData != IntPtr.Zero)
                Marshal.FreeHGlobal(bmpData);

            disposed = true;
        }

        // State values.
        private readonly IntPtr bmpData;
        private readonly Int32 width;
        private readonly Int32 height;
        private readonly Int32 stride;
        private Boolean disposed;
    }
}
