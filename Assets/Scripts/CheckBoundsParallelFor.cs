using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

// this is optimized for checking against a static set of Bounds,
// but can easily be adapted to handle when they change
public class CheckBoundsParallelFor : BaseJobObjectExample
{
    [SerializeField]
    protected float m_ObjectPlacementRadius = 100f;

    [SerializeField]
    protected Bounds m_LastResult;

    [SerializeField]
    protected Bounds m_LastRayResult;

    NativeArray<Vector3> m_Positions;
    NativeArray<Bounds> m_NativeBounds;
    NativeArray<int> m_RayIntersectionResults;

    NativeArray<Bounds> m_Results;

    BoundsContainsPointJob m_Job;
    BoundsIntersectionJob m_IntersectionJob;

    RayIntersectionJob m_RayIntersectionJob;
    RayIntersectionListJob m_RayIntersectionListJob;

    JobHandle m_JobHandle;
    JobHandle m_IntersectionJobHandle;
    JobHandle m_RayIntersectionJobHandle;
    JobHandle m_RayIntersectionListJobHandle;

    protected virtual void Start()
    {
        m_Positions = new NativeArray<Vector3>(m_ObjectCount, Allocator.Persistent);
        m_NativeBounds = new NativeArray<Bounds>(m_ObjectCount, Allocator.Persistent);
        m_RayIntersectionResults = new NativeArray<int>(m_ObjectCount, Allocator.Persistent);

        m_Objects = new GameObject[m_ObjectCount];
        m_Objects = SetupUtils.PlaceRandomCubes(m_ObjectCount, m_ObjectPlacementRadius);

        m_Transforms = new Transform[m_ObjectCount];
        m_Renderers = new Renderer[m_ObjectCount];

        for (int i = 0; i < m_ObjectCount; i++)
        {
            m_Transforms[i] = m_Objects[i].transform;
            m_Renderers[i] = m_Objects[i].GetComponent<Renderer>();
            m_NativeBounds[i] = m_Renderers[i].bounds;
        }
    }

    struct BoundsContainsPointJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Bounds> boundsArray;

        public Vector3 point;

        public Bounds resultBounds;

        // The code actually running on the job
        public void Execute(int i)
        {
            Bounds testAgainst = boundsArray[i];
            if (testAgainst.Contains(point))
            {
                Debug.Log("point " + point + " is in Bounds: " + testAgainst);
                resultBounds = testAgainst; 
            }
        }
    }

    struct BoundsIntersectionJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Bounds> boundsArray;

        public Bounds boundsToCheck;

        public Bounds resultBounds;

        public void Execute(int i)
        {
            Bounds testAgainst = boundsArray[i];
            if (boundsToCheck.Intersects(testAgainst))
            {
                // Debug.Log(boundsToCheck + " intersects with: " + testAgainst);
                resultBounds = testAgainst;
            }
        }
    }

    // assemble an array of ints representing whether the associated Bounds was intersected
    struct RayIntersectionJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Bounds> boundsArray;

        public NativeArray<int> results;

        public Ray ray;

        public void Execute(int i)
        {
            Bounds testAgainst = boundsArray[i];
            if (testAgainst.IntersectRay(ray))
            {
                // Debug.Log("ray " + ray + " intersects with: " + testAgainst);
                results[i] = 1;
            }
        }
    }

    // now use a single-threaded job to assemble a friendlier results array
    struct RayIntersectionListJob : IJob
    {
        [ReadOnly]
        public NativeArray<Bounds> boundsArray;

        [ReadOnly]
        public NativeArray<int> boundsIntersected;

        public NativeArray<Bounds> results;

        int resultIndex;

        public void Execute()
        {
            for (int i = 0; i < boundsArray.Length; i++)
            {
                if (boundsIntersected[i] == 1)
                {
                    results[resultIndex] = boundsArray[i];
                    resultIndex++;
                }

                if (resultIndex == results.Length)
                    break;
            }
        }
    }

    // in real code you'd want to schedule the job early instead of this
    public void LateUpdate()
    {
        m_RayIntersectionListJobHandle.Complete();

        m_JobHandle.Complete();
        m_IntersectionJobHandle.Complete();

        m_LastResult = m_IntersectionJob.resultBounds;

        Debug.Log(m_RayIntersectionListJob.results[0]);

        m_Results.Dispose();
    }

    public void Update()
    {
        var point = UnityEngine.Random.insideUnitSphere * 100f;

        // check if a point intersects any of the cube's bounds
        m_Job = new BoundsContainsPointJob()
        {
            point = point,
            boundsArray = m_NativeBounds,
        };

        // check if a bounding box outside that point intersects any cubes
        m_IntersectionJob = new BoundsIntersectionJob()
        {
            boundsToCheck = new Bounds(point, Vector3.one),
            boundsArray = m_NativeBounds
        };

        var randomVec = UnityEngine.Random.insideUnitSphere;
        var testRay = new Ray(Vector3.zero - randomVec, Vector3.right + Vector3.up + randomVec);

        m_RayIntersectionJob = new RayIntersectionJob()
        {
            ray = testRay,
            results = m_RayIntersectionResults,
            boundsArray = m_NativeBounds
        };

        m_Results = new NativeArray<Bounds>(new Bounds[20], Allocator.TempJob);

        m_RayIntersectionListJob = new RayIntersectionListJob()
        {
            boundsIntersected = m_RayIntersectionResults,
            boundsArray = m_NativeBounds,
            results = m_Results
        };

        m_RayIntersectionJobHandle = m_RayIntersectionJob.Schedule(m_NativeBounds.Length, 64);
        m_RayIntersectionListJobHandle = m_RayIntersectionListJob.Schedule(m_RayIntersectionJobHandle);

        m_JobHandle = m_Job.Schedule(m_Positions.Length, 64);
        m_IntersectionJobHandle = m_IntersectionJob.Schedule(m_NativeBounds.Length, 64);
    }

    private void OnDestroy()
    {
        m_Positions.Dispose();
        m_NativeBounds.Dispose();
        m_RayIntersectionResults.Dispose();
    }
}