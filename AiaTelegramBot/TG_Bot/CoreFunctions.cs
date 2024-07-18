using AiaTelegramBot.Logging;
using AiaTelegramBot.TG_Bot.models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
namespace AiaTelegramBot.TG_Bot
{
    internal class CoreFunctions
    {
        public static byte[] GetHash(string inputString)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }
        public static string? GetHashString(string inputString)
        {
            if (string.IsNullOrEmpty(inputString)) return null;
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));
            return sb.ToString();
        }
        public static int GetRandom(int minValue, int maxValue)
        {
            int randomValue = 0;
            if (minValue < maxValue)
            {
                randomValue = System.Security.Cryptography.RandomNumberGenerator.GetInt32(minValue, maxValue);
            }
            return randomValue;
        }
        public static double StringCompare(string a, string b)
        {
            if (a == b) //Same string, no iteration needed.
                return 100;
            if ((a.Length == 0) || (b.Length == 0)) //One is empty, second is not
            {
                return 0;
            }
            double maxLen = a.Length > b.Length ? a.Length : b.Length;
            int minLen = a.Length < b.Length ? a.Length : b.Length;
            int sameCharAtIndex = 0;
            for (int i = 0; i < minLen; i++) //Compare char by char
            {
                if (a[i] == b[i])
                {
                    sameCharAtIndex++;
                }
            }
            return sameCharAtIndex / maxLen * 100;
        }
    }
}
