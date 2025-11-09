# Guia de Configuração e Uso do Sistema de Bloqueio de Visualização

Este guia explica como configurar e ajustar o sistema de bloqueio de visualização para vídeos 360°.

## Introdução

O sistema de bloqueio de visualização permite controlar a direção em que o usuário pode olhar durante determinados intervalos de tempo em vídeos 360°. Existem dois modos de bloqueio:

1. **Modo 0 - Inverter Rotação da Cabeça**: Bloqueia a visão na posição onde o usuário estava olhando quando o bloqueio foi ativado.
2. **Modo 1 - Fixar em Ponto Específico**: Força o usuário a olhar para uma direção específica, independentemente de para onde ele esteja olhando.

## Interface de Configuração no Editor

No Inspector do objeto que contém o componente `VRManager`, você encontrará uma seção chamada "Ferramentas de Configuração de Bloqueio" que permite configurar os intervalos de bloqueio para cada vídeo.

### Como Adicionar um Novo Vídeo

1. Clique no botão "Adicionar Novo Vídeo".
2. Digite o nome exato do arquivo de vídeo, incluindo a extensão (ex: "rio.mp4").
3. Clique em "OK".

### Como Configurar Intervalos de Bloqueio

Para cada vídeo, você pode adicionar quantos intervalos de bloqueio desejar:

1. Expanda a seção do vídeo clicando no botão "►" à esquerda do nome do vídeo.
2. Clique em "Adicionar Novo Intervalo" para adicionar um intervalo de bloqueio.
3. Configure:
   - **Tempo Início**: Momento em segundos em que o bloqueio será ativado.
   - **Tempo Fim**: Momento em segundos em que o bloqueio será desativado.
   - **Bloqueio Parcial**: Se marcado, permite restringir apenas uma parte da visão.
   - **Ângulo de Restrição**: (Apenas para bloqueio parcial) Define o ângulo de visão disponível.
   - **Yaw (Horizontal)**: Ângulo horizontal para onde o usuário será forçado a olhar (0° = Norte, 90° = Leste, 180° = Sul, 270° = Oeste).
   - **Pitch (Vertical)**: Ângulo vertical para onde o usuário será forçado a olhar (0° = horizonte, 90° = cima, -90° = baixo).

## Como Testar e Ajustar os Ângulos

Para encontrar os ângulos corretos, você pode usar:

1. **No Editor**: Use o componente `ViewSetup` que mostra os ângulos atuais da câmera.
   - Entre no modo Play.
   - Utilize o modo VR e olhe na direção desejada.
   - No console, serão exibidos os ângulos atuais a cada poucos frames.
   - Use esses valores para configurar os ângulos de bloqueio.

2. **No dispositivo Quest**: 
   - Pressione o botão A do controle para exibir no console os ângulos atuais.
   - Anote esses valores para usar na configuração posteriormente.

## Ferramentas de Teste

No modo Play, a seção "Ferramentas de Teste" permite testar a visualização em ângulos específicos:

1. Expanda a seção "Ferramentas de Teste".
2. Configure os ângulos Yaw e Pitch desejados.
3. Clique em "Testar Visualização" para ver como ficará o bloqueio nessa direção.

## Configuração para Cada Vídeo

Você pode usar estas referências para configurar os pontos de interesse em cada vídeo:

### Rio.mp4
- Intervalo 1 (00:20-00:44): Use Yaw=0°, Pitch=0° para focar no Cristo Redentor.
- Intervalo 2 (02:39-03:48): Use Yaw=180°, Pitch=0° para focar na Baía de Guanabara.

### Amazonia.mp4
- Intervalo 1 (01:55-02:10): Use Yaw=0°, Pitch=0° para focar nas árvores, bloqueio parcial de 120°.
- Intervalo 2 (03:17-03:25): Use Yaw=180°, Pitch=15° para focar na vista do rio, bloqueio parcial de 100°.

### Noronha.mp4
- Intervalo 1 (00:40-00:59): Use Yaw=270°, Pitch=0° para focar no mar.

## Dicas Adicionais

- **Modo Recomendado**: O Modo 1 (fixar em ponto específico) geralmente funciona melhor no Meta Quest.
- **Suavidade da Transição**: Ajuste o valor de `Transition Smoothness` para controlar quão suave é a transição ao entrar ou sair de um bloqueio.
- **Velocidade de Rotação**: O valor de `Rotation Speed` controla quão rápido a esfera do vídeo gira para acompanhar o movimento da cabeça durante o bloqueio.

## Solução de Problemas

Se o bloqueio não estiver funcionando corretamente:

1. **Verifique as referências**: Certifique-se de que o `videoSphere` está corretamente atribuído.
2. **Modo de bloqueio**: Tente alternar entre os modos 0 e 1 para ver qual funciona melhor.
3. **Logs de debug**: Ative o `diagnosticMode` para ver mais informações no console.
4. **Referência correta da câmera**: O sistema tenta automaticamente encontrar a câmera do Quest, mas verifique se está sendo usada a câmera correta nos logs. 