#include <WiFi.h>
#include <DNSServer.h>
#include <WebServer.h>
#include <WebSocketsServer.h>  // Biblioteca para servidor WebSocket

const char* ssid = "AEGEA-ESP";
const char* password = "12345678";
IPAddress apIP(192, 168, 0, 1);

DNSServer dnsServer;
WebServer webServer(80);
WebSocketsServer webSocket = WebSocketsServer(81);  // Porta 81 para WebSocket

unsigned long lastCheck = 0;
const unsigned long checkInterval = 5000; // Verificar a cada 5 segundos
unsigned long lastPing = 0;
const unsigned long pingInterval = 10000; // Ping a cada 10 segundos para manter conexões vivas

// Handler de eventos WebSocket
void webSocketEvent(uint8_t num, WStype_t type, uint8_t * payload, size_t length) {
    switch(type) {
        case WStype_DISCONNECTED:
            Serial.printf("[%u] Cliente desconectado!\n", num);
            break;
        case WStype_CONNECTED:
            {
                IPAddress ip = webSocket.remoteIP(num);
                Serial.printf("[%u] Cliente conectado de %d.%d.%d.%d\n", num, ip[0], ip[1], ip[2], ip[3]);
                // Enviar mensagem de boas-vindas
                webSocket.sendTXT(num, "Connected");
            }
            break;
        case WStype_TEXT:
            Serial.printf("[%u] Mensagem recebida: %s\n", num, payload);
            // Echo da mensagem (ou processar comando aqui)
            // Se for PING, responder com PONG
            String message = String((char*)payload);
            if (message.startsWith("PING:")) {
                // Responder com PONG
                webSocket.sendTXT(num, "PONG");
            } else {
                // Echo da mensagem
                webSocket.sendTXT(num, payload);
            }
            break;
        case WStype_PING:
            Serial.printf("[%u] Ping recebido\n", num);
            // WebSocket library responde automaticamente com PONG
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
    
    Serial.println("Iniciando Access Point...");
    WiFi.mode(WIFI_AP);
    WiFi.setTxPower(WIFI_POWER_19_5dBm);
    Serial.println("Potência de transmissão configurada: 19.5dBm (máxima)");
    
    if (!WiFi.softAPConfig(apIP, apIP, IPAddress(255, 255, 255, 0))) {
        Serial.println("ERRO: Falha ao configurar AP!");
        return;
    }
    
    // Canal ajustado para 44 (5GHz)
    int wifiChannel = 44;
    bool hidden = false;
    int maxConnections = 10;
    
    Serial.print("Configurando AP no canal ");
    Serial.println(wifiChannel);
    
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
    
    // Configurações importantes para manter conexão estável
    WiFi.setSleep(false);  // Desabilitar sleep WiFi - mantém conexão ativa
    WiFi.setAutoReconnect(true);  // Reconectar automaticamente
    
    Serial.println("========================================");
    Serial.println("Access Point iniciado com sucesso!");
    Serial.println("========================================");
    Serial.print("SSID: "); Serial.println(ssid);
    Serial.print("IP do AP: "); Serial.println(ip);
    Serial.print("Canal WiFi: "); Serial.println(wifiChannel);
    Serial.println("Banda: 5GHz");
    Serial.println("WiFi Sleep: DESABILITADO (conexão sempre ativa)");
    Serial.println("========================================");
    
    dnsServer.start(53, "*", apIP);
    webServer.begin();
    webSocket.begin();  // Iniciar servidor WebSocket
    webSocket.onEvent(webSocketEvent);  // Registrar handler de eventos
    
    Serial.println("DNS, Web Server e WebSocket iniciados.");
    Serial.println("WebSocket disponível em: ws://192.168.0.1:81");
}

void loop() {
    dnsServer.processNextRequest();
    webServer.handleClient();
    webSocket.loop();  // Processar eventos WebSocket (CRÍTICO - deve ser chamado frequentemente)
    
    yield();
    
    // Enviar ping periódico para manter conexões vivas
    if (millis() - lastPing > pingInterval) {
        webSocket.broadcastPing();  // Envia ping para todos os clientes conectados
        lastPing = millis();
    }
    
    if (millis() - lastCheck > checkInterval) {
        int connected = WiFi.softAPgetStationNum();
        int wsConnected = webSocket.connectedClients();  // Número de clientes WebSocket conectados
        Serial.printf("WiFi: %d / 10 | WebSocket: %d (Canal: 44, Potência: 19.5dBm)\n", 
                      connected, wsConnected);
        lastCheck = millis();
    }
}

