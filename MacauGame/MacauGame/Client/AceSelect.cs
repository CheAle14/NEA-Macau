using MacauEngine.Models;
using MacauEngine.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MacauGame.Client
{
    public partial class AceSelect : Form
    {
        public AceSelect()
        {
            InitializeComponent();
        }

        public Suit? Selection { get; set; }

        private void AceSelect_Load(object sender, EventArgs e)
        {
            this.Text = "Select Ace";
            var size = new Size(101, 165);
            var gapX = 3;
            var gapY = 10;
            int y = 3;
            int x = 3;
            foreach (var house in new Suit[] { Suit.Club, Suit.Spade, Suit.Diamond, Suit.Heart})
            {
                var crd = new Card(house, Number.Ace);
                var pb = new PictureBox();
                pb.Name = crd.ImageName;
                pb.Image = Images.GetImage(crd.ImageName) ?? Properties.Resources.ERROR;
                pb.Size = size;
                pb.SizeMode = PictureBoxSizeMode.Zoom;
                pb.Location = new Point(x, y);
                pb.Tag = house;
                pb.Click += Pb_Click;
                this.Controls.Add(pb);
                x += size.Width + gapX;
            }
            y += size.Height + gapY;
            this.Size = new Size(x + gapX, y + gapY);
        }

        private void Pb_Click(object sender, EventArgs e)
        {
            if(sender is PictureBox pb && pb.Tag is Suit suit)
            {
                Selection = suit;
                this.Close();
            }
        }
    }
}
