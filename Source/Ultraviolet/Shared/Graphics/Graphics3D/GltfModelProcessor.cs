﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpGLTF.Schema2;
using Ultraviolet.Content;
using Ultraviolet.Graphics.PackedVector;

namespace Ultraviolet.Graphics.Graphics3D
{
    /// <summary>
    /// Represents a content processor which converts <see cref="ModelRoot"/> instances to <see cref="Model"/> instances.
    /// </summary>
    [ContentProcessor, CLSCompliant(false)]
    public class GltfModelProcessor : ContentProcessor<ModelRoot, Model>
    {
        /// <inheritdoc/>
        public override Model Process(ContentManager manager, IContentProcessorMetadata metadata, ModelRoot input)
        {
            var uvTextures = new Dictionary<String, Texture2D>();
            var uvScenes = ProcessScenes(manager, input, uvTextures, out var defaultSceneIndex);
            var uvModel = new Model(manager.Ultraviolet, uvScenes, uvTextures.Values.ToList());

            if (defaultSceneIndex.HasValue)
                uvModel.Scenes.ChangeDefaultScene(defaultSceneIndex.Value);

            return uvModel;
        }

        /// <summary>
        /// Converts a <see cref="SharpGLTF.Schema2.PrimitiveType"/> value to an Ultraviolet <see cref="PrimitiveType"/> value.
        /// </summary>
        private PrimitiveType ConvertPrimitiveType(SharpGLTF.Schema2.PrimitiveType type)
        {
            switch (type)
            {
                case SharpGLTF.Schema2.PrimitiveType.LINES:
                    return PrimitiveType.LineList;

                case SharpGLTF.Schema2.PrimitiveType.LINE_STRIP:
                    return PrimitiveType.LineStrip;

                case SharpGLTF.Schema2.PrimitiveType.TRIANGLES:
                    return PrimitiveType.TriangleList;

                case SharpGLTF.Schema2.PrimitiveType.TRIANGLE_STRIP:
                    return PrimitiveType.TriangleStrip;

                default:
                    throw new NotSupportedException(UltravioletStrings.UnsupportedPrimitiveType.Format(type));
            }
        }

        /// <summary>
        /// Gets the alpha for the specified material.
        /// </summary>
        private static Single GetMaterialAlpha(SharpGLTF.Schema2.Material material)
        {
            if (material.Alpha == AlphaMode.OPAQUE)
                return 1f;

            var baseColor = material.FindChannel("BaseColor");
            if (baseColor == null)
                return 1f;

            return baseColor.Value.Parameter.W;
        }

        /// <summary>
        /// Gets the specular power for the specified material.
        /// </summary>
        private static Single GetMaterialSpecularPower(SharpGLTF.Schema2.Material material)
        {
            var mr = material.FindChannel("MetallicRoughness");
            if (mr == null)
                return 16f;

            var metallic = mr.Value.Parameter.X;
            return 4f + 16f * metallic;
        }

        /// <summary>
        /// Gets the diffuse color for the specified material.
        /// </summary>
        private static Color GetMaterialDiffuseColor(SharpGLTF.Schema2.Material material)
        {
            var diffuse = material.FindChannel("Diffuse") ?? material.FindChannel("BaseColor");
            if (diffuse == null)
                return Color.White;

            return new Color(diffuse.Value.Parameter.X, diffuse.Value.Parameter.Y, diffuse.Value.Parameter.Z);
        }

        /// <summary>
        /// Gets the emissive color for the specified material.
        /// </summary>
        private static Color GetMaterialEmissiveColor(SharpGLTF.Schema2.Material material)
        {
            var emissive = material.FindChannel("Emissive");
            if (emissive == null)
                return Color.Black;

            return new Color(emissive.Value.Parameter.X, emissive.Value.Parameter.Y, emissive.Value.Parameter.Z);
        }

        /// <summary>
        /// Gets the specular color for the specified material.
        /// </summary>
        private static Color GetMaterialSpecularColor(SharpGLTF.Schema2.Material material)
        {
            var mr = material.FindChannel("MetallicRoughness");
            if (mr == null)
                return Color.White;

            var diffuse = GetMaterialDiffuseColor(material).ToVector3();
            var metallic = mr.Value.Parameter.X;
            var roughness = mr.Value.Parameter.Y;

            var k = Vector3.Zero;
            k += Vector3.Lerp(diffuse, Vector3.Zero, roughness);
            k += Vector3.Lerp(diffuse, Vector3.One, metallic);
            k *= 0.5f;

            return new Color(k);
        }

        /// <summary>
        /// Gets the texture for the specified material.
        /// </summary>
        private static Texture2D GetMaterialTexture(ContentManager contentManager, SharpGLTF.Schema2.Material material, Dictionary<String, Texture2D> textures)
        {
            var diffuse = material.FindChannel("Diffuse") ?? material.FindChannel("BaseColor");
            if (diffuse == null)
                return null;

            var texture = diffuse.Value.Texture;
            if (texture != null)
            {
                var textureName = $"{diffuse.Value.LogicalParent.Name ?? "null"}-{diffuse.Value.Key}";
                var textureContent = texture.PrimaryImage?.Content;
                if (textureContent != null && textureContent.Value.IsValid)
                {
                    if (!textures.TryGetValue(textureName, out var textureResource))
                    {
                        using (var textureStream = textureContent.Value.Open())
                        {
                            textureResource = contentManager.LoadFromStream<Texture2D>(textureStream, textureContent.Value.FileExtension);
                        }
                        textures[textureName] = textureResource;
                    }
                    return textureResource;
                }
            }

            return null;
        }

        /// <summary>
        /// Processes the specified node mesh.
        /// </summary>
        private ModelMesh ProcessNodeMesh(ContentManager contentManager, Mesh mesh, Dictionary<String, Texture2D> textures)
        {
            if (mesh == null)
                return null;

            var uvModelMeshGeometries = new List<ModelMeshGeometry>();
            foreach (var primitive in mesh.Primitives)
            {
                var uvPrimitiveGeometryStream = GeometryStream.Create();
                var vCount = AttachVertexBuffer(primitive, uvPrimitiveGeometryStream);
                var iCount = AttachIndexBuffer(primitive, uvPrimitiveGeometryStream);

                var uvPrimitiveMaterial = new BasicMaterial
                {
                    Alpha = GetMaterialAlpha(primitive.Material),
                    DiffuseColor = GetMaterialDiffuseColor(primitive.Material),
                    SpecularPower = GetMaterialSpecularPower(primitive.Material),
                    SpecularColor = GetMaterialSpecularColor(primitive.Material),
                    EmissiveColor = GetMaterialEmissiveColor(primitive.Material),
                    Texture = GetMaterialTexture(contentManager, primitive.Material, textures)
                };

                var uvPrimitiveType = ConvertPrimitiveType(primitive.DrawPrimitiveType);
                var uvPrimitiveGeometry = new ModelMeshGeometry(uvPrimitiveType, uvPrimitiveGeometryStream, vCount, iCount, uvPrimitiveMaterial);
                uvModelMeshGeometries.Add(uvPrimitiveGeometry);
            }

            return new ModelMesh(mesh.Name, uvModelMeshGeometries);
        }

        /// <summary>
        /// Gets a <see cref="VertexElementFormat"/> instance which represents the specified accessor.
        /// </summary>
        private static VertexElementFormat GetVertexElementFormatFromAccessor(Accessor accessor, VertexElementUsage usage, out Int32 sizeInBytes)
        {
            if (usage == VertexElementUsage.Color)
            {
                sizeInBytes = sizeof(UInt32);
                return VertexElementFormat.Color;
            }

            var format = accessor.Format;
            switch (format.Encoding)
            {
                case EncodingType.BYTE:
                    switch (format.Dimensions)
                    {
                        case DimensionType.SCALAR:
                            sizeInBytes = sizeof(SByte);
                            return format.Normalized ? VertexElementFormat.NormalizedSByte : VertexElementFormat.SByte;

                        case DimensionType.VEC2:
                            sizeInBytes = sizeof(SByte) * 2;
                            return format.Normalized ? VertexElementFormat.NormalizedSByte2 : VertexElementFormat.SByte2;

                        case DimensionType.VEC3:
                            sizeInBytes = sizeof(SByte) * 3;
                            return format.Normalized ? VertexElementFormat.NormalizedSByte3 : VertexElementFormat.SByte3;

                        case DimensionType.VEC4:
                            sizeInBytes = sizeof(SByte) * 4;
                            return format.Normalized ? VertexElementFormat.NormalizedSByte4 : VertexElementFormat.SByte4;

                        default:
                            throw new NotSupportedException(UltravioletStrings.UnsupportedVertexAccessorFormat.Format(format.Encoding, format.Dimensions));
                    }

                case EncodingType.UNSIGNED_BYTE:
                    switch (format.Dimensions)
                    {
                        case DimensionType.SCALAR:
                            sizeInBytes = sizeof(Byte);
                            return format.Normalized ? VertexElementFormat.NormalizedByte : VertexElementFormat.Byte;

                        case DimensionType.VEC2:
                            sizeInBytes = sizeof(Byte) * 2;
                            return format.Normalized ? VertexElementFormat.NormalizedByte2 : VertexElementFormat.Byte2;

                        case DimensionType.VEC3:
                            sizeInBytes = sizeof(Byte) * 3;
                            return format.Normalized ? VertexElementFormat.NormalizedByte3 : VertexElementFormat.Byte3;

                        case DimensionType.VEC4:
                            sizeInBytes = sizeof(Byte) * 4;
                            return format.Normalized ? VertexElementFormat.NormalizedByte4 : VertexElementFormat.Byte4;

                        default:
                            throw new NotSupportedException(UltravioletStrings.UnsupportedVertexAccessorFormat.Format(format.Encoding, format.Dimensions));
                    }

                case EncodingType.SHORT:
                    switch (format.Dimensions)
                    {
                        case DimensionType.SCALAR:
                            sizeInBytes = sizeof(Int16);
                            return format.Normalized ? VertexElementFormat.NormalizedShort : VertexElementFormat.Short;

                        case DimensionType.VEC2:
                            sizeInBytes = sizeof(Int16) * 2;
                            return format.Normalized ? VertexElementFormat.NormalizedShort2 : VertexElementFormat.Short2;

                        case DimensionType.VEC3:
                            sizeInBytes = sizeof(Int16) * 3;
                            return format.Normalized ? VertexElementFormat.NormalizedShort3 : VertexElementFormat.Short3;

                        case DimensionType.VEC4:
                            sizeInBytes = sizeof(Int16) * 4;
                            return format.Normalized ? VertexElementFormat.NormalizedShort4 : VertexElementFormat.Short4;

                        default:
                            throw new NotSupportedException(UltravioletStrings.UnsupportedVertexAccessorFormat.Format(format.Encoding, format.Dimensions));
                    }

                case EncodingType.UNSIGNED_SHORT:
                    switch (format.Dimensions)
                    {
                        case DimensionType.SCALAR:
                            sizeInBytes = sizeof(UInt16);
                            return format.Normalized ? VertexElementFormat.NormalizedUnsignedShort : VertexElementFormat.UnsignedShort;

                        case DimensionType.VEC2:
                            sizeInBytes = sizeof(UInt16) * 2;
                            return format.Normalized ? VertexElementFormat.NormalizedUnsignedShort2 : VertexElementFormat.UnsignedShort2;

                        case DimensionType.VEC3:
                            sizeInBytes = sizeof(UInt16) * 3;
                            return format.Normalized ? VertexElementFormat.NormalizedUnsignedShort3 : VertexElementFormat.UnsignedShort3;

                        case DimensionType.VEC4:
                            sizeInBytes = sizeof(UInt16) * 4;
                            return format.Normalized ? VertexElementFormat.NormalizedUnsignedShort4 : VertexElementFormat.UnsignedShort4;

                        default:
                            throw new NotSupportedException(UltravioletStrings.UnsupportedVertexAccessorFormat.Format(format.Encoding, format.Dimensions));
                    }


                case EncodingType.UNSIGNED_INT:
                    switch (format.Dimensions)
                    {
                        case DimensionType.SCALAR:
                            sizeInBytes = sizeof(Int32);
                            return format.Normalized ? VertexElementFormat.Int : VertexElementFormat.NormalizedInt;

                        case DimensionType.VEC2:
                            sizeInBytes = sizeof(Int32) * 2;
                            return format.Normalized ? VertexElementFormat.Int2 : VertexElementFormat.NormalizedInt2;

                        case DimensionType.VEC3:
                            sizeInBytes = sizeof(Int32) * 3;
                            return format.Normalized ? VertexElementFormat.Int3 : VertexElementFormat.NormalizedInt3;

                        case DimensionType.VEC4:
                            sizeInBytes = sizeof(Int32) * 4;
                            return format.Normalized ? VertexElementFormat.Int4 : VertexElementFormat.NormalizedInt4;

                        default:
                            throw new NotSupportedException(UltravioletStrings.UnsupportedVertexAccessorFormat.Format(format.Encoding, format.Dimensions));
                    }

                case EncodingType.FLOAT:
                    switch (format.Dimensions)
                    {
                        case DimensionType.SCALAR:
                            sizeInBytes = sizeof(Single);
                            return VertexElementFormat.Single;

                        case DimensionType.VEC2:
                            sizeInBytes = sizeof(Single) * 2;
                            return VertexElementFormat.Vector2;

                        case DimensionType.VEC3:
                            sizeInBytes = sizeof(Single) * 3;
                            return VertexElementFormat.Vector3;

                        case DimensionType.VEC4:
                            sizeInBytes = sizeof(Single) * 4;
                            return VertexElementFormat.Vector4;

                        default:
                            throw new NotSupportedException(UltravioletStrings.UnsupportedVertexAccessorFormat.Format(format.Encoding, format.Dimensions));
                    }

                default:
                    throw new NotSupportedException(UltravioletStrings.UnsupportedVertexAccessorFormat.Format(format.Encoding, format.Dimensions));
            }
        }

        /// <summary>
        /// Gets a <see cref="VertexElementUsage"/> instance which represents the specified accessor.
        /// </summary>
        private static VertexElementUsage? GetVertexElementUsageFromAccessor(String attributeName, out Int32 index)
        {
            if (String.Equals("POSITION", attributeName, StringComparison.OrdinalIgnoreCase))
            {
                index = 0;
                return VertexElementUsage.Position;
            }
            
            if (String.Equals("NORMAL", attributeName, StringComparison.OrdinalIgnoreCase))
            {
                index = 0;
                return VertexElementUsage.Normal;
            }

            if (String.Equals("TANGENT", attributeName, StringComparison.OrdinalIgnoreCase))
            {
                index = 0;
                return VertexElementUsage.Tangent;
            }

            if (attributeName.StartsWith("TEXCOORD_", StringComparison.OrdinalIgnoreCase))
            {
                if (!Int32.TryParse(attributeName.Substring("TEXCOORD_".Length), out index))
                    throw new InvalidDataException(UltravioletStrings.InvalidVertexAttributeName.Format(attributeName));

                return VertexElementUsage.TextureCoordinate;
            }

            if (attributeName.StartsWith("COLOR_", StringComparison.OrdinalIgnoreCase))
            {
                if (!Int32.TryParse(attributeName.Substring("COLOR_".Length), out index))
                    throw new InvalidDataException(UltravioletStrings.InvalidVertexAttributeName.Format(attributeName));

                return VertexElementUsage.Color;
            }

            if (attributeName.StartsWith("JOINTS_", StringComparison.OrdinalIgnoreCase))
            {
                if (!Int32.TryParse(attributeName.Substring("JOINTS_".Length), out index))
                    throw new InvalidDataException(UltravioletStrings.InvalidVertexAttributeName.Format(attributeName));

                return VertexElementUsage.BlendIndices;
            }

            if (attributeName.StartsWith("WEIGHTS_", StringComparison.OrdinalIgnoreCase))
            {
                if (!Int32.TryParse(attributeName.Substring("WEIGHTS_".Length), out index))
                    throw new InvalidDataException(UltravioletStrings.InvalidVertexAttributeName.Format(attributeName));

                return VertexElementUsage.BlendWeight;
            }

            index = 0;
            return null;
        }

        /// <summary>
        /// Creates a <see cref="VertexElement"/> instance from the specified accessor.
        /// </summary>
        private static VertexElement? CreateVertexElementFromAccessor(String attributeName, Accessor accessor, ref Int32 position)
        {
            var usage = GetVertexElementUsageFromAccessor(attributeName, out var index);
            if (usage == null)
                return null;

            var format = GetVertexElementFormatFromAccessor(accessor, usage.Value, out var sizeInBytes);
            var result = new VertexElement(position, format, usage.Value, index);
            position += sizeInBytes;

            return result;
        }
        
        /// <summary>
        /// Populates a vertex data with the specified attribute data.
        /// </summary>
        private static void PopulateVertexBufferAttributeData<TSrc, TDst>(VertexBuffer vBuffer, KeyValuePair<String, Accessor> accessor, Int32 position, Int32 size,
            Func<Accessor, IList<TSrc>> getter, Func<TSrc, TDst> converter) 
            where TSrc : unmanaged
            where TDst : unmanaged
        {
            var data = getter(accessor.Value);
            var offset = 0;
            for (var i = 0; i < accessor.Value.Count; i++)
            {
                unsafe
                {
                    var vdata = converter(data[i]);
                    vBuffer.SetRawData((IntPtr)(&vdata), 0, offset + position, size, SetDataOptions.NoOverwrite);
                    offset += vBuffer.VertexDeclaration.VertexStride;
                }
            }
        }

        /// <summary>
        /// Attaches a vertex buffer representing the specified primitive's vertices to the specified geometry stream.
        /// </summary>
        private Int32 AttachVertexBuffer(MeshPrimitive primitive, GeometryStream geometryStream)
        {
            if (!primitive.VertexAccessors.Any())
                return 0;

            var vCount = primitive.VertexAccessors.First().Value.Count;
            var vElements = new List<VertexElement>();
            var vElementsByAttribute = new Dictionary<String, VertexElement>();
            var vElementPosition = 0;

            foreach (var accessor in primitive.VertexAccessors)
            {
                var vElement = CreateVertexElementFromAccessor(accessor.Key, accessor.Value, ref vElementPosition);
                if (vElement == null)
                    continue;

                vElementsByAttribute[accessor.Key] = vElement.Value;
                vElements.Add(vElement.Value);
            }

            var vDecl = new VertexDeclaration(vElements);
            var vBuffer = VertexBuffer.Create(vDecl, vCount);

            foreach (var accessor in primitive.VertexAccessors)
            {
                var vElement = vElementsByAttribute[accessor.Key];
                switch (vElement.Format)
                {
                    case VertexElementFormat.Color:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Byte) * 4,
                            x => x.AsColorArray(), x => new Color(x.X, x.Y, x.Z, x.W));
                        break;

                    case VertexElementFormat.SByte:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(SByte),
                            x => x.AsScalarArray(), x => new SByte1(x));
                        break;

                    case VertexElementFormat.SByte2:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(SByte) * 2,
                            x => x.AsVector2Array(), x => new SByte2(x));
                        break;

                    case VertexElementFormat.SByte3:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(SByte) * 3,
                            x => x.AsVector3Array(), x => new SByte3(x));
                        break;

                    case VertexElementFormat.SByte4:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(SByte) * 4,
                            x => x.AsVector4Array(), x => new SByte4(x));
                        break;

                    case VertexElementFormat.NormalizedSByte:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(SByte),
                            x => x.AsScalarArray(), x => new NormalizedSByte1(x));
                        break;

                    case VertexElementFormat.NormalizedSByte2:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(SByte) * 2,
                            x => x.AsVector2Array(), x => new NormalizedSByte2(x));
                        break;

                    case VertexElementFormat.NormalizedSByte3:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(SByte) * 3,
                            x => x.AsVector3Array(), x => new NormalizedSByte3(x));
                        break;

                    case VertexElementFormat.NormalizedSByte4:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(SByte) * 4,
                            x => x.AsVector4Array(), x => new NormalizedSByte4(x));
                        break;

                    case VertexElementFormat.Byte:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Byte),
                            x => x.AsScalarArray(), x => new Byte1(x));
                        break;

                    case VertexElementFormat.Byte2:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Byte) * 2,
                            x => x.AsVector2Array(), x => new Byte2(x));
                        break;

                    case VertexElementFormat.Byte3:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Byte) * 3,
                            x => x.AsVector3Array(), x => new Byte3(x));
                        break;

                    case VertexElementFormat.Byte4:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Byte) * 4,
                            x => x.AsVector4Array(), x => new Byte4(x));
                        break;

                    case VertexElementFormat.NormalizedByte:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Byte),
                            x => x.AsScalarArray(), x => new NormalizedByte1(x));
                        break;

                    case VertexElementFormat.NormalizedByte2:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Byte) * 2,
                            x => x.AsVector2Array(), x => new NormalizedByte2(x));
                        break;

                    case VertexElementFormat.NormalizedByte3:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Byte) * 3,
                            x => x.AsVector3Array(), x => new NormalizedByte3(x));
                        break;

                    case VertexElementFormat.NormalizedByte4:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Byte) * 4,
                            x => x.AsVector4Array(), x => new NormalizedByte4(x));
                        break;

                    case VertexElementFormat.Short:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Int16),
                            x => x.AsScalarArray(), x => new Short1(x));
                        break;

                    case VertexElementFormat.Short2:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Int16) * 2,
                            x => x.AsVector2Array(), x => new Short2(x));
                        break;

                    case VertexElementFormat.Short3:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Int16) * 3,
                            x => x.AsVector3Array(), x => new Short3(x));
                        break;

                    case VertexElementFormat.Short4:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Int16) * 4,
                            x => x.AsVector4Array(), x => new Short4(x));
                        break;

                    case VertexElementFormat.NormalizedShort:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Int16),
                            x => x.AsScalarArray(), x => new NormalizedShort1(x));
                        break;

                    case VertexElementFormat.NormalizedShort2:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Int16) * 2,
                            x => x.AsVector2Array(), x => new NormalizedShort2(x));
                        break;

                    case VertexElementFormat.NormalizedShort3:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Int16) * 3,
                            x => x.AsVector3Array(), x => new NormalizedShort3(x));
                        break;

                    case VertexElementFormat.NormalizedShort4:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Int16) * 4,
                            x => x.AsVector4Array(), x => new NormalizedShort4(x));
                        break;

                    case VertexElementFormat.UnsignedShort:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(UInt16),
                            x => x.AsScalarArray(), x => new UnsignedShort1(x));
                        break;

                    case VertexElementFormat.UnsignedShort2:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(UInt16) * 2,
                            x => x.AsVector2Array(), x => new UnsignedShort2(x));
                        break;

                    case VertexElementFormat.UnsignedShort3:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(UInt16) * 3,
                            x => x.AsVector3Array(), x => new UnsignedShort3(x));
                        break;

                    case VertexElementFormat.UnsignedShort4:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(UInt16) * 4,
                            x => x.AsVector4Array(), x => new UnsignedShort4(x));
                        break;

                    case VertexElementFormat.NormalizedUnsignedShort:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(UInt16),
                            x => x.AsScalarArray(), x => new NormalizedUnsignedShort1(x));
                        break;

                    case VertexElementFormat.NormalizedUnsignedShort2:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(UInt16) * 2,
                            x => x.AsVector2Array(), x => new NormalizedUnsignedShort2(x));
                        break;

                    case VertexElementFormat.NormalizedUnsignedShort3:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(UInt16) * 3,
                            x => x.AsVector3Array(), x => new NormalizedUnsignedShort3(x));
                        break;

                    case VertexElementFormat.NormalizedUnsignedShort4:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(UInt16) * 4,
                            x => x.AsVector4Array(), x => new NormalizedUnsignedShort4(x));
                        break;

                    case VertexElementFormat.Int:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Int32),
                            x => x.AsScalarArray(), x => new Int1(x));
                        break;

                    case VertexElementFormat.Int2:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Int32) * 2,
                            x => x.AsVector2Array(), x => new Int2(x));
                        break;

                    case VertexElementFormat.Int3:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Int32) * 3,
                            x => x.AsVector3Array(), x => new Int3(x));
                        break;

                    case VertexElementFormat.Int4:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Int32) * 4,
                            x => x.AsVector4Array(), x => new Int4(x));
                        break;

                    case VertexElementFormat.NormalizedInt:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Int32),
                            x => x.AsScalarArray(), x => new NormalizedInt1(x));
                        break;

                    case VertexElementFormat.NormalizedInt2:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Int32) * 2,
                            x => x.AsVector2Array(), x => new NormalizedInt2(x));
                        break;

                    case VertexElementFormat.NormalizedInt3:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Int32) * 3,
                            x => x.AsVector3Array(), x => new NormalizedInt3(x));
                        break;

                    case VertexElementFormat.NormalizedInt4:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Int32) * 4,
                            x => x.AsVector4Array(), x => new NormalizedInt4(x));
                        break;

                    case VertexElementFormat.UnsignedInt:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(UInt32),
                            x => x.AsScalarArray(), x => new UnsignedInt1(x));
                        break;

                    case VertexElementFormat.UnsignedInt2:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(UInt32) * 2,
                            x => x.AsVector2Array(), x => new UnsignedInt2(x));
                        break;

                    case VertexElementFormat.UnsignedInt3:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(UInt32) * 3,
                            x => x.AsVector3Array(), x => new UnsignedInt3(x));
                        break;

                    case VertexElementFormat.UnsignedInt4:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(UInt32) * 4,
                            x => x.AsVector4Array(), x => new UnsignedInt4(x));
                        break;

                    case VertexElementFormat.NormalizedUnsignedInt:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(UInt32),
                            x => x.AsScalarArray(), x => new NormalizedUnsignedInt1(x));
                        break;

                    case VertexElementFormat.NormalizedUnsignedInt2:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(UInt32) * 2,
                            x => x.AsVector2Array(), x => new NormalizedUnsignedInt2(x));
                        break;

                    case VertexElementFormat.NormalizedUnsignedInt3:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(UInt32) * 3,
                            x => x.AsVector3Array(), x => new NormalizedUnsignedInt3(x));
                        break;

                    case VertexElementFormat.NormalizedUnsignedInt4:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(UInt32) * 4,
                            x => x.AsVector4Array(), x => new NormalizedUnsignedInt4(x));
                        break;

                    case VertexElementFormat.Single:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Single), 
                            x => x.AsScalarArray(), x => x);
                        break;

                    case VertexElementFormat.Vector2:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Single) * 2,
                            x => x.AsVector2Array(), x => (Vector2)x);
                        break;

                    case VertexElementFormat.Vector3:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Single) * 3, 
                            x => x.AsVector3Array(), x => (Vector3)x);
                        break;

                    case VertexElementFormat.Vector4:
                        PopulateVertexBufferAttributeData(vBuffer, accessor, vElement.Position, sizeof(Single) * 4, 
                            x => x.AsVector4Array(), x => (Vector4)x);
                        break;


                    default:
                        throw new NotSupportedException(UltravioletStrings.UnsupportedElementFormatInGltfLoader.Format(vElement.Format));
                }
            }

            geometryStream.Attach(vBuffer);
            return vBuffer.VertexCount;
        }

        /// <summary>
        /// Attaches an index buffer representing the specified primitive's indices to the specified geometry stream.
        /// </summary>
        private Int32 AttachIndexBuffer(MeshPrimitive primitive, GeometryStream geometryStream)
        {
            var accessor = primitive.GetIndexAccessor();
            if (accessor != null && accessor.Count > 0)
            {
                IndexBuffer iBuffer;
                switch (accessor.Format.ByteSize)
                {
                    case 1:
                    case 2:
                        iBuffer = IndexBuffer.Create(IndexBufferElementType.Int16, accessor.Count);
                        break;

                    case 3:
                    case 4:
                        iBuffer = IndexBuffer.Create(IndexBufferElementType.Int32, accessor.Count);
                        break;

                    default:
                        throw new NotSupportedException(UltravioletStrings.UnsupportedIndexAccessorFormat.Format(accessor.Format.ByteSize));
                }

                var data = accessor.AsIndicesArray();
                var dataDstOffset = 0;
                unsafe
                {
                    for (var i = 0; i < data.Count; i++)
                    {
                        var index = data[i];
                        if (iBuffer.IndexElementType == IndexBufferElementType.Int16)
                        {
                            var index16 = (Int16)index;
                            iBuffer.SetRawData((IntPtr)(&index16), 0, dataDstOffset, sizeof(Int16), SetDataOptions.NoOverwrite);
                            dataDstOffset += 2;
                        }
                        else
                        {
                            var index32 = (Int32)index;
                            iBuffer.SetRawData((IntPtr)(&index32), 0, dataDstOffset, sizeof(Int32), SetDataOptions.NoOverwrite);
                            dataDstOffset += 4;
                        }
                    }
                }

                geometryStream.Attach(iBuffer);
                return iBuffer.IndexCount;
            }

            return 0;
        }

        /// <summary>
        /// Processes the nodes in the specified container.
        /// </summary>
        private IList<ModelNode> ProcessNodes(ContentManager contentManager, IVisualNodeContainer container, Dictionary<String, Texture2D> textures)
        {
            var uvNodes = new List<ModelNode>();

            foreach (var node in container.VisualChildren)
            {
                var uvNodeChildren = ProcessNodes(contentManager, node, textures);
                var uvNodeModelMesh = ProcessNodeMesh(contentManager, node.Mesh, textures);
                var uvNode = new ModelNode(node.Name, uvNodeModelMesh, uvNodeChildren, node.LocalMatrix);
                uvNodes.Add(uvNode);
            }

            return uvNodes;
        }

        /// <summary>
        /// Processes the scenes in the specified model.
        /// </summary>
        private IList<ModelScene> ProcessScenes(ContentManager contentManager, ModelRoot input, Dictionary<String, Texture2D> textures, out Int32? defaultSceneIndex)
        {
            var uvScenes = new List<ModelScene>();
            var uvDefaultScene = default(ModelScene);

            foreach (var scene in input.LogicalScenes)
            {
                var uvSceneNodes = ProcessNodes(contentManager, scene, textures);
                var uvScene = new ModelScene(scene.Name, uvSceneNodes);
                uvScenes.Add(uvScene);

                if (input.DefaultScene == scene)
                    uvDefaultScene = uvScene;
            }

            defaultSceneIndex = (uvDefaultScene == null) ? (Int32?)null : uvScenes.IndexOf(uvDefaultScene);
            return uvScenes;
        }
    }
}
