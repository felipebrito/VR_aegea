using UnityEngine;
using System;

/// <summary>
/// Script para garantir que a aplicação inicie corretamente no tablet
/// Desabilita componentes do Oculus que podem causar crash
/// IMPORTANTE: Este script deve ter prioridade de execução alta (Script Execution Order)
/// </summary>
public class TabletStartupManager : MonoBehaviour
{
    void Awake()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            Debug.Log("🚀 TabletStartupManager.Awake() - Iniciando...");
            
            // PRIMEIRO: Desabilitar XR que pode causar crash
            DisableXR();
            
            // Desabilitar componentes do Oculus que podem causar crash no tablet
            DisableOculusComponents();
            
            // Log informações do sistema
            LogSystemInfo();
            
            Debug.Log("✅ TabletStartupManager.Awake() - Concluído");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ ERRO CRÍTICO em TabletStartupManager.Awake(): {e.Message}");
            Debug.LogError($"❌ StackTrace: {e.StackTrace}");
            // Continuar mesmo com erro
        }
        #endif
    }
    
    void DisableXR()
    {
        try
        {
            #if USING_XR_MANAGEMENT && UNITY_ANDROID && !UNITY_EDITOR
            // Desabilitar XR Manager ANTES que ele tente inicializar
            var xrSettings = UnityEngine.XR.Management.XRGeneralSettings.Instance;
            if (xrSettings != null)
            {
                var xrManager = xrSettings.Manager;
                if (xrManager != null)
                {
                    Debug.Log("⚠️ Desabilitando XR Manager no tablet...");
                    xrManager.enabled = false;
                    xrManager.automaticLoading = false;
                    xrManager.automaticRunning = false;
                }
            }
            #endif
            
            // Desabilitar Meta XR Audio que está causando problemas
            DisableMetaXRAudio();
            
            // Desabilitar qualquer inicialização de XR
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Debug.Log("✅ XR desabilitado");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ Erro ao desabilitar XR: {e.Message}");
        }
    }
    
    void DisableMetaXRAudio()
    {
        try
        {
            // Desabilitar todos os componentes Meta XR Audio
            var allObjects = FindObjectsOfType<GameObject>(true);
            foreach (var obj in allObjects)
            {
                var components = obj.GetComponents<MonoBehaviour>();
                foreach (var comp in components)
                {
                    if (comp != null)
                    {
                        string typeName = comp.GetType().Name;
                        if (typeName.Contains("MetaXR") || 
                            typeName.Contains("MetaXRAudio") || 
                            typeName.Contains("MetaXRAcoustic"))
                        {
                            Debug.Log($"⚠️ Desabilitando componente Meta XR: {typeName} em {obj.name}");
                            comp.enabled = false;
                        }
                    }
                }
                
                // Desabilitar objetos Meta XR
                if (obj.name.Contains("MetaXR") || obj.name.Contains("MetaXRAudio"))
                {
                    Debug.Log($"⚠️ Desabilitando GameObject Meta XR: {obj.name}");
                    obj.SetActive(false);
                }
            }
            
            Debug.Log("✅ Componentes Meta XR Audio desabilitados");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ Erro ao desabilitar Meta XR Audio: {e.Message}");
        }
    }
    
    void Start()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log("✅ TabletStartupManager.Start() - App iniciado com sucesso!");
        #endif
    }
    
    void DisableOculusComponents()
    {
        try
        {
            // Desabilitar OVRCameraRig se existir
            var ovrCameraRigs = FindObjectsOfType<MonoBehaviour>();
            foreach (var obj in ovrCameraRigs)
            {
                string typeName = obj.GetType().Name;
                if (typeName.Contains("OVR") || typeName.Contains("Oculus"))
                {
                    Debug.Log($"⚠️ Desabilitando componente Oculus: {typeName}");
                    obj.enabled = false;
                }
            }
            
            // Desabilitar scripts específicos do Oculus
            var allObjects = FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                // Desabilitar objetos com "OVR" no nome
                if (obj.name.Contains("OVR") || obj.name.Contains("Oculus"))
                {
                    Debug.Log($"⚠️ Desabilitando GameObject Oculus: {obj.name}");
                    obj.SetActive(false);
                }
            }
            
            Debug.Log("✅ Componentes do Oculus desabilitados");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ Erro ao desabilitar componentes Oculus: {e.Message}");
        }
    }
    
    void LogSystemInfo()
    {
        Debug.Log("==========================================");
        Debug.Log($"📱 Device: {SystemInfo.deviceModel}");
        Debug.Log($"📱 OS: {SystemInfo.operatingSystem}");
        Debug.Log($"📱 Unity: {Application.unityVersion}");
        Debug.Log($"📁 PersistentDataPath: {Application.persistentDataPath}");
        Debug.Log($"📁 StreamingAssetsPath: {Application.streamingAssetsPath}");
        Debug.Log($"💾 Memory: {SystemInfo.systemMemorySize}MB");
        Debug.Log($"🎮 Graphics: {SystemInfo.graphicsDeviceName}");
        Debug.Log("==========================================");
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

