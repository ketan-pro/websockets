using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
            bool isSecureWS = false;
            if (!isSecureWS)
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
            else
            {
                SecureWebsocketServer wss = new SecureWebsocketServer(8100);
                using (var ws = new WebSocket("wss://localhost:8100/service?qs=1"))
                {
                    ws.SslConfiguration.ServerCertificateValidationCallback =
                      (sender, certificate, chain, sslPolicyErrors) =>
                      {
                          string path = System.Environment.CurrentDirectory;
                          path = path.Substring(0, path.LastIndexOf("bin")) + @"cert\ApraLabs Root CA.cer";
                          string hash = X509Certificate.CreateFromCertFile(path).GetCertHashString();
                          return certificate.GetCertHashString().Equals(hash);
                      };

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
}
