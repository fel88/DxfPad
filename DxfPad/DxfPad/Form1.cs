using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DxfPad
{
    public partial class Form1 : Form
    {
        public Form1()
        {            
            InitializeComponent();
            Form = this;
        }
        public static Form1 Form;
        
        internal void SetStatus(string v)
        {
            //toolStripStatusLabel1.Text = v;
        }
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
        public ITool CurrentTool { get => _currentTool; }

    }
    public enum EditModeEnum
    {
        Part, Draft, Assembly
    }
}
