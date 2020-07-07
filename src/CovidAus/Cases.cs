using System.Text.Json.Serialization;

namespace CovidAus
{
	public class Cases
	{
		[JsonPropertyName("confirmed")]
		public int Confirmed { get; private set; }
		
		[JsonPropertyName("deaths")]
		public int Deaths { get; private set; }
		
		[JsonPropertyName("recovered")]
		public int Recovered { get; private set; }

		public Cases(int confirmed, int deaths, int recovered)
		{
			Confirmed = confirmed;
			Deaths = deaths;
			Recovered = recovered;
		}

		public Cases Difference(Cases updated)
		{
			return new Cases(
				updated.Confirmed - Confirmed,
				updated.Deaths - Deaths,
				updated.Recovered - Recovered
			);
		}
	}
}
