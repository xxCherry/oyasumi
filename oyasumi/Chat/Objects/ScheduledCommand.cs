using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static oyasumi.Chat.CommandEvents;

namespace oyasumi.Chat.Objects
{
    public class ScheduledCommand
    {
        public string Name { get; set; }
        public string[] Args { get; set; }
        public int ArgsRequired { get; set; }
        public OnArgsPushed OnArgsPushed { get; set; }
        public bool NoErrors { get; set; }
    }
}
