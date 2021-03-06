﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using CovidAus.Report;
using CsvHelper;

namespace CovidAus
{
	class Program
	{
		private const string WorkFolder = "covid-temp";
		private const string CovidDataUri = "https://github.com/CSSEGISandData/COVID-19/archive/master.zip";
		private const string DailyReportsPath = "COVID-19-master/csse_covid_19_data/csse_covid_19_daily_reports";

		static async Task Main(string[] args)
		{
			Program p = new Program();
			await p.Start();
		}

		private async Task Start()
		{
			var path = await DownloadCovidData();
			var reports = ProcessReports(path);
			var data = FormatAndCalculateData(reports);
			WriteData(data);
		}

		public async Task<string> DownloadCovidData()
		{
			// The names of all the folders we'll use
			Console.WriteLine("Preparing folders");
			var zipPath = Path.Combine(WorkFolder, "covid.zip");
			var tempExtractDirectory = Path.Combine(WorkFolder, "temp");
			var dataFolder = Path.Combine(WorkFolder, "data");

			// Clean everything up before starting, want to avoid any stray data
			if (Directory.Exists(WorkFolder)) Directory.Delete(WorkFolder, true);
			Directory.CreateDirectory(WorkFolder);
			Directory.CreateDirectory(tempExtractDirectory);
			Directory.CreateDirectory(dataFolder);

			// Download and extract the latest data
			using (var client = new WebClient())
			{
				Console.WriteLine("Downloading data");
				await client.DownloadFileTaskAsync(new Uri(CovidDataUri), zipPath);
			}

			Console.WriteLine("Extracting data");
			ZipFile.ExtractToDirectory(zipPath, tempExtractDirectory, true);
			File.Delete(zipPath);

			// There is a lot of other data in the repo, so pull the daily
			// reports out and put them in a top level folder, then delete
			// everything else.
			Console.WriteLine("Copying daily reports to data folder");
			var reportsDirectoryPath = Path.Combine(tempExtractDirectory, DailyReportsPath);
			DirectoryInfo reportsDirectory = new DirectoryInfo(reportsDirectoryPath);

			foreach (var file in reportsDirectory.GetFiles())
			{
				if (file.Extension.Equals(".csv", StringComparison.InvariantCultureIgnoreCase))
				{
					var newPath = Path.Combine(dataFolder, file.Name);
					file.CopyTo(newPath);
				}
			}

			Console.WriteLine("Removing temporary download folder");
			Directory.Delete(tempExtractDirectory, true);

			return dataFolder;
		}
		public class Constants
		{
			public class Headers
			{
				public static readonly string[] Country = new string[] { "Country/Region", "Country_Region" };
				public static readonly string[] State = new string[] { "Province/State", "Province_State" };
				public static readonly string[] LastUpdate = new string[] { "Last Update", "Last_Update" };
				public static readonly string[] Confirmed = new string[] { "Confirmed" };
				public static readonly string[] Deaths = new string[] { "Deaths" };
				public static readonly string[] Recovered = new string[] { "Recovered" };
			}
		}
		public SortedList<DateTimeOffset, DailyReport> ProcessReports(string reportsPath)
		{
			Console.WriteLine("Processing reports");
			var reports = new SortedList<DateTimeOffset, DailyReport>();
			var reportsDirectory = new DirectoryInfo(reportsPath);

			foreach (var file in reportsDirectory.GetFiles())
			{
				// Get the name of the file without the extension. The file name
				// is the report date, we'll use this for sorting.
				var fileName = file.Name.Substring(0, file.Name.Length - file.Extension.Length);
				var reportDate = DateTimeOffset.ParseExact(fileName, "MM-dd-yyyy", null, DateTimeStyles.AssumeUniversal);
				var dailyReport = new DailyReport();

				using (var reader = new StreamReader(file.OpenRead()))
				using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
				{
					csv.Configuration.PrepareHeaderForMatch = (header, index) => header.ToLower();

					csv.Read();
					csv.ReadHeader();

					while (csv.Read())
					{
						var country = GetField<string>(csv, Constants.Headers.Country).ToLowerInvariant();
						var state = GetField<string>(csv, Constants.Headers.State).ToLowerInvariant();

						// Only want to extract the Australian values
						if (country is object && state is object && country.Equals("australia", StringComparison.InvariantCultureIgnoreCase))
						{
							var stateData = new DailyReportStateData
							{
								Country = country,
								State = state,
								FormattedStateName = GetField<string>(csv, Constants.Headers.State),
								LastUpdate = GetField<string>(csv, Constants.Headers.LastUpdate),
								Confirmed = GetField<int>(csv, Constants.Headers.Confirmed),
								Deaths = GetField<int>(csv, Constants.Headers.Deaths),
								Recovered = GetField<int>(csv, Constants.Headers.Recovered)
							};

							dailyReport.Data.Add(state, stateData);
						}
					}
				}

				reports.Add(reportDate, dailyReport);
			}

			return reports;
		}

		// Since the data source started, there have been new columns added and
		// changes to the names of specific columns. This will go through a list
		// of potential column names and return the first one that it finds.
		public T GetField<T>(CsvReader reader, ReadOnlySpan<string> headers)
		{
			foreach (var header in headers)
			{
				if (reader.TryGetField<T>(header, out var value))
				{
					return value;
				}
			}

			// If no value is found for any of the headers, return the default value
			return default(T);
		}

		public Data FormatAndCalculateData(SortedList<DateTimeOffset, DailyReport> reports)
		{
			var runningTotals = new RunningTotals();
			var stateSet = new HashSet<string>();
			var data = new Data();

			// Prepare the dataset and target container for processing.
			//
			// Reports don't have data for each state for each day (as some states only
			// started reporting cases later in the outbreak). Need to do a first pass
			// of the data to find all of the states available for Australia.
			foreach (var day in reports)
			{
				// Keep a record of all of the dates, this will likely be the
				// x-axis labels wherever it is graphed.
				data.Dates.Add(day.Key);
				
				foreach (var state in day.Value.Data)
				{
					var stateName = state.Key;
					if (stateSet.Add(stateName))
					{
						// Add a new dataset container for this state, we'll add
						// data to it in the next pass.
						var newStateData = new StateData() {
							DisplayName = state.Value.FormattedStateName
						};

						data.States.Add(stateName, newStateData);
					}
				}
			}

			// There is a report every day, so we're just starting from the 
			// first day of data and working our way forward. These days need to
			// be in order or the running totals will not work.
			foreach (var reportDay in reports)
			{
				var reportDayDate = reportDay.Key;
				var reportDayStates = reportDay.Value;

				foreach (var stateName in stateSet)
				{
					var state = data.States[stateName];

					// If there is no data for the state on this day, it will be
					// recorded as all zeros. This will make sure we have a full
					// set of data for all states for all days. If there is data
					// for this day, we'll overwrite this value below.
					var totalCasesForState = new Cases();

					if (reportDayStates.Data.ContainsKey(stateName))
					{
						var reportDayStateData = reportDayStates.Data[stateName];
						totalCasesForState = new Cases(
							reportDayStateData.Confirmed,
							reportDayStateData.Deaths,
							reportDayStateData.Recovered
						);
					}
					
					var newCasesForState = runningTotals.GetNewCases(stateName, totalCasesForState);

					var dailyStateData = new DailyStateData(reportDayDate, totalCasesForState, newCasesForState);
					state.Data.Add(dailyStateData);
				}
			}

			return data;
		}

		public void WriteData(Data data)
		{
			Console.WriteLine("Saving data to disk");
			var options = new JsonSerializerOptions();
			options.WriteIndented = true;
			options.Converters.Add(new DateOnlyConverter());

			var dataPath = Path.Combine(WorkFolder, "data.json");
			var dataString = JsonSerializer.Serialize(data, options);

			File.WriteAllText(dataPath, dataString);
			Console.WriteLine($"Done saving data to disk at path: '{dataPath}'");
		}
	}
}
