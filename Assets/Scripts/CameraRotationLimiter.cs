using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
public class CameraRotationLimiter : MonoBehaviour
{
    public bool IsLimitActive = false;
    public float resetSpeed = 2f; // Speed to reset the sphere to face the camera.
    public Transform sphereTransform; // Reference to the sphere containing the camera.
    public float angle = 75f;

    private float currentRotationY;
    private Quaternion initialRotation;
    public VideoPlayer videoPlayer;
   // public string videoName = "Videos/amazonia.mp4";

    void Start()
    {
        if (sphereTransform == null)
        {
            Debug.LogError("Sphere Transform is not assigned!");
        }

        initialRotation = transform.rotation;

    
    }

    void Update()
    {
#if UNITY_EDITOR || !UNITY_ANDROID || !PLATFORM_ANDROID
        // Allow rotation control with mouse in the editor
        float mouseX = Input.GetAxis("Mouse X");
        transform.Rotate(0, mouseX * 2f, 0);

      
        sphereTransform.gameObject.SetActive(videoPlayer.isPlaying);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if(videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
            }
            else
            {
                videoPlayer.Play();
            }
        }


#endif

        if (IsLimitActive && sphereTransform != null)
        {
            // Get current local Y rotation
            currentRotationY = transform.localEulerAngles.y;

            if (currentRotationY > 180) // Normalize angle for comparison
                currentRotationY -= 360;

            // Check if rotation exceeds the limit
            if (Mathf.Abs(currentRotationY) > angle)
            {
                // Maintain fixed rotation for X and Z
                Quaternion targetRotation = Quaternion.Euler(-90, transform.localEulerAngles.y, -180);

                // Rotate the sphere to recentralize in front of the camera
                sphereTransform.rotation = Quaternion.Lerp(sphereTransform.rotation, targetRotation, Time.deltaTime * resetSpeed);
            }
        }
    }
}
