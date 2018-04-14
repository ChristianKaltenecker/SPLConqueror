using System.Collections.Generic;
using SPLConqueror_Core;
using MachineLearning.Learning.Regression;
using System;
using System.Linq;
using System.IO;
using System.Text;
using ProcessWrapper;

namespace MachineLearning.Analysis
{
    /// <summary>
    /// This class contains methods to analyze performance models and samples.
    /// </summary>
    public static class Analyzer
    {

        private static String CSV_ELEMENT_SEPARATOR = ";";
        private static String CSV_ROW_SEPARATOR = "\n";
        private static String INFLUENCE = "Influence";
        private static String FREQUENCY = "Frequency";

        /// <summary>
        /// Analyzes the given models by comparing the introduced terms.
        /// </summary>
        /// <returns>The overall error to the ground-truth (i.e., the whole population).</returns>
        /// <param name="learnedModels">The learned models, where the first model contains the whole population analysis.</param>
        public static void analyzeModels (FeatureSubsetSelection [] learnedModels, String [] names, String filePath, string outputPath)
        {
            // Search for the terms that were included by machine learning
            List<BinaryOption []> identifiedTerms = new List<BinaryOption []> ();
            Dictionary<BinaryOption [], double> [] influence = new Dictionary<BinaryOption [], double> [learnedModels.Length];
            for (int i = 0; i < learnedModels.Length; i++) {
                influence [i] = new Dictionary<BinaryOption [], double> ();
                foreach (LearningRound round in learnedModels [i].LearningHistory) {
                    List<BinaryOption> options = new List<BinaryOption> ();
                    options.AddRange (round.bestCandidate.participatingBoolOptions);
                    options.Sort ();
                    BinaryOption [] optionsArray = options.ToArray ();

                    if (!identifiedTerms.Contains (optionsArray)) {
                        identifiedTerms.Add (optionsArray);
                        for (int j = 0; j < learnedModels.Length; j++) {
                            influence [j].Add (optionsArray, 0);
                        }
                    }
                }
            }

            List<BinaryOption []> allTerms = deriveCombinations (2);


            // Get influences of all learned models
            for (int i = 0; i < learnedModels.Length; i++) {
                LearningRound best = learnedModels [0].LearningHistory [learnedModels [0].LearningHistory.Count - 1];
                double influenceSum = 0.0;
                foreach (Feature term in best.FeatureSet) {
                    List<BinaryOption> binOpts = new List<BinaryOption> ();
                    binOpts.AddRange (term.participatingBoolOptions);
                    binOpts.Sort ();
                    BinaryOption [] binArray = binOpts.ToArray ();
                    influence [i] [binArray] = Math.Abs (term.Constant);
                    influenceSum += Math.Abs (term.Constant);
                }

                // Compute the relative influence
                foreach (BinaryOption [] options in influence [i].Keys) {
                    influence [i] [options] /= influenceSum;
                }
                // Sort the dictionary
                influence [i] = influence [i].OrderBy (x => x.Key.Length).ThenBy (x => x.Value).ToDictionary (x => x.Key, x => x.Value);
            }



            // Count only the necessary configuration options and interactions
            Dictionary<BinaryOption [], double> [] sampleSetCounts = new Dictionary<BinaryOption [], double> [learnedModels.Length];
            Dictionary<BinaryOption [], double> [] sampleSetTermRank = new Dictionary<BinaryOption [], double> [learnedModels.Length];
            for (int i = 0; i < learnedModels.Length; i++) {
                sampleSetCounts [i] = new Dictionary<BinaryOption [], double> ();
                sampleSetTermRank [i] = new Dictionary<BinaryOption [], double> ();

                foreach (LearningRound round in learnedModels [i].LearningHistory) {
                    List<BinaryOption> options = new List<BinaryOption> ();
                    options.AddRange (round.bestCandidate.participatingBoolOptions);
                    options.Sort ();
                    BinaryOption [] optionsArray = options.ToArray ();
                    sampleSetTermRank [i] [optionsArray] = round.round;
                }

                foreach (Configuration config in learnedModels [i].LearningSet) {
                    countIfContained (config, identifiedTerms, sampleSetCounts [i]);
                }
                divideDictionaryValuesBy (sampleSetCounts [i], learnedModels [i].LearningSet.Count);
            }

            // Write the results in a file
            writeToFile (filePath, convertToString (sampleSetCounts, influence, names));

            // Call Python
            PythonWrapper pyWrapper = new PythonWrapper(AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + PythonWrapper.PLOTTER_SCRIPT, 
                new string[0], filePath + " " + outputPath);
            pyWrapper.waitForNextReceivedLine();
            pyWrapper.endProcess();
        }

        private static string convertToString (Dictionary<BinaryOption [], double> [] counts, Dictionary<BinaryOption [], double> [] influences, String [] names)
        {
            StringBuilder header = new StringBuilder ();
            header.Append ("Term");
            header.Append(CSV_ELEMENT_SEPARATOR);


            for (int i = 0; i < names.Length; i++) {
                header.Append (INFLUENCE);
                header.Append (names [i]);
                header.Append (CSV_ELEMENT_SEPARATOR);


            }

            for (int i = 0; i < names.Length; i++) {
                header.Append (FREQUENCY);
                header.Append (names [i]);
                header.Append (CSV_ELEMENT_SEPARATOR);
            }

            header.Remove (header.Length - CSV_ELEMENT_SEPARATOR.Length, CSV_ELEMENT_SEPARATOR.Length);

            StringBuilder results = new StringBuilder ();
            foreach (BinaryOption [] options in counts [0].Keys) {
                // Firstly insert the term
                results.Append (convertTerm (options));
                results.Append (CSV_ELEMENT_SEPARATOR);

                // Insert the influence scores
                for (int i = 0; i < names.Length; i++) {
                    results.Append (influences [i] [options]);
                    results.Append (CSV_ELEMENT_SEPARATOR);
                }

                // Insert the influence scores
                for (int i = 0; i < names.Length; i++) {
                    results.Append (counts [i] [options]);
                    results.Append (CSV_ELEMENT_SEPARATOR);
                }
                results.Remove (results.Length - CSV_ELEMENT_SEPARATOR.Length, CSV_ELEMENT_SEPARATOR.Length);
                results.Append (CSV_ROW_SEPARATOR);
            }

            return header.ToString() + CSV_ROW_SEPARATOR;
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

        private static List<BinaryOption []> deriveCombinations (int depth)
        {
            List<BinaryOption []> result = new List<BinaryOption []> ();
            foreach (List<BinaryOption> options in deriveCombinationsHelper (depth)) {
                options.Sort ();
                BinaryOption [] tmp = options.ToArray ();
                result.Add (tmp);

            }
            return result;
        }

        private static List<List<BinaryOption>> deriveCombinationsHelper (int depth)
        {
            if (depth == 0) {
                return new List<List<BinaryOption>>();
            } else if (depth == 1) {
                List<List<BinaryOption>> fw = new List<List<BinaryOption>> ();
                foreach (BinaryOption option in GlobalState.varModel.BinaryOptions) {
                    List<BinaryOption> tmp = new List<BinaryOption> ();
                    tmp.Add (option);
                    fw.Add (tmp);
                }
                return fw;
            }


            List<List<BinaryOption>> previousResult = deriveCombinationsHelper (depth - 1);

            List<List<BinaryOption>> result = new List<List<BinaryOption>> ();

            foreach (BinaryOption option in GlobalState.varModel.BinaryOptions) {
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
            foreach (BinaryOption [] key in dict.Keys) {
                dict [key] /= count;
            }
        }


        private static void countIfContained (Configuration config, List<BinaryOption []> identifiedTerms, Dictionary<BinaryOption[], double> count)
        {
            foreach (BinaryOption [] options in identifiedTerms) {
                bool contained = true;
                foreach (BinaryOption option in options) {
                    if (!config.BinaryOptions.ContainsKey (option) || config.BinaryOptions [option] != BinaryOption.BinaryValue.Selected) {
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
