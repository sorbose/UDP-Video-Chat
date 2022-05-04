using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace TCP_Server
{
    public class AddMessageEventArgs : EventArgs
    {
        public string mess;   //存放要显示的内容
    }

    class Listener
    {
        public static readonly int DIRECT = 0;
        public static readonly int CONNECT = 1;
        public static readonly int DISCONNECT = 2;
        public static readonly int TRANSFER = 3;
        public static readonly int REFRESH = 4;
        public static readonly int FRAME = 5;
        public static readonly string S_DIRECT = "DIRECT";
        public static readonly string S_CONNECT = "CONNNECT";
        public static readonly string S_DISCONNECT = "DISCONN";
        public static readonly string S_TRANSFER = "TRANSFER";
        public static readonly string S_REFRESH = "REFRESH";
        public static readonly string S_FRAME = "FRAME";
        public static ushort SERVER_LISTEN_PORT = 5656;

        private delegate void ReceiveMessageDelegate(out string receiveMessage);
        private ReceiveMessageDelegate receiveMessageDelegate;

        private Thread th;
        private static UdpClient udpl=null;
        public bool listenerRun = true;    //判断是否启动
        public event EventHandler<AddMessageEventArgs> OnAddMessage;
        public event EventHandler<AddMessageEventArgs> OnIpRemod;
        public static Dictionary<EndPoint, Listener> dic = new Dictionary<EndPoint, Listener>();
        public Listener()
        {
            if (udpl == null)
            {
                IPAddress addr = IPAddress.Parse(Program.form1.getLocalIpAddr());
                IPEndPoint ipLocalEndPoint = new IPEndPoint(addr, SERVER_LISTEN_PORT);
                udpl = new UdpClient(ipLocalEndPoint);
                //udpl.Start();
            }

        }

        //另一个线程开始监听
        public void StartListener()
        {
            th = new Thread(new ThreadStart(Listen));
            th.Start();
        }

        //停止监听
        public void Stop()
        {
            //udpl.Stop();
            th.Abort();
        }

        private void Listen()
        {
            string receiveString;

            receiveMessageDelegate = new ReceiveMessageDelegate(ReceiveMessage);

            IAsyncResult result = receiveMessageDelegate.BeginInvoke(out receiveString, null, null);    //异步操作2
            receiveMessageDelegate.EndInvoke(out receiveString, result);
        }

        private string removeHeader(string raw,out int code,out IPEndPoint transferTo)
        {
            code = -1;
            transferTo = null;
            string[] txt = raw.Split(new char[] { '\n' }, 2);
            string[] header = txt[0].Split(new char[] { ' ' });
            string operation = header[0];
            if (operation == S_CONNECT)
            {
                code = CONNECT;
            }else if (operation == S_DIRECT)
            {
                code = DIRECT;
            }else if (operation == S_FRAME)
            {
                code = FRAME;
            }
            else if (operation == S_DISCONNECT)
            {
                code = DISCONNECT;
            }else if (operation == S_REFRESH)
            {
                code = REFRESH;
            }else if (operation == S_TRANSFER)
            {
                code = TRANSFER;
                string ip = header[1].Split(':')[0];
                string port = header[1].Split(':')[1];
                transferTo = new IPEndPoint(IPAddress.Parse(ip), Int32.Parse(port));
            }
            if (txt.Length > 1)
            {
                return txt[1];
            }
            else
            {
                return "";
            }
        }

        public void ipRemove(AddMessageEventArgs argRe, IPEndPoint remove)
        {
            argRe.mess = remove.ToString();
            OnIpRemod(this, argRe);
        }

        public static void udpSend(string hostname,int port,string msg)
        {
            UdpClient udpc = new UdpClient(hostname, port);
            Byte[] data = System.Text.Encoding.UTF8.GetBytes(msg);
            udpc.DontFragment = false;
            udpc.Send(data, data.Length);
            udpc.Close();
        }

        public static void udpSendBytes(string hostname, int port, byte[] bytes)
        {
            UdpClient udpc = new UdpClient(hostname, port);
            udpc.Send(bytes, bytes.Length);
            udpc.Close();
        }

        public static Bitmap Base64StringToImage(string strbase64)
        {
            try
            {
                byte[] arr = Convert.FromBase64String(strbase64);
                MemoryStream ms = new MemoryStream(arr);
                Bitmap bmp = new Bitmap(ms);
                ms.Close();
                return bmp;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    
        private void ReceiveMessage(out string receiveMessage)
        {
            receiveMessage = "";
            Socket s=null;
            IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                //s = udpl.AcceptSocket();
                s = udpl.Client;
                string remote;
                while (true)
                {
                    byte[] receiveBytes = udpl.Receive(ref remoteIPEndPoint);
                    string raw = Encoding.UTF8.GetString(receiveBytes, 0, receiveBytes.Length);
                    remote = remoteIPEndPoint.ToString();
                    string msg;
                    int code;
                    IPEndPoint transferTo;
                    string str = removeHeader(raw, out code,out transferTo);
                    if (code==CONNECT)
                    {
                        dic.Add(remoteIPEndPoint, this);
                        Listener lis = new Listener();
                        lis.OnAddMessage += new EventHandler<AddMessageEventArgs>(Program.form1.AddMessage);
                        lis.OnIpRemod += new EventHandler<AddMessageEventArgs>(Program.form1.IpRemo);
                        lis.StartListener();
                        string str_ = "欢迎登录！服务器："+s.LocalEndPoint.ToString() + Environment.NewLine;
                        str_ = Sender.addHeader(str_, DIRECT, "");
                        udpSend((remoteIPEndPoint).Address.ToString(), int.Parse(remote.Split(':')[1]) + 10000, str_);
                        msg = "<" + remote + ">" + "上线"+Environment.NewLine;
                        AddMessageEventArgs arg = new AddMessageEventArgs();
                        arg.mess = msg;
                        OnAddMessage(this, arg);
                    }
                    else if (code == DISCONNECT)
                    {
                        msg = "<" + remote + ">"  + "断开" + Environment.NewLine;
                        AddMessageEventArgs argRe = new AddMessageEventArgs();
                        argRe.mess = remote;
                        OnIpRemod(this, argRe);
                        AddMessageEventArgs arg = new AddMessageEventArgs();
                        arg.mess = msg;
                        OnAddMessage(this, arg);
                        dic.Remove(remoteIPEndPoint);
                    }
                    else if(code==DIRECT)
                    {
                        msg = "<" + remote + ">" + str + Environment.NewLine;
                        AddMessageEventArgs arg = new AddMessageEventArgs();
                        arg.mess = msg;
                        OnAddMessage(this, arg);
                    }else if (code == FRAME)
                    {
                        Program.form1.setLabelNowVideoChat(remote);
                        Bitmap bitmap = Base64StringToImage(str);
                        Program.form1.changePic(bitmap);
                    }
                    else if (code == REFRESH)
                    {
                        EndPoint[] eps=dic.Keys.ToArray();
                        string[] ss = new string[eps.Length];
                        for(int ii = 0; ii < eps.Length; ii++)
                        {
                            ss[ii] = eps[ii].ToString();
                        }
                        string res= string.Join("\n", ss);
                        res = Sender.addHeader(res, REFRESH, "");
                        udpSend(((IPEndPoint)remoteIPEndPoint).Address.ToString(), Int32.Parse(remote.Split(':')[1]) + 10000, res);
                    }
                    else if (code == TRANSFER)
                    {
                        str = Sender.addHeader(str, TRANSFER, remoteIPEndPoint.ToString());
                        udpSend(transferTo.Address.ToString(), transferTo.Port+10000, str);
                        //TcpClient tcpc = new TcpClient(AddressFamily.InterNetwork);
                        //str = Sender.addHeader(str, TRANSFER, s.RemoteEndPoint.ToString());
                        //tcpc.BeginConnect(transferTo.Address, transferTo.Port+10000, new AsyncCallback((iar)=>
                        //{
                        //    try
                        //    {
                        //        List<object> list = (List<object>)iar.AsyncState;
                        //        string txt = (string)list[0];
                        //        TcpClient tcpc2 = (TcpClient)list[1];
                        //        NetworkStream tcpStream2 = tcpc2.GetStream();
                        //        byte[] data = Encoding.UTF8.GetBytes(txt);
                        //        tcpStream2.Write(data, 0, data.Length);
                        //        tcpStream2.Close();
                        //        tcpc2.Close();
                        //    }
                        //    catch (Exception ex) { }
                        //}), new List<object> { str,tcpc});
                    }
                }
            }
            catch (System.Security.SecurityException)
            {
                MessageBox.Show("防火墙禁止连接");
            }
            catch(SocketException ex)
            {
                //try
                //{
                    dic.Remove(remoteIPEndPoint);
                    AddMessageEventArgs argRe = new AddMessageEventArgs();
                    argRe.mess = remoteIPEndPoint.ToString();
                    OnIpRemod(this, argRe);
                //}
                //catch (Exception exc) { }
            }
            catch (ArgumentException ex)
            {
                //MessageBox.Show(ex.Message);
            }catch(Exception exc)
            {

            }
        }
    }
}
