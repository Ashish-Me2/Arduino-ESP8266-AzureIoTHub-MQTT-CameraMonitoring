#include <ESP8266WiFi.h>
#include <PubSubClient.h>
#include <WiFiClientSecure.h>
#include <ArduinoJson.h>

// wifi credentials
const char* wifi_ssid = "ZOMBIE";
const char* wifi_password = "CHANTI1-BANTI2";

// example: <myiothub>.azure-devices.net
const char* iothub_url = "ashishhub.azure-devices.net";

// this is the id of the device created in Iot Hub
// example: myCoolDevice
const char* iothub_deviceid = "homecamera";

// <myiothub>.azure-devices.net/<myCoolDevice>
const char* iothub_user = "ashishhub.azure-devices.net/homecamera";

// SAS token should look like "SharedAccessSignature sr=<myiothub>.azure-devices.net%2Fdevices%2F<myCoolDevice>&sig=123&se=456"
const char* iothub_sas_token = "SharedAccessSignature sr=ashishhub.azure-devices.net%2Fdevices%2Fhomecamera&sig=uxnPrkRQiTEHu%2FGCYzCAzd736BCM88te6sa3cGcUEmU%3D&se=1530624452";

// default topic feed for subscribing is "devices/<myCoolDevice>/messages/devicebound/#"
const char* iothub_subscribe_endpoint = "devices/homecamera/messages/devicebound/#";

// default topic feed for publishing is "devices/<myCoolDevice>/messages/events/"
const char* iothub_publish_endpoint = "devices/homecamera/messages/events/";

/*
http://hassansin.github.io/certificate-pinning-in-nodejs
for information on generating fingerprint
From Ubuntu subsystem on Windows 10
echo -n | openssl s_client -connect IoTCampAU.azure-devices.net:443 | sed -ne '/-BEGIN CERTIFICATE-/,/-END CERTIFICATE-/p' > cert.pem
openssl x509 -noout -in cert.pem -fingerprint
*/

//This is the certificate thumbprint for the AzureIotHub currently configured for Ashish Mathur
const char* thumbprint = "95:B4:61:DF:90:D9:D7:1D:15:22:D8:DB:2E:F1:7D:BC:F4:BB:41:D2";

WiFiClientSecure espClient;
PubSubClient client(espClient);

long lastMsg = 0;

// function to connect to the wifi
void setup_wifi() {
	delay(10);
	
	Serial.println();
	Serial.print("Connecting to wifi");

	WiFi.begin(wifi_ssid, wifi_password);

	while (WiFi.status() != WL_CONNECTED) {
		delay(500);
		Serial.print(".");
	}

	// debug wifi via serial
	Serial.println("");
	Serial.println("WiFi connected");
	Serial.println("IP address: ");
	Serial.println(WiFi.localIP());
}

// function to connect to MQTT server
void connect_mqtt() {
	// Loop until we're reconnected
	while (!client.connected()) {
		Serial.print("Attempting MQTT connection...");
		
		// Attempt to connect
		if (client.connect(iothub_deviceid, iothub_user, iothub_sas_token)) {
			verifyServerFingerprint();
			Serial.println("connected");
			// ... and subscribe
			if (client.subscribe(iothub_subscribe_endpoint)) {
				Serial.println("Subscribed to topic for callbacks.");
			}
		}
		else {
			Serial.print("failed, rc=");
			Serial.print(client.state());
			Serial.println("try again in 5 seconds");
			// Wait 5 seconds before retrying
			delay(5000);
		}
	}
}

void verifyServerFingerprint() {
	if (espClient.verify(thumbprint, iothub_url)) {
		Serial.print("Certificate fingerprint verified against ");
		Serial.print(iothub_url);
		Serial.println(" successfully");
		Serial.println("--------------------------------------");
	}
	else {
		Serial.println("*******************************************");
		Serial.println("Certificate fingerprint verification failed");
		Serial.println("*******************************************");
		ESP.restart();
	}
}


// callback function for when a message is dequeued from the MQTT server
void callback(char* topic, byte* payload, unsigned int length) {
	Serial.print("Message arrived [");
	Serial.print(topic);
	Serial.print("] ");
	for (int i = 0; i<length; i++) {
		Serial.print((char)payload[i]);
	}
	Serial.println();
}

void Stuff(char* bbuf, int size)
{
	char buf[4];
	int remain = size;
	while (remain)
	{
		int toCpy = remain > sizeof(buf) ? sizeof(buf) : remain;
		memcpy(buf, bbuf, toCpy);
		bbuf += toCpy;
		remain -= toCpy;
		//DoStuff(&buf, toCpy); //Send the buffer or whatever
	}
}

void setup() {
	// begin serial for debugging purposes
	Serial.begin(115200);

	// connect to wifi
	setup_wifi();

	// set up connection and callback for MQTT server
	client.setServer(iothub_url, 8883);
	
	client.setCallback(callback);
	// connect to MQTT
	connect_mqtt();
}


void loop() {
	client.loop();
	long now = millis();

	// publish data and debug mqtt connection every 10 seconds
	if (now - lastMsg > 1000) {
		lastMsg = now;
		if (client.connected()) {
			Serial.print("MQTT-OK-");

			// set up json object
			StaticJsonBuffer<200> jsonBuffer;
			JsonObject& root = jsonBuffer.createObject();

			// populate keys in json
			root["sensor"] = "moisture";
			// substitute the int value below for a real sensor reading (ie. an analogRead() result)
			root["data"] = 128;

			// convert json to buffer for publishing
			char buffer[256];
			root.printTo(buffer, sizeof(buffer));

			// publish
			if (client.publish(iothub_publish_endpoint, buffer)) {
				Serial.println("Data Sent");
			}
		}
		else {
			Serial.print("ALERT - MQTT disconnected. Attempting reconnect...");
			connect_mqtt();
			delay(5000);
		}
	}
}
