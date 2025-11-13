using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Painel individual para cada Oculus headset.
/// Exibe: seleção de idioma, status online, bateria e ID.
/// </summary>
public class OculusPanel : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Botões de seleção de idioma (PT, EN, ES)")]
    public Button[] languageButtons = new Button[3];
    
    [Tooltip("Texto dos botões de idioma")]
    public TextMeshProUGUI[] languageButtonTexts = new TextMeshProUGUI[3];
    
    [Tooltip("Ícone de status online/offline")]
    public Image statusIcon;
    
    [Tooltip("Texto do status (Online/Offline)")]
    public TextMeshProUGUI statusText;
    
    [Tooltip("Imagem da bateria")]
    public Image batteryIcon;
    
    [Tooltip("Texto do percentual da bateria")]
    public TextMeshProUGUI batteryText;
    
    [Tooltip("Texto do ID do Oculus")]
    public TextMeshProUGUI oculusIdText;
    
    [Header("Status Colors")]
    [Tooltip("Cor quando online")]
    public Color onlineColor = Color.green;
    
    [Tooltip("Cor quando offline")]
    public Color offlineColor = Color.red;
    
    [Header("Battery Colors")]
    [Tooltip("Cor da bateria quando alta (>50%)")]
    public Color batteryHighColor = Color.green;
    
    [Tooltip("Cor da bateria quando média (20-50%)")]
    public Color batteryMediumColor = Color.yellow;
    
    [Tooltip("Cor da bateria quando baixa (<20%)")]
    public Color batteryLowColor = Color.red;
    
    private int oculusId = 1;
    private string selectedLanguage = "";
    private bool isOnline = false;
    private float batteryLevel = 0f;
    
    void Start()
    {
        SetupLanguageButtons();
        UpdateUI();
    }
    
    public void SetOculusId(int id)
    {
        oculusId = id;
        if (oculusIdText != null)
        {
            oculusIdText.text = id.ToString();
        }
        UpdateUI();
    }
    
    void SetupLanguageButtons()
    {
        string[] languages = { "pt", "en", "es" };
        string[] languageNames = { "PT", "EN", "ES" };
        
        for (int i = 0; i < languageButtons.Length && i < languages.Length; i++)
        {
            if (languageButtons[i] != null)
            {
                string lang = languages[i];
                languageButtons[i].onClick.RemoveAllListeners();
                languageButtons[i].onClick.AddListener(() => OnLanguageSelected(lang));
                
                if (languageButtonTexts[i] != null)
                {
                    languageButtonTexts[i].text = languageNames[i];
                }
            }
        }
        
        // Selecionar PT por padrão
        if (languages.Length > 0)
        {
            OnLanguageSelected("pt");
        }
    }
    
    void OnLanguageSelected(string language)
    {
        selectedLanguage = language;
        Debug.Log($"🎯 Oculus {oculusId} selecionou idioma: {language.ToUpper()}");
        
        // Atualizar visual dos botões
        UpdateLanguageButtons();
    }
    
    void UpdateLanguageButtons()
    {
        string[] languages = { "pt", "en", "es" };
        
        for (int i = 0; i < languageButtons.Length && i < languages.Length; i++)
        {
            if (languageButtons[i] != null)
            {
                bool isSelected = languages[i] == selectedLanguage;
                
                // Mudar cor do botão selecionado
                ColorBlock colors = languageButtons[i].colors;
                colors.normalColor = isSelected ? Color.cyan : Color.white;
                colors.selectedColor = isSelected ? Color.cyan : Color.white;
                languageButtons[i].colors = colors;
                
                // Destacar texto do botão selecionado
                if (languageButtonTexts[i] != null)
                {
                    languageButtonTexts[i].fontStyle = isSelected ? 
                        FontStyles.Bold : FontStyles.Normal;
                    languageButtonTexts[i].color = isSelected ? 
                        Color.cyan : Color.white;
                }
            }
        }
    }
    
    public string GetSelectedLanguage()
    {
        return selectedLanguage;
    }
    
    public void SetOnlineStatus(bool online)
    {
        isOnline = online;
        UpdateUI();
    }
    
    public void SetBatteryLevel(float percent)
    {
        batteryLevel = Mathf.Clamp(percent, 0f, 100f);
        UpdateUI();
    }
    
    void UpdateUI()
    {
        // Atualizar status online/offline
        if (statusIcon != null)
        {
            statusIcon.color = isOnline ? onlineColor : offlineColor;
        }
        
        if (statusText != null)
        {
            statusText.text = isOnline ? "ONLINE" : "OFFLINE";
            statusText.color = isOnline ? onlineColor : offlineColor;
        }
        
        // Atualizar bateria
        if (batteryText != null)
        {
            batteryText.text = $"{batteryLevel:F0}%";
        }
        
        if (batteryIcon != null)
        {
            Color batteryColor;
            if (batteryLevel > 50f)
            {
                batteryColor = batteryHighColor;
            }
            else if (batteryLevel > 20f)
            {
                batteryColor = batteryMediumColor;
            }
            else
            {
                batteryColor = batteryLowColor;
            }
            
            batteryIcon.color = batteryColor;
            
            // Atualizar fill da bateria (se for uma imagem com fill)
            if (batteryIcon.type == Image.Type.Filled)
            {
                batteryIcon.fillAmount = batteryLevel / 100f;
            }
        }
        
        // Atualizar botões de idioma
        UpdateLanguageButtons();
    }
}

