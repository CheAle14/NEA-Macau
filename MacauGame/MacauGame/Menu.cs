using MacauGame.Client;
using MacauGame.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MacauGame
{
    public partial class Menu : Form
    {
        public Menu()
        {
            InitializeComponent();
            Instance = this;
        }
        public static Menu Instance { get; set; }
        public MacauServer Server { get; set; }
        public MacauClient Client { get; set; }

        void update()
        {
            btnServer.Enabled = Server == null;
            btnClient.Enabled = Client == null;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            update();
            if (Server != null)
                return;
            Server = new MacauServer();
            Server.Show();
        }

        private void Menu_Load(object sender, EventArgs e)
        {

        }

        private void btnClient_Click(object sender, EventArgs e)
        {
            update();
            if (Client != null) 
                return;
            Client = new MacauClient();
            Client.Show();
        }

        private void Menu_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(Server != null || Client != null)
            {
                this.Hide();
                e.Cancel = true;
            }
        }

        private void Menu_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar == 't')
            {
                var theme = new ThemeClient();
                theme.ShowDialog();
            }
        }
    }
}
