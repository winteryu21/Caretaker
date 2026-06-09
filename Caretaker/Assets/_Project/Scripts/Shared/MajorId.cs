namespace Caretaker.Shared
{
    /// <summary>
    /// Major Interaction progress identifiers used by GameFlow.
    /// </summary>
    /// <remarks>DSD §2.3, §3.1, §3.9 — Major Interaction progress.</remarks>
    public enum MajorId
    {
        None = 0,
        M1 = 1,
        M2 = 2,
        M3 = 3,
        M4 = 4
    }

    /// <summary>
    /// Converts stable string IDs and causality rule IDs into Major Interaction IDs.
    /// </summary>
    public static class MajorIdUtility
    {
        /// <summary>
        /// Parses a stable Major ID such as "M1".
        /// </summary>
        /// <param name="value">Stable Major ID.</param>
        /// <param name="majorId">Parsed Major ID, or <see cref="MajorId.None"/>.</param>
        /// <returns>True when the value is a known Major ID.</returns>
        public static bool TryParse(string value, out MajorId majorId)
        {
            majorId = value?.Trim() switch
            {
                "M1" => MajorId.M1,
                "M2" => MajorId.M2,
                "M3" => MajorId.M3,
                "M4" => MajorId.M4,
                _ => MajorId.None
            };

            return majorId != MajorId.None;
        }

        /// <summary>
        /// Resolves a causality rule ID or stable Major ID to a Major ID.
        /// </summary>
        /// <param name="ruleOrMajorId">Causal rule ID or stable Major ID.</param>
        /// <param name="majorId">Resolved Major ID, or <see cref="MajorId.None"/>.</param>
        /// <returns>True when the value maps to a known Major ID.</returns>
        public static bool TryResolve(string ruleOrMajorId, out MajorId majorId)
        {
            if (TryParse(ruleOrMajorId, out majorId))
            {
                return true;
            }

            majorId = ruleOrMajorId?.Trim() switch
            {
                "CR_P1_POWER_PANEL" => MajorId.M1,
                "CR_P1_SEC_HACK" => MajorId.M2,
                "CR_P2_BLUEPRINT_ID" => MajorId.M3,
                "CR_P2_VIRUS_PLANT" => MajorId.M4,
                _ => MajorId.None
            };

            return majorId != MajorId.None;
        }

        /// <summary>
        /// Returns the stable string ID used in data assets and network payloads.
        /// </summary>
        /// <param name="majorId">Major ID.</param>
        /// <returns>Stable string ID, or an empty string for <see cref="MajorId.None"/>.</returns>
        public static string ToStableId(this MajorId majorId)
        {
            return majorId switch
            {
                MajorId.M1 => "M1",
                MajorId.M2 => "M2",
                MajorId.M3 => "M3",
                MajorId.M4 => "M4",
                _ => string.Empty
            };
        }
    }
}
