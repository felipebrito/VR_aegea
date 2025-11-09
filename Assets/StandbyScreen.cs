using UnityEngine;

using UnityEngine.Video;
public class StandbyScreen : MonoBehaviour
{

    public VideoPlayer video;
    public MeshRenderer mesh;

     public GameObject standBy;
    public GameObject particles;
    // Update is called once per frame
    void Update()
    {
         mesh.enabled = video.isPlaying;
        particles.SetActive(!video.isPlaying);
         standBy.SetActive(!video.isPlaying);
    }
}
