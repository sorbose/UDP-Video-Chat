using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Configuration;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Threading;
using AForge.Video.DirectShow;
using AForge.Video;
using System.Drawing.Drawing2D;
using System.IO;

namespace TCP_Server
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public bool appRun = true;
        private Listener lis;//监听对象
        private Sender sen;//发送对象
        string chatToIp;
        ushort chatToPort;

        //返回信息
        public void AddMessage(object sender, AddMessageEventArgs e)
        {
            string message = e.mess;
            string appendText;
            string[] sep = message.Split('>');
            string[] sepIp = sep[0].Split('<', ':');
            bool checkIp = true;
            for (int i = 0; i < listBox1.Items.Count; i++)
            {
                if (listBox1.Items[i].ToString() == sepIp[1]+":"+sepIp[2])
                    checkIp = false;
            }
            if (checkIp && sep[1].Trim() != "断开")
            {
                this.listBox1.Items.Add(sepIp[1].Trim()+":"+ sepIp[2]);
                chatToIp = sepIp[1];
                chatToPort = UInt16.Parse( sepIp[2]);
            }

            appendText = sep[0] + ">:           " + System.DateTime.Now.ToString() + Environment.NewLine + sep[1] + Environment.NewLine;
            int txtGetMsgLength = this.richTextBox1.Text.Length;
            this.richTextBox1.AppendText(appendText);
            this.richTextBox1.Select(txtGetMsgLength, appendText.Length - Environment.NewLine.Length * 2 - sep[1].Length);
            this.richTextBox1.SelectionColor = Color.Red;
            this.richTextBox1.ScrollToCaret();
        }

        //下线
        public void IpRemo(object sender, AddMessageEventArgs e)
        {
            //string[] sep = e.mess.Split(':');
            try
            {
                int index = 0;
                for (int i = 0; i < listBox1.Items.Count; i++)
                {
                    string t1 = listBox1.Items[i].ToString();
                    string t2 = e.mess;
                    if (listBox1.Items[i].ToString() == e.mess)
                    {
                        index = i;
                        this.listBox1.Items.RemoveAt(index);
                    }
                }

            }
            catch
            {
                MessageBox.Show("没有这个IP:port");
            }
        }
               
        //启动监听
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            this.start_listen();
            Listener.SERVER_LISTEN_PORT = (ushort)numericUpDown2.Value;
            numericUpDown2.ReadOnly = true;
            comboBox1.Enabled = false;
            this.toolStripStatusLabel2.Text = "监听已启动    ";
            this.toolStripStatusLabel3.Text = "";
        }

        //停止监听
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            numericUpDown2.ReadOnly = false;
            comboBox1.Enabled = true;
            try
            {
                lis.listenerRun = false;
                lis.Stop();
                this.toolStripStatusLabel2.Text = "监听已停止    ";
            }
            catch (NullReferenceException)
            { }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            cam = new VideoCaptureDevice(videoDevices[0].MonikerString);
            string ips = GetMyIpAddress().Trim();
            this.label1.Text = "本主机IP是：" + ips;
            comboBox1.Items.AddRange(ips.Split(' '));
            comboBox1.SelectedIndex = 0;
        }

        //连接
        private void start_listen()
        {
            lis = new Listener();
            lis.OnAddMessage += new EventHandler<AddMessageEventArgs>(this.AddMessage);
            lis.OnIpRemod += new EventHandler<AddMessageEventArgs>(this.IpRemo);
            lis.StartListener();
        }

        //获取本机IP
        private static string GetMyIpAddress()
        {
            IPAddress[] tmp = Dns.GetHostByName(Dns.GetHostName()).AddressList;
            string res = "";
            for(int i = 0; i < tmp.Length; i++)
            {
                res += " ";
                res += tmp[i].ToString();
            }
            return res;
        }
        public string getLocalIpAddr()
        {
            return comboBox1.SelectedItem.ToString();
        }

        //发送
        private void button1_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0 && chatToIp == "" && chatToIp == null && listBox1.SelectedIndex < 0)
            {
                MessageBox.Show("请选择目标主机");
                return;
            }
            else if (textBox1.Text.Trim() == "")
            {
                MessageBox.Show("消息内容不能为空!", "错误");
                this.textBox1.Focus();
                return;
            }
            else
            {
                try
                {
                    sen = new Sender(chatToIp);
                    string txt = Sender.addHeader(textBox1.Text,Listener.DIRECT,"");
                    sen.Send(txt, (ushort)(chatToPort+10000));
                    string appendText;
                    appendText = "Me:       " + System.DateTime.Now.ToString() + Environment.NewLine + textBox1.Text + Environment.NewLine;

                    int txtGetMsgLength = this.richTextBox1.Text.Length;
                    this.richTextBox1.AppendText(appendText);
                    this.richTextBox1.Select(txtGetMsgLength, appendText.Length - Environment.NewLine.Length * 2 - textBox1.Text.Length);
                    this.richTextBox1.SelectionColor = Color.Green;
                    this.richTextBox1.ScrollToCaret();
                }
                catch
                { }
                this.textBox1.Text = "";
                this.textBox1.Focus();
            }
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Clicks != 0)
            {
                if (listBox1.SelectedItem != null)
                {
                    //this.start_listen();
                    chatToIp = listBox1.SelectedItem.ToString().Split(':')[0];
                    chatToPort = UInt16.Parse(listBox1.SelectedItem.ToString().Split(':')[1]);
                    toolStripStatusLabel3.Text = "与" + chatToIp+":"+chatToPort+ "聊天中";
                }
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
        private VideoCaptureDevice cam;
        private FilterInfoCollection videoDevices;
        private delegate void UpdatePicDele(Bitmap b);

        private void buttonStViChat_Click(object sender, EventArgs e)
        {
            buttonStViChat.Enabled = false;
            cam.NewFrame += new NewFrameEventHandler(cam_NewFrame);
            cam.Start();
            buttonEndViCh.Enabled = true;
        }
        void cam_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();
            Bitmap resize = KiResizeImage(bitmap, pictureBox1.Width, pictureBox1.Height);
            //if (pictureBox1.InvokeRequired)
            //{
            //    UpdatePicDele u = (b) => { pictureBox1.Image = b; };
            //    pictureBox1.Invoke(u,resize);
            //}
            //else
            //{
            //    pictureBox1.Image = resize;
            //}
            Bitmap toSend = (Bitmap)resize.Clone();
            pictureBox1.Image = resize;
            sendFrame(toSend, chatToIp, chatToPort);
            //sendFrame(resize, chatToIp, chatToPort);
        }
        void sendFrame(Bitmap bitmap,string remoteIp,int remotePort)
        {
            string base64 = ImgToBase64String(bitmap);
            string msg = Sender.addHeader(base64, Listener.FRAME, "");
            if (chatToIp != null && chatToPort != 0)
            {
                Listener.udpSend(remoteIp, remotePort+10000, msg);
            }
            
        }
        public static Bitmap KiResizeImage(Bitmap bmp, int newW, int newH)
        {
            try
            {
                Bitmap b = new Bitmap(newW, newH);
                Graphics g = Graphics.FromImage(b);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(bmp, new Rectangle(0, 0, newW, newH), new Rectangle(0, 0, bmp.Width, bmp.Height), GraphicsUnit.Pixel);
                g.Dispose();
                return b;
            }
            catch
            {
                return null;
            }
        }
        public static string ImgToBase64String(Bitmap bmp)
        {
            try
            {
                MemoryStream ms = new MemoryStream();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                byte[] arr = new byte[ms.Length];
                ms.Position = 0;
                ms.Read(arr, 0, (int)ms.Length);
                ms.Close();
                return Convert.ToBase64String(arr);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public void setLabelNowVideoChat(string text)
        {
            labelNowVideo.Text = text;
        }
        public void changePic(Bitmap bitmap)
        {
            Bitmap resize = KiResizeImage(bitmap, pictureBox2.Width, pictureBox2.Height);
            pictureBox2.Image = resize;
        }

        private void buttonEndViCh_Click(object sender, EventArgs e)
        {
            buttonEndViCh.Enabled = false;
            cam.Stop();
            buttonStViChat.Enabled = true;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            label1.Text = "本主机IP是："+comboBox1.SelectedItem.ToString();
        }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
    }
}
