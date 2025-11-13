#!/bin/bash
echo "📋 Capturando logs completos..."
echo ""
echo "1. Limpando logs antigos..."
adb logcat -c

echo "2. Iniciando captura de logs..."
echo "   (Abra o app no celular agora)"
echo "   (Aguarde 10 segundos, depois pressione Ctrl+C)"
echo ""

adb logcat > logs_completo.txt
