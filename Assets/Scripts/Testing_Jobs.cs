using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;


public class Testing_Jobs : MonoBehaviour {

    [SerializeField] private bool useJobs;
    [SerializeField] private Transform pfZombie;

    private List<Zombie> zombieList;

    public class Zombie {
        public Transform transform;
        public float moveY;
    }

    private void Start() {
        zombieList = new List<Zombie>();
        for (int i=0; i < 1000; i++) {
            Transform zombieTransform = Instantiate(pfZombie, new Vector3(UnityEngine.Random.Range(-9f, 9f), UnityEngine.Random.Range(-4.5f, 5f)), Quaternion.identity);
            zombieList.Add( new Zombie {
                transform = zombieTransform,
                moveY = UnityEngine.Random.Range(1f, 2f)
            });
        }
    }

    private void Update() {
        float startTime = Time.realtimeSinceStartup;

        if (useJobs) { 

            //NativeArray<float3> positionArray = new NativeArray<float3>(zombieList.Count, Allocator.TempJob);
            NativeArray<float> moveYArray = new NativeArray<float>(zombieList.Count, Allocator.TempJob);
            TransformAccessArray transformAccessArray = new TransformAccessArray(zombieList.Count);

            for (int i=0; i < zombieList.Count; i++ ) {
                //positionArray[i] = zombieList[i].transform.position; 
                moveYArray[i] = zombieList[i].moveY;
                transformAccessArray.Add(zombieList[i].transform);
            }

            //ReallyToughtParallelJob reallyToughtParallelJob = new ReallyToughtParallelJob {
            //    deltaTime = Time.deltaTime,
            //    positionArray = positionArray,
            //    moveYArray = moveYArray,
            //};

            //JobHandle jobHandle = reallyToughtParallelJob.Schedule(zombieList.Count, 100);
            //jobHandle.Complete();

            ReallyToughParallerJobTransforms reallyToughParallerJobTransforms = new ReallyToughParallerJobTransforms {
                deltaTime = Time.deltaTime,
                moveYArray = moveYArray
            };

            JobHandle jobHandle = reallyToughParallerJobTransforms.Schedule(transformAccessArray);
            jobHandle.Complete();

            for (int i=0; i < zombieList.Count; i++ ) {
                //zombieList[i].transform.position = positionArray[i];
                zombieList[i].moveY = moveYArray[i];
            }

            //positionArray.Dispose();
            moveYArray.Dispose();
            transformAccessArray.Dispose();

        } else { 
            foreach (Zombie zombie in zombieList) {
                zombie.transform.position += new Vector3(0, zombie.moveY * Time.deltaTime);
                if (zombie.transform.position.y > 5f ) {
                    zombie.moveY = -Mathf.Abs(zombie.moveY);
                } 
            
                if (zombie.transform.position.y < -5f) {
                    zombie.moveY = +Mathf.Abs(zombie.moveY);
                }
            }
        }
    }

    private void ReallyToughTask() {
        
        float value = 0f;
        for (int i=0; i < 50000; i++) {
            value = math.exp10(math.sqrt(value));
        }
    }

    private JobHandle ReallyToughTaskJob() {
        ReallyToughJob job = new ReallyToughJob();
        return job.Schedule();
    }
}

public struct ReallyToughJob : IJob {

    public void Execute() {
        float value = 0f;
        for (int i=0; i < 50000; i++) {
            value = math.exp10(math.sqrt(value));
        }
    }
}


public struct ReallyToughtParallelJob : IJobParallelFor {

    public NativeArray<float3> positionArray; 
    public NativeArray<float> moveYArray;
    public float deltaTime;

    public void Execute(int index) {
        positionArray[index] += new float3(0, moveYArray[index] * deltaTime, 0.0f);

        if (positionArray[index].y > 5f ) {
            moveYArray[index] = -Mathf.Abs(moveYArray[index]);
        } 
            
        if (positionArray[index].y < -5f) {
            moveYArray[index] = +Mathf.Abs(moveYArray[index]);
        }
    }
}

public struct ReallyToughParallerJobTransforms : IJobParallelForTransform {

    public NativeArray<float> moveYArray;
    public float deltaTime;

    public void Execute(int index, TransformAccess transform)
    {
        transform.position += new Vector3(0, moveYArray[index] * deltaTime, 0.0f);

        if (transform.position.y > 5f)
        {
            moveYArray[index] = -Mathf.Abs(moveYArray[index]);
        }

        if (transform.position.y < -5f)
        {
            moveYArray[index] = +Mathf.Abs(moveYArray[index]);
        }
    }
}