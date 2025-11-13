using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using TMPro;

#if UNITY_ANDROID && !UNITY_EDITOR && USING_OCULUS_SDK
using Oculus.VR;
using OVRInput = Oculus.VR.OVRInput;
#endif

public class VRManager : MonoBehaviour {
    [Header("Video Settings")]
    public VideoPlayer videoPlayer;
    [Tooltip("Objeto que contém a esfera do vídeo 360")]
    public Transform videoSphere;
    
    [Header("Networking")]
    public string serverUri = "ws://192.168.4.1:80";
    private ClientWebSocket webSocket;
    [Tooltip("Permanecer offline, não tentar conectar ao servidor")]
    public bool offlineMode = false;
    
    [Header("User Settings")]
    [Tooltip("Identifica se esta build é do usuário 1, 2, 3 ou 4")]
    public int userNumber = 1;
    
    [Header("Language Settings")]
    private Dictionary<string, string> videoLanguageMap = new Dictionary<string, string>();
    
    // Estados
    public bool isPlaying = false;
    private double pausedTime = 0.0;
    private string currentVideo = "";
    private int lastSentPercent = -1;
    
    // Controle de conexão
    private bool isShuttingDown = false;
    private CancellationTokenSource shutdownCts = null;
    private bool isReconnecting = false;
    
    // Menu de configuração (opcional - para compatibilidade com ConfigMenuHelper)
    public GameObject configMenuUI;
    public TMP_InputField ipInputField;
    public Button saveIpButton;
    public Button closeConfigButton;
    
    async void Start() {
        try {
            Debug.Log($"✅ VRManager iniciando - Usuário {userNumber}");
            
            // Validar userNumber
            if (userNumber < 1 || userNumber > 4) {
                Debug.LogWarning($"userNumber inválido ({userNumber}). Definindo como 1.");
                userNumber = 1;
            }
            
            // Inicializar mapeamento de idiomas
            InitializeVideoLanguageMap();
            
            // Preparar VideoPlayer
            if (videoPlayer == null) {
                videoPlayer = FindObjectOfType<VideoPlayer>();
            }
            if (videoPlayer != null) {
                videoPlayer.Prepare();
                videoPlayer.loopPointReached += OnVideoEnd;
            }
            
            // Conectar ao servidor (se não estiver em modo offline)
            if (!offlineMode) {
                try {
                    await ConnectWebSocket();
                } catch (Exception e) {
                    Debug.LogWarning($"⚠️ Erro na conexão inicial: {e.Message}");
                    Debug.LogWarning("⚠️ Aplicação continuará funcionando sem conexão");
                }
            } else {
                Debug.Log("ℹ️ Modo offline ativado - não conectando ao servidor");
            }
        }
        catch (Exception e) {
            Debug.LogError($"❌ Erro durante inicialização: {e.Message}");
        }
    }
    
    void InitializeVideoLanguageMap() {
        videoLanguageMap.Clear();
        videoLanguageMap.Add("pt", "Experiencia_Aegea_Cop2025_Portugues.mp4");
        videoLanguageMap.Add("en", "Experiencia_Aegea_Cop2025_Ingles.mp4");
        videoLanguageMap.Add("es", "Experiencia_Aegea_Cop2025_Espannhol.mp4");
        Debug.Log($"✅ Mapeamento de idiomas inicializado");
    }
    
    async Task ConnectWebSocket() {
        // Limpar conexão anterior se existir
        if (webSocket != null) {
            try {
                if (webSocket.State == WebSocketState.Open) {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconectando", CancellationToken.None);
                }
                webSocket.Dispose();
            } catch { }
            webSocket = null;
        }
        
        webSocket = new ClientWebSocket();
        webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(5);
        
        Debug.Log($"🌐 Conectando ao WebSocket: {serverUri}");
        
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10))) {
            try {
                await webSocket.ConnectAsync(new Uri(serverUri), cts.Token);
                Debug.Log("✅ Conexão WebSocket estabelecida");
                
                // Iniciar recebimento de mensagens
                ReceiveMessages();
                
                // Enviar mensagem de conexão
                await SendMessage($"vr_connected{userNumber}");
                
                // Enviar status inicial da bateria
                await SendBatteryStatus();
                
                // Iniciar envio periódico de bateria (a cada 30 segundos)
                InvokeRepeating(nameof(SendBatteryStatusPeriodic), 30f, 30f);
            }
            catch (OperationCanceledException) {
                Debug.LogWarning("⚠️ Conexão cancelada (timeout)");
                // Não tentar reconectar imediatamente se foi cancelado
            }
            catch (Exception e) {
                Debug.LogWarning($"⚠️ Erro ao conectar: {e.Message}");
                Debug.LogWarning("⚠️ Aplicação continuará funcionando em modo offline");
                
                // Limpar websocket em caso de erro
                if (webSocket != null) {
                    try {
                        webSocket.Dispose();
                    } catch { }
                    webSocket = null;
                }
                
                // Tentar reconectar após 5 segundos (sem bloquear a aplicação)
                if (!isReconnecting && !isShuttingDown && !offlineMode) {
                    StartCoroutine(ReconnectAfterDelay(5f));
                }
            }
        }
    }
    
    async void ReceiveMessages() {
        byte[] buffer = new byte[1024];
        
        while (webSocket != null && webSocket.State == WebSocketState.Open && !isShuttingDown) {
            try {
                CancellationToken ct = shutdownCts != null ? shutdownCts.Token : CancellationToken.None;
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                
                if (result.MessageType == WebSocketMessageType.Close) {
                    Debug.LogWarning("🔌 Servidor solicitou fechamento da conexão");
                    break;
                }
                
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Debug.Log($"📩 Mensagem recebida: {message}");
                
                ProcessMessage(message);
            }
            catch (OperationCanceledException) {
                // Cancelado durante shutdown - sair silenciosamente
                break;
            }
            catch (Exception e) {
                if (webSocket == null || isShuttingDown) break;
                Debug.LogWarning($"⚠️ Erro ao receber mensagem: {e.Message}");
                break;
            }
        }
        
        // Tentar reconectar se não estiver em shutdown (sem bloquear)
        if (!isShuttingDown && !isReconnecting && !offlineMode && this != null && this.isActiveAndEnabled) {
            StartCoroutine(ReconnectAfterDelay(2f));
        }
    }
    
    private IEnumerator ReconnectAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        if (this != null && this.isActiveAndEnabled) {
            ReconnectWebSocket();
        }
    }
    
    void ProcessMessage(string message) {
        message = message.Trim();
        
        // Ignorar pings
        if (message.StartsWith("PING:")) return;
        
        // Comando stop: stop{id}
        if (message.Equals($"stop{userNumber}", StringComparison.OrdinalIgnoreCase)) {
            StopVideo();
            _ = SendMessage($"vr_connected{userNumber}");
            return;
        }
        
        // Comando pause: pause{id}
        if (message.Equals($"pause{userNumber}", StringComparison.OrdinalIgnoreCase)) {
            PauseVideo();
            return;
        }
        
        // Comando resume: resume{id}
        if (message.Equals($"resume{userNumber}", StringComparison.OrdinalIgnoreCase)) {
            ResumeVideo();
            return;
        }
        
        // Comando restart: restart{id}
        if (message.Equals($"restart{userNumber}", StringComparison.OrdinalIgnoreCase)) {
            StopVideo();
            StartCoroutine(RestartVideoAfterDelay(0.1f));
            return;
        }
        
        // Comando play: play{id}_{idioma} (ex: play1_pt, play2_en)
        if (message.StartsWith("play", StringComparison.OrdinalIgnoreCase)) {
            string[] parts = message.Substring(4).Split('_');
            if (parts.Length >= 2) {
                if (int.TryParse(parts[0], out int messageId) && messageId == userNumber) {
                    string language = parts[1].ToLower().Trim();
                    PlayVideoByLanguage(language);
                }
            }
            return;
        }
        
        // Solicitação de bateria: get_battery{id}
        if (message.Equals($"get_battery{userNumber}", StringComparison.OrdinalIgnoreCase)) {
            _ = SendBatteryStatus();
            return;
        }
    }
    
    // ========== CONTROLE DE VÍDEO ==========
    
    public void PlayVideoByLanguage(string language) {
        if (string.IsNullOrEmpty(language)) {
            Debug.LogError("Idioma não especificado!");
            return;
        }
        
        language = language.ToLower().Trim();
        
        if (!videoLanguageMap.ContainsKey(language)) {
            Debug.LogError($"Idioma '{language}' não encontrado!");
            return;
        }
        
        string videoFileName = videoLanguageMap[language];
        Debug.Log($"🎬 Reproduzindo vídeo em {language.ToUpper()}: {videoFileName}");
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(LoadVideoFromStreamingAssets(videoFileName));
        #else
        string videoPath = FindVideoFile(videoFileName);
        if (!string.IsNullOrEmpty(videoPath)) {
            ConfigureAndPlayVideo(videoPath, videoFileName);
        }
        #endif
    }
    
    #if UNITY_ANDROID && !UNITY_EDITOR
    IEnumerator LoadVideoFromStreamingAssets(string fileName) {
        string streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, fileName);
        string persistentPath = Path.Combine(Application.persistentDataPath, fileName);
        
        if (File.Exists(persistentPath)) {
            ConfigureAndPlayVideo("file://" + persistentPath, fileName);
            yield break;
        }
        
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(streamingAssetsPath)) {
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success) {
                try {
                    string directory = Path.GetDirectoryName(persistentPath);
                    if (!Directory.Exists(directory)) {
                        Directory.CreateDirectory(directory);
                    }
                    File.WriteAllBytes(persistentPath, www.downloadHandler.data);
                    ConfigureAndPlayVideo("file://" + persistentPath, fileName);
                } catch (Exception e) {
                    Debug.LogError($"❌ Erro ao salvar vídeo: {e.Message}");
                }
            } else {
                ConfigureAndPlayVideo(streamingAssetsPath, fileName);
            }
        }
    }
    #endif
    
    string FindVideoFile(string fileName) {
        #if UNITY_ANDROID && !UNITY_EDITOR
        return Path.Combine(Application.streamingAssetsPath, fileName);
        #else
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        if (File.Exists(path)) return path;
        return fileName;
        #endif
    }
    
    void ConfigureAndPlayVideo(string videoPath, string videoFileName) {
        if (videoPlayer == null) {
            Debug.LogError("VideoPlayer não encontrado!");
            return;
        }
        
        if (videoPlayer.isPlaying) {
            videoPlayer.Stop();
        }
        
        videoPlayer.url = videoPath;
        currentVideo = videoFileName;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.Prepare();
        
        StartCoroutine(WaitForVideoPrepareAndPlay());
    }
    
    IEnumerator WaitForVideoPrepareAndPlay() {
        float timeout = 10f;
        float elapsed = 0f;
        
        while (!videoPlayer.isPrepared && elapsed < timeout) {
            yield return null;
            elapsed += Time.deltaTime;
        }
        
        if (videoPlayer.isPrepared) {
            videoPlayer.Play();
            isPlaying = true;
            CancelInvoke(nameof(SendTimecode));
            InvokeRepeating(nameof(SendTimecode), 0.5f, 1f);
        } else {
            Debug.LogError($"❌ Timeout ao preparar vídeo: {currentVideo}");
        }
    }
    
    public void PauseVideo() {
        if (videoPlayer != null && videoPlayer.isPlaying) {
            pausedTime = videoPlayer.time;
            videoPlayer.Pause();
            isPlaying = false;
            CancelInvoke(nameof(SendTimecode));
            Debug.Log($"⏸️ Vídeo pausado");
        }
    }
    
    public void ResumeVideo() {
        if (videoPlayer == null) return;
        
        if (!videoPlayer.isPlaying && pausedTime > 0) {
            videoPlayer.time = pausedTime;
            StartCoroutine(ResumeAfterSeek());
        } else if (!videoPlayer.isPlaying) {
            PlayVideoByLanguage(GetLanguageFromVideoName(currentVideo));
        }
    }
    
    private IEnumerator ResumeAfterSeek() {
        yield return null;
        videoPlayer.Play();
        isPlaying = true;
        CancelInvoke(nameof(SendTimecode));
        InvokeRepeating(nameof(SendTimecode), 0.5f, 1f);
        Debug.Log($"▶️ Vídeo retomado");
    }
    
    public void StopVideo() {
        if (videoPlayer != null) {
            videoPlayer.Stop();
            isPlaying = false;
            pausedTime = 0.0;
            lastSentPercent = -1;
            CancelInvoke(nameof(SendTimecode));
            Debug.Log($"⏹️ Vídeo parado");
        }
    }
    
    private IEnumerator RestartVideoAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        if (!string.IsNullOrEmpty(currentVideo)) {
            string language = GetLanguageFromVideoName(currentVideo);
            PlayVideoByLanguage(language);
        }
    }
    
    private string GetLanguageFromVideoName(string videoName) {
        if (videoName.Contains("Portugues") || videoName.Contains("_pt")) return "pt";
        if (videoName.Contains("Ingles") || videoName.Contains("_en")) return "en";
        if (videoName.Contains("Espanhol") || videoName.Contains("_es")) return "es";
        return "pt";
    }
    
    void OnVideoEnd(VideoPlayer vp) {
        Debug.Log("🎬 Vídeo concluído");
        _ = SendMessage($"video_ended{userNumber}");
        StopVideo();
    }
    
    // ========== TIMECODE ==========
    
    void SendTimecode() {
        if (isShuttingDown || videoPlayer == null || !videoPlayer.isPlaying) return;
        
        float currentTime = (float)videoPlayer.time;
        float videoDuration = (float)videoPlayer.length;
        
        if (videoDuration <= 0) return;
        
        int percent = Mathf.RoundToInt((currentTime / videoDuration) * 100f);
        if (percent > 100) percent = 100;
        
        if (percent == 0 && lastSentPercent == -1) {
            percent = 1;
        }
        
        if (lastSentPercent == -1 || Mathf.Abs(percent - lastSentPercent) >= 1) {
            lastSentPercent = percent;
            string message = $"percent{userNumber}:" + percent.ToString();
            _ = SendMessage(message);
        }
    }
    
    // ========== BATERIA ==========
    
    float GetBatteryLevel() {
        float batteryLevel = -1f;
        
        #if UNITY_ANDROID && !UNITY_EDITOR && USING_OCULUS_SDK
        // Método 1: Tentar usar OVRPlugin (método nativo do Oculus)
        try {
            float oculusBattery = OVRPlugin.GetSystemBatteryLevel();
            if (oculusBattery >= 0f && oculusBattery <= 1f) {
                batteryLevel = oculusBattery * 100f;
                Debug.Log($"🔋 Bateria via OVRPlugin: {batteryLevel:F1}%");
                return batteryLevel;
            } else {
                Debug.LogWarning($"⚠️ Valor inválido do OVRPlugin: {oculusBattery}");
            }
        } catch (Exception e) {
            Debug.LogWarning($"⚠️ Erro ao usar OVRPlugin.GetSystemBatteryLevel(): {e.Message}");
        }
        
        // Método 2: Tentar usar OVRPlugin com método alternativo
        try {
            // Tentar obter via SystemInfo se disponível
            if (SystemInfo.batteryLevel >= 0f) {
                batteryLevel = SystemInfo.batteryLevel * 100f;
                Debug.Log($"🔋 Bateria via SystemInfo: {batteryLevel:F1}%");
                if (batteryLevel >= 0f && batteryLevel <= 100f) {
                    return batteryLevel;
                }
            }
        } catch (Exception e) {
            Debug.LogWarning($"⚠️ Erro ao usar SystemInfo.batteryLevel: {e.Message}");
        }
        #endif
        
        // Método 3: Fallback para Android API nativa
        batteryLevel = GetBatteryLevelAndroid();
        if (batteryLevel >= 0f && batteryLevel <= 100f) {
            return batteryLevel;
        }
        
        // Se nenhum método funcionou, retornar valor padrão
        Debug.LogWarning($"⚠️ Não foi possível obter nível da bateria, usando fallback");
        return 0f;
    }
    
    float GetBatteryLevelAndroid() {
        #if UNITY_ANDROID && !UNITY_EDITOR
        try {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject intentFilter = new AndroidJavaObject("android.content.IntentFilter", "android.intent.action.BATTERY_CHANGED"))
            using (AndroidJavaObject batteryStatus = activity.Call<AndroidJavaObject>("registerReceiver", null, intentFilter)) {
                if (batteryStatus != null) {
                    int level = batteryStatus.Call<int>("getIntExtra", "level", -1);
                    int scale = batteryStatus.Call<int>("getIntExtra", "scale", -1);
                    
                    if (level >= 0 && scale > 0) {
                        float batteryPercent = (level * 100f) / scale;
                        Debug.Log($"🔋 Bateria via Android API: {batteryPercent:F1}% (level={level}, scale={scale})");
                        
                        // Validar valor
                        if (batteryPercent >= 0f && batteryPercent <= 100f) {
                            return batteryPercent;
                        } else {
                            Debug.LogWarning($"⚠️ Valor de bateria inválido: {batteryPercent}%");
                        }
                    } else {
                        Debug.LogWarning($"⚠️ Valores inválidos: level={level}, scale={scale}");
                    }
                } else {
                    Debug.LogWarning("⚠️ batteryStatus é null");
                }
            }
        } catch (Exception e) {
            Debug.LogError($"❌ Erro ao obter bateria via Android API: {e.Message}");
            Debug.LogError($"❌ StackTrace: {e.StackTrace}");
        }
        #endif
        return -1f; // Retornar -1 para indicar falha
    }
    
    async Task SendBatteryStatus() {
        try {
            if (isShuttingDown || offlineMode) return;
            
            if (webSocket != null && webSocket.State == WebSocketState.Open) {
                float batteryLevel = GetBatteryLevel();
                
                // Validar valor antes de enviar
                if (batteryLevel < 0f || batteryLevel > 100f) {
                    Debug.LogWarning($"⚠️ Valor de bateria inválido: {batteryLevel:F1}% - não enviando");
                    return;
                }
                
                string message = $"battery{userNumber}:{batteryLevel:F1}";
                await SendMessage(message);
                Debug.Log($"🔋 [User {userNumber}] Bateria enviada: {batteryLevel:F1}%");
            }
        } catch (Exception e) {
            Debug.LogError($"❌ Erro ao enviar status da bateria: {e.Message}");
            Debug.LogError($"❌ StackTrace: {e.StackTrace}");
        }
    }
    
    async void SendBatteryStatusPeriodic() {
        await SendBatteryStatus();
    }
    
    // ========== COMUNICAÇÃO ==========
    
    public async Task SendMessage(string message) {
        try {
            if (isShuttingDown || offlineMode) return;
            
            if (webSocket != null && webSocket.State == WebSocketState.Open) {
                byte[] data = Encoding.UTF8.GetBytes(message);
                CancellationToken ct = shutdownCts != null ? shutdownCts.Token : CancellationToken.None;
                await webSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, ct);
            } else {
                // Conexão não disponível - silenciosamente ignorar (não é erro crítico)
                if (webSocket == null || webSocket.State != WebSocketState.Open) {
                    // Tentar reconectar em background se não estiver já tentando
                    if (!isReconnecting && !isShuttingDown) {
                        StartCoroutine(ReconnectAfterDelay(5f));
                    }
                }
            }
        } catch (OperationCanceledException) {
            // Ignorar durante shutdown
        } catch (Exception e) {
            // Log apenas se for um erro inesperado (não apenas falta de conexão)
            if (webSocket != null && webSocket.State == WebSocketState.Open) {
                Debug.LogWarning($"⚠️ Erro ao enviar mensagem: {e.Message}");
            }
        }
    }
    
    async void ReconnectWebSocket() {
        if (isShuttingDown || isReconnecting || offlineMode) return;
        
        isReconnecting = true;
        
        // Limpar conexão anterior
        if (webSocket != null) {
            try {
                if (webSocket.State == WebSocketState.Open) {
                    CancellationTokenSource cts = new CancellationTokenSource(1000);
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconectando", cts.Token);
                }
                webSocket.Dispose();
            } catch { }
            webSocket = null;
        }
        
        Debug.Log("🔄 Tentando reconectar...");
        
        try {
            CancellationToken ct = shutdownCts != null ? shutdownCts.Token : CancellationToken.None;
            await Task.Delay(3000, ct);
        } catch (OperationCanceledException) {
            isReconnecting = false;
            return;
        }
        
        // Tentar conectar novamente (sem bloquear se falhar)
        try {
            await ConnectWebSocket();
        } catch (Exception e) {
            Debug.LogWarning($"⚠️ Erro na reconexão: {e.Message}");
            Debug.LogWarning("⚠️ Aplicação continuará funcionando sem conexão");
        }
        
        isReconnecting = false;
    }
    
    // ========== MÉTODOS DE TESTE ==========
    
    public void TestConnection() {
        Debug.Log("Teste de conexão manual");
        offlineMode = false;
        ReconnectWebSocket();
    }
    
    public void TestPlayVideo(string videoName = "") {
        if (string.IsNullOrEmpty(videoName)) {
            // Tentar usar primeiro idioma disponível
            PlayVideoByLanguage("pt");
        } else {
            // Tentar detectar idioma do nome do arquivo
            string language = GetLanguageFromVideoName(videoName);
            PlayVideoByLanguage(language);
        }
    }
    
    // ========== CLEANUP ==========
    
    void OnApplicationQuit() {
        isShuttingDown = true;
        
        if (shutdownCts == null) {
            shutdownCts = new CancellationTokenSource();
        }
        shutdownCts.Cancel();
        
        CancelInvoke();
        StopAllCoroutines();
        
        if (webSocket != null) {
            try {
                webSocket.Abort();
            } catch { }
            finally {
                try {
                    webSocket.Dispose();
                } catch { }
                webSocket = null;
            }
        }
        
        if (shutdownCts != null) {
            try {
                shutdownCts.Dispose();
            } catch { }
            shutdownCts = null;
        }
    }
    
    void OnDestroy() {
        try {
            isShuttingDown = true;
            
            if (shutdownCts != null && !shutdownCts.IsCancellationRequested) {
                shutdownCts.Cancel();
            }
            
            CancelInvoke();
            StopAllCoroutines();
            
            if (webSocket != null) {
                try {
                    webSocket.Abort();
                } catch { }
                finally {
                    try {
                        webSocket.Dispose();
                    } catch { }
                    webSocket = null;
                }
            }
            
            if (shutdownCts != null) {
                try {
                    shutdownCts.Dispose();
                } catch { }
                shutdownCts = null;
            }
        } catch { }
    }
}