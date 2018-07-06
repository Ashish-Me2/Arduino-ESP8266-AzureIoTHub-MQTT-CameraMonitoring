using System;
using System.Windows.Media.Imaging;
using System.Security.Cryptography;
using System.IO.Ports;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SerialPortMonitor
{
    public partial class Form1 : Form
    {
        string selectedPortName = null;
        Timer timer = new Timer();
        SerialPort monitorPort = null;
        Button startButton = null;
        Button stopButton = null;
        string imgPath = null;
        StringBuilder Base64StringSegments = new StringBuilder();
        bool isFirstFrame = true;
        Dictionary<int,int> patternsTable = new Dictionary<int, int>();
        int patternCounter = 0;
        List<byte> sBytes = new List<byte>();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            imgPath = (Environment.GetCommandLineArgs().Length>1)? Environment.GetCommandLineArgs()[1]: @"E:\PICTURES\2t.jpg";
            
            timer.Tick += Timer_Tick;
            timer.Interval = 10;
            string mcName = String.Format("{0}", Environment.MachineName);
            label2.Text += mcName;
            PopulateAllSerialPorts();
            comboBox1.SelectedIndex = (mcName.ToUpper().Equals("CHANTI-BANTI")) ? (comboBox1.Items.IndexOf("COM6")) : 0;
            startButton = this.btnStart;
            stopButton = this.btnStop;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            
        }

        private void PopulateAllSerialPorts()
        {
             foreach (String portName in System.IO.Ports.SerialPort.GetPortNames())
             {
                comboBox1.Items.Add(portName);
             }
        }

        private void SetupPortProps(SerialPort port)
        {
            port.BaudRate = 2000000;
            port.DataBits = 8;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedPortName = comboBox1.SelectedItem.ToString();
            if (monitorPort != null)
            {
                monitorPort.DataReceived -= MonitorPort_DataReceived;
                monitorPort.Close();
                monitorPort.Dispose();
            }
            monitorPort = new SerialPort(selectedPortName);
            SetupPortProps(monitorPort);
        }

        private void MonitorPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (InvokeRequired)
            {
                //this.Invoke(new MethodInvoker(delegate {
                //    label3.Text = String.Format("Data stream received at - {0}", DateTime.Now.ToLongTimeString());
                //}));
                
                SerialPort senderPort = (SerialPort)sender;
                int bytes = senderPort.BytesToRead;
                byte[] buffer = new byte[bytes];
                senderPort.Read(buffer, 0, bytes);
                HandleSerialData(buffer);
                return;
            }
        }


        int search(byte[] haystack, byte[] needle)
        {
            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                if (match(haystack, needle, i))
                {
                    return i;
                }
            }
            return -1;
        }

        bool match(byte[] haystack, byte[] needle, int start)
        {
            if (needle.Length + start > haystack.Length)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < needle.Length; i++)
                {
                    if (needle[i] != haystack[i + start])
                    {
                        return false;
                    }
                }
                return true;
            }
        }


        private void HandleSerialData(byte[] buffer)
        {
            byte[] needle = new byte[] { 65, 77 };
            bool AFound = false;
            bool PatternFound = false;
            int newFrameMarker = 0;
            int counter = 0;
            foreach (byte b in buffer)
            {
                if (b == 65)
                {
                    AFound = true;
                }
                if ((b == 77) & (AFound)){
                    PatternFound = true;
                    patternCounter++;
                    newFrameMarker = counter+3;
                    break;
                }
                counter++;
            }

            if ((PatternFound)&(patternCounter<1))
            {
                sBytes.Clear();
                sBytes.AddRange(buffer.Skip(2));
            }
            else if ((!PatternFound) & (patternCounter >0))
            {
                sBytes.AddRange(buffer);
            }
            else if ((PatternFound) & (patternCounter > 1))
            {
                byte[] oldFrame = buffer.Take(newFrameMarker - 3).ToArray();
                sBytes.AddRange(oldFrame);
                DumpFrame(sBytes.ToArray());
                sBytes.Clear();
                patternCounter = 0;
            }

             ////----------------------------------------------------------------------------------------------
            //Debug.Print("Buffer Size = " + buffer.Length);
            //if (PatternFound)
            //{
            //    Debug.Print("PATTERN FOUND");
            //    byte[] oldFrame = buffer.Take(newFrameMarker - 3).ToArray();
            //    byte[] newFrame = buffer.Skip(newFrameMarker).ToArray();

            //    //YES: This is the 1st frame being processed. Now setting the flag to false for subsequent frames
            //    //Start with new byte array
            //    if (isFirstFrame)
            //    {
            //        Debug.Print("1st FRAME");
            //        sBytes.Clear();
            //        //Add whatever is available after the AM marker
            //        sBytes.AddRange(newFrame);
            //        Debug.Print("Added NEW frame");
            //    }
            //    else
            //    {
            //        Debug.Print("NOT 1st FRAME");
            //        sBytes.AddRange(oldFrame);
            //        Debug.Print("Added OLD frame");
            //        DumpFrame(sBytes.ToArray());
            //        sBytes.Clear();
            //        sBytes.AddRange(newFrame);
            //        Debug.Print("DUMP IMAGE");
            //        Debug.Print("ALL CLEAR");
            //        Debug.Print("Added NEW frame");
            //    }
            //    isFirstFrame = false;
            //}
            //else
            //{
            //    Debug.Print("NO PATTERN FOUND");
            //    if (!isFirstFrame)
            //    {
            //        Debug.Print("NOT the 1st FRAME");
            //        sBytes.AddRange(buffer);
            //        Debug.Print("Added COMPLETE BUFFER");
            //    }
            //    {
            //        Debug.Print("1st FRAME. Doing Nothing");
            //    }
            //}
        }

        private void DumpFrame(byte[] sBytes)
        {
            Debug.Print("Bitmap Buffer Size = " + sBytes.Length);
            Bitmap bmp = ImageFromArray(sBytes, 640, 480);
            //bmp.Save("DEMO-" + DateTime.Now.ToLongTimeString() + ".jpg", ImageFormat.Jpeg);
            pictureBox1.Image = bmp;
        }

        Bitmap ImageFromArray(byte[] data, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            //Create a BitmapData and Lock all pixels to be written 
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),ImageLockMode.WriteOnly, bmp.PixelFormat);
            //Copy the data from the byte array into BitmapData.Scan0
            Marshal.Copy(data, 0, bmpData.Scan0, data.Length);
            //Unlock the pixels
            bmp.UnlockBits(bmpData);
            //Return the bitmap 
            return bmp;
        }


        private void button1_Click(object sender, EventArgs e)
        {
            Button thisButton = (Button)sender;
            thisButton.Enabled = false;
            thisButton.BackColor = Color.DimGray;
            stopButton.Enabled = true;
            stopButton.BackColor = Color.Red;

            if (String.IsNullOrEmpty(selectedPortName))
            {
                MessageBox.Show("Please select the serial port to start monitoring", "Action Needed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                comboBox1.Focus();
            }
            else
            {
                monitorPort.DataReceived += MonitorPort_DataReceived;
                timer.Enabled = true;
                Application.DoEvents();
                if (!monitorPort.IsOpen)
                {
                    try
                    {
                        monitorPort.Open();
                    }
                    catch (Exception exp)
                    {
                        MessageBox.Show(exp.Message, "Action Needed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Button thisButton = (Button)sender;
            thisButton.Enabled = false;
            thisButton.BackColor = Color.DimGray;
            startButton.Enabled = true;
            startButton.BackColor = Color.PaleGreen;

            timer.Enabled = false;
            monitorPort.DataReceived -= MonitorPort_DataReceived;
        }

        private byte[] GetBase64DecodedData(string encodedString)
        {
            return System.Convert.FromBase64String(encodedString);
        }

        private string GetBase64EncodedString(byte[] dataArray)
        {
            return Convert.ToBase64String(dataArray);
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            string encString = GetBase64EncodedString(File.ReadAllBytes(imgPath));
            FileInfo fInfo = new FileInfo(imgPath);
            if (!Directory.Exists(Path.Combine(fInfo.Directory.ToString(), "proc")))
            {
                Directory.CreateDirectory(Path.Combine(fInfo.Directory.ToString(), "proc"));
            }
            File.WriteAllText(Path.Combine(fInfo.Directory.ToString(), "proc","encoded.txt"), encString);
            //--------------------------------------------------------------------------------------

            File.WriteAllBytes(Path.Combine(fInfo.Directory.ToString(), "proc", "encoded.jpg"), GetBase64DecodedData(File.ReadAllText(Path.Combine(fInfo.Directory.ToString(), "proc", "encoded.txt"))));
        }

        private void ProcessJsonSegments(string rawDataReceived, StringBuilder Base64StringSegmentsInt)
        {
            Debug.Print(rawDataReceived);
            //MQTTDataObject obj = (MQTTDataObject)JsonConvert.DeserializeObject(rawDataReceived);
            //if (obj.seg_id.Equals("-START-", StringComparison.CurrentCultureIgnoreCase))
            //{
            //    Base64StringSegmentsInt.Clear();
            //}
            //else if (obj.seg_id.Equals("-PART-", StringComparison.CurrentCultureIgnoreCase))
            //{
            //    Base64StringSegmentsInt.Append(obj.seg_data);
            //}
            //else if (obj.seg_id.Equals("-END-", StringComparison.CurrentCultureIgnoreCase))
            //{
            //    //write the data to output device
            //    byte[] Data = GetBase64DecodedData(Base64StringSegmentsInt.ToString());
            //    File.WriteAllBytes("reconstructed.jpg", Data);
            //}
        }

        public void DoneJpegRead(byte[] data)
        {
            try
            {
                MemoryStream memstream = new MemoryStream(data);

                JpegBitmapDecoder decoder = new JpegBitmapDecoder(memstream, BitmapCreateOptions.None, BitmapCacheOption.Default);
                MainImage.Source = decoder.Frames[0];
            }
            catch (System.Exception e)
            {
            }
        }

        public void DoPortThread()
        {
            //open serial port
            SerialPort port = new SerialPort("COM3", 9600);
            port.Open();

            //clear any bytes waiting in it
            while (port.BytesToRead > 0)
                port.ReadByte();

            byte[] buff = null;
            while (true)
            {
                port.Write(new byte[] { 0 }, 0, 1);

                string val = port.ReadLine().Trim();
                System.Diagnostics.Debug.WriteLine(val);
                if (val == "Size")
                {
                    int size = Convert.ToInt32(port.ReadLine().Trim());
                    buff = new byte[size];
                }
                else if (val == "Data")
                {
                    int idx = 0;
                    int lastdisplay = 0;
                    System.Diagnostics.Debug.WriteLine("Reading " + buff.Length.ToString() + " bytes");
                    while (idx < buff.Length)
                    {
                        idx += port.Read(buff, idx, buff.Length - idx);
                        if (idx > (lastdisplay + 1))
                        {
                            lastdisplay = idx;
                        }
                    }
                    Dispatcher.Invoke(new Action(delegate { DoneJpegRead(buff); }), new object[] { });
                }
            }

            port.Close();
        }
    }
}
