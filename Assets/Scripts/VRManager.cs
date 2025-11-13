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
using System.Net;
using System.Net.Sockets;
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
    
    // Simulação de bateria no Editor
    private float simulatedBatteryLevel = 75f;
    private float batteryOscillationDirection = 1f;
    private float lastBatteryUpdateTime = 0f;
    
    // Controle de conexão
    private bool isShuttingDown = false;
    private CancellationTokenSource shutdownCts = null;
    private bool isReconnecting = false;
    private bool isReceivingMessages = false;
    private readonly object webSocketLock = new object();
    
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
        // Prevenir múltiplas conexões simultâneas
        lock (webSocketLock) {
            if (isReconnecting) {
                Debug.LogWarning("⚠️ Já está reconectando, ignorando nova tentativa");
                return;
            }
        }
        
        // Limpar conexão anterior se existir
        lock (webSocketLock) {
            if (webSocket != null) {
                try {
                    if (webSocket.State == WebSocketState.Open) {
                        webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconectando", CancellationToken.None).Wait(1000);
                    }
                    webSocket.Dispose();
                } catch { }
                webSocket = null;
            }
            
            // Cancelar InvokeRepeating anterior se existir
            CancelInvoke(nameof(SendClientInfoPeriodic));
        }
        
        ClientWebSocket newWebSocket = new ClientWebSocket();
        newWebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(5);
        
        Debug.Log($"🌐 Conectando ao WebSocket: {serverUri}");
        
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10))) {
            try {
                await newWebSocket.ConnectAsync(new Uri(serverUri), cts.Token);
                Debug.Log("✅ Conexão WebSocket estabelecida");
                
                // Atualizar webSocket dentro do lock
                lock (webSocketLock) {
                    webSocket = newWebSocket;
                }
                
                // Iniciar recebimento de mensagens (apenas se não estiver já recebendo)
                if (!isReceivingMessages) {
                    isReceivingMessages = true;
                    ReceiveMessages();
                }
                
                // Enviar mensagem de conexão
                await SendMessage($"vr_connected{userNumber}");
                Debug.Log($"✅ Mensagem vr_connected{userNumber} enviada");
                
                // Aguardar um pouco para garantir que a conexão está estável
                await Task.Delay(500);
                
                // Enviar informações do cliente (inclui bateria)
                Debug.Log("🔋 Tentando enviar CLIENT_INFO...");
                await SendClientInfo();
                
                // Iniciar envio periódico de informações (a cada 30 segundos)
                // Cancelar anterior antes de criar novo
                CancelInvoke(nameof(SendClientInfoPeriodic));
                InvokeRepeating(nameof(SendClientInfoPeriodic), 30f, 30f);
                Debug.Log("✅ Envio periódico de CLIENT_INFO configurado (30s)");
            }
            catch (OperationCanceledException) {
                Debug.LogWarning("⚠️ Conexão cancelada (timeout)");
                // Limpar websocket em caso de erro
                try {
                    newWebSocket?.Dispose();
                } catch { }
                // Não tentar reconectar imediatamente se foi cancelado
            }
            catch (Exception e) {
                Debug.LogWarning($"⚠️ Erro ao conectar: {e.Message}");
                Debug.LogWarning("⚠️ Aplicação continuará funcionando em modo offline");
                
                // Limpar websocket em caso de erro
                try {
                    newWebSocket?.Dispose();
                } catch { }
                
                lock (webSocketLock) {
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
        ClientWebSocket currentWebSocket;
        
        // Obter referência ao webSocket dentro do lock
        lock (webSocketLock) {
            currentWebSocket = webSocket;
        }
        
        while (currentWebSocket != null && !isShuttingDown) {
            try {
                // Verificar estado dentro do loop
                lock (webSocketLock) {
                    currentWebSocket = webSocket;
                    if (currentWebSocket == null || currentWebSocket.State != WebSocketState.Open) {
                        break;
                    }
                }
                
                CancellationToken ct = shutdownCts != null ? shutdownCts.Token : CancellationToken.None;
                WebSocketReceiveResult result = await currentWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                
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
                lock (webSocketLock) {
                    currentWebSocket = webSocket;
                }
                if (currentWebSocket == null || isShuttingDown) break;
                Debug.LogWarning($"⚠️ Erro ao receber mensagem: {e.Message}");
                break;
            }
        }
        
        // Marcar que não está mais recebendo mensagens
        isReceivingMessages = false;
        
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
            Debug.Log($"🔋 Solicitação de bateria recebida: {message}");
            _ = SendClientInfo(); // Enviar CLIENT_INFO em vez de apenas bateria
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
        Debug.Log($"🔋 GetBatteryLevel() chamado - Platform: {Application.platform}, Editor: {Application.isEditor}");
        
        #if UNITY_EDITOR
        // No Editor, retornar valor simulado que oscila para testes
        float currentTime = Time.time;
        
        // Atualizar a cada 2 segundos
        if (currentTime - lastBatteryUpdateTime >= 2f) {
            lastBatteryUpdateTime = currentTime;
            
            // Oscilar entre 60% e 95%
            simulatedBatteryLevel += batteryOscillationDirection * 5f;
            
            if (simulatedBatteryLevel >= 95f) {
                simulatedBatteryLevel = 95f;
                batteryOscillationDirection = -1f; // Começar a descer
            } else if (simulatedBatteryLevel <= 60f) {
                simulatedBatteryLevel = 60f;
                batteryOscillationDirection = 1f; // Começar a subir
            }
        }
        
        Debug.Log($"🔋 Editor detectado - retornando valor simulado oscilante: {simulatedBatteryLevel:F1}%");
        return simulatedBatteryLevel;
        #elif UNITY_ANDROID
        Debug.Log("🔋 Entrando no bloco UNITY_ANDROID");
        
        // Método 1: Tentar SystemInfo primeiro (mais simples e confiável no Quest)
        try {
            Debug.Log("🔋 Tentando SystemInfo.batteryLevel...");
            float systemBattery = SystemInfo.batteryLevel;
            Debug.Log($"🔋 SystemInfo.batteryLevel retornou: {systemBattery}");
            
            // SystemInfo.batteryLevel retorna valor entre 0.0 e 1.0, ou -1 se não disponível
            if (systemBattery >= 0f && systemBattery <= 1f) {
                float batteryPercent = systemBattery * 100f;
                Debug.Log($"🔋 Bateria via SystemInfo: {batteryPercent:F1}%");
                if (batteryPercent >= 0f && batteryPercent <= 100f) {
                    return batteryPercent;
                }
            } else if (systemBattery > 1f && systemBattery <= 100f) {
                // Pode retornar já em percentual em algumas versões
                Debug.Log($"🔋 SystemInfo retornou percentual direto: {systemBattery:F1}%");
                return systemBattery;
            } else if (systemBattery == -1f) {
                Debug.LogWarning("⚠️ SystemInfo.batteryLevel retornou -1 (não disponível)");
            }
        } catch (Exception e) {
            Debug.LogWarning($"⚠️ Erro ao usar SystemInfo.batteryLevel: {e.Message}");
        }
        
        #if USING_OCULUS_SDK
        // Método 2: Tentar usar OVRPlugin (método nativo do Oculus)
        try {
            float oculusBattery = OVRPlugin.GetSystemBatteryLevel();
            Debug.Log($"🔋 OVRPlugin.GetSystemBatteryLevel() retornou: {oculusBattery}");
            
            // OVRPlugin retorna valor entre 0.0 e 1.0
            if (oculusBattery >= 0f && oculusBattery <= 1f) {
                float batteryPercent = oculusBattery * 100f;
                Debug.Log($"🔋 Bateria via OVRPlugin: {batteryPercent:F1}%");
                if (batteryPercent >= 0f && batteryPercent <= 100f) {
                    return batteryPercent;
                }
            } else {
                Debug.LogWarning($"⚠️ Valor inválido do OVRPlugin: {oculusBattery}");
            }
        } catch (Exception e) {
            Debug.LogWarning($"⚠️ Erro ao usar OVRPlugin.GetSystemBatteryLevel(): {e.Message}");
        }
        #endif
        
        // Método 3: Fallback para Android API nativa (baseado em exemplos online)
        try {
            Debug.Log("🔋 Tentando obter bateria via Android API nativa...");
            
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            
            if (currentActivity == null) {
                Debug.LogError("❌ currentActivity é null!");
                return -1f;
            }
            
            AndroidJavaObject intentFilter = new AndroidJavaObject("android.content.IntentFilter", "android.intent.action.BATTERY_CHANGED");
            AndroidJavaObject batteryIntent = currentActivity.Call<AndroidJavaObject>("registerReceiver", null, intentFilter);
            
            if (batteryIntent == null) {
                Debug.LogError("❌ batteryIntent é null após registerReceiver!");
                return -1f;
            }
            
            int level = batteryIntent.Call<int>("getIntExtra", "level", -1);
            int scale = batteryIntent.Call<int>("getIntExtra", "scale", -1);
            
            Debug.Log($"🔋 Android API - level: {level}, scale: {scale}");
            
            if (level == -1 || scale == -1 || scale == 0) {
                Debug.LogWarning($"⚠️ Valores inválidos: level={level}, scale={scale}");
                return -1f;
            }
            
            float batteryPercent = ((float)level / (float)scale) * 100f;
            Debug.Log($"🔋 Bateria obtida via Android API: {batteryPercent:F1}%");
            
            // Validar resultado
            if (batteryPercent >= 0f && batteryPercent <= 100f) {
                return batteryPercent;
            } else {
                Debug.LogWarning($"⚠️ Valor de bateria fora do range: {batteryPercent:F1}%");
                return -1f;
            }
        } catch (Exception e) {
            Debug.LogError($"❌ Erro ao obter bateria via Android API: {e.Message}");
            Debug.LogError($"❌ StackTrace: {e.StackTrace}");
            return -1f;
        }
        
        #elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        Debug.Log("🔋 Entrando no bloco UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN");
        float battery = SystemInfo.batteryLevel * 100f;
        Debug.Log($"🔋 Bateria (Editor/Windows): {battery:F1}%");
        return battery;
        #else
        Debug.LogWarning($"⚠️ Plataforma não suportada para leitura de bateria - Platform: {Application.platform}");
        return -1f;
        #endif
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
    
    // ========== CLIENT INFO ==========
    
    string GetLocalIPAddress() {
        try {
            string hostName = Dns.GetHostName();
            IPHostEntry hostEntry = Dns.GetHostEntry(hostName);
            
            foreach (IPAddress ip in hostEntry.AddressList) {
                if (ip.AddressFamily == AddressFamily.InterNetwork && 
                    !IPAddress.IsLoopback(ip)) {
                    return ip.ToString();
                }
            }
        } catch (Exception e) {
            Debug.LogWarning($"⚠️ Erro ao obter IP local: {e.Message}");
        }
        return "0.0.0.0";
    }
    
    async Task SendClientInfo() {
        try {
            if (isShuttingDown) {
                Debug.LogWarning("⚠️ SendClientInfo: isShuttingDown = true");
                return;
            }
            
            if (offlineMode) {
                Debug.LogWarning("⚠️ SendClientInfo: offlineMode = true");
                return;
            }
            
            ClientWebSocket currentWebSocket;
            lock (webSocketLock) {
                currentWebSocket = webSocket;
            }
            
            if (currentWebSocket == null) {
                Debug.LogWarning("⚠️ SendClientInfo: webSocket é null");
                return;
            }
            
            if (currentWebSocket.State != WebSocketState.Open) {
                Debug.LogWarning($"⚠️ SendClientInfo: webSocket.State = {currentWebSocket.State}");
                return;
            }
            
            string clientName = SystemInfo.deviceName;
            string clientIP = GetLocalIPAddress();
            string clientOS = SystemInfo.operatingSystem;
            float batteryFloat = GetBatteryLevel();
            int batteryLevel = (int)batteryFloat;
            
            Debug.Log($"🔋 SendClientInfo: GetBatteryLevel() retornou {batteryFloat:F1}% (int: {batteryLevel}%)");
            
            // Validar valor de bateria antes de enviar
            if (batteryLevel < 0 || batteryLevel > 100) {
                Debug.LogWarning($"⚠️ Valor de bateria inválido: {batteryLevel}% - não enviando CLIENT_INFO");
                return;
            }
            
            string infoMessage = $"CLIENT_INFO:{clientName}|{clientIP}|{clientOS}|{batteryLevel}%";
            
            byte[] data = Encoding.UTF8.GetBytes(infoMessage);
            CancellationToken ct = shutdownCts != null ? shutdownCts.Token : CancellationToken.None;
            await currentWebSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, ct);
            
            Debug.Log($"✅ CLIENT_INFO enviado com sucesso: {infoMessage}");
            Debug.Log($"✅ Tamanho da mensagem: {data.Length} bytes");
        } catch (Exception e) {
            Debug.LogError($"❌ Erro ao enviar informações do cliente: {e.Message}");
            Debug.LogError($"❌ StackTrace: {e.StackTrace}");
        }
    }
    
    async void SendClientInfoPeriodic() {
        await SendClientInfo();
    }
    
    // ========== COMUNICAÇÃO ==========
    
    public async Task SendMessage(string message) {
        try {
            if (isShuttingDown || offlineMode) return;
            
            ClientWebSocket currentWebSocket;
            lock (webSocketLock) {
                currentWebSocket = webSocket;
            }
            
            if (currentWebSocket != null && currentWebSocket.State == WebSocketState.Open) {
                byte[] data = Encoding.UTF8.GetBytes(message);
                CancellationToken ct = shutdownCts != null ? shutdownCts.Token : CancellationToken.None;
                await currentWebSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, ct);
            } else {
                // Conexão não disponível - silenciosamente ignorar (não é erro crítico)
                bool shouldReconnect = false;
                lock (webSocketLock) {
                    if (currentWebSocket == null || currentWebSocket.State != WebSocketState.Open) {
                        shouldReconnect = !isReconnecting && !isShuttingDown;
                    }
                }
                
                // Tentar reconectar em background se não estiver já tentando
                if (shouldReconnect) {
                    StartCoroutine(ReconnectAfterDelay(5f));
                }
            }
        } catch (OperationCanceledException) {
            // Ignorar durante shutdown
        } catch (Exception e) {
            // Log apenas se for um erro inesperado (não apenas falta de conexão)
            ClientWebSocket currentWebSocket;
            lock (webSocketLock) {
                currentWebSocket = webSocket;
            }
            if (currentWebSocket != null && currentWebSocket.State == WebSocketState.Open) {
                Debug.LogWarning($"⚠️ Erro ao enviar mensagem: {e.Message}");
            }
        }
    }
    
    async void ReconnectWebSocket() {
        // Prevenir múltiplas reconexões simultâneas
        lock (webSocketLock) {
            if (isShuttingDown || isReconnecting || offlineMode) return;
            isReconnecting = true;
        }
        
        // Limpar conexão anterior
        lock (webSocketLock) {
            if (webSocket != null) {
                try {
                    if (webSocket.State == WebSocketState.Open) {
                        CancellationTokenSource cts = new CancellationTokenSource(1000);
                        webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconectando", cts.Token).Wait(1000);
                    }
                    webSocket.Dispose();
                } catch { }
                webSocket = null;
            }
            
            // Cancelar InvokeRepeating
            CancelInvoke(nameof(SendClientInfoPeriodic));
        }
        
        Debug.Log("🔄 Tentando reconectar...");
        
        try {
            CancellationToken ct = shutdownCts != null ? shutdownCts.Token : CancellationToken.None;
            await Task.Delay(3000, ct);
        } catch (OperationCanceledException) {
            lock (webSocketLock) {
                isReconnecting = false;
            }
            return;
        }
        
        // Tentar conectar novamente (sem bloquear se falhar)
        try {
            await ConnectWebSocket();
        } catch (Exception e) {
            Debug.LogWarning($"⚠️ Erro na reconexão: {e.Message}");
            Debug.LogWarning("⚠️ Aplicação continuará funcionando sem conexão");
        }
        
        lock (webSocketLock) {
            isReconnecting = false;
        }
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
        
        // Limpar webSocket de forma thread-safe
        lock (webSocketLock) {
            if (webSocket != null) {
                try {
                    webSocket.Abort();
                } catch { }
                try {
                    webSocket.Dispose();
                } catch { }
                webSocket = null;
            }
            isReconnecting = false;
            isReceivingMessages = false;
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
            
            // Limpar webSocket de forma thread-safe
            lock (webSocketLock) {
                if (webSocket != null) {
                    try {
                        webSocket.Abort();
                    } catch { }
                    try {
                        webSocket.Dispose();
                    } catch { }
                    webSocket = null;
                }
                isReconnecting = false;
                isReceivingMessages = false;
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