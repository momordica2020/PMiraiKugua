using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.IO.Compression;

namespace MMDK.Util
{
    /// <summary>
    /// 本地文件管理模块
    /// </summary>
    class FileManager
    {
        public enum FileType
        {
            TextRaw,
            TextWithCompress,
            LinesRaw,
            LinesWithCompress,
        }
        public static Encoding encoding = Encoding.UTF8;

        /// <summary>
        /// 读一个文件全文进来
        /// </summary>
        /// <param name="resourceName">注意，是资源名称，会从配置文件json里找对应路径</param>
        /// <param name="IsCompress">true表示是压缩文件，会用默认方式将之解压先</param>
        /// <returns></returns>
        public static string ReadResource(string resourceName, bool IsCompress=false)
        {
            try
            {
                string realPath = Config.Instance.ResourceFullPath(resourceName);
                return Read(realPath, IsCompress);
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
            }
            return "";
        }

        /// <summary>
        /// 读一个文件全文进来
        /// </summary>
        /// <param name="fullPath">完整路径</param>
        /// <param name="IsCompress">true表示是压缩文件，会用默认方式将之解压先</param>
        /// <returns></returns>
        public static string Read(string fullPath, bool IsCompress = false)
        {
            try
            {
                if (File.Exists(fullPath))
                {
                    string result = "";
                    if (IsCompress)
                    {
                        // 压缩文件，将之解压先
                        using (FileStream fileStream = new FileStream(fullPath, FileMode.Open))
                        using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                        using (StreamReader reader = new StreamReader(gzipStream))
                        {
                            result = reader.ReadToEnd();
                        }
                    }
                    else
                    {
                        using (FileStream fileStream = new FileStream(fullPath, FileMode.Open))
                        using (StreamReader reader = new StreamReader(fileStream))
                        {
                            result = reader.ReadToEnd();
                        }
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
            }
            return "";
        }

        /// <summary>
        /// 读一个文件并拆分每行成list
        /// </summary>
        /// <param name="resourceName">注意，是资源名称，会从配置文件json里找对应路径</param>
        /// <param name="IsCompress">true表示是压缩文件，会用默认方式将之解压先</param>
        /// <returns></returns>
        public static IEnumerable<string> ReadResourceLines(string resourceName, bool IsCompress = false)
        {
            try
            {
                string rawData = ReadResource(resourceName, IsCompress);
                if (!string.IsNullOrWhiteSpace(rawData))
                {
                    return rawData.Split( '\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
            }
            return new List<string>();
        }


        public static string[] ReadLines(string file)
        {
            List<string> res = new List<string>();

            try
            {
                using(FileStream fs = new FileStream(file, FileMode.OpenOrCreate))
                {
                    using(StreamReader sr=new StreamReader(fs, encoding))
                    {
                        while (!sr.EndOfStream)
                        {
                            string r = sr.ReadLine().Trim();
                            if (!string.IsNullOrWhiteSpace(r))
                            {
                                res.Add(r);
                            }
                            
                        }
                        
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Instance.Log(ex);
            }

            return res.ToArray();
        }

       

        public static void writeText(string file, string text)
        {
            try
            {
                File.WriteAllText(file, text, encoding);
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(ex);
            }
        }


        public static void writeLines(string file, IEnumerable<string> lines)
        {
            try
            {
                File.WriteAllLines(file, lines, encoding);
            }
            catch(Exception ex)
            {
                Logger.Instance.Log(ex);
            }
        }


        /// <summary>
        /// 解压缩文件
        /// </summary>
        /// <param name="compressedFilePath"></param>
        /// <param name="decompressedFilePath"></param>
        public static void DecompressFile(string compressedFilePath, string decompressedFilePath)
        {
            using (FileStream fileStream = new FileStream(compressedFilePath, FileMode.Open))
            using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
            using (FileStream outputFileStream = new FileStream(decompressedFilePath, FileMode.Create))
            {
                gzipStream.CopyTo(outputFileStream);
            }
        }

        /// <summary>
        /// 压缩文件
        /// </summary>
        /// <param name="decompressedFilePath"></param>
        /// <param name="compressedFilePath"></param>
        public static void CompressFile(string decompressedFilePath, string compressedFilePath)
        {
            using (FileStream inputFileStream = new FileStream(decompressedFilePath, FileMode.Open))
            using (FileStream fileStream = new FileStream(compressedFilePath, FileMode.Create))
            using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Compress))
            {
                inputFileStream.CopyTo(gzipStream);
            }
        }


    }
}
