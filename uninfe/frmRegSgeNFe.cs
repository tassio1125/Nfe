using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using uninfe;

namespace sgenfe
{
    public partial class FrmRegistro : Form
    {
        ClassSge oSge = new ClassSge();
        public FrmRegistro()
        {
            InitializeComponent();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult=DialogResult.Cancel;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            Random r = new Random();
            int i = r.Next(100000,999999);
            string sNSerirHD = oSge.GetNSerieHD();
            string registro = "SGENFE-" + sNSerirHD + "-" + i.ToString();
            registro = registro + "-" + oSge.GeraDigitoVerificadorRegistro(registro);
            this.edRegistro.Text = registro;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string chReg = oSge.GerarChave(edRegistro.Text);
            string chDig = edChave.Text;
            bool chOK = chDig == chReg;
            if (edChave.Text.Trim() == "")
            {
                MessageBox.Show("Informa a chave para o registro informado!");
                edChave.Focus();
            }
            else if ( chOK == true)
            {
                if (oSge.SalvaRegistro(edRegistro.Text, chDig)) DialogResult = DialogResult.OK;
                else DialogResult = DialogResult.Cancel;
            }
            else
            {
                MessageBox.Show("Chave inválida!");
                edChave.Focus();
            }

        }
    }
}
