# 🔧 Como Resolver o Problema do App Fechando

## 🐛 Problema
O app abre, mostra a tela do Unity e fecha imediatamente.

## ✅ Solução Implementada

### 1. Script TabletStartupManager
- Desabilita XR Manager (causa comum de crash no tablet)
- Desabilita componentes do Oculus
- Adiciona logs detalhados

### 2. Proteção em Scripts Críticos
- Try-catch em `OculusController.Start()`
- Try-catch em `VideoPreview.Start()`

## 📋 Passos para Resolver

### Passo 1: Adicionar TabletStartupManager na Cena

1. Abra a cena `aegea-tablet.unity`
2. Crie um GameObject vazio (GameObject → Create Empty)
3. Renomeie para "TabletStartupManager"
4. Adicione o componente `TabletStartupManager`
5. **IMPORTANTE**: Configure Script Execution Order:
   - Edit → Project Settings → Script Execution Order
   - Adicione `TabletStartupManager` com ordem `-100` (executa primeiro)
6. Salve a cena

### Passo 2: Desabilitar XR Manager no Projeto

1. Edit → Project Settings → XR Plug-in Management
2. Desmarque "Initialize XR on Startup" para Android
3. Ou desmarque o Oculus Loader

### Passo 3: Recompilar

1. File → Build Settings
2. Build
3. Instale no celular
4. Teste

## 🔍 Como Ver os Logs

### Via ADB:
```bash
# Conecte o celular via USB
# Habilite "Depuração USB"

# Ver logs em tempo real
adb logcat -s Unity

# Ver apenas erros
adb logcat *:E

# Ver crash completo
adb logcat | grep -A 50 "FATAL EXCEPTION"
```

### O que procurar nos logs:
- `TabletStartupManager.Awake()` - Deve aparecer primeiro
- `FATAL EXCEPTION` - Indica crash
- `XR` ou `Oculus` - Pode indicar problema com VR

## ⚠️ Se Ainda Não Funcionar

1. **Verifique se o TabletStartupManager está na cena**
2. **Verifique Script Execution Order** (deve ser -100)
3. **Desabilite XR completamente** nas Project Settings
4. **Veja os logs do ADB** para identificar o erro exato

## 💡 Dica

Se o app ainda fechar, os logs do ADB vão mostrar exatamente onde está o problema. Compartilhe os logs para análise.

