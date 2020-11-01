using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oyasumi.Utilities
{
    public class ResponseUtils
    {
        public static ContentResult Content(string message, int code = 200)
        {
            var result = new ContentResult
            {
                StatusCode = code,
                Content = message
            };

            return result;
        }
    }
}
