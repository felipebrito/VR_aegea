# VR Video Player com Controle de Rotação

Aplicativo de visualização de vídeos em VR com controle de rotação, pontos focais automáticos e interface de administração via WebSocket.

## Características Principais

- Reprodução de vídeos 360° em VR
- Sistema de controle de rotação com pontos focais
- Transições suaves entre ângulos de visualização
- Interface de administração via WebSocket
- Modo offline para demonstrações
- Reprodução local dos vídeos (não requer streaming)
- Sistema de bloqueio de visualização configurável
- Suporte para múltiplos vídeos

## Requisitos

- Unity 2022.3 ou superior
- Oculus Integration Package
- Android Build Support (para builds Quest)
- Meta Quest 1 ou 2

## Estrutura do Projeto

```
Assets/
├── Scripts/
│   ├── VRManager.cs           # Gerenciador principal do sistema VR
│   └── SmoothOrbitFollower.cs # Sistema de órbita suave para objetos
├── Scenes/
│   └── Main.unity            # Cena principal do aplicativo
└── StreamingAssets/         # Pasta alternativa para vídeos (opcional)
```

## Configuração de Vídeos

Os vídeos são reproduzidos diretamente do dispositivo, não necessitando de streaming ou múltiplos builds:

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

## Comunicação WebSocket

### Conexão
- URL padrão: `ws://192.168.1.30:8181`
- Configurável via `serverUri` no VRManager
- Reconexão automática em caso de falha

### Comandos Disponíveis

| Comando | Formato | Descrição | Exemplo |
|---------|---------|-----------|---------|
| Play | `play:filename.mp4` | Inicia reprodução de vídeo | `play:rio.mp4` |
| Pause | `pause` | Pausa o vídeo atual | `pause` |
| Resume | `resume` | Retoma reprodução | `resume` |
| Stop | `stop` | Para a reprodução | `stop` |
| Seek | `seek:seconds` | Pula para tempo específico | `seek:120` |
| Mensagem | `aviso:texto` | Exibe mensagem na interface | `aviso:Iniciando tour` |

### Respostas do Cliente

| Mensagem | Formato | Descrição |
|----------|---------|-----------|
| Timecode | `TIMECODE:seconds` | Tempo atual do vídeo |
| Status | `STATUS:state` | Estado atual do player |
| Info | `CLIENT_INFO:data` | Informações do dispositivo |

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

1. **Ativação**:
   - Automática após falhas de conexão
   - Manual via `offlineMode = true`
   - Após timeout configurável

2. **Funcionalidades**:
   - Carregamento automático do primeiro vídeo
   - Interface de diagnóstico
   - Manutenção de todas funcionalidades locais

## Features Adicionais

1. **Sistema de Diagnóstico**:
   - Logs detalhados
   - Interface de debug in-game
   - Monitoramento de conexão

2. **Gerenciamento de Memória**:
   - Liberação automática de recursos
   - Controle de carregamento de vídeos
   - Otimização para VR

3. **Transições Suaves**:
   - Fade in/out entre vídeos
   - Interpolação suave de rotação
   - Retorno suave ao ponto focal

4. **Permissões Android**:
   - Solicitação automática de acesso ao armazenamento
   - Fallback para StreamingAssets
   - Tratamento de erros de permissão

## Solução de Problemas

1. **Vídeos não carregam**:
   - Verifique o formato do vídeo
   - Confirme as permissões de armazenamento
   - Verifique o caminho configurado

2. **Problemas de Conexão**:
   - Verifique o endereço do servidor
   - Confirme a rede local
   - Verifique logs de diagnóstico

3. **Problemas de Rotação**:
   - Verifique configurações de bloqueio
   - Ajuste valores de maxAngle e resetSpeed
   - Confirme orientação inicial do vídeo

## Licença

Todos os direitos reservados. 