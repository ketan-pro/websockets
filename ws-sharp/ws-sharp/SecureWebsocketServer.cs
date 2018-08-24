using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ws_sharp
{
    class WSService : WebSocketBehavior
    {
        Dictionary<string, WebSocket> clients;
        public WSService()
        {
            IgnoreExtensions = true;
            OriginValidator = (val) => { return true; };
            CookiesValidator = (req, res) => { return true; };
        }
        protected override void OnOpen()
        {
            clients = clients == null ? new Dictionary<string, WebSocket>() : clients;
            clients.Add(ID, Context.WebSocket);
            base.OnOpen();
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            if (e.Data.Contains("subscribe"))
            {
                string substr = e.Data.Substring(e.Data.IndexOf("\"name\":\"") + 8);
                WSCache.ClientConfig conf = new WSCache.ClientConfig();
                conf.name = substr.Substring(0, substr.IndexOf("\""));
                string camCnt = substr.Substring(substr.IndexOf("\"cameras\":") + 10);
                for (int i = 0; i < int.Parse(camCnt.Substring(0, camCnt.IndexOf("}"))); i++)
                {
                    conf.cameras.Add(ID + "_" + (i + 1));
                }
                WSCache.Instance.clients.Add(ID, conf);
                Sessions.Broadcast(e.Data);
            }
            else if (e.Data.Contains("getCameras"))
            {
                Send(JsonConvert.SerializeObject(WSCache.Instance.clients));
            }
            else
            {
                var msg = e.Data == "BALUS"
                ? "I've been balused already..."
                : "I'm not available now.";

                Send(msg);
            }
            //else
            //{
            //    foreach (string id in Sessions.ActiveIDs)
            //    {
            //        if (!id.Equals(ID))
            //        {
            //            Sessions.SendTo(e.Data, id);
            //        }
            //    }
            //}
            base.OnMessage(e);
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.Exception);
            base.OnError(e);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Console.WriteLine(e.Code);
            base.OnClose(e);
        }
    }

    class SecureWebsocketServer
    {
        WebSocketServer server;

        public SecureWebsocketServer(string url)
        {
            string str = url.Substring(url.IndexOf("//") + 2);
            str = str.Substring(str.IndexOf("/") + 1);
            string[] addr = str.Split(':');
            IPAddress ipAddr = IPAddress.Parse(addr[0]);
            server = new WebSocketServer(ipAddr, int.Parse(addr[1]), true);
            startServer();
        }

        public SecureWebsocketServer(int port)
        {
            server = new WebSocketServer(port, true);
            startServer();
        }

        private void startServer()
        {
            string path = System.Environment.CurrentDirectory;
            path = path.Substring(0, path.LastIndexOf("bin")) + @"cert\ApraLabs Root CA.pfx";
            server.SslConfiguration.ServerCertificate = new X509Certificate2(path, "@prA!a6sD3vTe@m");
            server.AddWebSocketService<WSService>("/service");
            server.Start();
        }

        ~SecureWebsocketServer()
        {
            server.Stop();
        }
    }
}
