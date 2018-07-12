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
using System.Threading;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SerialPortMonitor
{
    public partial class Form1 : Form
    {
        string selectedPortName = null;
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        SerialPort monitorPort = null;
        Button startButton = null;
        Button stopButton = null;
        string imgPath = null;
        StringBuilder Base64StringSegments = new StringBuilder();
        bool isFirstFrame = true;
        Dictionary<int, int> patternsTable = new Dictionary<int, int>();
        int patternCounter = 0;
        List<byte> sBytes = new List<byte>();
        byte[] imgBytes;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            imgPath = (Environment.GetCommandLineArgs().Length > 1) ? Environment.GetCommandLineArgs()[1] : @"E:\PICTURES\2t.jpg";

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
            port.BaudRate = 9600;
            port.DataBits = 8;
            HandlePort(port);
        }

        private void HandlePort(SerialPort port)
        {
            bool markerFound = false;
            List<int> markPos = new List<int>();
            int counter = 0;
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                if ((port.IsOpen)&&(port.BytesToRead > 0))
                {
                    string data = port.ReadExisting();
                    if (data.Contains("*RDY*"))
                    {
                        counter++;
                    }
                    sb.Append(data);
                    Thread.Sleep(50);
                    if (counter > 2)
                    {
                        string _temp = sb.ToString();
                        int pos1 = _temp.IndexOf("*RDY*");
                        int pos2 = _temp.IndexOf("*RDY*", pos1+4);
                        string _data = _temp.Substring(pos1 + 5, pos2);
                    }
                }
                else
                {
                    if (!port.IsOpen) port.Open();
                }
            }
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
        }

        private void DumpFrame(byte[] imgBytes)
        {
            int width = 640;
            int height = 480;
            int multiplier = width * height ;
            if (sBytes.Count() > multiplier)
            {
                byte[] temp = sBytes.Take(multiplier).ToArray();
                byte[] imageData = new byte[multiplier];
                sBytes.RemoveRange(0, multiplier);
                //Here create the Bitmap to the know height, width and format
                Bitmap bmp = new Bitmap(width, height, PixelFormat.Gdi);

                ////Create a BitmapData and Lock all pixels to be written 
                BitmapData bmpData = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadWrite, bmp.PixelFormat);

                ////Copy the data from the byte array into BitmapData.Scan0
                Marshal.Copy(temp, 0, bmpData.Scan0, imageData.Length);
                ////Unlock the pixels
                bmp.UnlockBits(bmpData);
                pictureBox1.Image = bmp;
            }
        }

        Bitmap ImageFromArray(byte[] data, int width, int height)
        {
            Bitmap b2 = new Bitmap(@"G:\OneDrive\Arduino_Workspace\640_480_best.jpg");
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format16bppRgb565);
            //Create a BitmapData and Lock all pixels to be written 
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);
            //Copy the data from the byte array into BitmapData.Scan0
            Marshal.Copy(data, 0, bmpData.Scan0, data.Length );
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
            ReadTestImage(@"G:\OneDrive\Arduino_Workspace\640_480_best.jpg");
            
            
            //string encString = GetBase64EncodedString(File.ReadAllBytes(imgPath));
            //FileInfo fInfo = new FileInfo(imgPath);
            //if (!Directory.Exists(Path.Combine(fInfo.Directory.ToString(), "proc")))
            //{
            //    Directory.CreateDirectory(Path.Combine(fInfo.Directory.ToString(), "proc"));
            //}
            //File.WriteAllText(Path.Combine(fInfo.Directory.ToString(), "proc", "encoded.txt"), encString);
            ////--------------------------------------------------------------------------------------

            //File.WriteAllBytes(Path.Combine(fInfo.Directory.ToString(), "proc", "encoded.jpg"), GetBase64DecodedData(File.ReadAllText(Path.Combine(fInfo.Directory.ToString(), "proc", "encoded.txt"))));
        }

        private void ReadTestImage(string imgPath)
        {
            imgBytes = File.ReadAllBytes(imgPath);
            DumpFrame(imgBytes);
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

    }
}
