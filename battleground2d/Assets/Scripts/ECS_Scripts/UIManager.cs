using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
public class UIManager : MonoBehaviour
{
    public Slider healthSlider;
    // The EntityManager and required components for the UIManager
    private EntityManager _entityManager;
    private EntityQuery _commanderQuery;

    // Start is called before the first frame update
    void Start()
    {
        // Initialize health slider
        healthSlider.value = 100;

        // Get the EntityManager from the World
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // Create a query for entities with the CommanderComponent
        _commanderQuery = _entityManager.CreateEntityQuery(typeof(CommanderComponent), typeof(HealthComponent));
    }

    // Update is called once per frame
    void Update()
    {
        // Perform the entity query and update the UI
        var entities = _commanderQuery.ToEntityArray(Unity.Collections.Allocator.TempJob);

        foreach (var entity in entities)
        {
            var health = _entityManager.GetComponentData<HealthComponent>(entity);

            if (healthSlider != null)
            {
                healthSlider.value = health.Health / health.maxHealth;
            }
        }

        entities.Dispose(); // Don't forget to dispose of the array after use!
    }
}

