using TMPro;
using Unity.Entities;
using UnityEngine;

public class Example : MonoBehaviour
{
	[SerializeField] private bool _jobSystemTest = true;

	[SerializeField] private TextMeshProUGUI _valueLabel;
	[SerializeField] private TextMeshProUGUI _checkFirstLabel;
	[SerializeField] private TextMeshProUGUI _checkSecondLabel;
	[SerializeField] private TextMeshProUGUI _removeLabel;

	private EntityManager _entityManager;
	private Entity        _entity;

	private void Start()
	{
		_entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

		EntityArchetype archetype = _entityManager.CreateArchetype(typeof(ExampleComponent));

		_entity = _entityManager.CreateEntity(archetype);

		if (!_jobSystemTest) _entityManager.AddComponent<ComponentSystemTag>(_entity);

		DynamicBufferHashSet.AddDynamicBufferHashSet<TestHashSetBufferElement>(_entityManager, _entity, 32);
	}

	public void ProcessHashSet()
	{
		_entityManager.AddComponent<ProcessHashSetBufferComponent>(_entity);
	}

	private void Update()
	{
		var exampleComponentData = _entityManager.GetComponentData<ExampleComponent>(_entity);

		_valueLabel.text       = "Time  "   + exampleComponentData.Time.ToString("00.0");
		_checkFirstLabel.text  = "check 1 -" + exampleComponentData.Check1;
		_checkSecondLabel.text = "check 2 -" + exampleComponentData.Check2;
		_removeLabel.text      = "remove -"  + exampleComponentData.Remove;
	}
}