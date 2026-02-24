using System.Text.Json;

namespace YardOps.Models.ViewModels.Activities
{
    /// <summary>
    /// ViewModel for displaying activity logs in the admin activity log page.
    /// </summary>
    public class ActivityLogViewModel
    {
        public int Id { get; set; }
        public string UserFullName { get; set; } = "";
        public string UserEmail { get; set; } = "";
        public string CreatedOn { get; set; } = "";
        public DateTime RawCreatedOn { get; set; }
        public string Action { get; set; } = "";
        public string? Description { get; set; }
        public string? JsonData { get; set; }

        /// <summary>
        /// Returns initials for avatar display (e.g., "JD" for John Doe)
        /// </summary>
        public string Initials
        {
            get
            {
                var parts = UserFullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    return $"{parts[0][0]}{parts[1][0]}".ToUpper();
                else if (parts.Length == 1 && parts[0].Length > 0)
                    return parts[0][0].ToString().ToUpper();
                return "?";
            }
        }

        /// <summary>
        /// Returns a formatted, human-readable version of JsonData.
        /// </summary>
        public string FormattedJsonData
        {
            get
            {
                if (string.IsNullOrEmpty(JsonData))
                    return "N/A";
                try
                {
                    using var doc = JsonDocument.Parse(JsonData);
                    var root = doc.RootElement;
                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        var parts = new List<string>();
                        foreach (var prop in root.EnumerateObject())
                        {
                            if (prop.Name == "UserId" || prop.Name == "CreatedBy") continue;
                            parts.Add($"{prop.Name}: {prop.Value}");
                        }
                        return parts.Count > 0 ? string.Join(", ", parts) : "N/A";
                    }
                    return JsonData;
                }
                catch
                {
                    return JsonData;
                }
            }
        }
    }
}