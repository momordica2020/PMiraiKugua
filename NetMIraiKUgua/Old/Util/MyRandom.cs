using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MMDK.Util
{
    class MyRandom
    {
        private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create(); // 静态实例
        // 生成范围在 min 和 max 之间的（高随机度的）随机整数
        public static int Next(int minValue, int maxValue)
        {
            try
            {
                if (minValue < int.MinValue) minValue = int.MinValue;
                if (maxValue > int.MaxValue) maxValue = int.MaxValue;
                if (minValue == maxValue) return minValue;
                if (minValue > maxValue)
                {
                    int tmp = minValue;
                    minValue = maxValue;
                    maxValue = tmp;
                }

                // 计算生成的随机范围
                int range = maxValue - minValue;

                // 使用 byte 数组存储随机字节
                byte[] randomNumber = new byte[4]; // 4 字节可以表示一个 32 位整数
                _rng.GetBytes(randomNumber); // 填充随机字节

                // 将字节转换为无符号整数
                uint uintRandomNumber = BitConverter.ToUInt32(randomNumber, 0);

                // 返回范围内的随机整数
                return (int)(uintRandomNumber % (uint)range) + minValue; // 使用模运算限制范围并偏移
            }
            catch (Exception ex)
            {

            }
            return minValue;


        }
        // 生成随机整数（不带范围）
        public static int Next()
        {
            return Next(0, int.MaxValue);
        }



        public static int Next(int maxValue)
        {
            return Next(0, maxValue);
        }
    }
}
