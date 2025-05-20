using Unity.Entities;
using UnityEngine;

//public class DisableUnusedSystem : SystemBase
//{
//    protected override void OnCreate()
//    {
//        base.OnCreate();
//        // Disable all systems by default (except the ones we need)
//        DisableUnusedSystems();
//    }

//    private void DisableUnusedSystems()
//    {
//        var world = World.DefaultGameObjectInjectionWorld;

//        // List of systems to disable (example)
//        var systemsToDisable = new[]
//        {
//            typeof(AnimationSystem),
//            typeof(CombatSystem),
//            typeof(DeathSystem),
//            typeof(GridSystem),
//            typeof(MovementSystem),
//            typeof(RenderSystem),
//            // Add any system you want to disable here
//        };

//        // Disable specified systems
//        foreach (var systemType in systemsToDisable)
//        {
//            var system = world.GetExistingSystem(systemType);
//            if (system != null)
//                system.Enabled = false;
//        }
//    }

//    protected override void OnUpdate() { }
//}

public class DisableUnusedSystem : MonoBehaviour
{
    public bool disableSimulationGroup = true;

    void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;

        //if (disableSimulationGroup)
        //{
        //    var simGroup = world.GetExistingSystem<SimulationSystemGroup>();
        //    if (simGroup != null)
        //    {
        //        simGroup.Enabled = false;
        //        Debug.Log("Disabled SimulationSystemGroup for this scene.");
        //    }
        //}


        System.Type[] systemsToDisable = new[]
        {
                    typeof(AnimationSystem),
                    typeof(CombatSystem),
                    typeof(DeathSystem),
                    //typeof(GridSystem),
                    typeof(MovementSystem),
                    typeof(RenderSystem),
                    // Add any system you want to disable here
                };
        // Example: Disable a specific system
        foreach (System.Type item in systemsToDisable)
        {
            var toDisable = world.GetExistingSystem(item);

            if (toDisable != null)
            {
                toDisable.Enabled = false;
                Debug.Log("Disabled CollisionResolutionSystem manually.");
            } 
        }
    }
}