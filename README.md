# VR Video Player com Controle Multi-Usuário

Aplicativo de visualização de vídeos em VR com controle remoto via tablet, suporte para múltiplos usuários simultâneos (até 4 Quest conectados), sistema de timecode, monitoramento de bateria e interface de administração via WebSocket.

## ⚠️ NOTA IMPORTANTE - COP 2025

**Esta versão está sendo utilizada em produção pelo Leo no stand AEGEA durante a COP 2025.**

O sistema foi testado e otimizado para suportar múltiplas conexões simultâneas (até 4 Quest) com estabilidade garantida através de:
- Sincronização thread-safe de todas as operações de rede
- Prevenção de race conditions em conexões múltiplas
- Otimização de frequência de ping e requisições de bateria
- Sistema robusto de reconexão automática

**Status:** ✅ Estável e pronto para produção

---

## Características Principais

- **Reprodução de vídeos 360° em VR** com suporte multi-idioma (PT, EN, ES)
- **Sistema de controle remoto via tablet** com interface WebSocket
- **Suporte para até 4 Quest simultâneos** com identificação individual
- **Monitoramento de bateria em tempo real** para todos os dispositivos
- **Sistema de timecode** para sincronização de progresso
- **Transições suaves** entre vídeos e estados
- **Modo offline** para demonstrações sem servidor
- **Reprodução local dos vídeos** (não requer streaming)
- **Sistema de bloqueio de visualização** configurável
- **Reconexão automática** com tratamento robusto de erros

## Requisitos

- Unity 2022.3 ou superior
- Oculus Integration Package
- Android Build Support (para builds Quest e Tablet)
- Meta Quest 1, 2 ou 3
- Tablet Android para controle (opcional, mas recomendado)

## Arquitetura do Sistema

### Componentes Principais

```
Assets/
├── Scripts/
│   ├── VRManager.cs              # Cliente VR (Quest) - Gerencia conexão, vídeo e bateria
│   ├── OculusController.cs       # Servidor Tablet - Gerencia múltiplas conexões Quest
│   ├── OculusPanel.cs            # UI individual para cada Quest no tablet
│   ├── VideoPreview.cs           # Preview de vídeo no tablet
│   ├── ConfigMenuHelper.cs       # Menu de configuração
│   └── ConnectionHelper.cs       # Utilitários de conexão
├── Scenes/
│   └── aegea.unity              # Cena principal do aplicativo
└── StreamingAssets/             # Vídeos e recursos
```

### Fluxo de Comunicação

```
Tablet (OculusController)          Quest 1-4 (VRManager)
     │                                    │
     │  ┌─ WebSocket Server (8181)       │
     │  │                                 │
     │  ├─ Aceita conexões               │
     │  │                                 │
     │  └─ Gerencia múltiplas conexões    │
     │                                    │
     │                                    ├─ Conecta via WebSocket
     │                                    ├─ Envia: vr_connected{1-4}
     │                                    ├─ Envia: CLIENT_INFO (bateria, IP, OS)
     │                                    └─ Recebe comandos de vídeo
     │
     ├─ Envia: play:idioma               │
     ├─ Envia: pause/resume/stop         │
     ├─ Envia: get_battery{1-4}          │
     │                                    │
     │                                    ├─ Responde: battery{1-4}:XX%
     │                                    ├─ Responde: CLIENT_INFO:...
     │                                    └─ Responde: TIMECODE:seconds
```

## Configuração de Vídeos

Os vídeos são reproduzidos diretamente do dispositivo, não necessitando de streaming:

1. **Formato Recomendado**:
   ```bash
   ffmpeg -y -hwaccel cuda -hwaccel_output_format cuda -i "video_original.mp4" \
   -c:v hevc_nvenc -preset p1 -tune hq -rc:v vbr_hq \
   -b:v 12M -maxrate 15M -bufsize 20M -spatial-aq 1 \
   -vf "scale_cuda=3072:1536" -c:a aac -b:a 128k -ac 2 "video_convertido.mp4"
   ```

2. **Localização dos Vídeos**:
   - Pasta `Download` do Quest (recomendado)
   - StreamingAssets do aplicativo (alternativa)
   - Configurável via `useExternalStorage` no VRManager

3. **Mapeamento de Idiomas**:
   - `pt` → `Experiencia_Aegea_Cop2025_Portugues.mp4`
   - `en` → `Experiencia_Aegea_Cop2025_Ingles.mp4`
   - `es` → `Experiencia_Aegea_Cop2025_Espannhol.mp4`

## Comunicação WebSocket

### Configuração do Servidor (Tablet)

- **Porta padrão:** `8181`
- **IP:** Configurável via interface ou código
- **Protocolo:** WebSocket custom (handshake manual)
- **Timeout:** 10 segundos
- **Ping interval:** 5 segundos (otimizado para múltiplas conexões)

### Configuração do Cliente (Quest)

- **URL padrão:** `ws://192.168.1.123:8181` (configurável)
- **KeepAlive:** 5 segundos
- **Timeout de conexão:** 10 segundos
- **Reconexão automática:** Sim, com backoff exponencial

### Comandos Disponíveis (Tablet → Quest)

| Comando | Formato | Descrição | Exemplo |
|---------|---------|-----------|---------|
| Play | `play:PT` ou `play:EN` ou `play:ES` | Inicia reprodução por idioma | `play:PT` |
| Pause | `pause` | Pausa o vídeo atual | `pause` |
| Resume | `resume` | Retoma reprodução | `resume` |
| Stop | `stop` | Para a reprodução | `stop` |
| Seek | `seek:seconds` | Pula para tempo específico | `seek:120` |
| Get Battery | `get_battery{1-4}` | Solicita nível de bateria | `get_battery1` |

### Respostas do Cliente (Quest → Tablet)

| Mensagem | Formato | Descrição |
|----------|---------|-----------|
| Connection | `vr_connected{1-4}` | Identificação na conexão |
| Client Info | `CLIENT_INFO:{name}\|{IP}\|{OS}\|{battery}%` | Informações completas do dispositivo |
| Battery | `battery{1-4}:XX%` | Nível de bateria em percentual |
| Timecode | `TIMECODE:seconds` | Tempo atual do vídeo |
| Video Ended | `video_ended{1-4}` | Notificação de fim de vídeo |

## Sistema de Sincronização e Thread Safety

### Melhorias Implementadas (v2.0)

O sistema foi completamente refatorado para suportar múltiplas conexões simultâneas de forma estável:

#### OculusController.cs (Servidor Tablet)

1. **Sincronização Thread-Safe:**
   - `connectionsLock` protege todos os dicionários compartilhados
   - Operações de leitura/escrita sincronizadas
   - Cópias de dicionários criadas dentro do lock antes de iterar

2. **Prevenção de Race Conditions:**
   - Flags `isPinging` e `isRequestingBattery` previnem tasks sobrepostas
   - Verificação de `client.Connected` antes de enviar mensagens
   - Limpeza de conexões protegida por lock

3. **Otimização de Rede:**
   - Ping reduzido de 3s para 5s (menos sobrecarga)
   - Requisições de bateria de ~5s para ~10s
   - Tratamento de erros não bloqueante

#### VRManager.cs (Cliente Quest)

1. **Sincronização WebSocket:**
   - `webSocketLock` protege todas as operações no WebSocket
   - Flag `isReceivingMessages` previne múltiplas tasks de recebimento
   - Prevenção de múltiplas reconexões simultâneas

2. **Gerenciamento de Estado:**
   - `isReconnecting` protegido por lock
   - Cancelamento de `InvokeRepeating` antes de criar novos
   - Cleanup thread-safe em `OnDestroy` e `OnApplicationQuit`

3. **Robustez de Conexão:**
   - Reconexão automática com backoff
   - Modo offline funcional
   - Tratamento silencioso de erros de rede

## Sistema de Monitoramento de Bateria

### Implementação Multi-Camada

O sistema utiliza três métodos para obter o nível de bateria (em ordem de prioridade):

1. **SystemInfo.batteryLevel** (Unity nativo)
2. **OVRPlugin.GetSystemBatteryLevel()** (SDK Oculus)
3. **Android API nativa** (fallback)

### Envio Automático

- **Na conexão:** Enviado via `CLIENT_INFO` após handshake
- **Solicitação manual:** Via comando `get_battery{1-4}`
- **Periódico:** A cada 30 segundos via `CLIENT_INFO`
- **No Editor:** Valor simulado oscilando entre 60-95% para testes

## Sistema de Bloqueio de Visualização

O sistema permite definir momentos específicos onde a visualização é restrita:

```csharp
public class LockTimeRange {
    public float startTime;    // Tempo inicial em segundos
    public float endTime;      // Tempo final em segundos
    public float maxAngle;     // Ângulo máximo de rotação permitido
    public float resetSpeed;   // Velocidade de retorno ao centro
}
```

### Configuração via Unity Editor
1. Selecione o VRManager na cena
2. Expanda "Lock Time Ranges"
3. Configure os intervalos para cada vídeo
4. Ajuste ângulos e velocidades conforme necessário

## Modo Offline

O aplicativo pode funcionar sem conexão ao servidor:

1. **Ativação:**
   - Automática após falhas de conexão
   - Manual via `offlineMode = true`
   - Após timeout configurável

2. **Funcionalidades:**
   - Carregamento automático do primeiro vídeo
   - Interface de diagnóstico
   - Manutenção de todas funcionalidades locais

## Features Adicionais

1. **Sistema de Diagnóstico:**
   - Logs detalhados com emojis para fácil identificação
   - Interface de debug in-game
   - Monitoramento de conexão em tempo real

2. **Gerenciamento de Memória:**
   - Liberação automática de recursos
   - Controle de carregamento de vídeos
   - Otimização para VR

3. **Transições Suaves:**
   - Fade in/out entre vídeos
   - Interpolação suave de rotação
   - Retorno suave ao ponto focal

4. **Permissões Android:**
   - Solicitação automática de acesso ao armazenamento
   - Fallback para StreamingAssets
   - Tratamento de erros de permissão

## Solução de Problemas

### Problemas de Conexão

1. **Múltiplas conexões causam crash:**
   - ✅ **Resolvido:** Sistema agora usa locks e sincronização thread-safe
   - Verifique se está usando a versão mais recente

2. **Conexão não estabelece:**
   - Verifique o endereço IP do servidor (tablet)
   - Confirme que a porta 8181 está aberta
   - Verifique logs de diagnóstico

3. **Desconexões frequentes:**
   - Verifique a qualidade da rede Wi-Fi
   - Ping interval foi otimizado para 5s
   - Sistema tem reconexão automática

### Problemas de Bateria

1. **Bateria mostra 0%:**
   - ✅ **Resolvido:** Sistema agora usa múltiplos métodos de leitura
   - Verifique se o dispositivo está realmente carregado
   - No Editor, valor simulado oscila entre 60-95%

2. **Bateria não atualiza:**
   - Verifique se `CLIENT_INFO` está sendo enviado
   - Confirme que o tablet está recebendo as mensagens
   - Verifique logs no Quest e no Tablet

### Problemas de Vídeo

1. **Vídeos não carregam:**
   - Verifique o formato do vídeo (HEVC recomendado)
   - Confirme as permissões de armazenamento
   - Verifique o caminho configurado

2. **Vídeo não inicia:**
   - Verifique se o idioma foi selecionado no tablet
   - Confirme que o arquivo existe no dispositivo
   - Verifique logs de erro

3. **Problemas de Rotação:**
   - Verifique configurações de bloqueio
   - Ajuste valores de maxAngle e resetSpeed
   - Confirme orientação inicial do vídeo

## Changelog

### v2.0 - Estabilidade Multi-Conexão (COP 2025)

**Melhorias Críticas:**
- ✅ Sincronização thread-safe completa em `OculusController.cs`
- ✅ Proteção de WebSocket com locks em `VRManager.cs`
- ✅ Prevenção de race conditions em operações assíncronas
- ✅ Otimização de frequência de ping e requisições
- ✅ Sistema robusto de reconexão automática
- ✅ Melhoria na leitura de bateria com múltiplos fallbacks
- ✅ Sistema de `CLIENT_INFO` para monitoramento completo

**Otimizações:**
- Ping interval: 3s → 5s
- Battery requests: ~5s → ~10s
- Thread-safe operations em todos os dicionários
- Prevenção de tasks sobrepostas

**Status:** ✅ Testado e estável para produção (4 Quest simultâneos)

### v1.0 - Versão Inicial
- Sistema básico de controle de vídeo
- Suporte para múltiplos idiomas
- Interface de tablet
- Monitoramento básico de bateria

## Licença

Todos os direitos reservados.

---

**Desenvolvido para AEGEA - COP 2025**
