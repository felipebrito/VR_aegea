# ✅ Checklist Rápido - Build do Tablet

## ⚡ Ações Necessárias (5 minutos)

### ✅ 1. No Unity Editor - Project Settings

1. **Edit → Project Settings → XR Plug-in Management → Android**
   - [ ] Desmarque "Oculus" (se estiver marcado)
   - [ ] Desmarque "Initialize XR on Startup"
   - Salve

2. **Edit → Project Settings → Audio**
   - [ ] Spatializer Plugin: **None**
   - [ ] Ambisonic Decoder Plugin: **None**
   - Salve

### ✅ 2. Na Cena do Tablet

1. Abra: `Assets/Scenes/aegea-tablet.unity`
2. [ ] Criar GameObject vazio → "TabletStartupManager"
3. [ ] Adicionar componente `TabletStartupManager`
4. Salve a cena

### ✅ 3. Script Execution Order

1. **Edit → Project Settings → Script Execution Order**
2. [ ] Adicionar `TabletStartupManager` com ordem **-100**
3. Salve

### ✅ 4. Compilar

1. **File → Build Settings**
2. [ ] Selecionar **Android**
3. [ ] Clicar em **Build**
4. [ ] Instalar no celular
5. [ ] Testar

---

## 🔍 Verificação Rápida

Após compilar, execute no terminal:
```bash
adb logcat -s Unity | head -50
```

**Deve aparecer:**
- ✅ `TabletStartupManager.Awake()`
- ✅ App funcionando

**NÃO deve aparecer:**
- ❌ `This .apk was built with the Oculus XR Plugin loader enabled`
- ❌ `Quit requested`

---

## 🆘 Se Ainda Não Funcionar

1. Desinstale o app antigo do celular
2. Recompile do zero
3. Compartilhe os logs do ADB

