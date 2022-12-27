using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace ServerTool
{
    class Network
    {
        private Socket m_ServerSocket;
        private List<Socket> m_ClientSocket;
        private byte[] szData;

        private Form1 form1;

        public Socket ServerSocket
        {
            get { return m_ServerSocket; }
        }

        public void SetForm(Form1 form)
        {
            form1 = form;
        }

        public void SocketServer()
        {
            m_ClientSocket = new List<Socket>();
            m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ipEP = new IPEndPoint(IPAddress.Any, 3000);
            //IPEndPoint ipEP = new IPEndPoint(IPAddress.Parse("192.168.0.33"), 3000);

            m_ServerSocket.Bind(ipEP);
            m_ServerSocket.Listen(20);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptCompleted);
            m_ServerSocket.AcceptAsync(args);
        }

        // 클라이언트 접속 수락 Callback 함수
        private void AcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            bool isDuplication = false;

            foreach (var cSocket in m_ClientSocket)
            {
                IPEndPoint ipEp1 = (IPEndPoint)cSocket.RemoteEndPoint;
                IPEndPoint ipEp2 = (IPEndPoint)e.AcceptSocket.RemoteEndPoint;

                if (Equals(ipEp1.Address, ipEp2.Address))
                {
                    ServerPacket sp = new ServerPacket();
                    sp.m_packetType = PacketType.DuplicationCheck;
                    sp.m_isSuccess = false;

                    PacketSend(e.AcceptSocket, sp);

                    isDuplication = true;
                }
            }

            Socket clientSocket = e.AcceptSocket;

            if (!isDuplication)
            {
                m_ClientSocket.Add(clientSocket);
                form1.writeRichTextBox(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss\n") + clientSocket.RemoteEndPoint.ToString() + " 접속");
            }
            else
            {
                form1.writeRichTextBox(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss\n") + clientSocket.RemoteEndPoint.ToString() + " ip중복 접속");
            }

            if (m_ClientSocket != null)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                szData = new byte[1024];
                args.SetBuffer(szData, 0, 1024);
                args.UserToken = m_ClientSocket;
                args.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveCompleted);
                clientSocket.ReceiveAsync(args);
                //form1.writeRichTextBox(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss\n") + clientSocket.RemoteEndPoint.ToString() + " 접속");
            }

            e.AcceptSocket = null;
            m_ServerSocket.AcceptAsync(e);
        }

        // 데이터 수신 Callback 함수
        private void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            Socket clientSocket = (Socket)sender;

            if (clientSocket.Connected && e.BytesTransferred > 0)
            {
                szData = e.Buffer;

                var s = ClientPacket.Deserialize(szData);

                form1.writeRichTextBox(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss\n") + clientSocket.RemoteEndPoint.ToString() + "에서 패킷을 수신하였습니다.");

                PacketCheck(clientSocket, s);

                // 데이터 수신 byte배열 초기화
                for (int i = 0; i < szData.Length; i++)
                {
                    szData[i] = 0;
                }

                e.SetBuffer(szData, 0, 1024);
                clientSocket.ReceiveAsync(e);
            }
            else
            {
                clientSocket.Disconnect(false);
                m_ClientSocket.Remove(clientSocket);
                form1.writeRichTextBox(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss\n") + clientSocket.RemoteEndPoint.ToString() + "의 연결이 끊어졌습니다.");
            }
        }

        public void PacketSend(Socket clientSoc, ServerPacket sp)
        {
            SocketAsyncEventArgs sendEventArgs = new SocketAsyncEventArgs();
            if (sendEventArgs == null)
            {
                MessageBox.Show("SocketAsyncEventArgs is null");
                return;
            }

            sendEventArgs.Completed += SendComplected;
            sendEventArgs.UserToken = this;

            byte[] sendData = sp.Serialize();

            sendEventArgs.SetBuffer(sendData, 0, sendData.Length);

            bool pending = clientSoc.SendAsync(sendEventArgs);
            if (!pending)
            {
                SendComplected(null, sendEventArgs);
            }
        }

        private void SendComplected(object sender, SocketAsyncEventArgs e)
        {
            form1.writeRichTextBox("패킷 전송에 성공하였습니다.");
        }

        // 서버에서 클라이언트 리스트에 담긴 클라이언트로 데이터 전송
        public void SendMessage(string str)
        {
            byte[] data = Encoding.Unicode.GetBytes(str);

            for (int i = 0; i < m_ClientSocket.Count; i++)
            {
                m_ClientSocket[i].Send(data, data.Length, SocketFlags.None);
            }
        }

        private bool LoginCheck(string str1, string str2)
        {
            SqlConnection conn;

            string myConn = "Server=localhost;Integrated security=SSPI;database=master";
            conn = new SqlConnection(myConn);

            conn.Open();

            string sql = "USE MyDatabase;"
                         + "SELECT * FROM prototype WHERE user_id=\'" + str1 + "\'";

            SqlCommand cmd = new SqlCommand(sql, conn);
            SqlDataReader mdr = cmd.ExecuteReader();

            while (mdr.Read())
            {
                if (str1 == (string)mdr["user_id"] && str2 == (string)mdr["user_pw"])
                {
                    return true;
                }
            }

            return false;
        }

        private bool IDCheck(string str1)
        {
            SqlConnection conn;

            string myConn = "Server=localhost;Integrated security=SSPI;database=master";
            conn = new SqlConnection(myConn);

            conn.Open();

            string sql = "USE MyDatabase;"
                         + "SELECT * FROM prototype WHERE user_id=\'" + str1 + "\'";

            SqlCommand cmd = new SqlCommand(sql, conn);
            SqlDataReader mdr = cmd.ExecuteReader();

            while (mdr.Read())
            {
                if (str1 == (string)mdr["user_id"])
                {
                    return true;
                }
            }

            return false;
        }

        private void CreateID(string str1, string str2)
        {
            SqlConnection conn;

            string myConn = "Server=localhost;Integrated security=SSPI;database=MyDatabase";
            conn = new SqlConnection(myConn);

            conn.Open();

            string str = "INSERT INTO prototype (user_id, user_pw)" +
                $"VALUES ('{str1}', '{str2}')";

            SqlCommand command = new SqlCommand();
            command.Connection = conn;
            command.CommandText = str;
            command.ExecuteNonQuery();

            if (form1.m_TempTable == "prototype")
            {
                SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM prototype", conn);
                DataSet ds = new DataSet();
                adapter.Fill(ds, "prototype");
                form1.UpdateDataGridView(ds);
            }
            conn.Close();
        }

        public void PacketCheck(Socket cs, ClientPacket cp)
        {
            ServerPacket sp = new ServerPacket();
            if (cp.m_packetType == PacketType.Login)
            {
                sp.m_packetType = PacketType.Login;
                if (LoginCheck(cp.m_id, cp.m_pw))
                {
                    sp.m_isSuccess = true;
                }
            }

            else if (cp.m_packetType == PacketType.IdCheck)
            {
                sp.m_packetType = PacketType.IdCheck;
                if (IDCheck(cp.m_id))
                {
                    sp.m_isSuccess = false;
                }
                else
                {
                    sp.m_isSuccess = true;
                }
            }

            else if (cp.m_packetType == PacketType.SignUp)
            {
                sp.m_packetType = PacketType.SignUp;
                if (IDCheck(cp.m_id))
                {
                    sp.m_isSuccess = false;
                }
                else
                {
                    CreateID(cp.m_id, cp.m_pw);
                    sp.m_isSuccess = true;
                }
            }
            PacketSend(cs, sp);
        }
    }
}
