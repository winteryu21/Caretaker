namespace Caretaker.Shared
{
    /// <summary>
    /// Final game flow result resolved by the Host.
    /// </summary>
    /// <remarks>DSD §2.3, §3.9 — Phase 3 result flow.</remarks>
    public enum GameResult
    {
        None = 0,
        EscapeSuccess = 1,
        EscapeFail = 2
    }

    /// <summary>
    /// Provides stable string conversion for result UI and network diagnostics.
    /// </summary>
    public static class GameResultUtility
    {
        /// <summary>
        /// Returns the stable string ID used by result UI lookup.
        /// </summary>
        /// <param name="gameResult">Game result.</param>
        /// <returns>Stable string ID, or an empty string for <see cref="GameResult.None"/>.</returns>
        public static string ToStableId(this GameResult gameResult)
        {
            return gameResult == GameResult.None ? string.Empty : gameResult.ToString();
        }
    }
}
