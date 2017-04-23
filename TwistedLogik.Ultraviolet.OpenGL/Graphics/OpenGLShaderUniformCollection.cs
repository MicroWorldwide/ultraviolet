﻿using System;
using System.Collections.Generic;
using Ultraviolet.Core;

namespace TwistedLogik.Ultraviolet.OpenGL.Graphics
{
    /// <summary>
    /// Represents a collection of shader uniforms.
    /// </summary>
    public sealed class OpenGLShaderUniformCollection : UltravioletNamedCollection<OpenGLShaderUniform>
    {
        /// <summary>
        /// Initializes a new instance of the OpenGLShaderUniformCollection class.
        /// </summary>
        [Preserve]
        public OpenGLShaderUniformCollection(IEnumerable<OpenGLShaderUniform> uniforms)
        {
            Contract.Require(uniforms, nameof(uniforms));

            foreach (var uniform in uniforms)
                AddInternal(uniform);
        }

        /// <summary>
        /// Gets the specified item's name.
        /// </summary>
        /// <param name="item">The item for which to retrieve a name.</param>
        /// <returns>The specified item's name.</returns>
        [Preserve]
        protected override String GetName(OpenGLShaderUniform item)
        {
            return item.Name;
        }
    }
}
