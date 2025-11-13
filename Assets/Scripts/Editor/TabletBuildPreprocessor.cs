using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Script de pré-build para remover vídeos grandes antes de compilar para tablet.
/// Mantém apenas o vídeo comprimido na pasta Tablet/.
/// </summary>
public class TabletBuildPreprocessor : IPreprocessBuildWithReport
{
    // Ordem de execução (menor número = executa primeiro)
    public int callbackOrder => 0;
    
    // Lista de vídeos grandes a serem removidos temporariamente
    private static readonly string[] LARGE_VIDEOS = {
        "Experiencia_Aegea_Cop2025_Portugues.mp4",
        "Experiencia_Aegea_Cop2025_Ingles.mp4",
        "Experiencia_Aegea_Cop2025_Espannhol.mp4"
    };
    
    // Pasta temporária para backup dos vídeos
    private static readonly string BACKUP_FOLDER = "Assets/StreamingAssets/_Backup_LargeVideos";
    
    public void OnPreprocessBuild(BuildReport report)
    {
        // Só processar se for build para Android
        if (report.summary.platform != BuildTarget.Android)
        {
            Debug.Log("📦 Build não é para Android - mantendo todos os vídeos");
            return;
        }
        
        // DESABILITADO: Este script estava causando problemas
        // Para usar, descomente e configure corretamente
        Debug.Log("📦 Script de pré-build desabilitado (para não afetar editor)");
        return;
        
        // Verificar se é build de tablet (pode usar define symbol ou sempre remover para Android)
        // Por padrão, remove vídeos grandes para qualquer build Android
        // Se precisar fazer build para Oculus, comente este script temporariamente
        
        Debug.Log("📦 Iniciando pré-processamento para build do tablet...");
        Debug.Log("💡 Dica: Se for build para Oculus, comente este script temporariamente");
        
        string streamingAssetsPath = Application.dataPath + "/StreamingAssets";
        
        // Verificar se StreamingAssets existe
        if (!Directory.Exists(streamingAssetsPath))
        {
            Debug.LogWarning("⚠️ Pasta StreamingAssets não encontrada!");
            return;
        }
        
        // Criar pasta de backup se não existir
        string backupPath = Application.dataPath + "/StreamingAssets/_Backup_LargeVideos";
        if (!Directory.Exists(backupPath))
        {
            Directory.CreateDirectory(backupPath);
            Debug.Log($"📁 Pasta de backup criada: {backupPath}");
        }
        
        // Mover vídeos grandes para backup
        List<string> movedFiles = new List<string>();
        
        foreach (string videoFile in LARGE_VIDEOS)
        {
            string sourcePath = Path.Combine(streamingAssetsPath, videoFile);
            string backupFilePath = Path.Combine(backupPath, videoFile);
            
            if (File.Exists(sourcePath))
            {
                try
                {
                    // Mover para backup
                    File.Move(sourcePath, backupFilePath);
                    movedFiles.Add(videoFile);
                    
                    // Também mover o arquivo .meta se existir
                    string metaPath = sourcePath + ".meta";
                    string backupMetaPath = backupFilePath + ".meta";
                    if (File.Exists(metaPath))
                    {
                        File.Move(metaPath, backupMetaPath);
                    }
                    
                    Debug.Log($"✅ Vídeo grande movido para backup: {videoFile}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ Erro ao mover {videoFile}: {e.Message}");
                }
            }
            else
            {
                Debug.Log($"ℹ️ Vídeo não encontrado (já removido?): {videoFile}");
            }
        }
        
        if (movedFiles.Count > 0)
        {
            Debug.Log($"📦 {movedFiles.Count} vídeo(s) grande(s) removido(s) da build do tablet");
            Debug.Log($"💾 Vídeos salvos em: {backupPath}");
            Debug.Log("✅ Build do tablet otimizada - apenas vídeo comprimido será incluído");
            
            // Forçar refresh do Asset Database
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.Log("ℹ️ Nenhum vídeo grande encontrado para remover");
        }
    }
}

/// <summary>
/// Script de pós-build para restaurar os vídeos grandes após a compilação.
/// </summary>
public class TabletBuildPostprocessor : IPostprocessBuildWithReport
{
    // Ordem de execução (menor número = executa primeiro)
    public int callbackOrder => 0;
    
    public void OnPostprocessBuild(BuildReport report)
    {
        // Só processar se for build para Android
        if (report.summary.platform != BuildTarget.Android)
        {
            return;
        }
        
        Debug.Log("📦 Restaurando vídeos grandes após build...");
        
        string streamingAssetsPath = Application.dataPath + "/StreamingAssets";
        string backupPath = Application.dataPath + "/StreamingAssets/_Backup_LargeVideos";
        
        if (!Directory.Exists(backupPath))
        {
            Debug.Log("ℹ️ Nenhum backup encontrado para restaurar");
            return;
        }
        
        // Restaurar vídeos do backup
        string[] backupFiles = Directory.GetFiles(backupPath, "*.mp4");
        
        foreach (string backupFile in backupFiles)
        {
            string fileName = Path.GetFileName(backupFile);
            string destinationPath = Path.Combine(streamingAssetsPath, fileName);
            
            try
            {
                // Restaurar vídeo
                if (File.Exists(backupFile))
                {
                    File.Move(backupFile, destinationPath);
                    Debug.Log($"✅ Vídeo restaurado: {fileName}");
                    
                    // Restaurar .meta também
                    string backupMetaPath = backupFile + ".meta";
                    string destinationMetaPath = destinationPath + ".meta";
                    if (File.Exists(backupMetaPath))
                    {
                        File.Move(backupMetaPath, destinationMetaPath);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erro ao restaurar {fileName}: {e.Message}");
            }
        }
        
        // Remover pasta de backup se estiver vazia
        try
        {
            if (Directory.GetFiles(backupPath).Length == 0)
            {
                Directory.Delete(backupPath);
                // Remover .meta da pasta também
                string backupMetaPath = backupPath + ".meta";
                if (File.Exists(backupMetaPath))
                {
                    File.Delete(backupMetaPath);
                }
                Debug.Log("🗑️ Pasta de backup removida");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ Não foi possível remover pasta de backup: {e.Message}");
        }
        
        // Forçar refresh do Asset Database
        AssetDatabase.Refresh();
        
        Debug.Log("✅ Restauração concluída");
    }
}

