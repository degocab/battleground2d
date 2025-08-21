using Unity.Entities;
using UnityEngine;

public struct AnimationComponent : IComponentData
{
    public int CurrentFrame;
    public int FrameCount;
    public float FrameTimer;
    public float FrameTimerMax;

    public int animationHeightOffset;
    public int animationWidthOffset;

    public Vector4 uv;
    public Matrix4x4 matrix;

    public EntitySpawner.UnitType UnitType;
    public EntitySpawner.Direction Direction;
    public EntitySpawner.AnimationType AnimationType;

    public EntitySpawner.Direction prevDirection;
    public EntitySpawner.AnimationType prevAnimationType;

    /// <summary>
    /// Bool to set for animation that needs to continue after value resets
    /// Ex: spacebarPressedThisFrame = Input.GetKeyDown(KeyCode.Space);
    /// This resets on each frame, so it could finish the animation early.
    /// </summary>
    public bool finishAnimation;
    public bool isFrozen;
}