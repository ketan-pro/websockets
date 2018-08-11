using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WebSocketSharp;

namespace ws_sharp
{
    class Program
    {
        static void Main(string[] args)
        {
            WebsocketServer server = new WebsocketServer("ws://0.0.0.0:8100");
            using (var ws = new WebSocket("ws://localhost:8100/main?qs=1"))
            {
                ws.OnMessage += (sender, e) =>
                {
                    Console.WriteLine("Laputa says: " + e.Data);
                };
                ws.Connect();
                ws.Send("BALUS");
                Console.ReadKey(true);
            }
        }
    }
}
