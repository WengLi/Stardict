using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;

namespace Stardict
{
    public partial class Stardict : Form
    {
        private FileStream IdxStream = null;
        private FileStream DictStream = null;
        private DictInfo DictInfo = new DictInfo();
        private string InfoPath = "E:\\DOC\\stardict-stardict1.3-2.4.2\\stardict1.3.ifo";// ifo文件路径
        private string IdxPath = "E:\\DOC\\stardict-stardict1.3-2.4.2\\stardict1.3.idx";// idx文件路径
        private string DictPath = "E:\\DOC\\stardict-stardict1.3-2.4.2\\stardict1.3.dict";// dict文件路径
        private List<DictIndex> dictIndexList = new List<DictIndex>();
        private bool CanChange = true;
        public Stardict()
        {
            InitializeComponent();
            this.FormClosed += Disposable;
            KeyWord.TextChanged += KeyWord_TextChanged;
            KeyWord.KeyDown += KeyWord_KeyDown;
            LoadDictFile();
        }

        private void KeyWord_TextChanged(object sender, EventArgs e)
        {
            int pos = BinarySearch(dictIndexList, KeyWord.Text);
            if (pos != -1)
            {
                TextBox.Text = ReadData((int)dictIndexList[pos].offset, (int)dictIndexList[pos].size);
            }
            if (!CanChange) return;
            ListBox.Items.Clear();
            List<DictIndex> idxList = Query(KeyWord.Text);
            ListBox.Items.AddRange(idxList.Select(o => o.word).ToArray());
            ListBox.SelectedItem = ListBox.Items.Cast<string>().FirstOrDefault(o => o.ToLower() == KeyWord.Text.ToLower());
        }

        private void KeyWord_KeyDown(object sender, KeyEventArgs e)
        {
            if (ListBox.Items.Count == 0)
                return;
            switch (e.KeyValue)
            {
                case 38:
                    if (ListBox.SelectedIndex > 0)
                    {
                        ListBox.SelectedIndex -= 1;
                        KeyWord.Text = ListBox.SelectedItem.ToString();
                    }
                    break;
                case 40:
                    if (ListBox.SelectedIndex + 1 <= ListBox.Items.Count - 1)
                    {
                        ListBox.SelectedIndex += 1;
                        KeyWord.Text = ListBox.SelectedItem.ToString();
                    }
                    break;
                default:
                    CanChange = true;
                    break;
            }
        }

        private void Disposable(object sender, FormClosedEventArgs e)
        {
            IdxStream.Close();
            IdxStream.Dispose();
            DictStream.Close();
            DictStream.Dispose();
        }

        private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            CanChange = false;
            KeyWord.Text = ListBox.SelectedItem.ToString();
        }

        #region Input File
        private void LoadDictFile()
        {
            using (StreamReader reader = new StreamReader(InfoPath, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] info = line.Split('=');
                    if (info[0] == "bookname")
                    {
                        DictInfo.BookName = info[1];
                    }
                    else if (info[0] == "wordcount")
                    {
                        DictInfo.WordCount = info[1];
                    }
                    else if (info[0] == "idxfilesize")
                    {
                        DictInfo.IdxFileSize = info[1];
                    }
                }
                TextBox.Text = DictInfo.ToString();
            }
            IdxStream = new FileStream(IdxPath, FileMode.Open, FileAccess.Read);
            dictIndexList = GetIndex();
            dictIndexList.Sort((x, y) =>
            {
                return CompareTo(x.word, y.word);
            });
            Decompress(new FileInfo("E:\\DOC\\stardict-stardict1.3-2.4.2\\stardict1.3.dict.dz"));
            DictStream = new FileStream(DictPath, FileMode.Open, FileAccess.Read);
        }
        #endregion

        #region index
        private int CompareTo(string x, string y)
        {
            int len1 = x.Length;
            int len2 = y.Length;
            int lim = Math.Min(len1, len2);
            char[] v1 = x.ToArray();
            char[] v2 = y.ToArray();
            int k = 0;
            while (k < lim)
            {
                char c1 = v1[k];
                char c2 = v2[k];
                if (c1 != c2)
                {
                    return c1 - c2;
                }
                k++;
            }
            return len1 - len2;
        }

        private List<DictIndex> Query(string word)
        {
            List<DictIndex> idxList = new List<DictIndex>();
            List<int> posList = BinaryQuery(dictIndexList, word).OrderBy(o => o).ToList();
            for (int j = 0; j < posList.Count; j++)
            {
                idxList.Add(dictIndexList[posList[j]]);
            }
            return idxList;
        }

        /// <summary>
        /// 二分查找算法
        /// </summary>
        /// <returns></returns>
        private int BinarySearch(List<DictIndex> dictIndexList, string word)
        {
            int low = 0;
            int high = dictIndexList.Count - 1;
            while ((low <= high) && (low <= dictIndexList.Count - 1) && (high <= dictIndexList.Count - 1))
            {
                int middle = (high + low) >> 1;
                DictIndex midVal = dictIndexList[middle];
                if (CompareTo(midVal.word, word) < 0)
                {
                    low = middle + 1;
                }
                else if (CompareTo(midVal.word, word) > 0)
                {
                    high = middle - 1;
                }
                else
                {
                    return middle;
                }
            }
            return -1;
        }

        /// <summary>
        /// 二分法模糊搜索
        /// </summary>
        /// <param name="dictIndexList"></param>
        /// <param name="word"></param>
        /// <returns></returns>
        private List<int> BinaryQuery(List<DictIndex> dictIndexList, string word)
        {
            int low = 0;
            int high = dictIndexList.Count - 1;
            List<int> simVal = new List<int>();
            while ((low <= high) && (low <= dictIndexList.Count - 1) && (high <= dictIndexList.Count - 1))
            {
                int middle = (high + low) >> 1;
                DictIndex midVal = dictIndexList[middle];
                if (midVal.word.ToLower().StartsWith(word.ToLower()))
                {
                    simVal.Add(middle);
                }
                if (CompareTo(midVal.word, word) < 0)
                {
                    low = middle + 1;
                }
                else if (CompareTo(midVal.word, word) > 0)
                {
                    high = middle - 1;
                }
                else
                {
                    return simVal;
                }
            }
            return simVal;
        }

        private List<DictIndex> GetIndex()
        {
            //List<DictIndex> dictIndexList = new List<DictIndex>();
            // the maximun length of a word  must less 256
            // 256 bytes(word) + 1 byte('\0') + 4 bytes(offset) + 4 bytes(def size)
            byte[] bytes = new byte[256 + 1 + 4 + 4];
            int currentPos = 0;
            while (IdxStream.Read(bytes, 0, bytes.Length) > 0)
            {
                int j = 0;
                bool isWordPart = true;
                bool isOffsetPart = false;
                bool isSizePart = false;
                string word = null;
                long offset = 0;        // offset of a word in data file
                long size = 0;          // size of word's defition
                int wordLength = 0;     // the byte(s) length of a word
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (isWordPart)
                    {
                        if (bytes[i] == 0)
                        {
                            wordLength = i;
                            word = System.Text.Encoding.UTF8.GetString(bytes, j, i - j);
                            j = i;
                            isWordPart = false;
                            isOffsetPart = true;
                        }
                        continue;
                    }
                    if (isOffsetPart)
                    {
                        i += 3;// skip the split token: '\0'
                        j++; 
                        if (i >= bytes.Length)
                        {
                            i = bytes.Length - 1;
                        }
                        offset = ByteConverter.Unsigned4BytesToInt(bytes, j);
                        j = i + 1;
                        isOffsetPart = false;
                        isSizePart = true;
                        continue;
                    }
                    if (isSizePart)
                    {
                        i += 3;
                        if (i >= bytes.Length)
                        {
                            i = bytes.Length - 1;
                        }
                        size = ByteConverter.Unsigned4BytesToInt(bytes, j);
                        j = i + 1;
                        isSizePart = false;
                        isWordPart = true;
                    }
                    DictIndex dictIndex = new DictIndex();
                    dictIndex.word = word;
                    dictIndex.offset = offset;
                    dictIndex.size = size;
                    dictIndexList.Add(dictIndex);

                    // skip current index entry
                    int indexSize = wordLength + 1 + 4 + 4;
                    IdxStream.Seek(indexSize + currentPos, SeekOrigin.Begin);
                    currentPos += indexSize;
                    break;
                }
            }
            return dictIndexList;
        }
        #endregion

        #region dict
        /// <summary>
        /// gzip解压
        /// </summary>
        /// <param name="fileToDecompress"></param>
        /// <returns></returns>
        public void Decompress(FileInfo fileToDecompress)
        {
            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                string currentFileName = fileToDecompress.FullName;
                string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);
                if (File.Exists(newFileName))
                    return;
                using (FileStream decompressedFileStream = File.Create(newFileName))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                    }
                }
            }
        }
        /// <summary>
        /// 读取字典数据
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public string ReadData(int offset, int length)
        {
            string result = string.Empty;
            DictStream.Position = offset;
            byte[] bytes = new byte[length];
            int v = DictStream.Read(bytes, 0, bytes.Length);
            result = System.Text.Encoding.UTF8.GetString(bytes).Replace("\0", Environment.NewLine);
            return result;
        }
        #endregion
    }

    public class DictIndex
    {
        public String word;
        public long offset;
        public long size;
        public override string ToString()
        {
            return word + "\t" + offset + "\t" + size;
        }
    }

    public class ByteConverter
    {
        /// <summary>
        /// convert unsigned one byte into a 32-bit integer
        /// </summary>
        /// <param name="b">byte</param>
        /// <returns>convert result</returns>
        public static int UnsignedByteToInt(byte b)
        {
            return (int)b & 0xFF;
        }

        /// <summary>
        ///  convert unsigned one byte into a hexadecimal digit
        /// </summary>
        /// <param name="b">byte</param>
        /// <returns>convert result</returns>
        public static String byteToHex(byte b)
        {
            int i = b & 0xFF;
            return i.ToString("0x");
        }

        /// <summary>
        /// convert unsigned 4 bytes into a 32-bit integer
        /// </summary>
        /// <param name="buf">bytes buffer</param>
        /// <param name="pos">beginning <code>byte</code> for converting</param>
        /// <returns>convert result</returns>
        public static long Unsigned4BytesToInt(byte[] buf, int pos)
        {
            int ret = 0;

            ret += UnsignedByteToInt(buf[pos++]) << 24;
            ret += UnsignedByteToInt(buf[pos++]) << 16;
            ret += UnsignedByteToInt(buf[pos++]) << 8;
            ret += UnsignedByteToInt(buf[pos++]) << 0;

            return ret;
        }


    }

    public class DictInfo 
    {
        public String BookName;
        public String WordCount;
        public String IdxFileSize;
        public override string ToString()
        {
            return "Book Name: " + BookName +
                "\r\nWord Count: " + WordCount +
                "\r\nIndex File Size: " + IdxFileSize;
        }
    }
}
