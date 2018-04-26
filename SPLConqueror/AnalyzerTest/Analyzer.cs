using System.Collections.Generic;
using SPLConqueror_Core;
using System;
using System.Linq;
using System.IO;
using System.Text;
using ProcessWrapper;

namespace AnalyzerTest
{
    /// <summary>
    /// This class contains methods to analyze performance models and samples.
    /// </summary>
    public static class Analyzer
    {
		#region Constants
		private const String CSV_ELEMENT_SEPARATOR = ";";
        private const String CSV_ROW_SEPARATOR = "\n";
        private const String INFLUENCE = "_inf";
        private const String FREQUENCY = "_freq";

		public const string BASE = "base";
		#endregion

		public static string AnalyzeModels (Dictionary<string, CaseStudy> caseStudyResults, Dictionary<string, string> strategies, string size, string path = null, bool onlyBestModel = true, bool onlyWP=false)
		{
			if (path == null) {
				path = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
				Directory.CreateDirectory (path);
			}

			foreach (string caseStudyName in caseStudyResults.Keys) {
				CaseStudy caseStudy = caseStudyResults [caseStudyName];

				// Stage 1: Search for the terms that were included by machine learning
				Dictionary<string, BinaryOption []> termPool = new Dictionary<string, BinaryOption []> ();

				// For the whole population ...
				termPool = caseStudy.AllConfigurations.FillTermPool (termPool, caseStudy.VariabilityModel);


				// ... and for the sampling strategies inclusive the best runs
				if (!onlyWP) {
					if (onlyBestModel) {
						foreach (string strategy in strategies.Keys) {
							SamplingResults info = caseStudy.BestSampleInfo [size] [strategy];

							termPool = info.FillTermPool (termPool, caseStudy.VariabilityModel);
						}
					} else {
						foreach (string strategy in strategies.Keys) {
							foreach (string run in caseStudy.SampleInfo [size] [strategy].Keys) {
								SamplingResults info = caseStudy.SampleInfo [size] [strategy] [run];

								termPool = info.FillTermPool (termPool, caseStudy.VariabilityModel);
							}
                        }
					}
				}

				// Stage 2: Retrieve the influences of all terms
                Dictionary<BinaryOption[], LowerUpperBound> wpInfluences = caseStudy.AllConfigurations.RetrieveInfluences (termPool);

                Dictionary<string, Dictionary<string, Dictionary<BinaryOption [], LowerUpperBound>>> strategyInfluences = new Dictionary<string, Dictionary<string, Dictionary<BinaryOption [], LowerUpperBound>>> ();

				if (onlyBestModel) {
					foreach (string strategy in strategies.Keys) {
						SamplingResults info = caseStudy.BestSampleInfo [size] [strategy];
                        strategyInfluences [strategy] = new Dictionary<string, Dictionary<BinaryOption [], LowerUpperBound>> ();                  
						strategyInfluences [strategy] ["best"] = info.RetrieveInfluences (termPool);
					}
				} else {
					foreach (string strategy in strategies.Keys) {
                        strategyInfluences [strategy] = new Dictionary<string, Dictionary<BinaryOption [], LowerUpperBound>> ();
						foreach (string run in caseStudy.SampleInfo [size] [strategy].Keys) {
							SamplingResults info = caseStudy.SampleInfo [size] [strategy] [run];                     
							strategyInfluences [strategy] [run] = info.RetrieveInfluences (termPool);
						}
                    }
				}

				// Stage 3: Count the times where only the configuration options from the terms are enabled and disabled
				Dictionary<BinaryOption[], int> wpCount = new Dictionary<BinaryOption[], int>();
				Dictionary<string, Dictionary<string, Dictionary<BinaryOption [], int>>> strategyCount = new Dictionary<string, Dictionary<string, Dictionary<BinaryOption [], int>>> ();
				foreach (string term in termPool.Keys) {
					wpCount [termPool [term]] = caseStudy.AllConfigurations.CountTermEnabledDisabled (termPool [term]);

					if (!onlyWP) {
						if (onlyBestModel) {
							foreach (string strategy in strategies.Keys) {
								if (!strategyCount.ContainsKey (strategy)) {
									strategyCount [strategy] = new Dictionary<string, Dictionary<BinaryOption [], int>> ();
								}
								if (!strategyCount [strategy].ContainsKey ("best")) {
									strategyCount [strategy] ["best"] = new Dictionary<BinaryOption [], int> ();
								}

								strategyCount [strategy] ["best"] [termPool [term]] = caseStudy.BestSampleInfo [size] [strategy].CountTermEnabledDisabled (termPool [term]);
							}
						} else {
							foreach (string strategy in strategies.Keys) {
								foreach (string run in caseStudy.SampleInfo [size] [strategy].Keys) {
									if (!strategyCount.ContainsKey (strategy)) {
										strategyCount [strategy] = new Dictionary<string, Dictionary<BinaryOption [], int>> ();
									}
									if (!strategyCount [strategy].ContainsKey (run)) {
                                        strategyCount [strategy] [run] = new Dictionary<BinaryOption [], int> ();
                                    }

									strategyCount [strategy] [run] [termPool [term]] = caseStudy.BestSampleInfo [size] [strategy].CountTermEnabledDisabled (termPool [term]);
								}
							}
						}
					}
				}


				// Stage 4: Print it out

				// Create the order in which the terms should be printed
				List<BinaryOption []> binOptionOrder = new List<BinaryOption []> ();
				var optionOrder = wpInfluences.OrderBy (x => x.Key.Length).ThenBy (x => x.Value).ToList();
                foreach (KeyValuePair<BinaryOption [], LowerUpperBound> option in optionOrder) {
					binOptionOrder.Add (option.Key);
				}

				string fileName = caseStudyName + "_" + size + "_";
				if (onlyBestModel) {
					fileName += "best";
				} else {
					fileName += "all";
				}

				// At first the influence results
				string strategyNameMapping = CreateNameMapping(caseStudy, strategies, size);
				writeToFile (Path.Combine (path, fileName + "_mapping" + Extractor.CSV_SUFIX), strategyNameMapping);
				string content = InfluenceResultsToString (binOptionOrder, caseStudy, size, termPool, strategies, wpInfluences, strategyInfluences);
				writeToFile (Path.Combine (path, fileName + INFLUENCE + Extractor.CSV_SUFIX), content);

				// And the count results afterwards
				content = CountResultsToString (binOptionOrder, caseStudy, size, termPool, strategies, wpCount, strategyCount);
				writeToFile (Path.Combine (path, fileName + FREQUENCY + Extractor.CSV_SUFIX), content);
			}

            // Return the path to the directory including the results
			return path;
		}

		private static string CreateNameMapping (CaseStudy caseStudy, Dictionary<string, string> strategies, string size)
		{
			StringBuilder stringBuilder = new StringBuilder ();

			// Create the header for the .csv-file
			stringBuilder.Append ("ID");
			stringBuilder.Append (CSV_ELEMENT_SEPARATOR);
			stringBuilder.Append("Name");
			stringBuilder.Append (CSV_ROW_SEPARATOR);

			// Add whole population
			stringBuilder.Append (1);
			stringBuilder.Append (CSV_ELEMENT_SEPARATOR);
			stringBuilder.Append ("Whole Population");
			stringBuilder.Append (" (");
			stringBuilder.Append ((int)Math.Round (caseStudy.AllConfigurations.ModelError));
			stringBuilder.Append ("%)");
			stringBuilder.Append (CSV_ROW_SEPARATOR);

			int i = 2;

			foreach (string strategy in strategies.Keys) {
				stringBuilder.Append (i);
				stringBuilder.Append (CSV_ELEMENT_SEPARATOR);
				stringBuilder.Append (strategies[strategy]);
				stringBuilder.Append (" (");
				stringBuilder.Append ((int)Math.Round (caseStudy.GetOverallError (size, strategy)));
				stringBuilder.Append ("%)");
				stringBuilder.Append (CSV_ROW_SEPARATOR);
				i++;
			}
            // Remove last row separator
			stringBuilder.Remove (stringBuilder.Length - 1, 1);

			return stringBuilder.ToString ();
		}

		private static string InfluenceResultsToString (List<BinaryOption []> optionOrder, CaseStudy caseStudy, string size,
		                                                Dictionary<string, BinaryOption[]> termPool,
                                                        Dictionary<string, string> strategies,
                                                        Dictionary<BinaryOption[], LowerUpperBound> wpInfluences, 
                                                        Dictionary<string, Dictionary<string, Dictionary<BinaryOption [], LowerUpperBound>>> strategyInfluences)
		{
			StringBuilder stringBuilder = new StringBuilder ();
			stringBuilder.Append(CreateHeader (caseStudy, strategies, size));


			foreach (BinaryOption [] option in optionOrder) {
				// Find the string representation
				string term = termPool.FirstOrDefault (x => x.Value.Equals (option)).Key;         
                stringBuilder.Append(WriteBound(term + CSV_ELEMENT_SEPARATOR + 1 + CSV_ELEMENT_SEPARATOR,
                                                wpInfluences[option],
                                                CSV_ROW_SEPARATOR));

				int idCounter = 2;
				foreach (string strategy in strategies.Keys) {
					foreach (string run in (strategyInfluences [strategy].Keys)) {
                        stringBuilder.Append(WriteBound(term + CSV_ELEMENT_SEPARATOR + idCounter + CSV_ELEMENT_SEPARATOR,
                                                        strategyInfluences[strategy] [run] [option],
                                                        CSV_ROW_SEPARATOR));
					}
					idCounter++;
				}            
			}

            // Remove the last row separator
			stringBuilder.Remove (stringBuilder.Length - 1, 1);


			return stringBuilder.ToString();
		}

        private static string WriteBound(string prefix, LowerUpperBound lup, string sufix) {
            StringBuilder sb = new StringBuilder();
                sb.Append(prefix);
                sb.Append(lup.LowerBound);
                sb.Append(sufix);
            if (lup.LowerBound != lup.UpperBound) {
                sb.Append(prefix);
                sb.Append(lup.UpperBound);
                sb.Append(sufix);
            }

            return sb.ToString();
        }

		private static string CountResultsToString (List<BinaryOption []> optionOrder, CaseStudy caseStudy, string size, 
					                                Dictionary<string, BinaryOption []> termPool,
                                                    Dictionary<string, string> strategies,
    											    Dictionary<BinaryOption [], int> wpCount,
    											    Dictionary<string, Dictionary<string, Dictionary<BinaryOption [], int>>> strategyCount)
		{
			StringBuilder stringBuilder = new StringBuilder ();
            stringBuilder.Append (CreateHeader (caseStudy, strategies, size));

			foreach (BinaryOption [] option in optionOrder) {
                // Find the string representation
                string term = termPool.FirstOrDefault (x => x.Value.Equals (option)).Key;

                stringBuilder.Append (term);
                stringBuilder.Append (CSV_ELEMENT_SEPARATOR);
                stringBuilder.Append (1);
                stringBuilder.Append (CSV_ELEMENT_SEPARATOR);
				stringBuilder.Append (wpCount [option]);
				stringBuilder.Append (CSV_ROW_SEPARATOR);

                int idCounter = 2;
                foreach (string strategy in strategies.Keys) {
					foreach (string run in (strategyCount [strategy].Keys)) {
                        stringBuilder.Append (term);
                        stringBuilder.Append (CSV_ELEMENT_SEPARATOR);
                        stringBuilder.Append (idCounter);
                        stringBuilder.Append (CSV_ELEMENT_SEPARATOR);
						stringBuilder.Append (strategyCount [strategy] [run] [option]);
                        stringBuilder.Append (CSV_ROW_SEPARATOR);
                    }
					idCounter++;
                }
            }

            // Remove the last row separator
            stringBuilder.Remove (stringBuilder.Length - 1, 1);


            return stringBuilder.ToString ();
		}

		private static string CreateHeader (CaseStudy caseStudy, Dictionary<string, string> strategies, string size)
		{
			StringBuilder result = new StringBuilder ();
			result.Append ("Term");
			result.Append (CSV_ELEMENT_SEPARATOR);
            result.Append ("Strategy");
			result.Append (CSV_ELEMENT_SEPARATOR);
			result.Append ("Result");
			result.Append (CSV_ROW_SEPARATOR);


			return result.ToString ();
		}
        

		/// <summary>
		/// Analyzes the given models by comparing the introduced terms.
		/// </summary>
		/// <returns>The overall error to the ground-truth (i.e., the whole population).</returns>
		/// <param name="learnedModels">The learned models, where the first model contains the whole population analysis.</param>
		public static void analyzeModels (VariabilityModel vm, Tuple<String, String, String>[] learnedModels, String filePath, String outputPath)
        {
            // Read in measurements and log files
            List<Configuration>[] allMeasurements = new List<Configuration>[learnedModels.Length];
            Tuple<string[][], double>[] logInfo = new Tuple<string[][], double>[learnedModels.Length];
            for (int i = 0; i < learnedModels.Length; i++)
            {
                allMeasurements[i] = ConfigurationReader.readConfigurations(learnedModels[i].Item2, vm);
                logInfo[i] = ExtractTerms(getLastModelLine(learnedModels[i].Item3));
            }
            

            // Search for the terms that were included by machine learning
            List<BinaryOption []> identifiedTerms = new List<BinaryOption []> ();
            Dictionary<String, BinaryOption []> termPool = new Dictionary<string, BinaryOption []> ();
            Dictionary<BinaryOption [], double> [] influence = new Dictionary<BinaryOption [], double> [learnedModels.Length];
            for (int i = 0; i < learnedModels.Length; i++) {
                influence [i] = new Dictionary<BinaryOption [], double> ();
            }

            for (int i = 0; i < learnedModels.Length; i++) {
                
                foreach (string[] model in logInfo[i].Item1) { 
                    List<BinaryOption> options = new List<BinaryOption> ();
                    
                    for (int j = 1; j < model.Length; j++)
                    {
                        options.Add(new BinaryOption(vm, model[j]));
                    }

                    options.Sort ();
                    BinaryOption [] optionsArray = options.ToArray ();

                    if (!termPool.Keys.Contains (OptionsToString(optionsArray))) {
                        identifiedTerms.Add (optionsArray);
                        termPool.Add (OptionsToString (optionsArray), optionsArray);
                        for (int j = 0; j < learnedModels.Length; j++) {
                            influence [j].Add (optionsArray, 0);
                        }
                    }
                }
            }

            List<BinaryOption []> allTerms = deriveCombinations (2, vm);


            // Get influences of all learned models
            for (int i = 0; i < learnedModels.Length; i++) {
                string[][] wpModel = logInfo[0].Item1;
                double influenceSum = 0.0;
                foreach (string[] term in logInfo[i].Item1) {
                    List<BinaryOption> binOpts = new List<BinaryOption> ();
                    for (int j = 1; j < term.Length; j++)
                    {
                        binOpts.Add(new BinaryOption(vm, term[j]));
                    }
                    binOpts.Sort ();
                    if (!termPool.Keys.Contains (OptionsToString (binOpts.ToArray ()))) {
                        continue;
                    }
                    BinaryOption [] binArray = termPool[OptionsToString (binOpts.ToArray())];
                    influence [i] [binArray] = Math.Abs (Double.Parse(term[0]));
                    influenceSum += Math.Abs(Double.Parse(term[0]));
                }

                // Compute the relative influence
                for (int j = 0; j < influence [i].Keys.Count; j++) {
                    //foreach (BinaryOption [] options in influence [i].Keys) {
                    BinaryOption [] options = influence [i].Keys.ElementAt (j);
                    influence [i] [options] /= influenceSum;
                }
                // Sort the dictionary
                influence [i] = influence [i].OrderBy (x => x.Key.Length).ThenBy (x => x.Value).ToDictionary (x => x.Key, x => x.Value);

            }

            var optionOrder = influence [0].OrderBy (x => x.Key.Length).ThenBy (x => x.Value).ToList();

            List<BinaryOption []> binaryOptionOrder = new List<BinaryOption []> ();

            for (int i = 0; i < optionOrder.Count; i++) {
                binaryOptionOrder.Add(optionOrder [i].Key);
            }



                // Count only the necessary configuration options and interactions
                Dictionary<BinaryOption [], double> [] sampleSetCounts = new Dictionary<BinaryOption [], double> [learnedModels.Length];
            for (int i = 0; i < learnedModels.Length; i++) {
                sampleSetCounts [i] = new Dictionary<BinaryOption [], double> ();

                //foreach (string[] term in logInfo[i].Item1) {
                //    List<BinaryOption> options = new List<BinaryOption> ();
                //    for (int j = 1; j < term.Length; j++)
                //    {
                //        options.Add(new BinaryOption(vm, term[j]));
                //    }
                //    options.Sort ();
                //    BinaryOption [] optionsArray = options.ToArray ();
                //}

                foreach (Configuration config in allMeasurements[i]) {
                    countIfContained (config, identifiedTerms, sampleSetCounts [i]);
                }
                divideDictionaryValuesBy (sampleSetCounts [i], allMeasurements[i].Count);
            }

            string[] names = new string[learnedModels.Length];
            for (int i = 0; i < learnedModels.Length; i++)
            {
                names[i] = learnedModels[i].Item1 + " (" + (int)Math.Round(logInfo[i].Item2) + "%)";
            }

            // Write the results in a file
            writeToFile (filePath, convertToString (sampleSetCounts, influence, names, vm, binaryOptionOrder));

            // Call Python
            PythonWrapper pyWrapper = new PythonWrapper(AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + PythonWrapper.PLOTTER_SCRIPT, 
                new string[0], filePath + " " + outputPath);
            pyWrapper.waitForNextReceivedLine();
            pyWrapper.endProcess();
        }

        private static Tuple<string[][], double> ExtractTerms(string line)
        {
            string[] elements = line.Split(';');
            string[] terms = elements[1].Split('+');
            string[][] allTerms = new string[terms.Length][];
            for (int i = 0; i < allTerms.Length; i++)
            {
                allTerms[i] = terms[i].Split('*');
            }

            double errorRate = Double.Parse(elements[2]);
            return new Tuple<string[][], double>(allTerms, errorRate);
        }

        private static string OptionsToString (BinaryOption [] options)
        {
            StringBuilder result = new StringBuilder ();
            for (int i = 0; i < options.Length; i++) {
                result.Append (options [i].Name);
                result.Append (";");
            }
            return result.ToString ();
        }

        private static string getLastModelLine(String file)
        {
            string[] lines = File.ReadAllLines(file);

            for (int i = lines.Length - 1; i > 0; i--)
            {
                if (lines[i].Contains(";"))
                {
                    return lines[i];
                }
            }

            return "";
        }

        private static string convertToString (Dictionary<BinaryOption [], double> [] counts, Dictionary<BinaryOption [], double> [] influences, String [] names, VariabilityModel vm, List<BinaryOption[]> optionOrder)
        {
            StringBuilder header = new StringBuilder ();
            header.Append ("Term");
            header.Append(CSV_ELEMENT_SEPARATOR);


            for (int i = 0; i < names.Length; i++) {
                header.Append (INFLUENCE);
                header.Append ("-");
                header.Append (names [i]);
                header.Append (CSV_ELEMENT_SEPARATOR);


            }

            for (int i = 0; i < names.Length; i++) {
                header.Append (FREQUENCY);
                header.Append ("-");
                header.Append (names [i]);
                header.Append (CSV_ELEMENT_SEPARATOR);
            }

            header.Remove (header.Length - CSV_ELEMENT_SEPARATOR.Length, CSV_ELEMENT_SEPARATOR.Length);

            StringBuilder results = new StringBuilder ();
            foreach (BinaryOption [] options in optionOrder) {
                // Firstly insert the term
                results.Append (convertTerm (options));
                results.Append (CSV_ELEMENT_SEPARATOR);

                // Insert the influence scores
                for (int i = 0; i < names.Length; i++) {
                    if (!influences [i].ContainsKey (options)) {
                        results.Append (0);
                    } else {
                        results.Append (influences [i] [options]);
                    }
                    results.Append (CSV_ELEMENT_SEPARATOR);
                }

                // Insert the influence scores
                for (int i = 0; i < names.Length; i++) {
                    if (!counts [i].ContainsKey (options)) {
                        results.Append (0);
                    } else {
                        results.Append (counts [i] [options]);
                    }
                    results.Append (CSV_ELEMENT_SEPARATOR);
                }
                results.Remove (results.Length - CSV_ELEMENT_SEPARATOR.Length, CSV_ELEMENT_SEPARATOR.Length);
                results.Append (CSV_ROW_SEPARATOR);
            }

            return header.ToString() + CSV_ROW_SEPARATOR + results.ToString();
        }

        private static string convertTerm (BinaryOption [] options)
        {
            StringBuilder result = new StringBuilder ();
            foreach (BinaryOption option in options) {
                result.Append (option);
                result.Append ("*");
            }

            result.Remove (result.Length - 1, 1);

            return result.ToString ();
        }

        private static void writeToFile (String path, String content)
        {
            FileStream ostrm;
            ostrm = new FileStream (path, FileMode.OpenOrCreate, FileAccess.Write);
            ostrm.SetLength (0);
            StreamWriter writer = new StreamWriter (ostrm);
            writer.WriteLine (content);

            writer.Flush ();
            writer.Close ();
        }

        private static List<BinaryOption []> deriveCombinations (int depth, VariabilityModel vm)
        {
            List<BinaryOption []> result = new List<BinaryOption []> ();
            foreach (List<BinaryOption> options in deriveCombinationsHelper (depth, vm)) {
                options.Sort ();
                BinaryOption [] tmp = options.ToArray ();
                result.Add (tmp);

            }
            return result;
        }

        private static List<List<BinaryOption>> deriveCombinationsHelper (int depth, VariabilityModel vm)
        {
            if (depth == 0) {
                return new List<List<BinaryOption>>();
            } else if (depth == 1) {
                List<List<BinaryOption>> fw = new List<List<BinaryOption>> ();
                foreach (BinaryOption option in vm.BinaryOptions) {
                    List<BinaryOption> tmp = new List<BinaryOption> ();
                    tmp.Add (option);
                    fw.Add (tmp);
                }
                return fw;
            }


            List<List<BinaryOption>> previousResult = deriveCombinationsHelper (depth - 1, vm);

            List<List<BinaryOption>> result = new List<List<BinaryOption>> ();

            foreach (BinaryOption option in vm.BinaryOptions) {
                foreach (List<BinaryOption> options in previousResult) {
                    if (!options.Contains (option)) {
                        List<BinaryOption> tmp = new List<BinaryOption> (options);
                        tmp.Add (option);
                        result.Add (tmp);
                    }
                }
            }

            return result;
        }

        private static void divideDictionaryValuesBy (Dictionary<BinaryOption [], double> dict, int count)
        {
            for (int i = 0; i < dict.Keys.Count; i++) {
                //foreach (BinaryOption [] key in dict.Keys) {
                BinaryOption [] key = dict.Keys.ElementAt (i);
                dict [key] /= count;
            }
        }


        private static void countIfContained (Configuration config, List<BinaryOption []> identifiedTerms, Dictionary<BinaryOption[], double> count)
        {
            foreach (BinaryOption [] options in identifiedTerms) {
                bool contained = true;
                foreach (BinaryOption option in options) {
                    bool found = false;
                    foreach (BinaryOption configOption in config.BinaryOptions.Keys) {
                        if (configOption.Name.Equals (option.Name)) {
                            found = true;
                        }
                    }

                    if (!found) {
                        contained = false;
                    }
                }
                if (contained) {
                    if (count.ContainsKey (options)) {
                        count [options]++;
                    } else {
                        count [options] = 1;
                    }
                }
            }
        }
    }
}
