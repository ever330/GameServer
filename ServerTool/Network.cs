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
using client_test;

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
            Socket ClientSocket = e.AcceptSocket;
            m_ClientSocket.Add(ClientSocket);

            if (m_ClientSocket != null)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                szData = new byte[1024];
                args.SetBuffer(szData, 0, 1024);
                args.UserToken = m_ClientSocket;
                args.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveCompleted);
                ClientSocket.ReceiveAsync(args);
                form1.writeRichTextBox(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss\n") + ClientSocket.RemoteEndPoint.ToString() + " 접속");
            }

            e.AcceptSocket = null;
            m_ServerSocket.AcceptAsync(e);
        }

        // 데이터 수신 Callback 함수
        private void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            Socket ClientSocket = (Socket)sender;

            if (ClientSocket.Connected && e.BytesTransferred > 0)
            {
                byte[] szData = e.Buffer;
                string sData = Encoding.Unicode.GetString(szData);

                string Test = sData.Replace("|0", "").Trim();
                form1.writeRichTextBox(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss\n") + ClientSocket.RemoteEndPoint.ToString() + " : " + Test);
                
                var s = LoginPacket.Deserialize(szData);
                
                form1.writeRichTextBox(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss\nTest\n") + ClientSocket.RemoteEndPoint.ToString() + " : " + "id : " + s.m_id + "\npw : " + s.m_pw);


                int index = Test.IndexOf('\0');
                if (index > 0)
                {
                    string message = Test.Substring(0, index);
                    byte[] data = Encoding.Unicode.GetBytes(MessageCheck(message));
                    ClientSocket.Send(data, data.Length, SocketFlags.None);
                }


                for (int i = 0; i < szData.Length; i++)
                {
                    szData[i] = 0;
                }

                e.SetBuffer(szData, 0, 1024);
                ClientSocket.ReceiveAsync(e);
            }
            else
            {
                ClientSocket.Disconnect(false);
                m_ClientSocket.Remove(ClientSocket);
                form1.writeRichTextBox(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss\n") + ClientSocket.RemoteEndPoint.ToString() + "의 연결이 끊어졌습니다.");
            }
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

        private string MessageCheck(string str)
        {
            // 클라이언트로부터 전달받은 메시지 분석을 위한 문자열
            string loginStr = "@login@";
            string signupStr = "@signup@";
            string idcheckStr = "@idcheck@";

            int loginIndex = str.IndexOf(loginStr);
            int signupIndex = str.IndexOf(signupStr);
            int idcheckIndex = str.IndexOf(idcheckStr);

            string result = "";

            if (loginIndex != -1)
            {
                string newStr = str.Substring(loginIndex + 7);

                string idStr = "@id@";
                string pwStr = "@pw@";

                int idIndex = newStr.IndexOf(idStr);
                int pwIndex = newStr.IndexOf(pwStr);

                string userID = newStr.Substring(idIndex + 4, pwIndex - idIndex - 4);
                string userPW = newStr.Substring(pwIndex + 4);

                if (LoginCheck(userID, userPW))
                {
                    result = "로그인 성공";
                }
                else 
                {
                    result = "로그인 실패";
                }
            }

            else if (idcheckIndex != -1)
            {
                string userID = str.Substring(idcheckIndex + 9);

                if (IDCheck(userID))
                {
                    result = "아이디 중복";
                }
                else
                {
                    result = "아이디 생성 가능";
                }
            }

            else if (signupIndex != -1)
            {
                string newStr = str.Substring(signupIndex + 8);

                string idStr = "@id@";
                string pwStr = "@pw@";
                //string pwcheckStr = "@pwcheck@";

                int idIndex = newStr.IndexOf(idStr);
                int pwIndex = newStr.IndexOf(pwStr);
                //int pwcheckIndex = newStr.IndexOf(pwcheckStr);

                string userID = newStr.Substring(idIndex + 4, pwIndex - idIndex - 4);
                string userPW = newStr.Substring(pwIndex + 4);
                //string userPW = newStr.Substring(pwIndex + 4, pwcheckIndex - pwIndex - 4);
                //string userPWCheck = newStr.Substring(pwcheckIndex + 9);

                if (userID == "" || userPW == "")
                {
                    result = "아이디 생성 불가";
                }
                else
                {
                    if (IDCheck(userID))
                    {
                        result = "아이디 중복";
                    }
                    //else if (userPW != userPWCheck)
                    //{
                    //    result = "비밀번호 확인";
                    //}
                    else
                    {
                        CreateID(userID, userPW);
                        result = "아이디 생성";
                    }
                }
            }
            return result;
        }

        private bool LoginCheck(string str1, string str2)
        {
            SqlConnection conn;

            string myConn = "Server=localhost;Integrated security=SSPI;database=master";
            conn = new SqlConnection(myConn);

            conn.Open();

            string sql = "USE MyDatabase;"
                         + "SELECT * FROM test_table WHERE user_id=\'" + str1 + "\'";

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
                         + "SELECT * FROM test_table WHERE user_id=\'" + str1 + "\'";

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

            string str = "INSERT INTO test_table (user_id, user_pw)" +
                $"VALUES ('{str1}', '{str2}')";

            SqlCommand command = new SqlCommand();
            command.Connection = conn;
            command.CommandText = str;
            command.ExecuteNonQuery();

            if (form1.m_TempTable == "test_table")
            {
                SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM test_table", conn);
                DataSet ds = new DataSet();
                adapter.Fill(ds, "test_table");
                form1.UpdataDataGridView(ds);
            }
            conn.Close();
        }
    }
}
