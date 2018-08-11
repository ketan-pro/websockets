using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ws_sharp
{
    public sealed class WSCache
    {
        private static readonly WSCache instance = new WSCache();
        
        public class ClientConfig
        {
            public string name;
            public List<string> cameras = new List<string>();
        }
        public Dictionary<string, ClientConfig> clients;
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static WSCache() { }
        private WSCache() {
            clients = new Dictionary<string, ClientConfig>();
        }

        public static WSCache Instance
        {
            get
            {
                return instance;
            }
        }
    }

    class WSController : WebSocketBehavior
    {
        Dictionary<string, WebSocket> clients;
        public WSController()
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
                foreach (string id in Sessions.ActiveIDs)
                {
                    if (!id.Equals(ID))
                    {
                        Sessions.SendTo(e.Data, id);
                    }
                }
            }
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
    public class WebsocketServer
    {
        WebSocketServer server;

        public WebsocketServer(string url)
        {
            server = new WebSocketServer(url);
            server.AddWebSocketService<WSController>("/main");
            server.Start();
        }

        ~WebsocketServer()
        {
            server.Stop();
        }
    }
}
