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
        public string Timestamp { get; set; } = "";
        public DateTime RawTimestamp { get; set; }
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
        /// Converts JSON to "Key: Value, Key2: Value2" format.
        /// Fixes Unicode escaping like \u0027 → actual characters.
        /// Returns "N/A" if JsonData is null or empty.
        /// Skips "UserId" key if present (redundant).
        /// </summary>
        public string FormattedJsonData
        {
            get
            {
                if (string.IsNullOrEmpty(JsonData))
                    return "N/A";
                try
                {
                    // Parse the JSON to get key-value pairs
                    using var doc = JsonDocument.Parse(JsonData);
                    var root = doc.RootElement;
                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        var parts = new List<string>();
                        foreach (var prop in root.EnumerateObject())
                        {
                            if (prop.Name.Equals("UserId", StringComparison.OrdinalIgnoreCase))
                                continue;  // Skip UserId key
                            var value = FormatJsonValue(prop.Value);
                            parts.Add($"{prop.Name}: {value}");
                        }
                        // Join all key-value pairs with comma separator
                        return string.Join(", ", parts);
                    }
                    // If not an object, return the raw value (unescaped)
                    return UnescapeUnicode(JsonData);
                }
                catch
                {
                    // If parsing fails, return raw data with Unicode unescaped
                    return UnescapeUnicode(JsonData);
                }
            }
        }
        /// <summary>
        /// Formats a JSON element value to a readable string.
        /// Handles arrays, objects, and primitives.
        /// </summary>
        private static string FormatJsonValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => UnescapeUnicode(element.GetString() ?? ""),
                JsonValueKind.Number => element.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => "null",
                JsonValueKind.Array => FormatJsonArray(element),
                JsonValueKind.Object => FormatNestedObject(element),
                _ => element.GetRawText()
            };
        }
        /// <summary>
        /// Formats a JSON array to a readable string like "[item1, item2]"
        /// </summary>
        private static string FormatJsonArray(JsonElement array)
        {
            var items = new List<string>();
            foreach (var item in array.EnumerateArray())
            {
                items.Add(FormatJsonValue(item));
            }
            return $"[{string.Join(", ", items)}]";
        }
        /// <summary>
        /// Formats a nested JSON object to a readable string like "{Key: Value}"
        /// </summary>
        private static string FormatNestedObject(JsonElement obj)
        {
            var parts = new List<string>();
            foreach (var prop in obj.EnumerateObject())
            {
                parts.Add($"{prop.Name}: {FormatJsonValue(prop.Value)}");
            }
            return $"{{{string.Join(", ", parts)}}}";
        }
        /// <summary>
        /// Unescapes Unicode sequences like \u0027 to actual characters (e.g., ')
        /// </summary>
        private static string UnescapeUnicode(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            // Use Regex to replace \uXXXX patterns with actual characters
            return System.Text.RegularExpressions.Regex.Replace(
                input,
                @"\\u([0-9A-Fa-f]{4})",
                m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString()
            );
        }
    }
}