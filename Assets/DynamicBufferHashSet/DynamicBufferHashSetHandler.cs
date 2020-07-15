using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

public struct DynamicBufferHashSetHandler<T> where T : struct, IBufferElementData, IEqualityComparer<T>
{
	[NativeDisableParallelForRestriction] public BufferFromEntity<T>                        ItemsBufferFromEntity;
	[NativeDisableParallelForRestriction] public BufferFromEntity<BufferHashSetElementData> HashSetBufferFromEntity;

	[NativeDisableParallelForRestriction] public DynamicBuffer<T>                        ItemsBuffer;
	[NativeDisableParallelForRestriction] public DynamicBuffer<BufferHashSetElementData> HashSetBuffer;

	public void Init(Entity entity)
	{
		Initialized   = true;
		ItemsBuffer   = ItemsBufferFromEntity[entity];
		HashSetBuffer = HashSetBufferFromEntity[entity];
	}

	public int Length => ItemsBuffer.Length;

	public bool Initialized { get; private set; }
}