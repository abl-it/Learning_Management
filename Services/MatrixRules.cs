namespace Training.Services
{
    /// <summary>
    /// Aturan kecil yang dipakai bersama oleh service Training Matrix / Change Request:
    /// parsing target, normalisasi requirement, dan pencocokan role terhadap dbo.Workflow.Allowed.
    /// </summary>
    internal static class MatrixRules
    {
        internal const string EditorRole = "tc";
        internal const string CreatorToken = "creator";
        internal const int MaxTargets = 300;
        internal const int MaxCourseIds = 500;

        internal static readonly string[] WorkflowActions =
            { "Request for Approve", "Approve", "Reject" };

        internal static bool IsEditor(IEnumerable<string>? roles) =>
            roles is not null
            && roles.Any(r => string.Equals(r, EditorRole, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Role pengguna yang berhak atas nilai dbo.Workflow.Allowed ('creator' dipetakan ke tc).
        /// </summary>
        internal static string? FindMatchingRole(IEnumerable<string>? roles, string? allowed)
        {
            if (roles is null || string.IsNullOrWhiteSpace(allowed))
            {
                return null;
            }

            var target = string.Equals(allowed, CreatorToken, StringComparison.OrdinalIgnoreCase)
                ? EditorRole
                : allowed.Trim();

            return roles.FirstOrDefault(
                r => string.Equals(r, target, StringComparison.OrdinalIgnoreCase));
        }

        internal static string NormalizeRequirement(string? requirement)
        {
            if (string.Equals(requirement?.Trim(), "Mandatory", StringComparison.OrdinalIgnoreCase))
            {
                return "Mandatory";
            }

            if (string.Equals(requirement?.Trim(), "Recommended", StringComparison.OrdinalIgnoreCase))
            {
                return "Recommended";
            }

            throw new ArgumentException("Requirement must be Mandatory or Recommended.");
        }

        internal static List<int> ValidateIds(IReadOnlyCollection<int>? ids)
        {
            var list = (ids ?? Array.Empty<int>()).Distinct().ToList();

            if (list.Count == 0)
            {
                throw new ArgumentException("Select at least one row.");
            }

            if (list.Any(i => i <= 0))
            {
                throw new ArgumentException("Invalid row identifier.");
            }

            return list;
        }

        internal static List<(int SubDepartmentId, int Layer)> ParseTargets(IEnumerable<string>? raw)
        {
            var result = new List<(int, int)>();

            foreach (var item in (raw ?? Enumerable.Empty<string>()).Distinct())
            {
                var parts = item?.Split(':');

                if (parts is not { Length: 2 }
                    || !int.TryParse(parts[0], out var sub) || sub <= 0
                    || !int.TryParse(parts[1], out var layer) || layer is < 1 or > 3)
                {
                    throw new ArgumentException(
                        $"Invalid target '{item}'. Use SubDepartmentId:Layer (Layer 1-3).");
                }

                result.Add((sub, layer));
            }

            if (result.Count == 0)
            {
                throw new ArgumentException("Select at least one Sub Department and Layer.");
            }

            if (result.Count > MaxTargets)
            {
                throw new ArgumentException($"Too many targets selected (max {MaxTargets}).");
            }

            return result.Distinct().ToList();
        }
    }
}
