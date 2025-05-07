using Unity.Entities;
using UnityEngine;

public struct AnimationComponent : IComponentData
{
    public int currentFrame;
    public int frameCount;
    public float frameTimer;
    public float frameTimerMax;

    public int animationHeightOffset;
    public int animationWidthOffset;

    public Vector4 uv;
    public Matrix4x4 matrix;

    public EntitySpawner.UnitType unitType;
    public EntitySpawner.Direction direction;
    public EntitySpawner.AnimationType animationType;

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