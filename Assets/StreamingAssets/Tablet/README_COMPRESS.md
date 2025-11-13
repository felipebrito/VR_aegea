# Vídeos Comprimidos para Tablet

## 📋 Instruções

Esta pasta deve conter **versões comprimidas** dos vídeos para uso no tablet.

## 📁 Arquivos Necessários

Coloque os seguintes arquivos nesta pasta:

1. `Experiencia_Aegea_Cop2025_Portugues_tablet.mp4`
2. `Experiencia_Aegea_Cop2025_Ingles_tablet.mp4`
3. `Experiencia_Aegea_Cop2025_Espannhol_tablet.mp4`

## 🎬 Como Comprimir os Vídeos

### Opção 1: Usando FFmpeg (Recomendado)

```bash
# Comprimir vídeo português
ffmpeg -i ../Experiencia_Aegea_Cop2025_Portugues.mp4 \
  -vf "scale=1280:720" \
  -c:v libx264 -crf 28 -preset medium \
  -c:a aac -b:a 128k \
  -movflags +faststart \
  Experiencia_Aegea_Cop2025_Portugues_tablet.mp4

# Comprimir vídeo inglês
ffmpeg -i ../Experiencia_Aegea_Cop2025_Ingles.mp4 \
  -vf "scale=1280:720" \
  -c:v libx264 -crf 28 -preset medium \
  -c:a aac -b:a 128k \
  -movflags +faststart \
  Experiencia_Aegea_Cop2025_Ingles_tablet.mp4

# Comprimir vídeo espanhol
ffmpeg -i ../Experiencia_Aegea_Cop2025_Espannhol.mp4 \
  -vf "scale=1280:720" \
  -c:v libx264 -crf 28 -preset medium \
  -c:a aac -b:a 128k \
  -movflags +faststart \
  Experiencia_Aegea_Cop2025_Espannhol_tablet.mp4
```

### Opção 2: Usando HandBrake (GUI)

1. Abra o HandBrake
2. Selecione o vídeo original
3. Configure:
   - **Preset**: Fast 720p30
   - **Quality**: RF 28 (ou ajuste conforme necessário)
   - **Audio**: AAC 128kbps
4. Salve com sufixo `_tablet.mp4`

### Opção 3: Usando Adobe Media Encoder

1. Importe o vídeo original
2. Configure:
   - **Format**: H.264
   - **Resolution**: 1280x720 (720p)
   - **Bitrate**: 2-3 Mbps (ou CRF 28)
   - **Audio**: AAC 128kbps
3. Exporte com sufixo `_tablet.mp4`

## 📊 Recomendações de Compressão

- **Resolução**: 1280x720 (720p) ou 1920x1080 (1080p) - suficiente para tablet
- **Bitrate de Vídeo**: 2-3 Mbps (ou CRF 28)
- **Bitrate de Áudio**: 128 kbps AAC
- **Codec**: H.264 (compatível com todos os dispositivos)

## ⚠️ Importante

- Os vídeos originais (grandes) ficam em `StreamingAssets/` para os Oculus
- Os vídeos comprimidos ficam em `StreamingAssets/Tablet/` para o tablet
- O script `VideoPreview.cs` procura primeiro os vídeos comprimidos
- Se não encontrar, usa os vídeos originais como fallback

## 🔍 Verificação

Após comprimir, verifique se os arquivos estão nesta pasta:
```
Assets/StreamingAssets/Tablet/
  ├── Experiencia_Aegea_Cop2025_Portugues_tablet.mp4
  ├── Experiencia_Aegea_Cop2025_Ingles_tablet.mp4
  └── Experiencia_Aegea_Cop2025_Espannhol_tablet.mp4
```

