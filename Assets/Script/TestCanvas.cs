using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCanvas : MonoBehaviour
{
    [SerializeField]
    GameObject InstructionNotices;

    private Queue<KeyValuePair<GameObject,int>> instructions = new();

    void Update()
    {
        if (instructions.Count > 0 && Time.frameCount - instructions.Peek().Value >= 15)
        {
            Destroy(instructions.Dequeue().Key);
        }
    }
    public void AddInstructionNode(GameObject obj)
    {
        KeyValuePair<GameObject,int> NodeInfo = new KeyValuePair<GameObject,int>(obj, Time.frameCount);
        instructions.Enqueue(NodeInfo);
        obj.transform.SetParent(InstructionNotices.transform);
        obj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        obj.transform.localScale = Vector3.one;
    }
}
