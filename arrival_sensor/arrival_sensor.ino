#define INPUT_PIN 2
uint8_t found;

void setup() {
  // put your setup code here, to run once:
  Serial.begin(9600);
  pinMode(INPUT_PIN, INPUT);
  found = 1;
}

void loop() {
  // put your main code here, to run repeatedly:

  // 1 = oggetto assente
  // 0 = oggetto presente
  found = digitalRead(INPUT_PIN);

  if (!found) {
    Serial.print(found);
    delay(500);
  }
}
