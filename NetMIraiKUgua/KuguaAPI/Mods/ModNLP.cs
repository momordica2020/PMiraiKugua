using MMDK.Util;
using System.Text.RegularExpressions;
using System.Threading.Tasks;



namespace MMDK.Mods
{
    internal class ModNLP : Mod
    {
        ViterbiModel model;
        private List<string> pinyinMapping;
        

        //private static readonly Lazy<ModNLP> instance = new Lazy<ModNLP>(() => new ModNLP());
        //public static ModNLP Instance => instance.Value;
        //private ModNLP()
        //{


        //}
        public void Exit()
        {
            
        }

        public bool HandleText(long userId, long groupId, string message, List<string> results)
        {
            if (string.IsNullOrWhiteSpace(message)) { return false; }
            try
            {
                Regex reg = new Regex("^谐音(.+)", RegexOptions.Singleline);
                Match m = reg.Match(message);
                if (m.Success)
                {
                    string target = m.Groups[1].Value;
                    if (!string.IsNullOrWhiteSpace(target))
                    {
                        string py = getPinyinFirstList(target);
                        if (string.IsNullOrWhiteSpace(py)) return false;
                        var bestSequence = model.GetSamePinyinSentnse(py);
                        results.Add(bestSequence);
                        return true;
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
            }
            
            return false;
        }

        public bool Init(string[] args)
        {
            try
            {
                model = new ViterbiModel(this);
                //string updateFile = "input.txt"; // 这里是输入的文本文件
                string modelData = Config.Instance.ResourceFullPath("NLP_MODEL1");
                string pinyinUTF8 = Config.Instance.ResourceFullPath("Pinyin");
                pinyinMapping = new List<string>();
                foreach (var line in File.ReadLines(pinyinUTF8))
                {
                    pinyinMapping.Add(line.Trim());
                }

                var load = model.LoadModel(modelData);
                if (!load)
                {
                    //model.TrainModel(updateFile);
                    //model.SaveModel(modelData);

                    //model = new ViterbiModel();
                    //model.LoadModel(modelData);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
            }


            return true;
        }










        private string getPinyinFirstList(string input)
        {
            string output = "";
            if (model != null)
            {


                foreach (var c in input)
                {
                    string pinyinfull = GetPinyinSingle(c);
                    if (!string.IsNullOrWhiteSpace(pinyinfull))
                    {
                        output += pinyinfull[0];
                    }
                    else
                    {
                        output += c;
                    }
                }





            }
            return output;
        }



        // 汉字获取拼音首字母
        public string GetPinyinSingle(char character)
        {
            if (IsHan(character)) // 汉字范围
            {
                int index = (int)character - 0x4E00; // 计算索引
                if (index >= 0 && index < pinyinMapping.Count && !string.IsNullOrWhiteSpace(pinyinMapping[index]))
                {
                    return pinyinMapping[index]; // 返回首字母
                }
            }
            if (character >= 'a' && character <= 'z') return character.ToString().Replace("i", "y").Replace("u", "w").Replace("v", "w");
            if (character >= 'A' && character <= 'Z') return character.ToString().ToLower().Replace("i", "y").Replace("u", "w").Replace("v", "w");
            return "";
        }

        public static bool IsHan(char character)
        {
            return character >= 0x4E00 && character <= 0x9FFF;
        }



    }


    class ViterbiModel
    {
        ModNLP fatherModel;
        Random random = new Random();
        private Dictionary<string, double> startProb; // 初始状态概率
        private Dictionary<string, Dictionary<string, double>> transProb; // 转移概率
        private Dictionary<string, Dictionary<string, double>> emitProb; // 发射概率
        private string[] states; // 状态集合
        private readonly string stateEmpty = "\0";

        

        public ViterbiModel(ModNLP _father)
        {
            fatherModel = _father;
            startProb = new Dictionary<string, double>();
            transProb = new Dictionary<string, Dictionary<string, double>>();
            emitProb = new Dictionary<string, Dictionary<string, double>>();
            //states = string[];// new HashSet<string>();
        }


        public string GetSamePinyinSentnse(string inputSequence)
        {
            string output = "";

            if (string.IsNullOrWhiteSpace(inputSequence)) return output;

            int beginIndex = 0;
            int endIndex = -1;
            for (int i = 0; i < inputSequence.Length; i++)
            {
                if (inputSequence[i] >= 'a' && inputSequence[i] <= 'z')
                {
                    endIndex = i;

                }
                else
                {
                    if (endIndex >= beginIndex)
                        output += ViterbiFirstPinyin(inputSequence.Substring(beginIndex, endIndex - beginIndex + 1));
                    output += inputSequence[i];
                    beginIndex = i + 1;
                }
            }
            if (endIndex >= beginIndex) output += ViterbiFirstPinyin(inputSequence.Substring(beginIndex));

            return output;
        }

        /// <summary>
        /// 得到首字母序列推算的汉字
        /// </summary>
        /// <param name="inputSequence"></param>
        /// <returns></returns>
        string ViterbiFirstPinyin(string inputSequence)
        {

            //double threshold = 1e-6;
            //int topK = 6;

            string[] observations = inputSequence.Select(e => e.ToString()).ToArray();
            //string[] observations = inputSequence.Where(e => e >= 'a' && e <= 'z').Select(e=>e.ToString()).ToArray();

            int n = observations.Length;

            var dp = new Dictionary<string, double>[n];
            var path = new Dictionary<string, string>[n];
            for (int i = 0; i < n; i++)
            {
                dp[i] = new Dictionary<string, double>();
                path[i] = new Dictionary<string, string>();
            }

            // 初始化 DP 表和路径表
            foreach (string state in states.Where(s => IsRelevant(s, observations[0])))
            {
                // 获取起始概率
                if (startProb.TryGetValue(state, out var startProbability))
                {
                    dp[0][state] = startProbability * GetEmissionProbability(state, observations[0]);
                }
                else
                {
                    dp[0][state] = 0; // 如果没有起始概率，设为 0
                }

                path[0][state] = stateEmpty; // 初始化路径
            }
            // 递推过程
            for (int i = 1; i < n; i++)
            {

                dp[i] = new Dictionary<string, double>();
                path[i] = new Dictionary<string, string>();

                // 只考虑与当前观察相关的状态
                var relevantStates = states.Where(s => IsRelevant(s, observations[i])).ToList();

                foreach (string state in relevantStates)
                {
                    double maxProb = double.NegativeInfinity;
                    string maxState = "\0";
                    foreach (string prevState in dp[i - 1].Keys)
                    {
                        //if (!dp[i - 1].ContainsKey(prevState)) continue;

                        double transitionProb = GetTransitionProbability(prevState, state);
                        double emissionProb = GetEmissionProbability(state, observations[i]);
                        double prob = dp[i - 1][prevState] * transitionProb * emissionProb;

                        if (prob > maxProb)
                        {
                            maxProb = prob;
                            maxState = prevState;
                        }
                    }

                    if (maxState != stateEmpty)
                    {
                        dp[i][state] = maxProb;
                        path[i][state] = maxState;
                    }
                }
            }

            // 终止步骤
            double finalMax = double.NegativeInfinity;
            string bestState = stateEmpty;

            foreach (string state in dp[n - 1].Keys)
            {
                if (dp[n - 1][state] > finalMax)
                {
                    finalMax = dp[n - 1][state];
                    bestState = state;
                }
            }

            // 回溯过程
            List<string> bestPath = new List<string>();

            for (int i = n - 1; i >= 0; i--)
            {
                bestPath.Add(bestState);
                bestState = path[i][bestState];
                if (bestState == stateEmpty) break;
            }

            bestPath.Reverse(); // 反转以获得正确顺序
            return new string(string.Join("", bestPath));

        }


        /// <summary>
        /// 判断状态是否能够生成所观测的值。用于动态规划过程中的剪枝
        /// </summary>
        /// <param name="state"></param>
        /// <param name="observation"></param>
        /// <returns></returns>
        private bool IsRelevant(string state, string observation)
        {
            if (string.IsNullOrWhiteSpace(state) || string.IsNullOrWhiteSpace(observation)) return false;
            // 获取状态的拼音首字母
            var a = fatherModel.GetPinyinSingle(state[0]);
            if (string.IsNullOrWhiteSpace(a)) return false;
            else return (a[0] == observation[0]);

        }
        

        /// <summary>
        /// 获取发射概率
        /// </summary>
        /// <param name="state"></param>
        /// <param name="observation"></param>
        /// <returns></returns>
        private double GetEmissionProbability(string state, string observation)
        {
            if (emitProb.TryGetValue(state, out var aaa))
            {
                if (aaa.TryGetValue(observation, out var probability))
                {
                    return probability * random.Next(10, 100);
                }

            }
            return 0.00001 * random.Next(1, 100);
        }

        /// <summary>
        /// 获取转移概率
        /// </summary>
        /// <param name="fromState"></param>
        /// <param name="toState"></param>
        /// <returns></returns>
        private double GetTransitionProbability(string fromState, string toState)
        {
            if (transProb.TryGetValue(fromState, out var transitions) && transitions.TryGetValue(toState, out var probability))
            {
                return probability * random.Next(10, 100);
            }
            return 0.00001 * random.Next(1, 100);
        }





        // 从文件加载模型
        public bool LoadModel(string filePath)
        {
            


            if (!File.Exists(filePath)) return false;
            using (StreamReader reader = new StreamReader(filePath))
            {
                string section = string.Empty;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine().Trim();
                    if (line == "StartProb")
                    {
                        section = "StartProb";
                    }
                    else if (line == "TransProb")
                    {
                        section = "TransProb";
                    }
                    else if (line == "EmitProb")
                    {
                        section = "EmitProb";
                    }
                    else
                    {
                        var parts = line.Split('\t');
                        if (section == "StartProb" && parts.Length == 2)
                        {
                            startProb[parts[0]] = double.Parse(parts[1]);
                        }
                        else if (section == "TransProb" && parts.Length == 3)
                        {
                            if (!transProb.ContainsKey(parts[0]))
                                transProb[parts[0]] = new Dictionary<string, double>();
                            transProb[parts[0]][parts[1]] = double.Parse(parts[2]);
                        }
                        else if (section == "EmitProb" && parts.Length == 3)
                        {
                            if (!emitProb.ContainsKey(parts[0]))
                                emitProb[parts[0]] = new Dictionary<string, double>();
                            emitProb[parts[0]][parts[1]] = double.Parse(parts[2]);
                        }
                    }
                }
            }

            // 重新计算状态集合
            states = startProb.Keys.ToArray();

            return true;
        }
    }

}
