using System.Collections.Generic;

namespace CovidAus
{
	public class RunningTotals
	{
		private Dictionary<string, Cases> currentTotals { get; set; } = new Dictionary<string, Cases>();

		// Returns the difference between the existing total and the new total
		// (which is the count of new cases). Sets the new total as the current
		// total. This method needs to be called in date order or the new cases
		// will not be counted correctly.
		public Cases GetNewCases(string state, Cases newTotal)
		{
			state = state.ToLowerInvariant();
			
			if (currentTotals.ContainsKey(state))
			{
				// If there are already some cases for this state, calculate
				// the difference betwen the current total and this new total
				// to get the count of new cases.
				var current = currentTotals[state];
				currentTotals[state] = newTotal;
				return current.Difference(newTotal);
			}
			else
			{
				// If there is no record for the current state, the number of
				// new cases is going to be equal to the total (because this
				// is the state's first time reporting).
				currentTotals[state] = newTotal;
				return newTotal;
			}
		}
	}
}
