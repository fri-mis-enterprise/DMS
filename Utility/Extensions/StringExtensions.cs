namespace Document_Management.Utility.Extensions
{
    public static class StringExtensions
    {
        public static string RemoveCommas(this string? value)
        {
            return value?.Replace(",", "").Trim() ?? string.Empty;
        }
    }
}
