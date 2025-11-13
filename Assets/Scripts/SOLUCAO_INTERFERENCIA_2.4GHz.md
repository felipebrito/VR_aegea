# Solução para Interferência WiFi 2.4GHz

## ⚠️ PROBLEMA IDENTIFICADO

Baseado no scan WiFi fornecido:
- **AEGEA-ESP está no Canal 11 (2.4GHz)**
- **Ambiente MUITO congestionado:**
  - Canal 1: **10 redes** (CONGESTIONADO)
  - Canal 6: **13 redes** (MUITO CONGESTIONADO - EVITAR!)
  - Canal 9: **2 redes** (BOM)
  - Canal 11: **2 redes** (BOM, mas já está usando e pode ter interferência)

## 🔍 IMPORTANTE: ESP32 e 2.4GHz

**ESP32 padrão (ESP32-WROOM, ESP32-DevKit) funciona APENAS em 2.4GHz!**
- Este documento foca na solução para ESP32 padrão usando banda 2.4GHz
- Canais disponíveis: 1-13 (dependendo do país)
- Canais que não se sobrepõem: 1, 6, 11 (padrão) ou 3, 9, 13 (alternativas)

## ✅ SOLUÇÃO: ESP32_WEBSOCKET_SERVER_2.4GHz.ino

**Arquivo:** `Assets/Scripts/ESP32_WEBSOCKET_SERVER_2.4GHz.ino`

### Melhorias Implementadas:

1. **Canal 3 (2.4GHz)** - Menos congestionado que 1, 6, 11
2. **Ping a cada 3 segundos** - Muito frequente para manter conexão viva
3. **Potência máxima (19.5dBm)** - Sinal mais forte
4. **WiFi Sleep desabilitado** - Conexão sempre ativa
5. **Auto-reconnect habilitado** - Reconecta automaticamente

### Canais Recomendados (2.4GHz):

**Canais que NÃO se sobrepõem:**
- **Canal 1, 6, 11** - Padrão (mas estão congestionados no seu ambiente)
- **Canal 3, 9, 13** - Alternativas (menos usados)

**Recomendações baseadas no seu scan:**
1. **Canal 3** (atual no código) - Não aparece no scan, provavelmente livre
2. **Canal 9** - Apenas 2 redes (boa opção)
3. **Canal 13** - Se disponível no seu país (menos usado)

### Como Mudar o Canal:

No arquivo `ESP32_WEBSOCKET_SERVER_2.4GHz.ino`, linha 39:
```cpp
int wifiChannel = 3; // Mude para 9 ou 13 se necessário
```

## 📋 Passos para Implementar

### 1. Atualizar ESP32

1. Abra `ESP32_WEBSOCKET_SERVER_2.4GHz.ino` no Arduino IDE
2. Se necessário, mude o canal (linha 39):
   ```cpp
   int wifiChannel = 9; // Ou 13, se disponível
   ```
3. Faça upload para o ESP32
4. Verifique no Serial Monitor que está no canal correto
5. Confirme que o Serial Monitor mostra: `Canal: 3 (2.4GHz)` ou o canal escolhido

### 2. Verificar Conexão

Após atualizar, faça um novo scan WiFi e verifique:
- AEGEA-ESP deve aparecer no **Canal 3** (ou 9/13, conforme configurado)
- **NÃO** deve estar mais no Canal 11
- RSSI deve estar bom (quanto mais próximo de 0, melhor)

### 3. Usar OculusController_UltraRobust.cs

O código do tablet já está otimizado:
- Ping a cada 3 segundos
- Verificação a cada 2 segundos
- Timeout de inatividade de 10 segundos

## 🎯 Por Que Funciona?

1. **Canal Menos Congestionado**: Canal 3 não aparece no scan 2.4GHz, então provavelmente está livre de interferência
2. **Ping Frequente (3s)**: Mantém conexão WebSocket viva mesmo com interferência temporária de outras redes
3. **Potência Máxima (19.5dBm)**: Sinal mais forte = menos perda de pacotes e maior alcance
4. **Detecção Rápida (2-3s)**: Identifica desconexões rapidamente e inicia reconexão automática
5. **WiFi Sleep Desabilitado**: Conexão sempre ativa, sem pausas que podem causar desconexões
6. **Auto-Reconnect**: Reconecta automaticamente se houver perda temporária de conexão

## 🔧 Troubleshooting

### Problema: Ainda desconecta no Canal 3
**Solução**: Tente Canal 9 (apenas 2 redes no scan)
```cpp
// No arquivo ESP32_WEBSOCKET_SERVER_2.4GHz.ino, linha 39:
int wifiChannel = 9;
```

### Problema: Canal 13 não funciona
**Solução**: Alguns países não permitem Canal 13. Use Canal 9:
```cpp
// No arquivo ESP32_WEBSOCKET_SERVER_2.4GHz.ino, linha 39:
int wifiChannel = 9;
```

### Problema: Muitas redes em todos os canais 2.4GHz
**Solução**: 
1. Use Canal 3 (não aparece no scan - provavelmente livre)
2. Se Canal 3 não funcionar, tente Canal 9 (apenas 2 redes)
3. Potência já está em 19.5dBm (máximo) - não pode aumentar mais
4. Reduza distância física entre ESP32 e Oculus (melhora sinal)
5. Verifique se há objetos bloqueando o sinal (paredes, metais)
6. Considere usar antena externa se disponível no seu ESP32

### Problema: ESP32 não aparece no scan
**Solução**: 
1. Verifique Serial Monitor do ESP32
2. Verifique se o SSID está correto: "AEGEA-ESP"
3. Reinicie o ESP32
4. Verifique se o canal está dentro do range permitido (1-13 para 2.4GHz)

## 📊 Comparação de Canais

| Canal | Redes no Scan | Recomendação |
|-------|---------------|--------------|
| 1 | 10 | ❌ EVITAR (congestionado) |
| 3 | 0 | ✅ RECOMENDADO (livre) |
| 6 | 13 | ❌ EVITAR (muito congestionado) |
| 9 | 2 | ✅ BOM (poucas redes) |
| 11 | 2 | ⚠️ OK (mas já está usando) |
| 13 | ? | ✅ BOM (se disponível) |

## 🎯 Resultado Esperado

Com essas mudanças:
- ✅ **Conexão mais estável** usando canal menos congestionado
- ✅ **Menos interferência** de outras redes
- ✅ **Reconexão rápida** em caso de desconexão temporária
- ✅ **Ping frequente** mantém conexão viva

## 📝 Notas Importantes

1. **ESP32 padrão = 2.4GHz apenas**: Este documento é para ESP32 padrão (WROOM, DevKit) que suporta apenas 2.4GHz
2. **Canais que não se sobrepõem**: 
   - Padrão: 1, 6, 11 (mas estão congestionados no seu ambiente)
   - Alternativas: 3, 9, 13 (menos usados)
3. **Canal 3 é a melhor opção inicial**: Não aparece no scan, então provavelmente está livre
4. **Ordem de teste recomendada**: 
   - Primeiro: Canal 3 (livre)
   - Se não funcionar: Canal 9 (apenas 2 redes)
   - Último recurso: Canal 13 (se disponível no seu país)
5. **Como mudar canal**: Edite linha 39 do arquivo `ESP32_WEBSOCKET_SERVER_2.4GHz.ino`
6. **Verificação**: Após upload, confira no Serial Monitor qual canal está sendo usado

