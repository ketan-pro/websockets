using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ws_sharp
{
    public class Header
    {
        public string ID { get; set; }
        public string Type { get; set; }
        public string TimeStamp { get; set; }
    }

    public class Content
    {
        public string CMD { get; set; }
        public object Params { get; set; }
    }

    public class Message
    {
        public Header header { get; set; }
        public Content content { get; set; }
    }
}
