# 📱 Passo a Passo: Build do Tablet Funcionando

## 🎯 Objetivo
Fazer a build do tablet funcionar no Samsung S24 Ultra (ou qualquer Android).

## ⚠️ Problema Atual
O app fecha porque o Oculus XR Plugin está habilitado e detecta que não é um dispositivo Oculus.

---

## 📋 PASSO 1: Desabilitar Oculus XR Plugin

### 1.1 Abrir Project Settings
1. No Unity, clique em **Edit** (menu superior)
2. Clique em **Project Settings...**

### 1.2 Navegar para XR Plug-in Management
1. Na janela Project Settings, no painel esquerdo, procure por **XR Plug-in Management**
2. Clique em **XR Plug-in Management**
3. No submenu que aparece, clique em **Android** (não em "Oculus")

### 1.3 Desabilitar Oculus Loader
1. Na área principal, você verá uma lista de "Provider Plug-ins"
2. Procure por **Oculus** na lista
3. **DESMARQUE** a caixa ao lado de "Oculus"
4. Se não aparecer, vá para o próximo passo

### 1.4 Desabilitar Inicialização Automática
1. Ainda na mesma tela (XR Plug-in Management → Android)
2. Procure por **"Initialize XR on Startup"**
3. **DESMARQUE** essa opção
4. Salve (Ctrl+S ou Cmd+S)

---

## 📋 PASSO 2: Verificar Audio Settings

### 2.1 Abrir Audio Settings
1. Na janela Project Settings, no painel esquerdo, clique em **Audio**

### 2.2 Remover Meta XR Audio
1. Procure por **"Spatializer Plugin"**
2. Clique no dropdown e selecione **"None"** (ou deixe vazio)
3. Procure por **"Ambisonic Decoder Plugin"**
4. Clique no dropdown e selecione **"None"** (ou deixe vazio)
5. Salve

---

## 📋 PASSO 3: Adicionar TabletStartupManager na Cena

### 3.1 Abrir a Cena do Tablet
1. No Unity, vá em **File → Open Scene**
2. Abra a cena: `Assets/Scenes/aegea-tablet.unity`

### 3.2 Criar GameObject
1. Na Hierarchy (painel esquerdo), clique com botão direito
2. Selecione **Create Empty**
3. Renomeie para: `TabletStartupManager`

### 3.3 Adicionar Script
1. Selecione o GameObject `TabletStartupManager`
2. No Inspector (painel direito), clique em **Add Component**
3. Digite: `TabletStartupManager`
4. Selecione o script quando aparecer
5. Salve a cena (Ctrl+S ou Cmd+S)

### 3.4 Configurar Script Execution Order (IMPORTANTE)
1. Vá em **Edit → Project Settings**
2. No painel esquerdo, clique em **Script Execution Order**
3. Clique no botão **+** (adicionar)
4. Digite: `TabletStartupManager`
5. Arraste a barra de ordem para **-100** (ou digite -100)
6. Salve

---

## 📋 PASSO 4: Verificar Build Settings

### 4.1 Abrir Build Settings
1. Vá em **File → Build Settings...**

### 4.2 Selecionar Android
1. Na lista de plataformas, selecione **Android**
2. Se não estiver selecionado, clique em **Switch Platform** (pode demorar)

### 4.3 Verificar Player Settings
1. Clique em **Player Settings...**
2. Procure por **XR Plug-in Management** no painel esquerdo
3. Clique em **XR Plug-in Management → Android**
4. Confirme que **Oculus** está DESMARCADO
5. Confirme que **"Initialize XR on Startup"** está DESMARCADO
6. Feche a janela

---

## 📋 PASSO 5: Compilar

### 5.1 Build
1. Na janela Build Settings, clique em **Build**
2. Escolha onde salvar o APK
3. Aguarde a compilação terminar

### 5.2 Instalar no Celular
1. Conecte o celular via USB
2. Habilite "Depuração USB" no celular
3. Instale o APK:
   ```bash
   adb install caminho/para/tablet.apk
   ```
   Ou arraste o APK para o celular e instale manualmente

---

## 📋 PASSO 6: Testar e Verificar Logs

### 6.1 Ver Logs em Tempo Real
1. Conecte o celular via USB
2. Abra o terminal
3. Execute:
   ```bash
   adb logcat -c  # Limpar logs antigos
   adb logcat -s Unity  # Ver logs do Unity
   ```

### 6.2 Abrir o App
1. No celular, abra o app
2. Observe os logs no terminal

### 6.3 O que deve aparecer nos logs:
✅ **BOM:**
- `TabletStartupManager.Awake()`
- `Device: samsung SM-S928B`
- App abre normalmente

❌ **RUIM (se aparecer, ainda tem problema):**
- `This .apk was built with the Oculus XR Plugin loader enabled`
- `loading library OVRPlugin`
- `Quit requested`

---

## 🔧 Se Ainda Não Funcionar

### Verificação Final:
1. ✅ Oculus Loader está desmarcado em XR Plug-in Management → Android?
2. ✅ "Initialize XR on Startup" está desmarcado?
3. ✅ Spatializer Plugin está vazio (None)?
4. ✅ TabletStartupManager está na cena?
5. ✅ Script Execution Order está em -100?

### Se tudo estiver correto e ainda não funcionar:
1. Compile novamente (às vezes precisa recompilar após mudanças)
2. Desinstale o app antigo do celular antes de instalar o novo
3. Verifique os logs do ADB para ver o erro exato

---

## 📞 Próximos Passos

Depois que funcionar:
- Para fazer build do Oculus: Reabilite o Oculus Loader
- Para fazer build do Tablet: Desabilite o Oculus Loader

**Dica:** Crie Build Profiles separados para facilitar!

