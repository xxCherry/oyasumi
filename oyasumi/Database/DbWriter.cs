using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using oyasumi.Utilities;
using oyasumi.Database.Attributes;

namespace oyasumi.Database
{
    public class DbWriter
    {
        private static StringBuilder _builder = new StringBuilder();

        private static Dictionary<string, List<PropertyInfo>> Columns = new();
        private static Dictionary<string, Type> Tables = new();

        public static void Save(string path)
        {
            _builder.Append("t_oyasumi:");

            foreach (var type in Assembly.GetEntryAssembly().GetTypes())
            {
                var attr = type.GetCustomAttribute<Table>();
                if (attr is not null)
                {
                    var propList = new List<PropertyInfo>();

                    foreach (var property in type.GetProperties())
                        propList.Add(property);

                    Columns.Add(attr.Name, propList);
                    Tables.Add(attr.Name, type);
                }
            }

            for (var i = 0; i < Tables.Count; i++)
            {
                var table = Tables.ElementAt(i);

                _builder.Append($"{table.Key} ");
                _builder.Append('[');

                var props = Columns[table.Key];

                var tableObject = typeof(DbContext).GetField(table.Key)?.GetValue(null); // Get the instance of table's object

                IMultiKeyDictionary tableMultiDict = null;
                IDictionary tableDict = null;
                IList tableList = null;

                if (tableObject is not null)
                {
                    var interfaceTypes = tableObject.GetType().GetInterfaces();
                    if (interfaceTypes.FirstOrDefault() == typeof(IMultiKeyDictionary))
                    {
                        tableMultiDict = (IMultiKeyDictionary)tableObject;
                    }
                    else if (interfaceTypes.Contains(typeof(IDictionary)))
                    {
                        tableDict = (IDictionary)tableObject;
                    }
                    else
                    {
                        tableList = (IList)tableObject;
                    }
                }

                if (tableObject is null)
                {
                    Console.WriteLine($"Can't find database list for the {table.Key}.");
                    _builder.Append(']');
                    continue;
                }
                
                var instanceCount = tableDict is not null ?
                        tableDict.Count : tableMultiDict is not null ?
                        tableMultiDict.Count : tableList.Count;

                IEnumerable<object> tableDictValues = null;

                if (tableDict is not null)
                    tableDictValues = tableDict.Values.Cast<object>();
                
                for (var j = 0; j < instanceCount; j++)
                {
                    object instance = null;
   
                    instance = tableDict is not null ? tableDictValues.ElementAt(j) : tableMultiDict is not null ? tableMultiDict.ValueAt(j) : tableList[j];

                    for (var k = 0; k < props.Count; k++)
                    {
                        var prop = props[k];

                        _builder.Append($"{prop.PropertyType.FullName}\a {prop.GetValue(instance)}");

                        if (k != props.Count - 1)
                            _builder.Append("\x01");
                    }

                    if (j != instanceCount - 1)
                        _builder.Append(" \x02 ");
                }

                _builder.Append(']');
                if (i != Tables.Count - 1)
                    _builder.Append('\x03');
            }

            File.WriteAllText(path, _builder.ToString());
        }
    }
}
