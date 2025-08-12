
//using UnityEngine;

//public class Pipeline_layout_script : MonoBehaviour
//{
//    public string parentName = "SpawnedObjects";
//    public Material lineMaterial;
//    public float lineWidth = 0.05f;

//    private Transform[] pipes;
//    private Vector3[] rears;
//    private Vector3[] fronts;

//    void Update()
//    {
//        FindPipes();

//        if (pipes != null && pipes.Length >= 2)
//        {
//            CalculatePipeEnds();
//            DrawAllLines();
//        }
//    }

//    void FindPipes()
//    {
//        Transform parent = GameObject.Find(parentName)?.transform;

//        if (parent != null && parent.childCount >= 2)
//        {
//            pipes = new Transform[parent.childCount];
//            for (int i = 0; i < parent.childCount; i++)
//                pipes[i] = parent.GetChild(i);

//            rears = new Vector3[pipes.Length];
//            fronts = new Vector3[pipes.Length];
//        }
//        else
//        {
//            pipes = null;
//        }
//    }

//    void CalculatePipeEnds()
//    {
//        for (int i = 0; i < pipes.Length; i++)
//        {
//            MeshRenderer mr = pipes[i].GetComponentInChildren<MeshRenderer>();
//            if (mr != null)
//            {
//                Bounds b = mr.bounds;
//                Vector3 dir = pipes[i].forward.normalized;

//                rears[i] = b.center - dir * Vector3.Dot(b.extents, dir);
//                fronts[i] = b.center + dir * Vector3.Dot(b.extents, dir);
//            }
//            else
//            {
//                rears[i] = pipes[i].position;
//                fronts[i] = pipes[i].position;
//            }
//        }
//    }

//    void DrawAllLines()
//    {
//        // Remove old lines
//        foreach (Transform child in transform)
//            Destroy(child.gameObject);

//        // Create one LineRenderer per connection
//        for (int i = 0; i < pipes.Length - 1; i++)
//        {
//            GameObject lineObj = new GameObject($"Line_{i}_{i + 1}");
//            lineObj.transform.parent = transform;

//            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
//            lr.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
//            lr.startWidth = lineWidth;
//            lr.endWidth = lineWidth;
//            lr.startColor = Color.green;
//            lr.endColor = Color.green;
//            lr.positionCount = 2;
//            lr.useWorldSpace = true;

//            lr.SetPosition(0, rears[i]);       // rear of current pipe
//            lr.SetPosition(1, fronts[i + 1]);  // front of next pipe
//        }
//    }
//}

using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class Pipeline_layout_script : MonoBehaviour
{
    public string parentName = "SpawnedObjects";
    public Material extrusionMaterial;
  
    public int sides = 12;           // Number of segments around the circle
    public float radius = 0.25f;     // Pipe radius

    private Transform[] pipes;
    private Vector3[] rears;
    private Vector3[] fronts;

    private void Start()
    {
        StartCoroutine(BuildPipelinesCoroutine());
    }

    IEnumerator BuildPipelinesCoroutine()
    {
        while (true)
        {
            FindPipes();

            if (pipes != null && pipes.Length >= 2)
            {
                CalculatePipeEnds();
                DrawAllSplines();
            }

            yield return new WaitForSeconds(15f);
        }
    }

    void FindPipes()
    {
        Transform parent = GameObject.Find(parentName)?.transform;

        if (parent != null && parent.childCount >= 2)
        {
            pipes = new Transform[parent.childCount];
            for (int i = 0; i < parent.childCount; i++)
                pipes[i] = parent.GetChild(i);

            rears = new Vector3[pipes.Length];
            fronts = new Vector3[pipes.Length];
        }
        else
        {
            pipes = null;
        }
    }

    void CalculatePipeEnds()
    {
        for (int i = 0; i < pipes.Length; i++)
        {
            Transform pipeChild = pipes[i].Find("Pipe");

            if (pipeChild != null)
            {
                Transform frontEnd = pipeChild.Find("FrontEnd");
                Transform rearEnd = pipeChild.Find("RearEnd");

                rears[i] = rearEnd ? rearEnd.position : pipes[i].position;
                fronts[i] = frontEnd ? frontEnd.position : pipes[i].position;
            }
            else
            {
                rears[i] = pipes[i].position;
                fronts[i] = pipes[i].position;
            }
        }
    }

    void DrawAllSplines()
    {
        // Clear old spline objects
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        for (int i = 0; i < pipes.Length - 1; i++)
        {
            GameObject splineObj = new GameObject($"Spline_{i}_{i + 1}");
            splineObj.transform.parent = transform;

            SplineContainer splineContainer = splineObj.AddComponent<SplineContainer>();

            Spline spline = new Spline();

            BezierKnot startKnot = new BezierKnot((float3)rears[i]);
            BezierKnot endKnot = new BezierKnot((float3)fronts[i + 1]);

            Vector3 tangentOffset = (fronts[i + 1] - rears[i]) * 0.25f;
            startKnot.TangentOut = (float3)tangentOffset;
            endKnot.TangentIn = (float3)(-tangentOffset);

            spline.Add(startKnot);
            spline.Add(endKnot);

            splineContainer.Spline = spline;

            // Mesh Filter & Renderer
            MeshFilter mf = splineObj.AddComponent<MeshFilter>();
            
            MeshRenderer mr = splineObj.AddComponent<MeshRenderer>();
            mr.material = extrusionMaterial;

            // Extrusion
            var extrude = splineObj.AddComponent<SplineExtrude>();
            extrude.Container = splineContainer;
            //extrude.Resolution = 8; // Curve smoothness
            extrude.Sides = sides;
            extrude.Radius = radius;
            
            //extrude.Output = mf; // Mesh output

            extrude.Rebuild();
        }
    }
}
