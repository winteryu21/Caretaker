namespace Caretaker.Core
{
    public enum NetworkSessionStatus : byte
    {
        Offline = 0,
        WaitingForPlayers = 1,
        BothConnected = 2,
        BothReady = 3,
        GameStarting = 4,
        InGame = 5,
        ShuttingDown = 6
    }
}
