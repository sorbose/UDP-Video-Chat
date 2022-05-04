using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;

namespace WindowsFormsApp2
{
    public partial class Client : Form
    {
        public Client()
        {
            InitializeComponent();
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void Client_Load(object sender, EventArgs e)
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            cam = new VideoCaptureDevice(videoDevices[0].MonikerString);
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            string ips = GetMyIpAddress().Trim();
            comboBox1.Items.AddRange(ips.Split(' '));
            comboBox1.SelectedIndex = 0;
            textBoxSerIP.Text = comboBox1.SelectedItem.ToString();
            listBox1.Items.Clear();
            listBox1.Items.Add(comboBox1.SelectedItem.ToString() + ":" + SERVER_LISTEN_PORT);

        }

        public ushort localSendPort = 19132;
        public static UdpClient udpClient;
        private Listener listener;
        public static ushort SERVER_LISTEN_PORT = 5656;
        public static IPEndPoint iPRemoteEndPoint;
        private Sender sen;
        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            numericUpDown1.ReadOnly = true;
            numericUpDown2.ReadOnly = true;
            textBoxSerIP.ReadOnly = true;
            comboBox1.Enabled = false;
            SERVER_LISTEN_PORT = (ushort)numericUpDown2.Value;
            localSendPort = (ushort)numericUpDown1.Value;
            start_listen();
            string msg = textBoxMsg.Text;
            Thread threadConnect = new Thread(ConnectoServer);
            threadConnect.IsBackground = true;
            threadConnect.Start();
        }
        private delegate void  RefreshListBoxDelegate(string t);
        private delegate void CleanListBoxDelegate();
        public void refreshListBox(string txt)
        {
            string[] lines = txt.Split('\n');
            if (listBox1.InvokeRequired)
            {
                RefreshListBoxDelegate rld = delegate (string t) { 
                    listBox1.Items.Add(t);
                    listBox1.Refresh();
                };
                CleanListBoxDelegate cld = () => { listBox1.Items.Clear(); };
                listBox1.Invoke(cld);
                listBox1.Invoke(rld, comboBox1.SelectedItem.ToString() + ":" + SERVER_LISTEN_PORT);
                for (int i=0;i<lines.Length;i++)
                {
                    string line = lines[i];
                    listBox1.Invoke(rld, line);
                }
            }
            else
            {
                listBox1.Items.Add(comboBox1.SelectedItem.ToString() + ":" + SERVER_LISTEN_PORT);
                listBox1.Items.Clear();
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    listBox1.Items.Add(line);
                }
            }
        }
        public void changePic(Bitmap bitmap)
        {
            Bitmap resize = KiResizeImage(bitmap, pictureBox2.Width, pictureBox2.Height);
            pictureBox2.Image = resize;
        }
        public void setLabelNowVideoChat(string text)
        {
            labelNowVideo.Text = text;
        }
        public int getComboBoxSelIndex()
        {
            return comboBox1.SelectedIndex;
        }

        private void ConnectoServer()
        {
            iPRemoteEndPoint = new IPEndPoint(IPAddress.Parse(textBoxSerIP.Text), SERVER_LISTEN_PORT);
            //AsyncCallback requestcallback;
            try
            {
                //requestcallback = new AsyncCallback(RequestCallBack);
                udpClient = new UdpClient(AddressFamily.InterNetwork);
                IPAddress addr = new IPAddress(Dns.GetHostByName(Dns.GetHostName()).AddressList[comboBox1.SelectedIndex].Address);
                udpClient.Client.Bind(new IPEndPoint(addr, localSendPort));
                string msg = Sender.addHeader("上线了", Sender.CONNECT, "");
                byte[] data = Encoding.UTF8.GetBytes(msg);
                udpClient.Send(data, data.Length, iPRemoteEndPoint);
            }
            catch (SocketException ex)
            {
                toolStripStatusLabel1.Text = "连接失败，原因：端口号被占用，请更换端口重试";
                numericUpDown1.Value += 1;
                button2.Enabled = true;
                numericUpDown1.ReadOnly = !true;
                numericUpDown2.ReadOnly = !true;
                textBoxSerIP.ReadOnly = !true;
                return;
            }catch(Exception ex)
            {
                MessageBox.Show("error: " + ex.Message);
                button2.Enabled = true;
                numericUpDown1.ReadOnly = !true;
                numericUpDown2.ReadOnly = !true;
                textBoxSerIP.ReadOnly = !true;
                return;
            }
        }

        private void RequestCallBack(IAsyncResult iar)
        {
            try
            {
                udpClient = (UdpClient)iar.AsyncState;
                if (udpClient != null)
                {
                    start_listen();
                    //NetworkStream tcpStream = udpClient.GetStream();
                    string msg = Sender.addHeader("上线了"+Environment.NewLine, Sender.CONNECT, "");
                    Byte[] data = Encoding.UTF8.GetBytes(msg);
                    udpClient.Send(data,data.Length, iPRemoteEndPoint);
                    toolStripStatusLabel1.Text = "当前状态：已连接到服务器";
                }
            }
            catch (Exception ex)
            {
                button2.Enabled = true;
                numericUpDown1.ReadOnly = !true;
                numericUpDown2.ReadOnly = !true;
                textBoxSerIP.ReadOnly = !true;
                toolStripStatusLabel1.Text = "当前状态：连接失败 "+ex.Message;
            }
        }

        public void setStatus(string msg)
        {
            toolStripStatusLabel1.Text = msg;
        }

        //private void start_listen()
        //{
        //    try
        //    {
        //        if (listener.listenerRun == true)
        //        {
        //            listener.listenerRun = false;
        //            listener.Stop();
        //        }
        //    }
        //    catch (NullReferenceException)
        //    {
        //    }
        //    finally
        //    {
        //        listener = new Listener((ushort)(localSendPort + 10000));
        //        listener.OnAddMessage += new EventHandler<AddMessageEventArgs>(this.AddMessage);
        //        listener.StartListener();
        //    }
        //}
        private void start_listen()
        {
            listener = new Listener((ushort)(localSendPort + 10000));
            listener.OnAddMessage += new EventHandler<AddMessageEventArgs>(this.AddMessage);
            listener.StartListener();
        }

        public void AddMessage(object sender, AddMessageEventArgs e)
        {
            string message = e.mess;
            string appendText;
            string[] sep = message.Split('>');
            appendText = sep[0] + ">:           " + System.DateTime.Now.ToString() + Environment.NewLine + sep[1] + Environment.NewLine;
            int txtGetMsgLength = this.richTextBox1.Text.Length;
            this.richTextBox1.AppendText(appendText);
            this.richTextBox1.Select(txtGetMsgLength, appendText.Length - Environment.NewLine.Length * 2 - sep[1].Length);
            this.richTextBox1.SelectionColor = Color.Red;
            this.richTextBox1.ScrollToCaret();
        }

        private static string GetMyIpAddress()
        {
            IPAddress[] tmp = Dns.GetHostByName(Dns.GetHostName()).AddressList;
            string res = "";
            for (int i = 0; i < tmp.Length; i++)
            {
                res += " ";
                res += tmp[i].ToString();
            }
            return res;
        }

        //private byte[] GetKeepAliveData()
        //{
        //    uint dummy = 0;
        //    byte[] inOptionValues = new byte[4 * 3];
        //    BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
        //    BitConverter.GetBytes((uint)3000).CopyTo(inOptionValues, 4);//keep-alive间隔
        //    BitConverter.GetBytes((uint)500).CopyTo(inOptionValues, 4 * 2);// 尝试间隔
        //    return inOptionValues;
        //}

        private void button3_Click_1(object sender, EventArgs e)
        {
            try
            {
                button2.Enabled = !false;
                numericUpDown1.ReadOnly = !true;
                numericUpDown2.ReadOnly = !true;
                textBoxSerIP.ReadOnly = !true;
                comboBox1.Enabled = true;
                //NetworkStream tcpStream = udpClient.GetStream();
                string msg = Sender.addHeader("", Sender.DISCONNECT, "");
                Byte[] data = System.Text.UTF8Encoding.UTF8.GetBytes(msg);
                udpClient.Send(data, data.Length, iPRemoteEndPoint);
                //tcpStream.Write(data, 0, data.Length);
                listener.Stop();
                udpClient.Close();
                listener = null;
                udpClient = null;
                toolStripStatusLabel1.Text = "连接已断开";
            }catch(NullReferenceException ex)
            {
                toolStripStatusLabel1.Text = ex.Message;
            }catch(Exception ex)
            {
                toolStripStatusLabel1.Text = ex.Message;
            }
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            if (textBoxSerIP.Text.Trim() == "")
            {
                MessageBox.Show("请选择目标主机");
                return;
            }
            else if (textBoxMsg.Text.Trim() == "")
            {
                MessageBox.Show("消息内容不能为空!", "错误");
                this.textBoxMsg.Focus();
                return;
            }
            else
            {
                try
                {
                    sen = new Sender(textBoxSerIP.Text);
                    string msg;
                    if (textBoxTrans.Text==""||textBoxTrans.Text==textBoxSerIP.Text||textBoxTrans.Text==textBoxSerIP.Text+":"+SERVER_LISTEN_PORT)
                    {
                        msg = Sender.addHeader(textBoxMsg.Text, Sender.DIRECT, "");
                    }
                    else
                    {
                        msg = Sender.addHeader(textBoxMsg.Text, Sender.TRANSFER, textBoxTrans.Text);
                    }
                    sen.Send(msg);
                    string appendText;
                    appendText = "Me:   " + System.DateTime.Now.ToString() + Environment.NewLine + textBoxMsg.Text + Environment.NewLine;
                    int txtGetMsgLength = this.richTextBox1.Text.Length;
                    this.richTextBox1.AppendText(appendText);
                    this.richTextBox1.Select(txtGetMsgLength, appendText.Length - Environment.NewLine.Length * 2 - textBoxSerIP.Text.Length);
                    this.richTextBox1.SelectionColor = Color.Green;
                    this.richTextBox1.ScrollToCaret();
                }
                catch (Exception ex)
                {
                    toolStripStatusLabel1.Text=ex.Message;
                }
                this.textBoxMsg.Text = "";
                this.textBoxMsg.Focus();
            }
        }

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            sen = new Sender(textBoxSerIP.Text);
            sen.Send(Sender.addHeader("", Sender.REFRESH, ""));
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem  != null)
            {
                textBoxTrans.Text= listBox1.SelectedItem.ToString();
            }
            
        }
        private VideoCaptureDevice cam;
        private FilterInfoCollection videoDevices;
        private void buttonStViChat_Click(object sender, EventArgs e)
        {
            buttonStViChat.Enabled = false;
            udpImg = new UdpClient(textBoxSerIP.Text, SERVER_LISTEN_PORT);
            cam.NewFrame += new NewFrameEventHandler(cam_NewFrame);
            cam.Start();
            buttonEndViCh.Enabled = true;
        }
        void cam_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();
            Bitmap resize = KiResizeImage(bitmap, pictureBox1.Width, pictureBox1.Height);
            Bitmap toSend = (Bitmap)resize.Clone();
            pictureBox1.Image = resize;
            udpSendImg(toSend);
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

        public static void udpSend(string hostname, int port, string msg)
        {
            UdpClient udpc = new UdpClient(hostname, port);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(msg);
            udpc.Send(data, data.Length);
            udpc.Close();
        }
        UdpClient udpImg = null;
        public void udpSendImg(Bitmap b)
        {
            try
            {
                if (udpImg != null)
                {
                    //string ascii = Encoding.ASCII.GetString(Imgt);
                    string base64 = ImgToBase64String(b);
                    string msg = Sender.addHeader(base64, Sender.FRAME, "");
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(msg);
                    udpImg.Send(data, data.Length);
                }
            }
            catch (NullReferenceException ex) { }

        }

        private void buttonEndViCh_Click(object sender, EventArgs e)
        {
            buttonEndViCh.Enabled = false;
            udpImg = null;
            cam.Stop();
            buttonStViChat.Enabled = true;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            label1.Text = "本机IP " + comboBox1.SelectedItem.ToString();
        }
    }
}
