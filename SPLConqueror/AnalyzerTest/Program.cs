using System;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using MachineLearning.Analysis;
using SPLConqueror_Core;
using System.IO;

namespace AnalyzerTest
{
    public class Program
    {
        static void Main (string [] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture ("en-US");

            if (args.Length <= 2 || (args.Length - 2) % 3 != 0) {
                PrintUsage ();
                Environment.Exit (0);
            }

            String vmPath = args [0];
            VariabilityModel vm = VariabilityModel.loadFromXML (vmPath);
            GlobalState.varModel = vm;
            String outputPath = args [1];
            List<Tuple<String, String, String>> learnedModels = new List<Tuple<string, string, string>> ();
            for (int i = 2; i < args.Length; i = i + 3) {
                learnedModels.Add (new Tuple<string, string, string> (args[i], args[i+1], args[i+2]));
            }

            Analyzer.analyzeModels (vm, learnedModels.ToArray(), Path.GetTempFileName(), outputPath);
        }

        private static void PrintUsage ()
        {
            Console.WriteLine ("Usage: <vmPath> <outputPath> (<name> <samplePath> <logPath>)*");
        }

    }
}
