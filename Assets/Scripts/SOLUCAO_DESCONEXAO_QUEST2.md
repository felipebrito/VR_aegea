# 🔧 Solução: Desconexão Constante no Quest 2

## 🐛 Problema Identificado

O Quest 2 ficava desconectando constantemente, perdendo a conexão WebSocket e não conseguindo ser controlado.

## ✅ Melhorias Implementadas

### 1. Unity (VRManager.cs)

#### Configurações de Conexão Melhoradas:
- **connectionCheckInterval**: Reduzido de 5s para 3s (detecta desconexões mais rápido)
- **pingInterval**: Novo campo - ping a cada 10 segundos para manter conexão viva
- **maxReconnectAttempts**: Aumentado de 10 para 999 (nunca para de tentar reconectar)

#### Ping Periódico Habilitado:
- **SendPingPeriodic()**: Novo método que envia ping automaticamente a cada 10 segundos
- **SendPing()**: Melhorado com validações robustas e tratamento de erros
- Ping mantém a conexão WebSocket viva e detecta desconexões rapidamente

#### Keep-Alive Configurado:
- **KeepAliveInterval**: Configurado para 10 segundos no ClientWebSocket
- Mantém a conexão TCP ativa mesmo sem tráfego

#### Verificação Periódica:
- **CheckConnection()**: Verifica conexão a cada 3 segundos
- Detecta desconexões rapidamente e inicia reconexão automática

### 2. ESP32 (Código Arduino)

#### Servidor WebSocket Implementado:
- Biblioteca `WebSocketsServer` adicionada
- Servidor WebSocket na porta **81** (separado do HTTP na porta 80)
- Handler de eventos para gerenciar conexões

#### Configurações WiFi Melhoradas:
- **WiFi.setSleep(false)**: Desabilita sleep WiFi - mantém conexão sempre ativa
- **WiFi.setAutoReconnect(true)**: Reconecta automaticamente se desconectar

#### Ping Automático:
- **broadcastPing()**: Envia ping para todos os clientes conectados a cada 10 segundos
- Mantém conexões WebSocket vivas no servidor

## 📋 Configuração Necessária

### 1. Atualizar URI do WebSocket no Unity

**IMPORTANTE**: O servidor WebSocket está na porta **81**, não 80!

No Unity, configure o `serverUri` no VRManager:
```
ws://192.168.0.1:81
```

Ou se estiver usando IP diferente:
```
ws://192.168.4.1:81
```

### 2. Instalar Biblioteca no ESP32

No Arduino IDE, instale a biblioteca:
1. **Sketch → Include Library → Manage Libraries**
2. Procure por: **WebSocketsServer** ou **WebSockets**
3. Instale: **WebSockets by Markus Sattler**

### 3. Upload do Código no ESP32

1. Abra o arquivo `ESP32_WEBSOCKET_SERVER.ino` no Arduino IDE
2. Selecione a placa: **Tools → Board → ESP32 Dev Module**
3. Selecione a porta USB do ESP32
4. **Sketch → Upload**

### 4. Verificar Conexão

Após upload, abra o Serial Monitor (115200 baud):
- Deve aparecer: "Access Point iniciado com sucesso!"
- Deve aparecer: "WebSocket disponível em: ws://192.168.0.1:81"

## 🔍 Como Verificar se Está Funcionando

### No Unity (Logs):
```
📡 [User X] Ping WebSocket enviado (keep-alive)
✅ Conexão WebSocket bem-sucedida.
```

### No ESP32 (Serial Monitor):
```
[0] Cliente conectado de 192.168.0.XXX
WiFi: 1 / 10 | WebSocket: 1
```

### Se Ainda Desconectar:

1. **Verifique a porta**: Certifique-se de que está usando porta 81 no URI
2. **Verifique logs**: Veja se há erros de ping ou conexão
3. **Verifique distância**: Quest 2 muito longe do ESP32 pode causar desconexões
4. **Verifique canal WiFi**: Canal 44 (5GHz) deve estar livre de interferências

## ⚙️ Configurações Ajustáveis

No Unity (Inspector do VRManager):
- **connectionCheckInterval**: 3s (padrão) - Reduzir se ainda desconectar
- **pingInterval**: 10s (padrão) - Reduzir para 5s se necessário
- **maxReconnectAttempts**: 999 (nunca parar)

No ESP32 (código):
- **pingInterval**: 10000ms (10s) - Reduzir se necessário
- **wifiChannel**: 44 (5GHz) - Mudar se houver interferência

## 💡 Dicas Adicionais

1. **Mantenha Quest 2 próximo ao ESP32** (dentro de 5-10 metros)
2. **Evite interferências** (outros dispositivos WiFi na mesma frequência)
3. **Use canal 5GHz** (menos congestionado que 2.4GHz)
4. **Potência máxima** já está configurada (19.5dBm)

## 🚨 Troubleshooting

### Problema: Ainda desconecta
- Verifique se o URI está correto (porta 81)
- Verifique logs do ESP32 (Serial Monitor)
- Reduza `pingInterval` para 5s
- Reduza `connectionCheckInterval` para 2s

### Problema: Não conecta
- Verifique se ESP32 está rodando
- Verifique se biblioteca WebSockets está instalada
- Verifique se porta 81 está acessível
- Verifique firewall/antivírus

### Problema: Ping não funciona
- Verifique se `SendPingPeriodic` está sendo chamado (logs)
- Verifique se WebSocket está em estado Open
- Verifique se servidor está respondendo

