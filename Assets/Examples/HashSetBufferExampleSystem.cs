using Unity.Collections;
using Unity.Entities;

public class HashSetBufferExampleSystem : ComponentSystem
{
	private EntityQuery _query;

	protected override void OnCreate()
	{
		var queryDesc = new EntityQueryDesc
		{
			All = new[]
			{
				ComponentType.ReadWrite<ExampleComponent>(),
				ComponentType.ReadOnly<ProcessHashSetBufferComponent>(),
				ComponentType.ReadOnly<ComponentSystemTag>()
			}
		};

		_query = GetEntityQuery(queryDesc);
	}


	protected override void OnUpdate()
	{
		var entities          = _query.ToEntityArray(Allocator.TempJob);
		var exampleComponents = _query.ToComponentDataArray<ExampleComponent>(Allocator.TempJob);

		for (var i = 0; i < entities.Length; i++)
		{
			var exampleComponent = exampleComponents[i];
			var entity           = entities[i];

			exampleComponent.Time += Time.DeltaTime;

			if (DynamicBufferHashSet.Length<TestHashSetBufferElement>(EntityManager, entity) == 0 &&
			    !exampleComponent.Remove && exampleComponent.Time > 3f && exampleComponent.Time < 5f)
			{
				DynamicBufferHashSet.TryAdd(EntityManager, entity, new TestHashSetBufferElement
				{
					Value = 12345
				});
				DynamicBufferHashSet.TryAdd(EntityManager, entity, new TestHashSetBufferElement
				{
					Value = 78901
				});
				DynamicBufferHashSet.TryAdd(EntityManager, entity, new TestHashSetBufferElement
				{
					Value = 5674654
				});
			}

			if (exampleComponent.Remove)
			{
				DynamicBufferHashSet.TryRemove(EntityManager, entity, new TestHashSetBufferElement
				{
					Value = 78901
				});
				DynamicBufferHashSet.TryRemove(EntityManager, entity, new TestHashSetBufferElement
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
				exampleComponent.Check1 = DynamicBufferHashSet.Contains(EntityManager, entity, new TestHashSetBufferElement
				{
					Value = 12345
				});
				exampleComponent.Check2 = DynamicBufferHashSet.Contains(EntityManager, entity, new TestHashSetBufferElement
				{
					Value = 54321
				});
			}

			exampleComponents[i] = exampleComponent;
		}

		_query.CopyFromComponentDataArray(exampleComponents);

		entities.Dispose();
		exampleComponents.Dispose();
	}
}