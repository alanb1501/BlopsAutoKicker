using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace BlopsAutoKicker
{
    /// <summary>
    /// Encapsulates RCON Commands
    /// </summary>
    internal class RCON
    {
        private string Password { get; set; }

        internal RCON(string address, int port, string pw)
        {
            Password = pw;
            ConnectionPool.InitializePool(address, port);
        }

        internal void Kick(string playerName)
        {
            SendCommand(String.Concat("kick ", playerName));
        }

        internal void ClientKick(string id)
        {
            SendCommand(String.Concat("clientkick ", id));
        }

        internal void PermaBan(Guid Guid)
        {

        }

        internal Guid GetPlayerGuid(string playerName)
        {
            return Guid.Empty;
        }

        internal void Say(string message, string player = null)
        {
            if (player == null)
            {
                SendCommand(String.Concat("say ", message));
            }
            else
            {
                SendCommand(String.Format("tell {0} {1}", player, message));
            }
        }

        private void SendCommand(string rconCommand)
        {
            var client = ConnectionPool.GetConnection();
           
            string command = String.Concat(Password," ",rconCommand);
            byte[] bufferTemp = Encoding.ASCII.GetBytes(command);
            byte[] bufferSend = new byte[bufferTemp.Length + 5];

            //intial 5 characters as per standard
            bufferSend[0] = byte.Parse("255");
            bufferSend[1] = byte.Parse("255");
            bufferSend[2] = byte.Parse("255");
            bufferSend[3] = byte.Parse("255");
            bufferSend[4] = byte.Parse("00");
            int j = 5;

            for (int i = 0; i < bufferTemp.Length; i++)
            {
                bufferSend[j++] = bufferTemp[i];
            }

            //send rcon command and get response
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            client.SendTimeout = 1000;

            try
            {
                client.Send(bufferSend, SocketFlags.None);
            }
            catch (SocketException ex)
            {
                ConnectionPool.RecyleConnection(client);
                client = ConnectionPool.GetConnection();
                client.Send(bufferSend, SocketFlags.None); //resent
                Logging.WriteLog("Send Socket Exception: " + ex.Message);
            }

            //big enough to receive response
            byte[] bufferRec = new byte[10000];
            client.ReceiveTimeout = 1000;

            try
            {
                client.Receive(bufferRec);
            }
            catch (SocketException ex)
            {
                //Logging.WriteLog("Recv Socket Exception: " + ex.Message);
            }

            ConnectionPool.RecyleConnection(client);
        }
       
    }
}
