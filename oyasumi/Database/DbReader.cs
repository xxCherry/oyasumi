using oyasumi.Utilities;
using oyasumi.Database.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace oyasumi.Database
{
    public class DbReader
    {
        private static Dictionary<string, List<PropertyInfo>> Columns = new();
        private static Dictionary<string, Type> Tables = new();

        /// <summary>
        /// Parses and loads objects from database file
        /// </summary>
        /// <param name="path">Path to the database file.</param>
        /// <returns></returns>
        public static void Load(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            var lines = File.ReadAllLines(path);

            if (lines.Length > 0)
            {
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

                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.StartsWith("t_"))
                    {
                        var tableSeparator = line.IndexOf(":");

                        var databaseName = line[2..tableSeparator];
                        var tablesData = line[(tableSeparator + 1)..^0];

                        var tableLines = tablesData.Split('\x03');

                        foreach (var tableLine in tableLines)
                        {
                            var tableName = tableLine.Split(" ")[0];
                            var tableObject = typeof(DbContext).GetField(tableName)?.GetValue(null); // Get the instance of table's object

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
                                Console.WriteLine($"Can't find table's list for the {tableName}.");

                            var startBracket = tableLine.IndexOf('[') + 1;
                            var endBracket = tableLine.LastIndexOf(']');

                            var tableData = tableLine[startBracket..endBracket];

                            var rows = tableData.Split(" \x02 ");

                            if (rows.Length == 1 && rows[0] == string.Empty)
                                continue;

                            foreach (var row in rows)
                            {
                                var rowValues = row.Split('\x01'); // fuck you osu!

                                object instance = null;

                                if (Tables.TryGetValue(tableName, out var tableType))
                                    instance = Activator.CreateInstance(tableType);

                                List<PropertyInfo> rowProperties = null;

                                if (Columns.TryGetValue(tableName, out var props))
                                    rowProperties = props;

                                for (var j = 0; j < rowValues.Length; j++)
                                {
                                    var rowValue = rowValues[j];
                                    var typeValue = rowValue.Split('\a');
                                    var type = typeValue[0].Trim();
                                    var value = string.Empty;
                                    if (typeValue.Length > 2)
                                    {
                                        var builder = new StringBuilder();
                                        for (var k = 1; k < typeValue.Length; k++)
                                            builder.Append(typeValue[k]);

                                        value = builder.ToString().TrimStart();
                                    }
                                    else
                                    {
                                        value = typeValue[1].TrimStart();
                                    }

                                    if (rowProperties is not null)
                                    {
                                        var prop = rowProperties[j];

                                        if (prop.PropertyType.FullName != type)
                                        {
                                            Console.WriteLine($"{prop.Name} ({prop.PropertyType.FullName}) type is not equal to it's database column type (Database type: {type}). Skipping...");
                                            continue;
                                        }
                                        var rowType = Type.GetType(type);
                                        object convertedValue = null;

                                        if (rowType.IsEnum)
                                            convertedValue = Enum.Parse(rowType, value);
                                        else if (rowType == typeof(DateTimeOffset))
                                            convertedValue = DateTimeOffset.Parse(value);
                                        else
                                        {
                                            convertedValue = Convert.ChangeType(value, rowType);
                                        }

                                        prop.SetValue(instance, convertedValue);
                                    }
                                }

                                if (tableMultiDict is not null && rowValues.Length > 2)
                                    tableMultiDict.Add(rowProperties[0].GetValue(instance), rowProperties[1].GetValue(instance), instance);

                                if (tableDict is not null)
                                    tableDict.Add(rowProperties[0].GetValue(instance), instance);

                                if (tableList is not null)
                                    tableList.Add(instance);
                            }
                        }
                    }
                }
            }
        }
    }
}
