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

        public static string WaitingForId { get; set; }

        public bool IsCurrentPlayer => WaitingForId != null && SelfPlayer != null && WaitingForId == SelfPlayer.Id;
        public bool HasFinished { get; set; }

        Table table = new Table();
        List<Player> Players = new List<Player>();

        PlayerSlot PlayerA;
        PlayerSlot PlayerB;
        PlayerSlot PlayerC;
        PlayerSlot PlayerD;
        PlayerSlot PlayerE;

        bool CanInteract = false;

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

        public void UpdateUI()
        {
            if(this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateUI()));
                return;
            }
            btnPlace.Visible = !HasFinished && IsCurrentPlayer;
            btnAltAction.Visible = !HasFinished && IsCurrentPlayer;
            //panelTable.HorizontalScroll.Maximum = panelTable.Width;
            //panelTable.AutoScrollPosition = new Point(panelTable.Width - 50, 0);
            CanInteract = !HasFinished && IsCurrentPlayer;
            DisplayPlayers();
            if (HasFinished)
            {
                MessageBox.Show($"Game has finished!", "Game Over", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void HandleGamePacket(Packet packet)
        {
            if (packet.Id == PacketId.ProvideGameInfo)
            {
                var jobj = packet.Content;
                var playerArray = (JArray)jobj["players"];
                Players = new List<Player>();
                foreach (var plyObj in playerArray)
                {
                    var player = new Player((JObject)plyObj);
                    Players.Add(player);
                }
                SelfPlayer = Players.FirstOrDefault(x => x.Id == Client.SELF_HWID);
                this.Invoke(new Action(() =>
                {
                    DisplayPlayers();
                    lblCardHint.Visible = true;
                    lblTableEffect.Visible = true;
                }));
            } else if (packet.Id == PacketId.BulkPickupCards)
            {
                var jary = (JArray)packet.Content;
                foreach (JObject jobj in jary)
                {
                    var card = new Card(jobj);
                    SelfPlayer.Hand.Add(card);
                }
                this.Invoke(new Action(() =>
                {
                    DisplayHand();
                }));
            } else if (packet.Id == PacketId.NewCardsPlaced)
            {
                var jary = (JArray)packet.Content;
                foreach (JObject jobj in jary)
                {
                    var card = new Card(jobj);
                    table.ShowingCards.Add(card);
                }
                this.Invoke(new Action(() =>
                {
                    DisplayTableCards();
                }));
            } else if (packet.Id == PacketId.WaitingOn)
            {
                var id = packet.Content.ToObject<string>();
                WaitingForId = id;
                UpdateUI();
            } else if (packet.Id == PacketId.ClearActive)
            {
                table.ShowingCards.ForEach(x => x.IsActive = false);
                this.Invoke(new Action(() =>
                {
                    DisplayTableCards();
                }));
            } else if (packet.Id == PacketId.NewPlayerJoined)
            {
                var player = new Player((JObject)packet.Content);
                Players.Add(player);
                Players = Players.OrderBy(x => x.Order).ToList();
                this.Invoke(new Action(() =>
                {
                    DisplayPlayers();
                }));
            } else if (packet.Id == PacketId.PlayerFinished)
            {
                var who = packet.Content["id"].ToObject<string>();
                var player = Players.First(x => x.Id == who);
                player.Hand = new List<Card>();
                player.FinishedPosition = System.Threading.Interlocked.Increment(ref Player._position);
                HasFinished = packet.Content["game_ended"].ToObject<bool>();
                this.Invoke(new Action(() => UpdateUI()));
            } else if (packet.Id == PacketId.Message)
            {
                MessageBox.Show(packet.Content.ToObject<string>(), "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            if (!CanInteract)
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
            var active = cards.Where(x => x.IsActive || x.AceSuit.HasValue).ToList();
            if (active.Count == 0)
                return "None.";
            int pickup = 0;
            int missing = 0;
            foreach(var card in active)
            {
                if(card.Value == Number.Seven)
                {
                    pickup = 0;
                    missing = 0;
                } else if (card.Value == Number.Four)
                {
                    missing++;
                } else if (card.IsPickupCard)
                {
                    pickup += card.PickupValue;
                }
            }
            if (pickup > 0)
                return $"Pickup {pickup} card{(pickup == 1 ? "" : "s")}";

            if (missing > 0)
                return $"Miss {missing} turn{(missing == 1 ? "" : "s")}";

            var topcard = cards.Last();
            if (topcard.Value == Number.Ace)
                return $"Suit changed: {topcard.AceSuit.Value}";
            return "None.";
        }

        #region Displaying Table Cards

        int tblocation = 0;
        private void PanelTable_MouseWheel(object sender, MouseEventArgs e)
        {/*
            if(e.Delta < 0)
            {
                // scroll right
                if (tblocation + 20 < panelTable.HorizontalScroll.Maximum)
                {
                    tblocation += 20;
                    panelTable.HorizontalScroll.Value = tblocation;
                }
                else
                {
                    tblocation = panelTable.HorizontalScroll.Maximum;
                    panelTable.AutoScrollPosition = new Point(tblocation, 0);
                }
            } else
            {
                if (tblocation - 20 > 0)
                {
                    tblocation -= 20;
                    panelTable.HorizontalScroll.Value = tblocation;
                }
                else
                {
                    // If scroll position is below 0 set the position to 0 (MIN)
                    tblocation = 0;
                    panelTable.AutoScrollPosition = new Point(tblocation, 0);
                }
            }*/
        }

        public void DisplayTableCards()
        {
            panelTable.Controls.Clear();
            if (table.ShowingCards.Count == 0)
                return;
            var size = new Size(101, 165);
            int gap = (int)(size.Width * getGapModifier(table.ShowingCards.Count)); // have some overlap to signify top/bottom
            int maxX = (gap * table.ShowingCards.Count) + 3 + 3;
            int remSize = panelTable.Size.Width - maxX;
            int leftPadding = (int)(remSize / 2);
            if (leftPadding <= 3)
                leftPadding = 3;
            int x = leftPadding;
            int y = 3;
            foreach (var card in table.ShowingCards)
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
            string effect = determineEffect(table.ShowingCards);
            lblTableEffect.Text = "Effect:\r\n" + effect;
            btnAltAction.Visible = true;
            btnAltAction.Text = effect.StartsWith("Miss")
                ? "Skip Turn"
                : "Pickup";
            panelTable.HorizontalScroll.Value = panelTable.HorizontalScroll.Maximum;
            //panelTable.HorizontalScroll.Maximum = panelTable.Width;
        }

        #endregion

        private void Pb_Click(object sender, EventArgs e)
        {
            if(sender is PictureBox pb && pb.Tag is Card card)
            {
                MessageBox.Show(card.ToString());
            }
        }

        private void GameClient_Load(object sender, EventArgs e)
        {
#if DEBUG
            this.Text = Program.Configuration.Name; 
#else
            this.Text = "Macau";
#endif
            flip(pbPlayerD);
            flip(pbPlayerE);
            panelTable.AutoScroll = true;
            //panelTable.MouseWheel += PanelTable_MouseWheel;
            //panelTable.HorizontalScroll.Maximum = panelTable.Width;
            //panelTable.AutoScrollPosition = new Point(0, 0);
            UpdateUI();
        }

        private void pbPlayerA_Click(object sender, EventArgs e)
        {
            return;
#if DEBUG
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
            DisplayTableCards();
#endif
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
                Image.Visible = player.FinishedPosition.HasValue == false;
                Label.Text = player.Name;
                Label.ForeColor = player.Id == GameClient.WaitingForId ? Color.Red : Color.Black;
                if(player.FinishedPosition.HasValue)
                {
                    int pos = player.FinishedPosition.Value;
                    Label.Text = $"{pos}. {player.Name}";
                    if (pos == 1)
                        Label.ForeColor = Color.Gold;
                    else if (pos == 2)
                        Label.ForeColor = Color.Silver;
                    else if (pos == 3)
                        Label.ForeColor = Color.Brown;
                }
            }
            public void Reset()
            {
                Label.Visible = false;
                Label.ForeColor = Color.Black;
                Image.Visible = false;
                Label.ForeColor = Color.Black;
            }
        }

        void resetPlayers()
        {
            foreach (var thing in new PlayerSlot[] { PlayerA, PlayerB, PlayerC, PlayerD, PlayerE })
            {
                thing.Reset();
            }
        }

        public void DisplayPlayers()
        {
            if (SelfPlayer == null)
                return;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    DisplayPlayers();
                }));
                return;
            }
            var selfIndex = Players.IndexOf(SelfPlayer);
            var shiftingToFront = new List<Player>();
            for (int i = selfIndex + 1; i < Players.Count; i++)
            {
                var player = Players[i];
                shiftingToFront.Add(player);
            }
            for(int i = 0; i < selfIndex; i++)
            {
                var player = Players[i];
                shiftingToFront.Add(player);
            }
            resetPlayers();
            if (shiftingToFront.Count == 1)
            {
                PlayerB.Set(shiftingToFront[0]);
            }
            else if (shiftingToFront.Count == 2)
            {
                PlayerA.Set(shiftingToFront[0]);
                PlayerC.Set(shiftingToFront[1]);
            }
            else if (shiftingToFront.Count == 3)
            {
                PlayerA.Set(shiftingToFront[0]);
                PlayerB.Set(shiftingToFront[1]);
                PlayerC.Set(shiftingToFront[2]);
            }
            else if (shiftingToFront.Count == 4)
            {
                PlayerD.Set(shiftingToFront[0]);
                PlayerA.Set(shiftingToFront[1]);
                PlayerB.Set(shiftingToFront[2]);
                PlayerC.Set(shiftingToFront[3]);
            }
            else if (shiftingToFront.Count == 5)
            {
                PlayerD.Set(shiftingToFront[0]);
                PlayerA.Set(shiftingToFront[1]);
                PlayerB.Set(shiftingToFront[2]);
                PlayerC.Set(shiftingToFront[3]);
                PlayerE.Set(shiftingToFront[4]);
            }
        }
#endregion

        private void pbPlayerB_Click(object sender, EventArgs e)
        {
            return;
            SelfPlayer = SelfPlayer ?? new Player("aaaa", "Alex");
            SelfPlayer.Hand = SelfPlayer.Hand ?? new List<Card>();
            SelfPlayer.Hand.Add(table.DrawCard());
            DisplayHand();
        }

        private void btnPlace_Click(object sender, EventArgs e)
        {
            if (!CanInteract)
                return;
            if(selectedProposedCards.Count == 0)
            {
                MessageBox.Show("No cards are selected to be placed.\r\nIf you cannot select any cards, you will have to pickup", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var validator = new PlaceValidator(selectedProposedCards, table.ShowingCards.Last());
            var result = validator.Validate();
            if(result.IsSuccess == false)
            { // Technically, I am validating twice.
                // But if they place two cards in order, then remove the first, the second may not nessecarily be valid
                MessageBox.Show(result.ErrorReason, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var jArray = new JArray();
            var lastCard = selectedProposedCards.Last();
            if(lastCard.Value == Number.Ace && lastCard.AceSuit.HasValue == false)
            {
                var form = new AceSelect();
                form.ShowDialog();
                lastCard.AceSuit = form.Selection ?? lastCard.House;
            }
            foreach (var card in selectedProposedCards)
                jArray.Add(card.ToJson());
            var request = new Packet(PacketId.PlaceCards, jArray);
            CanInteract = false;
            Client.GetResponse(request, response =>
            {
                placeResponse(request, response);
            });
        }

        void placeResponse(Packet request, Packet response)
        {
            if (response == null)
            {
                var thing = MessageBox.Show("Server did not respond", "Place Cards", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);
                if (thing == DialogResult.Retry)
                    Client.GetResponse(request, x => { placeResponse(request, x); });
                return;
            }
            if (response.Id == PacketId.Error)
            {
                MessageBox.Show(response.Content.ToObject<string>(), "Place Cards", MessageBoxButtons.OK);
                CanInteract = true;
            }
            if(response.Id == PacketId.Success)
            {
                SelfPlayer.Hand.RemoveAll(x => selectedProposedCards.Contains(x));
                selectedProposedCards.Clear();
                this.Invoke(new Action(() =>
                {
                    DisplayHand();
                }));
            }
        }

        private void btnVoteStart_Click(object sender, EventArgs e)
        {
            Client.GetResponse(new Packet(PacketId.VoteStartGame, JValue.CreateNull()), x =>
            {
                if(x != null)
                {
                    MessageBox.Show(x.Content.ToObject<string>(), "Vote To Start");
                }
            });
            btnVoteStart.Enabled = false;
        }

        private void btnAltAction_Click(object sender, EventArgs e)
        {
            var packet = new Packet(
                btnAltAction.Text.StartsWith("Skip")
                ? PacketId.IndicateSkipsTurn
                : PacketId.IndicatePickupCard, JValue.CreateNull());
            CanInteract = false;
            Client.GetResponse(packet, x =>
            {
                handleAltAction(packet, x);
            });
        }

        void handleAltAction(Packet request, Packet response)
        {
            if(response == null)
            {
                var result = MessageBox.Show("Sever did not respond", "Alternate Action", MessageBoxButtons.RetryCancel);
                if(result == DialogResult.Retry)
                {
                    Client.GetResponse(request, y => { handleAltAction(request, y); });
                }
                return;
            }
            if(response.Id == PacketId.Error)
            {
                MessageBox.Show(response.Content.ToObject<string>(), "Alternate Action", MessageBoxButtons.OK, MessageBoxIcon.Error);
                CanInteract = true;
            }
            if(response.Id == PacketId.BulkPickupCards)
            {
                HandleGamePacket(response);
            }
        }
    }
}
