#ifndef Bmp180_h
#define Bmp180_h

#include "Sensor.h"
#include "DigitalPin.h"


class Bmp180 : public Sensor
{
  public:
    Bmp180(){};
    Bmp180(DigitalPin *powerPin):Sensor(powerPin){};
    void measure();

  protected:

  private:
    void initialise();  
};

#endif
