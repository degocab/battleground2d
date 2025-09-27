using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;
using UnityEngine.UI;
using System.Text;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class LoadingSystem : SystemBase
{
    private BeginInitializationEntityCommandBufferSystem _ecbSystem;
    private float timeSinceLastLog = 0f;
    private const float LogIntervalSeconds = 10f; // 5 minutes
    private string logPath;

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
            //Debug.Log($"Loading: {gameState.LoadingTimer:F1}/{gameState.LoadingDuration:F1}s");

            if (gameState.LoadingTimer >= gameState.LoadingDuration)
            {
                // Switch to playing state
                gameState.CurrentState = GameState.Playing;
                SetSingleton(gameState);
                Debug.Log("Game started!");
            }
        }


        //timeSinceLastLog += Time.DeltaTime;
        //if (timeSinceLastLog >= LogIntervalSeconds)
        //{
        //    timeSinceLastLog = 0f;
        //    var sb = new StringBuilder();
        //    sb.AppendLine($"=== ECS SYSTEM UPDATE ORDER [{System.DateTime.Now}] ===");

        //    // Traverse main system groups
        //    LogGroup(World.GetOrCreateSystem<InitializationSystemGroup>(), sb, 0);
        //    LogGroup(World.GetOrCreateSystem<SimulationSystemGroup>(), sb, 0);
        //    LogGroup(World.GetOrCreateSystem<PresentationSystemGroup>(), sb, 0);

        //    string output = sb.ToString();
        //    Debug.Log(output);

        //    string logOutput = sb.ToString();

        //    Debug.Log(logOutput);
        //}
    }
    private void LogGroup(ComponentSystemGroup group, StringBuilder sb, int indentLevel)
    {
        string indent = new string(' ', indentLevel * 2);
        sb.AppendLine($"{indent}{group.GetType().Name}");

        foreach (var system in group.Systems)
        {
            if (system is ComponentSystemGroup subgroup)
            {
                LogGroup(subgroup, sb, indentLevel + 1);
            }
            else
            {
                sb.AppendLine($"{indent}  - {system.GetType().Name}");
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

        // Set anchors to center but use position offset
        loadingText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        loadingText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        loadingText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        loadingText.rectTransform.anchoredPosition = new Vector2(0, 0); // Center of screen
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