using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialPortMonitor
{
    public class MQTTDataObject
    {
        public DateTime tid { get; set; }
        public int seg_id { get; set; }
        public string seg_data { get; set; }
    }
}
