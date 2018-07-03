using System;
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

namespace SerialPortMonitor
{
    public partial class Form1 : Form
    {
        string selectedPortName = null;
        Timer timer = new Timer();
        SerialPort monitorPort = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            timer.Tick += Timer_Tick;
            timer.Interval = 10;
            string mcName = String.Format("{0}", Environment.MachineName);
            label2.Text += mcName;
            PopulateAllSerialPorts();
            comboBox1.SelectedIndex = (mcName.ToUpper().Equals("CHANTI-BANTI")) ? (comboBox1.Items.IndexOf("COM6")) : 0;
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
            
        }

        private void MonitorPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            label3.Text += DateTime.Now.ToLongTimeString();
            SerialPort senderPort = (SerialPort)sender;
            string indata = senderPort.ReadExisting();
            Debug.Print("Data Received:");

            byte[] completeDataArray = new byte[3000];
            if (monitorPort.BytesToRead > 0)
            {
                byte[] inbyte = new byte[1];
                monitorPort.Read(inbyte, 0, 1);
                if (inbyte.Length > 0)
                {
                    byte value = (byte)inbyte.GetValue(0);
                    //do other necessary processing you may want. 
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(selectedPortName))
            {
                MessageBox.Show("Please select the serial port to start monitoring", "Action Needed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                comboBox1.Focus();
            }
            else
            {
                timer.Enabled = true;
                if (!monitorPort.IsOpen)
                {
                    try
                    {
                        monitorPort.DataReceived += MonitorPort_DataReceived;
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
            timer.Enabled = false;
            monitorPort.DataReceived -= MonitorPort_DataReceived;
        }
    }
}
