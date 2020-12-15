using System;

namespace RippleDatabaseMerger.Enums
{
    [Flags]
    public enum RipplePrivileges
    {
        UserBanned = 0,
        UserPublic = 1,
        UserNormal = 2 << 0,
        UserDonor = 2 << 1,
        AdminAccessRAP = 2 << 2,
        AdminManageUsers = 2 << 3,
        AdminBanUsers = 2 << 4,
        AdminSilenceUsers = 2 << 5,
        AdminWipeUsers = 2 << 6,
        AdminManageBeatmaps = 2 << 7,
        AdminManageServers = 2 << 8,
        AdminManageSettings = 2 << 9,
        AdminManageBetaKeys = 2 << 10,
        AdminManageReports = 2 << 11,
        AdminManageDocs = 2 << 12,
        AdminManageBadges = 2 << 13,
        AdminViewRAPLogs = 2 << 14,
        AdminManagePrivileges = 2 << 15,
        AdminSendAlerts = 2 << 16,
        AdminChatMod = 2 << 17,
        AdminKickUsers = 2 << 18,
        UserPendingVerification = 2 << 19,
        UserTournamentStaff = 2 << 20,
        AdminCaker = 2 << 21,
        AdminViewTopScores = 2 << 22
    }
}