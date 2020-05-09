using oyasumi.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace oyasumi.Objects
{
    public static class Global
    {
        public static OyasumiDbContext DBContext;
        public static HttpListenerRequest Request;
        public static HttpListenerResponse Response;
    }
}
