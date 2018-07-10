using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialPortMonitor
{
    public class BMP
    {
        byte[] bytes;

        //public int[][] readBMP(String paramString)
        //{
        //    byte[] arrayOfByte = new byte[54];
        //    int[][] arrayOfInt = (int[][])null;
        //    try
        //    {
        //        FileInputStream localFileInputStream = new FileInputStream(new File(paramString));
        //        localFileInputStream.read(arrayOfByte, 0, arrayOfByte.length);

        //        int i = ((arrayOfByte[21] & 0xFF) << 24) + ((arrayOfByte[20] & 0xFF) << 16) + ((arrayOfByte[19] & 0xFF) << 8) + (arrayOfByte[18] & 0xFF);
        //        int j = ((arrayOfByte[25] & 0xFF) << 24) + ((arrayOfByte[24] & 0xFF) << 16) + ((arrayOfByte[23] & 0xFF) << 8) + (arrayOfByte[22] & 0xFF);

        //        arrayOfInt = new int[j][i];
        //        for (int k = 0; k < j; k++)
        //        {
        //            for (int m = 0; m < i; m++)
        //            {
        //                localFileInputStream.read(arrayOfByte, 0, 3);
        //                arrayOfInt[k][m] = (((arrayOfByte[2] & 0xFF) << 16) + ((arrayOfByte[1] & 0xFF) << 8) + (arrayOfByte[0] & 0xFF));
        //            }
        //        }
        //    }
        //    catch (IOException localIOException)
        //    {
        //        throw new IllegalStateException(localIOException);
        //    }
        //    return arrayOfInt;
        //}

        public void saveBMP(String paramString, int[,] paramArrayOfInt)
        {
            FileStream localFileOutputStream = null;
            try
            {
                if (File.Exists(paramString))
                {
                    File.Delete(paramString);
                }

                localFileOutputStream = new FileStream(paramString, FileMode.CreateNew);

                this.bytes = new byte[54 + 3 * paramArrayOfInt.GetLength(0) * paramArrayOfInt.GetLength(1)];

                saveFileHeader();
                saveInfoHeader(paramArrayOfInt.GetLength(0), paramArrayOfInt.GetLength(1));
                saveBitmapData(paramArrayOfInt);

                localFileOutputStream.Write(this.bytes, 0, this.bytes.Length);

                localFileOutputStream.Close();
            }
            catch (IOException localIOException)
            {
                throw;
            }
        }

        private void saveFileHeader()
        {
            this.bytes[0] = 66;
            this.bytes[1] = 77;

            this.bytes[5] = ((byte)this.bytes.Length);
            this.bytes[4] = ((byte)(this.bytes.Length >> 8));
            this.bytes[3] = ((byte)(this.bytes.Length >> 16));
            this.bytes[2] = ((byte)(this.bytes.Length >> 24));

            this.bytes[10] = 54;
        }

        private void saveInfoHeader(int paramInt1, int paramInt2)
        {
            this.bytes[14] = 40;

            this.bytes[18] = ((byte)paramInt2);
            this.bytes[19] = ((byte)(paramInt2 >> 8));
            this.bytes[20] = ((byte)(paramInt2 >> 16));
            this.bytes[21] = ((byte)(paramInt2 >> 24));

            this.bytes[22] = ((byte)paramInt1);
            this.bytes[23] = ((byte)(paramInt1 >> 8));
            this.bytes[24] = ((byte)(paramInt1 >> 16));
            this.bytes[25] = ((byte)(paramInt1 >> 24));

            this.bytes[26] = 1;

            this.bytes[28] = 24;
        }

        private void saveBitmapData(int[,] paramArrayOfInt)
        {
            for (int i = 0; i < paramArrayOfInt.GetLength(0); i++)
            {
                writeLine(i, paramArrayOfInt);
            }
        }

        private void writeLine(int paramInt, int[,] paramArrayOfInt)
        {
            int i = paramArrayOfInt.GetLength(1);
            for (int j = 0; j < i; j++)
            {
                int k = paramArrayOfInt[paramInt,j];
                int m = 54 + 3 * (j + i * paramInt);

                this.bytes[(m + 2)] = ((byte)(k >> 16));
                this.bytes[(m + 1)] = ((byte)(k >> 8));
                this.bytes[m] = ((byte)k);
            }
        }
    }
}

