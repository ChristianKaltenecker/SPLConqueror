using System;
using System.Collections.Generic;
using SPLConqueror_Core;

namespace AnalyzerTest
{
    public class CaseStudy
    {
		public VariabilityModel VariabilityModel { get; private set; }
		public SamplingResults AllConfigurations { get; private set; }
		public Dictionary<string, Dictionary<string, SamplingResults>> BestSampleInfo { get; private set; }
		public Dictionary<string, Dictionary<string, string>> BestRun { get; private set; }

		public Dictionary<string, Dictionary<string, Dictionary<string, SamplingResults>>> SampleInfo { get; private set; }
		private Dictionary<string, Dictionary<string, double>> OverallError;
		private Dictionary<string, Dictionary<string, int>> ErrorCount;
		
		public CaseStudy (VariabilityModel variabilityModel, SamplingResults allConfigurations)
        {
			this.VariabilityModel = variabilityModel;

			SampleInfo = new Dictionary<string, Dictionary<string, Dictionary<string, SamplingResults>>> ();
			BestSampleInfo = new Dictionary<string, Dictionary<string, SamplingResults>> ();
			BestRun = new Dictionary<string, Dictionary<string, string>> ();
			this.OverallError = new Dictionary<string, Dictionary<string, double>> ();
			this.ErrorCount = new Dictionary<string, Dictionary<string, int>> ();

			this.AllConfigurations = allConfigurations;
        }

		public double GetOverallError (string size, string strategy)
		{
			return this.OverallError [size] [strategy] / this.ErrorCount [size] [strategy];
		}

		public void AddSamplingStrategy (string size, string strategy, string run, SamplingResults results)
		{
			// Add the results to the overall error
			if (!this.OverallError.ContainsKey (size)) {
				this.OverallError.Add (size, new Dictionary<string, double> ());
				this.ErrorCount.Add (size, new Dictionary<string, int> ());
			}

			if (!this.OverallError [size].ContainsKey (strategy)) {
				this.OverallError [size].Add (strategy, 0.0);
				this.ErrorCount [size].Add (strategy, 0);
			}

			this.OverallError [size] [strategy] += results.ModelError;
			this.ErrorCount [size] [strategy] += 1;


            // Fill the sample info for all runs
			if (!this.SampleInfo.ContainsKey (size)) {
				this.SampleInfo.Add (size, new Dictionary<string, Dictionary<string, SamplingResults>> ());
			}

			if (!this.SampleInfo [size].ContainsKey (strategy)) {
				this.SampleInfo [size].Add (strategy, new Dictionary<string, SamplingResults> ());
			}

			this.SampleInfo [size][strategy].Add (run, results);

			// Add the best sample if it the current one is better than the previous
			if (!this.BestSampleInfo.ContainsKey (size)) {
				this.BestSampleInfo.Add (size, new Dictionary<string, SamplingResults> ());
				this.BestRun.Add (size, new Dictionary<string, string> ());
			}

			if (!this.BestSampleInfo [size].ContainsKey (strategy)) {
				this.BestSampleInfo [size].Add (strategy, results);
				this.BestRun [size].Add(strategy, run);
			} else if (this.BestSampleInfo [size] [strategy].ModelError > results.ModelError) {
				this.BestSampleInfo [size] [strategy] = results;
				this.BestRun [size] [strategy] = run;
			}

            
		}
    }
}
