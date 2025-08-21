using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class FormationGenerator
{
    public List<float3> GeneratePhalanxFormation(int unitCount, int unitsPerPhalanx = 256,
        float unitSpacing = 0.25f, float phalanxSpacing = 1f)
    {
        var positions = new List<float3>();
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

    private List<float3> GenerateSinglePhalanx(int unitCount, float unitSpacing, float startY)
    {
        var positions = new List<float3>();
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(unitCount));

        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                if (positions.Count >= unitCount) break;

                float x = col * unitSpacing * -1; // Negative for left movement
                float y = startY + row * unitSpacing;
                positions.Add(new float3(x, y, 0));
            }
        }

        return positions;
    }
}