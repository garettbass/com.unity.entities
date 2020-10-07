using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System;
using System.Collections.Generic;
using Unity.Jobs;

#if !UNITY_PORTABLE_TEST_RUNNER
using System.Linq;
#endif


// ******* COPY AND PASTE WARNING *************
// NOTE: Duplicate tests (with only type differences)
// - BufferElementDataTests.cs and BufferElementDataSystemStateTests.cs
// - Any change to this file should be reflected in the other file.
// Changes between two files:
// - s/BufferElementDataTests/BufferElementDataSystemStateTests/
// - s/EcsIntElement/EcsIntStateElement/g
// - s/IBufferElementData/ISystemStateBufferElementData/g
// ******* COPY AND PASTE WARNING *************

#pragma warning disable 0649
#pragma warning disable 0219 // assigned but its value is never used

namespace Unity.Entities.Tests
{
    class BufferElementDataTests : ECSTestsFixture
    {
        [InternalBufferCapacity(1024 * 1024)]
        public struct OverSizedCapacity : IBufferElementData
        {
            public int Value;
        }

        [Test]
        public void BufferTypeClassificationWorks()
        {
            var t  = TypeManager.GetTypeInfo<EcsIntElement>();
            Assert.AreEqual(TypeManager.TypeCategory.BufferData, t.Category);
            Assert.AreEqual(8, t.BufferCapacity);
        }

        [Test]
        public void BufferComponentTypeCreationWorks()
        {
            var bt = ComponentType.ReadWrite<EcsIntElement>();
            var typeInfo = TypeManager.GetTypeInfo(bt.TypeIndex);
            Assert.AreEqual(ComponentType.AccessMode.ReadWrite, bt.AccessModeType);
            Assert.AreEqual(8, typeInfo.BufferCapacity);
        }

        [Test]
        public void CreateEntityWithIntThrows()
        {
            Assert.Throws<System.ArgumentException>(() => { m_Manager.CreateEntity(typeof(int));});
        }

        [Test]
        public void AddComponentWithIntThrows()
        {
            var entity = m_Manager.CreateEntity();
            Assert.Throws<System.ArgumentException>(() => { m_Manager.AddComponent(entity, ComponentType.ReadWrite<int>()); });
        }

        [Test]
        // Invalid because chunk size is too small to hold a single entity
        public void CreateEntityWithInvalidInternalCapacity()
        {
            var arrayType = ComponentType.ReadWrite<OverSizedCapacity>();
            Assert.Throws<ArgumentException>(() => m_Manager.CreateEntity(arrayType));
        }

        [Test]
        public void HasComponent()
        {
            var arrayType = ComponentType.ReadWrite<EcsIntElement>();
            var entity = m_Manager.CreateEntity(arrayType);
            Assert.IsTrue(m_Manager.HasComponent(entity, arrayType));
        }

        [Test]
        public void InitialCapacityWorks()
        {
            var arrayType = ComponentType.ReadWrite<EcsIntElement>();
            var entity = m_Manager.CreateEntity(arrayType);
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            Assert.AreEqual(8, buffer.Capacity);
        }

        [Test]
        public void InitialCapacityWorks2()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            Assert.AreEqual(8, buffer.Capacity);
        }

        [Test]
        public void AddWorks()
        {
            var arrayType = ComponentType.ReadWrite<EcsIntElement>();
            var entity = m_Manager.CreateEntity(arrayType);
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            for (int i = 0; i < 189; ++i)
                buffer.Add(i);

            Assert.AreEqual(189, buffer.Length);
            for (int i = 0; i < 189; ++i)
            {
                Assert.AreEqual(i, buffer[i].Value);
            }
        }

        [Test]
        public void InsertWorks()
        {
            var arrayType = ComponentType.ReadWrite<EcsIntElement>();
            var entity = m_Manager.CreateEntity(arrayType);
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            // Insert at end
            for (int i = 0; i < 189; ++i)
                buffer.Insert(i, i);

            Assert.AreEqual(189, buffer.Length);
            for (int i = 0; i < 189; ++i)
            {
                Assert.AreEqual(i, buffer[i].Value);
            }

            buffer.Clear();

            // Insert at beginning
            for (int i = 0; i < 189; ++i)
                buffer.Insert(0, i);

            Assert.AreEqual(189, buffer.Length);
            for (int i = 0; i < 189; ++i)
            {
                Assert.AreEqual(188 - i, buffer[i].Value);
            }

            buffer.Clear();

            // Insert in middle
            for (int i = 0; i < 189; ++i)
                buffer.Insert(i / 2, i);

            Assert.AreEqual(189, buffer.Length);
            for (int i = 0; i < 189; ++i)
            {
                int expectedValue = i < 94 ? i * 2 + 1 : (188 - i) * 2;
                Assert.AreEqual(expectedValue, buffer[i].Value);
            }
        }

        [Test]
        public void AddRangeWorks()
        {
            var arrayType = ComponentType.ReadWrite<EcsIntElement>();
            var entity = m_Manager.CreateEntity(arrayType);
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            for (int i = 0; i < 7; ++i)
                buffer.Add(i);

            Assert.AreEqual(7, buffer.Length);

            var blah = new NativeArray<EcsIntElement>(1024, Allocator.Temp);

            for (int i = 0; i < blah.Length; ++i)
            {
                blah[i] = i;
            }

            buffer.AddRange(blah);
            blah.Dispose();

            Assert.AreEqual(1024 + 7, buffer.Length);

            for (int i = 0; i < 7; ++i)
                Assert.AreEqual(i, buffer[i].Value);
            for (int i = 0; i < 1024; ++i)
                Assert.AreEqual(i, buffer[7 + i].Value);
        }

        [Test]
        public void RemoveAtWorks()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            for (int i = 0; i < 9; ++i)
                buffer.Add(i);

            buffer.RemoveAt(7);

            CheckBufferContents(buffer, new int[] { 0, 1, 2, 3, 4, 5, 6, 8 });
        }

        [Test]
        public void RemoveAtSwapBack_WithFirstElement_Works()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            buffer.Add(0);
            buffer.Add(1);
            buffer.Add(2);
            buffer.RemoveAtSwapBack(0);
            CheckBufferContents(buffer, new [] { 2, 1 });
        }

        [Test]
        public void RemoveAtSwapBack_WithMiddleElement_Works()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            buffer.Add(0);
            buffer.Add(1);
            buffer.Add(2);
            buffer.RemoveAtSwapBack(1);
            CheckBufferContents(buffer, new [] { 0, 2 });
        }

        [Test]
        public void RemoveAtSwapBack_WithLastElement_Works()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            buffer.Add(0);
            buffer.Add(1);
            buffer.Add(2);
            buffer.RemoveAtSwapBack(2);
            CheckBufferContents(buffer, new [] { 0, 1 });
        }

        [Test]
        public void RemoveAtSwapBack_WithInvalidIndex_Throws()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            buffer.Add(0);
            Assert.Throws<IndexOutOfRangeException>(() => buffer.RemoveAtSwapBack(17));
        }

        private static void CheckBufferContents(DynamicBuffer<EcsIntElement> buffer, int[] refs)
        {
            Assert.AreEqual(refs.Length, buffer.Length);

            for (int i = 0; i < refs.Length; ++i)
            {
                Assert.AreEqual(refs[i], buffer[i].Value);
            }
        }

        [Test]
        public void RemoveAtWorksFromStart()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            for (int i = 0; i < 9; ++i)
                buffer.Add(i);

            buffer.RemoveAt(0);

            CheckBufferContents(buffer, new int[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        }

        [Test]
        public void RemoveAtWorksFromEnd()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            for (int i = 0; i < 9; ++i)
                buffer.Add(i);

            buffer.RemoveAt(8);
            buffer.RemoveAt(7);

            CheckBufferContents(buffer, new int[] { 0, 1, 2, 3, 4, 5, 6 });
        }

        [Test]
        public void RemoveRangeWorksFromEnd()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            for (int i = 0; i < 9; ++i)
                buffer.Add(i);

            buffer.RemoveRange(5, 4);

            CheckBufferContents(buffer, new int[] { 0, 1, 2, 3, 4 });
        }

        [Test]
        public void RemoveRange_WithEmptyRange_NoChanges()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            for (int i = 0; i < 9; ++i)
                buffer.Add(i);

            buffer.RemoveRange(0, 0);
            for (int i = 0; i < 9; ++i)
                buffer.RemoveRange(i, 0);

            CheckBufferContents(buffer, new[] {0, 1, 2, 3, 4, 5, 6, 7, 8});
        }

        [Test]
        public void RemoveRange_WithInvalidIndex_Throws()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            buffer.Add(0);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            Assert.Throws<IndexOutOfRangeException>(() => buffer.RemoveRange(-1, 2));
            Assert.Throws<IndexOutOfRangeException>(() => buffer.RemoveRange(-1, 7));
            Assert.Throws<IndexOutOfRangeException>(() => buffer.RemoveRange(2, 4));
            Assert.Throws<IndexOutOfRangeException>(() => buffer.RemoveRange(5, 2));
        }

        [Test]
        public void RemoveRangeSwapBack_WithInvalidIndex_Throws()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            buffer.Add(0);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            Assert.Throws<IndexOutOfRangeException>(() => buffer.RemoveRangeSwapBack(-1, 2));
            Assert.Throws<IndexOutOfRangeException>(() => buffer.RemoveRangeSwapBack(-1, 7));
            Assert.Throws<IndexOutOfRangeException>(() => buffer.RemoveRangeSwapBack(2, 4));
            Assert.Throws<IndexOutOfRangeException>(() => buffer.RemoveRangeSwapBack(5, 2));
        }

        [Test]
        public void RemoveRangeSwapBack_WithFullBuffer_Works()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            for (int i = 0; i < 9; ++i)
                buffer.Add(i);

            buffer.RemoveRangeSwapBack(0, 9);

            CheckBufferContents(buffer, new int[] {});
        }

        [Test]
        public void RemoveRangeSwapBack_WithEmptyRange_NoChanges()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            for (int i = 0; i < 9; ++i)
                buffer.Add(i);

            buffer.RemoveRangeSwapBack(0, 0);
            for (int i = 0; i < 9; ++i)
                buffer.RemoveRangeSwapBack(i, 0);

            CheckBufferContents(buffer, new[] {0, 1, 2, 3, 4, 5, 6, 7, 8});
        }

        [Test]
        public void RemoveRangeSwapBack_WithStartRange_Works()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            for (int i = 0; i < 9; ++i)
                buffer.Add(i);

            buffer.RemoveRangeSwapBack(0, 2);

            CheckBufferContents(buffer, new[] {7, 8, 2, 3, 4, 5, 6});
        }

        [Test]
        public void RemoveRangeSwapBack_WithMiddleRange_Works()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            for (int i = 0; i < 9; ++i)
                buffer.Add(i);

            buffer.RemoveRangeSwapBack(4, 2);

            CheckBufferContents(buffer, new[] {0, 1, 2, 3, 7, 8, 6});
        }

        [Test]
        public void RemoveRangeSwapBack_WithMiddleRangeOverlappingEnd_Works()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            for (int i = 0; i < 9; ++i)
                buffer.Add(i);

            buffer.RemoveRangeSwapBack(6, 2);

            CheckBufferContents(buffer, new[] {0, 1, 2, 3, 4, 5, 8});
        }

        [Test]
        public void RemoveRangeSwapBack_WithEndRange_Works()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            for (int i = 0; i < 9; ++i)
                buffer.Add(i);

            buffer.RemoveRangeSwapBack(7, 2);

            CheckBufferContents(buffer, new[] {0, 1, 2, 3, 4, 5, 6});
        }

        [Test]
        public void InitialCapacityWorksWithAddComponment()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponent(entity, ComponentType.ReadWrite<EcsIntElement>());
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            Assert.AreEqual(8, buffer.Capacity);
        }

        [Test]
        public void RemoveComponent()
        {
            var arrayType = ComponentType.ReadWrite<EcsIntElement>();
            var entity = m_Manager.CreateEntity(arrayType);
            Assert.IsTrue(m_Manager.HasComponent(entity, arrayType));
            m_Manager.RemoveComponent(entity, arrayType);
            Assert.IsFalse(m_Manager.HasComponent(entity, arrayType));
        }

        [Test]
        public void MutateBufferData()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddBuffer<EcsIntElement>(entity);

            var array = m_Manager.GetBuffer<EcsIntElement>(entity);
            Assert.AreEqual(0, array.Length);

            using (var array2 = new NativeArray<EcsIntElement>(6, Allocator.Temp))
            {
                array.CopyFrom(array2);

                Assert.AreEqual(6, array.Length);

                array[3] = 5;
                Assert.AreEqual(5, array[3].Value);
                Assert.AreNotEqual(5, array2[3].Value); // no aliasing
            }
        }

        [Test]
        public void BufferComponentGroupChunkIteration()
        {
            /*var entity64 =*/
            m_Manager.CreateEntity(typeof(EcsIntElement));
            /*var entity10 =*/
            m_Manager.CreateEntity(typeof(EcsIntElement));

            var group = m_Manager.CreateEntityQuery(typeof(EcsIntElement));

            var chunks = group.CreateArchetypeChunkArray(Allocator.TempJob);
            var buffers = chunks[0].GetBufferAccessor(m_Manager.GetBufferTypeHandle<EcsIntElement>(false));

            Assert.AreEqual(2, buffers.Length);
            Assert.AreEqual(0, buffers[0].Length);
            Assert.AreEqual(8, buffers[0].Capacity);
            Assert.AreEqual(0, buffers[1].Length);
            Assert.AreEqual(8, buffers[1].Capacity);

            buffers[0].Add(12);
            buffers[0].Add(13);

            Assert.AreEqual(2, buffers[0].Length);
            Assert.AreEqual(12, buffers[0][0].Value);
            Assert.AreEqual(13, buffers[0][1].Value);

            Assert.AreEqual(0, buffers[1].Length);

            chunks.Dispose();
        }

        [Test]
        public void BufferFromEntityWorks()
        {
            var entityInt = m_Manager.CreateEntity(typeof(EcsIntElement));
            m_Manager.GetBuffer<EcsIntElement>(entityInt).CopyFrom(new EcsIntElement[] { 1, 2, 3 });

            var intLookup = EmptySystem.GetBufferFromEntity<EcsIntElement>();
            Assert.IsTrue(intLookup.HasComponent(entityInt));
            Assert.IsFalse(intLookup.HasComponent(new Entity()));

            Assert.AreEqual(2, intLookup[entityInt][1].Value);
        }

        [Test]
        public void OutOfBoundsAccessThrows()
        {
            var entityInt = m_Manager.CreateEntity(typeof(EcsIntElement));
            var intArray = m_Manager.GetBuffer<EcsIntElement>(entityInt);
            intArray.Add(12);
            m_Manager.DestroyEntity(entityInt);

#if UNITY_2020_2_OR_NEWER
            Assert.Throws<ObjectDisposedException>(() =>
#else
            Assert.Throws<InvalidOperationException>(() =>
#endif
            {
                intArray.Add(123);
            });
        }

        [Test]
        public void UseAfterStructuralChangeThrows()
        {
            var entityInt = m_Manager.CreateEntity(typeof(EcsIntElement));
            var intArray = m_Manager.GetBuffer<EcsIntElement>(entityInt);
            m_Manager.DestroyEntity(entityInt);

#if UNITY_2020_2_OR_NEWER
            Assert.Throws<ObjectDisposedException>(() =>
#else
            Assert.Throws<InvalidOperationException>(() =>
#endif
            {
                intArray.Add(123);
            });
        }

        [Test]
        public void UseAfterStructuralChangeThrows2()
        {
            var entityInt = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBufferFromEntity<EcsIntElement>();
            var array = buffer[entityInt];
            m_Manager.DestroyEntity(entityInt);

#if UNITY_2020_2_OR_NEWER
            Assert.Throws<ObjectDisposedException>(() =>
#else
            Assert.Throws<InvalidOperationException>(() =>
#endif
            {
                array.Add(123);
            });
        }

        [Test]
        public void UseAfterStructuralChangeThrows3()
        {
            var entityInt = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entityInt);
            buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3 });
            m_Manager.AddComponentData(entityInt, new EcsTestData() { value = 20 });
#if UNITY_2020_2_OR_NEWER
            Assert.Throws<ObjectDisposedException>(() =>
#else
            Assert.Throws<InvalidOperationException>(() =>
#endif
                { buffer.Add(4); });
        }

        [Test]
        public void WritingReadOnlyThrows()
        {
            var entityInt = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBufferFromEntity<EcsIntElement>(true);
            var array = buffer[entityInt];
            Assert.Throws<InvalidOperationException>(() =>
            {
                array.Add(123);
            });
        }

        [Test]
        public void ReinterpretWorks()
        {
            var entityInt = m_Manager.CreateEntity(typeof(EcsIntElement));
            var intBuffer = m_Manager.GetBuffer<EcsIntElement>(entityInt);
            var floatBuffer = intBuffer.Reinterpret<float>();

            intBuffer.Add(0x3f800000);
            floatBuffer.Add(-1.0f);

            Assert.AreEqual(2, intBuffer.Length);
            Assert.AreEqual(2, floatBuffer.Length);

            Assert.AreEqual(0x3f800000, intBuffer[0].Value);
            Assert.AreEqual(1.0f, floatBuffer[0]);
            Assert.AreEqual(0xbf800000u, (uint)intBuffer[1].Value);
            Assert.AreEqual(-1.0f, floatBuffer[1]);
        }

        [Test]
        public void ReinterpretWrongSizeThrows()
        {
            var entityInt = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entityInt);
            Assert.Throws<InvalidOperationException>(() =>
            {
                buffer.Reinterpret<ushort>();
            });
        }

        [Test]
        public void TrimExcessWorks()
        {
            var entityInt = m_Manager.CreateEntity(typeof(EcsIntElement));
            var intBuffer = m_Manager.GetBuffer<EcsIntElement>(entityInt);

            Assert.AreEqual(0, intBuffer.Length);
            Assert.AreEqual(8, intBuffer.Capacity);

            intBuffer.CopyFrom(new EcsIntElement[] { 0, 1, 2, 3 });

            intBuffer.TrimExcess();

            Assert.AreEqual(4, intBuffer.Length);
            Assert.AreEqual(8, intBuffer.Capacity);

            for (int i = 4; i < 10; ++i)
            {
                intBuffer.Add(i);
            }

            Assert.AreEqual(10, intBuffer.Length);
            Assert.AreEqual(16, intBuffer.Capacity);

            intBuffer.TrimExcess();

            Assert.AreEqual(10, intBuffer.Length);
            Assert.AreEqual(10, intBuffer.Capacity);

            for (int i = 0; i < 10; ++i)
            {
                Assert.AreEqual(i, intBuffer[i].Value);
            }
        }

        [Test]
        public void BufferSurvivesArchetypeChange()
        {
            var entityInt = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entityInt);
            buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3 });

            m_Manager.AddComponentData(entityInt, new EcsTestData() { value = 20 });

            CheckBufferContents(m_Manager.GetBuffer<EcsIntElement>(entityInt), new int[] { 1, 2, 3 });
        }

        internal struct ElementWithoutCapacity : IBufferElementData
        {
            public float Value;
        }

        [Test]
        public void NoCapacitySpecifiedWorks()
        {
            var original = m_Manager.CreateEntity(typeof(ElementWithoutCapacity));
            var buffer = m_Manager.GetBuffer<ElementWithoutCapacity>(original);
            Assert.AreEqual(buffer.Capacity, 32);
        }

        [Test]
        public void ArrayInvalidationWorks()
        {
            var original = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(original);
            buffer.Add(1);
            var array = buffer.AsNativeArray();
            Assert.AreEqual(1, array[0].Value);
            Assert.AreEqual(1, array.Length);
            buffer.Add(2);
#if UNITY_2020_2_OR_NEWER
            Assert.Throws<ObjectDisposedException>(() =>
#else
            Assert.Throws<InvalidOperationException>(() =>
#endif
            {
                int value = array[0].Value;
            });

#if UNITY_2020_2_OR_NEWER
            Assert.Throws<ObjectDisposedException>(() =>
#else
            Assert.Throws<InvalidOperationException>(() =>
#endif
            {
                array[0] = 5;
            });
        }

        [Test]
        public void ArrayInvalidationHappensForAllInstances()
        {
            var e0 = m_Manager.CreateEntity(typeof(EcsIntElement));
            var e1 = m_Manager.CreateEntity(typeof(EcsIntElement));

            var b0 = m_Manager.GetBuffer<EcsIntElement>(e0);
            var b1 = m_Manager.GetBuffer<EcsIntElement>(e1);

            b0.Add(1);
            b1.Add(1);

            var a0 = b0.AsNativeArray();
            var a1 = b1.AsNativeArray();

            b0.Add(1);

#if UNITY_2020_2_OR_NEWER
            Assert.Throws<ObjectDisposedException>(() =>
#else
            Assert.Throws<InvalidOperationException>(() =>
#endif
            {
                int value = a0[0].Value;
            });

#if UNITY_2020_2_OR_NEWER
            Assert.Throws<ObjectDisposedException>(() =>
#else
            Assert.Throws<InvalidOperationException>(() =>
#endif
            {
                int value = a1[0].Value;
            });
        }

        [Test]
        public void ArraysAreNotInvalidateByWrites()
        {
            var original = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(original);
            buffer.Add(1);
            var array = buffer.AsNativeArray();
            Assert.AreEqual(1, array[0].Value);
            Assert.AreEqual(1, array.Length);
            buffer[0] = 2;
            Assert.AreEqual(2, array[0].Value);
        }

        struct ArrayConsumingJob : IJob
        {
            public NativeArray<EcsIntElement> Array;

            public void Execute()
            {
            }
        }

        [Test]
        public void BufferInvalidationNotPossibleWhenArraysAreGivenToJobs()
        {
            var original = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(original);
            buffer.Add(1);
            var handle = new ArrayConsumingJob {Array = buffer.AsNativeArray()}.Schedule();
            Assert.Throws<InvalidOperationException>(() => buffer.Add(2));
            Assert.Throws<InvalidOperationException>(() => m_Manager.DestroyEntity(original));
            handle.Complete();
        }

        struct WriteJob : IJobChunk
        {
            public BufferTypeHandle<EcsIntElement> Int;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int entityOffset)
            {
                var intValue = chunk.GetBufferAccessor(Int)[0];

                Assert.AreEqual(intValue.Length, 1);

                var intValueArray = intValue.AsNativeArray();

                Assert.AreEqual(5, intValue[0].Value);
                Assert.AreEqual(5, intValueArray[0].Value);

                intValueArray[0] = 6;

                Assert.AreEqual(intValueArray.Length, 1);
                Assert.AreEqual(6, intValue[0].Value);

                // Invalidate intValueArray
                intValue.Add(10);
                Assert.Throws<InvalidOperationException>(() => { var p = intValueArray[0]; });
                Assert.Throws<InvalidOperationException>(() => { intValueArray[0] = 5; });
            }
        }

        [Test]
        public void ReadWriteDynamicBuffer()
        {
            // Create multiple chunks so we ensure we are doing parallel for writing to buffers
            for (int i = 0; i != 10; i++)
            {
                var original = m_Manager.CreateEntity(typeof(EcsIntElement));
                m_Manager.AddSharedComponentData(original, new SharedData1(i));
                var buffer = m_Manager.GetBuffer<EcsIntElement>(original);
                buffer.Add(5);
            }

            var group = EmptySystem.GetEntityQuery(new EntityQueryDesc {All = new ComponentType[] {typeof(EcsIntElement)}});
            var job = new WriteJob
            {
                Int = EmptySystem.GetBufferTypeHandle<EcsIntElement>()
            };

            job.Schedule(group).Complete();
        }

        struct ReadOnlyJob : IJobChunk
        {
            [ReadOnly]
            public BufferTypeHandle<EcsIntElement> Int;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int entityOffset)
            {
                var intValue = chunk.GetBufferAccessor(Int)[0];

                // Reading buffer
                Assert.AreEqual(intValue.Length, 1);
                Assert.AreEqual(5, intValue[0].Value);

                // Reading casted native array
                var intValueArray = intValue.AsNativeArray();
                Assert.AreEqual(intValueArray.Length, 1);
                Assert.AreEqual(5, intValueArray[0].Value);

                // Can't write to buffer...
                Assert.Throws<InvalidOperationException>(() => { intValue[0] = 5; });
                Assert.Throws<InvalidOperationException>(() => { intValueArray[0] = 5; });
            }
        }

        public void ReadOnlyDynamicBufferImpl(bool readOnlyType)
        {
            // Create multiple chunks so we ensure we are doing parallel for reading from buffers
            for (int i = 0; i != 10; i++)
            {
                var original = m_Manager.CreateEntity(typeof(EcsIntElement));
                m_Manager.AddSharedComponentData(original, new SharedData1(i));
                var buffer = m_Manager.GetBuffer<EcsIntElement>(original);
                buffer.Add(5);
            }

            var group = EmptySystem.GetEntityQuery(new EntityQueryDesc {All = new ComponentType[] {typeof(EcsIntElement)}});
            var job = new ReadOnlyJob
            {
                Int = EmptySystem.GetBufferTypeHandle<EcsIntElement>(readOnlyType)
            };

            job.Schedule(group).Complete();
        }

        [Test]
        public void ReadOnlyDynamicBufferReadOnly()
        {
            ReadOnlyDynamicBufferImpl(true);
        }

        [Test]
        public void ReadOnlyDynamicBufferWritable()
        {
            ReadOnlyDynamicBufferImpl(false);
        }

        struct BufferConsumingJob : IJob
        {
            public DynamicBuffer<EcsIntElement> Buffer;

            public void Execute()
            {
            }
        }

        [Test]
        public void BufferInvalidationNotPossibleWhenBuffersAreGivenToJobs()
        {
            var original = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(original);
            buffer.Add(1);
            var handle = new BufferConsumingJob {Buffer = buffer}.Schedule();
            Assert.Throws<InvalidOperationException>(() => buffer.Add(2));
            Assert.Throws<InvalidOperationException>(() => m_Manager.DestroyEntity(original));
            handle.Complete();
        }

        struct ReadOnlyNativeArrayJob : IJob
        {
            [ReadOnly]
            public NativeArray<EcsIntElement> IntArray;

            public void Execute()
            {
                var array = IntArray;

                // Reading casted native array
                Assert.AreEqual(array.Length, 1);
                Assert.AreEqual(5, array[0].Value);

                // Can't write to buffer...
                Assert.Throws<InvalidOperationException>(() => { array[0] = 5; });
            }
        }

        [Test]
        public void NativeArrayInJobReadOnly()
        {
            var original = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(original);
            buffer.Add(5);

            var job = new ReadOnlyNativeArrayJob
            {
                IntArray = buffer.AsNativeArray()
            };
            var jobHandle = job.Schedule();

            Assert.Throws<InvalidOperationException>(() => { buffer.Add(5); });
            Assert.Throws<InvalidOperationException>(() => { buffer[0] = 6; });
            Assert.Throws<InvalidOperationException>(() => { job.IntArray[0] = 6; });
            Assert.Throws<InvalidOperationException>(() => { job.IntArray[0] = 6; });

            Assert.AreEqual(5, buffer[0].Value);
            Assert.AreEqual(5, job.IntArray[0].Value);

            jobHandle.Complete();
        }

        [Test]
        public void DynamicBuffer_Default_IsCreated_IsFalse()
        {
            DynamicBuffer<EcsIntElement> buffer = default;
            Assert.False(buffer.IsCreated);
        }

        [Test]
        public void DynamicBuffer_FromEntity_IsCreated_IsTrue()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            Assert.IsTrue(buffer.IsCreated);
        }

        [Test]
        [Explicit("Takes a long time (order of seconds).")]
        public void DynamicBuffer_AllocateBufferWithLongSize_DoesNotThrow()
        {
            if (IntPtr.Size == 4)
                Assert.Ignore("Test makes no sense in 32bit");

            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            int capacity = (int)(((long)int.MaxValue + 1) / UnsafeUtility.SizeOf<EcsIntElement>() + 1); //536870913
            Assert.DoesNotThrow(() => buffer.ResizeUninitialized(capacity));
            Assert.AreEqual(capacity, buffer.Length);
        }

        [Test]
        [Explicit("Takes a long time (order of seconds).")]
        public void DynamicBuffer_Insert_BufferHasLongSize_DoesNotThrow()
        {
            if (IntPtr.Size == 4)
                Assert.Ignore("Test makes no sense in 32bit");

            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            int capacity = (int)(((long)int.MaxValue + 1) / UnsafeUtility.SizeOf<EcsIntElement>() + 1); //536870913
            buffer.ResizeUninitialized(capacity);

            Assert.DoesNotThrow(() => buffer.Insert(0, new EcsIntElement { Value = 99 }));
            Assert.AreEqual(capacity + 1, buffer.Length);
        }

        [Test]
        [Explicit("Takes a long time (order of seconds).")]
        public void DynamicBuffer_AddRange_NewBufferHasLongSize_DoesNotThrow()
        {
            if (IntPtr.Size == 4)
                Assert.Ignore("Test makes no sense in 32bit");

            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            int capacity = (int)(((long)int.MaxValue + 1) / UnsafeUtility.SizeOf<EcsIntElement>() + 1); //536870913
            buffer.ResizeUninitialized(capacity);

            NativeArray<EcsIntElement> array = new NativeArray<EcsIntElement>(10, Allocator.Temp);
            Assert.DoesNotThrow(() => buffer.AddRange(array));
            Assert.AreEqual(capacity + 10, buffer.Length);
        }

        [Test]
        [Explicit("Takes a long time (order of seconds).")]
        public void DynamicBuffer_RemoveRange_MovedBufferHasLongSize_DoesNotThrow()
        {
            if (IntPtr.Size == 4)
                Assert.Ignore("Test makes no sense in 32bit");

            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            int capacity = (int)(((long)int.MaxValue + 1) / UnsafeUtility.SizeOf<EcsIntElement>() + 2);
            buffer.ResizeUninitialized(capacity);

            Assert.AreEqual(536870914, buffer.Length);
            Assert.DoesNotThrow(() => buffer.RemoveRange(0, 1));
            Assert.AreEqual(536870913, buffer.Length);
        }

        [Test]
        [Explicit("Takes a long time (order of seconds).")]
        public void DynamicBuffer_Add_NewBufferHasLongSize_DoesNotThrow()
        {
            if (IntPtr.Size == 4)
                Assert.Ignore("Test makes no sense in 32bit");

            var arrayType = ComponentType.ReadWrite<EcsIntElement>();
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            int capacity = (int)(((long)int.MaxValue + 1) / UnsafeUtility.SizeOf<EcsIntElement>() + 1); //536870913

            buffer.ResizeUninitialized(capacity);

            Assert.DoesNotThrow(() => buffer.Add(1));
        }

        [Test]
        [Explicit("Takes a long time (order of seconds).")]
        public void DynamicBuffer_TrimExcess_NewBufferHasLongSize_DoesNotThrow()
        {
            if (IntPtr.Size == 4)
                Assert.Ignore("Test makes no sense in 32bit");

            var arrayType = ComponentType.ReadWrite<EcsIntElement>();
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            int capacity = (int)(((long)int.MaxValue + 1) / UnsafeUtility.SizeOf<EcsIntElement>() + 1); //536870913

            buffer.ResizeUninitialized(capacity);
            // cause the capacity to double
            buffer.Add(1);

            Assert.DoesNotThrow(() => buffer.TrimExcess());
            Assert.AreEqual(capacity + 1, buffer.Length);
        }

        [Test]
        public void DynamicBuffer_Reserve_IncreasesCapacity()
        {
            var arrayType = ComponentType.ReadWrite<EcsIntElement>();
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);

            buffer.EnsureCapacity(100);

            Assert.AreEqual(100, buffer.Capacity);
            Assert.AreEqual(0, buffer.Length);
        }

        [Test]
        public void DynamicBuffer_TrimsToInternalBuffer()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);

            Assert.AreEqual(8, buffer.Capacity);
            Assert.AreEqual(0, buffer.Length);

            buffer.EnsureCapacity(100);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            Assert.AreEqual(100, buffer.Capacity);
            Assert.AreEqual(3, buffer.Length);

            buffer.TrimExcess();

            Assert.AreEqual(8, buffer.Capacity);
            Assert.AreEqual(3, buffer.Length);
        }
    }
}

#pragma warning restore 0649
#pragma warning restore 0219 // assigned but its value is never used
