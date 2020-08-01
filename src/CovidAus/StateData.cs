using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CovidAus
{
	public class StateData
	{
		[JsonPropertyName("displayName")]
		public string DisplayName { get; set; }

		[JsonPropertyName("data")]
		public List<DailyStateData> Data { get; set; } = new List<DailyStateData>();
	}
}
