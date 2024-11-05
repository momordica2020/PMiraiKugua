
using MMDK.Util;

namespace MMDK.Mods
{

    /// <summary>
    /// 字符串数字论证
    /// </summary>
    public class ModProof : Mod
    {

        Dictionary<string, int> bhdict = new Dictionary<string, int>();
        List<double> tbase = new List<double>();

        /* Maintains the number of ways to decompose the given number. */
        int counter = 0;

        List<string> proofres = new List<string>();
        string finalproof = "";

        /* Maintains the number of calculations performed. */
        int calculation = 0;
        double desired = 0;




        public bool Init(string[] args)
        {
            try
            {
                bhdict = new Dictionary<string, int>();
                var lines = FileManager.ReadResourceLines("Bihua");
                foreach (var line in lines)
                {
                    string[] vitem = line.Split('\t');
                    if (vitem.Length >= 2) bhdict[vitem[0]] = int.Parse(vitem[1]);
                }
            }
            catch (Exception e)
            {
                Logger.Instance.Log(e);
            }
            return true;
        }

        public void Exit()
        {
            
        }





        public bool HandleText(long userId, long groupId, string message, List<string> results)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            if (!message.Trim().StartsWith("数字论证"))
            {
                return false;
            }
            message = message.Replace("数字论证", "").Trim();
            long trynum;
            bool succeed = false;
            if (long.TryParse(message, out trynum))
            {
                succeed = getProof(trynum);
                if (succeed)
                {
                    results.Add(finalproof);
                    return true;
                }
            }


            succeed = getProofEngIndex(message);
            if (succeed)
            {
                results.Add(finalproof);
                return true;
            }


            succeed = getProofBh(message);
            if (succeed)
            {
                results.Add(finalproof);
                return true;
            }


            if (string.IsNullOrWhiteSpace(finalproof))
            {
                results.Add($"论 证 大 失 败");
                return true;
            }
            return false;
        }







        /// <summary>
        /// 获取汉字笔画数
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public int getbh(string word)
        {
            if (bhdict.ContainsKey(word)) return bhdict[word];
            return -1;
        }

        /// <summary>
        /// 英文字母序列的数字论证
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public bool getProofEngIndex(string str)
        {
            string eng = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            List<int> bhs = new List<int>();
            foreach (var word in str)
            {
                if ("\t\r\n []【】！@#￥%…&*（）+=-—_!@#$%^&*()|/\\。、，？?“”\"',".Contains(word)) continue;
                int index = eng.IndexOf(word) % 26 + 1;
                if (index <= 0)
                {
                    // not find!
                    return false;
                }
                bhs.Add(index);
            }
            string desc1 = $"{str} 的字母序号是 {string.Join(",", bhs)}\r\n";
            desc1 += $"{string.Join("+", bhs)} = {bhs.Sum()}\r\n";

            bool proofsuccess = getProof(bhs.Sum());
            if (proofsuccess)
            {
                finalproof = desc1 + finalproof;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 汉字笔画的数字论证
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public bool getProofBh(string str)
        {
            List<int> bhs = new List<int>();
            foreach (var word in str)
            {
                if ("\t\r\n []【】！@#￥%…&*（）+=-—_!@#$%^&*()|/\\。、，？?“”\"',".Contains(word)) continue;
                int bh = getbh(word.ToString());
                if (bh < 0)
                {
                    // not find!
                    return false;
                }
                bhs.Add(bh);
            }
            string desc1 = $"{str} 的笔划是 {string.Join(",", bhs)}\r\n";
            desc1 += $"{string.Join("+", bhs)} = {bhs.Sum()}\r\n";

            bool proofsuccess = getProof(bhs.Sum());
            if (proofsuccess)
            {
                finalproof = desc1 + finalproof;
                return true;
            }
            return false;
        }

        

        /// <summary>
        /// 数字求和，并打印求和表达式字符串
        /// </summary>
        /// <param name="num"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public long getNumSum(long num, out string description)
        {
            string ns = num.ToString();
            long sum = 0;
            description = "";
            foreach (var c in ns)
            {
                description += c + " + ";
                sum += long.Parse(c.ToString());
            }
            if (description.EndsWith("+ ")) description = description.Substring(0, description.Length - 2);
            description += "= " + sum;
            return sum;
        }

        /// <summary>
        /// 整数的数字论证
        /// </summary>
        /// <param name="desired1"></param>
        /// <returns></returns>
        public bool getProof(long desired1)
        {
            desired = desired1;
           // string result = "";

            if (strongProof() > 0)
            {
                // have strong.
                string p1 = proofres[MyRandom.Next(proofres.Count)];
                finalproof = $"{desired} = {p1}\r\nQ.E.D";
                return true;
            }
            else
            {
                // try sum
                try
                {
                    finalproof = "";
                    int time = 5;
                    while (time > 0)
                    {
                        time--;
                        string desc = "";
                        desired = getNumSum((long)desired, out desc);
                        if (finalproof.Length > 0) finalproof += " = ";
                        finalproof += desc;
                        if (strongProof() > 0)
                        {
                            string p1 = proofres[MyRandom.Next(proofres.Count)];
                            finalproof += $" = {p1}\r\nQ.E.D";
                            return true;
                        }
                    }
                }
                catch
                {
                    return false;
                }
            }

            return false;

        }

        int strongProof()
        {
            proofres = new List<string>();
            calculation = 0;
            counter = 0;
            tbase.Clear();
            tbase.Add(1.0);
            tbase.Add(1.0);
            tbase.Add(4.0);
            tbase.Add(5.0);
            tbase.Add(1.0);
            tbase.Add(4.0);
            List<int> array = new List<int>();
            array.Add(0);
            array.Add(0);

            put(array, 11);

            array.Clear();
            array.Add(-1);
            array.Add(0);
            put(array, 11);

            return counter;
        }


        void put(List<int> v, int max_length)
        {
            put(v, 2, 2, 0, max_length);
        }

        //recursively put in numbers and symbols in Reverse Polish Notation (RPN).
        //-1: -1, 0: number, 1: +, 2: -, 3: *, 4: /, 5: ^.
        void put(List<int> v, int pos, int numCounter, int symCounter, int length_)
        {
            //cout << "put called" << endl;
            if (pos == length_)
            {
                if (checker(v))
                {
                    counter++;
                    print(v);
                }
                calculation++;
                return;
            }
            int lower = 0;
            int upper = 6;
            if (numCounter == (length_ + 1) / 2)
            {
                lower = 1;
            }
            if (symCounter == (length_ + 1) / 2 - 1 || symCounter == numCounter - 1)
            {
                upper = 1;
            }
            while (lower < upper)
            {
                if (pos == (int)v.Count)
                    v.Add(lower);
                else if (pos > (int)v.Count)
                    return;
                //cout << "something went wrong" << endl;
                else
                    v[pos] = lower;
                if (lower == 0)
                    put(v, pos + 1, numCounter + 1, symCounter, length_);
                else
                    put(v, pos + 1, numCounter, symCounter + 1, length_);
                lower++;

            }

        }

        //check if the RPN stored in the array gives the desired result.
        bool checker(List<int> seed)
        {

            // double a = 1.0, b = 1.0, c = 4.0, d = 5.0, e = 1.0, f = 4.0;
            //cout << "checker called" << endl;
            Stack<double> myStack = new Stack<double>();
            int sign;
            double firstNum, secondNum;
            int numCount = 0;
            foreach (int i in seed)
            {
                if (i == 0)
                {
                    myStack.Push(tbase[numCount]);
                    numCount++;
                }
                else if (i == -1)
                {
                    myStack.Push(-1 * tbase[0]);
                    numCount++;
                }
                else
                {
                    sign = i;
                    secondNum = myStack.Peek();
                    myStack.Pop();
                    firstNum = myStack.Peek();
                    myStack.Pop();
                    //cout << "myStack size: " << myStack.size() << ", i = " << i << endl;
                    switch (sign)
                    {
                        case 1:
                            myStack.Push(firstNum + secondNum);
                            break;
                        case 2:
                            myStack.Push(firstNum - secondNum);
                            break;
                        case 3:
                            myStack.Push(firstNum * secondNum);
                            break;
                        case 4:
                            myStack.Push(firstNum / secondNum);
                            break;
                        case 5:
                            myStack.Push(Math.Pow(firstNum, secondNum));
                            break;
                    }
                }
            }
            // cout << "checker done" << endl;
            if (myStack.Peek() == desired)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        

        //print RPN.
        string print(List<int> seed)
        {
            // cout << "print called" << endl;

            int numCount = 0;
            // int i;
            string output = "";
            foreach (int i in seed)
            {
                switch (i)
                {
                    case -1:
                        output += ((int)tbase[0]).ToString();
                        output += "- ";
                        numCount++;
                        break;
                    case 0:
                        output += ((int)tbase[numCount]).ToString();
                        output += " ";
                        numCount++;
                        break;
                    case 1:
                        output += "+ ";
                        break;
                    case 2:
                        output += "- ";
                        break;
                    case 3:
                        output += "* ";
                        break;
                    case 4:
                        output += "/ ";
                        break;
                    case 5:
                        output += "^ ";
                        break;
                }
            }
            //cout << output << endl;
            // cout << std::to_string(desired) + "=" + output << endl;
            string resstr = $"{translate(output)}";
            proofres.Add(resstr);
            return $"{desired} = {translate(output)} \r\n";
        }
        string translate(string input)
        {
            // cout << "translate called" << endl;

            string symbol = "+-*/^";

            Stack<string> output = new Stack<string>();
            string str1;
            string str2;
            string outstr = "";
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == ' ')
                {
                    continue;
                }
                if ((int)symbol.IndexOf(input[i]) == -1)
                {
                    string total = "";
                    while (input[i] != ' ')
                    {
                        if (i == 1)
                        {
                            if (input[i] == '-')
                            {
                                total = "-1";
                                break;
                            }
                        }
                        total += input[i].ToString();
                        i++;
                    }
                    output.Push(total);
                }
                else
                {
                    str2 = output.Peek();
                    output.Pop();
                    str1 = output.Peek();
                    output.Pop();
                    if (i == input.Length - 2)
                    {
                        output.Push(str1 + input[i].ToString() + str2);
                    }
                    else
                    {
                        output.Push("(" + str1 + input[i].ToString() + str2 + ")");
                    }
                }
            }
            return output.Peek();
        }
    }


}
