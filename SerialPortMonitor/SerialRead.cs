using System;
using System.IO;
using System.IO.Ports;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SerialPortMonitor
{
    public class SerialRead
    {
        private static char[] COMMAND = { 'R', 'D', 'Y'};
        private static int WIDTH = 640;
        private static int HEIGHT = 480;

        Stream inputStream;
        SerialPort serialPort;

        //public static void main(String[] paramArrayOfString)
        //{
        //    Enumeration localEnumeration = CommPortIdentifier.getPortIdentifiers();
        //    while (localEnumeration.hasMoreElements())
        //    {
        //        portId = (CommPortIdentifier)localEnumeration.nextElement();
        //        if (portId.getPortType() == 1)
        //        {
        //            Debug.Print("Port name: " + portId.getName());
        //            if (portId.getName().equals("COM3"))
        //            {
        //                SerialRead localSerialRead = new SerialRead();
        //            }
        //        }
        //    }
        //}

        public bool IsReady { get; set; }
        public void SaveImage(Stream portStream)
        {
            IsReady = false;
            inputStream = portStream;

            int[,] arrayOfInt1 = new int[HEIGHT, WIDTH];
            int[,] arrayOfInt2 = new int[WIDTH, HEIGHT];
           

            try
            {
                int i = 0;
                for (; ; )
                {
                    Debug.Print("Looking for image");
                    while (!isImageStart(this.inputStream, 0)) { }
                    Debug.Print("Found image: " + i);
                    int k;
                    for (int j = 0; j < HEIGHT; j++)
                    {
                        for (k = 0; k < WIDTH; k++)
                        {
                            int m = read(this.inputStream);
                            arrayOfInt1[j,k] = ((m & 0xFF) << 16 | (m & 0xFF) << 8 | m & 0xFF);
                        }
                    }
                    for (int j = 0; j < HEIGHT; j++)
                    {
                        for (k = 0; k < WIDTH; k++)
                        {
                            arrayOfInt2[k,j] = arrayOfInt1[j, k];
                        }
                    }
                    BMP localBMP = new BMP();
                    localBMP.saveBMP(i++ + "ABB.bmp", arrayOfInt2);

                    Debug.Print("Saved image: " + i);
                    IsReady = true;
                }
            }
            catch (Exception localException)
            {
                Debug.Print(localException.StackTrace);
            }
        }

        int totalRead = 0;
        private int read(Stream paramInputStream)
        {
            int i = paramInputStream.ReadByte();
            if (i == -1) {
                Debug.WriteLine("Total chrs read from input stream = " + totalRead);
                throw new ApplicationException("Exit");
            }
            totalRead++;
            return i;
        }

        private bool isImageStart(Stream paramInputStream, int paramInt)
        {
            if (paramInt < COMMAND.Length)
            {
                if (COMMAND[paramInt] == Convert.ToChar(read(paramInputStream)))
                {
                    return isImageStart(paramInputStream, ++paramInt);
                }
                return false;
            }
            return true;
        }
    }
}
