using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Socket_Server
{

    public class StatusChangedEventArgs: EventArgs
    {
        private string EventMsg;

        public string EventMessage
        {
            get { return EventMsg; }
            set { EventMsg = value; }
        }
    
        public StatusChangedEventArgs(string EventMsg)
        {
            this.EventMsg = EventMsg;
        }
    }
    public delegate void StatusChangedEventHandler(object sender, StatusChangedEventArgs e);

    public class Server
    {
        public static Hashtable htUsuarios = new Hashtable(30); //30 usuários limite definido
        public static Hashtable htConexoes = new Hashtable(30);
        private IPAddress enderecoIP;
        private TcpClient tcpCliente;
       
        public static event StatusChangedEventHandler StatusChanged;
        private static StatusChangedEventArgs statusChanged;
        
        public Server(IPAddress endereco)
        {
            enderecoIP = endereco;
        }

        private Thread thrListener;
        private TcpListener tlsCliente;
        bool ServRodando = false;

        public static void IncluiUsuario(TcpClient tcpUsuario, string Username)
        {
            Server.htUsuarios.Add(Username, tcpUsuario);
            Server.htConexoes.Add(tcpUsuario, Username);
            EnviaMensagemServer(htConexoes[tcpUsuario] + " entrou...");
        }

        public static void RemoveUsuario(TcpClient tcpUsuario)
        {
            if (htConexoes[tcpUsuario] != null)
            {
                EnviaMensagemServer(htConexoes[tcpUsuario] + " saiu...");

                Server.htUsuarios.Remove(Server.htConexoes[tcpUsuario]);
                Server.htConexoes.Remove(tcpUsuario);
            }
        }

        public static void OnStatusChanged(StatusChangedEventArgs e)
        {
            StatusChangedEventHandler statusHandler = StatusChanged;
            if(statusHandler != null)
            {
                statusHandler(null, e);
            }
        }

        public static void EnviaMensagemServer(string Mensagem)
        {
            StreamWriter Sender;

            statusChanged = new StatusChangedEventArgs(Mensagem);
            OnStatusChanged(statusChanged);
            TcpClient[] tcpClientes = new TcpClient[Server.htUsuarios.Count];
            Server.htUsuarios.Values.CopyTo(tcpClientes,0);
            
            for(int i = 0; i < tcpClientes.Length; i++)
            {
                try
                {
                    if(Mensagem.Trim()=="" || tcpClientes[i] == null)
                    {
                        continue;
                    }
                    Sender = new StreamWriter(tcpClientes[i].GetStream());
                    Sender.WriteLine(Mensagem);
                    Sender.Flush();
                    Sender=null;
                }
                catch
                {
                    RemoveUsuario(tcpClientes[i]);
                }
            }
        }

        public static void EnviaMensagem(string Origem, string Mensagem)
        {
            StreamWriter Sender;

            statusChanged = new StatusChangedEventArgs(Origem +" disse: "+Mensagem);
            OnStatusChanged(statusChanged);

            TcpClient[] tcpClientes = new TcpClient[Server.htUsuarios.Count];
            Server.htUsuarios.Values.CopyTo(tcpClientes, 0);
            for(int i = 0;i < tcpClientes.Length; i++)
            {
                try
                {
                    if(Mensagem.Trim()=="" || tcpClientes[i] == null)
                    {
                        continue;
                    }
                    Sender = new StreamWriter(tcpClientes[i].GetStream());
                    Sender.WriteLine(Origem +" disse: "+ Mensagem);
                    Sender.Flush();
                    Sender = null;
                }
                catch
                {
                    RemoveUsuario(tcpClientes[i]);
                }
            }
        }
        
        public void IniciaAtendimento(int porta)
        {
            try
            {
                IPAddress ipaLocal = enderecoIP;
                
                tlsCliente = new TcpListener(ipaLocal,porta);
                tlsCliente.Start();
                ServRodando = true;
                thrListener = new Thread(MantemAtendimento);
                thrListener.Start();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void MantemAtendimento()
        {
            while(ServRodando == true)
            {
                tcpCliente = tlsCliente.AcceptTcpClient();
                Conexao newConnection = new Conexao(tcpCliente);
            }
        }
    }

    public class Conexao
    {
        TcpClient tcpCliente;

        private Thread thrSender;
        private StreamReader srReceptor;
        private StreamWriter swEnviador;
        private string usuarioAtual;
        private string Resposta;

        public Conexao(TcpClient tcpCon)
        {
            tcpCliente = tcpCon;
            thrSender = new Thread(AceitaCliente);
            thrSender.Start();
        }

        private void FechaConexao()
        {
            tcpCliente.Close();
            srReceptor.Close();
            swEnviador.Close();
        }

        private void AceitaCliente()
        {
            srReceptor = new StreamReader(tcpCliente.GetStream());
            swEnviador = new StreamWriter(tcpCliente.GetStream());

            usuarioAtual = srReceptor.ReadLine();

            if(usuarioAtual != "")
            {
                if(Server.htUsuarios.Contains(usuarioAtual) == true)
                {
                    swEnviador.WriteLine("Este nome de usuário já existe.");
                    swEnviador.Flush();
                    FechaConexao();
                    return;

                }
                else
                {
                    swEnviador.WriteLine("1");
                    swEnviador.Flush();

                    Server.IncluiUsuario(tcpCliente, usuarioAtual);
                }
            }
            else
            {
                FechaConexao();
                return;
            }
            try
            {
                while((Resposta = srReceptor.ReadLine()) != "")
                {
                    if(Resposta == null)
                    {
                        Server.RemoveUsuario(tcpCliente);
                    }
                    else
                    {
                        Server.EnviaMensagem(usuarioAtual, Resposta);
                    }
                }
            }
            catch
            {
                Server.RemoveUsuario(tcpCliente);
            }
        }
    }
}
