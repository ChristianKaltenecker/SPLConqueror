using System;
using System.Linq;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;

namespace AnalyzerTest
{
    public class Program
    {
        static void Main (string [] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture ("en-US");

            if (args.Length != 1) {
                PrintUsage ();
                Environment.Exit (0);
            }

			string ymlFilePath = args [0];

			AnalyzerInfo analyzer = YMLParser.ReadInFile (ymlFilePath);

			// Parse the whole population run directory
			Dictionary<string, CaseStudy> caseStudies = Extractor.ExtractWpInformation (analyzer.WpDirectory, analyzer.CaseStudies);

			// Parse the run directory
			caseStudies = Extractor.ExtractRunInformation (analyzer.RunDirectory, analyzer.CaseStudies, analyzer.Strategies, analyzer.Sizes, caseStudies);

			// Perform the analysis on all relevant data and print it in a file
			List<string> sizes = analyzer.Sizes;
			string directory = null;
			foreach (string size in sizes) {
				if (directory == null) {
					directory = Analyzer.AnalyzeModels (caseStudies, analyzer.Strategies, size);
				} else {
					Analyzer.AnalyzeModels (caseStudies, analyzer.Strategies, size, directory);
				}

				Analyzer.AnalyzeModels (caseStudies, analyzer.Strategies, size, directory, onlyBestModel: false);
			}
			Console.WriteLine ("Temporary directory: " + directory);


			// TODO: Find a way to handle alternatives

			// TODO: Create the plots (line-plot + box-plots) by using a python script
        }

        private static void PrintUsage ()
        {
            Console.WriteLine ("Usage: <pathToYamlFile>");
			Console.WriteLine ("pathToYamlFile\t The path to the .yml-file where the relevant information for the analysis is stored.");
			// TODO: Explain content of .yml-file
        }

    }
}
