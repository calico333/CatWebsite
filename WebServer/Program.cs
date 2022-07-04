using System;
using System.IO;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Sockets;
using WebServer.Classes;

namespace WebServer
{
    class Program
    { 
        static void Main(string[] args)
        {
            Server server = new Server("http://localhost:8080/");
            server.Start();
            
            Console.ReadLine();
        }
    }
}
