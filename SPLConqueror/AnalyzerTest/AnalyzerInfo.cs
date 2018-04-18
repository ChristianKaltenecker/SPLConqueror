using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalyzerTest
{
    class AnalyzerInfo
    {
        private string wpDirectory;
        private string runDirectory;
        private List<string> caseStudies;
        private Dictionary<string, string> strategies;

        public AnalyzerInfo(string wpDirectory, string runDirectory, List<string> caseStudies, Dictionary<string, string> strategies)
        {
            this.wpDirectory = wpDirectory;
            this.runDirectory = runDirectory;
            this.caseStudies = caseStudies;
            this.strategies = strategies;
        }

        public string GetWpDirectory()
        {
            return this.wpDirectory;
        }

        public string GetRunDirectory()
        {
            return this.runDirectory;
        }

        public List<string> GetCaseStudies()
        {
            return this.caseStudies;
        }

        public Dictionary<string, string> GetStrategies()
        {
            return this.strategies;
        }
    }
}
