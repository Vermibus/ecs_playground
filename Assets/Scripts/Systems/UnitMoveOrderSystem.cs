using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;


public class UnityMoveOrderSystem : ComponentSystem {

    protected override void OnUpdate() {

        if (Input.GetMouseButtonDown(0)) {
            Entities.ForEach((Entity entity, ref Translation translation) => {
                EntityManager.AddComponentData(entity, new PathFindingParamsComponent {
                    startPosition = new int2(2, 0),
                    endPosition = new int2(4, 0),
                });
            });
        }
    }
}
