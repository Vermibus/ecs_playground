using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;


public class Testing_PathFinding : MonoBehaviour {


    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14; 

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            TestPathFinding(10);
        }
    }

    public void TestPathFinding(int findPathConcurrentJobCount) {

        float startTime = Time.realtimeSinceStartup;

        NativeArray<JobHandle> jobHandleArray = new NativeArray<JobHandle>(findPathConcurrentJobCount, Allocator.TempJob);

        for (int i = 0; i < findPathConcurrentJobCount; i++) {
            FindPathJob findPathJob = new FindPathJob {
                startingPosition = new int2(0, 0),
                endPosition = new int2(19, 19)
            };
            jobHandleArray[i] = findPathJob.Schedule();
        }
        
        JobHandle.CompleteAll(jobHandleArray);
        jobHandleArray.Dispose();

        Debug.Log("Time: " + ((Time.realtimeSinceStartup - startTime) * 1000f));
    }
    
    [BurstCompile]
    private struct FindPathJob : IJob {

        public int2 startingPosition;
        public int2 endPosition;

        public void Execute() {

            int2 gridSize = new int2(20, 20);

            NativeArray<PathNode> pathNodeArray = new NativeArray<PathNode>(gridSize.x * gridSize.y, Allocator.Temp);

            for (int x = 0; x < gridSize.x; x++) {
                for (int y = 0; y < gridSize.y; y++) {
                    PathNode pathNode = new PathNode();
                    pathNode.x = x;
                    pathNode.y = y;
                    pathNode.index = CalculateIndex(x, y, gridSize.x);

                    pathNode.gCost = int.MaxValue;
                    pathNode.hCost = CalculateDistanceCost(new int2(x, y), endPosition);
                    pathNode.CalculateFCost();

                    pathNode.isWalkable = true;
                    pathNode.cameFronNodeIndex = -1;

                    pathNodeArray[pathNode.index] = pathNode;
                }
            }

            // place walls 
            // {
            //     PathNode walkablePathNode = pathNodeArray[CalculateIndex(1, 0, gridSize.x)];
            //     walkablePathNode.SetIsWalkable(false);
            //     pathNodeArray[CalculateIndex(1, 0, gridSize.x)] = walkablePathNode;
                
            //     walkablePathNode = pathNodeArray[CalculateIndex(1, 1, gridSize.x)];
            //     walkablePathNode.SetIsWalkable(false);
            //     pathNodeArray[CalculateIndex(1, 1, gridSize.x)] = walkablePathNode;
                
            //     walkablePathNode = pathNodeArray[CalculateIndex(1, 2, gridSize.x)];
            //     walkablePathNode.SetIsWalkable(false);
            //     pathNodeArray[CalculateIndex(1, 2, gridSize.x)] = walkablePathNode;
                
            // }

            NativeArray<int2> neighbourOffsetsArray = new NativeArray<int2>(8, Allocator.Temp);
            neighbourOffsetsArray[0] = new int2(-1, 0);     // left
            neighbourOffsetsArray[1] = new int2(+1, 0);     // right
            neighbourOffsetsArray[2] = new int2(0, +1);     // up 
            neighbourOffsetsArray[3] = new int2(0, -1);     // down
            neighbourOffsetsArray[4] = new int2(-1, -1);    // left down
            neighbourOffsetsArray[5] = new int2(-1, +1);    // left up
            neighbourOffsetsArray[6] = new int2(+1, -1);    // right down 
            neighbourOffsetsArray[7] = new int2(+1, +1);    // right up

            int endNodeIndex = CalculateIndex(endPosition.x, endPosition.y, gridSize.x);

            PathNode startNode = pathNodeArray[CalculateIndex(startingPosition.x, startingPosition.y, gridSize.x)];
            startNode.gCost = 0;
            startNode.CalculateFCost();
            pathNodeArray[startNode.index] = startNode;

            NativeList<int> openList = new NativeList<int>(Allocator.Temp);
            NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

            openList.Add(startNode.index);

            while (openList.Length > 0) {
                int currrentNodeIndex =  GetLowestCostFNodeIndex(openList, pathNodeArray);
                PathNode currentNode = pathNodeArray[currrentNodeIndex];
                if (currrentNodeIndex == endNodeIndex) {
                    // reached dest
                    break;
                }

                for (int i = 0; i < openList.Length; i++) {
                    if (openList[i] == currrentNodeIndex) {
                        openList.RemoveAtSwapBack(i);
                        break;
                    }
                }

                closedList.Add(currrentNodeIndex);

                for (int i = 0; i < neighbourOffsetsArray.Length; i++) {
                    int2 neighbourOffset = neighbourOffsetsArray[i];
                    int2 neightbourPosition = new int2(currentNode.x + neighbourOffset.x, currentNode.y + neighbourOffset.y);

                    if (!IsPositionInsideGrid(neightbourPosition, gridSize)) {
                        // Neighbour not valid position
                        continue;
                    }

                    int neightbourNodeIndex = CalculateIndex(neightbourPosition.x, neightbourPosition.y, gridSize.x);
                    if (closedList.Contains(neightbourNodeIndex)) {
                        // Allready searched this one
                        continue;
                    }

                    PathNode neightbourNode = pathNodeArray[neightbourNodeIndex];
                    if (!neightbourNode.isWalkable) {
                        // Not walkable
                        continue;
                    }

                    int2 currentNodePosition = new int2(currentNode.x, currentNode.y);

                    int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNodePosition, neightbourPosition);
                    if (tentativeGCost < neightbourNode.gCost) {
                        neightbourNode.cameFronNodeIndex = currrentNodeIndex;
                        neightbourNode.gCost = tentativeGCost;
                        neightbourNode.CalculateFCost();
                        pathNodeArray[neightbourNodeIndex] = neightbourNode;

                        if (!openList.Contains(neightbourNode.index)) {
                            openList.Add(neightbourNode.index);
                        }
                    }
                }
            }

            PathNode endNode = pathNodeArray[endNodeIndex];
            if (endNode.cameFronNodeIndex != -1) {
                // Found a path
                NativeList<int2> path = CalculatePath(pathNodeArray, endNode);
                
                // foreach (int2 pathPosition in path) {
                //     Debug.Log(pathPosition);
                // }

                path.Dispose();
            }

            pathNodeArray.Dispose();
            neighbourOffsetsArray.Dispose();
            openList.Dispose();
            closedList.Dispose();
        }

        private NativeList<int2> CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode) {
                if (endNode.cameFronNodeIndex == -1) {
                    // Couldn't find a path
                    return new NativeList<int2>(Allocator.Temp);
                } else {
                    // Found a path 
                    NativeList<int2> path = new NativeList<int2>(Allocator.Temp);
                    path.Add(new int2(endNode.x, endNode.y));

                    PathNode currentNode = endNode;
                    while (currentNode.cameFronNodeIndex != -1) {
                        PathNode cameFromNode = pathNodeArray[currentNode.cameFronNodeIndex];
                        path.Add(new int2(cameFromNode.x, cameFromNode.y));
                        currentNode = cameFromNode;
                    }

                    return path;
                }
            }

            private bool IsPositionInsideGrid(int2 gridPosition, int2 gridSize) {
                return
                        gridPosition.x >= 0 &&
                        gridPosition.y >= 0 &&
                        gridPosition.x < gridSize.x &&
                        gridPosition.y < gridSize.y;
            }

            private int CalculateIndex(int x, int y, int gridWidth) {
                return x + y * gridWidth;
            }
            
            private int CalculateDistanceCost(int2 aPosition, int2 bPosition) {
                int xDistance = math.abs(aPosition.x - bPosition.x);
                int yDistance = math.abs(aPosition.y - bPosition.y);
                int remaning  = math.abs(xDistance - yDistance);
                return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaning;
            }

            private int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray) {
                PathNode lowestCostPathNode = pathNodeArray[openList[0]];
                for (int i = 1; i < openList.Length; i++) {
                    PathNode testPathNode = pathNodeArray[openList[0]];
                    if (testPathNode.fCost < lowestCostPathNode.fCost) {
                        lowestCostPathNode = testPathNode;
                    }
                }
                return lowestCostPathNode.index;
            }

            private struct PathNode {
                public int x;
                public int y;

                public int index;

                public int gCost;
                public int hCost;
                public int fCost;

                public bool isWalkable;

                public int cameFronNodeIndex;

                public void CalculateFCost() {
                    fCost = gCost + hCost;
                }

                public void SetIsWalkable(bool isWalkable) {
                    this.isWalkable = isWalkable;
                }
            }

    }
}
