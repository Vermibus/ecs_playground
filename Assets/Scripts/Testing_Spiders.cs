using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Rendering;

public class Testing_Spiders : MonoBehaviour {

    public int numberOfSpiders = 5;

    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;

    private void Start() {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        EntityArchetype entityArchetype = entityManager.CreateArchetype(
            typeof(LevelComponent),
            typeof(Translation),
            typeof(MoveSpeedComponent),
            typeof(RenderMesh),
            typeof(LocalToWorld),
            typeof(RenderBounds)
        );

        NativeArray<Entity> entityArray = new NativeArray<Entity>(numberOfSpiders, Allocator.Temp);
        entityManager.CreateEntity(entityArchetype, entityArray);

        for (int i=0; i < entityArray.Length; i++) {
            Entity entity = entityArray[i];

            entityManager.SetComponentData(entity,
                new LevelComponent() { 
                    level = UnityEngine.Random.Range(10, 20) 
                }
            );
            entityManager.SetComponentData(entity,
                new MoveSpeedComponent() {
                    moveSpeed = UnityEngine.Random.Range(1f, 3f) 
                }
            );
            entityManager.SetComponentData(entity,
                new Translation() {
                    Value = new float3(UnityEngine.Random.Range(-9, 9f), UnityEngine.Random.Range(-4.5f, 6f), 0.0f)
                }
            );

            entityManager.SetSharedComponentData(entity, new RenderMesh {
                mesh = mesh, 
                material = material
            });
        }

        entityArray.Dispose();
    }
}