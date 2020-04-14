using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;


public class Testing_Triggers : JobComponentSystem {

    private struct TriggerJob : ITriggerEventsJob {

        public ComponentDataFromEntity<PhysicsVelocity> physicsVelocityEntities;
        

        // Was curious about EntityA and EntityB in triggerEvent.ColliderKeys, 
        // In my example EntityA was always a ball (entity which was moving), maybe if trigger is moving, this will swap them in pair (ball & trigger)
        public void Execute(TriggerEvent triggerEvent) {
            if (physicsVelocityEntities.HasComponent(triggerEvent.Entities.EntityA)) {
                PhysicsVelocity physicsVelocity = physicsVelocityEntities[triggerEvent.Entities.EntityA];
                physicsVelocity.Linear.y = 5f;
                physicsVelocityEntities[triggerEvent.Entities.EntityA] = physicsVelocity;
            }
        }
    }

    private BuildPhysicsWorld buildPhysicsWorld;
    private StepPhysicsWorld stepPhysicsWorld;

    protected override void OnCreate() {
        buildPhysicsWorld =  World.GetOrCreateSystem<BuildPhysicsWorld>();
        stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        TriggerJob triggerJob = new TriggerJob {
            physicsVelocityEntities = GetComponentDataFromEntity<PhysicsVelocity>()
        };
        return triggerJob.Schedule(stepPhysicsWorld.Simulation, ref buildPhysicsWorld.PhysicsWorld, inputDeps);
    }
}