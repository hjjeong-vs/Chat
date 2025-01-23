using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat
{
    public class ReceivePacket
    {
        public int type { get; set; }
        public string Name { get; set; }
        public string Message { get; set; }
        public DateTime date { get; set; }
        public List<string> clients { get; set; }
    }
}
