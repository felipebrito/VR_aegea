using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Adicionado para ToList()
using System.Net;
using System.Net.WebSockets;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

/// <summary>
/// Controlador principal para gerenciar os 4 Oculus headsets.
/// Funciona como servidor WebSocket - os Oculus se conectam a este tablet.
/// VERSÃO MELHORADA: Com ping periódico e verificação de conexões para manter conexões estáveis.
/// </summary>
public class OculusController_Improved : MonoBehaviour
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
    
    // Servidor WebSocket usando TcpListener (funciona melhor no Unity/Mono)
    private TcpListener tcpListener;
    private bool isServerRunning = false;
    private Dictionary<int, TcpClient> oculusConnections = new Dictionary<int, TcpClient>();
    private Dictionary<int, bool> oculusConnected = new Dictionary<int, bool>();
    private Dictionary<int, float> oculusBattery = new Dictionary<int, float>();
    private Dictionary<int, DateTime> oculusLastPing = new Dictionary<int, DateTime>(); // Último ping enviado para cada Oculus
    private bool isPlaying = false;
    private float lastConnectionCheck = 0f;
    private float lastPingTime = 0f;
    private const float CONNECTION_CHECK_INTERVAL = 5f; // Verificar conexões a cada 5 segundos
    private const float PING_INTERVAL = 10f; // Enviar ping a cada 10 segundos
    
    void Start()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            Debug.Log("🎮 OculusController.Start() - Iniciando...");
        #endif
        
        // Garantir que há um EventSystem (necessário para botões clicáveis)
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
            // Continuar mesmo com erro para não fechar o app
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
            Debug.Log("✅ EventSystem criado para permitir cliques nos botões");
        }
        else
        {
            Debug.Log("✅ EventSystem já existe na cena");
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
        }
    }
    
    void SetupButtons()
    {
        Debug.Log($"🔧 Configurando botões...");
        Debug.Log($"🔧 StartButton: {(startButton != null ? "Configurado" : "NULL")}");
        Debug.Log($"🔧 StopButton: {(stopButton != null ? "Configurado" : "NULL")}");
        
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartButtonClicked);
            Debug.Log($"✅ StartButton configurado com listener");
        }
        else
        {
            Debug.LogError("❌ StartButton não está configurado no Inspector!");
        }
        
        if (stopButton != null)
        {
            stopButton.onClick.RemoveAllListeners();
            stopButton.onClick.AddListener(OnStopButtonClicked);
            Debug.Log($"✅ StopButton configurado com listener");
        }
        else
        {
            Debug.LogError("❌ StopButton não está configurado no Inspector!");
        }
        
        // Verificar e inicializar painéis
        Debug.Log($"🔧 Painéis configurados: {oculusPanels?.Length ?? 0}");
        for (int i = 0; i < (oculusPanels?.Length ?? 0); i++)
        {
            if (oculusPanels[i] != null)
            {
                // Garantir que cada painel tem seu ID configurado
                oculusPanels[i].SetOculusId(i + 1);
                Debug.Log($"🔧 Painel {i + 1}: Configurado e inicializado");
            }
            else
            {
                Debug.LogWarning($"⚠️ Painel {i + 1}: NULL - não configurado no Inspector!");
            }
        }
        
        // Verificar se há exatamente 4 painéis
        if (oculusPanels == null || oculusPanels.Length != 4)
        {
            Debug.LogError($"❌ ERRO: Deve haver exatamente 4 painéis configurados! Encontrados: {oculusPanels?.Length ?? 0}");
        }
    }
    
    async void StartWebSocketServer()
    {
        try
        {
            // Obter IP local da máquina (tablet)
            string localIP = GetLocalIPAddress();
            
            if (string.IsNullOrEmpty(localIP) || localIP == "0.0.0.0")
            {
                Debug.LogError("❌ Não foi possível obter o IP local da máquina!");
                isServerRunning = false;
                return;
            }
            
            // Usar TcpListener diretamente (funciona melhor no Unity/Mono)
            IPAddress ipAddress = IPAddress.Any; // Aceita conexões de qualquer IP
            tcpListener = new TcpListener(ipAddress, serverPort);
            tcpListener.Start();
            isServerRunning = true;
            
            string serverUrl = $"ws://{localIP}:{serverPort}";
            
            Debug.Log($"✅ Servidor WebSocket iniciado na porta {serverPort}");
            Debug.Log($"📡 IP do servidor: {serverUrl}");
            Debug.Log($"📡 Aguardando conexões dos Oculus...");
            Debug.Log($"💡 Configure o cliente para conectar em: {serverUrl}");
            
            // Atualizar texto do IP se configurado
            if (serverIPText != null)
            {
                serverIPText.text = $"Servidor: {serverUrl}";
            }
            
            // Iniciar loop de aceitação de conexões
            _ = AcceptConnections();
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Erro ao iniciar servidor WebSocket: {e.Message}");
            Debug.LogError($"❌ StackTrace: {e.StackTrace}");
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
            Debug.LogError($"Erro ao obter IP local: {e.Message}");
        }
        
        return "0.0.0.0";
    }
    
    async Task AcceptConnections()
    {
        Debug.Log($"🔄 Loop de aceitação iniciado - Servidor escutando na porta {serverPort}");
        
        while (isServerRunning && tcpListener != null)
        {
            try
            {
                Debug.Log($"👂 Aguardando nova conexão...");
                TcpClient client = await tcpListener.AcceptTcpClientAsync();
                Debug.Log($"📥 Nova conexão TCP recebida de: {client.Client.RemoteEndPoint}");
                
                // Processar conexão WebSocket em thread separada
                _ = HandleWebSocketConnection(client);
            }
            catch (Exception e)
            {
                if (isServerRunning)
                {
                    Debug.LogError($"❌ Erro ao aceitar conexão: {e.Message}");
                    Debug.LogError($"❌ StackTrace: {e.StackTrace}");
                }
            }
        }
        
        // Servidor foi parado normalmente
        Debug.Log($"ℹ️ Loop de aceitação encerrado - servidor parado");
    }
    
    async Task HandleWebSocketConnection(TcpClient client)
    {
        NetworkStream stream = null;
        try
        {
            stream = client.GetStream();
            stream.ReadTimeout = 5000; // Timeout de 5 segundos
            
            // Ler handshake HTTP (pode vir em múltiplos pacotes)
            StringBuilder requestBuilder = new StringBuilder();
            byte[] buffer = new byte[4096];
            int totalBytesRead = 0;
            int bytesRead = 0;
            
            // Ler até encontrar o fim do handshake HTTP (\r\n\r\n)
            while (totalBytesRead < 4096)
            {
                bytesRead = await stream.ReadAsync(buffer, totalBytesRead, buffer.Length - totalBytesRead);
                if (bytesRead == 0) break;
                totalBytesRead += bytesRead;
                
                string partialRequest = Encoding.UTF8.GetString(buffer, 0, totalBytesRead);
                if (partialRequest.Contains("\r\n\r\n"))
                {
                    break; // Handshake completo
                }
            }
            
            if (totalBytesRead == 0)
            {
                Debug.LogWarning("⚠️ Nenhum dado recebido no handshake");
                client.Close();
                return;
            }
            
            string request = Encoding.UTF8.GetString(buffer, 0, totalBytesRead);
            Debug.Log($"📥 Handshake recebido ({totalBytesRead} bytes):\n{request.Substring(0, Math.Min(200, request.Length))}");
            
            // Verificar se é WebSocket
            if (!request.Contains("Upgrade: websocket") || !request.Contains("Sec-WebSocket-Key"))
            {
                Debug.LogWarning("⚠️ Não é uma requisição WebSocket válida");
                Debug.LogWarning($"⚠️ Request contém 'Upgrade: websocket': {request.Contains("Upgrade: websocket")}");
                Debug.LogWarning($"⚠️ Request contém 'Sec-WebSocket-Key': {request.Contains("Sec-WebSocket-Key")}");
                client.Close();
                return;
            }
            
            // Extrair Sec-WebSocket-Key
            string secWebSocketKey = ExtractWebSocketKey(request);
            if (string.IsNullOrEmpty(secWebSocketKey))
            {
                Debug.LogError("❌ Sec-WebSocket-Key não encontrado!");
                client.Close();
                return;
            }
            
            // Calcular resposta do handshake
            string secWebSocketAccept = CalculateWebSocketAccept(secWebSocketKey);
            
            // Enviar resposta do handshake
            string response = "HTTP/1.1 101 Switching Protocols\r\n" +
                             "Upgrade: websocket\r\n" +
                             "Connection: Upgrade\r\n" +
                             $"Sec-WebSocket-Accept: {secWebSocketAccept}\r\n" +
                             "\r\n";
            
            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            
            Debug.Log($"✅ Handshake WebSocket aceito! Aguardando mensagens do cliente...");
            
            // Processar mensagens WebSocket
            _ = ReceiveMessagesAndIdentifyOculus(client, stream);
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Erro ao processar conexão WebSocket: {e.Message}");
            Debug.LogError($"❌ StackTrace: {e.StackTrace}");
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
        // WebSocket handshake: SHA1(key + magic string) -> Base64
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
            stream.ReadTimeout = -1; // Timeout.Infinite - sem timeout para leitura contínua
            
            Debug.Log($"🔍 Iniciando loop de leitura de mensagens...");
            Debug.Log($"🔍 Cliente conectado: {client.Connected}");
            Debug.Log($"🔍 Stream pode ler: {stream.CanRead}");
            
            while (client.Connected && stream.CanRead)
            {
                try
                {
                    // Ler frame WebSocket
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        Debug.Log($"ℹ️ Conexão fechada pelo cliente");
                        break;
                    }
                    
                    // Verificar tipo de frame antes de decodificar
                    if (bytesRead >= 2)
                    {
                        int opcode = buffer[0] & 0x0F;
                        
                        // Verificar se é close frame (0x8)
                        if (opcode == 0x8)
                        {
                            Debug.Log($"🔌 Close frame recebido do cliente - encerrando conexão");
                            break; // Sair do loop para fechar conexão
                        }
                        
                        // Verificar se é ping (0x9)
                        if (opcode == 0x9)
                        {
                            Debug.Log("🏓 Ping recebido - enviando pong");
                            // Enviar pong (opcode 0xA)
                            byte[] pongFrame = new byte[6];
                            pongFrame[0] = 0x8A; // FIN + Pong
                            pongFrame[1] = 0x00; // Payload length 0
                            // Copiar payload do ping para o pong (se houver)
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
                            continue;
                        }
                        
                        // Verificar se é pong (0xA) - apenas ignorar
                        if (opcode == 0xA)
                        {
                            Debug.Log("🏓 Pong recebido");
                            continue;
                        }
                    }
                    
                    // Decodificar frame WebSocket (apenas para frames de texto)
                    string message = DecodeWebSocketFrame(buffer, bytesRead);
                    if (string.IsNullOrEmpty(message))
                    {
                        // Não logar warning para frames vazios - pode ser frame de controle válido
                        continue;
                    }
                    
                    Debug.Log($"📩 Mensagem recebida: {message}");
                    
                    // Se ainda não identificou, tentar identificar
                    if (!identified)
                    {
                        if (message.StartsWith("vr_connected"))
                        {
                            string idStr = message.Substring("vr_connected".Length);
                            if (int.TryParse(idStr, out int id) && id >= 1 && id <= 4)
                            {
                                oculusId = id;
                                identified = true;
                                
                                // Registrar conexão
                                oculusConnections[id] = client;
                                oculusConnected[id] = true;
                                oculusLastPing[id] = DateTime.Now; // Inicializar último ping
                                
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
                        // Já identificado, processar mensagem normalmente
                        if (oculusId.HasValue)
                        {
                            ProcessMessageFromOculus(oculusId.Value, message);
                        }
                    }
                }
                catch (System.IO.IOException ioEx)
                {
                    // Timeout ou conexão fechada - comportamento esperado quando cliente desconecta
                    if (ioEx.Message.Contains("interrupted") || ioEx.Message.Contains("closed"))
                    {
                        Debug.Log($"ℹ️ Conexão encerrada pelo cliente");
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Erro de IO: {ioEx.Message}");
                    }
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Erro ao receber mensagem: {e.Message}");
            Debug.LogError($"❌ StackTrace: {e.StackTrace}");
        }
        finally
        {
            // Limpar conexão quando desconectar
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
        if (length < 2)
        {
            Debug.LogWarning($"⚠️ Frame muito curto: {length} bytes");
            return null;
        }
        
        // Frame WebSocket simples: primeiro byte = opcode, segundo byte = tamanho
        bool fin = (buffer[0] & 0x80) != 0;
        int opcode = buffer[0] & 0x0F;
        bool masked = (buffer[1] & 0x80) != 0;
        int payloadLen = buffer[1] & 0x7F;
        
        Debug.Log($"🔍 Decodificando frame: FIN={fin}, Opcode={opcode:X}, Masked={masked}, PayloadLen={payloadLen}");
        
        int maskStart = 2;
        if (payloadLen == 126)
        {
            if (length < 4) return null;
            payloadLen = (buffer[2] << 8) | buffer[3];
            maskStart = 4;
            Debug.Log($"🔍 Payload length estendido: {payloadLen}");
        }
        else if (payloadLen == 127)
        {
            // Não suporta mensagens muito grandes
            Debug.LogWarning("⚠️ Payload muito grande (127) - não suportado");
            return null;
        }
        
        if (opcode == 0x8)
        {
            Debug.Log("🔍 Close frame recebido");
            return null; // Close frame
        }
        if (opcode == 0x9)
        {
            Debug.Log("🔍 Ping frame recebido - respondendo com pong");
            // Ping recebido - não decodificar, mas retornar null para processar separadamente
            return null; // Ping frame - será processado separadamente
        }
        if (opcode == 0xA)
        {
            Debug.Log("🔍 Pong frame recebido");
            return null; // Pong frame - apenas confirmação
        }
        if (opcode != 0x1)
        {
            Debug.LogWarning($"⚠️ Opcode não é texto: {opcode:X}");
            return null; // Apenas texto
        }
        
        int dataStart = maskStart;
        if (masked)
        {
            if (length < maskStart + 4) return null;
            dataStart += 4; // Skip mask
            byte[] mask = new byte[4];
            Array.Copy(buffer, maskStart, mask, 0, 4);
            
            Debug.Log($"🔍 Mask: {BitConverter.ToString(mask)}");
            
            // Decodificar payload com mask
            for (int i = 0; i < payloadLen && (dataStart + i) < length; i++)
            {
                buffer[dataStart + i] = (byte)(buffer[dataStart + i] ^ mask[i % 4]);
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Frame não está mascarado (cliente deve mascarar)");
            // Clientes sempre devem mascarar, mas vamos tentar decodificar mesmo assim
        }
        
        if (dataStart + payloadLen > length)
        {
            Debug.LogWarning($"⚠️ Payload maior que buffer: {dataStart + payloadLen} > {length}");
            return null;
        }
        
        string message = Encoding.UTF8.GetString(buffer, dataStart, payloadLen);
        Debug.Log($"✅ Mensagem decodificada: '{message}' ({payloadLen} bytes)");
        return message;
    }
    
    void EncodeWebSocketFrame(string message, byte[] output, out int length)
    {
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        int payloadLen = messageBytes.Length;
        
        output[0] = 0x81; // FIN + Text frame
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
        
        // Processar mensagens de bateria: battery{id}:{percent}
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
        // Processar mensagens de percentual: percent{id}:{percent}
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
        // Processar mensagens de vídeo terminado: video_ended{id}
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
        
        // Verificar painéis configurados
        if (oculusPanels == null || oculusPanels.Length < 4)
        {
            Debug.LogError("❌ Painéis não estão configurados corretamente! Deve haver exatamente 4 painéis.");
            return;
        }
        
        Debug.Log($"🔍 Enviando comando play para TODOS os 4 Oculus simultaneamente...");
        
        // Enviar comando play para TODOS os 4 Oculus em paralelo
        List<Task> sendTasks = new List<Task>();
        int sentCount = 0;
        
        for (int i = 0; i < 4; i++)
        {
            int oculusId = i + 1;
            
            // Obter idioma do painel ou usar padrão "pt"
            string language = "pt"; // Padrão
            if (oculusPanels[i] != null)
            {
                string selectedLang = oculusPanels[i].GetSelectedLanguage();
                if (!string.IsNullOrEmpty(selectedLang))
                {
                    language = selectedLang;
                }
                else
                {
                    Debug.LogWarning($"⚠️ Oculus {oculusId} não tem idioma selecionado - usando padrão 'pt'");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ Painel {oculusId} não configurado - usando idioma padrão 'pt'");
            }
            
            string message = $"play{oculusId}_{language}";
            Debug.Log($"🎬 Preparando envio para Oculus {oculusId}: {message}");
            
            // Adicionar task de envio com tratamento de exceção individual (será executada em paralelo)
            sendTasks.Add(SendMessageToOculusSafe(oculusId, message));
            sentCount++;
        }
        
        // Aguardar todos os envios em paralelo
        try
        {
            await Task.WhenAll(sendTasks);
            Debug.Log($"✅✅✅ Comandos play enviados para {sentCount} Oculus simultaneamente!");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Erro ao enviar comandos play: {e.Message}");
        }
        
        // Iniciar preview do vídeo (usar idioma do primeiro painel ou padrão)
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
        Debug.Log("⏹️⏹️⏹️ BOTÃO STOP CLICADO!");
        
        if (!isServerRunning)
        {
            Debug.LogWarning("⚠️ Servidor não está rodando!");
            return;
        }
        
        // FORÇAR isPlaying = false IMEDIATAMENTE
        isPlaying = false;
        Debug.Log($"⏹️ Estado isPlaying definido como FALSE");
        
        // Parar preview IMEDIATAMENTE
        if (videoPreview != null)
        {
            Debug.Log("⏹️ Parando preview do vídeo...");
            videoPreview.StopVideo();
        }
        
        Debug.Log($"🔍 Enviando comando stop para TODOS os 4 Oculus com retry...");
        
        // Enviar comando stop para TODOS os 4 Oculus em paralelo com retry
        List<Task> stopTasks = new List<Task>();
        int sentCount = 0;
        
        for (int i = 0; i < 4; i++)
        {
            int oculusId = i + 1;
            string message = $"stop{oculusId}";
            Debug.Log($"⏹️ Preparando stop para Oculus {oculusId}: {message}");
            
            // Adicionar task de envio com retry (será executada em paralelo)
            stopTasks.Add(SendStopMessageWithRetry(oculusId, message));
            sentCount++;
        }
        
        // Aguardar todos os envios em paralelo
        try
        {
            await Task.WhenAll(stopTasks);
            Debug.Log($"✅✅✅ Comandos stop enviados para {sentCount} Oculus simultaneamente!");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Erro ao enviar comandos stop: {e.Message}");
        }
        
        // Aguardar um pouco para garantir que mensagens foram processadas
        await Task.Delay(100);
        
        // Atualizar botões
        UpdateButtons();
        Debug.Log("✅✅✅ STOP CONCLUÍDO - Estado atualizado");
    }
    
    async Task SendStopMessageWithRetry(int oculusId, string message, int maxRetries = 3)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            // Verificar conexão de forma robusta
            bool isConnected = IsOculusConnected(oculusId);
            
            if (isConnected)
            {
                try
                {
                    await SendMessageToOculus(oculusId, message);
                    Debug.Log($"✅ Stop enviado com sucesso para Oculus {oculusId} (tentativa {attempt})");
                    return; // Sucesso - sair do retry
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"⚠️ Tentativa {attempt}/{maxRetries} falhou para Oculus {oculusId}: {e.Message}");
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(200 * attempt); // Backoff exponencial
                    }
                }
            }
            else
            {
                Debug.Log($"ℹ️ Oculus {oculusId} não está conectado (tentativa {attempt}/{maxRetries})");
                if (attempt < maxRetries)
                {
                    await Task.Delay(300 * attempt); // Aguardar um pouco antes de tentar novamente
                }
            }
        }
        
        Debug.LogWarning($"⚠️ Não foi possível enviar stop para Oculus {oculusId} após {maxRetries} tentativas");
    }
    
    bool IsOculusConnected(int oculusId)
    {
        try
        {
            if (!oculusConnections.ContainsKey(oculusId))
            {
                return false;
            }
            
            TcpClient client = oculusConnections[oculusId];
            if (client == null)
            {
                // Limpar entrada inválida
                oculusConnections.Remove(oculusId);
                oculusConnected[oculusId] = false;
                return false;
            }
            
            // Verificar se está conectado
            if (!client.Connected)
            {
                // Limpar conexão desconectada
                CleanupDisconnectedOculus(oculusId);
                return false;
            }
            
            // Verificar se o socket ainda está válido e ativo
            try
            {
                Socket socket = client.Client;
                if (socket == null)
                {
                    CleanupDisconnectedOculus(oculusId);
                    return false;
                }
                
                // Verificar se o socket está realmente conectado usando Poll
                // Poll com timeout 0 retorna true se há dados ou se a conexão foi fechada
                bool hasData = socket.Poll(0, System.Net.Sockets.SelectMode.SelectRead);
                bool hasNoData = socket.Available == 0;
                
                // Se Poll retorna true mas não há dados, a conexão foi fechada
                if (hasData && hasNoData)
                {
                    CleanupDisconnectedOculus(oculusId);
                    return false;
                }
                
                return true;
            }
            catch
            {
                // Se houver erro ao verificar socket, limpar e considerar desconectado
                CleanupDisconnectedOculus(oculusId);
                return false;
            }
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
            // Remover do dicionário de conexões
            if (oculusConnections.ContainsKey(oculusId))
            {
                TcpClient client = oculusConnections[oculusId];
                try
                {
                    client?.Close();
                }
                catch { }
                oculusConnections.Remove(oculusId);
            }
            
            // Atualizar status
            oculusConnected[oculusId] = false;
            
            // Atualizar UI
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
            Debug.Log($"ℹ️ Não foi possível enviar '{message}' para Oculus {oculusId}: {e.Message}");
        }
    }
    
    async Task SendMessageToOculus(int oculusId, string message)
    {
        // Verificar se Oculus está conectado de forma robusta
        if (!IsOculusConnected(oculusId))
        {
            throw new InvalidOperationException($"Oculus {oculusId} não está conectado");
        }
        
        TcpClient client = oculusConnections[oculusId];
        
        if (client == null)
        {
            throw new InvalidOperationException($"Oculus {oculusId} - conexão não disponível");
        }
        
        Debug.Log($"📤 Enviando mensagem para Oculus {oculusId}: {message}");
        
        NetworkStream stream = client.GetStream();
        if (stream == null)
        {
            throw new InvalidOperationException($"Oculus {oculusId} - stream é null");
        }
        
        if (!stream.CanWrite)
        {
            throw new InvalidOperationException($"Oculus {oculusId} - stream não pode escrever");
        }
        
        byte[] frame = new byte[4096];
        EncodeWebSocketFrame(message, frame, out int length);
        
        await stream.WriteAsync(frame, 0, length);
        Debug.Log($"✅ Mensagem enviada com sucesso para Oculus {oculusId}: {message}");
    }
    
    void UpdateButtons()
    {
        // Verificar se há pelo menos um Oculus conectado
        bool hasConnectedOculus = false;
        for (int i = 1; i <= 4; i++)
        {
            if (oculusConnected.ContainsKey(i) && oculusConnected[i] &&
                oculusConnections.ContainsKey(i) && oculusConnections[i] != null &&
                oculusConnections[i].Connected)
            {
                hasConnectedOculus = true;
                break;
            }
        }
        
        if (startButton != null)
        {
            // Start habilitado quando: servidor rodando, não está tocando, e há Oculus conectado
            startButton.interactable = !isPlaying && isServerRunning && hasConnectedOculus;
        }
        
        if (stopButton != null)
        {
            // Stop habilitado quando: servidor rodando E (está tocando OU há Oculus conectado)
            // Isso permite parar mesmo se o estado isPlaying não estiver sincronizado
            stopButton.interactable = isServerRunning && (isPlaying || hasConnectedOculus);
        }
    }
    
    void Update()
    {
        UpdateButtons();
        
        if (!isServerRunning) return;
        
        float currentTime = Time.time;
        
        // Verificar conexões ativas periodicamente
        if (currentTime - lastConnectionCheck >= CONNECTION_CHECK_INTERVAL)
        {
            CheckActiveConnections();
            lastConnectionCheck = currentTime;
        }
        
        // Enviar ping periódico para manter conexões vivas
        if (currentTime - lastPingTime >= PING_INTERVAL)
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
    // MELHORIAS: Ping periódico e verificação de conexões
    // ============================================================
    
    void CheckActiveConnections()
    {
        List<int> disconnectedOculus = new List<int>();
        
        foreach (var kvp in oculusConnections.ToList())
        {
            int oculusId = kvp.Key;
            TcpClient client = kvp.Value;
            
            if (client == null || !IsOculusConnected(oculusId))
            {
                disconnectedOculus.Add(oculusId);
            }
        }
        
        // Limpar conexões desconectadas
        foreach (int oculusId in disconnectedOculus)
        {
            Debug.LogWarning($"⚠️ Oculus {oculusId} desconectado detectado - limpando...");
            CleanupDisconnectedOculus(oculusId);
        }
    }
    
    async Task SendPingToAllConnected()
    {
        List<Task> pingTasks = new List<Task>();
        
        foreach (var kvp in oculusConnections.ToList())
        {
            int oculusId = kvp.Key;
            TcpClient client = kvp.Value;
            
            if (client != null && IsOculusConnected(oculusId))
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
            pingFrame[0] = 0x89; // FIN + Ping frame (opcode 0x9)
            pingFrame[1] = 0x00; // Payload length 0
            
            await stream.WriteAsync(pingFrame, 0, pingFrame.Length);
            
            // Atualizar último ping enviado
            oculusLastPing[oculusId] = DateTime.Now;
            
            Debug.Log($"📡 Ping enviado para Oculus {oculusId}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ Erro ao enviar ping para Oculus {oculusId}: {e.Message}");
            // Não limpar conexão aqui - pode ser temporário
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
        
        // Fechar todas as conexões
        foreach (var kvp in oculusConnections)
        {
            try
            {
                kvp.Value?.Close();
            }
            catch { }
        }
        
        // Parar servidor TCP
        try
        {
            tcpListener?.Stop();
        }
        catch { }
    }
}

