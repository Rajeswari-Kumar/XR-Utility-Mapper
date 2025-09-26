//using Unity.Collections;
//using Unity.Mathematics;
//using UnityEditor;
//using UnityEditor.Graphs;
//using UnityEngine;

//namespace AuroraSeeker.Pipelines4
//{
//    [RequireComponent(typeof(MeshFilter))]
//    [RequireComponent(typeof(MeshRenderer))]
//    public class MonoPipe : MonoBehaviour
//    {
//        [SerializeField] public float4[] Nodes;
//        [SerializeField] private float CutMaxAngle = 0.1f;
//        [SerializeField] private float MinimalBendAngle = 0.01f;
//        [SerializeField] private int VerticesPerCut = 7;
//        [SerializeField] private float PipeRadius = 0.1f;

//        private PipeJobsDispatcher _dispatcher;
//        private MeshFilter _meshFilter;
//        private Mesh _mesh;

//        private void Awake()
//        {
//            _dispatcher = new PipeJobsDispatcher(Allocator.Persistent);
//            _meshFilter = GetComponent<MeshFilter>();
//            _mesh = new Mesh();

//            _meshFilter.sharedMesh = _mesh;
//        }

//        private void OnDestroy()
//        {
//            _dispatcher.Dispose();
//        }

//        private void Update()
//        {
//            _dispatcher.CutMaxAngle = CutMaxAngle;
//            _dispatcher.MinimalBendAngle = MinimalBendAngle;
//            _dispatcher.VerticesPerCut = VerticesPerCut;
//            _dispatcher.PipeRadius = PipeRadius;

//            _dispatcher.SetNodes(Nodes);

//            _dispatcher.Dispatch();
//        }

//        private void LateUpdate()
//        {
//            _dispatcher.Complete( _mesh );
//        }

//        private void OnDrawGizmos()
//        {
//            for (var i = 0; i < Nodes.Length - 1; i++)
//                Gizmos.DrawLine(Nodes[i].xyz,Nodes[i+1].xyz);

//            _dispatcher?.DrawGizmos();
//        }
//    }
//}



using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace AuroraSeeker.Pipelines4
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class MonoPipe : MonoBehaviour
    {
        [SerializeField] public float4[] Nodes;
        [SerializeField] private float CutMaxAngle = 0.1f;
        [SerializeField] private float MinimalBendAngle = 0.01f;
        [SerializeField] private int VerticesPerCut = 7;
        [SerializeField] private float PipeRadius = 0.1f;

        private PipeJobsDispatcher _dispatcher;
        private MeshFilter _meshFilter;
        private Mesh _mesh;

        private bool isValid = false; // track if dispatcher is valid

        private void Awake()
        {
            _dispatcher = new PipeJobsDispatcher(Allocator.Persistent);
            _meshFilter = GetComponent<MeshFilter>();
            _mesh = new Mesh();
            _meshFilter.sharedMesh = _mesh;
        }

        private void OnDestroy()
        {
            _dispatcher.Dispose();
        }

        private void Update()
        {
            // Basic validation before running dispatcher
            if (Nodes == null || Nodes.Length < 2)
            {
                isValid = false;
                return;
            }
            if (PipeRadius <= 0.001f || VerticesPerCut < 4)
            {
                Debug.LogWarning("Invalid pipe parameters, skipping update.");
                isValid = false;
                return;
            }

            try
            {
                _dispatcher.CutMaxAngle = CutMaxAngle;
                _dispatcher.MinimalBendAngle = MinimalBendAngle;
                _dispatcher.VerticesPerCut = VerticesPerCut;
                _dispatcher.PipeRadius = PipeRadius;

                _dispatcher.SetNodes(Nodes);
                _dispatcher.Dispatch();
                isValid = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Pipe dispatch failed: {e.Message}");
                isValid = false;
            }
        }

        private void LateUpdate()
        {
            if (!isValid) return;

            // Use a temporary mesh so old mesh isn’t destroyed if new one is broken
            Mesh tempMesh = new Mesh();
            try
            {
                _dispatcher.Complete(tempMesh);

                if (tempMesh.vertexCount > 0)
                {
                    _mesh = tempMesh;
                    _meshFilter.sharedMesh = _mesh;
                }
                else
                {
                    Debug.LogWarning("Generated mesh has no vertices, keeping old mesh.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Pipe mesh completion failed: {e.Message}");
            }
        }

        private void OnDrawGizmos()
        {
            if (Nodes == null) return;

            for (var i = 0; i < Nodes.Length - 1; i++)
                Gizmos.DrawLine(Nodes[i].xyz, Nodes[i + 1].xyz);

            _dispatcher?.DrawGizmos();
        }
    }
}
