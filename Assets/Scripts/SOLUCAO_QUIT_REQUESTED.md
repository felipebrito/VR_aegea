# 🔧 Solução para "Quit requested" no Tablet

## 🐛 Problema Identificado

Pelos logs do ADB, o problema é:
1. **Meta XR Audio está inicializando** (linhas 196-220)
2. **"Quit requested" aparece** (linha 221)
3. **App fecha imediatamente**

## ✅ Solução

### Passo 1: Desabilitar Meta XR Audio nas Project Settings

**CRÍTICO**: O Meta XR Audio inicializa via `OnBeforeSceneLoadRuntimeMethod`, que é chamado **ANTES** do `Awake()`. Por isso precisa ser desabilitado nas Project Settings.

1. **Edit → Project Settings → Audio**
2. **Spatializer Plugin**: Deixe vazio (não use "Meta XR Audio")
3. **Ambisonic Decoder Plugin**: Deixe vazio
4. Salve

### Passo 2: Desabilitar XR Manager

1. **Edit → Project Settings → XR Plug-in Management**
2. **Android**: Desmarque "Initialize XR on Startup"
3. **Desmarque o Oculus Loader** na lista de providers
4. Salve

### Passo 3: Adicionar TabletStartupManager na Cena

1. Abra `aegea-tablet.unity`
2. Crie GameObject vazio → "TabletStartupManager"
3. Adicione componente `TabletStartupManager`
4. **Configure Script Execution Order**:
   - Edit → Project Settings → Script Execution Order
   - Adicione `TabletStartupManager` com ordem `-100`
5. Salve a cena

### Passo 4: Recompilar

1. File → Build Settings
2. Build
3. Teste no celular

## 🔍 Verificação

Após compilar, verifique os logs:

```bash
adb logcat -s Unity | grep -i "tabletstartup\|metaxr\|quit"
```

Você deve ver:
- ✅ `TabletStartupManager.Awake()` aparecendo
- ✅ `Desabilitando componente Meta XR` aparecendo
- ❌ **NÃO** deve aparecer `Meta XR Audio Native Interface initialized`
- ❌ **NÃO** deve aparecer `Quit requested`

## ⚠️ Se Ainda Não Funcionar

Se o Meta XR Audio ainda inicializar, você precisa:

1. **Remover o pacote Meta XR SDK** temporariamente para builds do tablet
2. Ou criar uma **cena separada** para tablet sem componentes do Oculus
3. Ou usar **Define Symbols** para excluir código do Oculus na build do tablet

## 💡 Alternativa: Define Symbol

Crie um Define Symbol `TABLET_BUILD` e use:

```csharp
#if !TABLET_BUILD
// Código do Oculus aqui
#endif
```

