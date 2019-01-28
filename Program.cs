using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

//TO DO create object to store indexDictionary and readText name it ParserWorker or something like that.

namespace Parser
{
    class Program
    {
        private static Regex mainRegex = new Regex(@"(?<=(<.*?>))(\w|\d|\n|[().,\-:;@#$%^*\[\]+–/\/®°⁰!?|`~]| )+?(?=(</.*?>))");
        private static Regex placeholderRegex = new Regex(@"placeholder=""([A-Za-z0-9a-zżźćńółęąśŻŹĆĄŚĘŁÓŃ?!.\-, ]+)""");
        private static Regex valueRegex = new Regex(@"value=""([A-Za-z0-9a-zżźćńółęąśŻŹĆĄŚĘŁÓŃ?!.\-, ]+)""");
        private static List<string> specialWords = new List<string>() {"&nbsp","&rsaquo;","&#10003;"};

        static void Main(string[] args)
        {
            foreach( var arg in args) {
                Parse(arg);
            }
        }

        static void Parse(string path){
             Dictionary<string,int> indexDictionary = GetIndexDictionary("kody.csv");
             string readText = "";
             try{
                readText = File.ReadAllText(path);
             }catch (FileNotFoundException exception) {
                Console.WriteLine("File with path: {0} \n Exception: {1}",path,exception.Message);
                return;
             }
             

            var matchesLength = GetMatchesCount(readText,mainRegex);

            for(var i = 0 ; i < matchesLength; i++) {
                Match match = mainRegex.Match(readText);
                var index = match.Index;
                var matchedString = match.Value;
                Console.WriteLine("Matched string: '{0}'", matchedString);
                var indexFromDictionary = GetIndexInIndexDictionaryIfItExists(ref indexDictionary,matchedString);
                var replaceString = GetReplaceString(matchedString,indexFromDictionary);
                readText = ReplaceMyStringAndRemoveOldOne(readText,matchedString,replaceString,index);
            }

            ParseTextForRegexArgument(ref readText,ref indexDictionary,placeholderRegex);
            ParseTextForRegexArgument(ref readText,ref indexDictionary,valueRegex);

            SaveIndexDictionaryToCsv(indexDictionary,"kody.csv");
            string fileResultPath = path;

            Encoding utf8WithoutBom = new UTF8Encoding(false);
            File.WriteAllText(fileResultPath, readText, utf8WithoutBom);
        }

        static void ParseTextForRegexArgument(ref string readText,ref Dictionary<string,int> indexDictionary, Regex regex) {
            var matchesCount = GetMatchesCount(readText,regex);

            for(var i =0; i < matchesCount; i++) {
                Match match = regex.Match(readText);
                var correctMatchGroup = match.Groups.Last();
                string matchedString = correctMatchGroup.Value;
                int positionIndexInText = correctMatchGroup.Index;
                Console.WriteLine("Matched string: '{0}'", matchedString);
                var indexFromDictionary = GetIndexInIndexDictionaryIfItExists(ref indexDictionary,matchedString);
                var replaceString = GetReplaceString(matchedString,indexFromDictionary);
                readText = ReplaceMyStringAndRemoveOldOne(readText,matchedString,replaceString,positionIndexInText);
            }
        }

    
        static int GetIndexInIndexDictionaryIfItExists(ref Dictionary<string,int> indexDictionary,string matchedString) {
             var isInDictionary = indexDictionary.ContainsKey(matchedString);

            var indexFromDictionary = 0;

            if(isInDictionary) {
                indexFromDictionary = indexDictionary.GetValueOrDefault(matchedString);
            }else {
                if(indexDictionary.Values.Count > 0)
                {
                    var lastIndexFromDictionary = indexDictionary.Values.Last();
                    indexFromDictionary = ++lastIndexFromDictionary;
                }else {
                    indexFromDictionary = 1;
                }

                indexDictionary.Add(matchedString,indexFromDictionary);
            }

            return indexFromDictionary;
        }

        static void SaveIndexDictionaryToCsv(Dictionary<string,int> indexDictionary,string path) {
            var csv = new StringBuilder();
            foreach(var key in indexDictionary.Keys){
                var first = key;
                var second = indexDictionary.GetValueOrDefault(key);
                var newLine = string.Format("{0};{1}{2}",first,second,Environment.NewLine);
                csv.Append(newLine);
            }

            var textToSave = csv.ToString();
            Encoding utf8WithoutBom = new UTF8Encoding(false);
            File.WriteAllText(path,textToSave, utf8WithoutBom);
        }

        static Dictionary<string,int> GetIndexDictionary(string path) {
            Dictionary<string,int> dictionary = new Dictionary<string,int>();

            using(var reader = new StreamReader(path))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(';');
                    int index = 0;
                    try{
                     index = Convert.ToInt32(values[1]);
                    }
                    catch(Exception ex) {
                        Console.WriteLine("Key: "+ values[0] + "\n Exception:"+ ex.Message);
                    }
                    
                    dictionary.Add(values[0],index);
                }
            }

            return dictionary;
        }

        static string GetReplaceString(string matchedText,int indexFromDictionary) {
            return "{'" + matchedText + "'|lng:"+ indexFromDictionary + "}"; 
        }

        static int GetMatchesCount(string text, Regex regex) {
            MatchCollection matches = regex.Matches(text);

            Console.WriteLine("{0} matches found", matches.Count); 
            return matches.Count;
        }

        static string ReplaceMyStringAndRemoveOldOne(string source, string oldString,string newString, int posiiton) {
            
            var firstPart = source.Substring(0,posiiton);
            var secondPart = source.Substring(posiiton+oldString.Length);

            return firstPart + newString + secondPart;
        }
    }
}
