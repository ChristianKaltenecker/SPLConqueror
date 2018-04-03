using System.Collections.Generic;
using SPLConqueror_Core;
using MachineLearning.Learning.Regression;
using System;

namespace MachineLearning.Analysis
{
    /// <summary>
    /// This class contains methods to analyze performance models and samples.
    /// </summary>
    public class Analyzer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:MachineLearning.Analyser"/> class.
        /// This constructor is private, as no instance of this class is needed so far.
        /// </summary>
        private Analyzer () { }

        /// <summary>
        /// Analyzes the given models by comparing the introduced terms.
        /// </summary>
        /// <returns>The overall error to the ground-truth (i.e., the whole population).</returns>
        /// <param name="learnedModels">The learned models.</param>
        public static double[] analyzeModels (FeatureSubsetSelection [] learnedModels)
        {
            // Search for the terms that were included by machine learning
            List<BinaryOption []> identifiedTerms = new List<BinaryOption []> ();
            for (int i = 0; i < learnedModels.Length; i++) {
                foreach (LearningRound round in learnedModels [i].LearningHistory) {
                    List<BinaryOption> options = new List<BinaryOption> ();
                    options.AddRange (round.bestCandidate.participatingBoolOptions);
                    options.Sort ();
                    BinaryOption [] optionsArray = options.ToArray ();

                    if (!identifiedTerms.Contains (optionsArray)) {
                        identifiedTerms.Add (optionsArray);
                    }
                }
            }

            // Count only the necessary configuration options and interactions on the whole population
            Dictionary<BinaryOption [], double> wpCounts = new Dictionary<BinaryOption [], double> ();
            foreach (Configuration config in GlobalState.allMeasurements.Configurations) {
                countIfContained (config, identifiedTerms, wpCounts);
            }
            divideDictionaryValuesBy (wpCounts, GlobalState.allMeasurements.Configurations.Count);



            // Do the same on the sample sets
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


            // Analyze them by using (1) the difference of the number of appearencies, ((2) the general impact), 
            // and (3) the number of appearencies in the whole population

            // Acquire the needed data
            double [] appearenciesError = new double [learnedModels.Length];
            for (int i = 0; i < learnedModels.Length; i++) {
                foreach (BinaryOption [] options in sampleSetCounts [i].Keys) {
                    appearenciesError[i] += Math.Abs(wpCounts[options] - sampleSetCounts[i][options]) / wpCounts[options];
                }
            }

            // Compute the difference between the models
            double [] sampleSetError = new double [learnedModels.Length];

            for (int i = 0; i < learnedModels.Length; i++) {
                foreach (BinaryOption [] options in sampleSetCounts [i].Keys) {
                    sampleSetError[i] += appearenciesError [i] * wpCounts[options];
                }
            }

            return sampleSetError;
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
