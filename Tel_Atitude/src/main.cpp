#include <Arduino.h>
#include <M5StickCPlus2.h>
#include <math.h>

// ==========================================
// Constantes e Parâmetros do Filtro Madgwick
// ==========================================
#define MADGWICK_BETA 0.1f

// GPIOs dos botões (leitura direta, active LOW)
#define BTN_A_PIN 37
#define BTN_B_PIN 39

// Quaternion de atitude (W, X, Y, Z) inicializado na identidade
float q0 = 1.0f, q1 = 0.0f, q2 = 0.0f, q3 = 0.0f;

// ==========================================
// Variáveis de Controle de Tempo e Taxa
// ==========================================
uint32_t lastUpdate = 0;
uint32_t frameCount = 0;
uint32_t lastDisplayUpdate = 0;
float currentHz = 0.0f;

// ==========================================
// Estado da UI de Confirmação de Zeragem
// ==========================================
bool awaitingConfirmation = false;

// Estado anterior dos botões para detecção de borda (evita repeat)
bool lastBtnAState = HIGH;
bool lastBtnBState = HIGH;

// ==========================================
// Implementação do Filtro Madgwick (IMU - 6DOF)
// ==========================================
void MadgwickAHRSupdateIMU(float gx, float gy, float gz, float ax, float ay, float az, float dt) {
    float recipNorm;
    float s0, s1, s2, s3;
    float qDot1, qDot2, qDot3, qDot4;
    float _2q0, _2q1, _2q2, _2q3, _4q0, _4q1, _4q2, _8q1, _8q2, q0q0, q1q1, q2q2, q3q3;

    gx *= 0.0174533f;
    gy *= 0.0174533f;
    gz *= 0.0174533f;

    qDot1 = 0.5f * (-q1 * gx - q2 * gy - q3 * gz);
    qDot2 = 0.5f * (q0 * gx + q2 * gz - q3 * gy);
    qDot3 = 0.5f * (q0 * gy - q1 * gz + q3 * gx);
    qDot4 = 0.5f * (q0 * gz + q1 * gy - q2 * gx);

    if (!((ax == 0.0f) && (ay == 0.0f) && (az == 0.0f))) {
        recipNorm = 1.0f / sqrt(ax * ax + ay * ay + az * az);
        ax *= recipNorm;
        ay *= recipNorm;
        az *= recipNorm;

        _2q0 = 2.0f * q0; _2q1 = 2.0f * q1;
        _2q2 = 2.0f * q2; _2q3 = 2.0f * q3;
        _4q0 = 4.0f * q0; _4q1 = 4.0f * q1;
        _4q2 = 4.0f * q2;
        _8q1 = 8.0f * q1; _8q2 = 8.0f * q2;
        q0q0 = q0 * q0; q1q1 = q1 * q1;
        q2q2 = q2 * q2; q3q3 = q3 * q3;

        s0 = _4q0 * q2q2 + _2q2 * ax + _4q0 * q1q1 - _2q1 * ay;
        s1 = _4q1 * q3q3 - _2q3 * ax + 4.0f * q0q0 * q1 - _2q0 * ay - _4q1 + _8q1 * q1q1 + _8q1 * q2q2 + _4q1 * az;
        s2 = 4.0f * q0q0 * q2 + _2q0 * ax + _4q2 * q3q3 - _2q3 * ay - _4q2 + _8q2 * q1q1 + _8q2 * q2q2 + _4q2 * az;
        s3 = 4.0f * q1q1 * q3 - _2q1 * ax + 4.0f * q2q2 * q3 - _2q2 * ay;

        recipNorm = 1.0f / sqrt(s0 * s0 + s1 * s1 + s2 * s2 + s3 * s3);
        s0 *= recipNorm; s1 *= recipNorm;
        s2 *= recipNorm; s3 *= recipNorm;

        qDot1 -= MADGWICK_BETA * s0;
        qDot2 -= MADGWICK_BETA * s1;
        qDot3 -= MADGWICK_BETA * s2;
        qDot4 -= MADGWICK_BETA * s3;
    }

    q0 += qDot1 * dt; q1 += qDot2 * dt;
    q2 += qDot3 * dt; q3 += qDot4 * dt;

    recipNorm = 1.0f / sqrt(q0 * q0 + q1 * q1 + q2 * q2 + q3 * q3);
    q0 *= recipNorm; q1 *= recipNorm;
    q2 *= recipNorm; q3 *= recipNorm;
}

// ==========================================
// Leitura de borda de descida dos botões via GPIO
// Retorna true apenas no ciclo em que o botão foi pressionado
// ==========================================
bool btnAPressed() {
    bool current = digitalRead(BTN_A_PIN);
    bool pressed = (current == LOW && lastBtnAState == HIGH);
    lastBtnAState = current;
    return pressed;
}

bool btnBPressed() {
    bool current = digitalRead(BTN_B_PIN);
    bool pressed = (current == LOW && lastBtnBState == HIGH);
    lastBtnBState = current;
    return pressed;
}

// ==========================================
// Exibe tela de confirmação de zeragem
// ==========================================
void showConfirmationScreen() {
    StickCP2.Display.fillScreen(BLACK);
    StickCP2.Display.setTextSize(2);
    StickCP2.Display.setCursor(0, 5);
    StickCP2.Display.setTextColor(YELLOW);
    StickCP2.Display.println("Zerar ref?");
    StickCP2.Display.println("");
    StickCP2.Display.setTextColor(GREEN);
    StickCP2.Display.println("Sim = A");
    StickCP2.Display.setTextColor(RED);
    StickCP2.Display.println("Nao = B");
}

// ==========================================
// Zera o quaternion para a identidade
// ==========================================
void resetReference() {
    q0 = 1.0f; q1 = 0.0f;
    q2 = 0.0f; q3 = 0.0f;

    StickCP2.Display.fillScreen(BLACK);
    StickCP2.Display.setTextSize(2);
    StickCP2.Display.setCursor(0, 20);
    StickCP2.Display.setTextColor(GREEN);
    StickCP2.Display.println("Referencia");
    StickCP2.Display.println("zerada!");
    delay(1200);
}

// ==========================================
// Setup Principal
// ==========================================
void setup() {
    auto cfg = M5.config();
    StickCP2.begin(cfg);

    // Configura GPIOs dos botões com pull-up interno
    pinMode(BTN_A_PIN, INPUT_PULLUP);
    pinMode(BTN_B_PIN, INPUT_PULLUP);

    Serial.begin(115200);

    StickCP2.Display.setRotation(1);
    StickCP2.Display.setTextSize(2);
    StickCP2.Display.fillScreen(BLACK);
    StickCP2.Display.setCursor(10, 10);
    StickCP2.Display.setTextColor(GREEN);
    StickCP2.Display.print("TX OK");

    lastUpdate = micros();
    lastDisplayUpdate = millis();
}

// ==========================================
// Loop Principal
// ==========================================
void loop() {
    StickCP2.update();

    // ==========================================
    // Lógica dos Botões (leitura direta via GPIO)
    // ==========================================
    if (!awaitingConfirmation) {
        if (btnAPressed()) {
            awaitingConfirmation = true;
            showConfirmationScreen();
        }
    } else {
        if (btnAPressed()) {
            awaitingConfirmation = false;
            resetReference();
        } else if (btnBPressed()) {
            awaitingConfirmation = false;
            StickCP2.Display.fillScreen(BLACK);
        }
        // Suspende IMU e serial durante a confirmação
        return;
    }

    // ==========================================
    // Cálculo do dt
    // ==========================================
    uint32_t now = micros();
    float dt = (now - lastUpdate) / 1000000.0f;
    dt = constrain(dt, 0.0001f, 0.05f);
    lastUpdate = now;

    // ==========================================
    // Leitura da IMU
    // ==========================================
    float ax, ay, az, gx, gy, gz;
    StickCP2.Imu.getAccelData(&ax, &ay, &az);
    StickCP2.Imu.getGyroData(&gx, &gy, &gz);

    // ==========================================
    // Fusão de Sensores (Madgwick)
    // ==========================================
    MadgwickAHRSupdateIMU(gx, gy, gz, ax, ay, az, dt);

    // ==========================================
    // Serialização: W,X,Y,Z,AX,AY,AZ\n
    // ==========================================
    Serial.print(q0, 4);
    Serial.print(",");
    Serial.print(q1, 4);
    Serial.print(",");
    Serial.print(q2, 4);
    Serial.print(",");
    Serial.print(q3, 4);
    Serial.print(",");
    Serial.print(ax, 3);
    Serial.print(",");
    Serial.print(ay, 3);
    Serial.print(",");
    Serial.println(az, 3);

    // ==========================================
    // Atualização do Display (1s de intervalo)
    // ==========================================
    frameCount++;
    if (millis() - lastDisplayUpdate >= 1000) {
        currentHz = (float)frameCount;
        frameCount = 0;
        lastDisplayUpdate = millis();

        StickCP2.Display.fillScreen(BLACK);
        StickCP2.Display.setCursor(0, 0);
        StickCP2.Display.setTextColor(GREEN);
        StickCP2.Display.printf("TX OK | Hz: %.0f\n", currentHz);
        StickCP2.Display.printf("W: %.2f\n", q0);
        StickCP2.Display.printf("X: %.2f\n", q1);
        StickCP2.Display.printf("Y: %.2f\n", q2);
        StickCP2.Display.printf("Z: %.2f\n", q3);
    }
}