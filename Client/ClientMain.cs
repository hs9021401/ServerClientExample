using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClientExample
{
    public partial class ClientMain : Form
    {
        public delegate void DelUpdateUI(string s);        
        private TcpClient m_client;
        //private Guid m_guid;

        public ClientMain()
        {
            InitializeComponent();
            //m_guid = Guid.NewGuid();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                m_client = new TcpClient(txtIP.Text, int.Parse(txtPort.Text));
                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;
            }
            catch (SocketException ex)
            {
                MessageBox.Show(ex.Message);
            }            
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            byte[] bBuffer = Encoding.UTF8.GetBytes(txtUserName.Text + "**" + txtMsg.Text);
            try
            {
                if (m_client != null && m_client.Connected)
                {
                    NetworkStream ns = m_client.GetStream();
                    ns.Write(bBuffer, 0, bBuffer.Length);
                    txtMsg.Text = "";

                    //讀取server回傳訊息
                    Task.Run(() =>
                    {
                        String responseData = String.Empty;
                        byte[] bReceive = new byte[1024];
                        Int32 bytes = ns.Read(bReceive, 0, bReceive.Length);
                        responseData = System.Text.Encoding.UTF8.GetString(bReceive, 0, bytes);
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine(responseData);
                        sb.AppendLine(txtResponse.Text);
                        bool isServerDown = responseData.Equals("Bye") ? true : false;
                        this.Invoke(UpdateUI, new object[] { sb.ToString(), isServerDown });
                    });
                }
                else
                {
                    MessageBox.Show("Please connect server first!");
                }
                
            }
            catch (SocketException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void UpdateUI(string s, bool bIsServerDown)
        {
            if (bIsServerDown)
            {
                btnDisconnect.PerformClick();   
            }
            else
            {
                txtResponse.Text = s;
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            if (m_client != null)
            {
                m_client.Close();
                btnDisconnect.Enabled = false;
                btnConnect.Enabled = true;
            }                    
        }
    }
}