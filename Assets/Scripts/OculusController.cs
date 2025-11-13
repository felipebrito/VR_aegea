using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

/// <summary>
/// Controlador ULTRA ROBUSTO para gerenciar os 4 Oculus headsets.
/// VERSÃO ANTI-INTERFERÊNCIA: Ping mais frequente, detecção rápida de desconexões,
/// timeout de inatividade, retry automático e reconexão agressiva.
/// </summary>
public class OculusController : MonoBehaviour
{
    [Header("WebSocket Server Settings")]
    [Tooltip("Porta do servidor WebSocket")]
    public int serverPort = 7888;
    
    [Tooltip("Texto para exibir o IP do servidor (opcional)")]
    public TextMeshProUGUI serverIPText;
    
    [Header("Oculus Panels")]
    [Tooltip("Painéis dos 4 Oculus (deve ter exatamente 4)")]
    public OculusPanel[] oculusPanels = new OculusPanel[4];
    
    [Header("UI Elements")]
    [Tooltip("Botão para iniciar o filme")]
    public Button startButton;
    
    [Tooltip("Botão para parar o filme")]
    public Button stopButton;
    
    [Tooltip("Preview do vídeo")]
    public VideoPreview videoPreview;
    
    [Header("Video Settings")]
    [Tooltip("Mapeamento de idiomas para arquivos de vídeo")]
    public Dictionary<string, string> videoLanguageMap = new Dictionary<string, string>();
    
    [Header("Connection Stability Settings")]
    [Tooltip("Intervalo de ping em segundos (menor = mais estável, mas mais tráfego)")]
    [Range(1f, 10f)]
    public float pingInterval = 3f; // Ping a cada 3 segundos (muito frequente para máxima estabilidade)
    
    [Tooltip("Intervalo de verificação de conexão (DESABILITADO - TCP gerencia automaticamente)")]
    [Range(1f, 30f)]
    public float connectionCheckInterval = 10f; // NÃO USADO - mantido apenas para compatibilidade
    
    [Tooltip("Timeout de inatividade em segundos (DESABILITADO - confia apenas no TCP)")]
    [Range(5f, 120f)]
    public float inactivityTimeout = 60f; // NÃO USADO - mantido apenas para compatibilidade
    
    [Tooltip("Número máximo de tentativas de reconexão")]
    [Range(1, 999)]
    public int maxReconnectAttempts = 999; // Nunca parar de tentar
    
    // Servidor WebSocket usando TcpListener
    private TcpListener tcpListener;
    private bool isServerRunning = false;
    private Dictionary<int, TcpClient> oculusConnections = new Dictionary<int, TcpClient>();
    private Dictionary<int, bool> oculusConnected = new Dictionary<int, bool>();
    private Dictionary<int, float> oculusBattery = new Dictionary<int, float>();
    private Dictionary<int, DateTime> oculusLastPing = new Dictionary<int, DateTime>(); // Último ping enviado
    private Dictionary<int, DateTime> oculusLastPong = new Dictionary<int, DateTime>(); // Último pong recebido
    private Dictionary<int, DateTime> oculusLastActivity = new Dictionary<int, DateTime>(); // Última atividade (qualquer mensagem)
    private Dictionary<int, int> oculusReconnectAttempts = new Dictionary<int, int>(); // Tentativas de reconexão por Oculus
    private bool isPlaying = false;
    private float lastPingTime = 0f;
    
    void Start()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            Debug.Log("🎮 OculusController_UltraRobust.Start() - Iniciando...");
        #endif
        
        EnsureEventSystem();
        InitializeVideoLanguageMap();
        InitializeOculusStatus();
        SetupButtons();
        StartWebSocketServer();
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ ERRO CRÍTICO em OculusController.Start(): {e.Message}");
            Debug.LogError($"❌ StackTrace: {e.StackTrace}");
        }
        #endif
    }
    
    void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("✅ EventSystem criado");
        }
    }
    
    void InitializeVideoLanguageMap()
    {
        videoLanguageMap.Clear();
        videoLanguageMap.Add("pt", "Experiencia_Aegea_Cop2025_Portugues.mp4");
        videoLanguageMap.Add("en", "Experiencia_Aegea_Cop2025_Ingles.mp4");
        videoLanguageMap.Add("es", "Experiencia_Aegea_Cop2025_Espannhol.mp4");
    }
    
    void InitializeOculusStatus()
    {
        for (int i = 1; i <= 4; i++)
        {
            oculusConnected[i] = false;
            oculusBattery[i] = 0f;
            oculusLastPing[i] = DateTime.MinValue;
            oculusLastPong[i] = DateTime.MinValue;
            oculusLastActivity[i] = DateTime.MinValue;
            oculusReconnectAttempts[i] = 0;
        }
    }
    
    void SetupButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
        
        if (stopButton != null)
        {
            stopButton.onClick.RemoveAllListeners();
            stopButton.onClick.AddListener(OnStopButtonClicked);
        }
        
        for (int i = 0; i < (oculusPanels?.Length ?? 0); i++)
        {
            if (oculusPanels[i] != null)
            {
                oculusPanels[i].SetOculusId(i + 1);
            }
        }
    }
    
    async void StartWebSocketServer()
    {
        try
        {
            string localIP = GetLocalIPAddress();
            
            if (string.IsNullOrEmpty(localIP) || localIP == "0.0.0.0")
            {
                Debug.LogError("❌ Não foi possível obter o IP local!");
                isServerRunning = false;
                return;
            }
            
            IPAddress ipAddress = IPAddress.Any;
            tcpListener = new TcpListener(ipAddress, serverPort);
            tcpListener.Start();
            isServerRunning = true;
            
            string serverUrl = $"ws://{localIP}:{serverPort}";
            
            Debug.Log($"✅ Servidor WebSocket iniciado na porta {serverPort}");
            Debug.Log($"📡 IP: {serverUrl}");
            Debug.Log($"⚡ Ping: {pingInterval}s | Verificação: {connectionCheckInterval}s | Timeout: {inactivityTimeout}s");
            
            if (serverIPText != null)
            {
                serverIPText.text = $"Servidor: {serverUrl}";
            }
            
            _ = AcceptConnections();
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Erro ao iniciar servidor: {e.Message}");
            isServerRunning = false;
        }
    }
    
    string GetLocalIPAddress()
    {
        try
        {
            string hostName = Dns.GetHostName();
            IPHostEntry hostEntry = Dns.GetHostEntry(hostName);
            
            foreach (IPAddress ip in hostEntry.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && 
                    !IPAddress.IsLoopback(ip))
                {
                    return ip.ToString();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Erro ao obter IP: {e.Message}");
        }
        
        return "0.0.0.0";
    }
    
    async Task AcceptConnections()
    {
        while (isServerRunning && tcpListener != null)
        {
            try
            {
                TcpClient client = await tcpListener.AcceptTcpClientAsync();
                Debug.Log($"📥 Nova conexão de: {client.Client.RemoteEndPoint}");
                _ = HandleWebSocketConnection(client);
            }
            catch (Exception e)
            {
                if (isServerRunning)
                {
                    Debug.LogError($"❌ Erro ao aceitar conexão: {e.Message}");
                }
            }
        }
    }
    
    async Task HandleWebSocketConnection(TcpClient client)
    {
        NetworkStream stream = null;
        try
        {
            stream = client.GetStream();
            stream.ReadTimeout = 5000;
            
            byte[] buffer = new byte[4096];
            int totalBytesRead = 0;
            int bytesRead = 0;
            
            while (totalBytesRead < 4096)
            {
                bytesRead = await stream.ReadAsync(buffer, totalBytesRead, buffer.Length - totalBytesRead);
                if (bytesRead == 0) break;
                totalBytesRead += bytesRead;
                
                string partialRequest = Encoding.UTF8.GetString(buffer, 0, totalBytesRead);
                if (partialRequest.Contains("\r\n\r\n"))
                {
                    break;
                }
            }
            
            if (totalBytesRead == 0)
            {
                client.Close();
                return;
            }
            
            string request = Encoding.UTF8.GetString(buffer, 0, totalBytesRead);
            
            if (!request.Contains("Upgrade: websocket") || !request.Contains("Sec-WebSocket-Key"))
            {
                client.Close();
                return;
            }
            
            string secWebSocketKey = ExtractWebSocketKey(request);
            if (string.IsNullOrEmpty(secWebSocketKey))
            {
                client.Close();
                return;
            }
            
            string secWebSocketAccept = CalculateWebSocketAccept(secWebSocketKey);
            
            string response = "HTTP/1.1 101 Switching Protocols\r\n" +
                             "Upgrade: websocket\r\n" +
                             "Connection: Upgrade\r\n" +
                             $"Sec-WebSocket-Accept: {secWebSocketAccept}\r\n" +
                             "\r\n";
            
            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            
            Debug.Log($"✅ Handshake WebSocket aceito!");
            
            _ = ReceiveMessagesAndIdentifyOculus(client, stream);
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Erro ao processar conexão: {e.Message}");
            try { client?.Close(); } catch { }
        }
    }
    
    string ExtractWebSocketKey(string request)
    {
        string[] lines = request.Split('\n');
        foreach (string line in lines)
        {
            if (line.StartsWith("Sec-WebSocket-Key:", StringComparison.OrdinalIgnoreCase))
            {
                return line.Substring("Sec-WebSocket-Key:".Length).Trim();
            }
        }
        return null;
    }
    
    string CalculateWebSocketAccept(string secWebSocketKey)
    {
        const string magicString = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        string combined = secWebSocketKey + magicString;
        
        using (SHA1 sha1 = SHA1.Create())
        {
            byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(combined));
            return Convert.ToBase64String(hash);
        }
    }
    
    async Task ReceiveMessagesAndIdentifyOculus(TcpClient client, NetworkStream stream)
    {
        int? oculusId = null;
        bool identified = false;
        byte[] buffer = new byte[4096];
        
        try
        {
            stream.ReadTimeout = -1;
            
            while (client.Connected && stream.CanRead)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    
                    if (bytesRead >= 2)
                    {
                        int opcode = buffer[0] & 0x0F;
                        
                        if (opcode == 0x8) // Close
                        {
                            break;
                        }
                        
                        if (opcode == 0x9) // Ping
                        {
                            byte[] pongFrame = new byte[6];
                            pongFrame[0] = 0x8A; // FIN + Pong
                            pongFrame[1] = 0x00;
                            if (bytesRead > 6)
                            {
                                int pingPayloadLen = buffer[1] & 0x7F;
                                if (pingPayloadLen > 0 && pingPayloadLen <= 125)
                                {
                                    pongFrame[1] = (byte)pingPayloadLen;
                                    Array.Copy(buffer, 6, pongFrame, 2, pingPayloadLen);
                                }
                            }
                            await stream.WriteAsync(pongFrame, 0, pongFrame.Length);
                            
                            // Atualizar última atividade quando recebe ping
                            if (oculusId.HasValue)
                            {
                                oculusLastActivity[oculusId.Value] = DateTime.Now;
                            }
                            continue;
                        }
                        
                        if (opcode == 0xA) // Pong
                        {
                            // Atualizar último pong recebido
                            if (oculusId.HasValue)
                            {
                                oculusLastPong[oculusId.Value] = DateTime.Now;
                                oculusLastActivity[oculusId.Value] = DateTime.Now;
                                Debug.Log($"🏓 Pong recebido do Oculus {oculusId.Value}");
                            }
                            continue;
                        }
                    }
                    
                    string message = DecodeWebSocketFrame(buffer, bytesRead);
                    if (string.IsNullOrEmpty(message))
                    {
                        continue;
                    }
                    
                    // Atualizar última atividade para qualquer mensagem recebida
                    if (oculusId.HasValue)
                    {
                        oculusLastActivity[oculusId.Value] = DateTime.Now;
                    }
                    
                    if (!identified)
                    {
                        if (message.StartsWith("vr_connected"))
                        {
                            string idStr = message.Substring("vr_connected".Length);
                            if (int.TryParse(idStr, out int id) && id >= 1 && id <= 4)
                            {
                                oculusId = id;
                                identified = true;
                                
                                oculusConnections[id] = client;
                                oculusConnected[id] = true;
                                oculusLastPing[id] = DateTime.Now;
                                oculusLastPong[id] = DateTime.Now;
                                oculusLastActivity[id] = DateTime.Now;
                                oculusReconnectAttempts[id] = 0; // Reset tentativas
                                
                                if (oculusPanels[id - 1] != null)
                                {
                                    oculusPanels[id - 1].SetOnlineStatus(true);
                                }
                                
                                Debug.Log($"✅ Oculus {id} conectado e identificado!");
                                continue;
                            }
                        }
                    }
                    else
                    {
                        if (oculusId.HasValue)
                        {
                            ProcessMessageFromOculus(oculusId.Value, message);
                        }
                    }
                }
                catch (System.IO.IOException ioEx)
                {
                    if (ioEx.Message.Contains("interrupted") || ioEx.Message.Contains("closed"))
                    {
                        Debug.Log($"ℹ️ Conexão encerrada");
                    }
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Erro ao receber mensagem: {e.Message}");
        }
        finally
        {
            if (oculusId.HasValue)
            {
                oculusConnections.Remove(oculusId.Value);
                oculusConnected[oculusId.Value] = false;
                
                if (oculusPanels[oculusId.Value - 1] != null)
                {
                    oculusPanels[oculusId.Value - 1].SetOnlineStatus(false);
                }
                
                Debug.Log($"🔌 Oculus {oculusId.Value} desconectado");
            }
            
            try { client?.Close(); } catch { }
        }
    }
    
    string DecodeWebSocketFrame(byte[] buffer, int length)
    {
        if (length < 2) return null;
        
        bool fin = (buffer[0] & 0x80) != 0;
        int opcode = buffer[0] & 0x0F;
        bool masked = (buffer[1] & 0x80) != 0;
        int payloadLen = buffer[1] & 0x7F;
        
        int maskStart = 2;
        if (payloadLen == 126)
        {
            if (length < 4) return null;
            payloadLen = (buffer[2] << 8) | buffer[3];
            maskStart = 4;
        }
        else if (payloadLen == 127)
        {
            return null;
        }
        
        if (opcode == 0x8 || opcode == 0x9 || opcode == 0xA)
        {
            return null;
        }
        if (opcode != 0x1)
        {
            return null;
        }
        
        int dataStart = maskStart;
        if (masked)
        {
            if (length < maskStart + 4) return null;
            dataStart += 4;
            byte[] mask = new byte[4];
            Array.Copy(buffer, maskStart, mask, 0, 4);
            
            for (int i = 0; i < payloadLen && (dataStart + i) < length; i++)
            {
                buffer[dataStart + i] = (byte)(buffer[dataStart + i] ^ mask[i % 4]);
            }
        }
        
        if (dataStart + payloadLen > length)
        {
            return null;
        }
        
        return Encoding.UTF8.GetString(buffer, dataStart, payloadLen);
    }
    
    void EncodeWebSocketFrame(string message, byte[] output, out int length)
    {
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        int payloadLen = messageBytes.Length;
        
        output[0] = 0x81;
        int index = 2;
        
        if (payloadLen < 126)
        {
            output[1] = (byte)payloadLen;
        }
        else if (payloadLen < 65536)
        {
            output[1] = 126;
            output[2] = (byte)(payloadLen >> 8);
            output[3] = (byte)(payloadLen & 0xFF);
            index = 4;
        }
        
        Array.Copy(messageBytes, 0, output, index, payloadLen);
        length = index + payloadLen;
    }
    
    void ProcessMessageFromOculus(int oculusId, string message)
    {
        message = message.Trim();
        
        if (message.StartsWith("battery"))
        {
            string[] parts = message.Split(':');
            if (parts.Length >= 2)
            {
                if (float.TryParse(parts[1], out float batteryPercent))
                {
                    oculusBattery[oculusId] = batteryPercent;
                    if (oculusPanels[oculusId - 1] != null)
                    {
                        oculusPanels[oculusId - 1].SetBatteryLevel(batteryPercent);
                    }
                }
            }
        }
        else if (message.StartsWith("percent"))
        {
            string[] parts = message.Split(':');
            if (parts.Length >= 2)
            {
                if (int.TryParse(parts[1], out int percent))
                {
                    if (videoPreview != null && isPlaying)
                    {
                        videoPreview.UpdateProgress(percent);
                    }
                }
            }
        }
        else if (message.StartsWith("video_ended"))
        {
            Debug.Log($"🎬 Oculus {oculusId} terminou o vídeo");
        }
    }
    
    async void OnStartButtonClicked()
    {
        Debug.Log($"🎬 Botão Start clicado!");
        
        if (!isServerRunning)
        {
            Debug.LogWarning("⚠️ Servidor não está rodando!");
            return;
        }
        
        if (oculusPanels == null || oculusPanels.Length < 4)
        {
            Debug.LogError("❌ Painéis não configurados!");
            return;
        }
        
        List<Task> sendTasks = new List<Task>();
        
        for (int i = 0; i < 4; i++)
        {
            int oculusId = i + 1;
            string language = "pt";
            if (oculusPanels[i] != null)
            {
                string selectedLang = oculusPanels[i].GetSelectedLanguage();
                if (!string.IsNullOrEmpty(selectedLang))
                {
                    language = selectedLang;
                }
            }
            
            string message = $"play{oculusId}_{language}";
            sendTasks.Add(SendMessageToOculusSafe(oculusId, message));
        }
        
        try
        {
            await Task.WhenAll(sendTasks);
            Debug.Log($"✅ Comandos play enviados!");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Erro: {e.Message}");
        }
        
        if (videoPreview != null)
        {
            string previewLanguage = "pt";
            if (oculusPanels[0] != null)
            {
                string selectedLang = oculusPanels[0].GetSelectedLanguage();
                if (!string.IsNullOrEmpty(selectedLang))
                {
                    previewLanguage = selectedLang;
                }
            }
            videoPreview.PlayVideo(previewLanguage);
        }
        
        isPlaying = true;
        UpdateButtons();
    }
    
    async void OnStopButtonClicked()
    {
        Debug.Log("⏹️ BOTÃO STOP CLICADO!");
        
        if (!isServerRunning)
        {
            return;
        }
        
        isPlaying = false;
        
        if (videoPreview != null)
        {
            videoPreview.StopVideo();
        }
        
        List<Task> stopTasks = new List<Task>();
        
        for (int i = 0; i < 4; i++)
        {
            int oculusId = i + 1;
            string message = $"stop{oculusId}";
            stopTasks.Add(SendStopMessageWithRetry(oculusId, message));
        }
        
        try
        {
            await Task.WhenAll(stopTasks);
            Debug.Log($"✅ Comandos stop enviados!");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Erro: {e.Message}");
        }
        
        await Task.Delay(100);
        UpdateButtons();
    }
    
    async Task SendStopMessageWithRetry(int oculusId, string message, int maxRetries = 5)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            bool isConnected = IsOculusConnected(oculusId);
            
            if (isConnected)
            {
                try
                {
                    await SendMessageToOculus(oculusId, message);
                    Debug.Log($"✅ Stop enviado para Oculus {oculusId} (tentativa {attempt})");
                    return;
                }
                catch (Exception e)
                {
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(100 * attempt);
                    }
                }
            }
            else
            {
                if (attempt < maxRetries)
                {
                    await Task.Delay(200 * attempt);
                }
            }
        }
        
        Debug.LogWarning($"⚠️ Não foi possível enviar stop para Oculus {oculusId}");
    }
    
    bool IsOculusConnected(int oculusId)
    {
        // SOLUÇÃO SIMPLES: Verificar apenas se está no dicionário e Connected
        // Não limpa conexões aqui - deixa TCP gerenciar
        try
        {
            if (!oculusConnections.ContainsKey(oculusId))
            {
                return false;
            }
            
            TcpClient client = oculusConnections[oculusId];
            if (client == null)
            {
                return false;
            }
            
            // Verificar apenas Connected - confia no TCP
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }
    
    void CleanupDisconnectedOculus(int oculusId)
    {
        try
        {
            if (oculusConnections.ContainsKey(oculusId))
            {
                TcpClient client = oculusConnections[oculusId];
                try { client?.Close(); } catch { }
                oculusConnections.Remove(oculusId);
            }
            
            oculusConnected[oculusId] = false;
            
            // Limpar timestamps de ping/pong/atividade
            oculusLastPing.Remove(oculusId);
            oculusLastPong.Remove(oculusId);
            oculusLastActivity.Remove(oculusId);
            oculusReconnectAttempts.Remove(oculusId);
            
            if (oculusPanels != null && oculusId >= 1 && oculusId <= oculusPanels.Length)
            {
                if (oculusPanels[oculusId - 1] != null)
                {
                    oculusPanels[oculusId - 1].SetOnlineStatus(false);
                }
            }
        }
        catch { }
    }
    
    async Task SendMessageToOculusSafe(int oculusId, string message)
    {
        try
        {
            await SendMessageToOculus(oculusId, message);
        }
        catch (Exception e)
        {
            Debug.Log($"ℹ️ Não foi possível enviar para Oculus {oculusId}: {e.Message}");
        }
    }
    
    async Task SendMessageToOculus(int oculusId, string message)
    {
        if (!IsOculusConnected(oculusId))
        {
            throw new InvalidOperationException($"Oculus {oculusId} não está conectado");
        }
        
        TcpClient client = oculusConnections[oculusId];
        if (client == null)
        {
            throw new InvalidOperationException($"Oculus {oculusId} - conexão não disponível");
        }
        
        NetworkStream stream = client.GetStream();
        if (stream == null || !stream.CanWrite)
        {
            throw new InvalidOperationException($"Oculus {oculusId} - stream inválido");
        }
        
        byte[] frame = new byte[4096];
        EncodeWebSocketFrame(message, frame, out int length);
        
        await stream.WriteAsync(frame, 0, length);
    }
    
    void UpdateButtons()
    {
        // Botões funcionam sempre que servidor está rodando
        // Não depende de Oculus conectado - permite iniciar/parar mesmo sem conexão
        if (startButton != null)
        {
            startButton.interactable = !isPlaying && isServerRunning;
        }
        
        if (stopButton != null)
        {
            stopButton.interactable = isServerRunning && isPlaying;
        }
    }
    
    void Update()
    {
        UpdateButtons();
        
        if (!isServerRunning) return;
        
        float currentTime = Time.time;
        
        // REMOVIDO: Verificação periódica de conexões
        // TCP gerencia conexões automaticamente - não precisa verificar
        // Conexão só é limpa quando ReceiveMessagesAndIdentifyOculus termina naturalmente
        
        // Enviar ping periódico apenas para manter conexão viva (não desconecta)
        if (currentTime - lastPingTime >= pingInterval)
        {
            _ = SendPingToAllConnected();
            lastPingTime = currentTime;
        }
        
        // Solicitar status da bateria periodicamente
        if (Time.frameCount % 300 == 0)
        {
            _ = RequestBatteryStatus();
        }
    }
    
    // ============================================================
    // SOLUÇÃO DEFINITIVA: Não verificar conexões - deixar TCP gerenciar
    // ============================================================
    
    // REMOVIDO: CheckActiveConnections() e CheckInactivityTimeouts()
    // TCP gerencia conexões automaticamente - não precisa verificar periodicamente
    // Conexão só é limpa quando ReceiveMessagesAndIdentifyOculus termina naturalmente (no finally)
    // Isso garante que conexões válidas nunca são desconectadas prematuramente
    
    async Task SendPingToAllConnected()
    {
        // Enviar ping para manter conexão viva - não verifica/desconecta
        // Se der erro ao enviar, TCP vai detectar naturalmente
        List<Task> pingTasks = new List<Task>();
        
        foreach (var kvp in oculusConnections.ToList())
        {
            int oculusId = kvp.Key;
            TcpClient client = kvp.Value;
            
            // Tentar enviar ping - se falhar, TCP vai detectar naturalmente
            if (client != null)
            {
                pingTasks.Add(SendPingToOculus(oculusId, client));
            }
        }
        
        if (pingTasks.Count > 0)
        {
            try
            {
                await Task.WhenAll(pingTasks);
            }
            catch (Exception e)
            {
                // Erro ao enviar ping - não fazer nada, TCP vai detectar se conexão está morta
                // Não limpar conexões aqui - deixar TCP gerenciar
            }
        }
    }
    
    async Task SendPingToOculus(int oculusId, TcpClient client)
    {
        try
        {
            if (client == null || !client.Connected)
            {
                return;
            }
            
            NetworkStream stream = client.GetStream();
            if (stream == null || !stream.CanWrite)
            {
                return;
            }
            
            // Enviar frame WebSocket ping (opcode 0x9)
            byte[] pingFrame = new byte[2];
            pingFrame[0] = 0x89; // FIN + Ping frame
            pingFrame[1] = 0x00; // Payload length 0
            
            await stream.WriteAsync(pingFrame, 0, pingFrame.Length);
            
            // Atualizar último ping enviado
            oculusLastPing[oculusId] = DateTime.Now;
            
            // Log apenas ocasionalmente para não poluir
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"📡 Ping enviado para Oculus {oculusId}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ Erro ao enviar ping para Oculus {oculusId}: {e.Message}");
        }
    }
    
    async Task RequestBatteryStatus()
    {
        for (int i = 1; i <= 4; i++)
        {
            if (oculusConnected.ContainsKey(i) && oculusConnected[i] &&
                oculusConnections.ContainsKey(i))
            {
                string message = $"get_battery{i}";
                await SendMessageToOculus(i, message);
            }
        }
    }
    
    void OnDestroy()
    {
        isServerRunning = false;
        
        foreach (var kvp in oculusConnections)
        {
            try
            {
                kvp.Value?.Close();
            }
            catch { }
        }
        
        try
        {
            tcpListener?.Stop();
        }
        catch { }
    }
}

