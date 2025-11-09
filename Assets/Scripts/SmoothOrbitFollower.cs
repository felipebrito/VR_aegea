using UnityEngine;

public class SmoothOrbitFollower : MonoBehaviour
{
    [Header("Follow Settings")]
    [Tooltip("A câmera que o objeto vai seguir")]
    public Transform cameraToFollow;
    
    [Tooltip("Distância que o objeto mantém da câmera")]
    public float orbitDistance = 2f;
    
    [Tooltip("Velocidade de suavização do movimento (quanto maior, mais suave)")]
    public float smoothSpeed = 5f;
    
    [Tooltip("Offset vertical em relação à câmera")]
    public float heightOffset = 0f;

    private Vector3 targetPosition;
    private Vector3 currentVelocity;

    void Start()
    {
        // Se não foi atribuída uma câmera, usar a principal
        if (cameraToFollow == null)
        {
            cameraToFollow = Camera.main.transform;
        }
        
        // Posicionar inicialmente na frente da câmera
        transform.position = cameraToFollow.position + cameraToFollow.forward * orbitDistance;
    }

    void Update()
    {
        if (cameraToFollow == null) return;

        // Calcular a posição alvo na direção do olhar da câmera
        Vector3 cameraForward = cameraToFollow.forward;
        cameraForward.y = 0; // Manter sempre na mesma altura se desejado
        cameraForward = cameraForward.normalized;

        // Calcular a posição alvo
        targetPosition = cameraToFollow.position + cameraForward * orbitDistance;
        
        // Adicionar offset de altura
        targetPosition.y += heightOffset;

        // Suavizar o movimento usando SmoothDamp
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref currentVelocity,
            1f / smoothSpeed
        );

        // Fazer o objeto sempre olhar para a câmera
        transform.LookAt(cameraToFollow);
    }

    // Método para visualizar o raio de órbita no editor
    void OnDrawGizmosSelected()
    {
        if (cameraToFollow == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(cameraToFollow.position, orbitDistance);
    }
} 