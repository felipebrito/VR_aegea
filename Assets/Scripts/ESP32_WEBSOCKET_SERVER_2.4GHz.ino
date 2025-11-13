#include <WiFi.h>
#include <DNSServer.h>
#include <WebServer.h>
#include <WebSocketsServer.h>

const char* ssid = "AEGEA-ESP";
const char* password = "12345678";
IPAddress apIP(192, 168, 0, 1);

DNSServer dnsServer;
WebServer webServer(80);
WebSocketsServer webSocket = WebSocketsServer(81);

unsigned long lastCheck = 0;
const unsigned long checkInterval = 3000; // Verificar a cada 3 segundos
unsigned long lastPing = 0;
const unsigned long pingInterval = 3000; // Ping a cada 3 segundos (MUITO FREQUENTE)

// CANAIS 2.4GHz MENOS CONGESTIONADOS
// Baseado no scan WiFi do usuário:
// - Canal 1: 10 redes (CONGESTIONADO - EVITAR)
// - Canal 6: 13 redes (MUITO CONGESTIONADO - EVITAR!)
// - Canal 9: 2 redes (BOM - menos congestionado)
// - Canal 11: 2 redes (BOM - mas já está usando, pode ter interferência)
// 
// CANAIS RECOMENDADOS (2.4GHz):
// - Canal 3: Geralmente menos usado (se não estiver no scan, pode estar livre)
// - Canal 9: Apenas 2 redes (boa opção)
// - Canal 13: Se disponível no seu país (menos usado)
//
// IMPORTANTE: Canais 2.4GHz não se sobrepõem completamente:
// - Canais 1, 6, 11 não se sobrepõem (melhor escolha)
// - Canais 3, 9, 13 também não se sobrepõem
// - Evite canais próximos (ex: 1 e 2, 6 e 7, 11 e 12)
//
// RECOMENDAÇÃO: Use Canal 3 ou 13 (se disponível) para evitar interferência
// Se não funcionar, tente Canal 9

int wifiChannel = 3; // CANAL MENOS CONGESTIONADO (2.4GHz)
// Alternativas: 9, 13 (se disponível no seu país)

void webSocketEvent(uint8_t num, WStype_t type, uint8_t * payload, size_t length) {
    switch(type) {
        case WStype_DISCONNECTED:
            Serial.printf("[%u] Cliente desconectado!\n", num);
            break;
        case WStype_CONNECTED:
            {
                IPAddress ip = webSocket.remoteIP(num);
                Serial.printf("[%u] Cliente conectado de %d.%d.%d.%d\n", num, ip[0], ip[1], ip[2], ip[3]);
                webSocket.sendTXT(num, "Connected");
            }
            break;
        case WStype_TEXT:
            Serial.printf("[%u] Mensagem recebida: %s\n", num, payload);
            String message = String((char*)payload);
            if (message.startsWith("PING:")) {
                webSocket.sendTXT(num, "PONG");
            } else {
                webSocket.sendTXT(num, payload);
            }
            break;
        case WStype_PING:
            Serial.printf("[%u] Ping recebido\n", num);
            // Biblioteca responde automaticamente com PONG
            break;
        case WStype_PONG:
            Serial.printf("[%u] Pong recebido\n", num);
            break;
        default:
            break;
    }
}

void send204() {
    webServer.sendHeader("Connection", "close");
    webServer.sendHeader("Cache-Control", "no-cache, no-store, must-revalidate");
    webServer.sendHeader("Pragma", "no-cache");
    webServer.send(204);
}

void send200(const String& contentType, const String& content) {
    webServer.sendHeader("Connection", "close");
    webServer.sendHeader("Cache-Control", "no-cache, no-store, must-revalidate");
    webServer.sendHeader("Pragma", "no-cache");
    webServer.send(200, contentType, content);
}

void setup() {
    Serial.begin(115200);
    delay(500);
    
    Serial.println("========================================");
    Serial.println("AEGEA ESP32 - Access Point 2.4GHz");
    Serial.println("OTIMIZADO PARA AMBIENTE COM INTERFERÊNCIA");
    Serial.println("========================================");
    
    Serial.println("Iniciando Access Point...");
    WiFi.mode(WIFI_AP);
    
    // CONFIGURAÇÕES ANTI-INTERFERÊNCIA PARA 2.4GHz
    WiFi.setTxPower(WIFI_POWER_19_5dBm); // Máxima potência
    WiFi.setSleep(false); // DESABILITAR SLEEP - conexão sempre ativa
    WiFi.setAutoReconnect(true); // Reconectar automaticamente
    
    Serial.println("Potência de transmissão: 19.5dBm (MÁXIMA)");
    Serial.println("WiFi Sleep: DESABILITADO");
    Serial.println("Auto-Reconnect: HABILITADO");
    
    if (!WiFi.softAPConfig(apIP, apIP, IPAddress(255, 255, 255, 0))) {
        Serial.println("ERRO: Falha ao configurar AP!");
        return;
    }
    
    bool hidden = false;
    int maxConnections = 10;
    
    Serial.print("Configurando AP no canal ");
    Serial.print(wifiChannel);
    Serial.println(" (2.4GHz - MENOS CONGESTIONADO)");
    Serial.println("NOTA: ESP32 padrão suporta apenas 2.4GHz");
    
    if (!WiFi.softAP(ssid, password, wifiChannel, hidden, maxConnections)) {
        Serial.println("ERRO: Falha ao iniciar AP!");
        return;
    }
    
    delay(500);
    IPAddress ip = WiFi.softAPIP();
    
    if (ip.toString() == "0.0.0.0") {
        Serial.println("ERRO: AP não obteve IP!");
        return;
    }
    
    Serial.println("========================================");
    Serial.println("Access Point iniciado com SUCESSO!");
    Serial.println("========================================");
    Serial.print("SSID: "); Serial.println(ssid);
    Serial.print("IP do AP: "); Serial.println(ip);
    Serial.print("Canal WiFi: "); Serial.print(wifiChannel);
    Serial.println(" (2.4GHz)");
    Serial.println("Banda: 2.4GHz (ESP32 padrão)");
    Serial.println("Potência: 19.5dBm (MÁXIMA)");
    Serial.println("Ping Interval: 3 segundos (MUITO FREQUENTE)");
    Serial.println("========================================");
    Serial.println("ANÁLISE DO AMBIENTE WiFi:");
    Serial.println("- Canal 1: 10 redes (CONGESTIONADO)");
    Serial.println("- Canal 6: 13 redes (MUITO CONGESTIONADO!)");
    Serial.println("- Canal 9: 2 redes (BOM)");
    Serial.println("- Canal 11: 2 redes (BOM, mas já usado)");
    Serial.println("========================================");
    Serial.println("CANAIS RECOMENDADOS (2.4GHz):");
    Serial.println("1. Canal 3 (atual) - menos congestionado");
    Serial.println("2. Canal 9 - apenas 2 redes no scan");
    Serial.println("3. Canal 13 - se disponível no seu país");
    Serial.println("========================================");
    Serial.println("DICA: Se ainda houver interferência,");
    Serial.println("mude o canal na linha 18 do código:");
    Serial.println("int wifiChannel = 9; // ou 13");
    Serial.println("========================================");
    
    dnsServer.start(53, "*", apIP);
    webServer.begin();
    webSocket.begin();
    webSocket.onEvent(webSocketEvent);
    
    Serial.println("DNS, Web Server e WebSocket iniciados.");
    Serial.println("WebSocket disponível em: ws://192.168.0.1:81");
}

void loop() {
    dnsServer.processNextRequest();
    webServer.handleClient();
    webSocket.loop(); // CRÍTICO - processar eventos WebSocket frequentemente
    
    yield();
    
    // PING MUITO MAIS FREQUENTE (a cada 3 segundos) para manter conexões vivas
    if (millis() - lastPing > pingInterval) {
        webSocket.broadcastPing(); // Envia ping para TODOS os clientes conectados
        lastPing = millis();
    }
    
    // Verificação mais frequente (a cada 3 segundos)
    if (millis() - lastCheck > checkInterval) {
        int connected = WiFi.softAPgetStationNum();
        int wsConnected = webSocket.connectedClients();
        Serial.printf("WiFi: %d / 10 | WebSocket: %d | Canal: %d (2.4GHz) | Ping: 3s\n", 
                      connected, wsConnected, wifiChannel);
        lastCheck = millis();
    }
}

