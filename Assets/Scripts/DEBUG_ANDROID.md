# 🔍 Como Debugar a Aplicação no Android

## 📱 Método 1: Ver Logs via ADB (Recomendado)

### Pré-requisitos:
1. Instalar Android SDK Platform Tools
2. Habilitar "Depuração USB" no celular
3. Conectar celular via USB

### Comandos:

```bash
# Ver todos os logs do Unity
adb logcat -s Unity

# Ver apenas erros
adb logcat *:E

# Ver logs em tempo real (filtrado)
adb logcat -s Unity:* AndroidRuntime:E

# Salvar logs em arquivo
adb logcat > debug_logs.txt
```

### Filtrar por tag específica:
```bash
# Ver apenas logs do VideoPreview
adb logcat | grep -i "VideoPreview\|VideoPlayer"

# Ver apenas erros críticos
adb logcat | grep -i "error\|exception\|crash"
```

---

## 📱 Método 2: Logs Salvos no Dispositivo

O script `AndroidDebugLogger.cs` salva logs automaticamente em:
```
/storage/emulated/0/Android/data/com.aegea.br/files/Logs/debug_YYYY-MM-DD_HH-mm-ss.txt
```

### Como acessar:
1. Conectar celular via USB
2. Abrir pasta do app no explorador de arquivos
3. Ou usar ADB:
```bash
# Listar arquivos de log
adb shell ls -la /sdcard/Android/data/com.aegea.br/files/Logs/

# Copiar log mais recente
adb pull /sdcard/Android/data/com.aegea.br/files/Logs/debug_*.txt ./
```

---

## 🐛 Problemas Comuns e Soluções

### 1. App não abre / Crash na inicialização

**Possíveis causas:**
- Erro no `Start()` de algum script
- Vídeo não encontrado
- Permissões não concedidas
- Memória insuficiente

**Debug:**
```bash
# Ver crash completo
adb logcat | grep -A 50 "FATAL EXCEPTION"
```

### 2. Vídeo não toca

**Verificar:**
- Se o vídeo está em `StreamingAssets/Tablet/`
- Se o caminho está correto nos logs
- Se há espaço suficiente no dispositivo

**Logs relevantes:**
```bash
adb logcat | grep -i "video\|streamingassets\|persistent"
```

### 3. Permissões

**Verificar permissões concedidas:**
```bash
adb shell dumpsys package com.aegea.br | grep permission
```

---

## 🔧 Adicionar AndroidDebugLogger na Cena

1. Abra a cena `aegea-tablet.unity`
2. Crie um GameObject vazio (GameObject → Create Empty)
3. Renomeie para "DebugLogger"
4. Adicione o componente `AndroidDebugLogger`
5. Salve a cena

O logger vai:
- ✅ Salvar todos os logs em arquivo
- ✅ Registrar crashes e exceções
- ✅ Mostrar informações do dispositivo
- ✅ Funcionar automaticamente

---

## 📊 Informações Úteis nos Logs

O logger registra:
- 🚀 Início da aplicação
- 📱 Modelo do dispositivo
- 📁 Caminhos (StreamingAssets, PersistentData)
- ❌ Todos os erros e exceções
- ⚠️ Avisos importantes
- 🛑 Fim da aplicação

---

## 🆘 Se o App Não Abre

1. **Verifique o logcat imediatamente:**
```bash
adb logcat -c  # Limpar logs antigos
adb logcat     # Ver logs em tempo real
# Tente abrir o app e veja o que aparece
```

2. **Procure por:**
   - `FATAL EXCEPTION` - Crash fatal
   - `AndroidRuntime` - Erro do Android
   - `Unity` - Erros do Unity
   - `VideoPreview` - Erros do preview

3. **Verifique se o vídeo existe:**
```bash
# Verificar se o APK tem o vídeo
unzip -l tablet.apk | grep -i "tablet\|preview"
```

---

## 💡 Dica Rápida

Para debug rápido, adicione este código no início de qualquer método importante:

```csharp
try {
    // seu código aqui
} catch (Exception e) {
    Debug.LogError($"❌ ERRO: {e.Message}\n{e.StackTrace}");
}
```

