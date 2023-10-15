using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

public unsafe class StringTable
{
    StringTableLive[] lives;
    char Zero = '\0';  // '"';

    public unsafe StringTable(Dictionary<string, int> strs, char end = '\0')
    {
        Zero = end;
        int max = 0;
        Dictionary<int, Dictionary<string, int>> dict = new Dictionary<int, Dictionary<string, int>>();
        foreach (var item in strs)
        {
            if (!dict.ContainsKey(item.Key.Length))
            {
                dict[item.Key.Length] = new Dictionary<string, int>();
            }
            dict[item.Key.Length][item.Key] = item.Value;
            if (max < item.Key.Length)
            {
                max = item.Key.Length;
            }
        }

        lives = new StringTableLive[max + 1];
        foreach (var item in dict)
        {
            lives[item.Key] = new StringTableLive(item.Value, item.Key, end);
        }
    }
    public int Find(char* d, int length)
    {
        StringTableLive stringTableLive = lives[length];
        if (stringTableLive == null)
        {
            return -1;
        }
        return stringTableLive.Run(d);
    }

    internal unsafe class StringTableLive
    {
        unsafe ~StringTableLive()
        {
            if (isOne)
            {
                Marshal.FreeHGlobal(allCharPtr);
            }
            else
            {
                Marshal.FreeHGlobal(stackIntPtr);
                Marshal.FreeHGlobal(allCharPtr);
            }
        }
        internal IntPtr stackIntPtr;
        internal IntPtr allCharPtr;
        internal Chake* chakes;
        internal int num;
        int oneData;
        bool isOne = false;
        char* oneChar;


        public unsafe StringTableLive(Dictionary<string, int> stringToIndex, int length, char end)
        {
            if (stringToIndex.Count == 1)
            {
                isOne = true;
                num = length;
                oneData = stringToIndex.First().Value;
                allCharPtr = Marshal.AllocHGlobal((length + 1) * sizeof(char));
                oneChar = (char*)allCharPtr.ToPointer();
                string str = stringToIndex.First().Key;
                for (int i = 0; i < str.Length; i++)
                {
                    oneChar[i] = str[i];
                }
                oneChar[length] = end;
                return;
            }
            string[] strs = new string[stringToIndex.Count];
            Dictionary<string, int> allCharIndex = new Dictionary<string, int>();
            int idd = 0;
            foreach (var item in stringToIndex)
            {
                allCharIndex[item.Key] = idd;
                strs[idd] = item.Key;
                idd++;
            }

            ChakeNode root = new ChakeNode();
            root.strs = stringToIndex.Keys.ToArray();
            root.Division();

            List<ChakeNode> list = new List<ChakeNode>();
            List<ChakeNode> list2 = new List<ChakeNode>();
            root.Assignment(list);
            int chakeLength = 0;
            foreach (var item in list)
            {
                if (item.strs.Length == 1)
                {
                    item.dataIndex = stringToIndex[item.strs[0]];
                    item.charIndex = allCharIndex[item.strs[0]];
                }
                if (item.isLast == false)
                {
                    item.index = list2.Count;
                    list2.Add(item);
                    ++chakeLength;
                }
            }
            num = length;
            int size = num + 1;
            stackIntPtr = Marshal.AllocHGlobal(chakeLength * Marshal.SizeOf(typeof(Chake)));
            chakes = (Chake*)stackIntPtr.ToPointer();

            allCharPtr = Marshal.AllocHGlobal(stringToIndex.Count * size * sizeof(char));
            char* allChar = (char*)allCharPtr.ToPointer();

            for (int i = 0; i < stringToIndex.Count; i++)
            {
                for (int j = 0; j < size - 1; j++)
                {
                    char v = strs[i][j];
                    allChar[i * size + j] = v;
                    //*(allChar + i * size + j) = strs[i][j];
                }
                //*(allChar + i * size + size - 1) = Mo;
                allChar[i * size + size - 1] = end;
            }


            ChakeTest[] chakes2 = new ChakeTest[chakeLength];
            for (int i = 0; i < chakes2.Length; i++)
            {
                chakes2[i] = new ChakeTest();
                chakes2[i].testIndex = i;
            }
            for (int i = 0; i < list2.Count; i++)
            {
                ChakeNode node = list2[i];
                chakes2[i].chakeIndex = node.chakeIndex;
                chakes2[i].chakeValue = node.chakeValue;
                if (node.next == null)
                {
                    chakes2[i].next = null;
                    chakes2[i].data = allChar + node.charIndex;
                    chakes2[i].dataIndex = node.dataIndex;
                }
                else
                {
                    chakes2[i].next = chakes2[node.next.index];
                }
                if (node.no.next == null && node.no.no == null)
                {
                    chakes2[i].no = null;
                    chakes2[i].noData = allChar + node.no.charIndex;
                    chakes2[i].noDataIndex = node.no.dataIndex;
                }
                else
                {
                    if (node.no.isLast)
                    {
                        chakes2[i].no = chakes2[node.no.next.index];
                    }
                    else
                    {
                        chakes2[i].no = chakes2[node.no.index];
                    }
                }
            }

            for (int i = 0; i < list2.Count; i++)
            {
                ChakeNode node = list2[i];
                chakes[i].chakeIndex = node.chakeIndex;
                chakes[i].chakeValue = node.chakeValue;
                if (node.next == null)
                {
                    chakes[i].next = null;
                    chakes[i].data = allChar + size * node.charIndex;
                    chakes[i].dataIndex = node.dataIndex;
                }
                else
                {
                    chakes[i].next = &chakes[node.next.index];
                }
                if (node.no.next == null && node.no.no == null)
                {
                    chakes[i].no = null;
                    chakes[i].noData = allChar + size * node.no.charIndex;
                    chakes[i].noDataIndex = node.no.dataIndex;
                }
                else
                {
                    if (node.no.isLast)
                    {
                        chakes[i].no = &chakes[node.no.next.index];
                    }
                    else
                    {
                        chakes[i].no = &chakes[node.no.index];
                    }
                }
            }

        }

        internal unsafe struct Chake
        {
            public int chakeIndex;
            public char chakeValue;

            public Chake* next;
            public Chake* no;

            public char* data;
            public char* noData;

            public int dataIndex;
            public int noDataIndex;
        }

        internal unsafe class ChakeTest
        {
            public int chakeIndex;
            public char chakeValue;

            public ChakeTest next;
            public ChakeTest no;

            public char* data;
            public char* noData;

            public int dataIndex;
            public int noDataIndex;
            public int testIndex;
        }

        public unsafe int Run(char* cha)
        {
            if (isOne)
            {
                return oneData;
                //if (EqualsHelper(oneChar, cha))
                //{
                //    return oneData;
                //}
                //return -1;
            }
            Chake* now = &chakes[0];
            while (true)
            {
                if (cha[now->chakeIndex] == now->chakeValue)
                {
                    if (now->next == null)
                    {
                        return now->dataIndex;
                        //if (EqualsHelper(now->data, cha))
                        //{
                        //    return now->dataIndex;
                        //}
                        //else
                        //{
                        //    return -1;
                        //}
                    }
                    else
                    {
                        now = now->next;
                    }
                }
                else
                {
                    if (now->no == null)
                    {
                        return now->noDataIndex;
                        //if (EqualsHelper(now->noData, cha))
                        //{
                        //    return now->noDataIndex;
                        //}
                        //else
                        //{
                        //    return -1;
                        //}
                    }
                    else
                    {
                        now = now->no;
                    }
                }
            }
        }


        private unsafe bool EqualsHelper(char* ptr, char* ptr3)
        {
            long* ptr2 = (long*)ptr;
            long* ptr4 = (long*)ptr3;
            while (num >= 12)
            {
                if (*ptr2 != *ptr4)
                {
                    return false;
                }
                if (*(ptr2 + 1) != *(ptr4 + 1))
                {
                    return false;
                }
                if (*(ptr2 + 2) != *(ptr4 + 2))
                {
                    return false;
                }
                ptr2 += 3;
                ptr4 += 3;
                num -= 12;
            }
            while (num >= 4)
            {
                if (*ptr2 != *ptr4)
                {
                    return false;
                }
                ++ptr2;
                ++ptr4;
                num -= 4;
            }
            if (num == 0)
            {
                return true;
            }
            if (num == 3)
            {
                if (*(int*)ptr2 != *(int*)ptr4)
                {
                    return false;
                }
                if (*((int*)ptr2 + 1) != *((int*)ptr4 + 1))
                {
                    return false;
                }
            }
            else
            {
                if (*(int*)ptr2 != *(int*)ptr4)
                {
                    return false;
                }
            }
            return true;
        }

        internal class ChakeNode
        {
            public List<ChakeNode> nodes = new List<ChakeNode>();
            public int chakeIndex = -1;
            public char chakeValue;
            public ChakeNode next;
            public ChakeNode no;
            public bool isLast;
            public string[] strs;
            public int index = 0;
            public int dataIndex = -1;
            public int charIndex = -1;
            int maxIndex = 0;
            int size = 0;
            public ChakeNode()
            {

            }

            public override string ToString()
            {
                if (isLast)
                {
                    return chakeValue + " " + chakeIndex + " last";
                }
                return chakeValue + " " + chakeIndex;
            }
            public ChakeNode(char chakeValue, int chakeIndex)
            {
                this.chakeValue = chakeValue;
                this.chakeIndex = chakeIndex;
            }


            struct Data
            {
                public string str;
                public int index;
                public Data(string str, int index)
                {
                    this.str = str;
                    this.index = index;
                }
            }

            public void Division()
            {
                //int nowIndex = chakeIndex;
                //Dictionary<char, List<string>> pairs = new Dictionary<char, List<string>>();
                size = strs[0].Length;
                //do
                //{
                //    nowIndex++;
                //    pairs = new Dictionary<char, List<string>>();
                //    for (int i = 0; i < strs.Length; i++)
                //    {
                //        char key = strs[i][nowIndex];
                //        if (pairs.ContainsKey(key))
                //        {
                //            pairs[key].Add(strs[i]);
                //        }
                //        else
                //        {
                //            pairs[key] = new List<string>();
                //            pairs[key].Add(strs[i]);
                //        }
                //    }
                //} while (pairs.Count == 1);



                int allMax = -1;
                for (int i = 0; i < size; i++)
                {
                    int max = 0;
                    //char maxChar = ' ';
                    Dictionary<char, int> pairs2 = new Dictionary<char, int>();
                    for (int j = 0; j < strs.Length; j++)
                    {
                        char key = strs[j][i];

                        if (pairs2.ContainsKey(key))
                        {
                            int d = pairs2[key] += 1;
                            if (max < d)
                            {
                                max = d;
                                //maxChar = key;
                            }
                        }
                        else
                        {
                            pairs2[key] = 0;
                        }
                    }
                    if (pairs2.Count == 1)
                    {
                    }
                    else
                    {
                        if (allMax < max)
                        {
                            allMax = max;
                            maxIndex = i;
                        }
                    }
                }


                Dictionary<char, List<string>> pairs = new Dictionary<char, List<string>>();
                pairs = new Dictionary<char, List<string>>();
                for (int i = 0; i < strs.Length; i++)
                {
                    char key = strs[i][maxIndex];
                    if (pairs.ContainsKey(key))
                    {
                        pairs[key].Add(strs[i]);
                    }
                    else
                    {
                        pairs[key] = new List<string>();
                        pairs[key].Add(strs[i]);
                    }
                }
                pairs = pairs.OrderByDescending(x => x.Value.Count).ToDictionary(x => x.Key, x => x.Value);



                foreach (var item in pairs)
                {
                    ChakeNode chakeNode = new ChakeNode();
                    //chakeNode.chakeIndex = nowIndex;
                    chakeNode.chakeIndex = maxIndex;
                    chakeNode.chakeValue = item.Key;
                    chakeNode.strs = item.Value.ToArray();
                    if (chakeNode.strs.Length > 1)
                    {
                        chakeNode.Division();
                    }
                    nodes.Add(chakeNode);
                }
            }

            public void Assignment(List<ChakeNode> list)
            {
                if (nodes.Count > 0)
                {
                    for (int i = 0; i < nodes.Count - 1; i++)
                    {
                        nodes[i].no = nodes[i + 1];
                        if (nodes[i].nodes.Count > 0)
                        {
                            nodes[i].next = nodes[i].nodes[0];
                        }
                        list.Add(nodes[i]);
                        nodes[i].Assignment(list);
                    }

                    if (nodes[nodes.Count - 1].nodes.Count > 0)
                    {
                        nodes[nodes.Count - 1].next = nodes[nodes.Count - 1].nodes[0];
                    }
                    list.Add(nodes[nodes.Count - 1]);
                    nodes[nodes.Count - 1].isLast = true;
                    nodes[nodes.Count - 1].Assignment(list);
                }
            }


        }

    }
}
