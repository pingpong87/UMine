using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ProbabilisticMine
{
    public partial class ProgressForm : Form
    {
        public ProgressForm(BackgroundWorker backgroundWorker1)
        {
            InitializeComponent();

            this.backgroundWorker1 = backgroundWorker1;
            this.backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);
            this.backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
        }

        private BackgroundWorker backgroundWorker1; //ProcessForm 窗体事件(进度条窗体)
        void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else if (e.Cancelled)
            {
                this.Close();//执行完之后，直接关闭页面
            }
            else
            {
                Thread.Sleep(10000);
               // Console.WriteLine("backgroundWorker1_RunWorkerCompleted:数据处理结束");
                MessageBox.Show("完成执行!!");
                this.Close();//执行完之后，直接关闭页面
            }
            
        }

        void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //this.progressBar1.Value = e.ProgressPercentage;
            //this.textBox1.AppendText(e.UserState.ToString());//主窗体传过来的值，通过e.UserState.ToString()来接受
            this.progressBar1.Value = e.ProgressPercentage;
            this.textBox1.Text += e.UserState.ToString(); //主窗体传过来的值，通过e.UserState.ToString()来接受

            textBox1.ScrollToCaret();
            this.textBox1.Focus();//获取焦点
            this.textBox1.Select(this.textBox1.TextLength, 0);//光标定位到文本最后
            this.textBox1.ScrollToCaret();//滚动到光标处
        }

        private void button1_Click(object sender, EventArgs e)
        {
            cancel();
        }

        private void cancel()
        {
            DialogResult dr;
            dr = MessageBox.Show("你确定取消数据加载", "系统提示", MessageBoxButtons.YesNoCancel,
                     MessageBoxIcon.Warning);
            if (dr == DialogResult.Yes)
            {
                this.backgroundWorker1.CancelAsync();
                this.button1.Enabled = false;
                this.Close();
            }
        }
    }
}
