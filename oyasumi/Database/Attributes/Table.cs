using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oyasumi.Database.Attributes
{
    public class Table : Attribute
    {
        public string Name;
        public Table(string name)
        {
            Name = name;
        }
    }
}
