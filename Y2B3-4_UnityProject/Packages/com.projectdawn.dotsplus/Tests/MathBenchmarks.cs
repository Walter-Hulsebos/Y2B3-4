using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.MethodImplOptions;

using UnityEngine;
using Unity.PerformanceTesting;
using static Unity.Mathematics.math;

using NUnit.Framework;

using FluentAssertions;

//NOTE: [Walter] Don't mind these custom naming, I just like it better.
//They're consistent and show all the information one may need, and they're shorter. 
//Also, they avoid an issue with the Unity.Mathematics creation shorthands to make it interchangeable with shader code.
//i.e. You can create a float3 by typing `float3(x, y, z)` in C# and HLSL, instead of `new float3(x, y, z)` or `float3 v = new(x, y, z)` in C# and `float3(x, y, z)` in HLSL.
using F32     = System.Single;
using F32x3   = Unity.Mathematics.float3;
using F32x3x2 = Unity.Mathematics.float3x2;
using F32x3x3 = Unity.Mathematics.float3x3;
using F32x4   = Unity.Mathematics.float4;
using F32x4x4 = Unity.Mathematics.float4x4;
using I32     = System.Int32;

using Random  = Unity.Mathematics.Random;
using Vector3 = UnityEngine.Vector3;

namespace ProjectDawn.Mathematics.Tests
{
    using static ProjectDawn.Mathematics.math2;
    
    internal class MathBenchmarks
    {
        private const I32 DIR_WARMUP_COUNT               = 10;
        private const I32 DIR_MEASUREMENT_COUNT          = 20;
        private const I32 DIR_ITERATIONS_PER_MEASUREMENT = 10_000;

        public class VectorExtensionsTests
        {
            [Test]
            public void SetMaxLength__Test_ReturnsSameVectorWhenWithinMaxLength()
            {
                // Arrange
                F32x3 __vector = new F32x3(x: 1, y: 2, z: 3);
                F32x3 __expected = __vector;
                const F32 MAX_LENGTH = 4.0f;

                // Act
                __vector.SetMaxLength(maxLength: MAX_LENGTH);

                // Assert
                __vector.Should().Be(__expected);
            }

            [Test]
            public void SetMaxLength__Test_ReturnsClampedVectorWhenExceedsMaxLength()
            {
                // Arrange
                F32x3 __vectorA   = new F32x3(x: +2.0f,      y: +4.0f,      z: +6.0f);      //Length = 7.483315
                F32x3 __expectedA = new F32x3(x: +1.336306f, y: +2.672612f, z: +4.008918f); //Length = 5.0
                
                F32x3 __vectorB   = new F32x3(x: -2.0f,      y: -4.0f,      z: -6.0f);      //Length = 7.483315
                F32x3 __expectedB = new F32x3(x: -1.336306f, y: -2.672612f, z: -4.008918f); //Length = 5.0
                
                const F32 MAX_LENGTH = 5.0f;

                // Act
                __vectorA.SetMaxLength(maxLength: MAX_LENGTH);
                __vectorB.SetMaxLength(maxLength: MAX_LENGTH);

                // Assert
                __vectorA.Should().Be(__expectedA);
                __vectorB.Should().Be(__expectedB);
            }

            [Test, Performance]
            public void SetMaxLength__Benchmark()
            {
                const F32 MAX_LENGTH = 5.0f;

                Random __rng = new Random(seed: 69);
                F32x3  __min = new F32x3(x: -10, y: -10, z: -10);
                F32x3  __max = new F32x3(x: +10, y: +10, z: +10);
                
                Measure.Method(action: () =>
                    {
                        // Arrange
                        F32x3 __vector = __rng.NextFloat3(min: __min, max: __max);

                        // Act
                        __vector.SetMaxLength(maxLength: MAX_LENGTH);
                    })
                    .WarmupCount(count: DIR_WARMUP_COUNT)
                    .MeasurementCount(count: DIR_MEASUREMENT_COUNT)
                    .IterationsPerMeasurement(count: DIR_ITERATIONS_PER_MEASUREMENT)
                    .GC()
                    .Run();
            }

            [Test]
            public void SetLength__Test()
            {
                // Arrange
                F32x3 __vector   = new F32x3(x: 2, y: 4, z: 6);                         //Length = 7.483315
                F32x3 __expected = new F32x3(x: 1.336306f, y: 2.672612f, z: 4.008918f); //Length = 5.0
                const F32 LENGTH = 5.0f;
                
                // Vector3 a = new Vector3(x: 2, y: 4, z: 6);
                // Debug.Log(a.normalized * LENGTH); 

                // Act
                __vector.SetLength(LENGTH);
                
                // Assert
                __vector.Should().Be(__expected);
            }
            
            [Test, Performance]
            public void SetLength__Benchmark()
            {
                const F32 LENGTH = 5.0f;

                Random __rng = new Random(seed: 69);
                F32x3  __min = new F32x3(x: -10, y: -10, z: -10);
                F32x3  __max = new F32x3(x: +10, y: +10, z: +10);
                
                Measure.Method(action: () =>
                    {
                        // Arrange
                        F32x3 __vector = __rng.NextFloat3(min: __min, max: __max);
                        F32   __length = __rng.NextFloat(); 
                        
                        F32x3 __expected = ((Vector3)__vector).normalized * LENGTH;

                        // Act
                        __vector.SetLength(length: LENGTH);
                    })
                    .WarmupCount(count: DIR_WARMUP_COUNT)
                    .MeasurementCount(count: DIR_MEASUREMENT_COUNT)
                    .IterationsPerMeasurement(count: DIR_ITERATIONS_PER_MEASUREMENT)
                    .GC()
                    .Run();
            }
            


            [Test]
            public void WithMaxLength__Test_ReturnsSameVectorWhenWithinMaxLength()
            {
                // Arrange
                F32x3 __vector = new F32x3(x: 1, y: 2, z: 3);
                const F32 MAX_LENGTH = 4.0f;

                // Act
                F32x3 __clampedVector = __vector.WithMaxLength(maxLength: MAX_LENGTH);

                // Assert
                __clampedVector.Should().Be(expected: __vector);
                
                float f = 69.000001f;
                f.Should().BeApproximately(expectedValue: 69f, precision: 0.0001f); //Succeeds
            }

            [Test]
            public void WithMaxLength__Test_ReturnsClampedVectorWhenExceedsMaxLength()
            {
                // Arrange
                F32x3 __vector   = new F32x3(x: 2, y: 4, z: 6);                         //Length = 7.483315
                F32x3 __expected = new F32x3(x: 1.336306f, y: 2.672612f, z: 4.008918f); //Length = 5
                const F32 MAX_LENGTH = 5.0f;

                // Act
                F32x3 __clampedVector = __vector.WithMaxLength(maxLength: MAX_LENGTH);

                // Assert
                __clampedVector.Should().Be(__expected);
            }
            
            [Test, Performance]
            public void WithMaxLength__Benchmark()
            {
                const F32 MAX_LENGTH = 5.0f;

                Random __rng = new Random(seed: 69);
                F32x3  __min = new F32x3(x: -10, y: -10, z: -10);
                F32x3  __max = new F32x3(x: +10, y: +10, z: +10);
                
                Measure.Method(action: () =>
                    {
                        // Arrange
                        F32x3 __vector = __rng.NextFloat3(min: __min, max: __max);

                        // Act
                        F32x3 __clampedVector = __vector.WithMaxLength(maxLength: MAX_LENGTH);
                    })
                    .WarmupCount(count: DIR_WARMUP_COUNT)
                    .MeasurementCount(count: DIR_MEASUREMENT_COUNT)
                    .IterationsPerMeasurement(count: DIR_ITERATIONS_PER_MEASUREMENT)
                    .GC()
                    .Run();
            }
        }

    }
}
