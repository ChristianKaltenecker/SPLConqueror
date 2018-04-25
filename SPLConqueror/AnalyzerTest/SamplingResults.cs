using System;
using System.Linq;
using System.Collections.Generic;
using SPLConqueror_Core;

namespace AnalyzerTest
{
    public class SamplingResults
    {
		public List<Configuration> SamplingSet { get; private set; }
		public Tuple<string [], double> Model { get; private set; }
		public double ModelError { get; private set; }
		private Dictionary<BinaryOption [], double> processedModel = new Dictionary<BinaryOption [], double> ();
		private double influenceSum = 0;
		private double baseInfluence;


		public SamplingResults (List<Configuration> configurations, Tuple<string [], double> model)
		{
			this.SamplingSet = configurations;
			this.Model = model;
			this.ModelError = Model.Item2;
		}


        /// <summary>
        /// Fills the term pool by adding the terms that are currently not included.
        /// </summary>
        /// <returns>The filled term pool.</returns>
        /// <param name="termPool">The term pool to fill.</param>
        /// <param name="variabilityModel">The variability model to retrieve the options from.</param>
		public Dictionary<string, BinaryOption []> FillTermPool (Dictionary<string, BinaryOption []> termPool, VariabilityModel variabilityModel)
		{
			ProcessModel (termPool, variabilityModel);
            
			return termPool;
		}

        /// <summary>
        /// Retrieves the influences of the terms.
        /// </summary>
        /// <returns>The influences of the terms.</returns>
        /// <param name="termPool">The term pool to fetch the right objects.</param>
		public Dictionary<BinaryOption[], double> RetrieveInfluences (Dictionary<string, BinaryOption []> termPool)
		{
			Dictionary<BinaryOption [], double> influences = new Dictionary<BinaryOption [], double> ();

			foreach (string term in termPool.Keys) {
				if (term.Equals (Analyzer.BASE)) {
					influences.Add (termPool [term], Math.Abs(baseInfluence / influenceSum));
				} else if (processedModel.ContainsKey (termPool [term])) {
					influences.Add (termPool [term], Math.Abs (processedModel [termPool [term]] / influenceSum));
				} else {
					influences.Add (termPool [term], 0);
				}
			}

			return influences;
		}

        /// <summary>
        /// Returns the number of configurations where exactly the features from the term are disabled and enabled.
        /// </summary>
        /// <returns>The number of configurations where the features from the term are disabled and enabled.</returns>
        /// <param name="termToCount">The term to search for.</param>
		public int CountTermEnabledDisabled (BinaryOption [] termToCount)
		{
			List<Configuration> selected = new List<Configuration> ();
			List<Configuration> toSearch = new List<Configuration> ();
			int count = 0;

			foreach (Configuration config in SamplingSet) {
				bool allEnabled = true;

				// TODO: Does this still work?
				foreach (BinaryOption binOpt in termToCount) {
					if (!config.getBinaryOptions (BinaryOption.BinaryValue.Selected).Contains (binOpt)) {
						allEnabled = false;
					}
				}

				if (allEnabled) {
					Dictionary<BinaryOption, BinaryOption.BinaryValue> binOpts = config
						.getBinaryOptions (BinaryOption.BinaryValue.Selected).Except (termToCount)
						.ToDictionary ((BinaryOption arg) => arg, (BinaryOption arg) => BinaryOption.BinaryValue.Selected);
					selected.Add (new Configuration (binOpts, new Dictionary<NumericOption, double> ()));
				} else {
					toSearch.Add (config);
				}
			}

			foreach (Configuration config in selected) {
				if (toSearch.Contains (config)) {
					toSearch.Remove (config);
					count++;
				}
			}

			return count;
		}

		private void ProcessModel (Dictionary<string, BinaryOption []> termPool, VariabilityModel variabilityModel)
		{
			List<Tuple<BinaryOption [], double>> terms = new List<Tuple<BinaryOption [], double>> ();

			for (int termNumber = 0; termNumber < Model.Item1.Length; termNumber++) {
				string term = Model.Item1 [termNumber];
				string [] split = term.Split ('*');
				double influence = Double.Parse (split [0]);
				this.influenceSum += Math.Abs(influence);
				List<BinaryOption> options = new List<BinaryOption> ();
				List<string> optionStrings = new List<string> ();

				for (int i = 1; i < split.Length; i++) {
					optionStrings.Add (split [i].Trim ());
					BinaryOption opt = variabilityModel.getBinaryOption (split [i].Trim ());
					options.Add (opt);
				}

				if (termNumber == 0) {
					this.baseInfluence = influence;
					if (!termPool.ContainsKey (Analyzer.BASE)) {
						termPool.Add (Analyzer.BASE, new BinaryOption [0]);
					}
				} else {


					optionStrings.Sort ();
					string optionsAsString = optionStrings.Aggregate ((string arg1, string arg2) => arg1 + "*" + arg2);
					BinaryOption [] allOptsOfTerm;
					if (termPool.ContainsKey (optionsAsString)) {
						allOptsOfTerm = termPool [optionsAsString];
					} else {
						allOptsOfTerm = options.ToArray ();
						termPool.Add (optionsAsString, allOptsOfTerm);
					}

					terms.Add (new Tuple<BinaryOption [], double> (allOptsOfTerm, influence));
					processedModel.Add (allOptsOfTerm, influence);
				}            
			}
		}
    }
}
