# 🔍 Comandos para Capturar Logs

## ⚡ Comando Rápido (Execute no Terminal)

```bash
cd "/Users/brito/Desktop/socket-client-main/socket-client-main"

# Limpar logs antigos
adb logcat -c

# Capturar TODOS os logs (não só Unity)
adb logcat > logs_completo.txt
```

**Depois:**
1. Abra o app no celular
2. Aguarde 10 segundos
3. Pressione **Ctrl+C** para parar
4. Execute: `cat logs_completo.txt | grep -i "unity\|oculus\|error\|fatal" | head -100`

---

## 🔍 Ver Logs de Erro Específicos

```bash
# Apenas erros e Unity
adb logcat *:E Unity:* -d | tail -100
```

---

## 📱 Verificar se o App Está Instalado

```bash
# Ver se o app está instalado
adb shell pm list packages | grep aegea

# Tentar abrir o app manualmente
adb shell am start -n com.aegea.br/.UnityPlayerActivity

# Ver logs imediatamente
adb logcat -d | tail -200
```

---

## 🆘 Se Nada Aparecer

Execute este comando e me mostre o resultado:

```bash
adb logcat -d | tail -500 > ultimos_logs.txt
cat ultimos_logs.txt
```

