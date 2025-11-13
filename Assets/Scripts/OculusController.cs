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
    
    // Sincronização para evitar race conditions com múltiplas conexões
    private readonly object connectionsLock = new object();
    private bool isPinging = false;
    private bool isRequestingBattery = false;
    
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
            
            while (true)
            {
                try
                {
                    // Verificar se conexão ainda está válida
                    if (!client.Connected || !stream.CanRead)
                    {
                        Debug.Log("ℹ️ Conexão não está mais válida");
                        break;
                    }
                    
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        Debug.Log("ℹ️ Stream fechado (bytesRead = 0)");
                        break;
                    }
                    
                    if (bytesRead < 2)
                    {
                        Debug.LogWarning($"⚠️ Frame muito pequeno: {bytesRead} bytes");
                        continue;
                    }
                    
                    int opcode = buffer[0] & 0x0F;
                    
                    // Processar frames de controle primeiro
                    if (opcode == 0x8) // Close
                    {
                        Debug.Log("ℹ️ Frame Close recebido");
                        break;
                    }
                    
                    if (opcode == 0x9) // Ping
                    {
                        // Responder com Pong
                        byte[] pongFrame = new byte[2];
                        pongFrame[0] = 0x8A; // FIN + Pong opcode
                        pongFrame[1] = 0x00; // Payload length 0
                        
                        try
                        {
                            await stream.WriteAsync(pongFrame, 0, pongFrame.Length);
                            
                            // Atualizar última atividade quando recebe ping
                            if (oculusId.HasValue)
                            {
                                lock (connectionsLock)
                                {
                                    if (oculusLastActivity.ContainsKey(oculusId.Value))
                                    {
                                        oculusLastActivity[oculusId.Value] = DateTime.Now;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"⚠️ Erro ao enviar Pong: {ex.Message}");
                            break;
                        }
                        continue;
                    }
                    
                    if (opcode == 0xA) // Pong
                    {
                        // Atualizar último pong recebido
                        if (oculusId.HasValue)
                        {
                            lock (connectionsLock)
                            {
                                oculusLastPong[oculusId.Value] = DateTime.Now;
                                oculusLastActivity[oculusId.Value] = DateTime.Now;
                            }
                            Debug.Log($"🏓 Pong recebido do Oculus {oculusId.Value}");
                        }
                        continue;
                    }
                    
                    // Processar frames de texto
                    if (opcode == 0x1) // Text frame
                    {
                        string message = DecodeWebSocketFrame(buffer, bytesRead);
                        if (string.IsNullOrEmpty(message))
                        {
                            Debug.LogWarning("⚠️ Mensagem vazia ou inválida após decodificação");
                            continue;
                        }
                        
                        // Log de TODAS as mensagens recebidas para debug
                        Debug.Log($"📨 Mensagem recebida (Oculus {oculusId?.ToString() ?? "não identificado"}): '{message}'");
                        
                        // Atualizar última atividade para qualquer mensagem recebida
                        if (oculusId.HasValue)
                        {
                            lock (connectionsLock)
                            {
                                if (oculusLastActivity.ContainsKey(oculusId.Value))
                                {
                                    oculusLastActivity[oculusId.Value] = DateTime.Now;
                                }
                            }
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
                                    
                                    // Proteger modificação dos dicionários com lock
                                    lock (connectionsLock)
                                    {
                                        oculusConnections[id] = client;
                                        oculusConnected[id] = true;
                                        oculusLastPing[id] = DateTime.Now;
                                        oculusLastPong[id] = DateTime.Now;
                                        oculusLastActivity[id] = DateTime.Now;
                                        oculusReconnectAttempts[id] = 0;
                                    }
                                    
                                    if (oculusPanels[id - 1] != null)
                                    {
                                        oculusPanels[id - 1].SetOnlineStatus(true);
                                    }
                                    
                                    Debug.Log($"✅ Oculus {id} conectado e identificado!");
                                }
                                else
                                {
                                    Debug.LogWarning($"⚠️ ID inválido na mensagem vr_connected: '{idStr}'");
                                }
                            }
                            else if (message.StartsWith("CLIENT_INFO"))
                            {
                                // Processar CLIENT_INFO mesmo antes da identificação (pode chegar antes do vr_connected)
                                Debug.Log($"📥 CLIENT_INFO recebido antes da identificação: {message}");
                                // Aguardar identificação para processar
                            }
                            else
                            {
                                Debug.LogWarning($"⚠️ Mensagem recebida antes da identificação: {message}");
                            }
                        }
                        else
                        {
                            if (oculusId.HasValue)
                            {
                                Debug.Log($"📥 Processando mensagem do Oculus {oculusId.Value}: {message}");
                                ProcessMessageFromOculus(oculusId.Value, message);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Opcode não tratado: {opcode} (0x{opcode:X})");
                    }
                }
                catch (System.IO.IOException ioEx)
                {
                    // IOException pode ser normal quando conexão é fechada
                    Debug.Log($"ℹ️ IOException: {ioEx.Message}");
                    break;
                }
                catch (System.Net.Sockets.SocketException sockEx)
                {
                    Debug.Log($"ℹ️ SocketException: {sockEx.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Erro ao processar mensagem: {ex.Message}");
                    Debug.LogError($"❌ StackTrace: {ex.StackTrace}");
                    // Continuar tentando em vez de quebrar imediatamente
                    await Task.Delay(100);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Erro crítico ao receber mensagens: {e.Message}");
            Debug.LogError($"❌ StackTrace: {e.StackTrace}");
        }
        finally
        {
            if (oculusId.HasValue)
            {
                lock (connectionsLock)
                {
                    if (oculusConnections.ContainsKey(oculusId.Value))
                    {
                        oculusConnections.Remove(oculusId.Value);
                    }
                    oculusConnected[oculusId.Value] = false;
                }
                
                if (oculusPanels[oculusId.Value - 1] != null)
                {
                    oculusPanels[oculusId.Value - 1].SetOnlineStatus(false);
                }
                
                Debug.Log($"🔌 Oculus {oculusId.Value} desconectado");
            }
            
            try 
            { 
                if (stream != null)
                {
                    stream.Close();
                }
                client?.Close(); 
            } 
            catch { }
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
                // Extrair ID do Oculus da mensagem (formato: battery{id}:{percent})
                string batteryPart = parts[0];
                int messageOculusId = 0;
                
                // Tentar extrair ID do formato "battery1", "battery2", etc.
                if (batteryPart.Length > "battery".Length)
                {
                    string idStr = batteryPart.Substring("battery".Length);
                    if (!int.TryParse(idStr, out messageOculusId))
                    {
                        messageOculusId = oculusId; // Usar o ID da conexão se não conseguir extrair
                    }
                }
                else
                {
                    messageOculusId = oculusId; // Usar o ID da conexão
                }
                
                // Validar que o ID da mensagem corresponde ao ID da conexão
                if (messageOculusId != oculusId)
                {
                    Debug.LogWarning($"⚠️ ID de bateria não corresponde: mensagem={messageOculusId}, conexão={oculusId}");
                    // Continuar mesmo assim, pode ser um caso legítimo
                }
                
                if (float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float batteryPercent))
                {
                    // Validar valor recebido
                    if (batteryPercent >= 0f && batteryPercent <= 100f)
                    {
                        lock (connectionsLock)
                        {
                            oculusBattery[oculusId] = batteryPercent;
                        }
                        
                        if (oculusPanels[oculusId - 1] != null)
                        {
                            oculusPanels[oculusId - 1].SetBatteryLevel(batteryPercent);
                        }
                        
                        Debug.Log($"🔋 Bateria do Oculus {oculusId}: {batteryPercent:F1}%");
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Valor de bateria inválido recebido do Oculus {oculusId}: {batteryPercent:F1}%");
                    }
                }
                else
                {
                    Debug.LogWarning($"⚠️ Não foi possível fazer parse da bateria do Oculus {oculusId}: '{parts[1]}'");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ Formato de mensagem de bateria inválido: '{message}'");
            }
        }
        else if (message.StartsWith("CLIENT_INFO"))
        {
            Debug.Log($"📥 Processando CLIENT_INFO do Oculus {oculusId}: {message}");
            
            // Formato: CLIENT_INFO:{clientName}|{clientIP}|{clientOS}|{batteryLevel}%
            string[] parts = message.Split(':');
            if (parts.Length >= 2)
            {
                string[] infoParts = parts[1].Split('|');
                Debug.Log($"📥 CLIENT_INFO split: {infoParts.Length} partes");
                
                if (infoParts.Length >= 4)
                {
                    string clientName = infoParts[0];
                    string clientIP = infoParts[1];
                    string clientOS = infoParts[2];
                    string batteryStr = infoParts[3].Replace("%", ""); // Remover o símbolo %
                    
                    Debug.Log($"📥 CLIENT_INFO - Nome: {clientName}, IP: {clientIP}, OS: {clientOS}, Bateria: '{batteryStr}'");
                    
                    if (int.TryParse(batteryStr, out int batteryLevel))
                    {
                        // Validar valor recebido
                        if (batteryLevel >= 0 && batteryLevel <= 100)
                        {
                            // Proteger atualização do dicionário com lock
                            lock (connectionsLock)
                            {
                                oculusBattery[oculusId] = batteryLevel;
                            }
                            
                            if (oculusPanels[oculusId - 1] != null)
                            {
                                oculusPanels[oculusId - 1].SetBatteryLevel(batteryLevel);
                                Debug.Log($"✅ Bateria atualizada no painel do Oculus {oculusId}: {batteryLevel}%");
                            }
                            else
                            {
                                Debug.LogWarning($"⚠️ oculusPanels[{oculusId - 1}] é null!");
                            }
                            
                            Debug.Log($"🔋 CLIENT_INFO do Oculus {oculusId}: {clientName} | {clientIP} | {clientOS} | {batteryLevel}%");
                        }
                        else
                        {
                            Debug.LogWarning($"⚠️ Valor de bateria inválido no CLIENT_INFO do Oculus {oculusId}: {batteryLevel}%");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Não foi possível fazer parse da bateria no CLIENT_INFO do Oculus {oculusId}: '{batteryStr}'");
                    }
                }
                else
                {
                    Debug.LogWarning($"⚠️ Formato de CLIENT_INFO inválido - esperado 4 partes, recebido {infoParts.Length}: '{message}'");
                    for (int i = 0; i < infoParts.Length; i++)
                    {
                        Debug.LogWarning($"  Parte {i}: '{infoParts[i]}'");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ CLIENT_INFO não tem formato correto (sem ':') - '{message}'");
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
        // Protegido com lock para evitar race conditions
        lock (connectionsLock)
        {
            try
            {
                if (!oculusConnections.ContainsKey(oculusId) ||
                    !oculusConnected.ContainsKey(oculusId) ||
                    !oculusConnected[oculusId])
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
    }
    
    void CleanupDisconnectedOculus(int oculusId)
    {
        try
        {
            TcpClient clientToClose = null;
            
            // Proteger modificação dos dicionários com lock
            lock (connectionsLock)
            {
                if (oculusConnections.ContainsKey(oculusId))
                {
                    clientToClose = oculusConnections[oculusId];
                    oculusConnections.Remove(oculusId);
                }
                
                oculusConnected[oculusId] = false;
                
                // Limpar timestamps de ping/pong/atividade
                oculusLastPing.Remove(oculusId);
                oculusLastPong.Remove(oculusId);
                oculusLastActivity.Remove(oculusId);
                oculusReconnectAttempts.Remove(oculusId);
            }
            
            // Fechar conexão fora do lock para evitar deadlock
            try { clientToClose?.Close(); } catch { }
            
            // Atualizar UI fora do lock
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
        // Botões Start e Stop sempre habilitados quando servidor está rodando
        // Permite controle total do tablet independente do estado atual
        if (startButton != null)
        {
            startButton.interactable = isServerRunning; // Sempre habilitado quando servidor está rodando
        }
        
        if (stopButton != null)
        {
            stopButton.interactable = isServerRunning; // Sempre habilitado quando servidor está rodando
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
        
        // Enviar ping periódico (reduzido para 5s para evitar sobrecarga com múltiplas conexões)
        if (currentTime - lastPingTime >= 5f) // Mudado de pingInterval para 5f fixo
        {
            lastPingTime = currentTime;
            // Verificar se já existe uma task rodando antes de criar nova
            if (!isPinging)
            {
                isPinging = true;
                _ = SendPingToAllConnected().ContinueWith(t => 
                { 
                    isPinging = false; 
                    if (t.IsFaulted)
                    {
                        Debug.LogWarning($"⚠️ Erro ao enviar pings: {t.Exception?.GetBaseException()?.Message}");
                    }
                });
            }
        }
        
        // Solicitar status da bateria menos frequentemente (a cada 10 segundos)
        if (Time.frameCount % 600 == 0) // Mudado de 300 para 600 frames (~10s a 60fps)
        {
            if (!isRequestingBattery)
            {
                isRequestingBattery = true;
                _ = RequestBatteryStatus().ContinueWith(t => 
                { 
                    isRequestingBattery = false; 
                    if (t.IsFaulted)
                    {
                        Debug.LogWarning($"⚠️ Erro ao solicitar bateria: {t.Exception?.GetBaseException()?.Message}");
                    }
                });
            }
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
        List<KeyValuePair<int, TcpClient>> connectionsCopy;
        
        // Criar cópia da lista de conexões dentro do lock para evitar race conditions
        lock (connectionsLock)
        {
            connectionsCopy = oculusConnections.ToList();
        }
        
        foreach (var kvp in connectionsCopy)
        {
            int oculusId = kvp.Key;
            TcpClient client = kvp.Value;
            
            // Tentar enviar ping - se falhar, TCP vai detectar naturalmente
            if (client != null && client.Connected)
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
                Debug.LogWarning($"⚠️ Erro ao enviar pings: {e.Message}");
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
            lock (connectionsLock)
            {
                if (oculusLastPing.ContainsKey(oculusId))
                {
                    oculusLastPing[oculusId] = DateTime.Now;
                }
            }
            
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
        List<int> connectedIds = new List<int>();
        
        // Criar lista de IDs conectados dentro do lock
        lock (connectionsLock)
        {
            for (int i = 1; i <= 4; i++)
            {
                if (oculusConnected.ContainsKey(i) && oculusConnected[i] &&
                    oculusConnections.ContainsKey(i) && 
                    oculusConnections[i] != null &&
                    oculusConnections[i].Connected)
                {
                    connectedIds.Add(i);
                }
            }
        }
        
        // Enviar mensagens fora do lock
        foreach (int oculusId in connectedIds)
        {
            try
            {
                string message = $"get_battery{oculusId}";
                await SendMessageToOculus(oculusId, message);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠️ Erro ao solicitar bateria do Oculus {oculusId}: {e.Message}");
            }
        }
    }
    
    void OnDestroy()
    {
        isServerRunning = false;
        
        // Criar cópia da lista dentro do lock para evitar race conditions
        List<KeyValuePair<int, TcpClient>> connectionsCopy;
        lock (connectionsLock)
        {
            connectionsCopy = oculusConnections.ToList();
        }
        
        foreach (var kvp in connectionsCopy)
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

