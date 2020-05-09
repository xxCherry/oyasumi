using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace oyasumi.Helpers
{
    public static class StreamHelper
    {
        public static string ReadBodyFromStream(Stream stream)
        {
            using var reader = new StreamReader(stream, leaveOpen: true);
            var response = reader.ReadToEnd();
            return response;
        }
    }
}
