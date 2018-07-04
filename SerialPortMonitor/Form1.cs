using System;
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
            port.BaudRate = 115200;
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
                this.Invoke(new MethodInvoker(delegate {
                    label3.Text = String.Format("Data stream received at - {0}", DateTime.Now.ToLongTimeString());
                }));

                SerialPort senderPort = (SerialPort)sender;
                string indata = senderPort.ReadLine();
                ProcessJsonSegments(indata, Base64StringSegments);
                Debug.Write(indata);

                return;
            }
            
            //byte[] completeDataArray = new byte[3000];
            //if (monitorPort.BytesToRead > 0)
            //{
            //    byte[] inbyte = new byte[1];
            //    monitorPort.Read(inbyte, 0, 1);
            //    if (inbyte.Length > 0)
            //    {
            //        byte value = (byte)inbyte.GetValue(0);
            //        //do other necessary processing you may want. 
            //    }
            //}
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
            MQTTDataObject obj = (MQTTDataObject)JsonConvert.DeserializeObject(rawDataReceived);
            if (obj.seg_id.Equals("-START-", StringComparison.CurrentCultureIgnoreCase))
            {
                Base64StringSegmentsInt.Clear();
            }
            else if (obj.seg_id.Equals("-PART-", StringComparison.CurrentCultureIgnoreCase))
            {
                Base64StringSegmentsInt.Append(obj.seg_data);
            }
            else if (obj.seg_id.Equals("-END-", StringComparison.CurrentCultureIgnoreCase))
            {
                //write the data to output device
                byte[] Data = GetBase64DecodedData(Base64StringSegmentsInt.ToString());
                File.WriteAllBytes("reconstructed.jpg", Data);
            }
        }
    }
}
