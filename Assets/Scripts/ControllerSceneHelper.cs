using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Script auxiliar para criar a UI da cena do controlador dos óculos.
/// Execute este script uma vez no Unity Editor para criar a UI automaticamente.
/// </summary>
public class ControllerSceneHelper : MonoBehaviour
{
    [ContextMenu("Create Controller Scene UI")]
    public void CreateControllerSceneUI()
    {
        // Criar Canvas principal
        Canvas canvas = FindOrCreateCanvas();
        
        // Criar controlador principal
        GameObject controllerObj = new GameObject("OculusController");
        OculusController controller = controllerObj.AddComponent<OculusController>();
        
        // Criar painéis dos 4 Oculus
        OculusPanel[] panels = new OculusPanel[4];
        for (int i = 0; i < 4; i++)
        {
            panels[i] = CreateOculusPanel(canvas, i + 1);
        }
        controller.oculusPanels = panels;
        
        // Criar botões de controle
        CreateControlButtons(canvas, controller);
        
        // Criar preview do vídeo
        CreateVideoPreview(canvas, controller);
        
        Debug.Log("✅ Cena do controlador criada com sucesso!");
    }
    
    Canvas FindOrCreateCanvas()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Garantir que há um EventSystem (necessário para botões clicáveis)
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("✅ EventSystem criado para permitir cliques nos botões");
        }
        
        return canvas;
    }
    
    OculusPanel CreateOculusPanel(Canvas canvas, int oculusId)
    {
        // Criar painel principal
        GameObject panelObj = new GameObject($"OculusPanel_{oculusId}");
        panelObj.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f + (oculusId - 1) * 0.2f, 0.5f);
        panelRect.anchorMax = new Vector2(0.1f + (oculusId - 1) * 0.2f, 0.5f);
        panelRect.sizeDelta = new Vector2(300, 400);
        panelRect.anchoredPosition = Vector2.zero;
        
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        OculusPanel panel = panelObj.AddComponent<OculusPanel>();
        panel.SetOculusId(oculusId);
        
        // Criar ID do Oculus
        GameObject idObj = new GameObject("OculusID");
        idObj.transform.SetParent(panelObj.transform, false);
        TextMeshProUGUI idText = idObj.AddComponent<TextMeshProUGUI>();
        idText.text = oculusId.ToString();
        idText.fontSize = 48;
        idText.color = Color.white;
        idText.alignment = TextAlignmentOptions.Center;
        RectTransform idRect = idObj.GetComponent<RectTransform>();
        idRect.anchorMin = new Vector2(0, 0.85f);
        idRect.anchorMax = new Vector2(1, 1);
        idRect.sizeDelta = Vector2.zero;
        panel.oculusIdText = idText;
        
        // Criar botões de idioma
        string[] languages = { "pt", "en", "es" };
        Button[] langButtons = new Button[3];
        TextMeshProUGUI[] langTexts = new TextMeshProUGUI[3];
        
        for (int i = 0; i < 3; i++)
        {
            GameObject btnObj = new GameObject($"LanguageButton_{languages[i]}");
            btnObj.transform.SetParent(panelObj.transform, false);
            
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(i * 0.33f, 0.6f);
            btnRect.anchorMax = new Vector2((i + 1) * 0.33f, 0.75f);
            btnRect.sizeDelta = Vector2.zero;
            btnRect.offsetMin = new Vector2(5, 0);
            btnRect.offsetMax = new Vector2(-5, 0);
            
            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = Color.white;
            
            Button btn = btnObj.AddComponent<Button>();
            langButtons[i] = btn;
            
            GameObject btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnText.text = languages[i].ToUpper();
            btnText.fontSize = 24;
            btnText.color = Color.black;
            btnText.alignment = TextAlignmentOptions.Center;
            RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.sizeDelta = Vector2.zero;
            langTexts[i] = btnText;
        }
        
        panel.languageButtons = langButtons;
        panel.languageButtonTexts = langTexts;
        
        // Criar status online/offline
        GameObject statusObj = new GameObject("Status");
        statusObj.transform.SetParent(panelObj.transform, false);
        Image statusIcon = statusObj.AddComponent<Image>();
        statusIcon.color = Color.red;
        RectTransform statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 0.4f);
        statusRect.anchorMax = new Vector2(0.5f, 0.55f);
        statusRect.sizeDelta = Vector2.zero;
        panel.statusIcon = statusIcon;
        
        GameObject statusTextObj = new GameObject("StatusText");
        statusTextObj.transform.SetParent(panelObj.transform, false);
        TextMeshProUGUI statusText = statusTextObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "OFFLINE";
        statusText.fontSize = 20;
        statusText.color = Color.red;
        statusText.alignment = TextAlignmentOptions.Center;
        RectTransform statusTextRect = statusTextObj.GetComponent<RectTransform>();
        statusTextRect.anchorMin = new Vector2(0.5f, 0.4f);
        statusTextRect.anchorMax = new Vector2(1, 0.55f);
        statusTextRect.sizeDelta = Vector2.zero;
        panel.statusText = statusText;
        
        // Criar bateria
        GameObject batteryObj = new GameObject("Battery");
        batteryObj.transform.SetParent(panelObj.transform, false);
        Image batteryIcon = batteryObj.AddComponent<Image>();
        batteryIcon.color = Color.green;
        batteryIcon.type = Image.Type.Filled;
        batteryIcon.fillMethod = Image.FillMethod.Horizontal;
        RectTransform batteryRect = batteryObj.GetComponent<RectTransform>();
        batteryRect.anchorMin = new Vector2(0, 0.2f);
        batteryRect.anchorMax = new Vector2(0.5f, 0.35f);
        batteryRect.sizeDelta = Vector2.zero;
        panel.batteryIcon = batteryIcon;
        
        GameObject batteryTextObj = new GameObject("BatteryText");
        batteryTextObj.transform.SetParent(panelObj.transform, false);
        TextMeshProUGUI batteryText = batteryTextObj.AddComponent<TextMeshProUGUI>();
        batteryText.text = "0%";
        batteryText.fontSize = 20;
        batteryText.color = Color.white;
        batteryText.alignment = TextAlignmentOptions.Center;
        RectTransform batteryTextRect = batteryTextObj.GetComponent<RectTransform>();
        batteryTextRect.anchorMin = new Vector2(0.5f, 0.2f);
        batteryTextRect.anchorMax = new Vector2(1, 0.35f);
        batteryTextRect.sizeDelta = Vector2.zero;
        panel.batteryText = batteryText;
        
        return panel;
    }
    
    void CreateControlButtons(Canvas canvas, OculusController controller)
    {
        // Botão Iniciar
        GameObject startBtnObj = new GameObject("StartButton");
        startBtnObj.transform.SetParent(canvas.transform, false);
        RectTransform startRect = startBtnObj.AddComponent<RectTransform>();
        startRect.anchorMin = new Vector2(0.7f, 0.8f);
        startRect.anchorMax = new Vector2(0.85f, 0.9f);
        startRect.sizeDelta = Vector2.zero;
        
        Image startImage = startBtnObj.AddComponent<Image>();
        startImage.color = new Color(0.2f, 0.6f, 0.2f, 1f);
        
        Button startButton = startBtnObj.AddComponent<Button>();
        controller.startButton = startButton;
        
        GameObject startTextObj = new GameObject("Text");
        startTextObj.transform.SetParent(startBtnObj.transform, false);
        TextMeshProUGUI startText = startTextObj.AddComponent<TextMeshProUGUI>();
        startText.text = "INICIAR";
        startText.fontSize = 32;
        startText.color = Color.white;
        startText.alignment = TextAlignmentOptions.Center;
        RectTransform startTextRect = startTextObj.GetComponent<RectTransform>();
        startTextRect.anchorMin = Vector2.zero;
        startTextRect.anchorMax = Vector2.one;
        startTextRect.sizeDelta = Vector2.zero;
        
        // Botão Parar
        GameObject stopBtnObj = new GameObject("StopButton");
        stopBtnObj.transform.SetParent(canvas.transform, false);
        RectTransform stopRect = stopBtnObj.AddComponent<RectTransform>();
        stopRect.anchorMin = new Vector2(0.7f, 0.7f);
        stopRect.anchorMax = new Vector2(0.85f, 0.8f);
        stopRect.sizeDelta = Vector2.zero;
        
        Image stopImage = stopBtnObj.AddComponent<Image>();
        stopImage.color = new Color(0.6f, 0.2f, 0.2f, 1f);
        
        Button stopButton = stopBtnObj.AddComponent<Button>();
        controller.stopButton = stopButton;
        
        GameObject stopTextObj = new GameObject("Text");
        stopTextObj.transform.SetParent(stopBtnObj.transform, false);
        TextMeshProUGUI stopText = stopTextObj.AddComponent<TextMeshProUGUI>();
        stopText.text = "PARAR";
        stopText.fontSize = 32;
        stopText.color = Color.white;
        stopText.alignment = TextAlignmentOptions.Center;
        RectTransform stopTextRect = stopTextObj.GetComponent<RectTransform>();
        stopTextRect.anchorMin = Vector2.zero;
        stopTextRect.anchorMax = Vector2.one;
        stopTextRect.sizeDelta = Vector2.zero;
    }
    
    void CreateVideoPreview(Canvas canvas, OculusController controller)
    {
        // Criar objeto do preview
        GameObject previewObj = new GameObject("VideoPreview");
        previewObj.transform.SetParent(canvas.transform, false);
        
        RectTransform previewRect = previewObj.AddComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0.7f, 0.2f);
        previewRect.anchorMax = new Vector2(0.95f, 0.65f);
        previewRect.sizeDelta = Vector2.zero;
        
        // Criar VideoPlayer
        GameObject videoPlayerObj = new GameObject("VideoPlayer");
        videoPlayerObj.transform.SetParent(previewObj.transform, false);
        UnityEngine.Video.VideoPlayer videoPlayer = videoPlayerObj.AddComponent<UnityEngine.Video.VideoPlayer>();
        
        // Criar RawImage para preview
        GameObject rawImageObj = new GameObject("PreviewImage");
        rawImageObj.transform.SetParent(previewObj.transform, false);
        RawImage rawImage = rawImageObj.AddComponent<RawImage>();
        RectTransform rawImageRect = rawImageObj.GetComponent<RectTransform>();
        rawImageRect.anchorMin = Vector2.zero;
        rawImageRect.anchorMax = new Vector2(1, 0.85f);
        rawImageRect.sizeDelta = Vector2.zero;
        
        // Criar slider de progresso
        GameObject sliderObj = new GameObject("ProgressSlider");
        sliderObj.transform.SetParent(previewObj.transform, false);
        Slider slider = sliderObj.AddComponent<Slider>();
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0, 0.1f);
        sliderRect.anchorMax = new Vector2(1, 0.15f);
        sliderRect.sizeDelta = Vector2.zero;
        
        // Criar textos de tempo
        GameObject currentTimeObj = new GameObject("CurrentTime");
        currentTimeObj.transform.SetParent(previewObj.transform, false);
        TextMeshProUGUI currentTimeText = currentTimeObj.AddComponent<TextMeshProUGUI>();
        currentTimeText.text = "00:00";
        currentTimeText.fontSize = 20;
        currentTimeText.color = Color.white;
        RectTransform currentTimeRect = currentTimeObj.GetComponent<RectTransform>();
        currentTimeRect.anchorMin = new Vector2(0, 0);
        currentTimeRect.anchorMax = new Vector2(0.5f, 0.1f);
        currentTimeRect.sizeDelta = Vector2.zero;
        
        GameObject totalTimeObj = new GameObject("TotalTime");
        totalTimeObj.transform.SetParent(previewObj.transform, false);
        TextMeshProUGUI totalTimeText = totalTimeObj.AddComponent<TextMeshProUGUI>();
        totalTimeText.text = "00:00";
        totalTimeText.fontSize = 20;
        totalTimeText.color = Color.white;
        totalTimeText.alignment = TextAlignmentOptions.Right;
        RectTransform totalTimeRect = totalTimeObj.GetComponent<RectTransform>();
        totalTimeRect.anchorMin = new Vector2(0.5f, 0);
        totalTimeRect.anchorMax = new Vector2(1, 0.1f);
        totalTimeRect.sizeDelta = Vector2.zero;
        
        // Configurar VideoPreview
        VideoPreview videoPreview = previewObj.AddComponent<VideoPreview>();
        videoPreview.videoPlayer = videoPlayer;
        videoPreview.previewImage = rawImage;
        videoPreview.progressSlider = slider;
        videoPreview.currentTimeText = currentTimeText;
        videoPreview.totalTimeText = totalTimeText;
        
        controller.videoPreview = videoPreview;
    }
}

