using System;
using System.Collections.Generic;
using System.Linq;
using SPLConqueror_Core;

namespace MachineLearning.Sampling.Hybrid.Distributive.SelectionHeuristic
{
	public class DiverseSelection : SolverSelection
    {
        /// <summary>
        /// This method selects a given number of configurations by using the sample mechanism of the CSP solver.
        /// </summary>
        /// <returns>A set of configurations that have been picked to fit the given distribution.</returns>
        /// <param name="wantedDistribution">The wanted distribution of the samples.</param>
        /// <param name="allBuckets">The buckets containing at least one configuration.</param>
        /// <param name="count">The number of configurations to select.</param>
        /// <param name="optimization">The optimization to use.</param>
		public List<Configuration> SampleFromDistribution (Dictionary<double, double> wantedDistribution, List<double> allBuckets, int count, Optimization optimization)
		{
			Random rand = new Random (seed);
            List<Configuration> selectedConfigurations = new List<Configuration> ();
            Dictionary<int, Configuration> selectedConfigurationsFromBucket = new Dictionary<int, Configuration> ();
            for (int i = 0; i < allBuckets.Count; i++) {
                selectedConfigurationsFromBucket [i] = null;
            }

            // Create and initialize the weight function
			Dictionary<List<BinaryOption>, int> featureWeight = InitializeWeightDict (GlobalState.varModel);

            List<KeyValuePair<List<BinaryOption>, int>> featureRanking;
            if (featureWeight == null)
            {
                throw new InvalidOperationException("For the diverse selection heuristic, a feature weight has to be provided.");
            }

            // Create a distribution for each candidate
            Dictionary<List<BinaryOption>, Dictionary<double, double>> candidateDistributions = new Dictionary<List<BinaryOption>, Dictionary<double, double>>();
            Dictionary<List<BinaryOption>, bool[]> noSamples = new Dictionary<List<BinaryOption>, bool[]>();

            foreach (List<BinaryOption> candidates in featureWeight.Keys)
            {
                candidateDistributions[candidates] = new Dictionary<double, double>(wantedDistribution);
                noSamples[candidates] = new bool[allBuckets.Count];
            }

            // First, select the feature which should forcedly included
            while (selectedConfigurations.Count < count && featureWeight.Keys.Count > 0) {
                // Select least frequent feature(s)
                featureRanking = featureWeight.ToList();
                featureRanking.Sort((first, second) => first.Value.CompareTo(second.Value));
                List<BinaryOption> leastFrequentFeature = featureRanking.ToArray()[0].Key;

                // Remove the candidate completely, if there are no more samples left
                if (!HasSamples(noSamples[leastFrequentFeature]))
                {
                    featureWeight.Remove(leastFrequentFeature);
                    continue;
                }

                // Select a distance according to the distribution
                Dictionary<double, double> currentDistribution = candidateDistributions[leastFrequentFeature];
                double randomDouble = rand.NextDouble();
                double currentProbability = 0;
                int currentBucket = 0;

                while (randomDouble > currentProbability + wantedDistribution.ElementAt(currentBucket).Value)
                {
                    currentProbability += wantedDistribution.ElementAt(currentBucket).Value;
                    currentBucket++;
                }

                // Note: This method works only for binary features and therefore, only integer buckets
                int distanceOfBucket = Convert.ToInt32(wantedDistribution.ElementAt(currentBucket).Key);

                // Repeat if there are currently no solutions in the bucket.
                // This is intended to reduce the work of the solver.
                // Should not happen anymore!
                if (noSamples[leastFrequentFeature][currentBucket] || !allBuckets.Contains(distanceOfBucket))
                {
                    throw new InvalidProgramException("A bucket was selected that already contains no more samples! This shouldn't happen.");
                }

                // Retrieve a sample
                if (ConfigurationBuilder.vg is Solver.Z3VariantGenerator)
                {
                    ((Solver.Z3VariantGenerator)ConfigurationBuilder.vg).setSeed(Convert.ToUInt32(this.seed));
                }

                List<BinaryOption> solution = null;

                solution = ConfigurationBuilder.vg.GenerateConfigurationWithFeatureAndBucket(GlobalState.varModel, currentBucket, leastFrequentFeature, selectedConfigurationsFromBucket[currentBucket]);

                // If a bucket was selected that now contains no more configurations, repeat the procedure
                if (solution == null)
                {
                    noSamples[leastFrequentFeature][currentBucket] = true;

                    // As a consequence, the probability to pick this bucket is set to 0 and the whole
                    // distribution is readjusted so that the sum of all probabilities is equal to 1 (i.e., 100%).
                    candidateDistributions[leastFrequentFeature][candidateDistributions[leastFrequentFeature].ElementAt(currentBucket).Key] = 0d;
                    wantedDistribution[wantedDistribution.ElementAt(currentBucket).Key] = 0d;
                    candidateDistributions[leastFrequentFeature] = DistributionUtils.AdjustToOne(candidateDistributions[leastFrequentFeature]);
                    continue;
                }

                // Update weights
                Configuration currentSelectedConfiguration = new Configuration(solution);
                selectedConfigurations.Add(currentSelectedConfiguration);
                selectedConfigurationsFromBucket[currentBucket] = currentSelectedConfiguration;

                UpdateWeights(GlobalState.varModel, featureWeight, currentSelectedConfiguration);
            }

            if (selectedConfigurations.Count < count)
            {
                GlobalState.logError.logLine("Sampled only " + selectedConfigurations.Count + " configurations as there are no more configurations.");
            }

            ConfigurationBuilder.vg.ClearCache();

            return selectedConfigurations;
        }
	}
}
