using oyasumi.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oyasumi.Attributes
{
    public class CommandAttribute : Attribute
    {
        public string Command;
        public string Description;
        public int RequiredArgs;
        public bool IsPublic;
        public Privileges PrivilegesRequired;

        public CommandAttribute(string command, string description, bool isPublic, Privileges privileges, int requiredArgs = -1)
        {
            Command = command;
            Description = description;
            RequiredArgs = requiredArgs;
            IsPublic = isPublic;
            PrivilegesRequired = privileges;
        }
    }
}
