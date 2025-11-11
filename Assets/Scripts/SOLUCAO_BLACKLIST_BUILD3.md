# 🔧 Solução: Blacklist na Build 3

## 🐛 Problema Identificado

A build 3 não instala no Oculus, alegando erro de "blacklist".

## ✅ Correção Aplicada

### Bundle Identifier Corrigido

**Antes:** `com.aegea3.br` (número diretamente no nome)  
**Depois:** `com.aegea.vr3.br` (formato padrão com pontos)

O problema era que o número "3" estava diretamente no nome do pacote, o que pode ser interpretado como suspeito pelo sistema Android/Oculus.

## 📋 Passos para Resolver

### 1. Desinstalar Versão Anterior (IMPORTANTE!)

Se você já tentou instalar a build 3 antes, **desinstale primeiro**:

```bash
# Conecte o Oculus via USB
# Habilite "Depuração USB" no Oculus

# Desinstalar versão antiga
adb uninstall com.aegea3.br

# Ou se já foi alterado:
adb uninstall com.aegea.vr3.br
```

**OU** desinstale manualmente:
- Vá em **Settings → Apps** no Oculus
- Procure pelo app "AEGEA-03" ou similar
- Clique em **Uninstall**

### 2. Verificar Bundle Identifier no Unity

1. **Edit → Project Settings → Player**
2. Na seção **Other Settings → Identification**
3. Verifique **Bundle Identifier** para Android
4. Deve estar: `com.aegea.vr3.br`
5. Se não estiver, altere manualmente

### 3. Recompilar a Build 3

1. **File → Build Settings**
2. Certifique-se de que:
   - **userNumber = 3** na cena
   - **Bundle Identifier = com.aegea.vr3.br**
   - **Product Name = AEGEA-03** (ou outro nome único)
3. **Build**

### 4. Instalar Nova Build

```bash
# Instalar nova build
adb install -r caminho/para/build3.apk

# O flag -r (replace) sobrescreve se já existir
```

## 🔍 Verificação

Após instalar, verifique:

```bash
# Ver apps instalados
adb shell pm list packages | grep aegea

# Deve mostrar:
# package:com.aegea.vr3.br
```

## ⚠️ Possíveis Causas Adicionais

Se ainda não funcionar, verifique:

### 1. Bundle Identifier Duplicado

Certifique-se de que cada build tem um Bundle Identifier único:
- Build 1: `com.aegea.vr1.br` (ou `com.aegea.br`)
- Build 2: `com.aegea.vr2.br`
- Build 3: `com.aegea.vr3.br` ✅
- Build 4: `com.aegea.vr4.br`

### 2. Assinatura do APK

Se você assinou o APK anteriormente com uma chave diferente, pode causar conflito:
- Desinstale completamente a versão anterior
- Ou use a mesma chave de assinatura

### 3. Permissões Problemáticas

Verifique se não há permissões suspeitas no AndroidManifest:
- Permissões de sistema críticas podem causar blacklist
- Permissões de root ou debug podem ser bloqueadas

### 4. Versão do Android/Oculus

- Certifique-se de que o Oculus está atualizado
- Verifique se a versão mínima do Android está correta (AndroidMinSdkVersion: 33)

## 💡 Dica: Bundle Identifiers Recomendados

Para evitar problemas futuros, use este padrão:

```
com.aegea.vr{numero}.br
```

Onde `{numero}` é 1, 2, 3 ou 4.

Exemplos:
- `com.aegea.vr1.br`
- `com.aegea.vr2.br`
- `com.aegea.vr3.br` ✅
- `com.aegea.vr4.br`

## 🚨 Se Ainda Não Funcionar

1. **Verifique logs do ADB durante instalação:**
   ```bash
   adb logcat | grep -i "package\|install\|blacklist"
   ```

2. **Tente instalar via ADB com mais informações:**
   ```bash
   adb install -r -d caminho/para/build3.apk
   ```

3. **Verifique se o dispositivo está em modo desenvolvedor:**
   - Settings → Developer Mode → ON

4. **Tente instalar manualmente:**
   - Copie o APK para o Oculus
   - Use um gerenciador de arquivos no Oculus
   - Instale diretamente pelo arquivo

