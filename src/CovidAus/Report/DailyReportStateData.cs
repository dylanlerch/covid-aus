namespace CovidAus.Report
{
	public class DailyReportStateData
	{
		public string Country { get; set; }
		public string State { get; set; }
		public string LastUpdate { get; set; }
		public int Confirmed { get; set; }
		public int Deaths { get; set; }
		public int Recovered { get; set; }
	}
}
