using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Script simples para testar se a aplicação abre sem erros
/// Adicione este script em um GameObject na cena para debug
/// </summary>
public class SimpleStartupTest : MonoBehaviour
{
    public TextMeshProUGUI debugText;
    
    void Awake()
    {
        Debug.Log("🚀 SimpleStartupTest.Awake() - Aplicação iniciando...");
    }
    
    void Start()
    {
        Debug.Log("✅ SimpleStartupTest.Start() - Aplicação iniciada!");
        
        // Tentar mostrar mensagem na tela se houver UI
        if (debugText != null)
        {
            debugText.text = "✅ App iniciou com sucesso!\n\nVerifique os logs para mais informações.";
        }
        
        // Log informações do sistema
        Debug.Log($"📱 Device: {SystemInfo.deviceModel}");
        Debug.Log($"📱 OS: {SystemInfo.operatingSystem}");
        Debug.Log($"📱 Unity: {Application.unityVersion}");
        Debug.Log($"📁 PersistentDataPath: {Application.persistentDataPath}");
        Debug.Log($"📁 StreamingAssetsPath: {Application.streamingAssetsPath}");
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        Debug.Log($"📱 App {(pauseStatus ? "pausado" : "retomado")}");
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        Debug.Log($"📱 App {(hasFocus ? "em foco" : "perdeu foco")}");
    }
}

