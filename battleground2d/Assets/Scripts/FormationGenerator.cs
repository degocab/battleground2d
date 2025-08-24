using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class FormationGenerator
{
    public enum FormationType
    {
        Phalanx,
        SinglePhalanx,
        Horde
    }
    public List<float2> GeneratePhalanxFormation(int unitCount, int unitsPerPhalanx = 256,
        float unitSpacing = 0.25f, float phalanxSpacing = 1f)
    {
        var positions = new List<float2>();
        int numFullPhalanxes = unitCount / unitsPerPhalanx;
        int remainingUnits = unitCount % unitsPerPhalanx;

        float currentY = 0f;

        for (int i = 0; i < numFullPhalanxes; i++)
        {
            positions.AddRange(GenerateSinglePhalanx(unitsPerPhalanx, unitSpacing, currentY));
            currentY += Mathf.CeilToInt(Mathf.Sqrt(unitsPerPhalanx)) * unitSpacing + phalanxSpacing;
        }

        if (remainingUnits > 0)
        {
            positions.AddRange(GenerateSinglePhalanx(remainingUnits, unitSpacing, currentY));
        }

        return positions;
    }

    private List<float2> GenerateSinglePhalanx(int unitCount, float unitSpacing, float startY)
    {
        var positions = new List<float2>();
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(unitCount));

        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                if (positions.Count >= unitCount) break;

                float x = col * unitSpacing * -1; // Negative for left movement
                float y = startY + row * unitSpacing;
                positions.Add(new float2(x, y));
            }
        }

        return positions;
    }

    public List<float2> GenerateHordeFormation(int unitCount, float frontWidth = 20f,
        float depthVariation = 1f, float spacingNoise = 0.3f, uint seed = 12345, float2? position = null)
    {
        Debug.Log("horde mode");
        var positions = new List<float2>();
        var random = new Unity.Mathematics.Random(seed);

        // Max 18 units in X direction (depth)
        int maxUnitsPerRow = 18;

        // Calculate number of rows needed for Y direction
        int rows = Mathf.CeilToInt((float)unitCount / maxUnitsPerRow);

        // Calculate spacing based on unit diameter (0.25f)
        float unitSpacing = 0.25f * 1.2f; // Add 20% extra space between units

        for (int i = 0; i < unitCount; i++)
        {
            // Calculate row (Y axis) and column (X axis) positions
            int row = i / maxUnitsPerRow; // Y position (vertical)
            int col = i % maxUnitsPerRow; // X position (horizontal/depth)

            // Add randomness to spacing
            float xOffset = random.NextFloat(-spacingNoise, spacingNoise);
            float yOffset = random.NextFloat(-spacingNoise, spacingNoise);

            // Calculate position - X is depth, Y is front line
            float x = col * unitSpacing + random.NextFloat(-depthVariation, depthVariation);
            float y = row * unitSpacing;

            // Center the formation
            x -= (maxUnitsPerRow * unitSpacing) / 2f;

            // Apply position offset if provided
            float2 finalPosition = new float2(x + xOffset, y + yOffset);
            if (position != null)
            {
                finalPosition += position.Value;
            }

            positions.Add(finalPosition);
        }

        Debug.Log($"horde mode: {unitCount} units, {maxUnitsPerRow} wide, {rows} deep");
        Debug.Log("horde mode position count " + positions.Count);
        return positions;
    }
}