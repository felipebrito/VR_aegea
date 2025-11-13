# 📋 Como Capturar Logs Quando Nada Aparece

## 🐛 Problema
O app fecha mas não aparecem logs no terminal.

## ✅ Solução: Capturar Logs Completos

### Opção 1: Todos os Logs (Recomendado)

```bash
# 1. Limpar logs antigos
adb logcat -c

# 2. Capturar TODOS os logs
adb logcat > logs_completo.txt

# 3. Abra o app no celular
# 4. Aguarde 5-10 segundos
# 5. Pressione Ctrl+C para parar

# 6. Ver os logs
cat logs_completo.txt | grep -i "unity\|oculus\|error\|fatal\|crash" | head -100
```

### Opção 2: Apenas Erros e Unity

```bash
# 1. Limpar logs
adb logcat -c

# 2. Capturar erros + Unity
adb logcat *:E Unity:* > logs_erros.txt

# 3. Abra o app no celular
# 4. Aguarde 5-10 segundos
# 5. Pressione Ctrl+C

# 6. Ver os logs
cat logs_erros.txt
```

### Opção 3: Logs em Tempo Real

```bash
# Ver logs em tempo real enquanto abre o app
adb logcat | grep -i "unity\|oculus\|error\|fatal\|crash\|androidruntime"
```

## 🔍 O Que Procurar nos Logs

### Se o app não abre:
- `FATAL EXCEPTION` - Crash fatal
- `AndroidRuntime` - Erro do Android
- `PackageManager` - Problema de instalação
- `Permission` - Problema de permissão

### Se o app abre mas fecha:
- `This .apk was built with the Oculus XR Plugin loader enabled`
- `Quit requested`
- `ExecutionEngineException`
- `OVRPlugin`

## 📱 Verificar se o App Está Instalado

```bash
# Listar apps instalados
adb shell pm list packages | grep aegea

# Ver informações do app
adb shell dumpsys package com.aegea.br | head -50
```

## 🆘 Se Nada Aparecer nos Logs

1. **Verifique se o ADB está funcionando:**
   ```bash
   adb devices
   ```
   Deve mostrar seu dispositivo

2. **Verifique se o app está instalado:**
   ```bash
   adb shell pm list packages | grep aegea
   ```

3. **Tente abrir o app manualmente:**
   ```bash
   adb shell am start -n com.aegea.br/.UnityPlayerActivity
   ```

4. **Veja os logs imediatamente após:**
   ```bash
   adb logcat -d | tail -200
   ```

