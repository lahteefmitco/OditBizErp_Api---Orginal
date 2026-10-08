using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MictcoWebService.Common
{
    /// <summary>
    /// Accepts the date strings the Flutter app sends, including an empty string for a missing date.
    /// System.Text.Json otherwise rejects those values before the action runs.
    /// </summary>
    public sealed class FlexibleNullableDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.String)
            {
                string text = reader.GetString();
                if (string.IsNullOrWhiteSpace(text) || text.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
                    return null;
                if (FlexibleDateTimeJson.TryParse(text, out DateTime value))
                    return value;
                throw new JsonException("The value '" + text + "' is not a valid date.");
            }

            throw new JsonException("The JSON value could not be converted to a date.");
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value);
            else
                writer.WriteNullValue();
        }
    }

    public sealed class FlexibleDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string text = reader.GetString();
                if (FlexibleDateTimeJson.TryParse(text, out DateTime value))
                    return value;
                throw new JsonException("The value '" + text + "' is not a valid date.");
            }

            throw new JsonException("The JSON value could not be converted to a date.");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }

    internal static class FlexibleDateTimeJson
    {
        private static readonly string[] Formats =
        {
            "yyyy-MM-dd",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss.fffffff",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss.fff",
            "dd-MM-yyyy",
            "dd/MM/yyyy",
            "d-M-yyyy",
            "d/M/yyyy",
            "dd-MM-yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm:ss",
            "MM/dd/yyyy",
            "MM/dd/yyyy HH:mm:ss"
        };

        public static bool TryParse(string text, out DateTime value)
        {
            text = (text ?? "").Trim();
            if (DateTime.TryParseExact(text, Formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal | DateTimeStyles.AdjustToUniversal,
                    out value))
                return true;

            return DateTime.TryParse(text, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out value);
        }
    }
}
