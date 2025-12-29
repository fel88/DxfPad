namespace DxfPad
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            erctangleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            circleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            polylineToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            closeLineToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripButton2 = new System.Windows.Forms.ToolStripButton();
            toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            toolStripDropDownButton2 = new System.Windows.Forms.ToolStripDropDownButton();
            randomSolveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripDropDownButton3 = new System.Windows.Forms.ToolStripDropDownButton();
            linearSizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            panel1 = new System.Windows.Forms.Panel();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripDropDownButton1, toolStripButton2, toolStripButton1, toolStripDropDownButton2, toolStripDropDownButton3 });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(988, 25);
            toolStrip1.TabIndex = 0;
            toolStrip1.Text = "toolStrip1";
            // 
            // toolStripDropDownButton1
            // 
            toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { erctangleToolStripMenuItem, circleToolStripMenuItem, polylineToolStripMenuItem, closeLineToolStripMenuItem });
            toolStripDropDownButton1.Image = Properties.Resources.plus;
            toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            toolStripDropDownButton1.Size = new System.Drawing.Size(56, 22);
            toolStripDropDownButton1.Text = "add";
            // 
            // erctangleToolStripMenuItem
            // 
            erctangleToolStripMenuItem.Name = "erctangleToolStripMenuItem";
            erctangleToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            erctangleToolStripMenuItem.Text = "rectangle";
            erctangleToolStripMenuItem.Click += erctangleToolStripMenuItem_Click;
            // 
            // circleToolStripMenuItem
            // 
            circleToolStripMenuItem.Name = "circleToolStripMenuItem";
            circleToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            circleToolStripMenuItem.Text = "circle";
            circleToolStripMenuItem.Click += circleToolStripMenuItem_Click;
            // 
            // polylineToolStripMenuItem
            // 
            polylineToolStripMenuItem.Name = "polylineToolStripMenuItem";
            polylineToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            polylineToolStripMenuItem.Text = "polyline";
            polylineToolStripMenuItem.Click += polylineToolStripMenuItem_Click;
            // 
            // closeLineToolStripMenuItem
            // 
            closeLineToolStripMenuItem.Name = "closeLineToolStripMenuItem";
            closeLineToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            closeLineToolStripMenuItem.Text = "close line";
            closeLineToolStripMenuItem.Click += closeLineToolStripMenuItem_Click;
            // 
            // toolStripButton2
            // 
            toolStripButton2.Image = Properties.Resources.magnifier;
            toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripButton2.Name = "toolStripButton2";
            toolStripButton2.Size = new System.Drawing.Size(53, 22);
            toolStripButton2.Text = "fit all";
            toolStripButton2.Click += toolStripButton2_Click;
            // 
            // toolStripButton1
            // 
            toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            toolStripButton1.Image = (System.Drawing.Image)resources.GetObject("toolStripButton1.Image");
            toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripButton1.Name = "toolStripButton1";
            toolStripButton1.Size = new System.Drawing.Size(63, 22);
            toolStripButton1.Text = "export dxf";
            toolStripButton1.Click += toolStripButton1_Click;
            // 
            // toolStripDropDownButton2
            // 
            toolStripDropDownButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            toolStripDropDownButton2.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { randomSolveToolStripMenuItem });
            toolStripDropDownButton2.Image = (System.Drawing.Image)resources.GetObject("toolStripDropDownButton2.Image");
            toolStripDropDownButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripDropDownButton2.Name = "toolStripDropDownButton2";
            toolStripDropDownButton2.Size = new System.Drawing.Size(38, 22);
            toolStripDropDownButton2.Text = "csp";
            // 
            // randomSolveToolStripMenuItem
            // 
            randomSolveToolStripMenuItem.Name = "randomSolveToolStripMenuItem";
            randomSolveToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            randomSolveToolStripMenuItem.Text = "random solve";
            randomSolveToolStripMenuItem.Click += randomSolveToolStripMenuItem_Click;
            // 
            // toolStripDropDownButton3
            // 
            toolStripDropDownButton3.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            toolStripDropDownButton3.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { linearSizeToolStripMenuItem });
            toolStripDropDownButton3.Image = (System.Drawing.Image)resources.GetObject("toolStripDropDownButton3.Image");
            toolStripDropDownButton3.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripDropDownButton3.Name = "toolStripDropDownButton3";
            toolStripDropDownButton3.Size = new System.Drawing.Size(78, 22);
            toolStripDropDownButton3.Text = "constraints";
            // 
            // linearSizeToolStripMenuItem
            // 
            linearSizeToolStripMenuItem.Name = "linearSizeToolStripMenuItem";
            linearSizeToolStripMenuItem.Size = new System.Drawing.Size(125, 22);
            linearSizeToolStripMenuItem.Text = "linear size";
            linearSizeToolStripMenuItem.Click += linearSizeToolStripMenuItem_Click;
            // 
            // panel1
            // 
            panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            panel1.Location = new System.Drawing.Point(0, 25);
            panel1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(988, 547);
            panel1.TabIndex = 1;
            // 
            // Form1
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(988, 572);
            Controls.Add(panel1);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Name = "Form1";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "DxfPad";
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem erctangleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem circleToolStripMenuItem;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton2;
        private System.Windows.Forms.ToolStripMenuItem randomSolveToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton3;
        private System.Windows.Forms.ToolStripMenuItem linearSizeToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton toolStripButton2;
        private System.Windows.Forms.ToolStripMenuItem polylineToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem closeLineToolStripMenuItem;
    }
}

