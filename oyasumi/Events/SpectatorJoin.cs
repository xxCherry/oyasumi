using System.IO;
using System.Threading.Tasks;
using oyasumi.Enums;
using oyasumi.IO;
using oyasumi.Layouts;
using oyasumi.Managers;
using oyasumi.Objects;

namespace oyasumi.Events
{
    public class SpectatorJoin
    {
        [Packet(PacketType.ClientSpectateStart)]
        public static async Task Handle(Packet p, Presence pr)
        {
            var ms = new MemoryStream(p.Data);
            using var reader = new SerializationReader(ms);

            var userId = reader.ReadInt32();

            var spectatingPresence = PresenceManager.GetPresenceById(userId);

            pr.Spectating = spectatingPresence;
            await pr.Spectating.SpectatorJoined(pr.Id);

            if (pr.Spectating.SpectatorChannel is null)
            {
                var channel = new Channel($"spect_{pr.Spectating.Id}", "", 1, true);
                pr.Spectating.SpectatorChannel = channel;

                ChannelManager.Channels.TryAdd(channel.RawName, channel);

                await pr.Spectating.JoinChannel($"spect_{pr.Spectating.Id}");
            }

            await pr.JoinChannel($"spect_{pr.Spectating.Id}");

            spectatingPresence.Spectators.Add(pr);
        }
    }
}