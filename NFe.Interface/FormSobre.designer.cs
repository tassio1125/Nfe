namespace NFe.Interface
{
    partial class FormSobre
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSobre));
            this.lblNomeAplicacao = new System.Windows.Forms.Label();
            this.lblDescricaoAplicacao = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox_licenca = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox_DataUltimaModificacao = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.textBox_versao = new System.Windows.Forms.TextBox();
            this.lblEmpresa = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // lblNomeAplicacao
            // 
            this.lblNomeAplicacao.AutoSize = true;
            this.lblNomeAplicacao.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNomeAplicacao.Location = new System.Drawing.Point(292, 11);
            this.lblNomeAplicacao.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblNomeAplicacao.Name = "lblNomeAplicacao";
            this.lblNomeAplicacao.Size = new System.Drawing.Size(149, 39);
            this.lblNomeAplicacao.TabIndex = 1;
            this.lblNomeAplicacao.Text = "SgeNFe";
            this.lblNomeAplicacao.Click += new System.EventHandler(this.lblNomeAplicacao_Click);
            // 
            // lblDescricaoAplicacao
            // 
            this.lblDescricaoAplicacao.AutoSize = true;
            this.lblDescricaoAplicacao.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDescricaoAplicacao.Location = new System.Drawing.Point(299, 49);
            this.lblDescricaoAplicacao.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblDescricaoAplicacao.Name = "lblDescricaoAplicacao";
            this.lblDescricaoAplicacao.Size = new System.Drawing.Size(258, 20);
            this.lblDescricaoAplicacao.TabIndex = 2;
            this.lblDescricaoAplicacao.Text = "Monitor da Nota Fiscal Eletrônica";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(296, 82);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(122, 17);
            this.label3.TabIndex = 3;
            this.label3.Text = "Desenvolvido por:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(13, 252);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(170, 17);
            this.label5.TabIndex = 8;
            this.label5.Text = "Autorização de utilização:";
            // 
            // textBox_licenca
            // 
            this.textBox_licenca.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_licenca.Location = new System.Drawing.Point(17, 274);
            this.textBox_licenca.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_licenca.Multiline = true;
            this.textBox_licenca.Name = "textBox_licenca";
            this.textBox_licenca.ReadOnly = true;
            this.textBox_licenca.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox_licenca.Size = new System.Drawing.Size(711, 169);
            this.textBox_licenca.TabIndex = 9;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(13, 198);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(182, 17);
            this.label6.TabIndex = 10;
            this.label6.Text = "Data da última modificação:";
            // 
            // textBox_DataUltimaModificacao
            // 
            this.textBox_DataUltimaModificacao.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_DataUltimaModificacao.Location = new System.Drawing.Point(17, 220);
            this.textBox_DataUltimaModificacao.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_DataUltimaModificacao.Name = "textBox_DataUltimaModificacao";
            this.textBox_DataUltimaModificacao.ReadOnly = true;
            this.textBox_DataUltimaModificacao.Size = new System.Drawing.Size(225, 22);
            this.textBox_DataUltimaModificacao.TabIndex = 11;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(296, 198);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(57, 17);
            this.label7.TabIndex = 13;
            this.label7.Text = "Versão:";
            // 
            // textBox_versao
            // 
            this.textBox_versao.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_versao.Location = new System.Drawing.Point(300, 219);
            this.textBox_versao.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_versao.Name = "textBox_versao";
            this.textBox_versao.ReadOnly = true;
            this.textBox_versao.Size = new System.Drawing.Size(294, 22);
            this.textBox_versao.TabIndex = 14;
            // 
            // lblEmpresa
            // 
            this.lblEmpresa.AutoSize = true;
            this.lblEmpresa.Location = new System.Drawing.Point(296, 103);
            this.lblEmpresa.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblEmpresa.Name = "lblEmpresa";
            this.lblEmpresa.Size = new System.Drawing.Size(244, 17);
            this.lblEmpresa.TabIndex = 16;
            this.lblEmpresa.Text = "G && R Sistemas e Unimake Softwares";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::NFe.Interface.Properties.Resources.logo;
            this.pictureBox1.Location = new System.Drawing.Point(16, 15);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(4);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(252, 177);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.WaitOnLoad = true;
            // 
            // FormSobre
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(745, 459);
            this.Controls.Add(this.lblEmpresa);
            this.Controls.Add(this.textBox_versao);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.textBox_DataUltimaModificacao);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.textBox_licenca);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.lblDescricaoAplicacao);
            this.Controls.Add(this.lblNomeAplicacao);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.Name = "FormSobre";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Sobre";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label lblNomeAplicacao;
        private System.Windows.Forms.Label lblDescricaoAplicacao;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox_licenca;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBox_DataUltimaModificacao;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBox_versao;
        private System.Windows.Forms.Label lblEmpresa;

    }
}