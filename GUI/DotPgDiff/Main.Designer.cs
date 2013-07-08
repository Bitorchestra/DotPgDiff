namespace BO.DotPgDiff
{
    partial class Main
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
            if (disposing && (components != null)) {
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.sourceDescription = new System.Windows.Forms.Label();
			this.sourceConnect = new System.Windows.Forms.Button();
			this.sourceTree = new System.Windows.Forms.TreeView();
			this.splitContainer2 = new System.Windows.Forms.SplitContainer();
			this.btnCopyTarget = new System.Windows.Forms.Button();
			this.btnCopySource = new System.Windows.Forms.Button();
			this.SQLTarget = new System.Windows.Forms.TextBox();
			this.btnDiff = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.SQLSource = new System.Windows.Forms.TextBox();
			this.targetConnect = new System.Windows.Forms.Button();
			this.targetDescription = new System.Windows.Forms.Label();
			this.targetTree = new System.Windows.Forms.TreeView();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
			this.splitContainer2.Panel1.SuspendLayout();
			this.splitContainer2.Panel2.SuspendLayout();
			this.splitContainer2.SuspendLayout();
			this.SuspendLayout();
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.splitContainer1.IsSplitterFixed = true;
			this.splitContainer1.Location = new System.Drawing.Point(0, 0);
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.sourceDescription);
			this.splitContainer1.Panel1.Controls.Add(this.sourceConnect);
			this.splitContainer1.Panel1.Controls.Add(this.sourceTree);
			this.splitContainer1.Panel1MinSize = 200;
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
			this.splitContainer1.Panel2MinSize = 700;
			this.splitContainer1.Size = new System.Drawing.Size(1112, 679);
			this.splitContainer1.SplitterDistance = 277;
			this.splitContainer1.TabIndex = 0;
			// 
			// sourceDescription
			// 
			this.sourceDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.sourceDescription.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.sourceDescription.Location = new System.Drawing.Point(3, 26);
			this.sourceDescription.Name = "sourceDescription";
			this.sourceDescription.Size = new System.Drawing.Size(270, 19);
			this.sourceDescription.TabIndex = 2;
			this.sourceDescription.Text = "Not connected";
			this.sourceDescription.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// sourceConnect
			// 
			this.sourceConnect.Dock = System.Windows.Forms.DockStyle.Top;
			this.sourceConnect.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.sourceConnect.Location = new System.Drawing.Point(0, 0);
			this.sourceConnect.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
			this.sourceConnect.Name = "sourceConnect";
			this.sourceConnect.Size = new System.Drawing.Size(277, 26);
			this.sourceConnect.TabIndex = 1;
			this.sourceConnect.Text = "Connect";
			this.sourceConnect.UseVisualStyleBackColor = true;
			this.sourceConnect.Click += new System.EventHandler(this.sourceConnect_Click);
			// 
			// sourceTree
			// 
			this.sourceTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.sourceTree.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.sourceTree.Location = new System.Drawing.Point(0, 45);
			this.sourceTree.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
			this.sourceTree.Name = "sourceTree";
			this.sourceTree.Size = new System.Drawing.Size(277, 634);
			this.sourceTree.TabIndex = 0;
			this.sourceTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.sourceTree_AfterSelect);
			// 
			// splitContainer2
			// 
			this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer2.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.splitContainer2.Location = new System.Drawing.Point(0, 0);
			this.splitContainer2.Name = "splitContainer2";
			// 
			// splitContainer2.Panel1
			// 
			this.splitContainer2.Panel1.Controls.Add(this.btnCopyTarget);
			this.splitContainer2.Panel1.Controls.Add(this.btnCopySource);
			this.splitContainer2.Panel1.Controls.Add(this.SQLTarget);
			this.splitContainer2.Panel1.Controls.Add(this.btnDiff);
			this.splitContainer2.Panel1.Controls.Add(this.label1);
			this.splitContainer2.Panel1.Controls.Add(this.SQLSource);
			this.splitContainer2.Panel1MinSize = 400;
			// 
			// splitContainer2.Panel2
			// 
			this.splitContainer2.Panel2.Controls.Add(this.targetConnect);
			this.splitContainer2.Panel2.Controls.Add(this.targetDescription);
			this.splitContainer2.Panel2.Controls.Add(this.targetTree);
			this.splitContainer2.Panel2MinSize = 200;
			this.splitContainer2.Size = new System.Drawing.Size(831, 679);
			this.splitContainer2.SplitterDistance = 556;
			this.splitContainer2.TabIndex = 0;
			// 
			// btnCopyTarget
			// 
			this.btnCopyTarget.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCopyTarget.Location = new System.Drawing.Point(478, 317);
			this.btnCopyTarget.Name = "btnCopyTarget";
			this.btnCopyTarget.Size = new System.Drawing.Size(75, 23);
			this.btnCopyTarget.TabIndex = 6;
			this.btnCopyTarget.Text = "Copia";
			this.btnCopyTarget.UseVisualStyleBackColor = true;
			this.btnCopyTarget.Click += new System.EventHandler(this.btnCopyTarget_Click);
			// 
			// btnCopySource
			// 
			this.btnCopySource.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnCopySource.Location = new System.Drawing.Point(4, 317);
			this.btnCopySource.Name = "btnCopySource";
			this.btnCopySource.Size = new System.Drawing.Size(75, 23);
			this.btnCopySource.TabIndex = 5;
			this.btnCopySource.Text = "Copia";
			this.btnCopySource.UseVisualStyleBackColor = true;
			this.btnCopySource.Click += new System.EventHandler(this.btnCopySource_Click);
			// 
			// SQLTarget
			// 
			this.SQLTarget.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.SQLTarget.Location = new System.Drawing.Point(310, 346);
			this.SQLTarget.Multiline = true;
			this.SQLTarget.Name = "SQLTarget";
			this.SQLTarget.ReadOnly = true;
			this.SQLTarget.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.SQLTarget.Size = new System.Drawing.Size(244, 330);
			this.SQLTarget.TabIndex = 4;
			// 
			// btnDiff
			// 
			this.btnDiff.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.btnDiff.Enabled = false;
			this.btnDiff.Location = new System.Drawing.Point(0, 0);
			this.btnDiff.Name = "btnDiff";
			this.btnDiff.Size = new System.Drawing.Size(554, 26);
			this.btnDiff.TabIndex = 3;
			this.btnDiff.Text = "D I F F !";
			this.btnDiff.UseVisualStyleBackColor = true;
			this.btnDiff.Click += new System.EventHandler(this.btnDiff_Click);
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.label1.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(0, 277);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(554, 19);
			this.label1.TabIndex = 2;
			this.label1.Text = "SQL source";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// SQLSource
			// 
			this.SQLSource.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.SQLSource.Location = new System.Drawing.Point(3, 346);
			this.SQLSource.Multiline = true;
			this.SQLSource.Name = "SQLSource";
			this.SQLSource.ReadOnly = true;
			this.SQLSource.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.SQLSource.Size = new System.Drawing.Size(267, 330);
			this.SQLSource.TabIndex = 0;
			// 
			// targetConnect
			// 
			this.targetConnect.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.targetConnect.Location = new System.Drawing.Point(0, 0);
			this.targetConnect.Name = "targetConnect";
			this.targetConnect.Size = new System.Drawing.Size(271, 26);
			this.targetConnect.TabIndex = 3;
			this.targetConnect.Text = "Connect";
			this.targetConnect.UseVisualStyleBackColor = true;
			this.targetConnect.Click += new System.EventHandler(this.targetConnect_Click);
			// 
			// targetDescription
			// 
			this.targetDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.targetDescription.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.targetDescription.Location = new System.Drawing.Point(0, 26);
			this.targetDescription.Name = "targetDescription";
			this.targetDescription.Size = new System.Drawing.Size(271, 19);
			this.targetDescription.TabIndex = 2;
			this.targetDescription.Text = "Not connected";
			this.targetDescription.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// targetTree
			// 
			this.targetTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.targetTree.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.targetTree.Location = new System.Drawing.Point(0, 45);
			this.targetTree.Name = "targetTree";
			this.targetTree.Size = new System.Drawing.Size(271, 634);
			this.targetTree.TabIndex = 0;
			this.targetTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.targetTree_AfterSelect);
			// 
			// Main
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1112, 679);
			this.Controls.Add(this.splitContainer1);
			this.DoubleBuffered = true;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "Main";
			this.Text = "DotPgDiff";
			this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
			this.SizeChanged += new System.EventHandler(this.Main_SizeChanged);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.splitContainer2.Panel1.ResumeLayout(false);
			this.splitContainer2.Panel1.PerformLayout();
			this.splitContainer2.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
			this.splitContainer2.ResumeLayout(false);
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Label sourceDescription;
        private System.Windows.Forms.Button sourceConnect;
        private System.Windows.Forms.TreeView sourceTree;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.TreeView targetTree;
        private System.Windows.Forms.TextBox SQLSource;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button targetConnect;
		private System.Windows.Forms.Label targetDescription;
		private System.Windows.Forms.Button btnDiff;
		private System.Windows.Forms.TextBox SQLTarget;
		private System.Windows.Forms.Button btnCopyTarget;
		private System.Windows.Forms.Button btnCopySource;

    }
}

