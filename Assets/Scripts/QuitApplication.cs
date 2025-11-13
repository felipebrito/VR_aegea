using UnityEngine;

public class QuitApplication : MonoBehaviour
{
    /// <summary>
    /// Fecha a aplicação. Pode ser chamado diretamente de um botão UI.
    /// </summary>
    public void Quit()
    {
        Debug.Log("🚪 Fechando aplicação...");
        
        #if UNITY_EDITOR
        // No Editor, para a execução
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        // No Build, fecha a aplicação
        Application.Quit();
        #endif
    }
}

