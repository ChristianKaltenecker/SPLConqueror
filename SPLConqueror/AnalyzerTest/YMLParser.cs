using System;
using System.IO;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Helpers;
using System.Collections.Generic;

namespace AnalyzerTest
{
    public static class YMLParser
    {
        public static AnalyzerInfo readInFile (String filePath)
        {
            StreamReader sr = new StreamReader (filePath);
            YamlStream yaml = new YamlStream ();

            yaml.Load (sr);

            string wpDirectory = null;
            string runDirectory = null;
            List<string> caseStudies = new List<string>() ;
            Dictionary<string, string> strategies = new Dictionary<string, string>();


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
                        foreach (YamlMappingNode item in seqNode)
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
                    default:
                        throw new ArgumentException("The node " + node.Key + " is not valid.");
                }
            }

            return new AnalyzerInfo(wpDirectory, runDirectory, caseStudies, strategies);


        }
    }
}
