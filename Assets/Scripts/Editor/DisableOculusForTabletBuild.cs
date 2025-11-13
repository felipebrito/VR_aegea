using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

/// <summary>
/// Script que desabilita automaticamente o Oculus XR Plugin antes de compilar para tablet
/// </summary>
public class DisableOculusForTabletBuild : IPreprocessBuildWithReport
{
    public int callbackOrder => -100; // Executa primeiro
    
    public void OnPreprocessBuild(BuildReport report)
    {
        // Só processar se for build para Android
        if (report.summary.platform != BuildTarget.Android)
        {
            return;
        }
        
        Debug.Log("🔧 DisableOculusForTabletBuild: Desabilitando Oculus XR Plugin...");
        
        try
        {
            // Desabilitar Oculus Loader via XR Management
            #if UNITY_XR_MANAGEMENT_4_0_0_OR_NEWER
            var xrGeneralSettings = UnityEngine.XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (xrGeneralSettings != null)
            {
                var xrManager = xrGeneralSettings.Manager;
                if (xrManager != null)
                {
                    // Remover Oculus Loader da lista
                    var loaders = xrManager.activeLoaders;
                    bool removed = false;
                    
                    foreach (var loader in loaders)
                    {
                        if (loader != null && loader.GetType().Name.Contains("Oculus"))
                        {
                            Debug.Log($"⚠️ Removendo Oculus Loader: {loader.GetType().Name}");
                            xrManager.TryRemoveLoader(loader);
                            removed = true;
                        }
                    }
                    
                    // Desabilitar inicialização automática
                    xrManager.automaticLoading = false;
                    xrManager.automaticRunning = false;
                    
                    if (removed)
                    {
                        Debug.Log("✅ Oculus Loader removido com sucesso");
                    }
                }
            }
            #endif
            
            Debug.Log("✅ Oculus XR Plugin desabilitado para build do tablet");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ Erro ao desabilitar Oculus: {e.Message}");
        }
    }
}

