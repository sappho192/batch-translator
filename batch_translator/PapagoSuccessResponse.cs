using System.Text.Json;
using System.Text.Json.Serialization;

#pragma warning disable CS8618
#pragma warning disable CS8603

// Based on auto-generated code by https://app.quicktype.io/
namespace BatchTranslator
{
	public partial class PapagoResponse
	{
		public static PapagoResponse FromJson(string json) => JsonSerializer.Deserialize<PapagoResponse>(json);
	}

	public partial class PapagoResponse
	{
		[JsonPropertyName("message")]
		public Message Message { get; set; }
	}

	public partial class Message
	{
		[JsonPropertyName("result")]
		public Result Result { get; set; }

		[JsonPropertyName("@type")]
		public string Type { get; set; }

		[JsonPropertyName("@service")]
		public string Service { get; set; }

		[JsonPropertyName("@version")]
		public string Version { get; set; }
	}

	public partial class Result
	{
		[JsonPropertyName("srcLangType")]
		public string SrcLangType { get; set; }

		[JsonPropertyName("tarLangType")]
		public string TarLangType { get; set; }

		[JsonPropertyName("translatedText")]
		public string TranslatedText { get; set; }

		[JsonPropertyName("engineType")]
		public string EngineType { get; set; }

		[JsonPropertyName("pivot")]
		public object Pivot { get; set; }

		[JsonPropertyName("dict")]
		public object Dict { get; set; }

		[JsonPropertyName("tarDict")]
		public object TarDict { get; set; }
	}
}
#pragma warning restore CS8603
#pragma warning restore CS8618
