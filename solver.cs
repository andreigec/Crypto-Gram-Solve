using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ANDREICSLIB;
using ANDREICSLIB.Helpers;

namespace CryptoSolve
{
    public static class Solver
    {

        public class SolveInstance
        {
            //word->encrypted
            private Dictionary<char, char> worden = new Dictionary<char, char>();
            //encrypted->word
            private Dictionary<char, char> enword = new Dictionary<char, char>();

            public Dictionary<String, String> SolvedStrings = new Dictionary<string, string>();

            public SolveInstance Clone()
            {
                var si = new SolveInstance();
                foreach (var k in worden)
                {
                    si.worden.Add(k.Key, k.Value);
                }

                foreach (var k2 in enword)
                {
                    si.enword.Add(k2.Key, k2.Value);
                }

                foreach (var k3 in SolvedStrings)
                {
                    si.SolvedStrings.Add(k3.Key, k3.Value);
                }

                return si;
            }

            public bool Compare(String word, String enword)
            {
                return CompareChange(word, enword, false);
            }

            public bool CompareChange(String word, String enword, bool change = true)
            {
                if (word.Length != enword.Length)
                    return false;

                for (int a = 0; a < word.Length; a++)
                {
                    if (CompareChange(word[a], enword[a], change) == false)
                        return false;
                }
                return true;
            }

            private bool CompareChange(char word, char en, bool change)
            {
                if (worden.ContainsKey(word) == false)
                {
                    if (change == false)
                        return true;

                    worden.Add(word, en);
                }

                if (enword.ContainsKey(en) == false)
                {
                    if (change == false)
                        return true;

                    enword.Add(en, word);
                }

                return (worden[word] == en && enword[en] == word);
            }
        }

        public static HashSet<String> DictionaryWords = new HashSet<string>();

        public static void Init()
        {
            var x = EmbeddedResources.ReadEmbeddedResource("dictionarywords.txt");

            var x2 = x.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var s in x2)
                DictionaryWords.Add(s);
        }


        private static IEnumerable<Tuple<SolveInstance, string>> Unencrypt(SolveInstance si, String encrypted)
        {
            //get all possible words of this length
            List<string> okstrs = DictionaryWords.Where((a, b) => si.Compare(a, encrypted)).ToList();

            if (okstrs.Count == 0)
                yield break;

            foreach (var s in okstrs)
            {
                var si2 = si.Clone();
                var ok = si2.CompareChange(s, encrypted);

                if (ok == false)
                    continue;

                si2.SolvedStrings[encrypted] = s;

                yield return new Tuple<SolveInstance, string>(si2, s);
            }
        }

        private static void OutputResult(SolveInstance si, Form1.IncrementCounter ic)
        {
            if (ic == null)
                return;

            String ret = GetResultText(si);

            ic(ret);
        }

        private static string GetResultText(SolveInstance si)
        {
            var ret = "";
            foreach (var s in si.SolvedStrings.Where(s => s.Value != null))
            {
                ret += s.Key + ":" + s.Value + ",";
            }
            return ret;
        }

        private static bool AskForCorrect(SolveInstance si, ref  List<Tuple<int, string>> tl, Form1.ResultCallBack callback)
        {
            var ret = (from x in tl.OrderBy(s => s.Item1)
                       select si.SolvedStrings[x.Item2]).Aggregate("", (q1, q2) => q1 + q2 + " ");
            
            return callback(ret);
        }

        public static String EncodeText(String intext)
        {
            var ret = "";

            //create bucket list for all chars
            var convert = new Dictionary<char, char>();
            var possiblechars = new List<char>();
            for (int a = 97; a < 122; a++)
                possiblechars.Add((char)a);

            bool usednumbers = false;
            bool useduppercase = false;

            var r = new Random(DateTime.Now.Millisecond);

            foreach(var c in intext)
            {
                if (string.IsNullOrWhiteSpace(c.ToString()))
                {
                    ret += c;
                    continue;
                }

                if (convert.ContainsKey(c)==false)
                {
                    //we try to only use lower case chars for encoding, but if this isnt enough, gradually include more obscure charsets
                    if (possiblechars.Count==0)
                    {
                        if (usednumbers==false)
                        {
                            for (int a = 48; a < 57; a++)
                                possiblechars.Add((char)a);
                            usednumbers = true;
                        }

                        if (useduppercase==false)
                        {
                            for (int a = 65; a < 90; a++)
                                possiblechars.Add((char)a);
                            useduppercase = true;
                        }
                    }

                    var r2 = r.Next()%possiblechars.Count;
                    var nc = possiblechars[r2];
                    possiblechars.Remove(nc);
                    convert.Add(c,nc);
                }

                ret += convert[c];
            }


            return ret;
        }

        /// <summary>
        /// tuple enc unenc
        /// </summary>
        /// <param name="si"></param>
        /// <returns></returns>
        private static IEnumerable<Tuple<string, string>> Solve(SolveInstance si, ref  List<Tuple<int, string>> tl, Form1.ResultCallBack callback, Form1.IncrementCounter ic)
        {
            var left = si.SolvedStrings.Where(s => s.Value == null);
            var lc = left.Count();
            if (lc == 0)
            {
                if (AskForCorrect(si, ref tl, callback) == false)
                    return null;

                return new List<Tuple<string, string>>();
            }

            var en = left.First().Key;

            foreach (var s in Unencrypt(si, en))
            {
                if (ic != null)
                    OutputResult(s.Item1, ic);

                var retr = Solve(s.Item1, ref tl, callback, ic);
                if (retr != null)
                {
                    var r = new Tuple<string, string>(en, s.Item2);
                    var ret = new List<Tuple<string, string>>();
                    ret.Add(r);
                    ret.AddRange(retr);
                    return ret;
                }
            }
            return null;
        }

        public static void Solve(string words, Form1.ResultCallBack callback, Form1.IncrementCounter ic)
        {
            callback("--START--");
            var si = new SolveInstance();

            var s = words.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var tl = s.Select((t, a) => new Tuple<int, string>(a, t)).ToList();

            //shortest first
            foreach (var s1 in tl.OrderByDescending(s1 => s1.Item2.Length))
            {
                if (si.SolvedStrings.ContainsKey(s1.Item2) == false)
                    si.SolvedStrings.Add(s1.Item2, null);
            }

            Solve(si, ref tl, callback, ic);
            callback("--FINISHED--");
        }


    }
}
