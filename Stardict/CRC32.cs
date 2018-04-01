using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stardict
{
    public class CRC32
    {
        private int crc;
        private ulong[] Crc32Table;

        public CRC32()
        {
            GetCRC32Table();
        }
        /// <summary>
        /// 生成CRC32码表
        /// </summary>
        private void GetCRC32Table()
        {
            ulong Crc;
            Crc32Table = new ulong[256];
            int i, j;
            for (i = 0; i < 256; i++)
            {
                Crc = (ulong)i;
                for (j = 8; j > 0; j--)
                {
                    if ((Crc & 1) == 1)
                        Crc = (Crc >> 1) ^ 0xEDB88320;
                    else
                        Crc >>= 1;
                }
                Crc32Table[i] = Crc;
            }
        }
        /// <summary>
        /// 获取字符串的CRC32校验值
        /// </summary>
        /// <param name="sInputString"></param>
        /// <returns></returns>
        public ulong GetCRC32Str(string sInputString)
        {
            byte[] buffer = System.Text.ASCIIEncoding.ASCII.GetBytes(sInputString); 
            ulong value = 0xffffffff;
            int len = buffer.Length;
            for (int i = 0; i < len; i++)
            {
                value = (value >> 8) ^ Crc32Table[(value & 0xFF) ^ buffer[i]];
            }
            return value ^ 0xffffffff;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="b"></param>
        public void Update(int b)
        {
            //crc = Update(crc, b);
        }
    }
}
