namespace MacauGame.Client
{
    partial class GameClient
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
            this.btnHandLeft = new System.Windows.Forms.Button();
            this.btnHandRight = new System.Windows.Forms.Button();
            this.panelHand = new System.Windows.Forms.Panel();
            this.panelTable = new System.Windows.Forms.Panel();
            this.panelPlace = new System.Windows.Forms.Panel();
            this.pbPickupDeck = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbPickupDeck)).BeginInit();
            this.SuspendLayout();
            // 
            // btnHandLeft
            // 
            this.btnHandLeft.Location = new System.Drawing.Point(12, 574);
            this.btnHandLeft.Name = "btnHandLeft";
            this.btnHandLeft.Size = new System.Drawing.Size(75, 75);
            this.btnHandLeft.TabIndex = 1;
            this.btnHandLeft.Text = "button1";
            this.btnHandLeft.UseVisualStyleBackColor = true;
            // 
            // btnHandRight
            // 
            this.btnHandRight.Location = new System.Drawing.Point(1306, 574);
            this.btnHandRight.Name = "btnHandRight";
            this.btnHandRight.Size = new System.Drawing.Size(75, 75);
            this.btnHandRight.TabIndex = 2;
            this.btnHandRight.Text = "button2";
            this.btnHandRight.UseVisualStyleBackColor = true;
            // 
            // panelHand
            // 
            this.panelHand.Location = new System.Drawing.Point(93, 491);
            this.panelHand.Name = "panelHand";
            this.panelHand.Size = new System.Drawing.Size(1207, 158);
            this.panelHand.TabIndex = 3;
            // 
            // panelTable
            // 
            this.panelTable.Location = new System.Drawing.Point(12, 225);
            this.panelTable.Name = "panelTable";
            this.panelTable.Size = new System.Drawing.Size(496, 260);
            this.panelTable.TabIndex = 4;
            // 
            // panelPlace
            // 
            this.panelPlace.Location = new System.Drawing.Point(514, 225);
            this.panelPlace.Name = "panelPlace";
            this.panelPlace.Size = new System.Drawing.Size(496, 260);
            this.panelPlace.TabIndex = 5;
            // 
            // pbPickupDeck
            // 
            this.pbPickupDeck.Image = global::MacauGame.Properties.Resources.C_10;
            this.pbPickupDeck.Location = new System.Drawing.Point(1016, 225);
            this.pbPickupDeck.Name = "pbPickupDeck";
            this.pbPickupDeck.Size = new System.Drawing.Size(173, 260);
            this.pbPickupDeck.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbPickupDeck.TabIndex = 6;
            this.pbPickupDeck.TabStop = false;
            // 
            // GameClient
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1393, 661);
            this.Controls.Add(this.pbPickupDeck);
            this.Controls.Add(this.panelPlace);
            this.Controls.Add(this.panelTable);
            this.Controls.Add(this.panelHand);
            this.Controls.Add(this.btnHandRight);
            this.Controls.Add(this.btnHandLeft);
            this.Name = "GameClient";
            this.Text = "GameClient";
            ((System.ComponentModel.ISupportInitialize)(this.pbPickupDeck)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btnHandLeft;
        private System.Windows.Forms.Button btnHandRight;
        private System.Windows.Forms.Panel panelHand;
        private System.Windows.Forms.Panel panelTable;
        private System.Windows.Forms.Panel panelPlace;
        private System.Windows.Forms.PictureBox pbPickupDeck;
    }
}