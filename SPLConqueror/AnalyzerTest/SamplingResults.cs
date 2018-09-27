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
        private Dictionary<BinaryOption [], LowerUpperBound> processedModel = new Dictionary<BinaryOption [], LowerUpperBound> ();
		private double influenceSum = 0;
		private double baseInfluence;
        public Dictionary<BinaryOption, List<BinaryOption>> alternatives = new Dictionary<BinaryOption, List<BinaryOption>>();


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
        public Dictionary<BinaryOption[], LowerUpperBound> RetrieveInfluences (Dictionary<string, BinaryOption []> termPool)
		{
            Dictionary<BinaryOption [], LowerUpperBound> influences = new Dictionary<BinaryOption [], LowerUpperBound> ();

			foreach (string term in termPool.Keys) {
				if (term.Equals (Analyzer.BASE)) {
                    LowerUpperBound lup = new LowerUpperBound();
                    lup.AddElement(baseInfluence);
                    lup.DivideBy(influenceSum);
					influences.Add (termPool [term], lup);
				} else if (processedModel.ContainsKey (termPool [term])) {
                    processedModel[termPool[term]].DivideBy(influenceSum);
					influences.Add (termPool [term], processedModel [termPool [term]]);
				} else {
                    LowerUpperBound lup = new LowerUpperBound();
                    lup.AddElement(0);
					influences.Add (termPool [term], lup);
				}
			}

			return influences;
		}

        /// <summary>
        /// Returns the number of configurations where exactly the features from the term are enabled.
        /// </summary>
        /// <returns>The number of configurations where the features from the term are enabled.</returns>
        /// <param name="termToCount">The term to search for.</param>
		public double CountTermEnabled (BinaryOption [] termToCount)
		{
			// Filter the base feature
			if (termToCount.Length == 0) {
				return 1;
			}

            // Count it
            int count = 0;

            foreach (Configuration config in SamplingSet)
            {
                List<BinaryOption> binOpts = config.getBinaryOptions(BinaryOption.BinaryValue.Selected);
                foreach (BinaryOption termOption in termToCount)
                {
                    if (binOpts.Contains(termOption))
                    {
                        count++;
                    }
                }
            }
            
			return count * 1.0d / SamplingSet.Count;
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
					BinaryOption opt = variabilityModel.getBinaryOption (split [i].Trim ());

                    // Add alternatives
                    if (opt.hasAlternatives()) {
                        if (!this.alternatives.ContainsKey((BinaryOption) opt.Parent)) {
                            this.alternatives[(BinaryOption) opt.Parent] = new List<BinaryOption>();
							this.alternatives [(BinaryOption)opt.Parent].Add (opt);
							foreach (ConfigurationOption configOption in opt.collectAlternativeOptions()) {
								this.alternatives [(BinaryOption)opt.Parent].Add ((BinaryOption) configOption);
							}                     
                        }
                        
                        optionStrings.Add("Group_" + opt.Parent.Name);
                        options.Add((BinaryOption) opt.Parent);
                    } else {
                        optionStrings.Add(split[i].Trim());
                        options.Add(opt);
                    }
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

                    if (processedModel.ContainsKey(allOptsOfTerm)) {
                        processedModel[allOptsOfTerm].AddElement(influence);
                    } else {
                        LowerUpperBound lub = new LowerUpperBound();
                        lub.AddElement(influence);
                        processedModel.Add(allOptsOfTerm, lub);    
                    }
				}            
			}
		}
    }
}
