using oyasumi.Enums;
using System.Runtime.InteropServices;

namespace oyasumi.Objects
{
	public struct Packet
	{
		public PacketType Type { get; set; }
		public byte[] Data { get; set; }
	}
}