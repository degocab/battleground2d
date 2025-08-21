using Unity.Entities;

public enum GameState { Loading, Playing, Paused, GameOver }

public struct GameStateComponent : IComponentData
{
    public GameState CurrentState;
    public float LoadingTimer;
    public float LoadingDuration;
}