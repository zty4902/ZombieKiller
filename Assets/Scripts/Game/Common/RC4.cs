using System;
using System.Text;

namespace BarrageGrab.Scripts
{
    /*RC4算法的特点是算法简单，运行速度快，而且密钥长度是可变的，可变范围为1-256字节(8-2048比特)，
     * 在如今技术支持的前提下，当密钥长度为128比特时，用暴力法搜索密钥已经不太可行，所以可以预见
     * RC4的密钥范围任然可以在今后相当长的时间里抵御暴力搜索密钥的攻击。实际上，如今也没有找到对
     * 于128bit密钥长度的RC4加密算法的有效攻击方法。*/
 
    /*RC4加密算法中的几个关键变量：
     
       1、密钥流：RC4算法的关键是根据明文和密钥生成相应的密钥流，密钥流的长度和明文的长度是对应的，
     也就是说明文的长度是500字节，那么密钥流也是500字节。当然，加密生成的密文也是500字节，
     因为密文第i字节=明文第i字节^密钥流第i字节；
       2、状态向量S：长度为256，S[0],S[1].....S[255]。每个单元都是一个字节，算法运行的任何时候，
     S都包括0-255的8比特数的排列组合，只不过值的位置发生了变换；
       3、临时向量T：长度也为256，每个单元也是一个字节。如果密钥的长度是256字节，就直接把密钥
     的值赋给T，否则，轮转地将密钥的每个字节赋给T；
       4、密钥K：长度为1-256字节，注意密钥的长度keylen与明文长度、密钥流的长度没有必然关系，
     通常密钥的长度趣味16字节（128比特）。
     * /
    /*     
        RC4的原理分为三步：
        1、初始化S和T
        2、初始排列S
        3、产生密钥流
     * 
     * */
    public class RC4
    {
 
        public static byte[] Encrypt(byte[] pwd, byte[] data)
        {
            int a, i, j, k, tmp;
            int[] key, box;
            byte[] cipher;
 
            key = new int[256];
            box = new int[256];
            //打乱密码,密码,密码箱长度
            cipher = new byte[data.Length];
            for (i = 0; i < 256; i++)
            {
                key[i] = pwd[i % pwd.Length];
                box[i] = i;
            }
            for (j = i = 0; i < 256; i++)
            {
 
                j = (j + box[i] + key[i]) % 256;
                tmp = box[i];
                box[i] = box[j];
                box[j] = tmp;
            }
            for (a = j = i = 0; i < data.Length; i++)
            {
                a++;
                a %= 256;
                j += box[a];
                j %= 256;
                tmp = box[a];
                box[a] = box[j];
                box[j] = tmp;
                k = box[((box[a] + box[j]) % 256)];
                cipher[i] = (byte)(data[i] ^ k);
            }
            return cipher;
        }
 
        public static byte[] Decrypt(byte[] pwd, byte[] data)
        {
            return Encrypt(pwd, data);
        }
 
 
        internal static string Encrypt(string pwd, string data)
        {
            byte[] key = Encoding.ASCII.GetBytes(pwd);
            //byte[] key = strToToHexByte(pwd);
            byte[] buff = strToToHexByte(data);
            byte[] rlt = Encrypt(key, buff);
 
            return byteToHexStr(rlt);
        }
 
        internal static string Decrypt(string pwd, string data)
        {
            byte[] key = Encoding.ASCII.GetBytes(pwd);
            byte[] buff = strToToHexByte(data);
            byte[] rlt = Decrypt(key, buff);
            return byteToHexStr(rlt);
        }
        /// <summary>
        /// 字符串转16进制字节数组
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        private static byte[] strToToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }
        /// <summary>
        /// 字节数组转16进制字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string byteToHexStr(byte[] bytes)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr += bytes[i].ToString("X2");
                }
            }
            return returnStr;
        }
    }
 
}