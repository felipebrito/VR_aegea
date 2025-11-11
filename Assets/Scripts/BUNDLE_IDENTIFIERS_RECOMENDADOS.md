# 📦 Bundle Identifiers Recomendados para Múltiplas Builds

## ✅ Formato Recomendado (SEM NÚMEROS)

Para evitar problemas de blacklist e seguir boas práticas Android, use letras:

### Build 1 (Oculus 1)
```
Bundle Identifier: com.aegea.vra.br
Product Name: AEGEA-VR-A
```
- `vra` = VR A (primeiro)

### Build 2 (Oculus 2)
```
Bundle Identifier: com.aegea.vrb.br
Product Name: AEGEA-VR-B
```
- `vrb` = VR B (segundo)

### Build 3 (Oculus 3)
```
Bundle Identifier: com.aegea.vrc.br
Product Name: AEGEA-VR-C
```
- `vrc` = VR C (terceiro)

### Build 4 (Oculus 4)
```
Bundle Identifier: com.aegea.vrd.br
Product Name: AEGEA-VR-D
```
- `vrd` = VR D (quarto)

## 🔄 Alternativa: Usar Nomes Descritivos

Se preferir nomes mais descritivos:

### Build 1
```
Bundle Identifier: com.aegea.headset1.br
Product Name: AEGEA-Headset-1
```

### Build 2
```
Bundle Identifier: com.aegea.headset2.br
Product Name: AEGEA-Headset-2
```

### Build 3
```
Bundle Identifier: com.aegea.headset3.br
Product Name: AEGEA-Headset-3
```

### Build 4
```
Bundle Identifier: com.aegea.headset4.br
Product Name: AEGEA-Headset-4
```

## ⚠️ IMPORTANTE

1. **Cada build DEVE ter um Bundle Identifier único**
2. **Desinstale versões antigas antes de instalar novas**
3. **Use o mesmo padrão para todas as builds**
4. **Evite números diretamente no nome** (ex: `com.aegea3.br`)

## 📋 Checklist Antes de Build

- [ ] Bundle Identifier único e sem números no nome
- [ ] Product Name único para identificar facilmente
- [ ] userNumber configurado corretamente na cena (1, 2, 3 ou 4)
- [ ] Versão anterior desinstalada (se houver)

