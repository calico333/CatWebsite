using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebServer.Classes
{
    class Server
    {
        private Router router;
        private HttpListener listener;
        private Semaphore sem = new Semaphore(100, 100);
        public int maxConnections = 100, connections = 0;
        public string address;
        public bool running = false;

        public Server(string address)
        {
            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add(address);

                router = new Router();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            this.address = address;

            Console.WriteLine(Dns.GetHostName());
        }

        private async void StartConnections(HttpListener listener)
        {
            HttpListenerContext ctx = null;
            HttpListenerRequest req = null; 
            HttpListenerResponse resp = null;
            ResponsePacket respPacket = null;

            try
            {
                ctx = await listener.GetContextAsync();
                sem.Release();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                respPacket = new ResponsePacket() { Redirect = Router.ErrorHandler(ServerError.ServerError) };
            }

            if (ctx != null)
            {
                req = ctx.Request;
                resp = ctx.Response; 
                Console.WriteLine(req.Url.AbsolutePath);

                Console.WriteLine(req.Url.ToString());
                Console.WriteLine(req.RawUrl.ToString());
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);
                Console.WriteLine();

                Dictionary<string, string> parameters = req.QueryString.Keys.Cast<string>().ToDictionary(k => k, v => req.QueryString[v]);

                respPacket = router.Route(req.HttpMethod, req.RawUrl.Contains('?') ? req.RawUrl.Substring(0, req.RawUrl.IndexOf("?")) : req.RawUrl, parameters);
            }                       
          
            if (respPacket.Error != ServerError.OK)
            {
                //respPacket = router.Route("GET", Router.ErrorHandler(respPacket.Error), null);
                respPacket.Redirect = Router.ErrorHandler(respPacket.Error);
            }

            if (string.IsNullOrEmpty(respPacket.Redirect))
            {
                resp.ContentType = respPacket.ContentType;
                resp.ContentLength64 = respPacket.Data.LongLength;
                resp.ContentEncoding = respPacket.Encoding;
                resp.StatusCode = (int)HttpStatusCode.OK;

                await resp.OutputStream.WriteAsync(respPacket.Data, 0, respPacket.Data.Length);
            }
            else
            {
                resp.StatusCode = (int)HttpStatusCode.Redirect;
                Console.WriteLine("Redirected");
                resp.Redirect(address + respPacket.Redirect);
            }

            resp.OutputStream.Close();

        }

        private void RunServer(HttpListener listener)
        {
            while (running)
            {
                sem.WaitOne();

                StartConnections(listener);
            }
        }

        public void Start()
        {
            try
            {
                listener.Start();
                Task.Run(() => this.RunServer(listener));
                running = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
