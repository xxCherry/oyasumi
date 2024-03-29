﻿using System.Linq;

namespace oyasumi.Objects
{
    // Don't recommend to look thru this
    public class Filter
    {
        private readonly string _filter;
        private readonly object _presence;

        public Filter(string filter, object pr)
        {
            _filter = filter;
            _presence = pr;
        }

        /// <summary>Presence filter format
        /// 
        /// <para> It's simple, for example: </para>
        /// <para> CurrentMatch is not null </para>
        /// 
        /// <para> They're can be splitted by | </para>
        /// <para> CurrentMatch is not null | Spectators is not null | Accuracy greater 0.0 </para>
        /// <para>It's equivalent to CurrentMatch is not null or Spectators is not null or Accuracy greater 0.0 </para>
        /// 
        /// <para> First string is always field of Presence</para>
        /// 
        /// <para> 
        /// Number types:
        /// Integer: 1, 2, 3 etc.
        /// Double: 1.5, 1.6, 1.9 etc.
        /// Float: 1.6f, 1.4f, 1.8f etc.
        /// </para>
        /// </summary>
        public bool IsMatch()
        {
            var noOther = !_filter.Contains('|');

            var expressions = noOther ? new [] { _filter } : _filter.Split("|");

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
                                return typeof(Presence).GetField(field).GetValue(_presence) is not null;
                        }
                        if (next == "null")
                            return typeof(Presence).GetField(field).GetValue(_presence) is null;

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
                                    return d > (double)typeof(Presence).GetField(field).GetValue(_presence);
                            }

                            if (int.TryParse(next, out var num))
                                return num > (int)typeof(Presence).GetField(field).GetValue(_presence);
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
                                        return f < (float)typeof(Presence).GetField(field).GetValue(_presence);
                                }

                                if (double.TryParse(next, out var d))
                                    return d < (double)typeof(Presence).GetField(field).GetValue(_presence);
                            }

                            if (int.TryParse(next, out var num))
                                return num < (int)typeof(Presence).GetField(field).GetValue(_presence);
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
