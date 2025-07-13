using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CommandConfig))]
public class CommandConfigEditor : Editor
{
    private SerializedProperty isActiveProp;
    private SerializedProperty instructionSetProp;

    private void OnEnable()
    {
        isActiveProp = serializedObject.FindProperty("isActive");
        instructionSetProp = serializedObject.FindProperty("_InstructionSet");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // 绘制基础设置
        EditorGUILayout.PropertyField(isActiveProp);
        
        // 绘制指令序列配置
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("指令序列配置", EditorStyles.boldLabel);
        
        // 显示序列数量控制
        int arraySize = instructionSetProp.arraySize;
        int newSize = EditorGUILayout.IntField("序列数量", arraySize);
        
        if (newSize != arraySize)
        {
            instructionSetProp.arraySize = newSize;
        }
        
        // 添加序列按钮
        if (GUILayout.Button("添加新序列"))
        {
            instructionSetProp.arraySize++;
        }
        
        EditorGUILayout.Space(5);
        
        // 绘制每个序列
        for (int i = 0; i < instructionSetProp.arraySize; i++)
        {
            var sequenceProp = instructionSetProp.GetArrayElementAtIndex(i);
            
            EditorGUILayout.BeginVertical("box");
            
            // 序列标题和删除按钮
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"序列 #{i + 1}", EditorStyles.boldLabel);
            
            if (GUILayout.Button("删除", GUILayout.Width(60)))
            {
                instructionSetProp.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                serializedObject.ApplyModifiedProperties();
                return;
            }
            EditorGUILayout.EndHorizontal();
            
            // 描述
            var descriptionProp = sequenceProp.FindPropertyRelative("description");
            EditorGUILayout.PropertyField(descriptionProp, new GUIContent("描述"));
            
            // 间隔时间
            var intervalProp = sequenceProp.FindPropertyRelative("maxInterval");
            EditorGUILayout.PropertyField(intervalProp, new GUIContent("最大间隔时间"));
            
            // 指令序列
            var sequenceArrayProp = sequenceProp.FindPropertyRelative("sequence");
            
            // 显示指令数量控制
            int seqSize = sequenceArrayProp.arraySize;
            int newSeqSize = EditorGUILayout.IntField("指令数量", seqSize);
            
            if (newSeqSize != seqSize)
            {
                sequenceArrayProp.arraySize = newSeqSize;
            }
            
            // 限制最大指令数
            if (sequenceArrayProp.arraySize > 7)
            {
                sequenceArrayProp.arraySize = 7;
                EditorGUILayout.HelpBox("指令序列最多只能包含7个指令", MessageType.Warning);
            }
            
            // 绘制每个指令
            for (int j = 0; j < sequenceArrayProp.arraySize; j++)
            {
                var elementProp = sequenceArrayProp.GetArrayElementAtIndex(j);
                EditorGUILayout.PropertyField(elementProp, new GUIContent($"指令 #{j + 1}"));
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}