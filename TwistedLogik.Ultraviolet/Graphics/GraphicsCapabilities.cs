﻿using System;

namespace TwistedLogik.Ultraviolet.Graphics
{
    /// <summary>
    /// Encapsulates the capabilities of a graphics device.
    /// </summary>
    public abstract class GraphicsCapabilities
    {
        /// <summary>
        /// Gets a value indicating whether the device supports texture which have both depth and stencil components.
        /// </summary>
        public abstract Boolean SupportsDepthStencilTextures { get; }

        /// <summary>
        /// Gets the maximum texture size supported by the device.
        /// </summary>
        public abstract Int32 MaximumTextureSize { get; }

        /// <summary>
        /// Gets the maximum supported viewport width.
        /// </summary>
        public abstract Int32 MaximumViewportWidth { get; }

        /// <summary>
        /// Gets the maximum supported viewport height.
        /// </summary>
        public abstract Int32 MaximumViewportHeight { get; }
    }
}
