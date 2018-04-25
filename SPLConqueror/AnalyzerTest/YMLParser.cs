using System;
using System.IO;
using YamlDotNet.RepresentationModel;
using System.Collections.Generic;

namespace AnalyzerTest
{
    public static class YMLParser
    {
		/// <summary>
        /// Reads in a .yml-file where the configuration is stored.
        /// </summary>
		/// <returns>The information from the file as <see cref="AnalyzerInfo"/>.</returns>
        /// <param name="filePath">The path to the .yml-file.</param>
        public static AnalyzerInfo ReadInFile (String filePath)
        {
            StreamReader sr = new StreamReader (filePath);
            YamlStream yaml = new YamlStream ();

            yaml.Load (sr);

            string wpDirectory = null;
            string runDirectory = null;
            List<string> caseStudies = new List<string>() ;
            Dictionary<string, string> strategies = new Dictionary<string, string>();
			List<string> sizes = new List<string> ();


            YamlMappingNode root = (YamlMappingNode) yaml.Documents [0].RootNode;

            foreach (KeyValuePair<YamlNode, YamlNode> node in root.Children)
            {
                switch (node.Key.ToString())
                {
                    case "wpDirectory":
                        wpDirectory = node.Value.ToString();
                        break;
                    case "runDirectory":
                        runDirectory = node.Value.ToString();
                        break;
                    case "caseStudies":
                        YamlSequenceNode seqNode = (YamlSequenceNode) node.Value;
					    foreach (YamlNode item in seqNode.Children)
                        {
                            caseStudies.Add(item.ToString());
                        }
                        break;
                    case "strategies":
                        foreach (KeyValuePair<YamlNode, YamlNode> dictEntry in ((YamlMappingNode)node.Value).Children)
                        {
                            strategies.Add(dictEntry.Key.ToString(), dictEntry.Value.ToString());
                        }
                        break;
			        case "sizes":
					    YamlSequenceNode sequenceNode = (YamlSequenceNode)node.Value;
					    foreach (YamlNode item in sequenceNode.Children) {
						    sizes.Add (item.ToString());
    					}
					    break;
                    default:
                        throw new ArgumentException("The node " + node.Key + " is not valid.");
                }
            }

            return new AnalyzerInfo(wpDirectory, runDirectory, caseStudies, strategies, sizes);


        }
    }
}
