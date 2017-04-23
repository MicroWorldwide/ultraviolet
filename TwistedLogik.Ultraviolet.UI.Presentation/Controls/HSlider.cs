﻿using System;
using Ultraviolet.Core;

namespace TwistedLogik.Ultraviolet.UI.Presentation.Controls
{
    /// <summary>
    /// Represents a horizontal slider.
    /// </summary>
    [Preserve(AllMembers = true)]
    [UvmlKnownType(null, "TwistedLogik.Ultraviolet.UI.Presentation.Controls.Templates.HSlider.xml")]
    public class HSlider : OrientedSlider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HSlider"/> class.
        /// </summary>
        /// <param name="uv">The Ultraviolet context.</param>
        /// <param name="name">The element's identifying name within its namescope.</param>
        public HSlider(UltravioletContext uv, String name)
            : base(uv, name)
        {

        }
    }
}
