using System.Collections.Generic;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[System.Serializable]
public class PipeMaterialEntry
{
    public string materialName;
    public Material material;
    [Header("Pipe Settings")]
    public MaterialType materialType = MaterialType.Steel;
    public float diameter = 0.1f; // meters (set properly in inspector)
    [Tooltip("Labor cost in INR per meter")]
    public float laborCostPerMeter = 100f; // INR per meter (tunable)
    [Header("Pricing (INR) - base cost PER 1.0 m diameter (INR/m)")]
    [Tooltip("Base material price per meter for 1.0 m diameter. Actual = base * diameter(m)")]
    public float steelBasePerMeterPer1mDia = 500f;
    public float pvcBasePerMeterPer1mDia = 200f;
    public float copperBasePerMeterPer1mDia = 1500f;

    [Header("Bend Settings")]
    [Range(0f, 1f)]
    public float bendComplexity = 0.2f; // multiplier (0..1)
    [Tooltip("How much INR a full 180° bend costs at complexity=1")]
    public float bendCostMultiplier = 200f; // INR for a full sharp bend * complexity

}

public class Types_of_PipeMaterials : MonoBehaviour
{
    [Header("Pipe Materials Setup")]
    public List<PipeMaterialEntry> pipeMaterials = new List<PipeMaterialEntry>();

    private Dictionary<string, Material> materialDict = new Dictionary<string, Material>();

    [Header("Pipe Renderer (Target)")]
    public MeshRenderer pipeRenderer; 

    void Awake()
    {
        // Fill dictionary
        foreach (var entry in pipeMaterials)
        {
            if (!string.IsNullOrEmpty(entry.materialName) && entry.material != null)
            {
                if (!materialDict.ContainsKey(entry.materialName))
                {
                    materialDict.Add(entry.materialName, entry.material);
                }
            }
        }
    }

    public void ChangePipeMaterial(string nameInput)
    {
        if (pipeRenderer == null)
        {
            Debug.LogError("Pipe Renderer not assigned!");
            return;
        }

        if (materialDict.TryGetValue(nameInput, out Material mat))
        {
            pipeRenderer.material = mat;
            if (nameInput == "Steel")
            {
                GetComponent<PipeCostCalculator>().selectedMaterialType = MaterialType.Steel;
            }
            if (nameInput == "PVC")
            {
                GetComponent<PipeCostCalculator>().selectedMaterialType = MaterialType.PVC;       
            }
            if (nameInput == "Copper")
            {
                GetComponent<PipeCostCalculator>().selectedMaterialType = MaterialType.Copper;
            }
            Debug.Log($"Pipe material successfully changed to {nameInput}");

        }
    
        else
        {
            Debug.LogWarning($"No material found for key: {nameInput}");
        }
    }
}



public enum MaterialType
{
    Steel,
    PVC,
    Copper
}
