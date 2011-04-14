using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace BlopsAutoKicker
{
    internal class ConnectionPool
    {

        static private Queue<Socket> pool;
        const int PoolSize = 2;
        static private string address;
        static private int port;

        static internal void InitializePool(string addr, int prt)
        {
            address = addr;
            port = prt;

            pool = new Queue<Socket>(PoolSize);
            for (int i = 0; i < PoolSize; ++i)
            {

                pool.Enqueue(CreateConnectSocket());
            }
        }

        static internal void DrainPool()
        {
            foreach (var sock in pool)
            {
                sock.Disconnect(false);
            }
        }

        static private Socket CreateConnectSocket()
        {
            Logging.WriteLog("Creating pooled socket");
            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sock.Connect(address, port);
            return sock;
        }

        static internal Socket GetConnection()
        {
            lock (pool)
            {
                if (pool.Count != 0)
                {
                    var sock = pool.Dequeue();

                    if (!sock.Connected)
                    {
                        sock = CreateConnectSocket();
                    }

                    return sock;
                }
                else
                {
                    return CreateConnectSocket();
                }
            }
        }

        static internal void RecyleConnection(Socket sock)
        {
            lock(pool)
            {
                pool.Enqueue(sock);
            }
        }
    }
}
