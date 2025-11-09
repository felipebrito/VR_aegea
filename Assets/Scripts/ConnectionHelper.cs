using UnityEngine;
using System.Collections;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
using System;

/// <summary>
/// Classe auxiliar para verificar conexões e diagnosticar problemas de rede.
/// Útil para quando a aplicação fica presa na tela de "aguardando".
/// </summary>
public class ConnectionHelper : MonoBehaviour
{
    [Header("Referências")]
    public VRManager vrManager;
    
    [Header("Configurações")]
    [Tooltip("Intervalo em segundos para verificar conexão")]
    public float checkInterval = 10f;
    [Tooltip("Tempo em segundos para determinar quando a conexão está presa")]
    public float stuckTimeout = 30f;
    
    [Header("Estados")]
    [SerializeField] private string connectionStatus = "Desconhecido";
    [SerializeField] private float timeWithoutResponse = 0f;
    [SerializeField] private bool networkAvailable = false;
    [SerializeField] private string lastReceivedMessage = "Nenhum";
    [SerializeField] private string localIP = "Desconhecido";
    
    // Evento para notificar quando a conexão está presa
    public System.Action<string> OnConnectionStuck;
    
    void Start()
    {
        if (vrManager == null)
        {
            vrManager = FindObjectOfType<VRManager>();
        }
        
        // Iniciar verificação de rede
        localIP = GetLocalIPAddress();
        Debug.Log($"IP Local: {localIP}");
        
        StartCoroutine(CheckNetworkRoutine());
    }
    
    IEnumerator CheckNetworkRoutine()
    {
        yield return new WaitForSeconds(3f); // Espera inicial
        
        while (true)
        {
            // Verificar disponibilidade da rede
            networkAvailable = CheckNetworkAvailability();
            
            // Verificar problema de conexão presa
            timeWithoutResponse += checkInterval;
            
            if (vrManager != null)
            {
                // Se temos VRManager e ele não está reproduzindo vídeo
                if (!vrManager.isPlaying)
                {
                    // Se passar muito tempo sem comando
                    if (timeWithoutResponse > stuckTimeout)
                    {
                        connectionStatus = "Presa (timeout)";
                        
                        // Notificar sobre o problema
                        OnConnectionStuck?.Invoke("Conexão presa por " + timeWithoutResponse + " segundos");
                        
                        // Sugerir reiniciar conexão
                        Debug.LogWarning("Conexão parece estar presa - sugerindo reinício");
                        
                        // Tentar reiniciar conexão
                        if (vrManager != null && !vrManager.offlineMode)
                        {
                            vrManager.TestConnection();
                        }
                    }
                    else
                    {
                        connectionStatus = "Aguardando";
                    }
                }
                else
                {
                    // Se estiver reproduzindo, reseta o timer
                    timeWithoutResponse = 0;
                    connectionStatus = "Ativa (reproduzindo)";
                }
            }
            
            yield return new WaitForSeconds(checkInterval);
        }
    }
    
    // Verificar se a rede está disponível
    bool CheckNetworkAvailability()
    {
        try
        {
            // Verificar ping para o gateway padrão
            string gateway = GetDefaultGateway();
            if (!string.IsNullOrEmpty(gateway))
            {
                UnityEngine.Ping ping = new UnityEngine.Ping(gateway);
                // Esperamos um curto período para não bloquear a thread
                float startTime = Time.time;
                while (!ping.isDone && Time.time - startTime < 2f)
                {
                    // Espera
                }
                
                if (ping.isDone && ping.time >= 0)
                {
                    return true;
                }
            }
            
            // Alternativa: verificar adaptadores de rede
            return NetworkInterface.GetIsNetworkAvailable();
        }
        catch (Exception e)
        {
            Debug.LogError("Erro ao verificar rede: " + e.Message);
            return false;
        }
    }
    
    // Obter o gateway padrão (roteador)
    string GetDefaultGateway()
    {
        try
        {
            // Obter interfaces de rede
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in interfaces)
            {
                // Verificar apenas interfaces ativas
                if (adapter.OperationalStatus == OperationalStatus.Up)
                {
                    // Obter propriedades IPv4
                    IPInterfaceProperties ipProps = adapter.GetIPProperties();
                    GatewayIPAddressInformationCollection gateways = ipProps.GatewayAddresses;
                    
                    if (gateways.Count > 0)
                    {
                        foreach (GatewayIPAddressInformation gateway in gateways)
                        {
                            // Retorna o primeiro gateway IPv4 encontrado
                            if (gateway.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                return gateway.Address.ToString();
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Erro ao obter gateway: " + e.Message);
        }
        
        // Fallback: gateway padrão comum
        return "192.168.1.1";
    }
    
    // Obter o endereço IP local
    public static string GetLocalIPAddress()
    {
        string localIP = "127.0.0.1";
        try
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Erro ao obter IP local: " + e.Message);
        }
        return localIP;
    }
    
    // Notificar quando uma mensagem é recebida (chamado pelo VRManager)
    public void NotifyMessageReceived(string message)
    {
        timeWithoutResponse = 0;
        lastReceivedMessage = message;
    }
    
    // Método para tentar forçar início do player
    public void ForceStartVideoPlayer(string videoFile = "rio.mp4")
    {
        if (vrManager != null)
        {
            Debug.Log("Forçando início de vídeo via ConnectionHelper: " + videoFile);
            vrManager.TestPlayVideo(videoFile);
        }
        else
        {
            Debug.LogError("VRManager não disponível para iniciar vídeo");
        }
    }
    
    // Método para reiniciar conexão
    public void ForceReconnect()
    {
        if (vrManager != null)
        {
            Debug.Log("Forçando reconexão ao servidor");
            vrManager.TestConnection();
        }
    }
    
    // Método para verificar vídeos disponíveis
    public string[] GetAvailableVideos()
    {
        try
        {
            string externalPath = System.IO.Path.Combine("/sdcard", "Download");
            if (System.IO.Directory.Exists(externalPath))
            {
                return System.IO.Directory.GetFiles(externalPath, "*.mp4");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Erro ao listar vídeos: " + e.Message);
        }
        
        return new string[0];
    }
} 