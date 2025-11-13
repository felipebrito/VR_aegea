# Script de Pré-Build para Tablet

## 📋 Descrição

Este script (`TabletBuildPreprocessor.cs`) remove automaticamente os vídeos grandes (932MB cada) antes de compilar para Android, mantendo apenas o vídeo comprimido (39MB) na build do tablet.

## 🎯 Como Funciona

1. **Antes da Build (Pré-processamento)**:
   - Detecta se é build para Android
   - Move os 3 vídeos grandes para uma pasta de backup temporária
   - Mantém apenas o vídeo comprimido em `StreamingAssets/Tablet/`

2. **Depois da Build (Pós-processamento)**:
   - Restaura os vídeos grandes do backup
   - Remove a pasta de backup temporária
   - Deixa tudo como estava antes

## 📦 Vídeos Afetados

Os seguintes vídeos são removidos temporariamente:
- `Experiencia_Aegea_Cop2025_Portugues.mp4` (~932MB)
- `Experiencia_Aegea_Cop2025_Ingles.mp4` (~927MB)
- `Experiencia_Aegea_Cop2025_Espannhol.mp4` (~927MB)

**Mantido na build:**
- `Tablet/Experiencia_Aegea_Cop2025_Preview_tablet.mp4` (39MB)

## ⚙️ Uso

### Build para Tablet (Android)
O script funciona automaticamente! Apenas compile normalmente:
1. File → Build Settings
2. Selecione Android
3. Build
4. Os vídeos grandes serão removidos automaticamente antes da build
5. Após a build, os vídeos serão restaurados automaticamente

### Build para Oculus (Android)
Se você precisar fazer build para Oculus (que precisa dos vídeos grandes):

**Opção 1: Desabilitar temporariamente**
1. No Unity, vá em `Assets/Scripts/Editor/TabletBuildPreprocessor.cs`
2. Comente a linha `public int callbackOrder => 0;` ou renomeie a classe
3. Faça a build para Oculus
4. Descomente depois

**Opção 2: Usar Define Symbol**
1. Edit → Project Settings → Player → Other Settings
2. Adicione `TABLET_BUILD` em Scripting Define Symbols
3. Modifique o script para verificar: `#if TABLET_BUILD`

## 📊 Economia de Espaço

- **Antes**: ~2.8GB (3 vídeos × 932MB)
- **Depois**: 39MB (1 vídeo comprimido)
- **Economia**: ~99% de redução no tamanho da build!

## ⚠️ Importante

- Os vídeos são **movidos** (não deletados) para uma pasta de backup
- A pasta de backup é `StreamingAssets/_Backup_LargeVideos/`
- Os vídeos são restaurados automaticamente após a build
- Se a build falhar, os vídeos ainda estarão no backup (restaure manualmente se necessário)

## 🔧 Troubleshooting

**Problema**: Vídeos não foram restaurados após build
- **Solução**: Verifique a pasta `StreamingAssets/_Backup_LargeVideos/` e mova manualmente de volta

**Problema**: Build para Oculus não tem vídeos
- **Solução**: Desabilite o script temporariamente (veja seção "Build para Oculus")

**Problema**: Script não está executando
- **Solução**: Verifique se o arquivo está em `Assets/Scripts/Editor/` e se tem `.cs` extension

