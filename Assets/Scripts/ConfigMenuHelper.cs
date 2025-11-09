using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Script auxiliar para criar a UI do menu de configuração de IP.
/// Execute este script uma vez no Unity Editor para criar a UI automaticamente.
/// </summary>
public class ConfigMenuHelper : MonoBehaviour
{
    [ContextMenu("Create Config Menu UI")]
    public void CreateConfigMenuUI()
    {
        // Verificar se já existe um Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            // Criar Canvas
            GameObject canvasObj = new GameObject("ConfigMenuCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            
            // Adicionar CanvasScaler
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // Adicionar GraphicRaycaster
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Criar painel principal do menu
        GameObject menuPanel = new GameObject("ConfigMenuPanel");
        menuPanel.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = menuPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(600, 400);
        panelRect.anchoredPosition = Vector2.zero;
        
        Image panelImage = menuPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.9f); // Fundo escuro semi-transparente
        
        // Criar título
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(menuPanel.transform, false);
        
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Configuração de IP do Servidor";
        titleText.fontSize = 32;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.sizeDelta = new Vector2(0, 60);
        titleRect.anchoredPosition = new Vector2(0, -30);
        
        // Criar label do input
        GameObject labelObj = new GameObject("IPLabel");
        labelObj.transform.SetParent(menuPanel.transform, false);
        
        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = "IP do Servidor:";
        labelText.fontSize = 24;
        labelText.color = Color.white;
        
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.5f);
        labelRect.anchorMax = new Vector2(1, 0.5f);
        labelRect.sizeDelta = new Vector2(-40, 40);
        labelRect.anchoredPosition = new Vector2(0, 50);
        
        // Criar input field
        GameObject inputObj = new GameObject("IPInputField");
        inputObj.transform.SetParent(menuPanel.transform, false);
        
        RectTransform inputRect = inputObj.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0, 0.5f);
        inputRect.anchorMax = new Vector2(1, 0.5f);
        inputRect.sizeDelta = new Vector2(-40, 50);
        inputRect.anchoredPosition = new Vector2(0, -20);
        
        Image inputBg = inputObj.AddComponent<Image>();
        inputBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        
        // Criar texto do input
        GameObject inputTextObj = new GameObject("Text");
        inputTextObj.transform.SetParent(inputObj.transform, false);
        
        RectTransform inputTextRect = inputTextObj.AddComponent<RectTransform>();
        inputTextRect.anchorMin = Vector2.zero;
        inputTextRect.anchorMax = Vector2.one;
        inputTextRect.sizeDelta = Vector2.zero;
        inputTextRect.offsetMin = new Vector2(10, 0);
        inputTextRect.offsetMax = new Vector2(-10, 0);
        
        TextMeshProUGUI inputText = inputTextObj.AddComponent<TextMeshProUGUI>();
        inputText.text = "";
        inputText.fontSize = 24;
        inputText.color = Color.white;
        inputText.alignment = TextAlignmentOptions.Left;
        
        inputField.textViewport = inputTextRect;
        inputField.textComponent = inputText;
        
        // Criar placeholder
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(inputObj.transform, false);
        
        RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.sizeDelta = Vector2.zero;
        placeholderRect.offsetMin = new Vector2(10, 0);
        placeholderRect.offsetMax = new Vector2(-10, 0);
        
        TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderText.text = "192.168.4.1";
        placeholderText.fontSize = 24;
        placeholderText.color = new Color(1, 1, 1, 0.5f);
        placeholderText.alignment = TextAlignmentOptions.Left;
        
        inputField.placeholder = placeholderText;
        
        // Criar botão Salvar
        GameObject saveBtnObj = new GameObject("SaveButton");
        saveBtnObj.transform.SetParent(menuPanel.transform, false);
        
        RectTransform saveBtnRect = saveBtnObj.AddComponent<RectTransform>();
        saveBtnRect.anchorMin = new Vector2(0, 0);
        saveBtnRect.anchorMax = new Vector2(0.5f, 0);
        saveBtnRect.sizeDelta = new Vector2(-20, 60);
        saveBtnRect.anchoredPosition = new Vector2(10, 20);
        
        Image saveBtnImage = saveBtnObj.AddComponent<Image>();
        saveBtnImage.color = new Color(0.2f, 0.6f, 0.2f, 1f);
        
        Button saveButton = saveBtnObj.AddComponent<Button>();
        
        GameObject saveBtnTextObj = new GameObject("Text");
        saveBtnTextObj.transform.SetParent(saveBtnObj.transform, false);
        
        RectTransform saveBtnTextRect = saveBtnTextObj.AddComponent<RectTransform>();
        saveBtnTextRect.anchorMin = Vector2.zero;
        saveBtnTextRect.anchorMax = Vector2.one;
        saveBtnTextRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI saveBtnText = saveBtnTextObj.AddComponent<TextMeshProUGUI>();
        saveBtnText.text = "Salvar";
        saveBtnText.fontSize = 28;
        saveBtnText.color = Color.white;
        saveBtnText.alignment = TextAlignmentOptions.Center;
        
        // Criar botão Fechar
        GameObject closeBtnObj = new GameObject("CloseButton");
        closeBtnObj.transform.SetParent(menuPanel.transform, false);
        
        RectTransform closeBtnRect = closeBtnObj.AddComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(0.5f, 0);
        closeBtnRect.anchorMax = new Vector2(1, 0);
        closeBtnRect.sizeDelta = new Vector2(-20, 60);
        closeBtnRect.anchoredPosition = new Vector2(-10, 20);
        
        Image closeBtnImage = closeBtnObj.AddComponent<Image>();
        closeBtnImage.color = new Color(0.6f, 0.2f, 0.2f, 1f);
        
        Button closeButton = closeBtnObj.AddComponent<Button>();
        
        GameObject closeBtnTextObj = new GameObject("Text");
        closeBtnTextObj.transform.SetParent(closeBtnObj.transform, false);
        
        RectTransform closeBtnTextRect = closeBtnTextObj.AddComponent<RectTransform>();
        closeBtnTextRect.anchorMin = Vector2.zero;
        closeBtnTextRect.anchorMax = Vector2.one;
        closeBtnTextRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI closeBtnText = closeBtnTextObj.AddComponent<TextMeshProUGUI>();
        closeBtnText.text = "Fechar";
        closeBtnText.fontSize = 28;
        closeBtnText.color = Color.white;
        closeBtnText.alignment = TextAlignmentOptions.Center;
        
        // Configurar referências no VRManager
        VRManager vrManager = FindObjectOfType<VRManager>();
        if (vrManager != null)
        {
            vrManager.configMenuUI = menuPanel;
            vrManager.ipInputField = inputField;
            vrManager.saveIpButton = saveButton;
            vrManager.closeConfigButton = closeButton;
            
            // Inicialmente esconder o menu
            menuPanel.SetActive(false);
            
            Debug.Log("✅ Menu de configuração criado com sucesso! Configure as referências no VRManager.");
        }
        else
        {
            Debug.LogWarning("⚠️ VRManager não encontrado! Configure as referências manualmente no Inspector.");
        }
    }
}

