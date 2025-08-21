using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;
using UnityEngine.UI;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class LoadingSystem : SystemBase
{
    private BeginInitializationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();

        // Create the game state entity
        EntityManager.CreateEntity(typeof(GameStateComponent));
        SetSingleton(new GameStateComponent
        {
            CurrentState = GameState.Loading,
            LoadingDuration = 5.0f // 2 seconds loading time
        });
    }

    protected override void OnUpdate()
    {
        var gameState = GetSingleton<GameStateComponent>();

        if (gameState.CurrentState == GameState.Loading)
        {
            gameState.LoadingTimer += Time.DeltaTime;
            SetSingleton(gameState);

            // Debug log to see loading progress
            Debug.Log($"Loading: {gameState.LoadingTimer:F1}/{gameState.LoadingDuration:F1}s");

            if (gameState.LoadingTimer >= gameState.LoadingDuration)
            {
                // Switch to playing state
                gameState.CurrentState = GameState.Playing;
                SetSingleton(gameState);
                Debug.Log("Game started!");
            }
        }
    }
}


[UpdateInGroup(typeof(PresentationSystemGroup))]
public class LoadingUISystem : ComponentSystem
{
    private GameObject loadingCanvas;
    private Text loadingText;

    protected override void OnCreate()
    {
        // Create UI canvas
        loadingCanvas = new GameObject("LoadingCanvas");
        var canvas = loadingCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        loadingCanvas.AddComponent<CanvasScaler>();
        loadingCanvas.AddComponent<GraphicRaycaster>();

        // Create loading text
        var textObj = new GameObject("LoadingText");
        textObj.transform.SetParent(loadingCanvas.transform);
        loadingText = textObj.AddComponent<Text>();
        loadingText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        loadingText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        loadingText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        loadingText.rectTransform.sizeDelta = new Vector2(400, 100);
        loadingText.alignment = TextAnchor.MiddleCenter;
        loadingText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        loadingText.fontSize = 24;
        loadingText.color = Color.white;

        loadingCanvas.SetActive(false);
    }

    protected override void OnUpdate()
    {
        if (HasSingleton<GameStateComponent>())
        {
            var gameState = GetSingleton<GameStateComponent>();

            if (gameState.CurrentState == GameState.Loading)
            {
                loadingCanvas.SetActive(true);
                loadingText.text = $"Loading... {gameState.LoadingTimer:F1}/{gameState.LoadingDuration:F1}s";
            }
            else
            {
                loadingCanvas.SetActive(false);
            }
        }
    }

    protected override void OnDestroy()
    {
        if (loadingCanvas != null)
            Object.Destroy(loadingCanvas);
    }
}

public static class EntityManagerExtensions
{
    public static bool HasSingleton<T>(this EntityManager entityManager) where T : struct, IComponentData
    {
        return entityManager.CreateEntityQuery(typeof(T)).CalculateEntityCount() > 0;
    }

    public static T GetSingleton<T>(this EntityManager entityManager) where T : struct, IComponentData
    {
        return entityManager.GetComponentData<T>(entityManager.CreateEntityQuery(typeof(T)).GetSingletonEntity());
    }

    public static bool TryGetSingleton<T>(this EntityManager entityManager, out T component) where T : struct, IComponentData
    {
        if (entityManager.HasSingleton<T>())
        {
            component = entityManager.GetSingleton<T>();
            return true;
        }

        component = default;
        return false;
    }
}