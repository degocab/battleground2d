using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// This class is used to preview the formation of units in the game. It is attached to a GameObject that represents the formation and is responsible for displaying the formation in the scene view.
/// </summary>
public class FormationPreview : MonoBehaviour
{
    private const float Radius = 0.0625f;
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<int> triangles = new List<int>();

    private Mesh mesh;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        mesh = new Mesh() { name ="Formation Preview"};
        mesh.MarkDynamic();
        GetComponent<MeshFilter>().mesh = mesh; 
        meshRenderer = GetComponent<MeshRenderer>();
        Hide();
    }

    public void Hide() => meshRenderer.enabled = false;

    public void ShowSlots(NativeArray<float2> slots)
    {
        vertices.Clear();
        triangles.Clear();
        const int segments = 12;
        const float outerRadius = 0.0625f;
        const float innerRadius = 0.05f;

        for (int i = 0; i < slots.Length; i++)
        {
            float2 slot = slots[i];
            int first = vertices.Count;
            var center = new Vector3(slot.x, slot.y, 0f);

            for (int j = 0; j < segments; j++)
            {
                float angle = j * (2f * Mathf.PI / segments);
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

                vertices.Add(center + direction * outerRadius);
                vertices.Add(center + direction * innerRadius);
            }

            for (int j = 0; j < segments; j++)
            {
                int outer = first + j * 2;
                int nextOuter = first + ((j + 1) % segments) * 2;

                triangles.Add(outer);
                triangles.Add(outer + 1);
                triangles.Add(nextOuter);

                triangles.Add(nextOuter);
                triangles.Add(outer + 1);
                triangles.Add(nextOuter + 1);
            }
        }
        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        meshRenderer.enabled = slots.Length > 0;

    }
    private void OnDestroy() => Destroy(mesh);  


}
