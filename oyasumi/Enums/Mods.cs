using System;

namespace oyasumi.Enums
{
    [Flags]
    public enum Mods
    {
        None = 0,
        NoFail = 1 << 0,
        Easy = 1 << 1,
        TouchDevice = 1 << 2,
        Hidden = 1 << 3,
        HardRock = 1 << 4,
        SuddenDeath = 1 << 5,
        DoubleTime = 1 << 6,
        Relax = 1 << 7,
        HalfTime = 1 << 8,
        Nightcore = 1 << 9,
        Flashlight = 1 << 10,
        Autoplay = 1 << 11,
        SpunOut = 1 << 12,
        Relax2 = 1 << 13,
        Perfect = 1 << 14,
        Key4 = 1 << 15,
        Key5 = 1 << 16,
        Key6 = 1 << 17,
        Key7 = 1 << 18,
        Key8 = 1 << 19,
        FadeIn = 1 << 20,
        Random = 1 << 21,
        Cinema = 1 << 22,
        Target = 1 << 23,
        Key9 = 1 << 24,
        KeyCoop = 1 << 25,
        Key1 = 1 << 26,
        Key3 = 1 << 27,
        Key2 = 1 << 28,
        LastMod = 1 << 29,
        KeyMod = Key1 | Key2 | Key3 | Key4 | Key5 | Key6 | Key7 | Key8 | Key9 | KeyCoop,

        FreeModAllowed = Hidden | HardRock | DoubleTime | Flashlight | FadeIn | Easy | Relax | Relax2 | SpunOut |
                         NoFail | Easy | HalfTime | Autoplay | KeyMod,

        ScoreIncreaseMods = Hidden | HardRock | DoubleTime | Flashlight | FadeIn | Easy | Relax | Relax2 | SpunOut |
                            NoFail | Easy | HalfTime | Autoplay | SuddenDeath | Perfect | KeyMod | Target | Random |
                            Nightcore | LastMod
    }
}