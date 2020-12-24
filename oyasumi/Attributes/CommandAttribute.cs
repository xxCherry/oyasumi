using oyasumi.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using oyasumi.Objects;
using System.Reflection;

namespace oyasumi.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string Command;
        public string Description;
        public int RequiredArgs;
        public bool IsPublic;
        public Privileges PrivilegesRequired;
        public string Filter;
        public bool Scheduled;
        public string OnArgsPushed;

        public CommandAttribute(string command, string description, bool isPublic, Privileges privileges, int requiredArgs = -1, bool scheduled = false, string filter = null, string onArgsPushed = null)
        {
            Command = command;
            Description = description;
            RequiredArgs = requiredArgs;
            IsPublic = isPublic;
            PrivilegesRequired = privileges;
            Filter = filter;
            Scheduled = scheduled;

            // Available only on scheduled commands where you can pass multi-line args, so you will able to check argument
            // (Really hate that i can't pass Delegate or atleast MethodInfo here)
            OnArgsPushed = onArgsPushed;
        }
    }
}
