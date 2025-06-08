using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;

public struct EntityGroup : IComponentData
{
    public int groupId;            // Unique ID for the group
    public int parentGroupId;      // Reference to the parent group (if applicable)
    public int groupSize;          // Number of entities in the group
    public Entity parent;
}

public struct GroupComponent : IComponentData
{
    public EntityGroup group;  // The group to which the entity belongs
}

public struct IsInBattleComponent : IComponentData
{
    public bool isInBattle;  // Whether the entity is currently in battle
}

public struct RowComponent : IComponentData
{
    public int rowIndex;  // Row index within the group
}

//[UpdateInGroup(typeof(SimulationSystemGroup))]
//[UpdateBefore(typeof(CombatSystem))]
//[BurstCompile]
//public class GroupComponentSystem : SystemBase
//{
//    protected override void OnCreate()
//    {
//        // Initialize any needed resources or configurations for the system
//    }

//    protected override void OnUpdate()
//    {
//        // Update entities to assign them to appropriate groups based on their GroupComponent
//        Entities.ForEach((ref GroupComponent group) =>
//        {
//            // Example logic: dynamically assign entities to groups
//            if (group.group.parent == Entity.Null)
//            {
//                // Logic to create or assign new groups dynamically
//                // For example, assign an entity to a default parent group or a new group
//            }

//        }).ScheduleParallel();

//        // Handle updating the battle state for entities
//        Entities.WithAll<IsInBattleComponent>().ForEach((ref IsInBattleComponent battleStatus, in GroupComponent group) =>
//        {
//            // Set or update the battle state for units in this group
//            if (group.group.parent != Entity.Null)
//            {
//                // Example logic: propagate battle state from parent to child group
//                var parentBattleStatus = EntityManager.GetComponentData<IsInBattleComponent>(group.group.parent);
//                battleStatus.isInBattle = parentBattleStatus.isInBattle;
//            }

//        }).ScheduleParallel();

//        // Handle entity grouping logic, for example by proximity or other conditions
//        Entities.ForEach((ref GroupComponent group) =>
//        {
//            // Group management: dynamically assign or update group membership
//            // E.g., groups might be redefined based on proximity or specific behaviors
//        }).ScheduleParallel();
//    }
//}