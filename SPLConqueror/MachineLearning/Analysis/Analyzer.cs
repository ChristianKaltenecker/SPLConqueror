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
        public static void analyzeModels (VariabilityModel vm, Tuple<String, String, String>[] learnedModels, String filePath, String outputPath)
        {
            // Read in measurements and log files
            List<Configuration>[] allMeasurements = new List<Configuration>[learnedModels.Length];
            Tuple<string[][], double>[] logInfo = new Tuple<string[][], double>[learnedModels.Length];
            for (int i = 0; i < learnedModels.Length; i++)
            {
                allMeasurements[i] = ConfigurationReader.readConfigurations(learnedModels[i].Item3, vm);
                logInfo[i] = ExtractTerms(getLastModelLine(learnedModels[i].Item3));
            }
            

            // Search for the terms that were included by machine learning
            List<BinaryOption []> identifiedTerms = new List<BinaryOption []> ();
            Dictionary<BinaryOption [], double> [] influence = new Dictionary<BinaryOption [], double> [learnedModels.Length];
            for (int i = 0; i < learnedModels.Length; i++) {
                influence [i] = new Dictionary<BinaryOption [], double> ();
                foreach (string[] model in logInfo[i].Item1) { 
                //foreach (LearningRound round in learnedModels [i].LearningHistory) {
                    List<BinaryOption> options = new List<BinaryOption> ();
                    
                    for (int j = 1; j < model.Length; j++)
                    {
                        options.Add(new BinaryOption(vm, model[j]));
                    }

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
                string[][] wpModel = logInfo[0].Item1;
                double influenceSum = 0.0;
                foreach (string[] term in logInfo[i].Item1) {
                    List<BinaryOption> binOpts = new List<BinaryOption> ();
                    for (int j = 1; j < term.Length; j++)
                    {
                        binOpts.Add(new BinaryOption(vm, term[j]));
                    }
                    binOpts.Sort ();
                    BinaryOption [] binArray = binOpts.ToArray ();
                    influence [i] [binArray] = Math.Abs (Double.Parse(term[0]));
                    influenceSum += Math.Abs(Double.Parse(term[0]));
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
            for (int i = 0; i < learnedModels.Length; i++) {
                sampleSetCounts [i] = new Dictionary<BinaryOption [], double> ();

                foreach (string[] term in logInfo[i].Item1) {
                    List<BinaryOption> options = new List<BinaryOption> ();
                    for (int j = 1; j < term.Length; j++)
                    {
                        options.Add(new BinaryOption(vm, term[j]));
                    }
                    options.Sort ();
                    BinaryOption [] optionsArray = options.ToArray ();
                }

                foreach (Configuration config in allMeasurements[i]) {
                    countIfContained (config, identifiedTerms, sampleSetCounts [i]);
                }
                divideDictionaryValuesBy (sampleSetCounts [i], allMeasurements[i].Count);
            }

            string[] names = new string[learnedModels.Length];
            for (int i = 0; i < learnedModels.Length; i++)
            {
                names[i] = learnedModels[i].Item1;
            }

            // Write the results in a file
            writeToFile (filePath, convertToString (sampleSetCounts, influence, names));

            // Call Python
            PythonWrapper pyWrapper = new PythonWrapper(AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + PythonWrapper.PLOTTER_SCRIPT, 
                new string[0], filePath + " " + outputPath);
            pyWrapper.waitForNextReceivedLine();
            pyWrapper.endProcess();
        }

        private static Tuple<string[][], double> ExtractTerms(string line)
        {
            string[] elements = line.Split(';');
            string[] terms = elements[1].Split('+');
            string[][] allTerms = new string[terms.Length][];
            for (int i = 0; i < allTerms.Length; i++)
            {
                allTerms[i] = terms[i].Split('*');
            }

            double errorRate = Double.Parse(elements[elements.Length - 1]);
            return new Tuple<string[][], double>(allTerms, errorRate);
        }

        private static string getLastModelLine(String file)
        {
            string[] lines = File.ReadAllLines(file);

            for (int i = lines.Length - 1; i > 0; i--)
            {
                if (lines[i].Contains(";"))
                {
                    return lines[i];
                }
            }

            return "";
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
