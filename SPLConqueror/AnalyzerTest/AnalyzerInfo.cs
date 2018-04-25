using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalyzerTest
{
    public class AnalyzerInfo
    {
		public string WpDirectory { get; private set; }
		public string RunDirectory { get; private set; }
		public List<string> CaseStudies { get; private set; }
		public Dictionary<string, string> Strategies { get; private set; }
		public List<string> Sizes { get; private set; }

        public AnalyzerInfo(string wpDirectory, string runDirectory, List<string> caseStudies, 
		                    Dictionary<string, string> strategies, List<string> sizes)
        {
			this.WpDirectory = wpDirectory;
			this.RunDirectory = runDirectory;
            this.CaseStudies = caseStudies;
            this.Strategies = strategies;
			this.Sizes = sizes;
        }
    }
}
