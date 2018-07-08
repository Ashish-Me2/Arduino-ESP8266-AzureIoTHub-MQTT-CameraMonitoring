/*
 Name:		ProximitySensor.ino
 Created:	7/7/2018 8:26:48 AM
 Author:	Ashish
*/

int PIRInput = 5;

// the setup function runs once when you press reset or power the board
void setup() {
	pinMode(LED_BUILTIN, OUTPUT);
	pinMode(PIRInput, INPUT);
}

// the loop function runs over and over again until power down or reset
void loop() {
	int state = digitalRead(PIRInput);
	digitalWrite(LED_BUILTIN, state);
	delay(100);
} 
