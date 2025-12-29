using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
using OpenTK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace DxfPad
{
    public partial class Form1 : Form, IEditor
    {
        public Form1()
        {            
            InitializeComponent();
            Form = this;
            de = new DraftEditorControl();
            de.UndosChanged += De_UndosChanged;
            de.Init(this);
            panel1.Controls.Add(de);

            Load += Form1_Load;

            _currentTool = new SelectionTool(this);

            de.Visible = true;

            de.SetDraft(new Draft()); 
            de.FitAll();

            de.Dock = DockStyle.Fill;

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            /*menu = new RibbonMenu();
            Controls.Add(menu);
            menu.AutoSize = true;
            menu.Dock = DockStyle.Top;

            toolStripContainer1.TopToolStripPanel.Visible = false;*/

            mf = new MessageFilter();
            Application.AddMessageFilter(mf);

            //tableLayoutPanel1.ColumnStyles[2].Width = 0;
        }
        public void RectangleStart()
        {
            SetTool(new RectDraftTool(de));
           // uncheckedAllToolButtons();
            //toolStripButton3.Checked = true;
        }

        public static Form1 Form;
        private void De_UndosChanged()
        {
            //toolStripButton16.Enabled = de.CanUndo;
        }
        internal void SetStatus(string v)
        {
            //toolStripStatusLabel1.Text = v;
            
        }

        public DraftEditorControl de;
        MessageFilter mf = null;
        public event Action<ITool> ToolChanged;

        public EditModeEnum EditMode;
        ITool _currentTool;
        public void SetTool(ITool tool)
        {
            _currentTool.Deselect();
            _currentTool = tool;
            _currentTool.Select();
            ToolChanged?.Invoke(_currentTool);
        }
        public void CircleStart()
        {
            SetTool(new DraftEllipseTool(de));
            //uncheckedAllToolButtons();
          //  toolStripButton4.Checked = true;
        }

        public void ObjectSelect(object nearest)
        {
            //throw new NotImplementedException();
        }

        public void ResetTool()
        {
            SetTool(new SelectionTool(this));
        }

        public void Backup()
        {
            //throw new NotImplementedException();
        }
        public List<string> Undos = new List<string>();

        public void Undo()
        {
            if (EditMode == EditModeEnum.Draft)
            {
                de.Undo();
                return;
            }

            if (Undos.Count == 0) return;
            var el = XElement.Parse(Undos.Last());
            //Scene.Restore(el);
            Undos.RemoveAt(Undos.Count - 1);
            //UndosChanged?.Invoke();
        }

        public ITool CurrentTool { get => _currentTool; }

        public IDrawable[] Parts => throw new NotImplementedException();

        public IntersectInfo Pick => throw new NotImplementedException();

        private void erctangleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RectangleStart();
        }
        void exportDxf(Draft draft)
        {
            //export to dxf
            IxMilia.Dxf.DxfFile file = new IxMilia.Dxf.DxfFile();
            foreach (var item in draft.DraftLines)
            {
                if (item.Dummy)
                    continue;

                file.Entities.Add(new DxfLine(new DxfPoint(item.V0.X, item.V0.Y, 0), new DxfPoint(item.V1.X, item.V1.Y, 0)));
            }
            foreach (var item in draft.DraftEllipses)
            {
                if (item.Dummy)
                    continue;
                if (!item.SpecificAngles)
                {
                    //file.Entities.Add(new DxfEllipse(new DxfPoint(item.Center.X, item.Center.Y, 0), new DxfVector((double)item.Radius, 0, 0), 360));
                    file.Entities.Add(new DxfCircle(new DxfPoint(item.Center.X, item.Center.Y, 0), (double)item.Radius));
                    //file.Entities.Add(new DxfArc(new DxfPoint(item.Center.X, item.Center.Y, 0), (double)item.Radius, 0, 360));
                }
                else
                {
                    var pp = item.GetPoints();

                    //file.Entities.Add(new DxfPolyline(pp.Select(zz => new DxfVertex(new DxfPoint(zz.X, zz.Y, 0)))));
                    for (int i = 1; i <= pp.Length; i++)
                    {
                        var p0 = pp[i - 1];
                        var p1 = pp[i % pp.Length];
                        //polyline?

                        file.Entities.Add(new DxfLine(new DxfPoint(p0.X, p0.Y, 0), new DxfPoint(p1.X, p1.Y, 0)));
                    }
                }
            }
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "DXF files (*.dxf)|*.dxf";

            if (sfd.ShowDialog() != DialogResult.OK) return;

            var ed = AutoDialog.DialogHelpers.StartDialog();
            ed.AddBoolField("mm", "mm units");
            ed.ShowDialog();
            var mmUnit = ed.GetBoolField("mm");                        

            if (mmUnit)
            {
                file.Header.DefaultDrawingUnits = DxfUnits.Millimeters;
                file.Header.Version = DxfAcadVersion.R2013; // default version does not support units
                file.Header.DrawingUnits = DxfDrawingUnits.Metric;

                file.Header.UnitFormat = DxfUnitFormat.Decimal;
                file.Header.UnitPrecision = 3;
                file.Header.DimensionUnitFormat = DxfUnitFormat.Decimal;
                file.Header.DimensionUnitToleranceDecimalPlaces = 3;
                file.Header.AlternateDimensioningScaleFactor = 0.0394;
            }

            file.Save(sfd.FileName);
            SetStatus($"{sfd.FileName} saved.");
        }

        /*public void ExportDraftToDxf()
        {
            exportDxf(editedDraft);
        }*/
        private void circleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CircleStart();
        }

        private void randomSolveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            de.Draft.RandomSolve();
        }

        private void linearSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetTool(new LinearConstraintTool(de));
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            exportDxf(de.Draft);
        }
    }
    public enum EditModeEnum
    {
        Part, Draft, Assembly
    }
}
