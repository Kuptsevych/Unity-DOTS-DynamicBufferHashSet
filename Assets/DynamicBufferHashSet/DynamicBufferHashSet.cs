using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct DynamicBufferHashSet
{
	private const int MinBufferCapacity = 32;

	public static DynamicBufferHashSetHandler<T> GetDynamicBufferHashSetHandler<T>(ComponentSystemBase componentSystemBase)
		where T : struct, IBufferElementData, IEqualityComparer<T>
	{
		return new DynamicBufferHashSetHandler<T>
		{
			ItemsBufferFromEntity   = componentSystemBase.GetBufferFromEntity<T>(),
			HashSetBufferFromEntity = componentSystemBase.GetBufferFromEntity<BufferHashSetElementData>()
		};
	}

	public static bool TryAdd<T>(DynamicBufferHashSetHandler<T> handler, T element)
		where T : struct, IBufferElementData, IEqualityComparer<T>
	{
		if (!CheckHandlerInitialization(handler)) return false;
		return TryAdd(handler.ItemsBuffer, handler.HashSetBuffer, element);
	}

	public static bool TryRemove<T>(DynamicBufferHashSetHandler<T> handler, T element)
		where T : struct, IBufferElementData, IEqualityComparer<T>
	{
		if (!CheckHandlerInitialization(handler)) return false;
		return TryRemove(handler.ItemsBuffer, handler.HashSetBuffer, element);
	}

	public static bool Contains<T>(DynamicBufferHashSetHandler<T> handler, T element)
		where T : struct, IBufferElementData, IEqualityComparer<T>
	{
		if (!CheckHandlerInitialization(handler)) return false;
		return Contains(handler.ItemsBuffer, handler.HashSetBuffer, element);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool CheckHandlerInitialization<T>(DynamicBufferHashSetHandler<T> handler)
		where T : struct, IBufferElementData, IEqualityComparer<T>
	{
		if (!handler.Initialized)
		{
#if UNITY_EDITOR
			Debug.LogError("Buffer hash set handler not initialized");
#endif
			return false;
		}

		return true;
	}

	public static DynamicBuffer<T> AddDynamicBufferHashSet<T>(EntityManager entityManager, Entity entity, int capacity)
		where T : struct, IBufferElementData, IEqualityComparer<T>
	{
		var buffer        = entityManager.AddBuffer<T>(entity);
		var hashMapBuffer = entityManager.AddBuffer<BufferHashSetElementData>(entity);

		for (int i = Mathf.Max(capacity, MinBufferCapacity) - 1; i >= 0; i--)
		{
			hashMapBuffer.Add(new BufferHashSetElementData(-1, true));
		}

		return buffer;
	}

	public static void Clear<T>(EntityManager entityManager, Entity entity, int capacity) where T : struct, IBufferElementData, IEqualityComparer<T>
	{
		if (entityManager.HasComponent(entity, typeof(T)))
		{
			var buffer = entityManager.GetBuffer<T>(entity);
			buffer.Clear();
			buffer.EnsureCapacity(capacity);
		}

		if (entityManager.HasComponent(entity, typeof(BufferHashSetElementData)))
		{
			var hashMapBuffer = entityManager.GetBuffer<BufferHashSetElementData>(entity);
			hashMapBuffer.Clear();
			hashMapBuffer.EnsureCapacity(capacity);
		}
	}


	public static bool TryAdd<T>(EntityManager entityManager, Entity entity, T element) where T : struct, IBufferElementData, IEqualityComparer<T>
	{
		var buffer = entityManager.GetBuffer<T>(entity);

		if (buffer.Length >= buffer.Capacity)
		{
			Debug.Log("Error, buffer is full");
			return false;
		}

		DynamicBuffer<BufferHashSetElementData> hashMapBuffer = entityManager.GetBuffer<BufferHashSetElementData>(entity);

		return TryAdd(buffer, hashMapBuffer, element);
	}

	public static bool TryAdd<T>(DynamicBuffer<T> buffer, DynamicBuffer<BufferHashSetElementData> hashMapBuffer, T element)
		where T : struct, IBufferElementData, IEqualityComparer<T>
	{
		if (buffer.Length >= buffer.Capacity)
		{
			Debug.Log("Error, buffer is full");
			return false;
		}

		int index = GetIndex(element, buffer.Capacity);

		if (hashMapBuffer[index].Empty)
		{
			int newElementIndex = buffer.Add(element);

			var hashMapBufferItem = hashMapBuffer[index];
			hashMapBufferItem.Index = newElementIndex;
			hashMapBufferItem.Empty = false;
			hashMapBuffer[index]    = hashMapBufferItem;

			var hashMapItemByIndex = hashMapBuffer[newElementIndex];
			hashMapItemByIndex.HashMapIndex = index;
			hashMapBuffer[newElementIndex]  = hashMapItemByIndex;

			return true;
		}

		int hashIndex = index;
		int nextIndex = hashMapBuffer[hashIndex].NextItem;
		for (int i = 0; i < buffer.Capacity; i++)
		{
			if (nextIndex < 0)
			{
				int newElementIndex = buffer.Add(element);

				hashMapBuffer.Add(new BufferHashSetElementData(newElementIndex, false, hashIndex, -1, index));

				hashMapBuffer[hashIndex] = new BufferHashSetElementData
				(hashMapBuffer[hashIndex].Index, hashMapBuffer[hashIndex].Empty, hashMapBuffer[hashIndex].PrevItem, hashMapBuffer.Length - 1,
					hashMapBuffer[hashIndex].HashMapIndex);

				var hashMapItemByIndex = hashMapBuffer[newElementIndex];
				hashMapItemByIndex.HashMapIndex = index;
				hashMapBuffer[newElementIndex]  = hashMapItemByIndex;

				return true;
			}

			if (hashMapBuffer[nextIndex].Empty)
			{
				int newElementIndex = buffer.Add(element);

				hashMapBuffer.Add(new BufferHashSetElementData(newElementIndex, false, hashIndex, -1, index));

				hashMapBuffer[hashIndex] = new BufferHashSetElementData
				(hashMapBuffer[hashIndex].Index, hashMapBuffer[hashIndex].Empty, hashMapBuffer[hashIndex].PrevItem, hashMapBuffer.Length - 1,
					hashMapBuffer[hashIndex].HashMapIndex);

				var hashMapItemByIndex = hashMapBuffer[newElementIndex];
				hashMapItemByIndex.HashMapIndex = index;
				hashMapBuffer[newElementIndex]  = hashMapItemByIndex;

				return true;
			}

			if (buffer[hashMapBuffer[nextIndex].Index].Equals(element, buffer[hashMapBuffer[nextIndex].Index]))
			{
#if UNITY_EDITOR || DEBUG
				Debug.Log("Already has item, add error::" + index);
#endif
				return false;
			}

			hashIndex = nextIndex;
			nextIndex = hashMapBuffer[nextIndex].NextItem;
		}

		return false;
	}

	public static bool TryRemove<T>(EntityManager entityManager, Entity entity, T element) where T : struct, IBufferElementData, IEqualityComparer<T>
	{
		DynamicBuffer<T> buffer = entityManager.GetBuffer<T>(entity);

		DynamicBuffer<BufferHashSetElementData> hashMapBuffer = entityManager.GetBuffer<BufferHashSetElementData>(entity);

		int itemIndex = FindBufferItemIndex(buffer, hashMapBuffer, element);

		if (itemIndex < 0) return false;

		RemoveItemAt(itemIndex, buffer, hashMapBuffer);

		return true;
	}

	public static bool TryRemove<T>(DynamicBuffer<T> buffer, DynamicBuffer<BufferHashSetElementData> hashMapBuffer, T element)
		where T : struct, IBufferElementData, IEqualityComparer<T>
	{
		int itemIndex = FindBufferItemIndex(buffer, hashMapBuffer, element);

		if (itemIndex < 0) return false;

		RemoveItemAt(itemIndex, buffer, hashMapBuffer);

		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void RemoveItemAt<T>(int index, DynamicBuffer<T> buffer, DynamicBuffer<BufferHashSetElementData> hashMapBuffer)
		where T : struct, IBufferElementData, IEqualityComparer<T>
	{
		int lastItemIndex = buffer.Length - 1;

		buffer[index] = buffer[lastItemIndex];
		buffer.RemoveAt(lastItemIndex);

		if (lastItemIndex != index)
		{
			SetHashItemIndex(ref hashMapBuffer, index, lastItemIndex);
			RemoveHashItem(ref hashMapBuffer, hashMapBuffer[index].HashMapIndex, index, buffer.Capacity);
			SetHashItemHashIndex(ref hashMapBuffer, hashMapBuffer[lastItemIndex].HashMapIndex, index);
			SetHashItemHashIndex(ref hashMapBuffer, -1,                                        lastItemIndex);
		}
		else
		{
			RemoveHashItem(ref hashMapBuffer, hashMapBuffer[lastItemIndex].HashMapIndex, lastItemIndex, buffer.Capacity);
			SetHashItemHashIndex(ref hashMapBuffer, -1, lastItemIndex);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void SetHashItemIndex(ref DynamicBuffer<BufferHashSetElementData> hashMapBuffer, int index, int bufferItemIndex)
	{
		var hashMapBufferIndex = hashMapBuffer[bufferItemIndex].HashMapIndex;

		BufferHashSetElementData hashSetItem = hashMapBuffer[hashMapBufferIndex];

		for (int i = 0; i < hashMapBuffer.Capacity; i++)
		{
			if (hashMapBuffer[hashMapBufferIndex].Index == bufferItemIndex)
			{
				break;
			}

			hashMapBufferIndex = hashMapBuffer[hashMapBufferIndex].NextItem;
			hashSetItem        = hashMapBuffer[hashMapBufferIndex];
		}

		hashSetItem.Index = index;

		if (index < 0) hashSetItem.Empty = true;

		hashMapBuffer[hashMapBufferIndex] = hashSetItem;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void SetHashItemHashIndex(ref DynamicBuffer<BufferHashSetElementData> hashMapBuffer, int index, int bufferItemIndex)
	{
		BufferHashSetElementData hashSetItem = hashMapBuffer[bufferItemIndex];

		hashSetItem.HashMapIndex = index;

		hashMapBuffer[bufferItemIndex] = hashSetItem;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void RemoveHashItem(ref DynamicBuffer<BufferHashSetElementData> hashMapBuffer, int hashMapIndex, int bufferIndex,
		int                                                                        bufferCapacity)
	{
		//get buffer element hash index
		var currentItemHashIndex = GetHashItemIndex(ref hashMapBuffer, hashMapIndex, bufferIndex);

		//get chain last item
		var chainLastHashItemIndex = GetChainLastHashItemIndex(ref hashMapBuffer, hashMapIndex);

		// if root item
		if (currentItemHashIndex < bufferCapacity)
		{
			// single item chain
			if (hashMapBuffer[hashMapIndex].NextItem < 0)
			{
				hashMapBuffer[hashMapIndex] = new BufferHashSetElementData(-1, true, -1, -1, hashMapBuffer[hashMapIndex].HashMapIndex);
			}
			else
			{
				RemoveHashItemInChain(ref hashMapBuffer, currentItemHashIndex, chainLastHashItemIndex);
				MoveTailToAndCut(ref hashMapBuffer, chainLastHashItemIndex);
			}
		}
		//if not root item
		else
		{
			RemoveHashItemInChain(ref hashMapBuffer, currentItemHashIndex, chainLastHashItemIndex);
			MoveTailToAndCut(ref hashMapBuffer, chainLastHashItemIndex);
		}
	}

	/// <summary>
	/// Remove hash item and rebuild chain (after removal move last item to removed place)
	/// </summary>
	/// <param name="hashMapBuffer"></param>
	/// <param name="hashMapIndex"></param>
	/// <param name="chainLastIndex"></param>
	/// <param name="bufferIndex"></param>
	/// <param name="bufferCapacity"></param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void RemoveHashItemInChain(ref DynamicBuffer<BufferHashSetElementData> hashMapBuffer, int hashMapIndex, int chainLastIndex)
	{
		var hashMapItem = hashMapBuffer[hashMapIndex];

		if (hashMapIndex == chainLastIndex)
		{
			var prevHashMapItem = hashMapBuffer[hashMapItem.PrevItem];
			prevHashMapItem.NextItem            = -1;
			hashMapBuffer[hashMapItem.PrevItem] = prevHashMapItem;
		}
		else
		{
			var lastItem = hashMapBuffer[chainLastIndex];

			if (chainLastIndex == hashMapItem.NextItem)
			{
				lastItem.PrevItem     = hashMapItem.PrevItem;
				lastItem.NextItem     = -1;
				lastItem.HashMapIndex = hashMapItem.HashMapIndex;

				hashMapBuffer[hashMapIndex] = lastItem;
			}
			else
			{
				int prevLastIndex = lastItem.PrevItem;
				var prevLastItem  = hashMapBuffer[prevLastIndex];

				lastItem.PrevItem     = hashMapItem.PrevItem;
				lastItem.NextItem     = hashMapItem.NextItem;
				lastItem.HashMapIndex = hashMapItem.HashMapIndex;

				hashMapBuffer[hashMapIndex] = lastItem;

				prevLastItem.NextItem        = -1;
				hashMapBuffer[prevLastIndex] = prevLastItem;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int GetHashItemIndex(ref DynamicBuffer<BufferHashSetElementData> hashMapBuffer, int hashMapIndex, int bufferIndex)
	{
		for (int i = 0; i < hashMapBuffer.Length; i++)
		{
			if (hashMapBuffer[hashMapIndex].Index == bufferIndex)
			{
				break;
			}

			hashMapIndex = hashMapBuffer[hashMapIndex].NextItem;
		}

		return hashMapIndex;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int GetChainLastHashItemIndex(ref DynamicBuffer<BufferHashSetElementData> hashMapBuffer, int hashMapIndex)
	{
		for (int i = 0; i < hashMapBuffer.Length; i++)
		{
			if (hashMapBuffer[hashMapIndex].NextItem < 0)
			{
				return hashMapIndex;
			}

			hashMapIndex = hashMapBuffer[hashMapIndex].NextItem;
		}
#if UNITY_EDITOR
		Debug.LogError("Chain last hash item index not found");
#endif
		return hashMapIndex;
	}

	/// <summary>
	/// Cut hash map tail (Process situation when last hash item not last in chain)
	/// </summary>
	/// <param name="hashMapBuffer"></param>
	/// <param name="hashMapIndex">move hash item to removed place and cut array</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void MoveTailToAndCut(ref DynamicBuffer<BufferHashSetElementData> hashMapBuffer, int hashMapIndex)
	{
		var lastIndex = hashMapBuffer.Length - 1;

		if (lastIndex == hashMapIndex)
		{
			hashMapBuffer.RemoveAt(lastIndex);
			return;
		}

		var lastItem = hashMapBuffer[lastIndex];

		var prevItem = hashMapBuffer[lastItem.PrevItem];
		prevItem.NextItem                = hashMapIndex;
		hashMapBuffer[lastItem.PrevItem] = prevItem;

		if (lastItem.NextItem >= 0)
		{
			var nextItem = hashMapBuffer[lastItem.NextItem];
			nextItem.PrevItem                = hashMapIndex;
			hashMapBuffer[lastItem.NextItem] = nextItem;
		}

		hashMapBuffer[hashMapIndex] = lastItem;

		hashMapBuffer.RemoveAt(lastIndex);
	}

	public static bool Contains<T>(EntityManager entityManager, Entity entity, T element) where T : struct, IBufferElementData, IEqualityComparer<T>
	{
		DynamicBuffer<T> buffer = entityManager.GetBuffer<T>(entity);

		DynamicBuffer<BufferHashSetElementData> hashMapBuffer = entityManager.GetBuffer<BufferHashSetElementData>(entity);

		return Contains(buffer, hashMapBuffer, element);
	}

	public static int Length<T>(EntityManager entityManager, Entity entity) where T : struct, IBufferElementData, IEqualityComparer<T>
	{
		DynamicBuffer<T> buffer = entityManager.GetBuffer<T>(entity);

		return buffer.Length;
	}

	public static bool Contains<T>(DynamicBuffer<T> buffer, DynamicBuffer<BufferHashSetElementData> hashMapBuffer, T element)
		where T : struct, IBufferElementData, IEqualityComparer<T>
	{
		return FindBufferItemIndex(buffer, hashMapBuffer, element) >= 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int FindBufferItemIndex<T>(DynamicBuffer<T> buffer, DynamicBuffer<BufferHashSetElementData> hashMapBuffer, T element)
		where T : struct, IBufferElementData, IEqualityComparer<T>
	{
		int index = GetIndex(element, buffer.Capacity);

		if (hashMapBuffer[index].Empty) return -1;

		int itemBufferIndex = hashMapBuffer[index].Index;

		if (buffer[itemBufferIndex].Equals(element, buffer[itemBufferIndex]))
		{
			return itemBufferIndex;
		}

		int nextIndex = hashMapBuffer[index].NextItem;
		for (int i = 0; i < buffer.Capacity; i++)
		{
			if (nextIndex < 0)
			{
				return -1;
			}

			var nextItem = buffer[hashMapBuffer[nextIndex].Index];

			if (nextItem.Equals(element, nextItem))
			{
				return hashMapBuffer[nextIndex].Index;
			}

			nextIndex = hashMapBuffer[nextIndex].NextItem;
		}

		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int GetIndex<T>(T key, int size) where T : struct, IBufferElementData, IEqualityComparer<T>
	{
		int index = key.GetHashCode() % size;
		return math.abs(index);
	}

#if UNITY_EDITOR

	public static void TestCorrectness<T>(EntityManager entityManager, Entity entity) where T : struct, IBufferElementData, IEqualityComparer<T>
	{
		DynamicBuffer<T> buffer = entityManager.GetBuffer<T>(entity);

		DynamicBuffer<BufferHashSetElementData> hashMapBuffer = entityManager.GetBuffer<BufferHashSetElementData>(entity);

		foreach (var bufferHashMapElement in hashMapBuffer)
		{
			if (bufferHashMapElement.Index >= buffer.Length)
			{
				Debug.LogError("HashMap has out of range buffer item");
			}
		}

		for (int i = 0; i < buffer.Length; i++)
		{
			int index = GetIndex(buffer[i], buffer.Capacity);

			Debug.Log("i::" + i + "::hash::" + index);

			if (hashMapBuffer[i].HashMapIndex != index)
			{
				Debug.Log("Error::" + index + "::" + i);
			}
		}

		for (int i = 0; i < buffer.Length; i++)
		{
			int index = GetIndex(buffer[i], buffer.Capacity);

			// check hash set indexes
			if (hashMapBuffer[index].Index != i)
			{
				if (!IsChainHasIndex(hashMapBuffer, index, i))
				{
					Debug.LogError("Buffer item & hashmap index mismatch, hashIndex:" + index + "::itemIndex::" + i);
				}
			}

			if (hashMapBuffer[i].HashMapIndex != index)
			{
				Debug.LogError("Incorrect buffer index to hashIndex value");
			}
		}

		for (var i = 0; i < hashMapBuffer.Length; i++)
		{
			for (var n = 0; n < hashMapBuffer.Length; n++)
			{
				if (i != n && hashMapBuffer[i].Index == hashMapBuffer[n].Index && hashMapBuffer[i].Index >= 0)
				{
					Debug.LogError("Hash set contain same index in two cells::" + i + "::itemIndex::" + hashMapBuffer[i].Index);
				}
			}
		}

		for (var i = 0; i < hashMapBuffer.Length; i++)
		{
			if (hashMapBuffer[i].Empty && hashMapBuffer[i].Index >= 0)
			{
				Debug.LogError("Empty item with index");
			}

			if (!hashMapBuffer[i].Empty && hashMapBuffer[i].Index < 0)
			{
				Debug.LogError("Not empty item without index, hash set index::" + i + "::item index::" + hashMapBuffer[i].Index);
			}

			if (hashMapBuffer[i].NextItem >= 0 && hashMapBuffer[i].NextItem >= hashMapBuffer.Length)
			{
				Debug.LogError("Hash set buffer next item out of range, hash set index::" + i            + "::nextItem::" +
				               hashMapBuffer[i].NextItem                                  + "::length::" + hashMapBuffer.Length);
			}

			if (hashMapBuffer[i].PrevItem >= 0 && hashMapBuffer[i].PrevItem >= hashMapBuffer.Length)
			{
				Debug.LogError("Hash set buffer prev item out of range, hash set index::" + i + "::prevItem::" + hashMapBuffer[i].PrevItem +
				               "::length::"                                               + hashMapBuffer.Length);
			}
		}
	}

	private static bool IsChainHasIndex(DynamicBuffer<BufferHashSetElementData> hashMapBuffer, int hashIndex, int itemIndex)
	{
		for (int i = 0; i < hashMapBuffer.Length; i++)
		{
			if (hashIndex                      < 0) return false;
			if (hashMapBuffer[hashIndex].Index == itemIndex) return true;
			hashIndex = hashMapBuffer[hashIndex].NextItem;
		}

		return false;
	}
#endif
}