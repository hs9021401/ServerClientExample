using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ServerClientExample
{
    public enum INFO_TYPE
    {
        TYPE_STATUS = 0,
        TYPE_MESSAGE,
    }
    
    //Delegate for status or message use
    public delegate void DelegateUI(string msg, INFO_TYPE type);
    //Delegate for recording login user list
    public delegate void DelegateHandleUserList(string name, bool bConnect);
    
    internal class ServerSock
    {
        #region Delegate
        private readonly DelegateUI _delUIStatus;
        private readonly DelegateHandleUserList _delHandleUserList, _delUIUserListBox;
        #endregion
        
        private readonly int _port;
        private readonly IPAddress _ipAddress;
        private TcpListener _listener;
        private Thread _thread;

        HashSet<string> connectedUser;

        public static bool bServerTerminate { get; set; }

        public ServerSock(IPAddress iPAddress, int port, DelegateUI delUIStatus, DelegateHandleUserList delUIListBox)
        {
            _ipAddress = iPAddress;
            _port = port;
            _delUIStatus = delUIStatus; //handle the status and message that displayed on the UI
            _delHandleUserList = HandleUserList;    //handle user list in the HashSet
            _delUIUserListBox = delUIListBox;   //handle the user linst in the list box
            connectedUser = new HashSet<string>();
        }

        public void StartAndListen()
        {
            bServerTerminate = false;
            
            _listener = new TcpListener(_ipAddress, _port);
            _listener.Start();

            _thread = new Thread(new ThreadStart(Listening));
            _thread.IsBackground = true;
            _thread.Start();
        }

        public void StopServer()
        {
            bServerTerminate = true;
            
            if(_listener!=null)
                _listener.Stop();

            connectedUser.Clear();
        }

        private void Listening()
        {
            try
            {
                TcpClient clientSocket = null!;
                while (!bServerTerminate)
                {
                    _delUIStatus("Waiting for connections...", INFO_TYPE.TYPE_STATUS);

                    clientSocket = _listener.AcceptTcpClient();                    
                    HandleMultiClient handleClient = new HandleMultiClient();
                    handleClient.startClient(clientSocket, _delUIStatus, _delHandleUserList);
                }

                _delUIStatus("Client disconnected", INFO_TYPE.TYPE_STATUS);
                
                if (clientSocket != null)
                    clientSocket.Close();
                if (_listener != null)
                    _listener.Stop();
            }
            catch (SocketException ex)
            {
                //_delUIStatus($"Socket error: ${ex.Message}", INFO_TYPE.TYPE_STATUS);
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Handle login user list
        /// </summary>
        /// <param name="name">user name</param>
        /// <param name="bConnect">true for connected; false for disconnected </param>
        private void HandleUserList(string name, bool bConnect)
        {
            if(bConnect)
            {
                connectedUser.Add(name);
            }
            else
            {
                if(connectedUser.Contains(name))
                {
                    connectedUser.Remove(name);
                }
            }

            //List all connected user in the list box
            _delUIUserListBox.Invoke(name, bConnect);
        }

        public class HandleMultiClient
        {
            private TcpClient _clientSocket = null!;
            private DelegateUI _DelStatus = null!;
            private DelegateHandleUserList _DelHandleUser = null!;

            public void startClient(TcpClient client, DelegateUI delStatus, DelegateHandleUserList delHandleUser)
            {
                _clientSocket = client;
                _DelStatus = delStatus;
                _DelHandleUser = delHandleUser;                
                Thread ctThread = new Thread(doChat);
                ctThread.IsBackground = true;
                ctThread.Start();
            }

            private void doChat()
            {                
                try
                {
                    NetworkStream stream = _clientSocket.GetStream();
                    byte[] bBuffer = new byte[256];
                    string sData= "";
                    string sUser = "";
                    string sMsg = "";

                    int i;
                    _DelStatus("Client connected", INFO_TYPE.TYPE_STATUS);
                    while ((i = stream.Read(bBuffer, 0, bBuffer.Length)) != 0)
                    {
                        if (bServerTerminate)
                        {
                            byte[] bSend2 = Encoding.UTF8.GetBytes("Bye");
                            stream.Write(bSend2, 0, bSend2.Length);
                            _clientSocket.Close();
                            break;
                        }
                            
                        sData = Encoding.UTF8.GetString(bBuffer, 0, i);
                        sUser = sData.Split("**")[0];    //Get User Name
                        sMsg = sData.Split("**")[1];    //Get Message
                        _DelStatus(DateTime.Now.ToString("HH:mm:ss") + " [" + sUser + "]: " + sMsg + Environment.NewLine, INFO_TYPE.TYPE_MESSAGE);                        
                        Thread.Sleep(5);

                        //Respond to the client
                        byte[] bSend = Encoding.UTF8.GetBytes("I got your message: " + sMsg);
                        stream.Write(bSend, 0, bSend.Length);

                        //Record login user
                        _DelHandleUser.Invoke(sUser, true);
                    }

                    //Disconnect, remove user
                    _DelHandleUser.Invoke(sUser, false);
                    _DelStatus("Client Disconnected...", INFO_TYPE.TYPE_STATUS);
                }
                catch (SocketException ex)
                {
                    _DelStatus(ex.Message, INFO_TYPE.TYPE_STATUS);
                }
                catch (IOException ex)
                {
                    _DelStatus(ex.Message, INFO_TYPE.TYPE_STATUS);
                }
            }
        }
    }
}
