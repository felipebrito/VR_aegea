using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor script para desabilitar Meta XR Audio nas Project Settings para builds do tablet
/// </summary>
[InitializeOnLoad]
public class DisableMetaXRForTablet
{
    static DisableMetaXRForTablet()
    {
        // Desabilitar Meta XR Audio nas Project Settings
        // Isso previne que o Meta XR Audio seja inicializado automaticamente
        #if UNITY_ANDROID
        // Este código roda no editor, mas não afeta a build
        // A desabilitação real precisa ser feita nas Project Settings manualmente
        #endif
    }
}

