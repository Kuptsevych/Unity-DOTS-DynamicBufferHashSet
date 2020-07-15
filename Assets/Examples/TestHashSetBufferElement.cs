using System.Collections.Generic;
using Unity.Entities;

public struct TestHashSetBufferElement : IBufferElementData, IEqualityComparer<TestHashSetBufferElement>
{
	public int Value;

	public bool Equals(TestHashSetBufferElement x, TestHashSetBufferElement y)
	{
		return x.Value == y.Value;
	}

	public int GetHashCode(TestHashSetBufferElement component)
	{
		unchecked
		{
			int hash = 17;
			hash = hash * 31 + component.Value.GetHashCode();
			return hash;
		}
	}
}