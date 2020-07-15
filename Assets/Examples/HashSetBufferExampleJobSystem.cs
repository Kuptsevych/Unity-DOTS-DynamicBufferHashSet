using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

public class HashSetBufferExampleJobSystem : JobComponentSystem
{
	private EntityQuery _query;

	protected override void OnCreate()
	{
		var queryDesc = new EntityQueryDesc
		{
			None = new[] {ComponentType.ReadOnly<ComponentSystemTag>()},
			All  = new[] {ComponentType.ReadWrite<ExampleComponent>(), ComponentType.ReadOnly<ProcessHashSetBufferComponent>()}
		};

		_query = GetEntityQuery(queryDesc);
	}

	private struct TestHashSetBufferJob : IJobChunk
	{
		[ReadOnly] public float DeltaTime;

		[ReadOnly] public ArchetypeChunkEntityType EntityType;

		public ArchetypeChunkComponentType<ExampleComponent> ExampleComponentType;

		public DynamicBufferHashSetHandler<TestHashSetBufferElement> Buffer;

		public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
		{
			NativeArray<Entity> entities = chunk.GetNativeArray(EntityType);

			NativeArray<ExampleComponent> exampleComponents = chunk.GetNativeArray(ExampleComponentType);

			for (int i = exampleComponents.Length - 1; i >= 0; i--)
			{
				var exampleComponent = exampleComponents[i];

				exampleComponent.Time += DeltaTime;

				Buffer.Init(entities[i]);

				if (Buffer.Length == 0 && !exampleComponent.Remove && exampleComponent.Time > 3f && exampleComponent.Time < 5f)
				{
					DynamicBufferHashSet.TryAdd(Buffer, new TestHashSetBufferElement
					{
						Value = 12345
					});
					DynamicBufferHashSet.TryAdd(Buffer, new TestHashSetBufferElement
					{
						Value = 78901
					});
					DynamicBufferHashSet.TryAdd(Buffer, new TestHashSetBufferElement
					{
						Value = 5674654
					});
				}

				if (exampleComponent.Remove)
				{
					DynamicBufferHashSet.TryRemove(Buffer, new TestHashSetBufferElement
					{
						Value = 78901
					});
					DynamicBufferHashSet.TryRemove(Buffer, new TestHashSetBufferElement
					{
						Value = 5674654
					});
				}

				if (exampleComponent.Time > 7f)
				{
					exampleComponent.Remove = true;
				}

				if (exampleComponent.Time > 10f && exampleComponent.Remove)
				{
					exampleComponent.Remove = false;
					exampleComponent.Check1 = DynamicBufferHashSet.Contains(Buffer, new TestHashSetBufferElement
					{
						Value = 12345
					});
					exampleComponent.Check2 = DynamicBufferHashSet.Contains(Buffer, new TestHashSetBufferElement
					{
						Value = 54321
					});
				}

				exampleComponents[i] = exampleComponent;
			}
		}
	}

	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		ArchetypeChunkEntityType archetypeChunkEntityType = EntityManager.GetArchetypeChunkEntityType();
		var                      exampleComponentType     = GetArchetypeChunkComponentType<ExampleComponent>();

		var job = new TestHashSetBufferJob
		{
			DeltaTime            = Time.DeltaTime,
			EntityType           = archetypeChunkEntityType,
			ExampleComponentType = exampleComponentType,
			Buffer               = DynamicBufferHashSet.GetDynamicBufferHashSetHandler<TestHashSetBufferElement>(this),
		};

		return job.Schedule(_query, inputDeps);
	}
}