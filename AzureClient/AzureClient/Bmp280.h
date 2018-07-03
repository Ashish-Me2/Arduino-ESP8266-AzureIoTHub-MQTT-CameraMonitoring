#ifndef Bmp280_h
#define Bmp280_h

#include "Sensor.h"

class Bmp280 : public Sensor
{
  public:
    Bmp280(){};
    Bmp280(DigitalPin *powerPin):Sensor(powerPin){};
    void measure();

  private:
    void initialise();  
};

#endif
