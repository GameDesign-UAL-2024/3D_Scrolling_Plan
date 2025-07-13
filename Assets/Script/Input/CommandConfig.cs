using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CommandConfig : MonoBehaviour
{
    [Header("基础设置")]
    [Tooltip("是否启用此技能指令")]
    public bool isActive = true;

    [Header("指令序列配置")]
    [SerializeField]
    [Tooltip("添加和配置技能指令序列")]
    private InstructionSets[] _InstructionSet = new InstructionSets[0]; // 初始化为空数组

    public InstructionSets[] InstructionSet => _InstructionSet;

    // 验证输入限制
    private void OnValidate()
    {
        if (_InstructionSet == null) return;
        
        for (int i = 0; i < _InstructionSet.Length; i++)
        {
            if (_InstructionSet[i] == null)
            {
                _InstructionSet[i] = new InstructionSets(); // 确保每个元素都不为null
                continue;
            }
            
            // 自动修剪超过7个的指令
            if (_InstructionSet[i].sequence.Length > 7)
            {
                Debug.LogWarning($"指令序列超过7个,已自动截断: {_InstructionSet[i].description}");
                System.Array.Resize(ref _InstructionSet[i].sequence, 7);
            }
        }
    }
}