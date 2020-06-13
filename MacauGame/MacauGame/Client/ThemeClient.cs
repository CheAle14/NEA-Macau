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
    public partial class ThemeClient : Form
    {
        public ThemeClient()
        {
            InitializeComponent();
        }

        private void ThemeClient_Load(object sender, EventArgs e)
        {
            Images.ClearCache();
            var size = new Size(101, 165);
            var gapX = 3;
            var gapY = 10;
            int maxX = 0;
            int y = 3;
            foreach(var house in new Suit[] {  Suit.Heart, Suit.Diamond, Suit.Club, Suit.Spade})
            {
                int x = 3;
                foreach(Number number in Enum.GetValues(typeof(Number)))
                {
                    if (number == Number.None)
                        continue;
                    var crd = new Card(house, number);
                    var pb = new PictureBox();
                    pb.Name = crd.ImageName;
                    pb.Image = Images.GetImage(crd.ImageName) ?? Properties.Resources.ERROR;
                    pb.Size = size;
                    pb.SizeMode = PictureBoxSizeMode.Zoom;
                    pb.Location = new Point(x, y);
                    pb.Tag = crd;
                    pb.Click += Pb_Click;
                    this.Controls.Add(pb);
                    x += size.Width + gapX;
                }
                y += size.Height + gapY;
                maxX = x;
            }
            this.Size = new Size(maxX + gapX, y + gapY);
        }

        private void Pb_Click(object sender, EventArgs e)
        {
            if(sender is PictureBox pb && pb.Tag is Card card)
            {
                MessageBox.Show($"{card.Value} of {card.House}s\r\nExpecting image under:\r\n" +
                    $"%appdata%/{Program.AppDataFolderName}/images/{card.ImageName}.png");
            }
        }
    }
}
