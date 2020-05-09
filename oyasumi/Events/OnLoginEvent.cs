using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HOPEless.Bancho;
using HOPEless.Bancho.Objects;
using osu.Shared.Serialization;
using oyasumi.Helpers;
using oyasumi.Objects;

namespace oyasumi.Events
{
    public struct LoginData
    {
        public string Username;
        public string Password;
        public string OsuVersion;
        public int Timezone;
        public string OsuHash;
        public string MACAdapters;
        public string MACAdaptersHash;
        public string UniqueId;
        public string UniqueId2;
        public bool BlockNonFriendPM;
    }
    public class Login
    {
        public static async Task Handle(MemoryStream body)
        {
            var LoginData = ProcessLoginData(body);
            SerializationWriter sw = new SerializationWriter(Global.Response.OutputStream);

            var UserId = UserHelper.GetId(LoginData.Username);
            var DbPassword = Global.DBContext.DBUsers.Where(x => x.Id == UserId).Select(x => x.Password).FirstOrDefault();

            /*if (UserId == 0 || !UserHelper.ValidatePassword(LoginData.Password, DbPassword)) // wrong password or account doesn't exist.
            {
                Console.WriteLine("wrong password");
                Console.WriteLine(UserId);
                new BanchoPacket(PacketType.ServerLoginReply, new BanchoInt(-1)).WriteToStream(sw);
                sw.Close();
                return;
            }*/

            var player = new Player(UserId, LoginData.Timezone);

            Global.Response.AddHeader("cho-token", player.Token);

            player.PacketEnqueue(new BanchoPacket(PacketType.ServerLoginReply, new BanchoInt(player.Id)));
            player.PacketEnqueue(new BanchoPacket(PacketType.ServerLockClient, new BanchoInt(0)));
            player.PacketEnqueue(new BanchoPacket(PacketType.ServerBanchoVersion, new BanchoInt(19)));
            player.PacketEnqueue(new BanchoPacket(PacketType.ServerUserPermissions, new BanchoInt((int)player.Permissions)));
            player.PacketEnqueue(new BanchoPacket(PacketType.ServerFriendsList, new BanchoIntList(new List<int> { player.Id })));
            player.PacketEnqueue(new BanchoPacket(PacketType.ServerUserPresence, player.ToUserPresence()));
            player.PacketEnqueue(new BanchoPacket(PacketType.ServerUserData, player.ToUserData()));

            player.PacketEnqueue(new BanchoPacket(PacketType.ServerChatChannelListingComplete, new BanchoInt(0)));
            player.PacketEnqueue(new BanchoPacket(PacketType.ServerChatChannelJoinSuccess, new BanchoString("#osu")));
            player.PacketEnqueue(new BanchoPacket(PacketType.ServerChatChannelAvailable, new BanchoChatChannel("#osu", "Basic Channel", 1)));

            player.PacketEnqueue(new BanchoPacket(PacketType.ServerNotification, new BanchoString("Hello, " + player.Username + "\nIt's oyasumi! Testing server.")));
            player.PacketEnqueue(new BanchoPacket(PacketType.ServerUserPresenceSingle, new BanchoInt(player.Id)));
            player.PacketEnqueue(new BanchoPacket(PacketType.ServerUserPresenceBundle, new BanchoIntList(Players.PlayerList.Select(x => x.Id))));

            foreach (var p in Players.PlayerList)
                player.PacketEnqueue(new BanchoPacket(PacketType.ServerUserPresence, p.ToUserPresence()));

            await player.SendPackets();

        }

        public static LoginData ProcessLoginData(MemoryStream body)
        {
            using var reader = new StreamReader(body, leaveOpen: true);
            var loginData = new LoginData();
            loginData.Username = reader.ReadLine();
            loginData.Password = reader.ReadLine();

            string[] detectionData = reader.ReadLine().Split("|");

            loginData.OsuVersion = detectionData[0];
            loginData.Timezone = int.Parse(detectionData[1]);
            loginData.BlockNonFriendPM = (detectionData[4] == "1");

            string[] anticheatData = detectionData[3].Split(":");

            loginData.OsuHash = anticheatData[0];
            loginData.MACAdapters = anticheatData[1];
            loginData.MACAdaptersHash = anticheatData[2];
            loginData.UniqueId = anticheatData[3];
            loginData.UniqueId2 = anticheatData[4];


            return loginData;
        }
    }
}
