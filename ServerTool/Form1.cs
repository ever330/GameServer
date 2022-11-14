using System;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace ServerTool
{
    public partial class Form1 : Form
    {
        private Network m_ServerNet;
        public string m_TempTable;

        public Form1()
        {
            InitializeComponent();
            m_ServerNet = new Network();
            m_ServerNet.SetForm(this);

        }

        // 쿼리문 전송 버튼
        private void button1_Click(object sender, EventArgs e)
        {
            String str;
            SqlConnection myConn = new SqlConnection("Server=localhost;Integrated security=SSPI;database=master");

            str = textBox1.Text;

            SqlCommand myCommand = new SqlCommand(str, myConn);
            
            try
            {
                myConn.Open();
                myCommand.ExecuteNonQuery();
                MessageBox.Show("전송에 성공하였습니다.", "MyProgram", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString(), "MyProgram", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                if (myConn.State == ConnectionState.Open)
                {
                    myConn.Close();
                }
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        // 서버의 데이버베이스 가져오기 버튼
        private void button2_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            int i = 0;
            String str;
            string myConn = "Server=localhost;Integrated security=SSPI;database=master";

            using (SqlConnection conn = new SqlConnection(myConn))
            {
                conn.Open();

                str = "SELECT * FROM sys.sysdatabases";

                SqlCommand myCommand = new SqlCommand();
                myCommand.Connection = conn;
                myCommand.CommandText = str;
                SqlDataReader myReader = myCommand.ExecuteReader();

                string[] db_name = new string[10];

                while (myReader.Read())
                {
                    db_name[i] = myReader.GetString(0);
                    listBox1.Items.Add(db_name[i]);
                    i++;
                }
            }
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        // 선택된 데이터베이스 내의 테이블 가져오기 버튼
        private void button3_Click(object sender, EventArgs e)
        {
            listBox2.Items.Clear();
            int j = 0;
            String str;
            //string myConn = "Server=localhost;Integrated security=SSPI;database=master";
            string myConn = "Server=localhost;Integrated security=SSPI;database=" + listBox1.SelectedItem;

            if (listBox1.SelectedIndex == -1)
                return;

            using (SqlConnection conn = new SqlConnection(myConn))
            {
                conn.Open();

                str =/* "USE " + listBox1.SelectedItem + ";" +*/
                    "SELECT * FROM sys.tables";

                SqlCommand myCommand = new SqlCommand();
                myCommand.Connection = conn;
                myCommand.CommandText = str;
                SqlDataReader myReader = myCommand.ExecuteReader();

                string[] db_name = new string[10];

                while (myReader.Read())
                {
                    db_name[j] = myReader.GetString(0);
                    listBox2.Items.Add(db_name[j]);
                    j++;
                }
            }
        }

        // 선택된 테이블 내의 데이터들 가져오기 버튼
        private void button4_Click(object sender, EventArgs e)
        {
            dataGridView2.Refresh();

            SqlConnection conn;

            string myConn = "Server=localhost;Integrated security=SSPI;database=master";
            conn = new SqlConnection(myConn);

            conn.Open();

            if (listBox1.SelectedIndex == -1 || listBox2.SelectedIndex == -1)
                return;

            string str = "USE " + listBox1.SelectedItem + ";" +
                "SELECT * FROM " + listBox2.SelectedItem + ";";

            DataSet ds = new DataSet();

            SqlDataAdapter adapter = new SqlDataAdapter(str, conn);

            adapter.Fill(ds);
            conn.Close();

            dataGridView2.DataSource = ds.Tables[0];

            m_TempTable = (string)listBox2.SelectedItem;
        }

        // 서버 오픈 버튼
        private void button5_Click(object sender, EventArgs e)
        {
            if (m_ServerNet.ServerSocket == null)
            {
                m_ServerNet.SocketServer();
                richTextBox2.AppendText("서버 오픈\n");
                richTextBox2.ScrollToCaret();
            }
            else
            {
                richTextBox2.AppendText("서버가 이미 오픈 되었습니다.\n");
                richTextBox2.ScrollToCaret();
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            for (int i = 0; i < host.AddressList.Length; i++)
            {
                if (host.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    textBox2.Text = host.AddressList[i].ToString();
                    break;
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        public void writeRichTextBox(string str)
        {
            if (richTextBox2.InvokeRequired)
            {
                Action safeWrite = delegate { writeRichTextBox(str); };
                richTextBox2.Invoke(safeWrite);
            }
            else
            {
                int index = str.IndexOf('\0');
                if (index > 0)
                {
                    string message = str.Substring(0, index);
                    richTextBox2.AppendText(message + "\n");
                    richTextBox2.ScrollToCaret();
                }
                else
                {
                    richTextBox2.AppendText(str + "\n");
                    richTextBox2.ScrollToCaret();
                }
            }
        }

        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {

        }

        // 모든 클라이언트에 메시지 전송
        private void button6_Click(object sender, EventArgs e)
        {
            m_ServerNet.SendMessage("서버에 접속되었습니다.");
        }

        public void UpdataDataGridView(DataSet ds)
        {
            if (dataGridView2.InvokeRequired)
            {
                dataGridView2.Invoke(new MethodInvoker(delegate { dataGridView2.DataSource = ds.Tables[0]; }));
            }
            else
            {
                dataGridView2.DataSource = ds.Tables[0];
            }
        }
    }
}
