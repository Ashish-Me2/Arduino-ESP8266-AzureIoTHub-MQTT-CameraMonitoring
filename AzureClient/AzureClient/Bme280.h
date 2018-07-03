
#include "Sensor.h"

class Bme280 : public Sensor
{
  public:
    void measure();

  protected:
    void initialise();  
    Adafruit_BME280  bme280; // I2C
};

#endif
