using MacauEngine.Models;
using MacauEngine.Models.Enums;
using MacauEngine.Validators;
using MacauGame.Server;
using Newtonsoft.Json.Linq;
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
    public partial class GameClient : Form
    {
        public Player SelfPlayer { get; set; }
        public Client.MacauClient Client { get; set; }
        Table table = new Table();
        List<Player> Players = new List<Player>();

        PlayerSlot PlayerA;
        PlayerSlot PlayerB;
        PlayerSlot PlayerC;
        PlayerSlot PlayerD;
        PlayerSlot PlayerE;

        public GameClient(MacauClient client)
        {
            Client = client;
            InitializeComponent();
            PlayerA = new PlayerSlot()
            {
                Label = lblPlayerAName,
                Image = pbPlayerA,
                Slot = 'A'
            };
            PlayerB = new PlayerSlot()
            {
                Label = lblPlayerBName,
                Image = pbPlayerB,
                Slot = 'B'
            };
            PlayerC = new PlayerSlot()
            {
                Label = lblPlayerCName,
                Image = pbPlayerC,
                Slot = 'C'
            };
            PlayerD = new PlayerSlot()
            {
                Label = lblPlayerDName,
                Image = pbPlayerD,
                Slot = 'D'
            };
            PlayerE = new PlayerSlot()
            {
                Label = lblPlayerEName,
                Image = pbPlayerE,
                Slot = 'E'
            };
        }
        public void Send(Packet packet) => Client.Send(packet);
        public void GetResponse(Packet packet, int timeout = 30000) => Client.GetResponse(packet, timeout);

        public void HandleGamePacket(Packet packet)
        {
            if(packet.Id == PacketId.ProvideGameInfo)
            {
                var jobj = packet.Content;
                var playerArray = (JArray)jobj["players"];
                Players = new List<Player>();
                foreach(var plyObj in playerArray)
                {
                    var player = new Player((JObject)plyObj);
                    Players.Add(player);
                }
                SelfPlayer = Players.FirstOrDefault(x => x.Id == UHWID.UHWIDEngine.AdvancedUid);
                DisplayPlayers();
            } else if (packet.Id == PacketId.BulkPickupCards)
            {
                var jary = (JArray)packet.Content;
                foreach(JObject jobj in jary)
                {
                    var card = new Card(jobj);
                    SelfPlayer.Hand.Add(card);
                }
            }
        }

        void resetPlayers()
        {
            foreach(var thing in new PlayerSlot[] { PlayerA, PlayerB, PlayerC, PlayerD, PlayerE })
            {
                thing.Reset();
            }
        }

        public void DisplayPlayers()
        {
            if(SelfPlayer == null)
                return;
            if(this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    DisplayPlayers();
                }));
                return;
            }
            var selfIndex = Players.IndexOf(SelfPlayer);
            var shiftingToFront = new List<Player>();
            for(int i = selfIndex + 1; i < Players.Count; i++)
            {
                var player = Players[i];
                shiftingToFront.Add(player);
            }
            resetPlayers();
            if(shiftingToFront.Count == 1)
            {
                PlayerB.Set(shiftingToFront[0]);
            } else if (shiftingToFront.Count == 2)
            {
                PlayerA.Set(shiftingToFront[0]);
                PlayerC.Set(shiftingToFront[1]);
            } else if (shiftingToFront.Count == 3)
            {
                PlayerA.Set(shiftingToFront[0]);
                PlayerB.Set(shiftingToFront[1]);
                PlayerC.Set(shiftingToFront[2]);
            } else if (shiftingToFront.Count == 4)
            {
                PlayerD.Set(shiftingToFront[0]);
                PlayerA.Set(shiftingToFront[1]);
                PlayerB.Set(shiftingToFront[2]);
                PlayerC.Set(shiftingToFront[3]);
            } else if (shiftingToFront.Count == 5)
            {
                PlayerD.Set(shiftingToFront[0]);
                PlayerA.Set(shiftingToFront[1]);
                PlayerB.Set(shiftingToFront[2]);
                PlayerC.Set(shiftingToFront[3]);
                PlayerE.Set(shiftingToFront[4]);
            }
        }

        List<Card> selectedProposedCards = new List<Card>();
        public void DisplayHand()
        {
            var size = new Size(101, 165);
            int x = 3;
            int y = 33;
            var orderedHand = SelfPlayer.Hand
                .OrderByDescending(c => c.IsSpecialCard)
                .ThenByDescending(c => c.IsPickupCard)
                .ThenByDescending(c => c.IsDefenseCard)
                .ThenByDescending(c => c.Value)
                .ThenByDescending(c => c.House);
            panelHand.Controls.Clear();
            foreach(var card in orderedHand)
            {
                var pb = new PictureBox();
                pb.Name = $"pb_{card.ImageName}";
                pb.Image = Images.GetImage(card.ImageName);
                pb.Size = size;
                pb.SizeMode = PictureBoxSizeMode.StretchImage;
                pb.Location = new Point(x, y);
                pb.Tag = card;
                pb.Click += PbSelectCardPlace;
                var lbl = new Label();
                lbl.Location = new Point(x, y + (size.Height - 30));
                lbl.AutoSize = false;
                lbl.Size = new Size(size.Width, 30);
                lbl.TextAlign = ContentAlignment.MiddleCenter;
                lbl.Text = "Hidden";
                lbl.Name = $"lbl_{card.ImageName}";
                lbl.Tag = card;
                panelHand.Controls.Add(lbl);
                panelHand.Controls.Add(pb);
                pb.BringToFront();
                x += size.Width + 5;
            }
        }

        private void PbSelectCardPlace(object sender, EventArgs e)
        {
            if (!(sender is PictureBox pb && pb.Tag is Card card))
                return;
            if(selectedProposedCards.Contains(card))
            {
                selectedProposedCards.Remove(card);
                pb.Location = new Point(pb.Location.X, pb.Location.Y + 30);
            } else
            {
                selectedProposedCards.Add(card);
                var validator = new PlaceValidator(selectedProposedCards, table.ShowingCards.Last());
                var result = validator.Validate();
                if(result.IsSuccess == false)
                {
                    selectedProposedCards.Remove(card);
                    MessageBox.Show(result.ErrorReason, "Invalid Placement", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                pb.Location = new Point(pb.Location.X, pb.Location.Y - 30);
            }
            int i = 1;
            foreach(var select in selectedProposedCards)
            {
                var lbl = panelHand.GetControl(x => x.Name == $"lbl_{select.ImageName}");
                lbl.Text = $"#{i++}";
            }
        }

        void flip(PictureBox pb)
        {
            Image flipImage = pb.Image;
            flipImage.RotateFlip(RotateFlipType.Rotate90FlipXY);
            pb.Image = flipImage;
        }

        double getGapModifier(int numCards)
        {
            if (numCards <= 5)
                return 0.9;
            var value = 0.9 - (numCards / 100d);
            return Math.Min(0.7, value);
        }

        string determineEffect(List<Card> cards)
        {
            if (cards.Count == 0)
                return "No cards placed";
            var active = cards.Where(x => x.IsActive).ToList();
            if (active.Count == 0)
                return "None.";
            var pickup = active.Sum(x => x.PickupValue);
            if (pickup > 0)
                return $"Pickup {pickup} card{(pickup == 1 ? "" : "s")}";

            var fours = active.Select(x => x.Value == Number.Four).ToList();
            if (fours.Count > 0)
                return $"Miss {fours.Count} turn{(fours.Count == 1 ? "" : "s")}";

            var topcard = cards.Last();
            if (topcard.Value == Number.Ace)
                return $"Suit changed: {topcard.AceSuit.Value}";
            return "None.";
        }

        public void DisplayTableCards(List<Card> cards)
        {
            panelTable.Controls.Clear();
            if (cards.Count == 0)
                return;
            var size = new Size(101, 165);
            int gap = (int)(size.Width * getGapModifier(cards.Count)); // have some overlap to signify top/bottom
            int maxX = (gap * cards.Count) + 3 + 3;
            int remSize = panelTable.Size.Width - maxX;
            int leftPadding = (int)(remSize / 2);
            if (leftPadding <= 3)
                leftPadding = 3;
            int x = leftPadding;
            int y = 3;
            foreach (var card in cards)
            {
                var pb = new PictureBox();
                pb.Name = $"pb_{x}{y}";
                pb.Tag = card;
                pb.Image = Images.GetImage(card.ImageName);
                pb.Location = new Point(x, y);
                pb.Size = size;
                pb.SizeMode = PictureBoxSizeMode.StretchImage;
                pb.Click += Pb_Click;
                panelTable.Controls.Add(pb);
                pb.BringToFront();
                x += gap;
            }
            lblTableEffect.Text = "Effect:\r\n" + determineEffect(cards);
        }

        private void Pb_Click(object sender, EventArgs e)
        {
            if(sender is PictureBox pb && pb.Tag is Card card)
            {
                MessageBox.Show(card.ToString());
            }
        }

        private void GameClient_Load(object sender, EventArgs e)
        {
            pbPlayerA.Image = Images.BACK;
            flip(pbPlayerD);
            flip(pbPlayerE);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click_1(object sender, EventArgs e)
        {
        }

        private void pbPlayerA_Click(object sender, EventArgs e)
        {
            var card = table.DrawCard();
            if (card.IsPickupCard || card.Value == Number.Four)
                card.IsActive = true;
            if(card.Value == Number.Seven)
            {
                table.ShowingCards.ForEach(x =>
                {
                    x.IsActive = false;
                });
            }
            if(card.Value == Number.Ace)
            {
                card.AceSuit = Suit.Spade;
                if (card.House == Suit.Spade)
                    card.AceSuit = Suit.Diamond;
            }
            table.ShowingCards.Add(card);
            if(table.ShowingCards.Count > 5)
            {
                table.ShowingCards.RemoveAt(0);
            }
            DisplayTableCards(table.ShowingCards);
        }

        #region Player Slot Handling

        class PlayerSlot
        {
            public char Slot { get; set; }
            public Label Label { get; set; }
            public PictureBox Image { get; set; }
            public void Set(Player player)
            {
                Label.Visible = true;
                Image.Visible = true;
                Label.Text = player.Name;
            }
            public void Reset()
            {
                Label.Visible = false;
                Label.ForeColor = Color.Black;
                Image.Visible = false;
            }
        }

        #endregion

        private void pbPlayerB_Click(object sender, EventArgs e)
        {
            SelfPlayer = SelfPlayer ?? new Player("aaaa", "Alex");
            SelfPlayer.Hand = SelfPlayer.Hand ?? new List<Card>();
            SelfPlayer.Hand.Add(table.DrawCard());
            DisplayHand();
        }
    }
}
