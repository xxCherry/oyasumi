using HOPEless.Bancho;
using oyasumi.Objects;


namespace oyasumi
{
    public interface IPacket
    {
        void Handle(BanchoPacket packet, Player player);
    }
}
