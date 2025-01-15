using Unity.Entities;
using UnityEngine;

public struct AnimationComponent : IComponentData
{
    public int currentFrame;
    public int frameCount;
    public float frameTimer;
    public float frameTimerMax;

    public Vector4 uv;
    public Matrix4x4 matrix;

    public EntitySpawner.UnitType unitType;
    public EntitySpawner.Direction direction;
    public EntitySpawner.AnimationType animationType;

    public EntitySpawner.Direction prevDirection;
    public EntitySpawner.AnimationType prevAnimationType;
}