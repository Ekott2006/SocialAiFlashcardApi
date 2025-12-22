using System.Text;

namespace Core.Helper;

public class OpaqueCursorHelper
{
    private const char Delimiter = '|';

    /// <summary>
    /// Encodes the sort key and ID into an opaque Base64 string.
    /// </summary>
    public static string EncodeCursor<TKey>(TKey sortKey, Guid id)
    {
        if (sortKey == null) return string.Empty;

        // Format: "SortKeyValue|Guid"
        string plainText = $"{sortKey}{Delimiter}{id}";
        byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        
        return Convert.ToBase64String(plainTextBytes);
    }

    /// <summary>
    /// Decodes an opaque Base64 string back into its component parts.
    /// </summary>
    public static bool TryDecodeCursor<TKey>(string? cursor, out TKey? sortKey, out Guid id)
    {
        sortKey = default;
        id = default;

        if (string.IsNullOrWhiteSpace(cursor)) return false;

        try
        {
            byte[] base64EncodedBytes = Convert.FromBase64String(cursor);
            string plainText = Encoding.UTF8.GetString(base64EncodedBytes);
            string[] parts = plainText.Split(Delimiter);

            if (parts.Length != 2) return false;

            // 1. Convert the Sort Key (e.g., DateTime, int, etc.)
            sortKey = (TKey)Convert.ChangeType(parts[0], typeof(TKey));

            // 2. Convert the Unique ID
            if (!Guid.TryParse(parts[1], out id)) return false;

            return true;
        }
        catch
        {
            return false; // Invalid cursor format
        }
    }

}