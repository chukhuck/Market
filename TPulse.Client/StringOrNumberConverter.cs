using System.Text.Json;
using System.Text.Json.Serialization;

namespace TPulse.Client
{
  internal class StringOrNumberConverter : JsonConverter<string?>
  {
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      if (reader.TokenType == JsonTokenType.String)
        return reader.GetString();

      if (reader.TokenType == JsonTokenType.Number)
      {
        // Возвращаем числовой токен как текст (без попытки приведения к long/double)
        return reader.GetInt64().ToString(System.Globalization.CultureInfo.InvariantCulture);
      }

      if (reader.TokenType == JsonTokenType.Null)
        return null;

      // На всякий случай — возвращаем строковое представление текущего токена
      return reader.GetString();
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
      if (value is null)
      {
        writer.WriteNullValue();
        return;
      }

      // Всегда записываем как строку — это безопасно для обратной сериализации
      writer.WriteStringValue(value);
    }
  }
}