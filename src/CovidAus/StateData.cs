using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CovidAus
{
	public class StateData
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }
		
		[JsonPropertyName("data")]
		public List<DailyStateData> DailyData { get; private set; } = new List<DailyStateData>();

		public StateData(string name)
		{
			Name = name;
		}
	}
}
