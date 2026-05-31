using System.Collections.Generic;
using UnityEngine;

public class GraphNode : MonoBehaviour
{
    public int nodeId;
    public string nodeName;

    [Header("Connected Nodes")]
    public List<GraphNode> connectedNodes = new List<GraphNode>();
}