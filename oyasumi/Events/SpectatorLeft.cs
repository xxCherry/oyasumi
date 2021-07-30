using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Layouts;
using oyasumi.Managers;
using oyasumi.Objects;
using oyasumi.Utilities;
using System.IO;
using System.Threading.Tasks;

namespace oyasumi.Events
{
    public class SpectatorLeft
    {
        [Packet(PacketType.ClientSpectateStop)]
        public static async Task Handle(Packet p, Presence pr)
        {
            if (pr.Spectating is not null)
            {
                pr.Spectating.Spectators.Remove(pr);
                await pr.Spectating.SpectatorLeft(pr.Id);
                await pr.LeaveChannel($"spect_{pr.Spectating.Id}", true);

                if (pr.Spectators.Count == 0)
                    await pr.Spectating.LeaveChannel($"spect_{pr.Spectating.Id}", true)
                        ;

                pr.Spectating = null;
            }
        }
    }
}