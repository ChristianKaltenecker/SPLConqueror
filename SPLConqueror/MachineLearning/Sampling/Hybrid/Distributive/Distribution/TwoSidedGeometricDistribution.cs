using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MachineLearning.Sampling.Hybrid.Distributive
{
    /// <summary>
    /// This class represents an two-sided geometric distribution.
    /// </summary>
    public class TwoSidedGeometricDistribution : IDistribution
    {

        /// <summary>
        /// See <see cref="IDistribution.CreateDistribution(List{double})"/>.
        /// </summary>
        public Dictionary<double, double> CreateDistribution(List<double> allBuckets)
        {
            GeometricDistribution geometricDistribution = new GeometricDistribution();
            Dictionary <double, double> original = geometricDistribution.CreateDistribution(allBuckets);
            Dictionary <double, double> result = new Dictionary <double, double>();

            for (int i = 0; i < allBuckets.Count; i++)
            {
                double index = allBuckets[i];
                double inverseIndex = allBuckets[allBuckets.Count - 1 - i];
                result[index] = original[index] + original[inverseIndex];
            }

            return DistributionUtils.AdjustToOne(result);
        }

        /// <summary>
        /// Returns the name of the distribution.
        /// </summary>
        /// <returns>the name of the distribution</returns>
        public string GetName()
        {
            return "TWOSIDEDGEOMETRIC";
        }
    }
}
