#include <ESP8266WiFi.h>
#include <PubSubClient.h>
#include <WiFiClientSecure.h>
#include <ArduinoJson.h>
#include <base64.h>

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
			StaticJsonBuffer<3000> jsonBuffer;
			JsonObject& root = jsonBuffer.createObject();

			// populate keys in json
			root["seg_id"] = "1" ;
			root["seg_data"] = 128;

			// convert json to buffer for publishing
			char buffer[3000];
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

char* GetDummyBase64EncodeString() {
	return "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";
}
