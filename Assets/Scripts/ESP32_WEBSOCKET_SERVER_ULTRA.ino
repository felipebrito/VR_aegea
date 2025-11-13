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
const unsigned long checkInterval = 3000; // Verificar a cada 3 segundos (mais frequente)
unsigned long lastPing = 0;
const unsigned long pingInterval = 3000; // Ping a cada 3 segundos (MUITO MAIS FREQUENTE para máxima estabilidade)

// CANAIS 5GHz MENOS CONGESTIONADOS (escolha um que não esteja sendo usado por outros roteadores)
// Canais disponíveis: 36, 40, 44, 48, 149, 153, 157, 161, 165
// Evite canais: 1, 6, 11 (2.4GHz) e canais próximos a outros roteadores
// Recomendado: 149, 153, 157 (menos usados em ambientes domésticos)
int wifiChannel = 149; // CANAL MENOS CONGESTIONADO (5GHz)

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
    Serial.println("AEGEA ESP32 - Access Point ULTRA ROBUSTO");
    Serial.println("========================================");
    
    Serial.println("Iniciando Access Point...");
    WiFi.mode(WIFI_AP);
    
    // CONFIGURAÇÕES ANTI-INTERFERÊNCIA
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
    Serial.println(" (5GHz - MENOS CONGESTIONADO)");
    
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
    Serial.println(" (5GHz)");
    Serial.println("Banda: 5GHz");
    Serial.println("Potência: 19.5dBm (MÁXIMA)");
    Serial.println("Ping Interval: 3 segundos (MUITO FREQUENTE)");
    Serial.println("========================================");
    Serial.println("DICA: Se houver interferência, tente mudar");
    Serial.println("o canal para: 36, 40, 44, 48, 153, 157, 161, 165");
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
        Serial.printf("WiFi: %d / 10 | WebSocket: %d | Canal: %d (5GHz) | Ping: 3s\n", 
                      connected, wsConnected, wifiChannel);
        lastCheck = millis();
    }
}

