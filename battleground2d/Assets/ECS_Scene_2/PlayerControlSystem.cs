using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlayerControlSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");
        var time = Time.DeltaTime;
        Entities.WithAll<CommanderComponent>().ForEach((ref PositionComponent position, in PlayerInputComponent input) => 
        {
            position.value.x += moveX * time * 5f;
            position.value.y += moveY * time * 5f;

        }).Schedule();
    }

}
