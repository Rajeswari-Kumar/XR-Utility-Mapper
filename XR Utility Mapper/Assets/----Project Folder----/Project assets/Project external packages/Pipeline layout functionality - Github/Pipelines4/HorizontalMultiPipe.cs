//using System;
//using System.Linq;
//using System.Collections.Generic;
//using Unity.Collections;
//using Unity.Mathematics;
//using UnityEngine;

//namespace AuroraSeeker.Pipelines4
//{
//    [SelectionBase]
//    public class HorizontalMultiPipe : MonoBehaviour
//    {
//        [Header("Pipe Settings")]
//        public float Separation = 0.5f;
//        [Range(0f, 1f)] public float RoundizatorMult = 1f;
//        public float RoundizatorFlat = 1f;
//        public float CutMaxAngle = 0.15f;
//        public float MinimalBendAngle = 0.05f;
//        public int VerticesPerCut = 7;
//        public float PipeRadius = 0.2f;

//        [Header("Node Placement")]
//        public Camera mainCamera;
//        public LayerMask groundMask;

//        // Runtime node storage
//        public List<float3> NodeList = new List<float3>();
//        public float3[] Nodes => NodeList.ToArray();

//        private MeshFilter[] _meshFilters;
//        private PipeJobsDispatcher[] _dispatchers;
//        private float4[][] _nodesBuffers;

//        private void Awake()
//        {
//            if (mainCamera == null) mainCamera = Camera.main;

//            _meshFilters = GetComponentsInChildren<MeshFilter>().ToArray();
//            _dispatchers = new PipeJobsDispatcher[_meshFilters.Length];

//            for (var i = 0; i < _meshFilters.Length; i++)
//                _dispatchers[i] = new PipeJobsDispatcher(Allocator.Persistent);

//            _nodesBuffers = new float4[_meshFilters.Length][];

//            foreach (var t in _meshFilters)
//                t.mesh = new Mesh();
//        }

//        private void Update()
//        {
//            HandleMouseInput();

//            if (NodeList.Count < 2) return; // Need at least 2 points for a pipe

//            // Resize node buffers
//            for (var i = 0; i < _nodesBuffers.Length; i++)
//                _nodesBuffers[i] = new float4[NodeList.Count];

//            // Fill intermediate nodes
//            for (var i = 1; i < NodeList.Count - 1; i++)
//            {
//                var in2d = (NodeList[i] - NodeList[i - 1]).xz;
//                var out2d = (NodeList[i] - NodeList[i + 1]).xz;

//                var in2n = math.normalize(in2d);
//                var out2n = math.normalize(out2d);

//                var inHdg = math.atan2(in2n.y, in2n.x);
//                var outHdg = math.atan2(out2n.y, out2n.x);
//                var meanHdg = (inHdg + outHdg) / 2f;

//                var meanVec3d = new float3(math.cos(meanHdg), 0, math.sin(meanHdg));
//                var crossingScalar = 1 / (math.sin(((inHdg - outHdg) / 2f)));
//                var shift = meanVec3d * crossingScalar * Separation;

//                for (var pipeIndex = 0; pipeIndex < _dispatchers.Length; pipeIndex++)
//                {
//                    var pipeShiftScalar = GetArmScalar(pipeIndex, _dispatchers.Length);
//                    var subNodePoint = NodeList[i] + shift * pipeShiftScalar;
//                    var subNodeRadius = Separation + RoundizatorFlat +
//                                        pipeShiftScalar * math.abs(crossingScalar) * Separation * RoundizatorMult;

//                    _nodesBuffers[pipeIndex][i] = new float4(subNodePoint, subNodeRadius);
//                }
//            }

//            // Send to dispatcher
//            for (var i = 0; i < _dispatchers.Length; i++)
//            {
//                var firstDelta = NodeList[1] - NodeList[0];
//                SetupNodeDirectly(0, firstDelta, 1f);

//                var lastDelta = NodeList[NodeList.Count - 1] - NodeList[NodeList.Count - 2];
//                SetupNodeDirectly(NodeList.Count - 1, lastDelta, 1f);

//                _dispatchers[i].SetNodes(_nodesBuffers[i]);
//                _dispatchers[i].CutMaxAngle = CutMaxAngle;
//                _dispatchers[i].MinimalBendAngle = MinimalBendAngle;
//                _dispatchers[i].VerticesPerCut = VerticesPerCut;
//                _dispatchers[i].PipeRadius = PipeRadius;

//                try { _dispatchers[i].Dispatch(); }
//                catch (Exception ex) { Debug.LogError(ex); }
//            }
//        }

//        private void HandleMouseInput()
//        {
//            if (Input.GetMouseButtonDown(0)) // Left click
//            {
//                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
//                if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundMask))
//                {
//                    NodeList.Add((float3)hit.point);
//                    Debug.Log($"Node added: {hit.point}");
//                }
//            }
//        }

//        private void SetupNodeDirectly(int nodeIndex, float3 delta, float roundization)
//        {
//            var rightVector = math.cross(new float3(0f, 1f, 0f), delta);
//            rightVector = math.normalize(rightVector);

//            for (var pipeIndex = 0; pipeIndex < _dispatchers.Length; pipeIndex++)
//            {
//                var pipeShiftScalar = GetArmScalar(pipeIndex, _dispatchers.Length);
//                var shift = rightVector * pipeShiftScalar * Separation;
//                _nodesBuffers[pipeIndex][nodeIndex] = new float4(NodeList[nodeIndex] + shift, roundization);
//            }
//        }

//        private void LateUpdate()
//        {
//            for (var i = 0; i < _dispatchers.Length; i++)
//            {
//                if (_dispatchers[i].CurrentState != PipeJobsDispatcher.State.Dispatched) continue;
//                _dispatchers[i].Complete(_meshFilters[i].sharedMesh);
//            }
//        }

//        private void OnDestroy()
//        {
//            foreach (var dispatcher in _dispatchers)
//                dispatcher.Dispose();
//        }

//        private static float GetArmScalar(int index, int len)
//        {
//            return -(len - 1) / 2f + index;
//        }

//        private void OnDrawGizmos()
//        {
//            Gizmos.color = Color.yellow;
//            for (var i = 0; i < NodeList.Count - 1; i++)
//                Gizmos.DrawLine(NodeList[i], NodeList[i + 1]);
//        }
//    }
//}


using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;

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

        // safety constants
        private const float EPS = 1e-4f;                // small length threshold (meters)
        private const float MIN_SIN = 1e-3f;            // avoid division by extremely small sin -> clamp
        private const float MAX_SHIFT_MULT = 10f;      // cap on crossingScalar-derived shift multiplier

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

            // sanitize NodeList: remove invalid points (NaN/Inf)
            SanitizeNodeList();

            if (NodeList.Count < 2) return; // Need at least 2 points for a pipe

            // Resize node buffers to NodeList.Count and prefill safe defaults
            for (var i = 0; i < _nodesBuffers.Length; i++)
            {
                _nodesBuffers[i] = new float4[NodeList.Count];
                for (int j = 0; j < NodeList.Count; j++)
                    _nodesBuffers[i][j] = new float4(NodeList[j], PipeRadius); // sensible default
            }

            // Ensure first and last nodes are set up (so buffers are never left uninitialized)
            var firstDelta = NodeList[1] - NodeList[0];
            var lastDelta = NodeList[NodeList.Count - 1] - NodeList[NodeList.Count - 2];

            for (var i = 0; i < _dispatchers.Length; i++)
            {
                SetupNodeDirectly(i, 0, firstDelta, 1f);
                SetupNodeDirectly(i, NodeList.Count - 1, lastDelta, 1f);
            }

            // Fill intermediate nodes with safe math (guarded against degenerate cases)
            for (var i = 1; i < NodeList.Count - 1; i++)
            {
                var pPrev = NodeList[i - 1];
                var pCur = NodeList[i];
                var pNext = NodeList[i + 1];

                // 2D projections on XZ plane
                var in2d = (pCur - pPrev).xz;
                var out2d = (pCur - pNext).xz;

                float inLen = math.length(in2d);
                float outLen = math.length(out2d);

                bool degenerate = inLen < EPS || outLen < EPS;

                // compute normalized directions safely
                float2 in2n = degenerate ? new float2(1f, 0f) : in2d / math.max(inLen, EPS);
                float2 out2n = degenerate ? new float2(1f, 0f) : out2d / math.max(outLen, EPS);

                // compute headings
                float inHdg = math.atan2(in2n.y, in2n.x);
                float outHdg = math.atan2(out2n.y, out2n.x);

                // half-angle mean heading
                float meanHdg = (inHdg + outHdg) / 2f;

                // mean direction in world xz
                var meanVec3d = new float3(math.cos(meanHdg), 0, math.sin(meanHdg));

                // compute crossingScalar safely (sin of half-delta)
                float halfDelta = (inHdg - outHdg) / 2f;
                float sinHalf = math.sin(halfDelta);

                // avoid divide-by-zero: if sinHalf is tiny, fallback to safe value 1
                float crossingScalar = 1f;
                if (math.abs(sinHalf) >= MIN_SIN)
                    crossingScalar = 1f / sinHalf;
                else
                    crossingScalar = 1f; // fallback: no large shift

                // clamp crossingScalar to avoid exploding shifts
                crossingScalar = math.clamp(crossingScalar, -MAX_SHIFT_MULT, MAX_SHIFT_MULT);

                // compute the shift vector
                var shift = meanVec3d * crossingScalar * Separation;

                // now fill buffers for each dispatcher, but if degenerate, use safe default
                for (var pipeIndex = 0; pipeIndex < _dispatchers.Length; pipeIndex++)
                {
                    var pipeShiftScalar = GetArmScalar(pipeIndex, _dispatchers.Length);

                    // if degenerate (colocated points or tiny segments), do not apply huge shift
                    if (degenerate)
                    {
                        // just use the node position with a reasonable radius
                        _nodesBuffers[pipeIndex][i] = new float4(pCur + new float3(0, 0, 0), Separation + RoundizatorFlat);
                        continue;
                    }

                    var subNodePoint = pCur + shift * pipeShiftScalar;

                    // compute radius with safeguards
                    var subNodeRadius = Separation + RoundizatorFlat +
                                        pipeShiftScalar * math.abs(crossingScalar) * Separation * RoundizatorMult;

                    // clamp radius to a sane positive range
                    subNodeRadius = math.max(0.001f, subNodeRadius);

                    // final safety cap on subNodePoint offset: do not allow more than Separation * MAX_SHIFT_MULT in magnitude
                    float3 offset = subNodePoint - pCur;
                    if (math.length(offset) > Separation * MAX_SHIFT_MULT)
                        subNodePoint = pCur + math.normalize(offset) * (Separation * MAX_SHIFT_MULT);

                    _nodesBuffers[pipeIndex][i] = new float4(subNodePoint, subNodeRadius);
                }
            }

            // Send to dispatcher (safe: buffers are fully populated)
            for (var i = 0; i < _dispatchers.Length; i++)
            {
                _dispatchers[i].SetNodes(_nodesBuffers[i]);
                _dispatchers[i].CutMaxAngle = CutMaxAngle;
                _dispatchers[i].MinimalBendAngle = MinimalBendAngle;
                _dispatchers[i].VerticesPerCut = VerticesPerCut;
                _dispatchers[i].PipeRadius = PipeRadius;

                try
                {
                    _dispatchers[i].Dispatch();
                }
                catch (Exception ex)
                {
                    // don't null out meshes on exception — log so we can debug but keep previous visible pipe
                    Debug.LogError($"PipeJobsDispatcher.Dispatch failed for dispatcher[{i}]: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0)) // Left click
            {
                // ignore input if pointer is over UI
                if (EventSystem.current.IsPointerOverGameObject())
                    return;


                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundMask))
                {
                    var candidate = (float3)hit.point;

                    if (IsValidFloat3(candidate))
                    {
                        // Add only valid nodes
                        NodeList.Add(candidate);
                        Debug.Log($"✅ Node added: {hit.point}");
                    }
                    else
                    {
                        // Skip invalid node — pipeline remains intact
                        Debug.LogWarning($"⚠️ Ignored invalid node: {hit.point}");
                    }
                }
            }
        }


        // this helper ensures bad float values are removed from NodeList
        private void SanitizeNodeList()
        {
            for (int i = NodeList.Count - 1; i >= 0; i--)
            {
                if (!IsValidFloat3(NodeList[i]))
                {
                    Debug.LogWarning($"Removing invalid node at index {i} (NaN/Inf): {NodeList[i]}");
                    NodeList.RemoveAt(i);
                }
            }
        }

        private bool IsValidFloat3(float3 v)
        {
            // check for nan/inf components
            return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z)
                     || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
        }

        // adjusted SetupNodeDirectly to accept dispatcher index and initialize the buffer entry safely
        private void SetupNodeDirectly(int dispatcherIndex, int nodeIndex, float3 delta, float roundization)
        {
            var rightVector = math.cross(new float3(0f, 1f, 0f), delta);
            float rvLen = math.length(rightVector);
            if (rvLen < EPS) rightVector = new float3(1f, 0f, 0f);
            else rightVector = rightVector / rvLen;

            var pipeShiftScalar = GetArmScalar(dispatcherIndex, _dispatchers.Length);
            var shift = rightVector * pipeShiftScalar * Separation;

            // ensure nodes buffer exists and has proper length
            if (_nodesBuffers[dispatcherIndex] == null || nodeIndex < 0 || nodeIndex >= _nodesBuffers[dispatcherIndex].Length)
                return;

            var p = NodeList[nodeIndex] + shift;
            _nodesBuffers[dispatcherIndex][nodeIndex] = new float4(p, roundization);
        }

        private void LateUpdate()
        {
            for (var i = 0; i < _dispatchers.Length; i++)
            {
                if (_dispatchers[i].CurrentState != PipeJobsDispatcher.State.Dispatched) continue;

                try
                {
                    _dispatchers[i].Complete(_meshFilters[i].sharedMesh);
                }
                catch (Exception ex)
                {
                    // keep previous mesh and just log the error
                    Debug.LogError($"PipeJobsDispatcher.Complete failed for dispatcher[{i}]: {ex.Message}\n{ex.StackTrace}");
                }
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


