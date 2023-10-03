using System;
using System.Net;
using System.Windows.Forms;

namespace Socket_Server
{
    public partial class FrmServer : Form 
    {
        public FrmServer()
        {
       
            InitializeComponent();
        }
        
        private delegate void AtualizaStatusCallback(string Mensagem);

        private void btnConectar_Click(object sender, EventArgs e)
        {
            if(string.IsNullOrEmpty(txtIp.Text))
                {
                MessageBox.Show("Informe o endereço IP.");
                txtIp.Focus();
                return;
            }
            try
            {
                IPAddress enderecoIP = IPAddress.Parse(txtIp.Text);

                Server mainServidor = new Server(enderecoIP);
                int porta = VerificaCaracteresValidos(txtPorta.Text);
             
                if(porta != 0)
                {
                    Server.StatusChanged += new StatusChangedEventHandler(mainServidor_StatusChanged);
                    mainServidor.IniciaAtendimento(porta);
                    txtMsgRecebida.AppendText("Conectando...\r\n");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro de conexão:" + ex.Message);
            }
        }

        private int VerificaCaracteresValidos(string text)
        {
            bool converte = int.TryParse(text, out int numero);

            if (converte) // if (converte == true) if (!converte)
            {
                return numero;
            }
            else
            {
                MessageBox.Show("Insira apenas números.");
                txtPorta.Text = "";
            }
            return 0;
        }

        public void mainServidor_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            this.Invoke(new AtualizaStatusCallback(this.AtualizaStatus), new object[] { e.EventMessage });
        }

        private void AtualizaStatus(string Mensagem)
        {
            txtMsgRecebida.AppendText(Mensagem + "\r\n");
        }
    }
}
