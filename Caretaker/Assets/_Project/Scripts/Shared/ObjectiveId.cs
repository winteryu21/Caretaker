namespace Caretaker.Shared
{
    /// <summary>
    /// HUD-facing objective identifiers resolved by GameFlow.
    /// </summary>
    /// <remarks>DSD §2.3, §3.9, §3.10 — phase progress and HUD objectives.</remarks>
    public enum ObjectiveId
    {
        None = 0,

        // Phase 1
        P1_Past_ExploreLobby = 100,
        P1_Future_FindPowerProblem = 101,
        P1_Past_GuideFutureToPowerRoom = 102,
        P1_Future_ReachPowerRoom = 103,
        P1_Past_RestoreFuturePower = 104,
        P1_Future_RestorePower = 105,
        P1_Past_GoToSecurityRoom = 106,
        P1_Future_AvoidSecurityAndGoUp = 107,
        P1_Past_GetPersonnelRecord = 108,
        P1_Future_FindSecurityWeakness = 109,

        // Phase 2
        P2_Past_GetKeyCard = 200,
        P2_Future_ClearShieldBarrier = 201,
        P2_Past_ReachArchive = 202,
        P2_Future_ReachLaboratory = 203,
        P2_Past_IdentifyBlueprint = 204,
        P2_Future_RunBlueprintExperiment = 205,
        P2_Past_HelpFutureRecoverSample = 206,
        P2_Future_RecoverSample = 207,

        // Phase 3
        P3_Past_Escape = 300,
        P3_Future_Escape = 301,
        P3_Past_ReachExit = 302,
        P3_Future_ReachExit = 303,

        // Result
        Result_EscapeSuccess = 900,
        Result_EscapeFail = 901
    }

    /// <summary>
    /// Provides stable string conversion for objective IDs used by data assets and UI lookup.
    /// </summary>
    public static class ObjectiveIdUtility
    {
        /// <summary>
        /// Returns the stable string ID used in data assets and UI lookup.
        /// </summary>
        /// <param name="objectiveId">Objective ID.</param>
        /// <returns>Stable string ID, or an empty string for <see cref="ObjectiveId.None"/>.</returns>
        public static string ToStableId(this ObjectiveId objectiveId)
        {
            return objectiveId == ObjectiveId.None ? string.Empty : objectiveId.ToString();
        }
    }
}
