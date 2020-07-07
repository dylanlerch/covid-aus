using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CovidAus
{
	public class Data
	{
		[JsonPropertyName("states")]
		public Dictionary<string, StateData> States { get; private set; } = new Dictionary<string, StateData>();

		public void AddDataForState(string state, DailyStateData data)
		{
			var formattedStateName = state;
			state = state.ToLowerInvariant();
			if (!States.ContainsKey(state)) States[state] = new StateData(formattedStateName);

			var stateData = States[state];
			stateData.DailyData.Add(data);
		}
	}
}
