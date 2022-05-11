using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerClientExample
{
    public partial class ServerMain : Form
    {
        ServerSock serverSock;

        public ServerMain()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {                
                serverSock = new ServerSock(IPAddress.Parse(txtIP.Text), int.Parse(txtPort.Text), UpdateStatus, HandleUserList);
                serverSock.StartAndListen();
                btnStart.Enabled = false;
                btnStop.Enabled = true;
            }
            catch (SocketException ex)
            {
                UpdateStatus($"Socket error: ${ex.Message}", INFO_TYPE.TYPE_STATUS);
            }   
        }

        private void UpdateStatus(string v, INFO_TYPE t)
        {
            if (this.InvokeRequired)
            {
                Action<string, INFO_TYPE> action = new Action<string, INFO_TYPE>(UpdateStatus);
                this.Invoke(action, new object[] { v, t});
            }
            else 
            {
                if(t == INFO_TYPE.TYPE_STATUS)
                {
                    lblStatus.Text = v;
                }
                else if(t == INFO_TYPE.TYPE_MESSAGE)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(v);
                    sb.Append(txtMsg.Text);
                    txtMsg.Text = sb.ToString();
                }
            }
        }

        private void HandleUserList(string name, bool operate)
        {
            if (this.InvokeRequired)
            {
                Action<string, bool> action = new Action<string, bool>(HandleUserList);
                this.Invoke(action, new object[] { name, operate });
            }
            else
            {
                if(operate)
                {
                    if (!this.lstUser.Items.Contains(name))
                        this.lstUser.Items.Add(name);
                }
                else
                {
                    if (this.lstUser.Items.Contains(name))
                        this.lstUser.Items.Remove(name);
                }
                
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if(serverSock != null)
                serverSock.StopServer();
            UpdateStatus("Server Down", INFO_TYPE.TYPE_STATUS);
            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }

        private void ServerMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            btnStop.PerformClick();
        }
    }
}