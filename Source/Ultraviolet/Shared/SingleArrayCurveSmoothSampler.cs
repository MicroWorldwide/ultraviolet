﻿using System;
using System.Buffers;

namespace Ultraviolet
{
    /// <summary>
    /// Represents an <see cref="ICurveSampler{TValue, TKey}"/> which performs smooth (bicubic) sampling on a curve of arrays of <see cref="Single"/> values.
    /// </summary>
    public class SingleArrayCurveSmoothSampler : ICurveSampler<ArraySegment<Single>, SmoothCurveKey<ArraySegment<Single>>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SingleArrayCurveSmoothSampler"/> class.
        /// </summary>
        private SingleArrayCurveSmoothSampler() { }

        /// <inheritdoc/>
        public void CreateTemporaryValue(Int32 elementCount, out ArraySegment<Single> value) => value = ArrayPool<Single>.Shared.Rent(elementCount);

        /// <inheritdoc/>
        public void ReleaseTemporaryValue(in ArraySegment<Single> value) => ArrayPool<Single>.Shared.Return(value.Array);

        /// <inheritdoc/>
        public ArraySegment<Single> InterpolateKeyframes(SmoothCurveKey<ArraySegment<Single>> key1, SmoothCurveKey<ArraySegment<Single>> key2, Single t, ArraySegment<Single> offset, in ArraySegment<Single> existing)
        {
            // NOTE: Candidate for SIMD optimization in .NET 5.

            var count = key1.Value.Count;
            if (count != key2.Value.Count || count != existing.Count)
                throw new ArgumentException(UltravioletStrings.SamplerArgumentsMustHaveSameLength);

            for (var i = 0; i < count; i++)
            {
                var t2 = t * t;
                var t3 = t2 * t;
                var key1Value = key1.Value[i];
                var key2Value = key2.Value[i];
                var tangentIn = key2.TangentIn[i];
                var tangentOut = key1.TangentOut[i];

                var polynomial1 = (2.0 * t3 - 3.0 * t2 + 1.0); // (2t^3 - 3t^2 + 1)
                var polynomial2 = (t3 - 2.0 * t2 + t);         // (t3 - 2t^2 + t)  
                var polynomial3 = (-2.0 * t3 + 3.0 * t2);      // (-2t^2 + 3t^2)
                var polynomial4 = (t3 - t2);                   // (t^3 - t^2)

                existing[i] = offset[i] + (Single)(key1Value * polynomial1 + tangentOut * polynomial2 + key2Value * polynomial3 + tangentIn * polynomial4);
            }

            return existing;
        }

        /// <inheritdoc/>
        public ArraySegment<Single> CalculateLinearExtension(SmoothCurveKey<ArraySegment<Single>> key, Single position, CurvePositionType positionType, in ArraySegment<Single> existing)
        {
            // NOTE: Candidate for SIMD optimization in .NET 5.

            var count = key.Value.Count;
            if (count != existing.Count)
                throw new ArgumentException(UltravioletStrings.SamplerArgumentsMustHaveSameLength);

            var positionDelta = (key.Position - position);
            switch (positionType)
            {
                case CurvePositionType.BeforeCurve:
                    for (var i = 0; i < count; i++)
                    {
                        existing[i] = key.Value[i] - key.TangentIn[i] * positionDelta;
                    }
                    return existing;

                case CurvePositionType.AfterCurve:
                    for (var i = 0; i < count; i++)
                    {
                        existing[i] = key.Value[i] - key.TangentOut[i] * positionDelta;
                    }
                    return existing;

                default:
                    return key.Value;
            }
        }

        /// <inheritdoc/>
        public ArraySegment<Single> CalculateCycleOffset(ArraySegment<Single> first, ArraySegment<Single> last, Int32 cycle, in ArraySegment<Single> existing)
        {
            // NOTE: Candidate for SIMD optimization in .NET 5.

            var count = first.Count;
            if (count != last.Count || count != existing.Count)
                throw new ArgumentException(UltravioletStrings.SamplerArgumentsMustHaveSameLength);

            for (var i = 0; i < count; i++)
                existing[i] = (last[i] - first[i]) * cycle;

            return existing;
        }

        /// <summary>
        /// Gets the singleton instance of the <see cref="SingleArrayCurveSmoothSampler"/> class.
        /// </summary>
        public static SingleArrayCurveSmoothSampler Instance { get; } = new SingleArrayCurveSmoothSampler();
    }
}
