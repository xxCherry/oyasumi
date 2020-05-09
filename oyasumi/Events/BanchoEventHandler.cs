using System;
using System.Linq;
using System.Reflection;
using HOPEless.Bancho;
using oyasumi.Objects;

namespace oyasumi.Events
{
    public static class BanchoEventHandler
    {
        public static void Handle(BanchoPacket packet, Player player)
        {
            var Interfaces = from t in Assembly.GetExecutingAssembly().GetTypes()
                             where t.GetInterfaces().Contains(typeof(IPacket))
                                      && t.GetConstructor(Type.EmptyTypes) != null // check if type has parameterless constructor
                             select Activator.CreateInstance(t) as IPacket;

            var PacketEvent = Interfaces.FirstOrDefault(x => x.GetType().Name == packet.Type.ToString());

            Console.WriteLine("Packet Type: " + packet.Type.ToString() + " Value: " + BitConverter.ToString(packet.Data));

            if (PacketEvent != default)
            {
                PacketEvent.Handle(packet, player);
            }
        } 
    }
}

