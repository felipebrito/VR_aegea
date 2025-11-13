// Exemplo de correção do erro "jump to case label"
// O problema está na função webSocketEvent

void webSocketEvent(uint8_t num, WStype_t type, uint8_t * payload, size_t length) {
    switch(type) {
        case WStype_DISCONNECTED:
            Serial.printf("[%u] Disconnected!\n", num);
            break;
            
        case WStype_CONNECTED: {
            IPAddress ip = webSocket.remoteIP(num);
            Serial.printf("[%u] Connected from %d.%d.%d.%d url: %s\n", num, ip[0], ip[1], ip[2], ip[3], payload);
            break;
        }
        
        case WStype_TEXT: {
            // SOLUÇÃO: Envolver o conteúdo do case em chaves {} para criar escopo local
            String message = String((char*)payload);
            Serial.printf("[%u] get Text: %s\n", num, message.c_str());
            
            // Seu código de processamento aqui
            // webSocket.sendTXT(num, "Message received");
            break;
        }
        
        case WStype_BIN:
            Serial.printf("[%u] get binary length: %u\n", num, length);
            // hexdump(payload, length);
            break;
            
        case WStype_PING:
            Serial.printf("[%u] get ping\n", num);
            break;
            
        case WStype_PONG:
            Serial.printf("[%u] get pong\n", num);
            break;
            
        default:
            Serial.printf("[%u] Unknown event type: %d\n", num, type);
            break;
    }
}

