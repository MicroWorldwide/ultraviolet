﻿using System;
using System.IO;
using Ultraviolet.Core;
using TwistedLogik.Ultraviolet.Content;

namespace TwistedLogik.Ultraviolet.OpenGL.Graphics
{
    /// <summary>
    /// Imports .vert files.
    /// </summary>
    [Preserve(AllMembers = true)]
    [ContentImporter(".vert")]
    public sealed class OpenGLVertexShaderImporter : ContentImporter<String>
    {
        /// <summary>
        /// Imports the data from the specified file.
        /// </summary>
        /// <param name="metadata">The asset metadata for the asset to import.</param>
        /// <param name="stream">The stream that contains the data to import.</param>
        /// <returns>The data structure that was imported from the file.</returns>
        public override String Import(IContentImporterMetadata metadata, Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
