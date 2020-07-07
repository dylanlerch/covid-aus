using System.Collections.Generic;

namespace CovidAus.Report
{
	public class DailyReport
	{
		public Dictionary<string, DailyReportStateData> Data { get; set; } = new Dictionary<string, DailyReportStateData>();
	}
}
