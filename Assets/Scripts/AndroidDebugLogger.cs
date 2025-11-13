using UnityEngine;
using System.IO;
using System;

/// <summary>
/// Sistema de logging para Android - salva logs em arquivo para debug
/// </summary>
public class AndroidDebugLogger : MonoBehaviour
{
    private static AndroidDebugLogger instance;
    private string logFilePath;
    private StreamWriter logWriter;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLogger();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeLogger()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // Criar arquivo de log no armazenamento interno
            string logDir = Path.Combine(Application.persistentDataPath, "Logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            logFilePath = Path.Combine(logDir, $"debug_{timestamp}.txt");
            
            logWriter = new StreamWriter(logFilePath, true);
            logWriter.AutoFlush = true;
            
            Log("==========================================");
            Log($"🚀 Aplicação iniciada - {DateTime.Now}");
            Log($"📱 Device: {SystemInfo.deviceModel}");
            Log($"📱 OS: {SystemInfo.operatingSystem}");
            Log($"📱 Unity: {Application.unityVersion}");
            Log($"📁 PersistentDataPath: {Application.persistentDataPath}");
            Log($"📁 StreamingAssetsPath: {Application.streamingAssetsPath}");
            Log("==========================================");
            
            // Redirecionar Debug.Log para arquivo também
            Application.logMessageReceived += HandleLog;
            
            Debug.Log("✅ AndroidDebugLogger inicializado");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Erro ao inicializar logger: {e.Message}");
        }
        #endif
    }
    
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (logWriter != null)
        {
            string logEntry = $"[{DateTime.Now:HH:mm:ss.fff}] [{type}] {logString}";
            if (type == LogType.Error || type == LogType.Exception)
            {
                logEntry += $"\n{stackTrace}";
            }
            logWriter.WriteLine(logEntry);
        }
    }
    
    public static void Log(string message)
    {
        Debug.Log(message);
        if (instance != null && instance.logWriter != null)
        {
            instance.logWriter.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }
    }
    
    public static void LogError(string message)
    {
        Debug.LogError(message);
        if (instance != null && instance.logWriter != null)
        {
            instance.logWriter.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [ERROR] {message}");
        }
    }
    
    public static void LogWarning(string message)
    {
        Debug.LogWarning(message);
        if (instance != null && instance.logWriter != null)
        {
            instance.logWriter.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [WARNING] {message}");
        }
    }
    
    void OnDestroy()
    {
        if (logWriter != null)
        {
            Log("==========================================");
            Log($"🛑 Aplicação finalizada - {DateTime.Now}");
            Log("==========================================");
            logWriter.Close();
            logWriter = null;
        }
        
        Application.logMessageReceived -= HandleLog;
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        Log($"📱 Aplicação {(pauseStatus ? "pausada" : "retomada")}");
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        Log($"📱 Aplicação {(hasFocus ? "em foco" : "perdeu foco")}");
    }
}

