using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CovidAus
{
	public class Data
	{
		[JsonPropertyName("dates")]
		public List<DateTimeOffset> Dates { get; private set; } = new List<DateTimeOffset>();

		[JsonPropertyName("states")]
		public Dictionary<string, StateData> States { get; private set; } = new Dictionary<string, StateData>();
	}
}
