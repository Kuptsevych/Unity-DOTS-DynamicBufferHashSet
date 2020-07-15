using Unity.Entities;

public struct BufferHashSetElementData : IBufferElementData
{
	public int  PrevItem;
	public int  NextItem;
	public int  Index;
	public bool Empty;

	public int HashMapIndex;

	//TODO for optimization
	public int LastItemIndex; // set last chain item for optimization

	public BufferHashSetElementData(int index, bool empty, int prevItem = -1, int nextItem = -1, int hashMapIndex = -1, int lastItemIndex = -1)
	{
		Index         = index;
		Empty         = empty;
		PrevItem      = prevItem;
		NextItem      = nextItem;
		HashMapIndex  = hashMapIndex;
		LastItemIndex = lastItemIndex;
	}
}