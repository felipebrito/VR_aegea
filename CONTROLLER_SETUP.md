# Sistema de Controlador dos Óculos VR

Este documento explica como configurar e usar o sistema de controlador para os 4 Oculus headsets.

## Componentes Criados

### 1. OculusController.cs
Controlador principal que gerencia:
- Conexão WebSocket com o servidor
- Comunicação com os 4 Oculus headsets
- Envio de comandos de play/stop
- Recebimento de status (conexão, bateria, progresso)

### 2. OculusPanel.cs
Painel individual para cada Oculus que exibe:
- Seleção de idioma (PT, EN, ES)
- Status online/offline
- Percentual da bateria
- ID do Oculus

### 3. VideoPreview.cs
Componente para preview do vídeo no tablet:
- Reproduz o mesmo vídeo que está sendo exibido nos Oculus
- Mostra progresso do vídeo
- Exibe tempo atual e total

### 4. ControllerSceneHelper.cs
Script helper para criar a UI automaticamente no Unity Editor.

## Como Configurar

### Passo 1: Criar a Cena
1. No Unity Editor, crie uma nova cena chamada `ControllerScene`
2. Adicione um GameObject vazio chamado `ControllerSceneHelper`
3. Adicione o componente `ControllerSceneHelper` a este GameObject
4. Clique com o botão direito no componente e selecione "Create Controller Scene UI"
5. Isso criará automaticamente toda a UI necessária

### Passo 2: Configurar o OculusController
1. O `OculusController` será criado automaticamente
2. Configure a `serverPort` no Inspector (padrão: 80)
3. Deixe `serverIP` vazio para aceitar conexões de qualquer IP
4. (Opcional) Configure `serverIPText` para exibir o IP do servidor na UI
5. Verifique se os 4 painéis estão configurados corretamente
6. **IMPORTANTE**: Configure o `serverUri` no `VRManager` de cada Oculus para apontar para o IP do tablet

### Passo 3: Configurar os Painéis
Cada painel (`OculusPanel_1` a `OculusPanel_4`) deve ter:
- 3 botões de idioma (PT, EN, ES)
- Ícone de status online/offline
- Ícone e texto da bateria
- Texto do ID do Oculus

### Passo 4: Configurar o VideoPreview
O `VideoPreview` precisa de:
- Um `VideoPlayer` configurado
- Um `RawImage` para exibir o preview
- Um `Slider` para mostrar o progresso
- Textos para tempo atual e total

## Como Usar

### Iniciar uma Sessão
1. Selecione o idioma para cada um dos 4 Oculus nos painéis
2. Aguarde até que todos os Oculus estejam conectados (status ONLINE)
3. Clique no botão "INICIAR"
4. O sistema enviará o comando `play{id}_{idioma}` para cada Oculus
5. O preview começará a tocar automaticamente

### Parar uma Sessão
1. Clique no botão "PARAR"
2. O sistema enviará o comando `stop{id}` para cada Oculus
3. O preview será parado

## Arquitetura de Comunicação

**IMPORTANTE**: O tablet funciona como **SERVIDOR WebSocket** e os Oculus são os **CLIENTES** que se conectam ao tablet.

### Configuração do Servidor (Tablet)
- O `OculusController` inicia um servidor WebSocket na porta configurada (padrão: 80)
- O IP do servidor é exibido automaticamente na UI (se `serverIPText` estiver configurado)
- Os Oculus devem se conectar ao IP do tablet usando o `serverUri` no `VRManager`

### Mensagens Enviadas pelo Controlador (Tablet → Oculus)
- `play{id}_{idioma}` - Inicia vídeo no Oculus {id} no idioma especificado
- `stop{id}` - Para o vídeo no Oculus {id}
- `get_battery{id}` - Solicita status da bateria do Oculus {id}

### Mensagens Recebidas pelo Controlador (Oculus → Tablet)
- `vr_connected{id}` - Oculus {id} conectado (enviado quando o Oculus se conecta)
- `battery{id}:{percent}` - Status da bateria do Oculus {id}
- `percent{id}:{percent}` - Progresso do vídeo no Oculus {id}
- `video_ended{id}` - Vídeo terminou no Oculus {id}

## Status da Bateria

O sistema obtém o status da bateria do Oculus de duas formas:
1. **OVRPlugin** (preferencial) - Usa o SDK do Oculus diretamente
2. **Android API** (fallback) - Usa a API nativa do Android

O status é enviado automaticamente:
- Quando o Oculus conecta
- A cada 30 segundos durante a sessão
- Quando solicitado pelo controlador

## Vídeos Suportados

Os vídeos devem estar localizados em:
- `StreamingAssets/` (recomendado)
- `/sdcard/Download/` (Android)
- `/storage/emulated/0/Download/` (Android alternativo)

Arquivos de vídeo esperados:
- `Experiencia_Aegea_Cop2025_Portugues.mp4` (PT)
- `Experiencia_Aegea_Cop2025_Ingles.mp4` (EN)
- `Experiencia_Aegea_Cop2025_Espannhol.mp4` (ES)

## Notas Importantes

1. **Servidor WebSocket**: O tablet funciona como servidor - o `OculusController` inicia o servidor automaticamente
2. **Rede**: Todos os dispositivos (tablet e Oculus) devem estar na mesma rede Wi-Fi
3. **IP do Tablet**: O IP do tablet é exibido automaticamente quando o servidor inicia (verifique os logs ou configure `serverIPText`)
4. **Configuração dos Oculus**: Cada Oculus deve ter o `serverUri` no `VRManager` configurado para o IP do tablet (ex: `ws://192.168.1.100:80`)
5. **Permissões**: 
   - No Android, o servidor pode precisar de permissões de rede
   - Os Oculus precisam ter permissão de armazenamento para acessar os vídeos
6. **Sincronização**: O preview no tablet acompanha o progresso dos Oculus através das mensagens `percent{id}:{percent}`

## Troubleshooting

### Oculus não conecta
- Verifique se o servidor WebSocket está rodando
- Verifique se o IP está correto
- Verifique a conexão de rede

### Bateria não aparece
- Verifique se o Oculus está realmente conectado
- Verifique se o SDK do Oculus está instalado
- O status da bateria pode levar alguns segundos para aparecer

### Preview não toca
- Verifique se os vídeos estão nos locais corretos
- Verifique se o VideoPlayer está configurado
- Verifique se o RenderTexture está configurado corretamente

