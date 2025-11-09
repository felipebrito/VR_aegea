# Resumo das Alterações

Este documento resume as principais alterações feitas ao sistema para corrigir o problema de bloqueio de visualização nos vídeos 360°.

## 1. Sistema de Bloqueio Melhorado

### VRManager.cs

- **Dois Modos de Bloqueio**:
  - Modo 0: Inverter rotação da cabeça (método original)
  - Modo 1: Fixar em ponto específico (novo método mais estável)

- **Configuração Expansível**:
  - Adicionada classe `VideoLockSettings` para permitir configurar bloqueios por vídeo no Inspector
  - Expondo todos os parâmetros de tempo e ângulos no editor para ajuste fácil

- **Interface com o Oculus SDK**:
  - Adicionado suporte para detectar a câmera do Oculus automaticamente
  - Otimizado para usar o centro do olho como referência para cálculos de rotação

- **Transições Suaves**:
  - Adicionado parâmetro `transitionSmoothness` para controlar a suavidade das transições
  - Implementada transição gradual ao iniciar ou terminar um bloqueio

## 2. Ferramentas de Configuração

### ViewSetup.cs

- **Ferramenta de Visualização de Ângulos**:
  - Mostra os ângulos atuais da câmera (Yaw e Pitch)
  - Desenha pontos cardeais para orientação
  - Permite testar ângulos específicos em tempo real

- **Compatibilidade com VR**:
  - Implementado suporte para o botão A do controle do Quest para copiar valores
  - Detecção automática do centro do olho do Quest

### Interface do Editor

- **Editor Custom (VRManagerEditor.cs)**:
  - Interface gráfica para adicionar e configurar vídeos
  - Editor personalizado para configurar intervalos de tempo de bloqueio
  - Ferramenta de teste para experimentar ângulos em tempo real

- **ReadOnlyAttribute**:
  - Permite mostrar valores de debug como somente leitura no Inspector

## 3. Diagnóstico e Solução de Problemas

- **Log Detalhado**:
  - Informações detalhadas sobre os intervalos de bloqueio de cada vídeo
  - Logs no console para mostrar quando os bloqueios são ativados/desativados
  - Mensagens de debug para identificar problemas de referência

- **Guia de Uso**:
  - Documentação completa sobre como configurar o sistema
  - Valores de referência para os ângulos de cada vídeo
  - Instruções passo a passo para ajustar os pontos de interesse

## Configuração Inicial

Os seguintes valores já foram configurados como padrão:

- **Rio.mp4**:
  - 00:20-00:44: Yaw=0°, Pitch=0° (Cristo Redentor)
  - 02:39-03:48: Yaw=180°, Pitch=0° (Baía de Guanabara)

- **Amazonia.mp4**:
  - 01:55-02:10: Yaw=0°, Pitch=0°, Parcial 120° (Árvores)
  - 03:17-03:25: Yaw=180°, Pitch=15°, Parcial 100° (Vista do rio)

- **Noronha.mp4**:
  - 00:40-00:59: Yaw=270°, Pitch=0° (Mar)

A aplicação está configurada para usar o Modo 1 (fixar em ponto específico) por padrão, pois geralmente produz resultados mais consistentes no Meta Quest.

## Próximos Passos

Para usar o sistema:

1. Compile e instale a aplicação no Meta Quest
2. Teste os bloqueios nos intervalos configurados
3. Se necessário, ajuste os ângulos usando as ferramentas de diagnóstico
4. Salve as configurações atualizadas no editor 