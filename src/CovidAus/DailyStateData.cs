using System;
using System.Text.Json.Serialization;

namespace CovidAus
{
	public class DailyStateData
	{
		[JsonPropertyName("date")]
		public DateTimeOffset Date { get; private set; }
		
		[JsonPropertyName("total")]
		public Cases Total { get; private set; }
		
		[JsonPropertyName("new")]
		public Cases New { get; private set; }

		public DailyStateData(DateTimeOffset date, Cases totalCases, Cases newCases)
		{
			Date = date;
			Total = totalCases;
			New = newCases;
		}
	}
}
