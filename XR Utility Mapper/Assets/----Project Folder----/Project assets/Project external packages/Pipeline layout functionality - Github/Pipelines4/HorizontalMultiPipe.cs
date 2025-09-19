using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace AuroraSeeker.Pipelines4
{
    [SelectionBase]
    public class HorizontalMultiPipe : MonoBehaviour
    {
        [Header("Pipe Settings")]
        public float Separation = 0.5f;
        [Range(0f, 1f)] public float RoundizatorMult = 1f;
        public float RoundizatorFlat = 1f;
        public float CutMaxAngle = 0.15f;
        public float MinimalBendAngle = 0.05f;
        public int VerticesPerCut = 7;
        public float PipeRadius = 0.2f;

        [Header("Node Placement")]
        public Camera mainCamera;
        public LayerMask groundMask;

        // Runtime node storage
        public List<float3> NodeList = new List<float3>();
        public float3[] Nodes => NodeList.ToArray();

        private MeshFilter[] _meshFilters;
        private PipeJobsDispatcher[] _dispatchers;
        private float4[][] _nodesBuffers;

        private void Awake()
        {
            if (mainCamera == null) mainCamera = Camera.main;

            _meshFilters = GetComponentsInChildren<MeshFilter>().ToArray();
            _dispatchers = new PipeJobsDispatcher[_meshFilters.Length];

            for (var i = 0; i < _meshFilters.Length; i++)
                _dispatchers[i] = new PipeJobsDispatcher(Allocator.Persistent);

            _nodesBuffers = new float4[_meshFilters.Length][];

            foreach (var t in _meshFilters)
                t.mesh = new Mesh();
        }

        private void Update()
        {
            HandleMouseInput();

            if (NodeList.Count < 2) return; // Need at least 2 points for a pipe

            // Resize node buffers
            for (var i = 0; i < _nodesBuffers.Length; i++)
                _nodesBuffers[i] = new float4[NodeList.Count];

            // Fill intermediate nodes
            for (var i = 1; i < NodeList.Count - 1; i++)
            {
                var in2d = (NodeList[i] - NodeList[i - 1]).xz;
                var out2d = (NodeList[i] - NodeList[i + 1]).xz;

                var in2n = math.normalize(in2d);
                var out2n = math.normalize(out2d);

                var inHdg = math.atan2(in2n.y, in2n.x);
                var outHdg = math.atan2(out2n.y, out2n.x);
                var meanHdg = (inHdg + outHdg) / 2f;

                var meanVec3d = new float3(math.cos(meanHdg), 0, math.sin(meanHdg));
                var crossingScalar = 1 / (math.sin(((inHdg - outHdg) / 2f)));
                var shift = meanVec3d * crossingScalar * Separation;

                for (var pipeIndex = 0; pipeIndex < _dispatchers.Length; pipeIndex++)
                {
                    var pipeShiftScalar = GetArmScalar(pipeIndex, _dispatchers.Length);
                    var subNodePoint = NodeList[i] + shift * pipeShiftScalar;
                    var subNodeRadius = Separation + RoundizatorFlat +
                                        pipeShiftScalar * math.abs(crossingScalar) * Separation * RoundizatorMult;

                    _nodesBuffers[pipeIndex][i] = new float4(subNodePoint, subNodeRadius);
                }
            }

            // Send to dispatcher
            for (var i = 0; i < _dispatchers.Length; i++)
            {
                var firstDelta = NodeList[1] - NodeList[0];
                SetupNodeDirectly(0, firstDelta, 1f);

                var lastDelta = NodeList[NodeList.Count - 1] - NodeList[NodeList.Count - 2];
                SetupNodeDirectly(NodeList.Count - 1, lastDelta, 1f);

                _dispatchers[i].SetNodes(_nodesBuffers[i]);
                _dispatchers[i].CutMaxAngle = CutMaxAngle;
                _dispatchers[i].MinimalBendAngle = MinimalBendAngle;
                _dispatchers[i].VerticesPerCut = VerticesPerCut;
                _dispatchers[i].PipeRadius = PipeRadius;

                try { _dispatchers[i].Dispatch(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }

        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0)) // Left click
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundMask))
                {
                    NodeList.Add((float3)hit.point);
                    Debug.Log($"Node added: {hit.point}");
                }
            }
        }

        private void SetupNodeDirectly(int nodeIndex, float3 delta, float roundization)
        {
            var rightVector = math.cross(new float3(0f, 1f, 0f), delta);
            rightVector = math.normalize(rightVector);

            for (var pipeIndex = 0; pipeIndex < _dispatchers.Length; pipeIndex++)
            {
                var pipeShiftScalar = GetArmScalar(pipeIndex, _dispatchers.Length);
                var shift = rightVector * pipeShiftScalar * Separation;
                _nodesBuffers[pipeIndex][nodeIndex] = new float4(NodeList[nodeIndex] + shift, roundization);
            }
        }

        private void LateUpdate()
        {
            for (var i = 0; i < _dispatchers.Length; i++)
            {
                if (_dispatchers[i].CurrentState != PipeJobsDispatcher.State.Dispatched) continue;
                _dispatchers[i].Complete(_meshFilters[i].sharedMesh);
            }
        }

        private void OnDestroy()
        {
            foreach (var dispatcher in _dispatchers)
                dispatcher.Dispose();
        }

        private static float GetArmScalar(int index, int len)
        {
            return -(len - 1) / 2f + index;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            for (var i = 0; i < NodeList.Count - 1; i++)
                Gizmos.DrawLine(NodeList[i], NodeList[i + 1]);
        }
    }
}
