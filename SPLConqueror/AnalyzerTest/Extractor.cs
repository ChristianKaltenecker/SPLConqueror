using System;
using System.IO;
using System.Collections.Generic;
using SPLConqueror_Core;
using System.Linq;

namespace AnalyzerTest
{
    public static class Extractor
    {
#region Constants
		private const string FEATURE_MODEL = "FeatureModel.xml";
		private const string ALL_CONFIGURATIONS = "allConfigurations.csv";
		private const string ALL_LOG = "out_all.log";

		private const string ANALYZE_LEARNING = "command: analyze-learning";

		private const string OUT_PREFIX = "out";
		private const string LOG_SUFIX = ".log";

		private const string SAMPLED_CONFIGURATIONS_PREFIX = "sampledConfigurations";
		public const string CSV_SUFIX = ".csv";
#endregion

		public static Dictionary<string, CaseStudy> ExtractWpInformation (string pathToWpDirectory, List<string> caseStudies)
		{
			string [] directories = FilterSubDirectories(pathToWpDirectory, caseStudies);
			Dictionary<string, CaseStudy> caseStudyResults = new Dictionary<string, CaseStudy> ();

			foreach (string directory in directories) {            
				// Load the vm
				VariabilityModel vm = VariabilityModel.loadFromXML (Path.Combine(directory, FEATURE_MODEL));

				GlobalState.varModel = vm;
				List<Configuration> configurations = ConfigurationReader.readConfigurations(Path.Combine(directory, ALL_CONFIGURATIONS), vm);

				Tuple<string [], double> model = FindBestModel (Path.Combine (directory, ALL_LOG));

				caseStudyResults.Add (Path.GetFileName(directory), new CaseStudy (vm, new SamplingResults(configurations, model)));
			}

			return caseStudyResults;
		}

		public static Dictionary<string, CaseStudy> ExtractRunInformation (string pathToRunDirectory, List<string> caseStudies, 
		                                                                   Dictionary<string, string> strategies, List<string> sizes,
		                                                                   Dictionary<string, CaseStudy> caseStudyResults)
		{
			string [] directories = FilterSubDirectories (pathToRunDirectory, caseStudies);

			foreach (string directory in directories) {
				string caseStudyName = Path.GetFileName (directory);
				CaseStudy caseStudy = caseStudyResults [caseStudyName];
				string [] runs = Directory.GetDirectories (directory);

				foreach (string run in runs) {

					string runNumber = Path.GetFileName (run).Split('_')[1];
					List<string> sizesInDir = Directory.GetFiles(run).Where((string arg) => Path.GetFileName (arg).StartsWith(SAMPLED_CONFIGURATIONS_PREFIX) 
					                                                   && Path.GetFileName (arg).Split('_')[1].Equals(strategies.Keys.ElementAt(0)))
					                              .Select((string arg) => Path.GetFileName(arg).Split('_')[2].Split('.')[0])
					                              .ToList();
					foreach (string size in sizesInDir.Where((string arg) => sizes.Contains(arg))) {
						foreach (string strategy in strategies.Keys) {
							VariabilityModel variabilityModel = caseStudy.VariabilityModel;

							GlobalState.varModel = variabilityModel;
							List<Configuration> configurations = ConfigurationReader.readConfigurations (Path.Combine(run, SAMPLED_CONFIGURATIONS_PREFIX + "_" + strategy + "_" + size + CSV_SUFIX), variabilityModel);

							Tuple<string [], double> model = FindBestModel (Path.Combine (run, OUT_PREFIX + "_" + strategy + "_" + size + LOG_SUFIX));

							SamplingResults results = new SamplingResults (configurations, model);
							caseStudy.AddSamplingStrategy (size, strategy, runNumber, results);
						}
					}
				}
			}

			return caseStudyResults;
		}

		private static string [] FilterSubDirectories (string directory, List<string> allowed, bool filterRuns = false)
		{
			List<string> subdirectories;
			if (filterRuns) {
				subdirectories = Directory.GetDirectories(directory).Where((string arg) => 
				                                                           allowed.Contains(arg.Split('_')[1]) && 
				                                                           (arg.Contains(SAMPLED_CONFIGURATIONS_PREFIX) || 
				                                                            arg.Contains(OUT_PREFIX)))
				                          .ToList();
			} else {
				subdirectories = Directory.GetDirectories (directory).Where ((string arg) => allowed.Contains (
								Path.GetFileName (arg))).ToList ();
			}
			return subdirectories.ToArray();
		}

		private static Tuple<string[], double> FindBestModel (string filePath)
		{
			string [] lines = File.ReadAllLines (filePath);

			string[] bestModel = new string[0];
			double error = Double.PositiveInfinity;

            for (int i = lines.Length - 1; i > 0; i--) {
				if (lines [i].Contains (ANALYZE_LEARNING)) {
					return new Tuple<string[], double>(bestModel, error);
				}

                if (lines [i].Contains (";")) {
					string [] split = lines [i].Split (';');
					double modelError = Double.Parse (split [split.Length - 1]);

					if (modelError < error) {
						error = modelError;
						bestModel = split [1].Split('+');
					}
                }
            }

			return new Tuple<string [], double> (bestModel, error);
		}
    }
}
