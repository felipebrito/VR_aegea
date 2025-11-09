using UnityEngine;
using UnityEngine.Video;
using System.Collections;

// Adicione essa diretiva para os trechos que usam o SDK do Oculus
#if USING_OCULUS_SDK
using Oculus.VR;
#endif

/// <summary>
/// Script auxiliar para configurar e gerenciar os objetos necessários para a restrição de visualização.
/// Deve ser adicionado à cena e garante que a estrutura correta esteja configurada.
/// </summary>
public class ViewSetup : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Referência ao gerenciador VR")]
    public VRManager vrManager;
    [Tooltip("Objeto que contém a esfera de vídeo 360")]
    public Transform videoPlayerObject;
    
    [Header("Debug Settings")]
    [Tooltip("Ativar ferramentas de visualização para configuração de bloqueios")]
    public bool enableDebugTools = true;
    [Tooltip("Mostrar pontos cardeais para orientação")]
    public bool showCardinalPoints = true;
    [Tooltip("Mostrar ângulos atuais da câmera")]
    public bool showCurrentAngles = true;
    
    [Header("Current View")]
    [ReadOnly]
    public float currentYaw = 0f;
    [ReadOnly]
    public float currentPitch = 0f;
    
    // Referência à câmera principal
    private Transform cameraTransform;
    // Referência ao centro da câmera (para VR)
    private Transform centerEyeAnchor;
    // Tempo desde que o último valor foi copiado para a área de transferência
    private float clipboardCooldown = 0f;
    
    void Start()
    {
        // Se vrManager não está configurado, tenta encontrar
        if (vrManager == null)
        {
            vrManager = FindObjectOfType<VRManager>();
            if (vrManager != null)
            {
                Debug.Log("VRManager encontrado automaticamente");
            }
            else
            {
                Debug.LogWarning("VRManager não encontrado! Funcionalidades do ViewSetup serão limitadas");
            }
        }
        
        // Se videoPlayerObject não está configurado, tenta pegar do VRManager
        if (videoPlayerObject == null && vrManager != null && vrManager.videoSphere != null)
        {
            videoPlayerObject = vrManager.videoSphere;
            Debug.Log("Video Sphere encontrada no VRManager: " + videoPlayerObject.name);
        }
        
        // Encontrar a câmera - primeiro tenta encontrar o centro do olho do Oculus
        #if UNITY_ANDROID && !UNITY_EDITOR && USING_OCULUS_SDK
        // Tenta encontrar o OVRCameraRig para Quest/GearVR
        var rig = FindObjectOfType<OVRCameraRig>();
        if (rig != null && rig.centerEyeAnchor != null)
        {
            centerEyeAnchor = rig.centerEyeAnchor;
            cameraTransform = centerEyeAnchor;
            Debug.Log("Usando centro do olho do Oculus VR");
        }
        #endif
        
        // Se não encontrou câmera VR, usa a câmera padrão
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main ? Camera.main.transform : null;
            
            if (cameraTransform == null)
            {
                Debug.LogWarning("Câmera principal não encontrada!");
                Camera[] cameras = FindObjectsOfType<Camera>();
                if (cameras.Length > 0)
                {
                    cameraTransform = cameras[0].transform;
                    Debug.Log("Usando câmera alternativa: " + cameras[0].name);
                }
            }
        }
        
        // Configurar o objeto do vídeo no VRManager, se necessário
        if (vrManager != null && videoPlayerObject != null && vrManager.videoSphere == null)
        {
            vrManager.videoSphere = videoPlayerObject;
            Debug.Log("Video Sphere configurada no VRManager");
        }
    }
    
    void Update()
    {
        // Atualizar ângulos atuais da câmera
        if (enableDebugTools && cameraTransform != null)
        {
            UpdateCurrentAngles();
            
            // Debug - copiar ângulos atuais para área de transferência ao pressionar tecla específica
            #if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.C) && clipboardCooldown <= 0f)
            {
                string angleData = $"Yaw: {currentYaw:F1}, Pitch: {currentPitch:F1}";
                GUIUtility.systemCopyBuffer = angleData;
                Debug.Log("Copiado para área de transferência: " + angleData);
                clipboardCooldown = 1f;
            }
            
            if (clipboardCooldown > 0f)
            {
                clipboardCooldown -= Time.deltaTime;
            }
            #endif
            
            // Para dispositivos VR/Quest, verifica botão do controle para copiar ângulos
            #if UNITY_ANDROID && !UNITY_EDITOR && USING_OCULUS_SDK
            if (OVRInput.GetDown(OVRInput.Button.One)) // Botão A no controle do Quest
            {
                string angleData = $"Yaw: {currentYaw:F1}, Pitch: {currentPitch:F1}";
                Debug.Log("Ângulos atuais: " + angleData);
            }
            #endif
        }
    }
    
    // Atualizar ângulos da câmera em relação ao norte/frente
    void UpdateCurrentAngles()
    {
        if (cameraTransform != null)
        {
            // Calcular ângulos esféricos da direção de visão
            Vector3 forward = cameraTransform.forward;
            
            // Calcular yaw (ângulo horizontal) - 0 = norte, 90 = leste, 180 = sul, 270 = oeste
            currentYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            if (currentYaw < 0) currentYaw += 360f;
            
            // Calcular pitch (ângulo vertical) - 0 = horizonte, 90 = cima, -90 = baixo
            currentPitch = Mathf.Asin(forward.y) * Mathf.Rad2Deg;
            
            // Exibir valores no console para debug
            if (showCurrentAngles && Time.frameCount % 30 == 0) // A cada 30 frames para não sobrecarregar
            {
                Debug.Log($"Ângulos atuais: Yaw={currentYaw:F1}° (horizontal), Pitch={currentPitch:F1}° (vertical)");
            }
        }
    }
    
    // Desenhar gizmos para visualização dos pontos cardeais e ângulos
    void OnDrawGizmos()
    {
        if (!enableDebugTools || !showCardinalPoints) return;
        
        if (videoPlayerObject != null)
        {
            float radius = 10f; // Raio para os gizmos
            Vector3 center = videoPlayerObject.position;
            
            // Desenhar ponto Norte - Vermelho
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(center + Vector3.forward * radius, 0.2f);
            Gizmos.DrawLine(center, center + Vector3.forward * radius);
            
            // Desenhar ponto Leste - Verde
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(center + Vector3.right * radius, 0.2f);
            Gizmos.DrawLine(center, center + Vector3.right * radius);
            
            // Desenhar ponto Sul - Azul
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(center + Vector3.back * radius, 0.2f);
            Gizmos.DrawLine(center, center + Vector3.back * radius);
            
            // Desenhar ponto Oeste - Amarelo
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(center + Vector3.left * radius, 0.2f);
            Gizmos.DrawLine(center, center + Vector3.left * radius);
            
            // Desenhar ponto Cima - Branco
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(center + Vector3.up * radius, 0.2f);
            Gizmos.DrawLine(center, center + Vector3.up * radius);
            
            // Desenhar ponto Baixo - Cinza
            Gizmos.color = Color.gray;
            Gizmos.DrawSphere(center + Vector3.down * radius, 0.2f);
            Gizmos.DrawLine(center, center + Vector3.down * radius);
        }
    }
    
    // Método para teste - fixar a visão em um ângulo específico
    public void TestLookAtAngle(float yaw, float pitch)
    {
        if (vrManager != null && vrManager.videoSphere != null)
        {
            Debug.Log($"Testando visão fixa em Yaw={yaw}°, Pitch={pitch}°");
            
            // Criar rotação com os ângulos especificados
            Quaternion targetYawRotation = Quaternion.Euler(0, yaw, 0);
            Quaternion targetPitchRotation = Quaternion.Euler(pitch, 0, 0);
            
            // Aplicar a rotação inversa à esfera de vídeo
            vrManager.videoSphere.rotation = Quaternion.Inverse(targetYawRotation * targetPitchRotation);
        }
    }
}

// Classe auxiliar para permitir exibir variáveis como somente leitura no Inspector
public class ReadOnlyAttribute : PropertyAttribute
{
} 