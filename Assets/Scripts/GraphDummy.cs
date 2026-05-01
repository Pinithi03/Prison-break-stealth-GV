using System.Collections.Generic;
using UnityEngine;

public class GraphDummy : MonoBehaviour
{
    // Node ID -> 3D Position
    public Dictionary<int, Vector3> nodePositions = new Dictionary<int, Vector3>();
    
    // Node ID -> List of connected Node IDs (Adjacency List)
    public Dictionary<int, List<int>> adjacencyList = new Dictionary<int, List<int>>();

    void Awake()
    {
        // Setup a hardcoded dummy graph for S4 testing
        
        // Node Positions
        nodePositions.Add(0, new Vector3(0, 0, 0));
        nodePositions.Add(1, new Vector3(5, 0, 0));
        nodePositions.Add(2, new Vector3(10, 0, 0));
        nodePositions.Add(3, new Vector3(5, 0, 5));
        nodePositions.Add(4, new Vector3(10, 0, 5));

        // Edges
        adjacencyList.Add(0, new List<int> { 1 });
        adjacencyList.Add(1, new List<int> { 0, 2, 3 });
        adjacencyList.Add(2, new List<int> { 1, 4 });
        adjacencyList.Add(3, new List<int> { 1, 4 });
        adjacencyList.Add(4, new List<int> { 2, 3 });
    }
}
