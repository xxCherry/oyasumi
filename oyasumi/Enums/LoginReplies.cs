namespace oyasumi.Enums
{
    public enum LoginReplies
    {
        RequireVerification = -8,
        PasswordReset = -7,
        TestBuildNoSupporter = -6,
        ServerSideError = -5,
        BannedError = -4,
        BannedError2 = -3, // same as -4
        OldVersion = -2,
        WrongCredentials = -1
    }
}