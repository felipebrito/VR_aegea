using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

/// <summary>
/// Componente para exibir preview do vídeo no tablet.
/// Sincroniza com o progresso dos Oculus headsets.
/// </summary>
public class VideoPreview : MonoBehaviour
{
    [Header("Video Player")]
    [Tooltip("VideoPlayer para reproduzir o preview")]
    public VideoPlayer videoPlayer;
    
    [Header("UI Elements")]
    [Tooltip("Imagem de preview (RawImage)")]
    public RawImage previewImage;
    
    [Tooltip("Slider de progresso")]
    public Slider progressSlider;
    
    [Tooltip("Texto do tempo atual")]
    public TextMeshProUGUI currentTimeText;
    
    [Tooltip("Texto do tempo total")]
    public TextMeshProUGUI totalTimeText;
    
    [Header("Video Settings")]
    [Tooltip("Mapeamento de idiomas para arquivos de vídeo")]
    public Dictionary<string, string> videoLanguageMap = new Dictionary<string, string>();
    
    [Tooltip("Usar vídeos comprimidos para tablet (sufixo _tablet)")]
    public bool useCompressedVideos = true;
    
    private RenderTexture renderTexture;
    private bool isPlaying = false;
    private int currentProgress = 0;
    
    void Start()
    {
        try
        {
            Debug.Log("🎬 VideoPreview.Start() - Iniciando...");
            InitializeVideoLanguageMap();
            SetupVideoPlayer();
            Debug.Log("✅ VideoPreview.Start() - Concluído");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ERRO em VideoPreview.Start(): {e.Message}");
            Debug.LogError($"❌ StackTrace: {e.StackTrace}");
        }
    }
    
    void InitializeVideoLanguageMap()
    {
        videoLanguageMap.Clear();
        // Usar apenas um vídeo comprimido para preview (todos os idiomas usam o mesmo vídeo)
        if (useCompressedVideos)
        {
            // Todos os idiomas usam o mesmo vídeo comprimido
            videoLanguageMap.Add("pt", "Experiencia_Aegea_Cop2025_Preview_tablet.mp4");
            videoLanguageMap.Add("en", "Experiencia_Aegea_Cop2025_Preview_tablet.mp4");
            videoLanguageMap.Add("es", "Experiencia_Aegea_Cop2025_Preview_tablet.mp4");
        }
        else
        {
            // Vídeos originais (fallback)
            videoLanguageMap.Add("pt", "Experiencia_Aegea_Cop2025_Portugues.mp4");
            videoLanguageMap.Add("en", "Experiencia_Aegea_Cop2025_Ingles.mp4");
            videoLanguageMap.Add("es", "Experiencia_Aegea_Cop2025_Espannhol.mp4");
        }
    }
    
    void SetupVideoPlayer()
    {
        try
        {
            if (videoPlayer == null)
            {
                videoPlayer = GetComponent<VideoPlayer>();
                if (videoPlayer == null)
                {
                    Debug.LogError("❌ VideoPlayer não encontrado!");
                    return;
                }
            }
            
            // Criar RenderTexture se necessário
            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(1920, 1080, 0);
                videoPlayer.targetTexture = renderTexture;
            }
            
            // Configurar preview image
            if (previewImage != null && renderTexture != null)
            {
                previewImage.texture = renderTexture;
            }
            
            // Configurar eventos
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.loopPointReached += OnVideoEnded;
            
            Debug.Log("✅ VideoPlayer configurado com sucesso");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ERRO em SetupVideoPlayer(): {e.Message}");
            Debug.LogError($"❌ StackTrace: {e.StackTrace}");
        }
    }
    
    public void PlayVideo(string language)
    {
        if (string.IsNullOrEmpty(language))
        {
            Debug.LogError("❌ Idioma não especificado!");
            return;
        }
        
        language = language.ToLower().Trim();
        
        if (!videoLanguageMap.ContainsKey(language))
        {
            Debug.LogError($"❌ Idioma '{language}' não encontrado!");
            return;
        }
        
        string videoFileName = videoLanguageMap[language];
        Debug.Log($"🎬 Iniciando preview: {videoFileName}");
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        // No Android, copiar de StreamingAssets para armazenamento interno
        StartCoroutine(LoadVideoFromStreamingAssets(videoFileName));
        #else
        // Buscar arquivo de vídeo
        string videoPath = FindVideoFile(videoFileName);
        
        if (string.IsNullOrEmpty(videoPath))
        {
            Debug.LogError($"❌ Arquivo de vídeo não encontrado: {videoFileName}");
            return;
        }
        
        // Configurar e tocar o vídeo
        if (videoPlayer != null)
        {
            videoPlayer.url = videoPath;
            videoPlayer.Prepare();
        }
        #endif
    }
    
    #if UNITY_ANDROID && !UNITY_EDITOR
    IEnumerator LoadVideoFromStreamingAssets(string fileName)
    {
        string streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, "Tablet", fileName);
        string persistentPath = Path.Combine(Application.persistentDataPath, fileName);
        
        Debug.Log($"🔍 StreamingAssets: {streamingAssetsPath}");
        Debug.Log($"🔍 Persistent: {persistentPath}");
        
        // Verificar se já existe no armazenamento interno
        if (File.Exists(persistentPath))
        {
            Debug.Log($"✅ Vídeo já existe em persistentDataPath: {persistentPath}");
            ConfigureAndPlayVideo("file://" + persistentPath, fileName);
            yield break;
        }
        
        // Copiar de StreamingAssets para persistentDataPath
        Debug.Log($"📋 Copiando vídeo de StreamingAssets para armazenamento interno...");
        
        using (UnityWebRequest www = UnityWebRequest.Get(streamingAssetsPath))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    File.WriteAllBytes(persistentPath, www.downloadHandler.data);
                    Debug.Log($"✅ Vídeo copiado com sucesso: {persistentPath}");
                    ConfigureAndPlayVideo("file://" + persistentPath, fileName);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ Erro ao salvar vídeo: {e.Message}");
                    // Tentar usar StreamingAssets diretamente como fallback
                    Debug.LogWarning("⚠️ Tentando usar StreamingAssets diretamente...");
                    ConfigureAndPlayVideo(streamingAssetsPath, fileName);
                }
            }
            else
            {
                Debug.LogError($"❌ Erro ao carregar vídeo: {www.error}");
                // Tentar usar StreamingAssets diretamente como fallback
                Debug.LogWarning("⚠️ Tentando usar StreamingAssets diretamente...");
                ConfigureAndPlayVideo(streamingAssetsPath, fileName);
            }
        }
    }
    #endif
    
    void ConfigureAndPlayVideo(string videoPath, string videoFileName)
    {
        if (videoPlayer == null)
        {
            Debug.LogError("❌ VideoPlayer não encontrado!");
            return;
        }
        
        // Parar vídeo atual se estiver tocando
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        
        videoPlayer.url = videoPath;
        videoPlayer.source = VideoSource.Url;
        Debug.Log($"📹 Configurando vídeo: {videoPath}");
        videoPlayer.Prepare();
    }
    
    string FindVideoFile(string fileName)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        // No Android, StreamingAssets está dentro do APK
        // Tentar primeiro StreamingAssets diretamente (funciona com VideoPlayer)
        if (useCompressedVideos)
        {
            // Tentar vídeo comprimido primeiro
            string compressedPath = Path.Combine(Application.streamingAssetsPath, "Tablet", fileName);
            Debug.Log($"🔍 Tentando vídeo comprimido: {compressedPath}");
            // No Android, não podemos usar File.Exists com StreamingAssets, mas VideoPlayer consegue acessar
            return compressedPath;
        }
        else
        {
            // Usar vídeo original
            string originalPath = Path.Combine(Application.streamingAssetsPath, fileName);
            Debug.Log($"🔍 Tentando vídeo original: {originalPath}");
            return originalPath;
        }
        #else
        // No Editor ou outras plataformas, usar File.Exists
        if (useCompressedVideos)
        {
            // PRIORIDADE 1: Tentar vídeo comprimido (tablet)
            string[] compressedPaths = {
                Path.Combine(Application.streamingAssetsPath, "Tablet", fileName),
                Path.Combine(Application.streamingAssetsPath, fileName),
                Path.Combine(Application.dataPath, "StreamingAssets", "Tablet", fileName),
                Path.Combine(Application.dataPath, "StreamingAssets", fileName)
            };
            
            foreach (string path in compressedPaths)
            {
                if (File.Exists(path))
                {
                    Debug.Log($"✅ Vídeo comprimido encontrado: {path}");
                    return path;
                }
            }
            
            // Se não encontrou comprimido, tentar vídeo original como fallback
            string originalFileName = fileName.Replace("_Preview_tablet.mp4", ".mp4").Replace("_tablet.mp4", ".mp4");
            Debug.LogWarning($"⚠️ Vídeo comprimido não encontrado: {fileName}. Tentando versão original: {originalFileName}");
            
            string[] fallbackPaths = {
                Path.Combine(Application.streamingAssetsPath, originalFileName),
                Path.Combine(Application.streamingAssetsPath, "Videos", originalFileName),
                Path.Combine(Application.dataPath, "Videos", originalFileName)
            };
            
            foreach (string path in fallbackPaths)
            {
                if (File.Exists(path))
                {
                    Debug.LogWarning($"⚠️ Usando vídeo original (fallback): {path}");
                    return path;
                }
            }
        }
        else
        {
            // Se não usar comprimidos, procurar vídeos originais
            string[] searchPaths = {
                Path.Combine(Application.streamingAssetsPath, fileName),
                Path.Combine(Application.streamingAssetsPath, "Videos", fileName),
                Path.Combine(Application.dataPath, "Videos", fileName)
            };
            
            foreach (string path in searchPaths)
            {
                if (File.Exists(path))
                {
                    Debug.Log($"✅ Vídeo encontrado: {path}");
                    return path;
                }
            }
        }
        
        // Último recurso: tentar usar StreamingAssets diretamente
        string streamingPath = useCompressedVideos 
            ? Path.Combine(Application.streamingAssetsPath, "Tablet", fileName)
            : Path.Combine(Application.streamingAssetsPath, fileName);
        Debug.LogWarning($"⚠️ Usando caminho StreamingAssets: {streamingPath}");
        return streamingPath;
        #endif
    }
    
    void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log("✅ Vídeo preparado para preview");
        
        if (videoPlayer != null)
        {
            videoPlayer.Play();
            isPlaying = true;
            
            // Atualizar tempo total
            if (totalTimeText != null)
            {
                float totalSeconds = (float)videoPlayer.length;
                totalTimeText.text = FormatTime(totalSeconds);
            }
        }
    }
    
    void OnVideoEnded(VideoPlayer vp)
    {
        Debug.Log("🎬 Preview terminado");
        isPlaying = false;
        
        if (progressSlider != null)
        {
            progressSlider.value = 1f;
        }
    }
    
    public void StopVideo()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
            isPlaying = false;
        }
        
        if (progressSlider != null)
        {
            progressSlider.value = 0f;
        }
        
        if (currentTimeText != null)
        {
            currentTimeText.text = "00:00";
        }
    }
    
    public void UpdateProgress(int percent)
    {
        currentProgress = Mathf.Clamp(percent, 0, 100);
        
        if (progressSlider != null && videoPlayer != null && videoPlayer.length > 0)
        {
            progressSlider.value = currentProgress / 100f;
            
            // Atualizar tempo atual baseado no percentual
            float currentTime = (float)videoPlayer.length * (currentProgress / 100f);
            if (currentTimeText != null)
            {
                currentTimeText.text = FormatTime(currentTime);
            }
        }
    }
    
    string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{secs:00}";
    }
    
    void Update()
    {
        // Atualizar progresso baseado no tempo do vídeo se estiver tocando
        if (isPlaying && videoPlayer != null && videoPlayer.isPlaying && videoPlayer.length > 0)
        {
            float currentTime = (float)videoPlayer.time;
            float totalTime = (float)videoPlayer.length;
            
            if (progressSlider != null)
            {
                progressSlider.value = currentTime / totalTime;
            }
            
            if (currentTimeText != null)
            {
                currentTimeText.text = FormatTime(currentTime);
            }
        }
    }
    
    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            renderTexture = null;
        }
    }
}

