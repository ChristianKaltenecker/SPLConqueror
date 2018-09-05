using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MachineLearning.Sampling.Hybrid.Distributive
{
    /// <summary>
    /// This class represents the inverse geometric distribution, where the last buckets have a high probability to be picked.
    /// </summary>
    class InverseGeometricDistribution : IDistribution
    {
        /// <summary>
        /// See <see cref="IDistribution.CreateDistribution(List{double})"/>.
        /// </summary>
        public Dictionary<double, double> CreateDistribution(List<double> allBuckets)
        {
            GeometricDistribution geometricDistribution = new GeometricDistribution();
            Dictionary<double, double> original = geometricDistribution.CreateDistribution(allBuckets);

            Dictionary<double, double> result = new Dictionary<double, double>();

            for (int i = 0; i < allBuckets.Count; i++)
            {
                double index = allBuckets[i];
                double inverseIndex = allBuckets[allBuckets.Count - 1 - i];

                result[inverseIndex] = original[index];
            }

            return DistributionUtils.AdjustToOne(result);
        }

        /// <summary>
        /// See <see cref="IDistribution.GetName"/>.
        /// </summary>
        public string GetName()
        {
            return "INVERSEGEOMETRIC";
        }
    }
}
