using AuroraSeeker.Pipelines4;
using UnityEngine;
using TMPro;
using System;

public class PipeCostCalculator : MonoBehaviour
{
    [Header("Node / Units")]
    [Tooltip("If your HorizontalMultiPipe.Nodes are local-space (relative to pipe.transform), set true.")]
    public bool nodesAreLocal = false;
    [Tooltip("Ignore tiny segments below this length (meters)")]
    public float minSegmentLength = 0.001f;

    [Header("Spline Reference")]
    public HorizontalMultiPipe pipe; // reference
    public TMP_Text CostDisplay;

    [Header("Material Selection")]
    public MaterialType selectedMaterialType = MaterialType.Steel;

    [HideInInspector]
    public PipeMaterialEntry a; // currently selected material

    [Header("Results (Read Only)")]
    public float totalLength = 0f; // meters
    public float totalCost = 0f;   // INR

    void Update()
    {
        if (pipe == null || pipe.Nodes == null || pipe.Nodes.Length < 2)
        {
            totalLength = 0f;
            totalCost = 0f;
            UpdateCostDisplay();
            return;
        }

        // Get material list from Types_of_PipeMaterials
        var parameters = GetComponent<Types_of_PipeMaterials>();
        if (parameters != null && parameters.pipeMaterials.Count > 0)
        {
            a = parameters.pipeMaterials.Find(x => x.materialType == selectedMaterialType);

            if (a == null)
            {
                Debug.LogWarning($"Pipe material of type {selectedMaterialType} not found!");
                return;
            }
        }
        else
        {
            Debug.LogWarning("Types_of_PipeMaterials component missing or empty!");
            return;
        }

        // Recalculate costs
        Recalculate(a);

        // Update display
        UpdateCostDisplay();
    }

    void Recalculate(PipeMaterialEntry material)
    {
        totalLength = 0f;

        // Compute total length (ignore tiny segments)
        Vector3 prev = GetNodeWorldPosition(0);
        for (int i = 1; i < pipe.Nodes.Length; i++)
        {
            Vector3 cur = GetNodeWorldPosition(i);
            float segLen = Vector3.Distance(prev, cur);
            if (segLen >= minSegmentLength)
                totalLength += segLen/10;
            prev = cur;
        }

        // Compute costs
        float materialPerMeter = GetMaterialCostPerMeter(material);
        float materialTotal = materialPerMeter * totalLength;
        float labor = GetLaborCost(material);
        float bends = GetBendCost(material);

        totalCost = materialTotal + labor + bends;
    }

    float GetMaterialCostPerMeter(PipeMaterialEntry material)
    {
        float basePerMeterPer1m = 0f;
        switch (material.materialType)
        {
            case MaterialType.Steel: basePerMeterPer1m = material.steelBasePerMeterPer1mDia; break;
            case MaterialType.PVC: basePerMeterPer1m = material.pvcBasePerMeterPer1mDia; break;
            case MaterialType.Copper: basePerMeterPer1m = material.copperBasePerMeterPer1mDia; break;
        }
        // Scale linearly by diameter
        return basePerMeterPer1m * material.diameter;
    }

    float GetLaborCost(PipeMaterialEntry material)
    {
        return material.laborCostPerMeter * totalLength;
    }

    float GetBendCost(PipeMaterialEntry material)
    {
        float bendCost = 0f;

        for (int i = 1; i < pipe.Nodes.Length - 1; i++)
        {
            Vector3 prev = GetNodeWorldPosition(i - 1);
            Vector3 cur = GetNodeWorldPosition(i);
            Vector3 next = GetNodeWorldPosition(i + 1);

            Vector3 aVec = cur - prev;
            Vector3 bVec = next - cur;

            if (aVec.magnitude < minSegmentLength || bVec.magnitude < minSegmentLength)
                continue;

            float angle = Vector3.Angle(aVec, bVec);
            float normalized = angle / 180f;

            bendCost += normalized * material.bendComplexity * material.bendCostMultiplier;
        }

        return bendCost;
    }

    Vector3 GetNodeWorldPosition(int index)
    {
        var n = pipe.Nodes[index];
        Vector3 v = new Vector3(n.x, n.y, n.z);
        return nodesAreLocal ? pipe.transform.TransformPoint(v) : v;
    }

    void UpdateCostDisplay()
    {
        if (CostDisplay == null) return;

        CostDisplay.text =
            $"Length: {totalLength:F3} m\n" +
            $"Total: Rs. {totalCost:F2}\n" +
            $"Material: Rs. {(totalCost - GetLaborCost(a) - GetBendCost(a)):F2}\n" +
            $"Labor: Rs. {GetLaborCost(a):F2}\n" +
            $"Bends: Rs. {GetBendCost(a):F2}";
    }

    // Optional: UI buttons to select material
    public void SetMaterialSteel() => selectedMaterialType = MaterialType.Steel;
    public void SetMaterialPVC() => selectedMaterialType = MaterialType.PVC;
    public void SetMaterialCopper() => selectedMaterialType = MaterialType.Copper;
}
