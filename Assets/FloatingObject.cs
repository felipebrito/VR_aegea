using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    public float amplitude = 0.5f;  // Altura da flutuação
    public float frequency = 1f;    // Velocidade da flutuação

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position; // Armazena a posição inicial
    }

    void Update()
    {
        if (startPos == null) return;

        float newY = startPos.y + Mathf.Sin(Time.time * frequency) * amplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
