using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Moves a NavMeshAgent along node waypoints produced by A*.
/// </summary>
public class GuardPathFollower : MonoBehaviour
{
    public NavMeshAgent agent;
    public AStar_Search aStar;
    public GraphReal graph;

    public int startNodeId = 0;
    public int goalNodeId = 1;

    public float waypointTolerance = 0.5f;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (aStar == null) aStar = FindObjectOfType<AStar_Search>();
        if (graph == null) graph = FindObjectOfType<GraphReal>();
    }

    [ContextMenu("ComputeAndFollowPath")]
    public void ComputeAndFollowPath()
    {
        if (aStar == null || graph == null || agent == null) return;
        List<int> path = aStar.FindPath(startNodeId, goalNodeId);
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("GuardPathFollower: No path found.");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(FollowNodePath(path));
    }

    IEnumerator FollowNodePath(List<int> nodePath)
    {
        foreach (int nodeId in nodePath)
        {
            if (!graph.nodePositions.ContainsKey(nodeId)) continue;
            Vector3 target = graph.nodePositions[nodeId];
            agent.SetDestination(target);

            while (Vector3.Distance(agent.transform.position, target) > waypointTolerance)
            {
                yield return null;
            }
        }
    }
}
