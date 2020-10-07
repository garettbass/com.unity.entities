using System;
using NUnit.Framework;
using Unity.Collections;

namespace Unity.Entities.Tests
{
    class CreateAndDestroyTests : ECSTestsFixture
    {
        [Test]
        unsafe public void CreateAndDestroyOne()
        {
            var entity = CreateEntityWithDefaultData(10);
            m_Manager.DestroyEntity(entity);
            AssertDoesNotExist(entity);
        }

        [Test]
        unsafe public void DestroyNullIsIgnored()
        {
            m_Manager.DestroyEntity(default(Entity));
        }

        [Test]
        unsafe public void DestroyTwiceIsIgnored()
        {
            var entity = CreateEntityWithDefaultData(10);
            m_Manager.DestroyEntity(entity);
            m_Manager.DestroyEntity(entity);
        }

        [Test]
        unsafe public void EmptyEntityIsNull()
        {
            CreateEntityWithDefaultData(10);
            Assert.IsFalse(m_Manager.Exists(new Entity()));
        }

        [Test]
        unsafe public void CreateAndDestroyTwo()
        {
            var entity0 = CreateEntityWithDefaultData(10);
            var entity1 = CreateEntityWithDefaultData(11);

            m_Manager.DestroyEntity(entity0);

            AssertDoesNotExist(entity0);
            AssertComponentData(entity1, 11);

            m_Manager.DestroyEntity(entity1);
            AssertDoesNotExist(entity0);
            AssertDoesNotExist(entity1);
        }

#if !UNITY_PORTABLE_TEST_RUNNER
        // https://unity3d.atlassian.net/browse/DOTSR-1432
        [TestCaseGeneric(typeof(EcsTestData))]
        [TestCaseGeneric(typeof(EcsState1))]
        unsafe public void CreateZeroEntities<TComponent>()
            where TComponent : struct, IComponentData
        {
            var array = new NativeArray<Entity>(0, Allocator.Temp);
            m_Manager.CreateEntity(m_Manager.CreateArchetype(typeof(TComponent)), array);
            array.Dispose();
        }

#endif

        [Test]
        unsafe public void InstantiateZeroEntities()
        {
            var array = new NativeArray<Entity>(0, Allocator.Temp);

            var srcEntity = m_Manager.CreateEntity(typeof(EcsTestData));
            m_Manager.Instantiate(srcEntity , array);
            array.Dispose();
        }

        [Test]
        unsafe public void CreateAndDestroyThree()
        {
            var entity0 = CreateEntityWithDefaultData(10);
            var entity1 = CreateEntityWithDefaultData(11);

            m_Manager.DestroyEntity(entity0);

            var entity2 = CreateEntityWithDefaultData(12);


            AssertDoesNotExist(entity0);

            AssertComponentData(entity1, 11);
            AssertComponentData(entity2, 12);
        }

        [Test]
        public void CreateEntity_InvalidEntityArchetypeThrows()
        {
            var archetype = new EntityArchetype();
            Assert.Throws<ArgumentException>(() => m_Manager.CreateEntity(archetype));
        }

        [Test]
        public void CreateEntity_PassArray_InvalidEntityArchetypeThrows()
        {
            var arr = new NativeArray<Entity>(10, Allocator.Temp);
            var archetype = new EntityArchetype();
            Assert.Throws<ArgumentException>(() => m_Manager.CreateEntity(archetype, arr));
            arr.Dispose();
        }

        [Test]
        public void CreateEntity_ReturnArray_InvalidEntityArchetypeThrows()
        {
            var archetype = new EntityArchetype();
            Assert.Throws<ArgumentException>(() => m_Manager.CreateEntity(archetype, 10, Allocator.Temp));
        }

        [Test]
        unsafe public void CreateAndDestroyStressTest()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2));
            var entities = new NativeArray<Entity>(10000, Allocator.Persistent);

            m_Manager.CreateEntity(archetype, entities);

            for (int i = 0; i < entities.Length; i++)
                AssertComponentData(entities[i], 0);

            m_Manager.DestroyEntity(entities);
            entities.Dispose();
        }

        [Test]
        unsafe public void CreateAndDestroyShuffleStressTest()
        {
            Entity[] entities = new Entity[10000];
            for (int i = 0; i < entities.Length; i++)
            {
                entities[i] = CreateEntityWithDefaultData(i);
            }

            for (int i = 0; i < entities.Length; i++)
            {
                if (i % 2 == 0)
                    m_Manager.DestroyEntity(entities[i]);
            }

            for (int i = 0; i < entities.Length; i++)
            {
                if (i % 2 == 0)
                {
                    AssertDoesNotExist(entities[i]);
                }
                else
                {
                    AssertComponentData(entities[i], i);
                }
            }

            for (int i = 0; i < entities.Length; i++)
            {
                if (i % 2 == 1)
                    m_Manager.DestroyEntity(entities[i]);
            }

            for (int i = 0; i < entities.Length; i++)
            {
                AssertDoesNotExist(entities[i]);
            }
        }

        [Test]
        unsafe public void InstantiateStressTest()
        {
            var entities = new NativeArray<Entity>(10000, Allocator.Persistent);
            var srcEntity = CreateEntityWithDefaultData(5);

            m_Manager.Instantiate(srcEntity, entities);

            for (int i = 0; i < entities.Length; i++)
                AssertComponentData(entities[i], 5);

            m_Manager.DestroyEntity(entities);
            entities.Dispose();
        }

        [Test]
        public void AddRemoveComponent()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2));

            var entity = m_Manager.CreateEntity(archetype);
            Assert.IsTrue(m_Manager.HasComponent<EcsTestData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<EcsTestData2>(entity));
            Assert.IsFalse(m_Manager.HasComponent<EcsTestData3>(entity));

            m_Manager.AddComponentData<EcsTestData3>(entity, new EcsTestData3(3));
            Assert.IsTrue(m_Manager.HasComponent<EcsTestData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<EcsTestData2>(entity));
            Assert.IsTrue(m_Manager.HasComponent<EcsTestData3>(entity));

            Assert.AreEqual(3, m_Manager.GetComponentData<EcsTestData3>(entity).value0);
            Assert.AreEqual(3, m_Manager.GetComponentData<EcsTestData3>(entity).value1);
            Assert.AreEqual(3, m_Manager.GetComponentData<EcsTestData3>(entity).value2);

            m_Manager.RemoveComponent<EcsTestData2>(entity);
            Assert.IsTrue(m_Manager.HasComponent<EcsTestData>(entity));
            Assert.IsFalse(m_Manager.HasComponent<EcsTestData2>(entity));
            Assert.IsTrue(m_Manager.HasComponent<EcsTestData3>(entity));

            Assert.AreEqual(3, m_Manager.GetComponentData<EcsTestData3>(entity).value0);
            Assert.AreEqual(3, m_Manager.GetComponentData<EcsTestData3>(entity).value1);
            Assert.AreEqual(3, m_Manager.GetComponentData<EcsTestData3>(entity).value2);

            m_Manager.DestroyEntity(entity);
        }

        [Test]
        public void RemoveMultipleComponents()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2), typeof(EcsTestData3), typeof(EcsTestData4));
            var entity = m_Manager.CreateEntity(archetype);

            m_Manager.RemoveComponent(entity, new ComponentTypes(typeof(EcsTestData2), typeof(EcsTestData4)));
            Assert.AreEqual(m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData3)), m_Manager.GetChunk(entity).Archetype);
            m_Manager.DestroyEntity(entity);
        }

        [Test]
        public void RemoveMultipleComponents_EmptyComponentTypes()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2), typeof(EcsTestData3), typeof(EcsTestData4));
            var entity = m_Manager.CreateEntity(archetype);

            m_Manager.RemoveComponent(entity, new ComponentTypes());
            Assert.AreEqual(archetype, m_Manager.GetChunk(entity).Archetype);
            m_Manager.DestroyEntity(entity);
        }

        [Test]
        public void RemoveMultipleComponents_ZeroComponentsPresent()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2), typeof(EcsTestData3));
            var entity = m_Manager.CreateEntity(archetype);

            m_Manager.RemoveComponent(entity, new ComponentTypes(typeof(EcsTestTag), typeof(EcsTestData4)));
            Assert.AreEqual(archetype, m_Manager.GetChunk(entity).Archetype);
            m_Manager.DestroyEntity(entity);
        }

        [Test]
        public void AddComponentSetsValueOfComponentToDefault()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData));
            var dummyEntity = m_Manager.CreateEntity(archetype);
            m_Manager.Debug.PoisonUnusedDataInAllChunks(archetype, 0xCD);

            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponent(entity, ComponentType.ReadWrite<EcsTestData>());
            Assert.AreEqual(0, m_Manager.GetComponentData<EcsTestData>(entity).value);

            m_Manager.DestroyEntity(dummyEntity);
            m_Manager.DestroyEntity(entity);
        }

        [Test]
        public void ReadOnlyAndNonReadOnlyArchetypeAreEqual()
        {
            var arch = m_Manager.CreateArchetype(ComponentType.ReadOnly(typeof(EcsTestData)));
            var arch2 = m_Manager.CreateArchetype(typeof(EcsTestData));
            Assert.AreEqual(arch, arch2);
        }

        [Test]
        public void ExcludeArchetypeReactToAddRemoveComponent()
        {
            var subtractiveArch = m_Manager.CreateEntityQuery(ComponentType.Exclude(typeof(EcsTestData)), typeof(EcsTestData2));

            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2));

            var entity = m_Manager.CreateEntity(archetype);
            Assert.AreEqual(0, subtractiveArch.CalculateEntityCount());

            m_Manager.RemoveComponent<EcsTestData>(entity);
            Assert.AreEqual(1, subtractiveArch.CalculateEntityCount());

            m_Manager.AddComponentData<EcsTestData>(entity, new EcsTestData());
            Assert.AreEqual(0, subtractiveArch.CalculateEntityCount());
        }

        [Test]
        public void ChunkCountsAreCorrect()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData));
            var entity = m_Manager.CreateEntity(archetype);

            Assert.AreEqual(1, archetype.ChunkCount);

            m_Manager.AddComponent(entity, typeof(EcsTestData2));
            Assert.AreEqual(0, archetype.ChunkCount);

            unsafe {
                Assert.IsTrue(archetype.Archetype->Chunks.Count == 0);
                Assert.AreEqual(0, archetype.Archetype->EntityCount);

                var archetype2 = m_Manager.GetCheckedEntityDataAccess()->EntityComponentStore->GetArchetype(entity);
                Assert.AreEqual(1, archetype2->Chunks.Count);
                Assert.AreEqual(1, archetype2->EntityCount);
            }
        }

        [Test]
        public void AddComponentsWorks()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData));
            var entity = m_Manager.CreateEntity(archetype);

            // Create dummy archetype with unrelated type to override the internal cache in the entity manager
            // This exposed a bug in AddComponent
            m_Manager.CreateArchetype(typeof(EcsTestData4));

            var typesToAdd = new ComponentTypes(new ComponentType[] {typeof(EcsTestData3), typeof(EcsTestData2)});
            m_Manager.AddComponents(entity, typesToAdd);

            var expectedTotalTypes = new ComponentTypes(new ComponentType[] {typeof(EcsTestData2), typeof(EcsTestData3), typeof(EcsTestData)});
            var actualTotalTypes = m_Manager.GetComponentTypes(entity);

            Assert.AreEqual(expectedTotalTypes.Length, actualTotalTypes.Length);
            for (var i = 0; i < expectedTotalTypes.Length; ++i)
                Assert.AreEqual(expectedTotalTypes.GetTypeIndex(i), actualTotalTypes[i].TypeIndex);

            actualTotalTypes.Dispose();
        }

        [Test]
        public void GetAllEntitiesCorrectCount([Values] bool immediate)
        {
            var archetype0 = m_Manager.CreateArchetype(typeof(EcsTestData));
            var entity = m_Manager.CreateEntity(archetype0);

            var archetype1 = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2));
            var moreEntities = new NativeArray<Entity>(1024, Allocator.Temp);
            m_Manager.CreateEntity(archetype1, moreEntities);

            var foundEntities = immediate ? m_Manager.GetAllEntitiesImmediate() : m_Manager.GetAllEntities();
            Assert.AreEqual(1024 + 1, foundEntities.Length);

            foundEntities.Dispose();
            moreEntities.Dispose();
        }

        [Test]
        public void GetAllEntitiesCorrectValues([Values] bool immediate)
        {
            var archetype0 = m_Manager.CreateArchetype(typeof(EcsTestData));
            var entity = m_Manager.CreateEntity(archetype0);
            m_Manager.SetComponentData(entity, new EcsTestData { value = 1000000});

            var archetype1 = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2));
            var moreEntities = new NativeArray<Entity>(1024, Allocator.Temp);
            m_Manager.CreateEntity(archetype1, moreEntities);
            for (int i = 0; i < 1024; i++)
            {
                m_Manager.SetComponentData(moreEntities[i], new EcsTestData { value = i + 1});
            }

            var foundEntities = immediate ? m_Manager.GetAllEntitiesImmediate() : m_Manager.GetAllEntities();

            Assert.AreEqual(1025, foundEntities.Length);

            var sum = 0;
            var expectedSum = 1524800;
            for (int i = 0; i < 1025; i++)
            {
                sum += m_Manager.GetComponentData<EcsTestData>(foundEntities[i]).value;
            }

            Assert.AreEqual(expectedSum, sum);

            foundEntities.Dispose();
            moreEntities.Dispose();
        }

        [Test]
        public void AddComponentsWithTypeIndicesWorks()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData));
            var entity = m_Manager.CreateEntity(archetype);

            var typesToAdd = new ComponentTypes(typeof(EcsTestData3), typeof(EcsTestData2));

            m_Manager.AddComponents(entity, typesToAdd);

            var expectedTotalTypes = new ComponentTypes(typeof(EcsTestData2), typeof(EcsTestData3), typeof(EcsTestData));

            var actualTotalTypes = m_Manager.GetComponentTypes(entity);

            Assert.AreEqual(expectedTotalTypes.Length, actualTotalTypes.Length);
            for (var i = 0; i < expectedTotalTypes.Length; ++i)
                Assert.AreEqual(expectedTotalTypes.GetTypeIndex(i), actualTotalTypes[i].TypeIndex);

            actualTotalTypes.Dispose();
        }

#if !UNITY_PORTABLE_TEST_RUNNER
        // https://unity3d.atlassian.net/browse/DOTSR-1432
        // TODO: IL2CPP_TEST_RUNNER can't handle Assert.That Throws

        [Test]
        public void AddComponent_InvalidEntity1_ShouldThrow()
        {
            var invalidEnt = m_Manager.CreateEntity();
            m_Manager.DestroyEntity(invalidEnt);
            Assert.That(() => { m_Manager.AddComponent<EcsTestData>(invalidEnt); },
                Throws.InvalidOperationException.With.Message.Contains("entity does not exist"));
        }

        [Test]
        public void AddComponentBatched_InvalidEntities_ShouldThrow([Values(10, 100)] int entityCount)
        {
            var invalidEnt = m_Manager.CreateEntity();
            m_Manager.DestroyEntity(invalidEnt);

            for (int i = 0; i < entityCount; ++i)
            {
                m_Manager.CreateEntity();
            }
            var entities = m_Manager.UniversalQuery.ToEntityArray(Allocator.TempJob);
            Assert.AreEqual(entities.Length, entityCount);

            entities[0] = invalidEnt;
            Assert.That(() => { m_Manager.AddComponent<EcsTestData>(entities); },
                Throws.ArgumentException.With.Message.Contains("All entities passed to EntityManager must exist"));
            entities.Dispose();
        }

#endif

        [Test]
        public void AddComponentsWithSharedComponentsWorks()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestSharedComp));
            var entity = m_Manager.CreateEntity(archetype);

            var sharedComponentValue = new EcsTestSharedComp(1337);
            m_Manager.SetSharedComponentData(entity, sharedComponentValue);

            var typesToAdd = new ComponentTypes(typeof(EcsTestData3), typeof(EcsTestSharedComp2), typeof(EcsTestData2));

            m_Manager.AddComponents(entity, typesToAdd);

            Assert.AreEqual(m_Manager.GetSharedComponentData<EcsTestSharedComp>(entity), sharedComponentValue);

            var expectedTotalTypes = new ComponentTypes(typeof(EcsTestData), typeof(EcsTestData2),
                typeof(EcsTestData3), typeof(EcsTestSharedComp), typeof(EcsTestSharedComp2));

            var actualTotalTypes = m_Manager.GetComponentTypes(entity);

            Assert.AreEqual(expectedTotalTypes.Length, actualTotalTypes.Length);
            for (var i = 0; i < expectedTotalTypes.Length; ++i)
                Assert.AreEqual(expectedTotalTypes.GetTypeIndex(i), actualTotalTypes[i].TypeIndex);

            actualTotalTypes.Dispose();
        }

        [Test]
        public void InstantiateWithSystemStateComponent()
        {
            for (int i = 0; i < 1000; ++i)
            {
                var src = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsState1), typeof(EcsTestData2));

                m_Manager.SetComponentData(src, new EcsTestData {value = i * 123});
                m_Manager.SetComponentData(src, new EcsTestData2 {value0 = i * 456, value1 = i * 789});

                var dst = m_Manager.Instantiate(src);

                Assert.AreEqual(i * 123, m_Manager.GetComponentData<EcsTestData>(dst).value);
                Assert.AreEqual(i * 456, m_Manager.GetComponentData<EcsTestData2>(dst).value0);
                Assert.AreEqual(i * 789, m_Manager.GetComponentData<EcsTestData2>(dst).value1);

                Assert.IsFalse(m_Manager.HasComponent<EcsState1>(dst));
            }
        }

        [TypeManager.ForcedMemoryOrderingAttribute(2)]
        struct EcsSharedForcedOrder : ISharedComponentData
        {
            public int Value;

            public EcsSharedForcedOrder(int value)
            {
                Value = value;
            }
        }

        [TypeManager.ForcedMemoryOrderingAttribute(1)]
        struct EcsSharedStateForcedOrder : ISystemStateSharedComponentData
        {
            public int Value;

            public EcsSharedStateForcedOrder(int value)
            {
                Value = value;
            }
        }

        [Test]
        public void InstantiateWithSharedSystemStateComponent()
        {
            var srcEntity = m_Manager.CreateEntity();

            var sharedValue = new EcsSharedForcedOrder(123);
            var systemValue = new EcsSharedStateForcedOrder(234);

            m_Manager.AddSharedComponentData(srcEntity, sharedValue);
            m_Manager.AddSharedComponentData(srcEntity, systemValue);

            var versionSharedBefore = m_Manager.GetSharedComponentOrderVersion(sharedValue);
            var versionSystemBefore = m_Manager.GetSharedComponentOrderVersion(systemValue);

            var dstEntity = m_Manager.Instantiate(srcEntity);
            var sharedValueCopied = m_Manager.GetSharedComponentData<EcsSharedForcedOrder>(dstEntity);

            var versionSharedAfter = m_Manager.GetSharedComponentOrderVersion(sharedValue);
            var versionSystemAfter = m_Manager.GetSharedComponentOrderVersion(systemValue);

            Assert.IsTrue(m_Manager.HasComponent<EcsSharedForcedOrder>(dstEntity));
            Assert.IsFalse(m_Manager.HasComponent<EcsSharedStateForcedOrder>(dstEntity));

            Assert.AreEqual(sharedValue, sharedValueCopied);

            Assert.AreNotEqual(versionSharedBefore, versionSharedAfter);
            Assert.AreEqual(versionSystemBefore, versionSystemAfter);
        }

        [Test]
        public void AddTagComponentTwiceByValue()
        {
            var entity = m_Manager.CreateEntity();

            var added0 = m_Manager.AddComponentData(entity, new EcsTestTag());
            var added1 = m_Manager.AddComponentData(entity, new EcsTestTag());

            Assert.That(added0, Is.True);
            Assert.That(added1, Is.False);
        }

        [Test]
        public void AddTagComponentTwiceByType()
        {
            var entity = m_Manager.CreateEntity();

            var added0 = m_Manager.AddComponent(entity, ComponentType.ReadWrite<EcsTestTag>());
            var added1 = m_Manager.AddComponent(entity, ComponentType.ReadWrite<EcsTestTag>());

            Assert.That(added0, Is.True);
            Assert.That(added1, Is.False);
        }

        [Test]
        public void AddTagComponentTwiceToGroup()
        {
            m_Manager.CreateEntity();

            m_Manager.AddComponent(m_Manager.UniversalQuery, ComponentType.ReadWrite<EcsTestTag>());

            // Not an error (null operation)
            m_Manager.AddComponent(m_Manager.UniversalQuery, ComponentType.ReadWrite<EcsTestTag>());
        }

        [Test]
        public void AddComponentThenSetThenAddDoesNotChangeValue()
        {
            m_Manager.CreateEntity();

            var value0 = new EcsTestData(1);
            m_Manager.AddComponent(m_Manager.UniversalQuery, ComponentType.ReadWrite<EcsTestData>());
            var entities = m_Manager.UniversalQuery.ToEntityArray(Allocator.Persistent);
            for (int i = 0; i < entities.Length; i++)
                m_Manager.SetComponentData(entities[i], value0);

            var addedQuery = m_Manager.CreateEntityQuery(typeof(EcsTestData));
            m_Manager.AddComponent(addedQuery, ComponentType.ReadWrite<EcsTestData>());
            var values = addedQuery.ToComponentDataArray<EcsTestData>(Allocator.Persistent);
            for (int i = 0; i < entities.Length; i++)
                Assert.AreEqual(value0.value, values[i].value);

            addedQuery.Dispose();
            entities.Dispose();
            values.Dispose();
        }

        [Test]
        public void AddComponentTwiceByTypeArray()
        {
            var entity = m_Manager.CreateEntity();

            m_Manager.AddComponents(entity, new ComponentTypes(ComponentType.ReadWrite<EcsTestTag>()));
            var archetypeBefore = m_Manager.GetChunk(entity).Archetype;
            m_Manager.AddComponents(entity, new ComponentTypes(ComponentType.ReadWrite<EcsTestTag>()));
            var archetypeAfter = m_Manager.GetChunk(entity).Archetype;
            Assert.AreEqual(archetypeBefore, archetypeAfter);

            m_Manager.AddComponents(entity, new ComponentTypes(ComponentType.ReadWrite<EcsTestData>()));
            archetypeBefore = m_Manager.GetChunk(entity).Archetype;
            m_Manager.AddComponents(entity, new ComponentTypes(ComponentType.ReadWrite<EcsTestData>()));
            archetypeAfter = m_Manager.GetChunk(entity).Archetype;
            Assert.AreEqual(archetypeBefore, archetypeAfter);
        }

        [Test]
        public void AddChunkComponentTwice()
        {
            var entity = m_Manager.CreateEntity();

            var added0 = m_Manager.AddChunkComponentData<EcsTestTag>(entity);
            var added1 = m_Manager.AddChunkComponentData<EcsTestTag>(entity);

            var added2 = m_Manager.AddChunkComponentData<EcsTestData>(entity);
            var added3 = m_Manager.AddChunkComponentData<EcsTestData>(entity);

            Assert.That(added0, Is.True);
            Assert.That(added1, Is.False);
            Assert.That(added2, Is.True);
            Assert.That(added3, Is.False);
        }

        [Test]
        public void AddChunkComponentToGroupTwice()
        {
            m_Manager.CreateEntity();

            m_Manager.AddChunkComponentData(m_Manager.UniversalQuery, new EcsTestTag());
            Assert.Throws<ArgumentException>(() => m_Manager.AddChunkComponentData(m_Manager.UniversalQuery, new EcsTestTag()));

            m_Manager.AddChunkComponentData(m_Manager.UniversalQuery, new EcsTestData {value = 123});
            Assert.Throws<ArgumentException>(() => m_Manager.AddChunkComponentData(m_Manager.UniversalQuery, new EcsTestData {value = 123}));
            Assert.Throws<ArgumentException>(() => m_Manager.AddChunkComponentData(m_Manager.UniversalQuery, new EcsTestData {value = 456}));
        }

        [Test]
        public void AddSharedComponentTwice()
        {
            var entity = m_Manager.CreateEntity();

            var added0 = m_Manager.AddSharedComponentData(entity, new EcsTestSharedComp());
            var added1 = m_Manager.AddSharedComponentData(entity, new EcsTestSharedComp());

            Assert.That(added0, Is.True);
            Assert.That(added1, Is.False);
        }

        [Test]
        public void SetArchetype_InvalidEntityArchetypeThrows()
        {
            var entity = m_Manager.CreateEntity();
            var archetype = new EntityArchetype();
            Assert.Throws<ArgumentException>(delegate { m_Manager.SetArchetype(entity, archetype); });
        }

        [Test]
        public void SetArchetypeWithSharedComponentWorks()
        {
            var entity = m_Manager.CreateEntity();

            m_Manager.AddComponentData(entity, new EcsTestData(1));
            m_Manager.AddComponentData(entity, new EcsTestData2(2));
            m_Manager.AddSharedComponentData(entity, new EcsTestSharedComp(3));
            m_Manager.AddSharedComponentData(entity, new EcsTestSharedComp3(4));
            var newArchetype = m_Manager.CreateArchetype(typeof(EcsTestData2), typeof(EcsTestData3), typeof(EcsTestSharedComp3), typeof(EcsTestSharedComp2));

            m_Manager.SetArchetype(entity, newArchetype);
            Assert.AreEqual(newArchetype, m_Manager.GetChunk(entity).Archetype);

            Assert.AreEqual(2, m_Manager.GetComponentData<EcsTestData2>(entity).value0);
            Assert.AreEqual(0, m_Manager.GetComponentData<EcsTestData3>(entity).value0);
            Assert.AreEqual(0, m_Manager.GetSharedComponentData<EcsTestSharedComp2>(entity).value0);
            Assert.AreEqual(4, m_Manager.GetSharedComponentData<EcsTestSharedComp3>(entity).value0);
        }

        [Test]
        public void SetArchetypeRemovingStateComponentThrows()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsState1));
            Assert.Throws<ArgumentException>(() => m_Manager.SetArchetype(entity, m_Manager.CreateArchetype(typeof(EcsTestData))));
            Assert.Throws<ArgumentException>(() => m_Manager.SetArchetype(entity, m_Manager.CreateArchetype()));
            Assert.Throws<ArgumentException>(() => m_Manager.SetArchetype(entity, m_Manager.CreateArchetype(typeof(EcsStateTag1))));
        }

        [Test]
        public void SetArchetypePreservingStateComponentNoThrows()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2), typeof(EcsState1));
            Assert.DoesNotThrow(() => m_Manager.SetArchetype(entity, m_Manager.CreateArchetype(typeof(EcsState1))));
        }

        [Test]
        public void SetArchetypeAddingStateComponent()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsState1));
            var newArchetype = m_Manager.CreateArchetype(typeof(EcsState1), typeof(EcsStateTag1));
            m_Manager.SetArchetype(entity, newArchetype);
            Assert.AreEqual(newArchetype, m_Manager.GetChunk(entity).Archetype);
        }

        [Test]
        public void AddComponentTagTwiceWithEntityArray()
        {
            var entities = new NativeArray<Entity>(3, Allocator.TempJob);

            entities[0] = m_Manager.CreateEntity();
            entities[1] = m_Manager.CreateEntity(typeof(EcsTestTag));
            entities[2] = m_Manager.CreateEntity(typeof(EcsTestTag), typeof(EcsTestData2));

            m_Manager.AddComponent(entities, typeof(EcsTestTag));

            Assert.IsTrue(m_Manager.HasComponent<EcsTestTag>(entities[0]));
            Assert.IsTrue(m_Manager.HasComponent<EcsTestTag>(entities[1]));
            Assert.IsTrue(m_Manager.HasComponent<EcsTestTag>(entities[2]));

            entities.Dispose();
        }

        [Test]
        public void AddComponentTagWithDuplicateEntities()
        {
            var entities = new NativeArray<Entity>(2, Allocator.TempJob);
            var e = m_Manager.CreateEntity();
            entities[0] = e;
            entities[1] = e;

            m_Manager.AddComponent(entities, typeof(EcsTestTag));

            Assert.IsTrue(m_Manager.HasComponent<EcsTestTag>(e));

            entities.Dispose();
        }

        [Test]
        public void AddComponentTagWithMultipleDuplicateEntities()
        {
            var entities = new NativeArray<Entity>(5, Allocator.TempJob);
            var e1 = m_Manager.CreateEntity();
            var e2 = m_Manager.CreateEntity();
            var e3 = m_Manager.CreateEntity();

            // e1 and e2 have duplicates, e3 is unique.
            entities[0] = e1;
            entities[1] = e2;
            entities[2] = e1;
            entities[3] = e3;
            entities[4] = e2;

            m_Manager.AddComponent(entities, typeof(EcsTestTag));

            Assert.IsTrue(m_Manager.HasComponent<EcsTestTag>(e1));
            Assert.IsTrue(m_Manager.HasComponent<EcsTestTag>(e2));
            Assert.IsTrue(m_Manager.HasComponent<EcsTestTag>(e3));

            entities.Dispose();
        }

        [Test]
        public void AddComponentTwiceWithEntityCommandBuffer()
        {
            using (var ecb = new EntityCommandBuffer(Allocator.TempJob))
            {
                var entity = ecb.CreateEntity();
                ecb.AddComponent(entity, new EcsTestTag());
                ecb.AddComponent(entity, new EcsTestTag());
                Assert.DoesNotThrow(() => ecb.Playback(m_Manager));
            }

            // without fixup

            using (var ecb = new EntityCommandBuffer(Allocator.TempJob))
            {
                var entity = ecb.CreateEntity();
                ecb.AddComponent(entity, new EcsTestData());
                ecb.AddComponent(entity, new EcsTestData());
                Assert.DoesNotThrow(() => ecb.Playback(m_Manager));
            }

            // with fixup

            using (var ecb = new EntityCommandBuffer(Allocator.TempJob))
            {
                var entity = ecb.CreateEntity();
                var other = ecb.CreateEntity();
                ecb.AddComponent(entity, new EcsTestDataEntity { value1 = other });
                ecb.AddComponent(entity, new EcsTestDataEntity { value1 = other });
                Assert.DoesNotThrow(() => ecb.Playback(m_Manager));
            }
        }

        [Test]
        public void AddBufferComponentTwice()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddBuffer<EcsIntElement>(entity);
            Assert.DoesNotThrow(() => m_Manager.AddBuffer<EcsIntElement>(entity));
        }

        [Test]
        public void DestroyEntityQueryWithLinkedEntityGroupPartialDestroyThrows()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestTag));
            var child = m_Manager.CreateEntity(typeof(EcsTestData2));

            var group = m_Manager.AddBuffer<LinkedEntityGroup>(entity);
            group.Add(entity);
            group.Add(child);


            var query = m_Manager.CreateEntityQuery(typeof(EcsTestTag));
            // we are destroying entity but it has a LinkedEntityGroup and child is not being destroyed. That's an error.
            Assert.Throws<ArgumentException>(() => m_Manager.DestroyEntity(query));
            // Just double checking that its a precondition & no leaking state
            Assert.Throws<ArgumentException>(() => m_Manager.DestroyEntity(query));
            Assert.AreEqual(2, m_Manager.UniversalQuery.CalculateEntityCount());

            // And after failed destroy, correct destroys do work
            m_Manager.DestroyEntity(m_Manager.UniversalQuery);
            Assert.AreEqual(0, m_Manager.UniversalQuery.CalculateEntityCount());
        }

        [Test]
        public void GetCreatedAndDestroyedEntities()
        {
            using (var state = new NativeList<int>(Allocator.TempJob))
            using (var created = new NativeList<Entity>(Allocator.TempJob))
            using (var destroyed = new NativeList<Entity>(Allocator.TempJob))
            {
                // Create e0 & e1
                var e0 = m_Manager.CreateEntity();
                var e1 = m_Manager.CreateEntity();
                m_Manager.GetCreatedAndDestroyedEntitiesAsync(state, created, destroyed).Complete();
                Assert.AreEqual(new[] { e0, e1 }, created.ToArray());
                Assert.AreEqual(0, destroyed.Length);

                // Create e3, destroy e0
                var e3 = m_Manager.CreateEntity();
                m_Manager.DestroyEntity(e0);
                m_Manager.GetCreatedAndDestroyedEntitiesAsync(state, created, destroyed).Complete();
                Assert.AreEqual(new[] { e3 }, created.ToArray());
                Assert.AreEqual(new[] { e0 }, destroyed.ToArray());

                // Change nothing
                m_Manager.GetCreatedAndDestroyedEntitiesAsync(state, created, destroyed).Complete();
                Assert.AreEqual(0, created.Length);
                Assert.AreEqual(0, destroyed.Length);

                // Destroy e2 and e3
                m_Manager.DestroyEntity(e1);
                m_Manager.DestroyEntity(e3);
                m_Manager.GetCreatedAndDestroyedEntitiesAsync(state, created, destroyed).Complete();
                Assert.AreEqual(0, created.Length);
                Assert.AreEqual(new[] { e1, e3 }, destroyed.ToArray());

                // Create e4
                var e4 = m_Manager.CreateEntity();
                m_Manager.GetCreatedAndDestroyedEntitiesAsync(state, created, destroyed).Complete();
                Assert.AreEqual(new[] { e4 }, created.AsArray());
                Assert.AreEqual(0, destroyed.Length);

                // Create & Destroy
                m_Manager.DestroyEntity(m_Manager.CreateEntity());;
                m_Manager.GetCreatedAndDestroyedEntitiesAsync(state, created, destroyed).Complete();
                Assert.AreEqual(0, created.Length);
                Assert.AreEqual(0, destroyed.Length);
            }
        }
    }
}
