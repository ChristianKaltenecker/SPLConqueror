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
        /// Returns the number of configurations where exactly the features from the term are disabled and enabled.
        /// </summary>
        /// <returns>The number of configurations where the features from the term are disabled and enabled.</returns>
        /// <param name="termToCount">The term to search for.</param>
		public double CountTermEnabledDisabled (BinaryOption [] termToCount)
		{
            Dictionary<BinaryOption, List<Configuration>> selected = new Dictionary<BinaryOption, List<Configuration>>();

			List<Configuration> toSearch = new List<Configuration> ();

            List<BinaryOption> nonAlternative = new List<BinaryOption>();

			// Filter the base feature
			if (termToCount.Length == 0) {
				return 1;
			}

            // Find out the non-alternative features to exclude them
            foreach (BinaryOption opt in termToCount) {
                if (!this.alternatives.ContainsKey(opt)) {
                    nonAlternative.Add(opt);
                } 
            }

            foreach (BinaryOption alternativeGroup in this.alternatives.Keys) {
                selected[alternativeGroup] = new List<Configuration>();
            }

            for (int i = 0; i < SamplingSet.Count; i++) {
                Configuration config = SamplingSet[i];
                List<BinaryOption> allSelectedBinOpts = config.getBinaryOptions(BinaryOption.BinaryValue.Selected);
                bool allIncluded = true;
                // First, look if all non-alternatives are included
                foreach (BinaryOption opt in nonAlternative) {
                    if (!allSelectedBinOpts.Contains(opt)) {
                        allIncluded = false;
                        break;
                    }
                }

				if (!allIncluded) {
					toSearch.Add (config);
					continue;
				}

                // Look if one alternative is included from every alternative group
                foreach (BinaryOption opt in this.alternatives.Keys) {
                    bool oneChildIncluded = false;
                    BinaryOption foundWith = null;
                    foreach (BinaryOption child in this.alternatives[opt])
                    {
                        if (allSelectedBinOpts.Contains(child))
                        {
                            foundWith = child;
                            oneChildIncluded = true;
                            break;
                        }
                    }

                    if (!oneChildIncluded) {
                        // This shouldn't happen in usual case studies
                        Console.Error.WriteLine("Configuration with no alternative option detected.");
                        allIncluded = false;
                    } else {
                        List<BinaryOption> configWithOtherOptionEnabled = new List<BinaryOption>(allSelectedBinOpts);
                        configWithOtherOptionEnabled = configWithOtherOptionEnabled.Except(nonAlternative).ToList();
                        configWithOtherOptionEnabled.Remove(foundWith);
                        foreach (BinaryOption otherChild in this.alternatives[opt]) {
                            if (otherChild != foundWith) {
                                List<BinaryOption> tmp = new List<BinaryOption>(configWithOtherOptionEnabled);

                                tmp.Add(otherChild);
                                Configuration newConfig = new Configuration(tmp, new Dictionary<NumericOption, double>());
                                if (!selected[opt].Contains(newConfig)) {
                                    selected[opt].Add(newConfig);
                                }
                            }
                        }   
                    }
                    
                }

				if (!allIncluded || nonAlternative.Count == 0) {
                    toSearch.Add(config);
                }
            }

            // Now count them (only once per alternative)
            int count = 0;

            foreach (BinaryOption opt in selected.Keys)
            {
                List<Configuration> toIgnore = new List<Configuration>();

                foreach (Configuration config in selected[opt])
                {
                    if (toIgnore.Contains(config)) {
                        continue;
                    }

                    if (toSearch.Contains(config))
                    {
                        count++;
                        // If counted, add similar configurations to ignorelist
                        List<BinaryOption> binOpts = config.getBinaryOptions(BinaryOption.BinaryValue.Selected);
						List<BinaryOption> alternatives = new List<BinaryOption>(this.alternatives[opt]);
                        BinaryOption selectedAlternative = alternatives.Where((BinaryOption arg) => binOpts.Contains(arg)).First();
						alternatives.Remove (selectedAlternative);

                        binOpts.Remove(selectedAlternative);

                        foreach (BinaryOption otherAlternative in alternatives) {
                            List<BinaryOption> tmp = new List<BinaryOption>(binOpts);
                            tmp.Add(otherAlternative);
                            toIgnore.Add(new Configuration(tmp, new Dictionary<NumericOption, double>()));
                        }
                    }
                }

            }
			return count / SamplingSet.Count;
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
