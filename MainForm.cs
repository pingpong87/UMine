using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace ProbabilisticMine
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

        }

        //===================================global parameters======================================
        List<Instance>[] arList;//store the data set
        double minProb; //probability threshold
        double minErro; //error threshold
        double minPrev;//prevalence threshold
        Dictionary<ID, List<double>> dicPR;
        List<Pattern> Probcol;//probabilistic prevalent colocation
        int limitl;
        bool[] isTabIns;

        //=========================== page one Data Management====================================
        //The data generated
        private void createbutt_Click(object sender, EventArgs e)
        {
            int feaNum = Convert.ToInt32(FeaNumBox.Text.Trim()); //特征个数
            int instanceNum = Convert.ToInt32(InstanceNumBox.Text.Trim());  //实例个数
           
            //Console.WriteLine("feaNum:{0};instanceNum:{1};expectationNum:{2};varianceNum:{3}", 
                //feaNum, instanceNum, expectationNum, varianceNum); //test

            //for循环对Instance实例化，并放到arList列表中
            arList = new List<Instance>[feaNum];
           //double[] arrPr = Gauss.arrPr(expectationNum, varianceNum, feaNum* instanceNum);  //获取符合正太分布的一组概率值
            Random ran = new Random();
            for (int i=0; i<feaNum; i++)
            {
                List<Instance> instList = new List<Instance>();
                SolidBrush myBrush = new SolidBrush(Util.pointColor(i));
                for (int j = 0; j < instanceNum; j++)
                {
                    double x = Math.Round(ran.NextDouble() * 2000,2);
                    double y = Math.Round(ran.NextDouble() * 2000,2);
                    double pr = Math.Round(ran.NextDouble(), 2);
                    Instance inst = new Instance(i, j, x, y, pr);
                    //Console.WriteLine("{0},{1},{2},{3},{4}", inst.getfea(), inst.getins(), inst.getx(), inst.gety(), inst.getpr()); //test
                   // PointToPicture.DrawPoint(coordinate, 2000,2000,inst.getx(), inst.gety(), myBrush);
                    instList.Add(inst);
                }
                arList[i] = instList;
            }
            Util.setTable(DataView, arList);
            //绘制坐标系
            PointToPicture.DrawPicture(coordinate, DataView.DataSource as DataTable);
            savaBut.Visible = true;
        }

        private void FlashBtn_Click(object sender, EventArgs e)
        {
            PointToPicture.DrawPicture(coordinate, DataView.DataSource as DataTable);
        }

        //保存数据
        private void savaBut_Click(object sender, EventArgs e)
        {
            DataTable dt = DataView.DataSource as DataTable;

            //Console.WriteLine("arList length:{0}  arList count:{1}", arList.Length, arList[0].Count);//测试数据是否贯穿context
            SaveFileDialog sfd = new SaveFileDialog();
            StreamWriter myStream;
            sfd.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            sfd.FilterIndex = 2;
            sfd.RestoreDirectory = true;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                myStream = new StreamWriter(sfd.FileName);
                foreach (DataRow dr in dt.Rows)
                {
                    myStream.WriteLine("{0},{1},{2},{3}", dr["fea"].ToString(), dr["x"].ToString(), dr["y"].ToString(), dr["pr"].ToString());

                }
                myStream.Close();//关闭流
            }
        }

        //============================ open data file ============================================
        private void OpenFilebut_Click(object sender, EventArgs e)
        {
            string mindata;
            string[] linedata;
            int midindex = 0;
            int insNo = 0;

            arList = new List<Instance>[Convert.ToInt32(FeaNumBox2.Text)];         /*   store the data set. 
                                                                                   * array are used to store feature, 
                                                                                   * and the element of the array are used to store the feature's instances */
            
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                InstanceNumBox2.Text = openFileDialog1.FileName;
                FileStream fs = File.OpenRead(openFileDialog1.FileName);
                StreamReader str = new StreamReader(fs);
                List<Instance> newlist = new List<Instance>();

                mindata = str.ReadLine();

                while (mindata != null)
                {
                    linedata = mindata.Split(',');
                    mindata = str.ReadLine();

                    if (Convert.ToInt64(linedata[0]) == midindex)
                    {
                        Instance ins = new Instance(midindex, insNo++, Convert.ToSingle(linedata[1]), Convert.ToSingle(linedata[2]), Convert.ToSingle(linedata[3]));
                        newlist.Add(ins);
                    }
                    else
                    {
                        arList[midindex++] = newlist;
                        newlist = new List<Instance>();
                        insNo = 0;
                        Instance ins = new Instance(midindex, insNo++, Convert.ToSingle(linedata[1]), Convert.ToSingle(linedata[2]), Convert.ToSingle(linedata[3]));
                        newlist.Add(ins);
                    }

                }
                arList[midindex] = newlist;
                //Console.WriteLine("打开文件：arList length:{0}  arList count:{1}", arList.Length, arList[0].Count);//测试数据是否贯穿context
            }
        }

        //=============================   go, mining algorithm     ================================================
        string resultbox;
       
        private void Miningbutt_Click(object sender, EventArgs e)
        {
            //Console.WriteLine("点击go按钮");
            this.Result_tBox.Text = "";

            BackgroundWorker bw = this.backgroundWorker1;
            bw.RunWorkerAsync(); // 运行 backgroundWorker 组件
            ProgressForm form = new ProgressForm(bw);// 显示进度条窗体
            form.ShowDialog(this);
            form.Close();

            this.Result_tBox.Text = resultbox;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {

            double[] exppr;
            double prev;
            bool needPrun;
            int i, j, k = 1;

            exppr = new double[arList.Length];
            Probcol = new List<Pattern>();
            needPrun = true;
            minProb = Convert.ToDouble(this.ProbBox.Text);
            //minFalse = Convert.ToDouble(this.falseBox.Text);
            minPrev = Convert.ToDouble(this.MinPrevBox.Text);
            minErro = Convert.ToDouble(this.errorBox.Text);
            prev = (minPrev * minProb) / 3.0;
            resultbox = "";
            //initialize the candidate patterns      

            for (i = 0; i < arList.Length; i++)
            {
                List<int> feaList = new List<int>();
                feaList.Add(i);
                Pattern untiPatt = new Pattern(feaList); //1-size colocation pattern's feature list.
                Probcol.Add(untiPatt);
            }
            //the mining process  
            //Console.WriteLine("数据处理中");
            BackgroundWorker worker = sender as BackgroundWorker;
            while (Probcol.Count > 0 && k < arList.Length)
            {

                double jd = Math.Round(k * 1.0 / arList.Length, 2) * 100;
                //Console.WriteLine("k阶：" + (k+1).ToString());
                //Console.WriteLine("数据处理：arList length:{0}  arList count:{1}", arList.Length, arList[0].Count);//测试数据是否贯穿context            
                //Console.WriteLine("进度值：" + jd);

                worker.ReportProgress((int)jd, "processing"+ k.ToString() + "-size patterns ...\r\n"); //注意：这里向子窗体返回信息值，这里是两个值，一个用于进度条，一个用于文本框的。
                if (worker.CancellationPending)  // 如果用户取消则跳出处理数据代码
                {
                    e.Cancel = true;
                    break;
                }
                List<Pattern> candicol = new List<Pattern>();
                int[] fea = new int[k + 1];

                //generate the (k+1)-size candidate patterns by k-size prevalent patterns             
                for (i = 0; i < Probcol.Count - 1; i++)
                    for (j = i + 1; j < Probcol.Count; j++)
                    {
                        Pattern newPatt;
                        if (Probcol[i].firstEqual(Probcol[j]))
                        {
                            newPatt = new Pattern(Probcol[i].merge(Probcol[j]).getFeaList());
                            if (checkedCandi(newPatt, Probcol)) candicol.Add(newPatt);
                        }
                        else break;
                    }

                //clear the k-size prevalent patterns
                Probcol.Clear();

                if (candicol.Count != 0)
                {

                    foreach (Pattern oneCandi in candicol)
                    {
                        gen_Neibtree(oneCandi);

                        needPrun = true;
                        for (j = 0; j < k + 1; j++)
                            fea[j] = oneCandi.getOneFea(j);
                        oneCandi.insList = new List<List<int>>();

                        /*
                         * generate table instances of pattern oneCandi
                         *   */
                        #region generate table instances of pattern oneCandi
                        if (fea.Length == 2)//2-size patterns' table instances 
                        {
                            for (i = 0; i < arList[fea[0]].Count; i++)
                            {
                                for (j = 0; j < arList[fea[0]][i].neighbor.Count; j++)
                                {
                                    List<int> insList = new List<int>();
                                    insList.Add(i);
                                    insList.Add(arList[fea[0]][i].neighbor[j].getins());
                                    oneCandi.insList.Add(insList);
                                }
                                arList[fea[0]][i].neighbor.Clear();
                            }
                        }
                        else//generate table instances of patterns whose length greater than 2
                        {
                            for (i = 0; i < arList[fea[0]].Count; i++)
                            {
                                if (arList[fea[0]][i].neighbor.Count >= k)
                                {
                                    foreach (Instance neiIns in arList[fea[0]][i].neighbor)
                                    {
                                        if (neiIns.getfea() == fea[1])
                                        {
                                            List<int> resultList = new List<int>();
                                            resultList.Add(i);
                                            arList[fea[0]][i].colocation = new List<Instance>();
                                            foreach (Instance ins in arList[fea[0]][i].neighbor)
                                            {
                                                Instance ins2 = new Instance(ins.getfea(), ins.getins());
                                                arList[fea[0]][i].colocation.Add(ins2);
                                            }
                                            gen_OneIns(neiIns, resultList, k, fea, oneCandi);
                                            arList[fea[0]][i].colocation.Clear();
                                        }
                                    }
                                }
                                arList[fea[0]][i].neighbor.Clear();
                            }
                        }//else
                        #endregion


                        if (oneCandi.insList.Count != 0)
                        {
                            if (expButt.Checked == true)
                            {
                                #region expectations colocation mining
                                dicPR = new Dictionary<ID, List<double>>();
                                for (i = 0; i < k + 1; i++)
                                {
                                    oneCandi.worldId = new List<List<int>>();
                                    prevCoLocation(oneCandi, i, 0, 0);
                                    oneCandi.worldId.Clear();
                                }

                                List<ID> keyList = new List<ID>();
                                foreach (ID key in dicPR.Keys)
                                    keyList.Insert(0, key);
                                foreach (ID key in keyList)
                                {
                                    for (i = 0; i < key.worldId.Count; i++)
                                    {
                                        for (j = 0; j < arList[fea[i]].Count; j++)
                                        {
                                            List<List<int>> newKey = new List<List<int>>();


                                            if (j >= key.worldId[i].Count || key.worldId[i][j] > j)
                                            {

                                                for (int r = 0; r < key.worldId.Count; r++)
                                                {
                                                    List<int> myList = new List<int>();
                                                    foreach (int midInt in key.worldId[r])
                                                        myList.Add(midInt);
                                                    if (r == i) myList.Insert(j, j);
                                                    newKey.Add(myList);
                                                }
                                            }
                                            else if (key.worldId[i][j] == -1)
                                            {
                                                for (int r = 0; r < key.worldId.Count; r++)
                                                {
                                                    List<int> myList = new List<int>();
                                                    if (r == i) myList.Add(j);
                                                    else
                                                    {
                                                        foreach (int midInt in key.worldId[r])
                                                            myList.Add(midInt);
                                                    }
                                                    newKey.Add(myList);
                                                }
                                            }

                                            if (newKey.Count != 0)
                                            {
                                                ID fatherId = new ID(newKey);
                                                List<double> result = new List<double>();
                                                result = dicPR[key];
                                                double worldPR = dicPR[fatherId][fea.Length];
                                                worldPR = worldPR / arList[fea[i]][j].getpr();
                                                worldPR = worldPR * (1.0 - arList[fea[i]][j].getpr());
                                                result.Add(worldPR);
                                                dicPR[key] = result;
                                                break;
                                            }

                                        }
                                        if (j < arList[fea[i]].Count) break;
                                    }
                                    if (i == key.worldId.Count)
                                    {
                                        double worldPR = 1;

                                        for (int r = 0; r < fea.Length; r++)
                                            for (j = 0; j < arList[fea[r]].Count; j++)
                                                worldPR = worldPR * arList[fea[r]][j].getpr();
                                        List<double> result = new List<double>();
                                        result = dicPR[key];
                                        result.Add(worldPR);
                                        dicPR[key] = result;
                                    }
                                }

                                //final checking step
                                double EPI = 0,PW=0;
                                foreach (ID key in dicPR.Keys)
                                {
                                    double r = min_Prev(dicPR[key]);//  r是该可能世界下的参与度
                                    EPI = EPI + r * dicPR[key][fea.Length];//dicPR[key][fea.Length]是概率
                                    if (r > 0) PW = PW + dicPR[key][fea.Length];
                                }
                                oneCandi.insList.Clear();
                                if (EPI >= minPrev * PW)
                                    Probcol.Add(oneCandi);
                                keyList.Clear();
                                dicPR.Clear();

#endregion
                            }
                            if (probButt.Checked == true)
                            {
                                //pruning using lemma 5
                                #region pruning using lemma 5
                                for (i = 0; i < arList.Length; i++)
                                {
                                    exppr[i] = 0;
                                    for (j = 0; j < arList[i].Count; j++)
                                        exppr[i] = exppr[i] + arList[i][j].getpr();
                                }

                                for (i = 0; i < k + 1; i++)
                                {
                                    int[] flag = new int[arList[fea[i]].Count];
                                    double exp = 0;

                                    for (j = 0; j < arList[fea[i]].Count; j++)
                                        flag[j] = 0;
                                    for (j = 0; j < oneCandi.insList.Count; j++)
                                        flag[oneCandi.insList[j][i]] = 1;
                                    for (j = 0; j < arList[fea[i]].Count; j++)
                                        exp = exp + flag[j] * arList[fea[i]][j].getpr();

                                    if (exp / exppr[fea[i]] < prev)
                                    {
                                        needPrun = false;
                                        break;
                                    }
                                }
                                #endregion
                                if (needPrun == true)
                                {
                                    /*
                                     * Dynamic Programming
                                     */
                                    #region Dynamic Programming
                                    dicPR = new Dictionary<ID, List<double>>();
                                    for (i = 0; i < k + 1; i++)
                                    {
                                        oneCandi.worldId = new List<List<int>>();
                                        prevCoLocation(oneCandi, i, 0, 0);
                                        oneCandi.worldId.Clear();
                                    }

                                    List<ID> keyList = new List<ID>();
                                    foreach (ID key in dicPR.Keys)
                                        keyList.Insert(0, key);
                                    foreach (ID key in keyList)
                                    {
                                        for (i = 0; i < key.worldId.Count; i++)
                                        {
                                            for (j = 0; j < arList[fea[i]].Count; j++)
                                            {
                                                List<List<int>> newKey = new List<List<int>>();


                                                if (j >= key.worldId[i].Count || key.worldId[i][j] > j)
                                                {

                                                    for (int r = 0; r < key.worldId.Count; r++)
                                                    {
                                                        List<int> myList = new List<int>();
                                                        foreach (int midInt in key.worldId[r])
                                                            myList.Add(midInt);
                                                        if (r == i) myList.Insert(j, j);
                                                        newKey.Add(myList);
                                                    }
                                                }
                                                else if (key.worldId[i][j] == -1)
                                                {
                                                    for (int r = 0; r < key.worldId.Count; r++)
                                                    {
                                                        List<int> myList = new List<int>();
                                                        if (r == i) myList.Add(j);
                                                        else
                                                        {
                                                            foreach (int midInt in key.worldId[r])
                                                                myList.Add(midInt);
                                                        }
                                                        newKey.Add(myList);
                                                    }
                                                }

                                                if (newKey.Count != 0)
                                                {
                                                    ID fatherId = new ID(newKey);
                                                    List<double> result = new List<double>();
                                                    result = dicPR[key];
                                                    double worldPR = dicPR[fatherId][fea.Length];
                                                    worldPR = worldPR / arList[fea[i]][j].getpr();
                                                    worldPR = worldPR * (1.0 - arList[fea[i]][j].getpr());
                                                    result.Add(worldPR);
                                                    dicPR[key] = result;
                                                    break;
                                                }

                                            }
                                            if (j < arList[fea[i]].Count) break;
                                        }
                                        if (i == key.worldId.Count)
                                        {
                                            double worldPR = 1;

                                            for (int r = 0; r < fea.Length; r++)
                                                for (j = 0; j < arList[fea[r]].Count; j++)
                                                    worldPR = worldPR * arList[fea[r]][j].getpr();
                                            List<double> result = new List<double>();
                                            result = dicPR[key];
                                            result.Add(worldPR);
                                            dicPR[key] = result;
                                        }
                                    }

                                    //final checking step
                                    double PPI = 0;
                                    foreach (ID key in dicPR.Keys)
                                    {
                                        double r = min_Prev(dicPR[key]);
                                        if (r >= minPrev) PPI = PPI + dicPR[key][fea.Length];
                                    }
                                    oneCandi.insList.Clear();
                                    if (PPI > minProb)
                                        Probcol.Add(oneCandi);
                                    keyList.Clear();
                                    dicPR.Clear();

                                    #endregion
                                }//if (needPrun == true)
                            }
                            if (approButt.Checked == true)
                            {
                                #region approximate Algorithm
                                int min = 0;
                                List<int>[] sortedFea = new List<int>[arList.Length];
                                int minCount = arList[fea[0]].Count;
                                for (i = 0; i < arList.Length; i++)
                                    sortedFea[i] = bubbleSort(i);

                                for (i = 0; i < fea.Length - 1; i++)
                                    if (minCount > arList[fea[i + 1]].Count)
                                    {
                                        minCount = arList[fea[i + 1]].Count;
                                        min = i + 1;
                                    }

                                double midl = 0;
                                //================================l的取值
                                 midl = minPrev * minPrev * minProb * minProb;
                                limitl = Convert.ToInt32(3.0 * System.Math.Log(1 / minErro, System.Math.E) / midl);

                                List<ID> resultList = new List<ID>();
                                List<List<int>> result = new List<List<int>>();
                                for (j = 0; j < min; j++)
                                {
                                    List<int> myList = new List<int>();
                                    myList.Add(sortedFea[fea[j]][0]);
                                    result.Add(myList);
                                }
                                List<int> myList1 = new List<int>();
                                myList1.Add(-1);
                                result.Add(myList1);
                                for (j = min + 1; j < fea.Length; j++)
                                {
                                    List<int> myList = new List<int>();
                                    myList.Add(sortedFea[fea[j]][0]);
                                    result.Add(myList);
                                }
                                ID newid = new ID(result);
                                newid.isnIndex = new List<int>();
                                for (j = 0; j < fea.Length; j++)
                                    newid.isnIndex.Add(0);
                                resultList.Add(newid);


                                double returnList = 0;

                                returnList = comp_OneSet(min, fea, sortedFea, oneCandi, newid, returnList);

                                i = 0;
                                while (i < fea.Length && returnList <= minProb && limitl > 0)
                                {
                                    if (i != min)
                                    {
                                        int a = resultList.Count;
                                        for (j = 0; j < a; j++)
                                        {
                                            int indexi = (resultList[j].isnIndex[i] + 1) % arList[fea[i]].Count;

                                            while (indexi > 0 && returnList <= minProb && limitl > 0)
                                            {
                                                List<int> myNewList = new List<int>();
                                                if (resultList[j].worldId[i].Contains(sortedFea[fea[i]][indexi])) { break; }
                                                else
                                                    myNewList.Add(sortedFea[fea[i]][indexi]);


                                                List<List<int>> newResult = new List<List<int>>(resultList[j].worldId);

                                                newResult.RemoveAt(i);
                                                newResult.Insert(i, myNewList);


                                                ID myId = new ID(newResult);
                                                myId.isnIndex = new List<int>(resultList[j].isnIndex);
                                                myId.isnIndex.RemoveAt(i);
                                                myId.isnIndex.Insert(i, indexi);
                                                returnList = comp_OneSet(min, fea, sortedFea, oneCandi, myId, returnList);
                                                resultList.Add(myId);

                                                indexi = (indexi + 1) % arList[fea[i]].Count;
                                                newResult.Clear();

                                            }

                                        }
                                    }
                                    i++;
                                }


                                oneCandi.insList.Clear();
                                if (returnList > minProb)
                                {
                                    Probcol.Add(oneCandi);

                                }

                                resultList.Clear();
                                #endregion
                            }

                        }// if (oneCandi.insList != null)

                    }////for every candi 

                }//if (oneCandi.insList != null)//if have new candi

                if (Probcol.Count != 0)
                {
                    resultbox += (k + 1).ToString() + "-patterns , total " + Probcol.Count.ToString()+System.Environment.NewLine;
                    foreach (Pattern patt in Probcol)
                    {
                        string boxstr = "";
                        for (i = 0; i < patt.getFeaList().Count; i++)
                            boxstr += patt.getOneFea(i).ToString() + " ";
                        resultbox += boxstr + System.Environment.NewLine;
                    }
                }
                //Console.WriteLine(resultbox);
                if (k + 1 == arList.Length)
                {
                    worker.ReportProgress((int)100, "Complete all prevalent patterns !\r\n");
                }
                k = k + 1;
                candicol.Clear();
            }

            //Console.WriteLine("jd：" + Math.Round((k + 1) * 1.0 / arList.Length, 2) * 100);
            if ((int)(Math.Round((k + 1) * 1.0 / arList.Length, 2) * 100) <= 100)
            {
                worker.ReportProgress(100, "Complete all prevalent patterns !\r\n");
            }
        }

        private void expButt_CheckedChanged(object sender, EventArgs e)
        {
            this.DistBox.ReadOnly = false;
            this.MinPrevBox.ReadOnly = false;
            this.ProbBox.ReadOnly = true;
            this.errorBox.ReadOnly = true;
        }

        private void probButt_CheckedChanged(object sender, EventArgs e)
        {
            this.DistBox.ReadOnly = false;
            this.MinPrevBox.ReadOnly = false;
            this.ProbBox.ReadOnly = false;
            this.errorBox.ReadOnly = true;
        }

        private void approButt_CheckedChanged(object sender, EventArgs e)
        {
            this.DistBox.ReadOnly = false;
            this.MinPrevBox.ReadOnly = false;
            this.ProbBox.ReadOnly = false;
            this.errorBox.ReadOnly = false;
        }

        private void ResultSaveBtn_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            StreamWriter myStream;
            sfd.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            sfd.FilterIndex = 2;
            sfd.RestoreDirectory = true;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                myStream = new StreamWriter(sfd.FileName);
                string resultText = this.Result_tBox.Text;
                myStream.WriteLine(resultText);
                myStream.Close();//关闭流
            }
        }

        private void coordinate_Paint(object sender, PaintEventArgs e)
        {
            PointToPicture.DrawPicture(this.coordinate, DataView.DataSource as DataTable);
        }

        private void AnalysisLoad_Click(object sender, EventArgs e)
        {
            string mindata;
            string[] linedata;
            int midindex = 0;
            int insNo = 0;

            arList = new List<Instance>[Convert.ToInt32(FeaNumBox3.Text)];

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                DataFilePath.Text = openFileDialog1.FileName;
                FileStream fs = File.OpenRead(openFileDialog1.FileName);
                StreamReader str = new StreamReader(fs);
                List<Instance> newlist = new List<Instance>();

                mindata = str.ReadLine();

                while (mindata != null)
                {
                    linedata = mindata.Split(',');
                    mindata = str.ReadLine();

                    if (Convert.ToInt64(linedata[0]) == midindex)
                    {
                        Instance ins = new Instance(midindex, insNo++, Convert.ToSingle(linedata[1]), Convert.ToSingle(linedata[2]), Convert.ToSingle(linedata[3]));
                        newlist.Add(ins);
                    }
                    else
                    {
                        arList[midindex++] = newlist;
                        newlist = new List<Instance>();
                        insNo = 0;
                        Instance ins = new Instance(midindex, insNo++, Convert.ToSingle(linedata[1]), Convert.ToSingle(linedata[2]), Convert.ToSingle(linedata[3]));
                        newlist.Add(ins);
                    }

                }
                arList[midindex] = newlist;
            }
        }

        string resultInf;
        float[] times;

        public void expming()
        {
            double[] exppr;
           
            int i, j, k = 1;

            exppr = new double[arList.Length];
            Probcol = new List<Pattern>();
            
            
            minPrev = Convert.ToDouble(this.PrevBox2.Text);
           
            

            resultInf = "";
            //initialize the candidate patterns      

            for (i = 0; i < arList.Length; i++)
            {
                List<int> feaList = new List<int>();
                feaList.Add(i);
                Pattern untiPatt = new Pattern(feaList); //1-size colocation pattern's feature list.
                Probcol.Add(untiPatt);
            }
            //the mining process  
           
            
            while (Probcol.Count > 0 && k < arList.Length)
            {


                List<Pattern> candicol = new List<Pattern>();
                int[] fea = new int[k + 1];

                //generate the (k+1)-size candidate patterns by k-size prevalent patterns             
                for (i = 0; i < Probcol.Count - 1; i++)
                    for (j = i + 1; j < Probcol.Count; j++)
                    {
                        Pattern newPatt;
                        if (Probcol[i].firstEqual(Probcol[j]))
                        {
                            newPatt = new Pattern(Probcol[i].merge(Probcol[j]).getFeaList());
                            if (checkedCandi(newPatt, Probcol)) candicol.Add(newPatt);
                        }
                        else break;
                    }

                //clear the k-size prevalent patterns
                Probcol.Clear();

                if (candicol.Count != 0)
                {

                    foreach (Pattern oneCandi in candicol)
                    {
                        gen_Neibtree(oneCandi);

                        
                        for (j = 0; j < k + 1; j++)
                            fea[j] = oneCandi.getOneFea(j);
                        oneCandi.insList = new List<List<int>>();

                        /*
                         * generate table instances of pattern oneCandi
                         *   */
                        #region generate table instances of pattern oneCandi
                        if (fea.Length == 2)//2-size patterns' table instances 
                        {
                            for (i = 0; i < arList[fea[0]].Count; i++)
                            {
                                for (j = 0; j < arList[fea[0]][i].neighbor.Count; j++)
                                {
                                    List<int> insList = new List<int>();
                                    insList.Add(i);
                                    insList.Add(arList[fea[0]][i].neighbor[j].getins());
                                    oneCandi.insList.Add(insList);
                                }
                                arList[fea[0]][i].neighbor.Clear();
                            }
                        }
                        else//generate table instances of patterns whose length greater than 2
                        {
                            for (i = 0; i < arList[fea[0]].Count; i++)
                            {
                                if (arList[fea[0]][i].neighbor.Count >= k)
                                {
                                    foreach (Instance neiIns in arList[fea[0]][i].neighbor)
                                    {
                                        if (neiIns.getfea() == fea[1])
                                        {
                                            List<int> resultList = new List<int>();
                                            resultList.Add(i);
                                            arList[fea[0]][i].colocation = new List<Instance>();
                                            foreach (Instance ins in arList[fea[0]][i].neighbor)
                                            {
                                                Instance ins2 = new Instance(ins.getfea(), ins.getins());
                                                arList[fea[0]][i].colocation.Add(ins2);
                                            }
                                            gen_OneIns(neiIns, resultList, k, fea, oneCandi);
                                            arList[fea[0]][i].colocation.Clear();
                                        }
                                    }
                                }
                                arList[fea[0]][i].neighbor.Clear();
                            }
                        }//else
                        #endregion


                        if (oneCandi.insList.Count != 0)
                        {

                           
                            #region expectations colocation mining
                            dicPR = new Dictionary<ID, List<double>>();
                            for (i = 0; i < k + 1; i++)
                            {
                                oneCandi.worldId = new List<List<int>>();
                                prevCoLocation(oneCandi, i, 0, 0);
                                oneCandi.worldId.Clear();
                            }

                            List<ID> keyList = new List<ID>();
                            foreach (ID key in dicPR.Keys)
                                keyList.Insert(0, key);
                            foreach (ID key in keyList)
                            {
                                for (i = 0; i < key.worldId.Count; i++)
                                {
                                    for (j = 0; j < arList[fea[i]].Count; j++)
                                    {
                                        List<List<int>> newKey = new List<List<int>>();


                                        if (j >= key.worldId[i].Count || key.worldId[i][j] > j)
                                        {

                                            for (int r = 0; r < key.worldId.Count; r++)
                                            {
                                                List<int> myList = new List<int>();
                                                foreach (int midInt in key.worldId[r])
                                                    myList.Add(midInt);
                                                if (r == i) myList.Insert(j, j);
                                                newKey.Add(myList);
                                            }
                                        }
                                        else if (key.worldId[i][j] == -1)
                                        {
                                            for (int r = 0; r < key.worldId.Count; r++)
                                            {
                                                List<int> myList = new List<int>();
                                                if (r == i) myList.Add(j);
                                                else
                                                {
                                                    foreach (int midInt in key.worldId[r])
                                                        myList.Add(midInt);
                                                }
                                                newKey.Add(myList);
                                            }
                                        }

                                        if (newKey.Count != 0)
                                        {
                                            ID fatherId = new ID(newKey);
                                            List<double> result = new List<double>();
                                            result = dicPR[key];
                                            double worldPR = dicPR[fatherId][fea.Length];
                                            worldPR = worldPR / arList[fea[i]][j].getpr();
                                            worldPR = worldPR * (1.0 - arList[fea[i]][j].getpr());
                                            result.Add(worldPR);
                                            dicPR[key] = result;
                                            break;
                                        }

                                    }
                                    if (j < arList[fea[i]].Count) break;
                                }
                                if (i == key.worldId.Count)
                                {
                                    double worldPR = 1;

                                    for (int r = 0; r < fea.Length; r++)
                                        for (j = 0; j < arList[fea[r]].Count; j++)
                                            worldPR = worldPR * arList[fea[r]][j].getpr();
                                    List<double> result = new List<double>();
                                    result = dicPR[key];
                                    result.Add(worldPR);
                                    dicPR[key] = result;
                                }
                            }

                            //final checking step
                            double EPI = 0, PW = 0;
                            foreach (ID key in dicPR.Keys)
                            {
                                double r = min_Prev(dicPR[key]);//  r是该可能世界下的参与度
                                EPI = EPI + r * dicPR[key][fea.Length];//dicPR[key][fea.Length]是概率
                                if (r > 0) PW = PW + dicPR[key][fea.Length];
                            }
                            oneCandi.insList.Clear();
                            if (EPI >= minPrev * PW)
                                Probcol.Add(oneCandi);
                            keyList.Clear();
                            dicPR.Clear();
                            #endregion

                        }// if (oneCandi.insList != null)

                    }////for every candi 

                }//if (oneCandi.insList != null)//if have new candi

                if (Probcol.Count != 0)
                {
                    modenum[k, 0] = "";
                    resultInf ="算法1："+ (k + 1).ToString() + "阶模式,共" + Probcol.Count.ToString() + "个" + System.Environment.NewLine;
                    foreach (Pattern patt in Probcol)
                    {
                        string boxstr = "";
                        for (i = 0; i < patt.getFeaList().Count; i++)
                            boxstr += patt.getOneFea(i).ToString() + " ";

                        resultInf += boxstr + System.Environment.NewLine;
                        modenum[k,0] += boxstr + System.Environment.NewLine;
                    }
                    Console.WriteLine(resultInf);
                }
                

                k = k + 1;
                candicol.Clear();
            }
        }
        public void proming()
        {
            double[] exppr;
            double prev;
            bool needPrun;
            int i, j, k = 1;

           

            exppr = new double[arList.Length];
            Probcol = new List<Pattern>();
            needPrun = true;
            minProb = Convert.ToDouble(this.ProbBox2.Text);
            //minFalse = Convert.ToDouble(this.falseBox.Text);
            minPrev = Convert.ToDouble(this.PrevBox2.Text);
            minErro = Convert.ToDouble(this.ErrBox2.Text);
            prev = (minPrev * minProb) / 3.0;
            resultInf = "";
            //initialize the candidate patterns      

            for (i = 0; i < arList.Length; i++)
            {
                List<int> feaList = new List<int>();
                feaList.Add(i);
                Pattern untiPatt = new Pattern(feaList); //1-size colocation pattern's feature list.
                Probcol.Add(untiPatt);
            }
            //the mining process  

            while (Probcol.Count > 0 && k < arList.Length)
            {
               
                List<Pattern> candicol = new List<Pattern>();
                int[] fea = new int[k + 1];

                //generate the (k+1)-size candidate patterns by k-size prevalent patterns             
                for (i = 0; i < Probcol.Count - 1; i++)
                    for (j = i + 1; j < Probcol.Count; j++)
                    {
                        Pattern newPatt;
                        if (Probcol[i].firstEqual(Probcol[j]))
                        {
                            newPatt = new Pattern(Probcol[i].merge(Probcol[j]).getFeaList());
                            if (checkedCandi(newPatt, Probcol)) candicol.Add(newPatt);
                        }
                        else break;
                    }

                //clear the k-size prevalent patterns
                Probcol.Clear();

                if (candicol.Count != 0)
                {
                    
                    foreach (Pattern oneCandi in candicol)
                    {
                        gen_Neibtree(oneCandi);

                        needPrun = true;
                        for (j = 0; j < k + 1; j++)
                            fea[j] = oneCandi.getOneFea(j);
                        oneCandi.insList = new List<List<int>>();

                        /*
                         * generate table instances of pattern oneCandi
                         *   */
                        #region generate table instances of pattern oneCandi
                        if (fea.Length == 2)//2-size patterns' table instances 
                        {
                            for (i = 0; i < arList[fea[0]].Count; i++)
                            {
                                for (j = 0; j < arList[fea[0]][i].neighbor.Count; j++)
                                {
                                    List<int> insList = new List<int>();
                                    insList.Add(i);
                                    insList.Add(arList[fea[0]][i].neighbor[j].getins());
                                    oneCandi.insList.Add(insList);
                                }
                                arList[fea[0]][i].neighbor.Clear();
                            }
                        }
                        else//generate table instances of patterns whose length greater than 2
                        {
                            for (i = 0; i < arList[fea[0]].Count; i++)
                            {
                                if (arList[fea[0]][i].neighbor.Count >= k)
                                {
                                    foreach (Instance neiIns in arList[fea[0]][i].neighbor)
                                    {
                                        if (neiIns.getfea() == fea[1])
                                        {
                                            List<int> resultList = new List<int>();
                                            resultList.Add(i);
                                            arList[fea[0]][i].colocation = new List<Instance>();
                                            foreach (Instance ins in arList[fea[0]][i].neighbor)
                                            {
                                                Instance ins2 = new Instance(ins.getfea(), ins.getins());
                                                arList[fea[0]][i].colocation.Add(ins2);
                                            }
                                            gen_OneIns(neiIns, resultList, k, fea, oneCandi);
                                            arList[fea[0]][i].colocation.Clear();
                                        }
                                    }
                                }
                                arList[fea[0]][i].neighbor.Clear();
                            }
                        }//else
                        #endregion


                        if (oneCandi.insList.Count != 0)
                        {
                            //pruning using lemma 5
                            #region pruning using lemma 5
                            DateTime alg2starttime = System.DateTime.Now;
                            for (i = 0; i < arList.Length; i++)
                            {
                                exppr[i] = 0;
                                for (j = 0; j < arList[i].Count; j++)
                                    exppr[i] = exppr[i] + arList[i][j].getpr();
                            }

                            for (i = 0; i < k + 1; i++)
                            {
                                int[] flag = new int[arList[fea[i]].Count];
                                double exp = 0;

                                for (j = 0; j < arList[fea[i]].Count; j++)
                                    flag[j] = 0;
                                for (j = 0; j < oneCandi.insList.Count; j++)
                                    flag[oneCandi.insList[j][i]] = 1;
                                for (j = 0; j < arList[fea[i]].Count; j++)
                                    exp = exp + flag[j] * arList[fea[i]][j].getpr();

                                if (exp / exppr[fea[i]] < prev)
                                {
                                    needPrun = false;
                                    break;
                                }
                            }
                            #endregion
                            if (needPrun == true)
                            {
                                /*
                                 * Dynamic Programming
                                 */
                                #region Dynamic Programming
                                dicPR = new Dictionary<ID, List<double>>();
                                for (i = 0; i < k + 1; i++)
                                {
                                    oneCandi.worldId = new List<List<int>>();
                                    prevCoLocation(oneCandi, i, 0, 0);
                                    oneCandi.worldId.Clear();
                                }

                                List<ID> keyList = new List<ID>();
                                foreach (ID key in dicPR.Keys)
                                    keyList.Insert(0, key);
                                foreach (ID key in keyList)
                                {
                                    for (i = 0; i < key.worldId.Count; i++)
                                    {
                                        for (j = 0; j < arList[fea[i]].Count; j++)
                                        {
                                            List<List<int>> newKey = new List<List<int>>();


                                            if (j >= key.worldId[i].Count || key.worldId[i][j] > j)
                                            {

                                                for (int r = 0; r < key.worldId.Count; r++)
                                                {
                                                    List<int> myList = new List<int>();
                                                    foreach (int midInt in key.worldId[r])
                                                        myList.Add(midInt);
                                                    if (r == i) myList.Insert(j, j);
                                                    newKey.Add(myList);
                                                }
                                            }
                                            else if (key.worldId[i][j] == -1)
                                            {
                                                for (int r = 0; r < key.worldId.Count; r++)
                                                {
                                                    List<int> myList = new List<int>();
                                                    if (r == i) myList.Add(j);
                                                    else
                                                    {
                                                        foreach (int midInt in key.worldId[r])
                                                            myList.Add(midInt);
                                                    }
                                                    newKey.Add(myList);
                                                }
                                            }

                                            if (newKey.Count != 0)
                                            {
                                                ID fatherId = new ID(newKey);
                                                List<double> result = new List<double>();
                                                result = dicPR[key];
                                                double worldPR = dicPR[fatherId][fea.Length];
                                                worldPR = worldPR / arList[fea[i]][j].getpr();
                                                worldPR = worldPR * (1.0 - arList[fea[i]][j].getpr());
                                                result.Add(worldPR);
                                                dicPR[key] = result;
                                                break;
                                            }

                                        }
                                        if (j < arList[fea[i]].Count) break;
                                    }
                                    if (i == key.worldId.Count)
                                    {
                                        double worldPR = 1;

                                        for (int r = 0; r < fea.Length; r++)
                                            for (j = 0; j < arList[fea[r]].Count; j++)
                                                worldPR = worldPR * arList[fea[r]][j].getpr();
                                        List<double> result = new List<double>();
                                        result = dicPR[key];
                                        result.Add(worldPR);
                                        dicPR[key] = result;
                                    }
                                }

                                //final checking step
                                double PPI = 0;
                                foreach (ID key in dicPR.Keys)
                                {
                                    double r = min_Prev(dicPR[key]);
                                    if (r >= minPrev) PPI = PPI + dicPR[key][fea.Length];
                                }
                                oneCandi.insList.Clear();
                                if (PPI > minProb)
                                    Probcol.Add(oneCandi);
                                keyList.Clear();
                                dicPR.Clear();

                                #endregion
                            }//if (needPrun == true)
                        }
                    }
                   
                }
                if (Probcol.Count != 0)
                {
                    modenum[k, 1] = "";
                    resultInf = "算法2：" + (k + 1).ToString() + "阶模式,共" + Probcol.Count.ToString() + "个" + System.Environment.NewLine;
                    foreach (Pattern patt in Probcol)
                    {
                        string boxstr = "";
                        for (i = 0; i < patt.getFeaList().Count; i++)
                            boxstr += patt.getOneFea(i).ToString() + " ";
                        resultInf += boxstr + System.Environment.NewLine;
                        modenum[k,1] += boxstr + System.Environment.NewLine;
                    }
                    Console.WriteLine(resultInf);
                }
                
                k = k + 1;
                candicol.Clear();
               
            }
        }

        public void appming()
        {

            double[] exppr;
            double prev;
            int i, j, k = 1;



            exppr = new double[arList.Length];
            Probcol = new List<Pattern>();
            minProb = Convert.ToDouble(this.ProbBox2.Text);
            //minFalse = Convert.ToDouble(this.falseBox.Text);
            minPrev = Convert.ToDouble(this.PrevBox2.Text);
            minErro = Convert.ToDouble(this.ErrBox2.Text);
            prev = (minPrev * minProb) / 3.0;
            resultInf = "";
            //initialize the candidate patterns      

            for (i = 0; i < arList.Length; i++)
            {
                List<int> feaList = new List<int>();
                feaList.Add(i);
                Pattern untiPatt = new Pattern(feaList); //1-size colocation pattern's feature list.
                Probcol.Add(untiPatt);
            }
            //the mining process  

            while (Probcol.Count > 0 && k < arList.Length)
            {

                List<Pattern> candicol = new List<Pattern>();
                int[] fea = new int[k + 1];

                //generate the (k+1)-size candidate patterns by k-size prevalent patterns             
                for (i = 0; i < Probcol.Count - 1; i++)
                    for (j = i + 1; j < Probcol.Count; j++)
                    {
                        Pattern newPatt;
                        if (Probcol[i].firstEqual(Probcol[j]))
                        {
                            newPatt = new Pattern(Probcol[i].merge(Probcol[j]).getFeaList());
                            if (checkedCandi(newPatt, Probcol)) candicol.Add(newPatt);
                        }
                        else break;
                    }

                //clear the k-size prevalent patterns
                Probcol.Clear();

                if (candicol.Count != 0)
                {

                    foreach (Pattern oneCandi in candicol)
                    {
                        gen_Neibtree(oneCandi);
                        
                        for (j = 0; j < k + 1; j++)
                            fea[j] = oneCandi.getOneFea(j);
                        oneCandi.insList = new List<List<int>>();

                        /*
                         * generate table instances of pattern oneCandi
                         *   */
                        #region generate table instances of pattern oneCandi
                        if (fea.Length == 2)//2-size patterns' table instances 
                        {
                            for (i = 0; i < arList[fea[0]].Count; i++)
                            {
                                for (j = 0; j < arList[fea[0]][i].neighbor.Count; j++)
                                {
                                    List<int> insList = new List<int>();
                                    insList.Add(i);
                                    insList.Add(arList[fea[0]][i].neighbor[j].getins());
                                    oneCandi.insList.Add(insList);
                                }
                                arList[fea[0]][i].neighbor.Clear();
                            }
                        }
                        else//generate table instances of patterns whose length greater than 2
                        {
                            for (i = 0; i < arList[fea[0]].Count; i++)
                            {
                                if (arList[fea[0]][i].neighbor.Count >= k)
                                {
                                    foreach (Instance neiIns in arList[fea[0]][i].neighbor)
                                    {
                                        if (neiIns.getfea() == fea[1])
                                        {
                                            List<int> resultList = new List<int>();
                                            resultList.Add(i);
                                            arList[fea[0]][i].colocation = new List<Instance>();
                                            foreach (Instance ins in arList[fea[0]][i].neighbor)
                                            {
                                                Instance ins2 = new Instance(ins.getfea(), ins.getins());
                                                arList[fea[0]][i].colocation.Add(ins2);
                                            }
                                            gen_OneIns(neiIns, resultList, k, fea, oneCandi);
                                            arList[fea[0]][i].colocation.Clear();
                                        }
                                    }
                                }
                                arList[fea[0]][i].neighbor.Clear();
                            }
                        }//else
                        #endregion


                        if (oneCandi.insList.Count != 0)
                        {
                            #region approximate Algorithm
                           
                            int min = 0;
                            List<int>[] sortedFea = new List<int>[arList.Length];
                            int minCount = arList[fea[0]].Count;
                            for (i = 0; i < arList.Length; i++)
                                sortedFea[i] = bubbleSort(i);

                            for (i = 0; i < fea.Length - 1; i++)
                                if (minCount > arList[fea[i + 1]].Count)
                                {
                                    minCount = arList[fea[i + 1]].Count;
                                    min = i + 1;
                                }

                            double midl = 0;
                            //================================l的取值
                            midl = minPrev * minPrev * minProb * minProb;
                            limitl = Convert.ToInt32(3.0 * System.Math.Log(1 / minErro, System.Math.E) / midl);

                            List<ID> resultList = new List<ID>();
                            List<List<int>> result = new List<List<int>>();
                            for (j = 0; j < min; j++)
                            {
                                List<int> myList = new List<int>();
                                myList.Add(sortedFea[fea[j]][0]);
                                result.Add(myList);
                            }
                            List<int> myList1 = new List<int>();
                            myList1.Add(-1);
                            result.Add(myList1);
                            for (j = min + 1; j < fea.Length; j++)
                            {
                                List<int> myList = new List<int>();
                                myList.Add(sortedFea[fea[j]][0]);
                                result.Add(myList);
                            }
                            ID newid = new ID(result);
                            newid.isnIndex = new List<int>();
                            for (j = 0; j < fea.Length; j++)
                                newid.isnIndex.Add(0);
                            resultList.Add(newid);


                            double returnList = 0;

                            returnList = comp_OneSet(min, fea, sortedFea, oneCandi, newid, returnList);

                            i = 0;
                            while (i < fea.Length && returnList <= minProb && limitl > 0)
                            {
                                if (i != min)
                                {
                                    int a = resultList.Count;
                                    for (j = 0; j < a; j++)
                                    {
                                        int indexi = (resultList[j].isnIndex[i] + 1) % arList[fea[i]].Count;

                                        while (indexi > 0 && returnList <= minProb && limitl > 0)
                                        {
                                            List<int> myNewList = new List<int>();
                                            if (resultList[j].worldId[i].Contains(sortedFea[fea[i]][indexi])) { break; }
                                            else
                                                myNewList.Add(sortedFea[fea[i]][indexi]);


                                            List<List<int>> newResult = new List<List<int>>(resultList[j].worldId);

                                            newResult.RemoveAt(i);
                                            newResult.Insert(i, myNewList);


                                            ID myId = new ID(newResult);
                                            myId.isnIndex = new List<int>(resultList[j].isnIndex);
                                            myId.isnIndex.RemoveAt(i);
                                            myId.isnIndex.Insert(i, indexi);
                                            returnList = comp_OneSet(min, fea, sortedFea, oneCandi, myId, returnList);
                                            resultList.Add(myId);

                                            indexi = (indexi + 1) % arList[fea[i]].Count;
                                            newResult.Clear();

                                        }

                                    }
                                }
                                i++;
                            }


                            oneCandi.insList.Clear();
                            if (returnList > minProb)
                            {
                                Probcol.Add(oneCandi);

                            }

                            resultList.Clear();
                            
                            #endregion
                        }
                    }

                }
                if (Probcol.Count != 0)
                {
                    modenum[k, 2] = "";
                    resultInf = "算法3：" + (k + 1).ToString() + "阶模式,共" + Probcol.Count.ToString() + "个" + System.Environment.NewLine;
                    foreach (Pattern patt in Probcol)
                    {
                        string boxstr = "";

                        for (i = 0; i < patt.getFeaList().Count; i++)
                            boxstr += patt.getOneFea(i).ToString() + " ";
                        resultInf += boxstr + System.Environment.NewLine;
                        modenum[k,2] += boxstr + System.Environment.NewLine;
                    }
                    Console.WriteLine(resultInf);
                }

                k = k + 1;
                candicol.Clear();

            }
        }

        string[,] modenum;
        private void AnalysisGoBtn_Click(object sender, EventArgs e)
        {
            //Console.WriteLine("点击AnalysisGoBtn_Click按钮");
            times = new float[3];
            modenum = new string[arList.Length,3];
            PointToPicture.DrawImage(this.histogramPanel, times);

            DataTable dt;
            dt = new DataTable();//建立个数据表
            dt.Columns.Add(new DataColumn("Size", typeof(string)));
            dt.Columns.Add(new DataColumn("Exp_Alg", typeof(string)));//在表中添加int类型的列
            dt.Columns.Add(new DataColumn("Pro_Alg", typeof(string)));
            dt.Columns.Add(new DataColumn("Appr_Alg", typeof(string)));

            DataGridView grv2 = this.dataGridView;
            grv2.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            //设置自动调整高度

            grv2.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;


            if (expCheck.Checked == true)
            {
                DateTime alg1starttime = System.DateTime.Now;
                expming();
                DateTime alg1endtime = System.DateTime.Now;
                TimeSpan ts = alg1endtime - alg1starttime;
                times[0] = (float)ts.TotalSeconds;
                Console.WriteLine("算法1用时：" + ts.TotalSeconds);
            }
            if (proCheck.Checked == true)
            {
                DateTime alg2starttime = System.DateTime.Now;
                proming();
                DateTime alg2endtime = System.DateTime.Now;
                TimeSpan ts = alg2endtime - alg2starttime;
                times[1] = (float)ts.TotalSeconds;
                Console.WriteLine("算法2用时：" + ts.TotalSeconds);
            }
            if (appCheck.Checked == true)
            {
                DateTime alg3starttime = System.DateTime.Now;
                appming();
                DateTime alg3endtime = System.DateTime.Now;
                TimeSpan ts = alg3endtime - alg3starttime;
                times[2] = (float)ts.TotalSeconds;
                Console.WriteLine("算法3用时：" + ts.TotalSeconds);
            }

            DataRow dr;//行
            for (int i = 1; i <arList.Length; i++)
            {
                    dr = dt.NewRow();
                    dr["Size"] = (i+1)+ "-Size";
                    dr["Exp_Alg"] = modenum[i,0];
                    dr["Pro_Alg"] = modenum[i,1];
                    dr["Appr_Alg"] = modenum[i,2];
                    dt.Rows.Add(dr);//在表的对象的行里添加此行
                
            }
            grv2.DataSource = dt;
            PointToPicture.DrawImage(this.histogramPanel, times);

        }
        

        private void histogramPanel_Paint(object sender, PaintEventArgs e)
        {
            //int[] times2 = times;
            PointToPicture.DrawImage(this.histogramPanel, times);
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Please read this article!!");
        }
        

        private void existToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            this.Close();
        }
    }

}
