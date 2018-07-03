#include <ESP8266WiFi.h>
#include "Device.h"
#include "IoTHub.h"
#include "DigitalPin.h"
#include "Sensor.h"
//#include "EventHub.h"  // uncomment if you plan to publish to Azure Event Bus

const char* connectionString = "HostName=ashishhub.azure-devices.net;DeviceId=homecamera;SharedAccessKey=+SHCAiOGdfat9SRqkxM6idbpbEc8OL9faHVuw/sDtTE=";
const char* wifi_ssid = "ZOMBIE";
const char* wifi_pwd = "CHANTI1-BANTI2";
const char* deviceLocation = "Hyderabad";

/* 
 http://hassansin.github.io/certificate-pinning-in-nodejs
 for information on generating fingerprint
 From Ubuntu subsystem on Windows 10
 echo -n | openssl s_client -connect IoTCampAU.azure-devices.net:443 | sed -ne '/-BEGIN CERTIFICATE-/,/-END CERTIFICATE-/p' > cert.pem
 openssl x509 -noout -in cert.pem -fingerprint
*/

//This is the certificate thumbprint for the AzureIotHub currently configured for Ashish Mathur
const char* certificateFingerprint = "95:B4:61:DF:90:D9:D7:1D:15:22:D8:DB:2E:F1:7D:BC:F4:BB:41:D2";

Device device(wifi_ssid, wifi_pwd);
IoT hub;
Sensor sensor;  // Fake sample environmental data
IPAddress timeServer(62, 237, 86, 238); // Update these with values suitable for your network.

void initDeviceConfig() { // Example device configuration
  device.deepSleepSeconds = 0;         // if greater than zero with call ESP8266 deep sleep (default is 0 disabled). GPIO16 needs to be tied to RST to wake from deepSleep. Causes a reset, execution restarts from beginning of sketch
  device.publishRateInSeconds = 8;     // limits publishing rate to specified seconds (default is 90 seconds).  Connectivity problems may result if number too small eg 2
  hub.sasExpiryPeriodInSeconds = 100 * 60; // Renew Sas Token every 15 minutes
  hub.certificateFingerprint = certificateFingerprint;
  hub.setConnectionString(connectionString);
  sensor.geo = deviceLocation;
}

void setup() {
  Serial.begin(115200);
  delay(100);
  initDeviceConfig();
  device.connectWifi();
  getCurrentTime();
}

void loop() {
  sensor.measure();
  hub.send(sensor.toJSON()); // response 204 means successful send of data to Azure IoT Hub
    delay(device.publishRateInSeconds * 100);  // limit publishing rate
  
  //if (device.deepSleepSeconds > 0) {
  //  WiFi.mode(WIFI_OFF);
  //  ESP.deepSleep(1000000 * device.deepSleepSeconds, WAKE_RF_DEFAULT); // GPIO16 needs to be tied to RST to wake from deepSleep. Execute restarts from beginning of sketch
  //}
  //else {
  //  delay(device.publishRateInSeconds * 1000);  // limit publishing rate
  //}
}

void getCurrentTime(){
  int ntpRetryCount = 0;
  while (timeStatus() == timeNotSet && ++ntpRetryCount < 20) { // get NTP time
    setSyncProvider(getNtpTime);
    setSyncInterval(60 * 60);  
  }
}


//------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
/*
SUMMARY:
This project was implemented and tested on the NodeMCU V2 (also known as V1.0), WeMos D1 Mini and Sparkfun ESP8266 Dev development boards on the ESP8266 platform
flashed with Arduino core for ESP8266 WiFi chip V2.0 firmware.
This solution streams sample sensor data securely over HTTPS / REST directly to Azure IoT Hub or Azure Event Hub.

PLATFORM: ESP8266

DEVELOPMENT BOARDS:
NodeMCU: http://nodemcu.com/index_en.html#fr_54747661d775ef1a3600009e (This project will reflash the NodeMCU in to the Arduino platform. IMHO LUA not stable)
WeMod D1 Mini: http://www.wemos.cc/wiki/doku.php?id=en:d1_mini
Sparkfun 8266 Thing Dev Board: https://learn.sparkfun.com/tutorials/esp8266-thing-development-board-hookup-guide/introduction
Other ESP8266 Development Boards - Adafruit, and others

Sparkfun ESP8266 Dev Board Notes:

With my limited experience with the Sparkfun ESP8266 Dev board I needed to use the Generic ESP8266 Arduino IDE Board Manager.
See notes at https://learn.sparkfun.com/tutorials/esp8266-thing-development-board-hookup-guide/setting-up-arduino
Change to Generic ESP8266 Module, and in the sub menus, make sure the following sub-menu's are also set:
- Flash Mode: DIO
- Flash Frequency: 80MHz
- Upload Using: Serial
- CPU Frequency: 80MHz
- Flash Size: 512K (no SPIFFS)
- Reset Method: nodemcu

FIRMWARE: Arduino core for ESP8266 WiFi chip V2.0 | https://github.com/esp8266/Arduino

ARDUINO IDE: 4.6.5 (as at Jan 2016)
- ESP8266 V2 requires Arduino IDE 4.6.5 (high versions will result is scope compiler errors
- http://esp8266.github.io/Arduino/versions/2.0.0/doc/installing.html


REQUIRED LIBRARIES: Install Arduino Libraries from Sketch -> Include Library -> Manage Libraries
- ArduinoJson
- Time
- Adafruit BMP085 (Unified version)
- Adafruit DHT (DON’T install the unified version)
- Adafruit Unified Sensor
- Adafruit BMP280
- Adafruit BME280


CLOUD CONFIGURATION:

The Azure IoT Hub device id, key and connection string can be obtained by right mouse clicking on a device in the Device Explorer.

The function initCloudConfig() called from setup in the AzureClient.ino has two signatures.

// Inline cloud and network configuration information
initCloudConfig("IoT hub device connection string", "Case Sensitive WiFi SSID", "WiFi password", "Geo location of the device").

Example:

initCloudConfig("HostName=YourIoTHub.azure-devices.net;DeviceId=YourDeviceId;SharedAccessKey=YourDeviceKey", "SSID", "Password", "Sydney");

or

// To read cloud and network configuration from EEPROM
initCloudConfig();

Optional EEPROM Configuration

To configure the EEPROM open the SetEEPROMConfiguration.ino found in the SetEEPROMConfiguration folder and update the following variables:-

Wi-Fi SSID and password pairs, put in priority order.
Wifi - Is the number of WiFi ssid and password pairs
Azure IoT Hub or Event Bus Host name eg "MakerDen.azure-devices.net", Device ID, and Key. For IoT Hub get this information from the Device Explorer, for Event Hub, get from Azure Management Portal.
Geo location of the device
Deploy this app to the NodeMCU to write configuration settings to EEPROM
Upload this sketch to burn these settings to the device EEPROM. After this you deploy the AzureClient sketch which will read this configuration information from the EEPROM. Be sure to call function initCloudConfig() with no parameters.


DEVICE SAMPLE CONFIGURATION:

// device configuration in this Arduino Sketch
void initDeviceConfig(){
device.deepSleepSeconds = 0;         // if greater than zero with call ESP8266 deep sleep (default is 0 disabled). GPIO16 needs to be tied to RST to wake from deepSleep. Causes a reset, execution restarts from beginning of sketch
cloud.cloudMode = IoTHub;            // CloudMode enumeration: IoTHub and EventHub (default is IoTHub)
cloud.publishRateInSeconds = 90;     // limits publishing rate to specified seconds (default is 90 seconds)
hub.sasExpiryPeriodInSeconds = 15 * 60; // Renew Sas Token every 15 minutes
}


DRIVERS:
- NodeMCU - On Windows, Mac and Linux you will need to install the latest CP210x USB to UART Bridge VCP Drivers. (https://www.silabs.com/products/mcu/Pages/USBtoUARTBridgeVCPDrivers.aspx)
- WeMos - On Windows and Mac install the latest Ch340G drivers. No drivers required for Linux. (http://www.wemos.cc/wiki/doku.php?id=en:ch340g)
- Sparkfun ESP8266 Thing Dev - https://learn.sparkfun.com/tutorials/esp8266-thing-development-board-hookup-guide/hardware-setup


ESP8266 ARDUINO IDE SUPPORT:

1. Install the latest Arduino 1.8.1 as at Jan 2017 from the Arduino website.
2. Start Arduino and open Preferences window.
3. Enter http://arduino.esp8266.com/stable/package_esp8266com_index.json into Additional Board Manager URLs field. You can add multiple URLs, separating them with commas.
4. RESTART Arduino IDE
5. Open Boards Manager from Tools > Board -> Boards Manager. Search for ESP8266 and install.
6. Select NodeMUC or WeMos D1 Mini Board: Tools -> Board  -> NodeMCU 1.0 (ESP-12E module) or WeMos D1 Mini
7. Set Port and Upload Speed: Tools. Note, you may need to try different port speeds to successfully flash the device.
- Faster is better as each time you upload the code to your device you are re-flashing the complete ROM not just your code, but faster can be less reliable.


OPTIONAL COMPONENTS:

Circuit Layout at https://github.com/gloveboxes/Arduino-NodeMCU-ESP8266-Secure-Azure-IoT-Hub-Client

- BMP180 Barometric Pressure Sensor
- Adafruit Mini 8x8 LED Matrix w/I2C Backpack
- Light Dependent Resistor
- 10k resistor

- WeMos Di Mini DHT(11) Shield: http://www.wemos.cc/wiki/doku.php?id=en:dht

*/

