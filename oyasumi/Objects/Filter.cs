using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace oyasumi.Objects
{
    /* Presence filter format>
*  It's simple, for example:
*  CurrentMatch is not null
*  they're splitted by | 
*  CurrentMatch is not null | Spectators is not null | Accuracy greater 0
*  It's equivalent to CurrentMatch is not null or Spectators is not null or Accuracy greater 0.0
*  So if one of the checks passed we're executing command
*/
    public class Filter
    {
        private string _filter;
        private object _presence;

        public Filter(string filter, object pr)
        {
            _filter = filter;
            _presence = pr;
        }

        /// <summary> Presence filter format
        /// 
        /// <para> It's simple, for example: </para>
        /// <para> CurrentMatch is not null </para>
        /// 
        /// <para> They're splitted by | </para>
        /// <para> CurrentMatch is not null | Spectators is not null | Accuracy greater 0.0 </para>
        /// <para>It's equivalent to CurrentMatch is not null or Spectators is not null or Accuracy greater 0.0 </para>
        /// 
        /// <para> First string is always field of Presence </para>
        /// 
        /// <para> 
        /// Number types:
        /// Integer: 1, 2, 3 etc.
        /// Double: 1.5, 1.6, 1.9, etc.
        /// Float: 1.6f, 1.4f, 1.8f etc.
        /// </para>
        /// </summary>
        /// <returns></returns>
        public bool IsMatch()
        {
            bool noOther = _filter.IndexOf('|') == -1;

            var expressions =  noOther ? new [] { _filter } : _filter.Split("|");

            foreach (var expr in expressions)
            {
                var tokens = expr.Split(' ');
                var field = tokens[0];
                for (var i = 0; i < tokens.Length; i++)
                {
                    var token = tokens[i];
                    if (token == "is")
                    {
                        if (i + 1 > tokens.Length)
                            throw new("No value to compare.");
                        var next = tokens[i + 1];
                        if (next == "not")
                        {
                            if (i + 2 > tokens.Length)
                                throw new("No value to compare.");
                            next = tokens[i + 2];
                            if (next == "null")
                            {
                                return typeof(Presence).GetField(field).GetValue(_presence) is not null;
                            }
                        }
                        if (next == "null")
                        {
                            return typeof(Presence).GetField(field).GetValue(_presence) is null;
                        }

                        if (next == "greater")
                        {
                            if (i + 2 > tokens.Length)
                                throw new("No value to compare.");
                            next = tokens[i + 2];
                            if (next.Contains("."))
                            {
                                if (next.LastOrDefault() == 'f')
                                {
                                    if (float.TryParse(next, out var f))
                                    {
                                        return f > (float)typeof(Presence).GetField(field).GetValue(_presence);
                                    }
                                }

                                if (double.TryParse(next, out var d))
                                {
                                    return d > (double)typeof(Presence).GetField(field).GetValue(_presence);
                                }
                            }

                            if (int.TryParse(next, out var num))
                            {
                                return num > (int)typeof(Presence).GetField(field).GetValue(_presence);
                            }
                        }

                        if (next == "less")
                        {
                            if (i + 2 > tokens.Length)
                                throw new("No value to compare.");
                            next = tokens[i + 2];
                            if (next.Contains("."))
                            {
                                if (next.LastOrDefault() == 'f')
                                {
                                    if (float.TryParse(next, out var f))
                                    {
                                        return f < (float)typeof(Presence).GetField(field).GetValue(_presence);
                                    }
                                }

                                if (double.TryParse(next, out var d))
                                {
                                    return d < (double)typeof(Presence).GetField(field).GetValue(_presence);
                                }
                            }

                            if (int.TryParse(next, out var num))
                            {
                                return num < (int)typeof(Presence).GetField(field).GetValue(_presence);
                            }
                        }
                    }
                    if (token == string.Empty)
                        continue;
                }
            }
            return false;
        }
    }
}
