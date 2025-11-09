using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor personalizado para o atributo ReadOnly.
/// Permite exibir variáveis como somente leitura no Inspector.
/// </summary>
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Desabilitar a edição do campo
        GUI.enabled = false;
        
        // Desenhar o campo normalmente
        EditorGUI.PropertyField(position, property, label, true);
        
        // Restaurar o estado de edição
        GUI.enabled = true;
    }
} 