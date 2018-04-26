using System;
using System.Collections.Generic;

namespace AnalyzerTest
{
    public class LowerUpperBound
    {
        public List<double> elements { get; private set; }= new List<double>();

        public double LowerBound { get; private set; }
        public double UpperBound { get; private set; }

        public LowerUpperBound()
        {
        }

        public void AddElement(double element) {
            elements.Add(element);
            if (LowerBound == null) {
                LowerBound = element;
                UpperBound = element;
            } else if (element.CompareTo(UpperBound) > 0) {
                UpperBound = element;
            } else if (element.CompareTo(LowerBound) < 0) {
                LowerBound = element;
            }
        }

        public int Length() {
            return elements.Count;
        }

        public void DivideBy(double divisor) {
            LowerBound = Math.Abs(LowerBound / divisor);
            UpperBound = Math.Abs(UpperBound / divisor);
            for (int i = 0; i < elements.Count; i++) {
                elements[i] = Math.Abs(elements[i] / divisor);
            }
        }
    }
}
