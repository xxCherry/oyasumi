using System;
using oyasumi.Enums;

namespace oyasumi
{
    public class PacketAttribute : Attribute
    {
        public PacketType PacketType;
        public PacketAttribute(PacketType t) =>
            PacketType = t;
    }}