using Unity.Entities;

public struct ExampleComponent : IComponentData
{
	public float Time;
	public bool  Remove;
	public bool  Check1;
	public bool  Check2;
}