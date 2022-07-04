using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebServer.Classes
{
    enum ServerError
    {
        OK,
        ExpiredSession,
        NotAuthorized,
        FileNotFound,
        PageNotFound,
        ServerError,
        UnknownType,
    }

    class ExtensionInfo
    {
        public Func<string, string, ExtensionInfo, ResponsePacket> loader { get; set; }
        public string contentType { get; set; }
    }

    class ResponsePacket
    {
        public byte[] Data { get; set; }
        public string ContentType { get; set; }
        public Encoding Encoding { get; set; }
        public ServerError Error { get; set; }
        public string Redirect { get; set; }
    }
    class Router
    {
        private Dictionary<string, ExtensionInfo> extFolderMap;
        private string BaseFolder;
        public Router()
        {

            string[] directory = AppContext.BaseDirectory.Split(Path.DirectorySeparatorChar);
            ArraySegment<string> slice = new ArraySegment<string>(directory, 0, directory.Length - 4);
            BaseFolder = Path.Combine(slice.ToArray()) + "\\HtmlData";

            extFolderMap = new Dictionary<string, ExtensionInfo>()
            {
                {"ico", new ExtensionInfo() {loader = ImageLoader, contentType = "image/ico"} },
                {"png", new ExtensionInfo() {loader = ImageLoader, contentType = "image/png"} },
                {"jpg", new ExtensionInfo() {loader = ImageLoader, contentType = "image/jpg"} },
                {"gif", new ExtensionInfo() {loader = ImageLoader, contentType = "image/gif"} },
                {"bmp", new ExtensionInfo() {loader = ImageLoader, contentType = "image/bmp"} },
                {"html", new ExtensionInfo() {loader = PageLoader, contentType = "text/html"} },
                {"css", new ExtensionInfo() {loader = FileLoader, contentType = "text/css"} },
                {"js", new ExtensionInfo() {loader = FileLoader, contentType = "text/javascript"} },
                {"/", new ExtensionInfo() {loader = PageLoader, contentType = "text/html"} },
            };
        }

        private ResponsePacket ImageLoader(string fullPath, string ext, ExtensionInfo extInfo)
        {
            ResponsePacket ret = null;
            try
            {
                FileStream fs = new FileStream(BaseFolder + "\\" + fullPath.Replace('/', '\\'), FileMode.Open, FileAccess.Read);
                BinaryReader br = new BinaryReader(fs);

                ret = new ResponsePacket() { Data = br.ReadBytes((int)fs.Length), ContentType = extInfo.contentType };

                br.Close();
                fs.Close();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);

                return new ResponsePacket() { Error = ServerError.FileNotFound, Redirect = Router.ErrorHandler(ServerError.FileNotFound) };
            }

            return ret;
        }

        private ResponsePacket FileLoader(string fullPath, string ext, ExtensionInfo extInfo)
        {
            try
            {
                string text = File.ReadAllText((fullPath.Contains(BaseFolder) ? "" : BaseFolder) + fullPath.Replace('/', '\\'));

                return new ResponsePacket() { Data = Encoding.UTF8.GetBytes(text), ContentType = extInfo.contentType, Encoding = Encoding.UTF8 };
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);

                if (ext == "html" || ext == "")
                {
                    return new ResponsePacket() { Error = ServerError.PageNotFound, Redirect = Router.ErrorHandler(ServerError.PageNotFound) };
                }
                else
                {
                    return new ResponsePacket() { Error = ServerError.FileNotFound, Redirect = Router.ErrorHandler(ServerError.FileNotFound) };
                }
            }
        }

        private ResponsePacket PageLoader(string fullPath, string ext, ExtensionInfo extInfo)
        {
            ResponsePacket ret = new ResponsePacket();

            if (fullPath == "/")
            {
                ret = Route("GET", "\\Pages\\index.html", null);
            }
            else
            {
                if (String.IsNullOrEmpty(ext))
                {
                    fullPath = fullPath + ".html";
                }

                string rightOf = fullPath.Replace(BaseFolder, "");

                fullPath = BaseFolder + (fullPath.Contains("Pages") ? "" : "\\Pages") + rightOf;
                ret = FileLoader(fullPath, ext, extInfo);
            }

            return ret;
        }

        public ResponsePacket Route(string verb, string path, Dictionary<string, string> kvParams)
        {
            string[] array = path.Split('.');
            string ext = array[array.Length - 1];

            ExtensionInfo extensionInfo;
            ResponsePacket ret = null;

            Console.WriteLine("Called route.");
            Console.WriteLine(verb);
            Console.WriteLine(path);
            Console.WriteLine(ext);

            if (extFolderMap.TryGetValue(ext, out extensionInfo))
            {
                string fullPath = Path.Combine(BaseFolder, path);
                Console.WriteLine(fullPath);
                ret = extensionInfo.loader(path, ext, extensionInfo);
            }
            else
            {
                return new ResponsePacket() { Error = ServerError.UnknownType, Redirect = ErrorHandler(ServerError.UnknownType) };
            }

            return ret;
        }

        public static string ErrorHandler(ServerError error)
        {
            switch (error)
            {
                case ServerError.ExpiredSession:
                    return "/ErrorPages/expiredSession.html";
                case ServerError.FileNotFound:
                    return "/ErrorPages/fileNotFound.html";
                case ServerError.NotAuthorized:
                    return "/ErrorPages/notAuthorized.html";
                case ServerError.PageNotFound:
                    return "/ErrorPages/pageNotFound.html";
                case ServerError.ServerError:
                    return "/ErrorPages/serverError.html";
                case ServerError.UnknownType:
                    return "/ErrorPages/unknownType.html";
            }

            return String.Empty;
        }
    }
}
