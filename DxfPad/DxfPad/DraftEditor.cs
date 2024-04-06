using System;
using System.Collections.Generic;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using OpenTK;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using SkiaSharp;
using System.Diagnostics;
using System.Globalization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using System.Xml;
using System.Numerics;
using System.Drawing.Drawing2D;
using ClipperLib;
using Vector2 = OpenTK.Vector2;
using System.Threading;
using TriangleNet.Geometry;
using System.Reflection;

namespace DxfPad
{
    public partial class DraftEditorControl : UserControl, IDraftEditor
    {
        public DraftEditorControl()
        {
            InitializeComponent();
            ctx = Activator.CreateInstance(DrawerType) as IDrawingContext;
            ctx.DragButton = MouseButtons.Right;
            //new SkiaGLDrawingContext() { DragButton = MouseButtons.Right };
            RenderControl = ctx.GenerateRenderControl();
            Controls.Add(RenderControl);
            RenderControl.Dock = DockStyle.Fill;
            ctx.Init(RenderControl);
            ctx.PaintAction = () => { Render(); };

            ctx.InitGraphics();
            ctx.MouseDown += Ctx_MouseDown;
            RenderControl.MouseUp += PictureBox1_MouseUp;
            RenderControl.MouseDown += PictureBox1_MouseDown;
            ctx.Tag = this;
            InitPaints();
        }
        Control RenderControl;

        SKPaint PointPaint;
        public void InitPaints()
        {
            PointPaint = new SKPaint();
            var clr = Pens.Black.Color;
            PointPaint.Color = new SKColor(clr.R, clr.G, clr.B);
            PointPaint.IsAntialias = true;
            PointPaint.StrokeWidth = Pens.Black.Width;
            PointPaint.Style = SKPaintStyle.Stroke;
        }

        void Render()
        {
            var sw = Stopwatch.StartNew();

            ctx.Clear(Color.White); //same thing but also erases anything else on the canvas first

            //   ctx.gr.Clear(Color.White);
            ctx.UpdateDrag();
            subSnapType = SubSnapTypeEnum.None;
            updateNearest();
            ctx.SetPen(Pens.Blue);
            ctx.DrawLineTransformed(new PointF(0, 0), new PointF(0, 100));
            ctx.SetPen(Pens.Red);
            ctx.DrawLineTransformed(new PointF(0, 0), new PointF(100, 0));

            if (_draft != null)
            {
                var dpnts = _draft.DraftPoints.ToArray();
                if (ShowHelpers)
                {
                    foreach (var item in _draft.Helpers.Where(z => z.Z < 0))
                    {
                        if (!item.Visible) continue;

                        item.Draw(ctx);
                    }
                }
                ctx.SetPen(Pens.Black);
                for (int i = 0; i < dpnts.Length; i++)
                {
                    var item = dpnts[i];
                    float gp = 5;
                    var tr = ctx.Transform(item.X, item.Y);

                    if (nearest == item || selected.Contains(item))
                    {
                        ctx.FillRectangle(Brushes.Blue, tr.X - gp, tr.Y - gp, gp * 2, gp * 2);
                    }
                    ctx.DrawRectangle(tr.X - gp, tr.Y - gp, gp * 2, gp * 2);
                }

                var dlns = _draft.DraftLines.ToArray();
                for (int i = 0; i < dlns.Length; i++)
                {   
                    var el = dlns[i];

                    Vector2d item0 = dlns[i].V0.Location;
                    Vector2d item = dlns[i].V1.Location;
                    var tr = ctx.Transform(item0.X, item0.Y);
                    var tr11 = ctx.Transform(item.X, item.Y);
                    Pen p = new Pen(selected.Contains(el) ? Color.Blue : Color.Black);

                    if (el.Dummy)
                        p.DashPattern = new float[] { 10, 10 };

                    //ctx.gr.DrawLine(p, tr, tr11);
                    ctx.SetPen(p);
                    ctx.DrawLine(tr, tr11);
                }

                var elps = _draft.DraftEllipses.Where(z => !z.SpecificAngles).ToArray();
                for (int i = 0; i < elps.Length; i++)
                {
                    var el = elps[i];
                    Vector2d item0 = elps[i].Center.Location;
                    var rad = (float)el.Radius * ctx.zoom;
                    var tr = ctx.Transform(item0.X, item0.Y);

                    Pen p = new Pen(selected.Contains(el) ? Color.Blue : Color.Black);

                    if (el.Dummy)
                        p.DashPattern = new float[] { 10, 10 };
                    if (nearest == el.Center || selected.Contains(el.Center))
                    {
                        p.Width = 2;
                        p.Color = Color.Blue;
                        ctx.DrawCircle(p, tr.X, tr.Y, rad);
                    }
                    else
                        ctx.DrawCircle(p, tr.X, tr.Y, rad);

                    float gp = 5;
                    tr = ctx.Transform(el.Center.X, el.Center.Y);

                    if (nearest == el.Center || selected.Contains(el.Center))
                    {
                        ctx.FillRectangle(Brushes.Blue, tr.X - gp, tr.Y - gp, gp * 2, gp * 2);
                    }
                    ctx.SetPen(p);
                    ctx.DrawRectangle(tr.X - gp, tr.Y - gp, gp * 2, gp * 2);
                }

                var hexes = _draft.DraftEllipses.Where(z => z.SpecificAngles).ToArray();
                for (int i = 0; i < hexes.Length; i++)
                {
                    var el = hexes[i];
                    Vector2d item0 = hexes[i].Center.Location;
                    var rad = (float)el.Radius * ctx.zoom;
                    var tr = ctx.Transform(item0.X, item0.Y);

                    Pen p = new Pen(selected.Contains(el) ? Color.Blue : Color.Black);

                    if (el.Dummy)
                        p.DashPattern = new float[] { 10, 10 };
                    if (nearest == el.Center || selected.Contains(el.Center))
                    {
                        p.Width = 2;
                        p.Color = Color.Blue;
                    }

                    ctx.DrawCircle(p, tr.X, tr.Y, rad, el.Angles, 0);

                    float gp = 5;
                    tr = ctx.Transform(el.Center.X, el.Center.Y);

                    if (nearest == el.Center || selected.Contains(el.Center))
                    {
                        ctx.FillRectangle(Brushes.Blue, tr.X - gp, tr.Y - gp, gp * 2, gp * 2);
                    }
                    ctx.SetPen(p);
                    ctx.DrawRectangle(tr.X - gp, tr.Y - gp, gp * 2, gp * 2);
                }

                if (ShowHelpers)
                {
                    foreach (var item in _draft.Helpers.Where(z => z.Z >= 0))
                    {
                        if (!item.Visible) continue;

                        item.Draw(ctx);
                    }
                }
            }
            if (ctx.MiddleDrag)//measure tool
            {
                Pen pen = new Pen(Color.Blue, 2);
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                pen.DashPattern = new float[] { 4.0F, 2.0F, 1.0F, 3.0F };
                ctx.SetPen(pen);
                Pen pen2 = new Pen(Color.White, 2);

                var gcur = ctx.GetCursor();
                var curp = ctx.Transform(gcur);
                double maxSnapDist = 10 / ctx.zoom;

                //check perpendicular of lines?
                foreach (var item in Draft.DraftLines)
                {
                    //get projpoint                     
                    var proj = GeometryUtils.GetProjPoint(gcur.ToVector2d(), item.V0.Location, item.Dir);
                    var sx = ctx.BackTransform(ctx.startx, ctx.starty);
                    var proj2 = GeometryUtils.GetProjPoint(new Vector2d(sx.X, sx.Y), item.V0.Location, item.Dir);
                    if (!item.ContainsPoint(proj))
                        continue;

                    var len = (proj - gcur.ToVector2d()).Length;
                    var len2 = (proj2 - gcur.ToVector2d()).Length;
                    if (len < maxSnapDist)
                    {
                        //sub nearest = projpoint
                        curp = ctx.Transform(proj);
                        gcur = proj.ToPointF();
                        subSnapType = SubSnapTypeEnum.PointOnLine;
                    }
                    if (len2 < maxSnapDist)
                    {
                        //sub nearest = projpoint
                        curp = ctx.Transform(proj2);
                        gcur = proj2.ToPointF();
                        subSnapType = SubSnapTypeEnum.Perpendicular;
                    }
                }

                if (nearest is DraftPoint dp)
                {
                    curp = ctx.Transform(dp.Location);
                    gcur = dp.Location.ToPointF();
                }
                if (nearest is DraftLine dl)
                {
                    var len = (dl.Center - gcur.ToVector2d()).Length;
                    if (len < maxSnapDist)
                    {
                        curp = ctx.Transform(dl.Center);
                        gcur = dl.Center.ToPointF();
                        subSnapType = SubSnapTypeEnum.CenterLine;
                    }
                }
                if (nearest is DraftEllipse de)
                {
                    var diff = (de.Center.Location - new Vector2d(gcur.X, gcur.Y)).Normalized();
                    var onEl = de.Center.Location - diff * (double)de.Radius;
                    //get point on ellipse
                    curp = ctx.Transform(onEl);
                    gcur = onEl.ToPointF();
                }
                var t = ctx.Transform(new PointF(ctx.startx, ctx.starty));

                //snap starto
                if (startMiddleDragNearest is DraftPoint sdp)
                {

                }
                ctx.SetPen(pen2);

                ctx.DrawLine(ctx.startx, ctx.starty, curp.X, curp.Y);
                ctx.SetPen(pen);

                ctx.DrawLine(ctx.startx, ctx.starty, curp.X, curp.Y);

                //ctx.gr.DrawLine(pen, ctx.startx, ctx.starty, curp.X, curp.Y);
                var pp = ctx.BackTransform(new PointF(ctx.startx, ctx.starty));
                Vector2 v1 = new Vector2(pp.X, pp.Y);
                Vector2 v2 = new Vector2(gcur.X, gcur.Y);
                var dist = (v2 - v1).Length;
                var hintText = dist.ToString("N2");
                if (subSnapType == SubSnapTypeEnum.PointOnLine)
                {
                    hintText = "[point on line] : " + hintText;
                }
                if (subSnapType == SubSnapTypeEnum.CenterLine)
                {
                    hintText = "[line center] : " + hintText;
                }
                if (subSnapType == SubSnapTypeEnum.Perpendicular)
                {
                    hintText = "[perpendicular] : " + hintText;
                }
                var mss = ctx.MeasureString(hintText, SystemFonts.DefaultFont);

                ctx.FillRectangle(Brushes.White, curp.X + 10, curp.Y, mss.Width, mss.Height);
                ctx.DrawString(hintText, SystemFonts.DefaultFont, Brushes.Black, curp.X + 10, curp.Y);


            }
            if (ctx.isLeftDrag)//rect tool
            {
                Pen pen = new Pen(Color.Blue, 2);
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

                pen.DashPattern = new float[] { 4.0F, 2.0F, 1.0F, 3.0F };
                var gcur = ctx.GetCursor();

                var curp = ctx.Transform(gcur);

                var t = ctx.Transform(new PointF(ctx.startx, ctx.starty));
                var rxm = Math.Min(ctx.startx, curp.X);
                var rym = Math.Min(ctx.starty, curp.Y);
                var rdx = Math.Abs(ctx.startx - curp.X);
                var rdy = Math.Abs(ctx.starty - curp.Y);
                ctx.SetPen(pen);
                ctx.DrawRectangle(rxm, rym, rdx, rdy);
                var pp = ctx.BackTransform(new PointF(ctx.startx, ctx.starty));
                Vector2 v1 = new Vector2(pp.X, pp.Y);
                Vector2 v2 = new Vector2(gcur.X, gcur.Y);
                var dist = (v2 - v1).Length;
                ctx.DrawString(dist.ToString("N2"), SystemFonts.DefaultFont, Brushes.Black, curp.X + 10, curp.Y);


            }
            editor.CurrentTool.Draw();

            sw.Stop();
            var ms = sw.ElapsedMilliseconds;
            LastRenderTime = ms;

            ctx.DrawString("current tool: " + Form1.Form.CurrentTool.GetType().Name, SystemFonts.DefaultFont, Brushes.Black, 5, 5);

        }

        internal void AddImage()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;
            var bmp = Bitmap.FromFile(ofd.FileName) as Bitmap;
            //Draft.Helpers.Add(new ImageDraftHelper(Draft, bmp));
        }

        internal void ArrayUI()
        {
            //var points = selected.OfType<DraftPoint>().ToArray();
            //if (points.Length == 0) return;

            //ArrayDialog ad = new ArrayDialog();
            //if (ad.ShowDialog() != DialogResult.OK) return;

            //Backup();
            //var maxx = points.Max(z => z.X);
            //var maxy = points.Max(z => z.Y);
            //var minx = points.Min(z => z.X);
            //var miny = points.Min(z => z.Y);
            //var width = maxx - minx;
            //var height = maxy - miny;

            //var lines = _draft.DraftLines.Where(z => points.Contains(z.V0) && points.Contains(z.V1)).ToArray();
            //var circles = _draft.DraftEllipses.Where(z => points.Contains(z.Center)).ToArray();

            //for (int i = 0; i < ad.QtyX; i++)
            //{
            //    for (int j = 0; j < ad.QtyY; j++)
            //    {
            //        if (i == 0 && j == 0) continue;
            //        var shx = i * (ad.OffsetX + width);
            //        var shy = j * (ad.OffsetY + height);
            //        List<DraftPoint> added = new List<DraftPoint>();
            //        foreach (var item in points)
            //        {
            //            var dp = new DraftPoint(_draft, item.X + shx, item.Y + shy);
            //            _draft.AddElement(dp);
            //            added.Add(dp);
            //        }
            //        foreach (var item in lines)
            //        {
            //            var v0 = added.First(z => (z.Location - (item.V0.Location + new Vector2d(shx, shy))).Length < float.Epsilon);
            //            var v1 = added.First(z => (z.Location - (item.V1.Location + new Vector2d(shx, shy))).Length < float.Epsilon);
            //            _draft.AddElement(new DraftLine(v0, v1, _draft));
            //        }
            //        foreach (var item in circles)
            //        {
            //            var v0 = added.First(z => (z.Location - (item.Center.Location + new Vector2d(shx, shy))).Length < float.Epsilon);
            //            _draft.AddElement(new DraftEllipse(v0, item.Radius, _draft));
            //        }
            //    }
            //}
        }

        public long LastRenderTime;

        public event Action UndosChanged;
        private void Ctx_MouseDown(float arg1, float arg2, MouseButtons e)
        {
            //var pos = ctx.PictureBox.Control.PointToClient(Cursor.Position);

            if (e == MouseButtons.Left)
            {
                if ((Control.ModifierKeys & Keys.Shift) != 0)
                {

                }
                else
                if ((Control.ModifierKeys & Keys.Control) != 0)
                {

                }
                else
                {
                    foreach (var item in selected)
                    {
                        if (item is IDrawable dd)
                        {
                            dd.Selected = false;
                        }
                    }
                    selected = new[] { nearest };
                }
                Form1.Form.SetStatus($"selected: {selected.Count()} objects");
                foreach (var item in selected)
                {
                    if (item is IDrawable dd)
                    {
                        dd.Selected = true;
                    }
                }
            }

            if (e == MouseButtons.Middle)
            {
                ctx.isMiddleDrag = true;
                if (nearest is DraftPoint pp)
                {
                    startMiddleDragNearest = nearest;
                    var tr = ctx.Transform(pp.Location);
                    ctx.startx = (float)tr.X;
                    ctx.starty = (float)tr.Y;
                }
            }
            if (e == MouseButtons.Left && editor.CurrentTool is SelectionTool)
            {
                ctx.isLeftDrag = true;
                if (nearest is DraftPoint pp)
                {
                    var tr = ctx.Transform(pp.Location);
                    ctx.startx = (float)tr.X;
                    ctx.starty = (float)tr.Y;
                }
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            return base.ProcessCmdKey(ref msg, keyData);
        }
        List<DraftElement> queue = new List<DraftElement>();
        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (editor.CurrentTool is SelectionTool && e.Button == MouseButtons.Left)
            {
                //if (nearest is LinearConstraintHelper lh)
                {
                    //   editor.ObjectSelect(lh.constraint);
                }
                //  else
                {
                    editor.ObjectSelect(nearest);
                }
            }
            if (editor.CurrentTool is PerpendicularConstraintTool && e.Button == MouseButtons.Left)
            {
                if (nearest is DraftLine)
                {
                    if (!queue.Contains(nearest))
                        queue.Add(nearest as DraftLine);
                }
                if (queue.Count > 1)
                {
                    var cc = new PerpendicularConstraint(queue[0] as DraftLine, queue[1] as DraftLine, _draft);
                    if (!_draft.Constraints.OfType<PerpendicularConstraint>().Any(z => z.IsSame(cc)))
                    {
                        _draft.AddConstraint(cc);
                        _draft.AddHelper(new PerpendicularConstraintHelper(cc));
                        _draft.Childs.Add(_draft.Helpers.Last());
                    }
                    else
                    {
                        GUIHelpers.Warning("such constraint already exist", ParentForm.Text);
                    }
                    queue.Clear();
                    editor.ResetTool();
                }
            }
            if (editor.CurrentTool is ParallelConstraintTool && e.Button == MouseButtons.Left)
            {
                if (nearest is DraftLine)
                {
                    if (!queue.Contains(nearest))
                        queue.Add(nearest as DraftLine);
                }
                if (queue.Count > 1)
                {
                    var cc = new ParallelConstraint(queue[0] as DraftLine, queue[1] as DraftLine, _draft);

                    if (!_draft.Constraints.OfType<ParallelConstraint>().Any(z => z.IsSame(cc)))
                    {
                        _draft.AddConstraint(cc);
                        _draft.AddHelper(new ParallelConstraintHelper(cc));
                        _draft.Childs.Add(_draft.Helpers.Last());
                    }
                    else
                    {
                        GUIHelpers.Warning("such constraint already exist", ParentForm.Text);
                    }
                    queue.Clear();
                    editor.ResetTool();

                }
            }

            editor.CurrentTool.MouseDown(e);
        }


        private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            //isPressed = false;
            var cp = Cursor.Position;
            var pos = ctx.PictureBox.Control.PointToClient(Cursor.Position);

            if (e.Button == MouseButtons.Right)
            {
                var dx = Math.Abs(ctx.startx - pos.X);
                var dy = Math.Abs(ctx.starty - pos.Y);
                float eps = 1;
                if ((dx + dy) < eps)
                {
                    //contextMenuStrip1.Show(cp);
                }
            }
            if (e.Button == MouseButtons.Left)
            {

                var gcur = ctx.GetCursor();
                var t = ctx.BackTransform(new PointF(ctx.startx, ctx.starty));
                var rxm = Math.Min(t.X, gcur.X);
                var rym = Math.Min(t.Y, gcur.Y);
                var rdx = Math.Abs(t.X - gcur.X);
                var rdy = Math.Abs(t.Y - gcur.Y);
                var rect = new RectangleF(rxm, rym, rdx, rdy);
                if (rect.Width > 1 && rect.Height > 1)
                {
                    var tt = _draft.DraftPoints.Where(z => rect.Contains((float)z.Location.X, (float)z.Location.Y)).ToArray();
                    tt = tt.Union(_draft.DraftEllipses.Select(z => z.Center).Where(z => rect.Contains((float)z.Location.X, (float)z.Location.Y))).ToArray();
                    if ((Control.ModifierKeys & Keys.Shift) != 0)
                    {
                        selected = selected.Except(tt).ToArray();
                    }
                    else
                    if ((Control.ModifierKeys & Keys.Control) != 0)
                    {
                        selected = selected.Union(tt).ToArray();
                    }
                    else
                        selected = tt;
                    Form1.Form.SetStatus($"selected: {tt.Count()} points");
                }
                else
                {
                    if ((Control.ModifierKeys & Keys.Control) != 0)
                    {
                        if (selected.Length == 1)
                        {
                            if (selected[0] is DraftLine dl)
                            {
                                List<DraftLine> contour = new List<DraftLine>();
                                contour.Add(dl);

                                //contour select
                                double eps = 1e-8;
                                var remains = Draft.DraftLines.Except(new[] { dl }).ToList();
                                while (true)
                                {
                                    DraftLine add = null;
                                    foreach (var line in remains)
                                    {
                                        var v1 = new[] { line.V0, line.V1 };
                                        if ((contour[0].V0.Location - v1[0].Location).Length < eps
                                            || (contour[0].V0.Location - v1[1].Location).Length < eps
                                             || (contour[0].V1.Location - v1[0].Location).Length < eps
                                              || (contour[0].V1.Location - v1[1].Location).Length < eps
                                            )
                                        {
                                            add = line;
                                            contour.Insert(0, line);
                                            break;
                                        }
                                    }

                                    if (add == null) break;
                                    remains.Remove(add);
                                }
                                //check closed
                                //select all
                                selected = contour.SelectMany(z => new[] { z.V0, z.V1 }).Distinct().OfType<object>().Union(contour.ToArray()).ToArray();
                            }
                        }
                    }
                }
            }
        }

        public List<string> Undos = new List<string>();
        public void Undo()
        {
            if (Undos.Count == 0) return;
            var el = XElement.Parse(Undos.Last());
            _draft.Restore(el);
            Undos.RemoveAt(Undos.Count - 1);
            SetDraft(_draft);
            UndosChanged?.Invoke();

        }

        public void Backup()
        {
            StringWriter sw = new StringWriter();
            _draft.Store(sw);
            Undos.Add(sw.ToString());
            UndosChanged?.Invoke();
        }

        public void FitAll()
        {
            if (_draft == null || _draft.Elements.Count() == 0) return;

            var t = _draft.DraftPoints.Select(z => z.Location).ToArray();
            var t2 = _draft.DraftEllipses.SelectMany(z => new[] {
                new Vector2d(z.Center.X - (double)z.Radius, z.Center.Y-(double)z.Radius) ,
                new Vector2d(z.Center.X + (double)z.Radius, z.Center.Y+(double)z.Radius) ,

            }).ToArray();
            t = t.Union(t2).ToArray();

            ctx.FitToPoints(t.Select(z => z.ToPointF()).ToArray(), 5);
        }
        public static Type DrawerType = typeof(SkiaGLDrawingContext);
        IDrawingContext ctx;
        public object nearest { get; private set; }
        public object startMiddleDragNearest;
        object[] selected = new object[] { };
        void updateNearest()
        {
            var pos = ctx.GetCursor();
            var _pos = new Vector2d(pos.X, pos.Y);
            double minl = double.MaxValue;
            object minp = null;
            double maxDist = 10 / ctx.zoom;
            foreach (var item in _draft.DraftPoints)
            {
                var d = (item.Location - _pos).Length;
                if (d < minl)
                {
                    minl = d;
                    minp = item;
                }
            }
            foreach (var item in _draft.DraftEllipses)
            {
                var d = (item.Center.Location - _pos).Length;
                if (d < minl)
                {
                    minl = d;
                    minp = item.Center;
                }

                d = Math.Abs((item.Center.Location - _pos).Length - (double)item.Radius);
                if (d < minl)
                {
                    minl = d;
                    minp = item;
                }

            }
            foreach (var item in _draft.DraftLines)
            {
                var loc = (item.V0.Location + item.V1.Location) / 2;
                var d = (loc - _pos).Length;
                if (d < minl)
                {
                    minl = d;
                    minp = item;
                }
            }
            foreach (var item in _draft.ConstraintHelpers)
            {
                var d = (item.SnapPoint - _pos).Length;
                if (d < minl)
                {
                    minl = d;
                    minp = item;
                }
            }
            if (minl < maxDist)
            {
                nearest = minp;
            }
            else
                nearest = null;
        }

        public enum SubSnapTypeEnum
        {
            None, Point, PointOnLine, CenterLine, Perpendicular, PointOnCircle
        }

        SubSnapTypeEnum subSnapType;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!Visible) return;
            RenderControl.Refresh();
            return;

        }

        public void ResetTool()
        {
            editor.ResetTool();
        }
        IEditor editor;

        Draft _draft;
        public Draft Draft => _draft;

        public IDrawingContext DrawingContext => ctx;

        public bool CanUndo => Undos.Any();

        public bool ShowHelpers { get; set; } = true;

        public void SetDraft(Draft draft)
        {
            _draft = draft;
            _draft.BeforeConstraintChanged = (c) =>
            {
                Backup();
            };

            //restore helpers
            foreach (var citem in draft.Constraints)
            {
                if (draft.ConstraintHelpers.Any(z => z.Constraint == citem)) continue;
                if (citem is LinearConstraint lc)
                {
                    draft.AddHelper(new LinearConstraintHelper(_draft, lc));
                }
                if (citem is VerticalConstraint vc)
                {
                    _draft.AddHelper(new VerticalConstraintHelper(vc));
                }
                if (citem is HorizontalConstraint hc)
                {
                    _draft.AddHelper(new HorizontalConstraintHelper(hc));
                }
                if (citem is EqualsConstraint ec)
                {
                    _draft.AddHelper(new EqualsConstraintHelper(draft, ec));
                }
            }
        }
        internal void Init(IEditor editor)
        {
            this.editor = editor;
            editor.ToolChanged += Editor_ToolChanged;
        }

        private void Editor_ToolChanged(ITool obj)
        {
            //lastDraftPoint = null;
        }

        internal void Finish()
        {
            _draft.EndEdit();
        }

        internal void Clear()
        {
            Backup();
            _draft.Clear();
        }

        internal void CloseLine()
        {
            if (_draft.DraftPoints.Any())
                _draft.Elements.Add(new DraftLine(_draft.DraftPoints.First(), _draft.DraftPoints.Last(), _draft));
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selected == null || selected.Length == 0) return;
            Backup();
            foreach (var item in selected)
            {
                if (item is DraftElement de)
                    _draft.RemoveElement(de);

                if (item is IDrawable dd)
                {
                    _draft.RemoveChild(dd);
                }
            }
        }

        private void detectCOMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var points = selected.OfType<DraftPoint>().ToArray();
            if (points.Length == 0) return;
            var sx = points.Sum(z => z.X) / points.Length;
            var sy = points.Sum(z => z.Y) / points.Length;

            _draft.AddElement(new DraftPoint(_draft, sx, sy));
        }

        private void approxByCircleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var points = selected.OfType<DraftPoint>().ToArray();
            if (points.Length == 0) return;
            var sx = points.Sum(z => z.X) / points.Length;
            var sy = points.Sum(z => z.Y) / points.Length;
            var dp = new DraftPoint(_draft, sx, sy);
            var rad = (decimal)(points.Select(z => (z.Location - dp.Location).Length).Average());

            _draft.AddElement(new DraftEllipse(dp, rad, _draft));
            if (GUIHelpers.ShowQuestion("Delete source points?", ParentForm.Text) == DialogResult.Yes)
            {
                for (int p = 0; p < points.Length; p++)
                {
                    _draft.RemoveElement(points[p]);
                }
            }

        }

        internal void FlipHorizontal()
        {
            var points = selected.OfType<DraftPoint>().ToArray();
            if (points.Length == 0) return;
            var maxx = points.Max(z => z.X);
            var minx = points.Min(z => z.X);
            var mx = (maxx + minx) / 2;
            for (int i = 0; i < points.Length; i++)
            {
                points[i].SetLocation(2 * mx - points[i].Location.X, points[i].Y);
            }
        }

        internal void FlipVertical()
        {
            var points = selected.OfType<DraftPoint>().ToArray();
            if (points.Length == 0) return;
            var maxy = points.Max(z => z.Y);
            var miny = points.Min(z => z.Y);
            var my = (maxy + miny) / 2;
            for (int i = 0; i < points.Length; i++)
            {
                points[i].SetLocation(points[i].X, 2 * my - points[i].Location.Y);
            }
        }

        public void TranslateUI()
        {
            /*var points = selected.OfType<DraftPoint>().ToArray();
            if (points.Length == 0) return;

            var ret = GUIHelpers.EditorStart(Vector3d.Zero, "Translate", typeof(Vector2dPropEditor));
            var r = (Vector2d)ret;

            Backup();
            foreach (var item in points)
            {
                item.SetLocation(item.Location + r);
            }*/
        }
        private void translateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TranslateUI();
        }

        public void Swap<T>(List<T> ar, int i, int j)
        {
            var temp = ar[i];
            ar[i] = ar[j];
            ar[j] = temp;
        }
        public DraftPoint[] ExtractStripContour(DraftLine[] lines)
        {
            List<DraftPoint> ret = new List<DraftPoint>();
            ret = lines.SelectMany(z => new[] { z.V0, z.V1 }).ToList();
            //reverse pairs inplace
            for (int i = 0; i < ret.Count - 2; i += 2)
            {
                var cur1 = ret[i];
                var cur2 = ret[i + 1];
                var next1 = ret[i + 2];
                var next2 = ret[i + 3];
                //find connection point
                var dist0 = (cur2.Location - next1.Location).Length;
                var dist1 = (cur1.Location - next2.Location).Length;
                var dist2 = (cur2.Location - next2.Location).Length;
                var dist3 = (cur1.Location - next1.Location).Length;
                var minIndex = new[] { dist0, dist1, dist2, dist3 }.Select((z, ii) => new Tuple<double, int>(z, ii)).OrderBy(z => z.Item1).First().Item2;
                bool reverse1 = false;
                bool reverse2 = false;
                switch (minIndex)
                {
                    case 0:

                        break;
                    case 1:
                        //reverse both
                        reverse1 = true;
                        reverse2 = true;
                        break;
                    case 2:

                        reverse2 = true;
                        break;
                    case 3:
                        reverse1 = true;
                        break;
                }
                if (reverse1)
                {
                    Swap(ret, i, i + 1);
                }
                if (reverse2)
                {
                    Swap(ret, i + 2, i + 3);
                }
            }

            /*for (int i = 1; i < ret.Count - 2; i += 2)
            {
                var cur = ret[i];
                var forw1 = ret[i + 1];
                var forw2 = ret[i + 2];
                var dist0 = (cur.Location - forw1.Location).Length;
                var dist1 = (cur.Location - forw2.Location).Length;
                if (dist0 < dist1)
                {

                }
                else
                {
                    //swap
                    var temp = ret[i + 1];
                    ret[i + 1] = ret[i + 2];
                    ret[i + 2] = temp;
                }
            }*/
            return ret.Distinct().ToArray();
        }

        public void OffsetUI()
        {
            //OffsetDialog od = new OffsetDialog();
            //if (od.ShowDialog() != DialogResult.OK) return;

            //Backup();

            //NFP p = new NFP();
            //NFP ph2 = new NFP();
            ////restore contours
            //var lines = selected.OfType<DraftLine>().ToArray();

            ////single contour support only yet

            //var l = Draft.DraftLines.Where(z => selected.Contains(z.V0) && selected.Contains(z.V1)).OfType<DraftElement>().ToArray();
            //var l2 = Draft.DraftEllipses.Where(z => selected.Contains(z.Center)).ToArray();

            ////restore contours
            ///*    l = l.Union(Draft.DraftEllipses.Where(z => selected.Contains(z.Center)).ToArray()).ToArray();
            //    foreach (var item in l)
            //    {
            //        item.Dummy = true;
            //    }*/

            ////p.Points = ph2.Polygon.Points.Select(z => new Vector2d(z.X, z.Y)).ToArray();
            //var strip = ExtractStripContour(lines);
            //p.Points = strip.Select(z => z.Location).ToArray();
            //var jType = od.JoinType;
            //double offset = od.Offset;
            //double miterLimit = 4;
            //double curveTolerance = od.CurveTolerance;
            //var offs = ClipperHelper.offset(p, offset, jType, curveTolerance: curveTolerance, miterLimit: miterLimit);
            ////if (offs.Count() > 1) throw new NotImplementedException();
            //NFP ph = new NFP();
            //foreach (var item in ph2.Childrens)
            //{
            //    var offs2 = ClipperHelper.offset(item, -offset, jType, curveTolerance: curveTolerance, miterLimit: miterLimit);
            //    var nfp1 = new NFP();
            //    if (offs2.Any())
            //    {
            //        //if (offs2.Count() > 1) throw new NotImplementedException();
            //        foreach (var zitem in offs2)
            //        {
            //            nfp1.Points = zitem.Points.Select(z => new Vector2d(z.X, z.Y)).ToArray();
            //            ph.Childrens.Add(nfp1);
            //        }
            //    }
            //}

            //if (offs.Any())
            //{
            //    ph.Points = offs.First().Points.Select(z => new Vector2d(z.X, z.Y)).ToArray();
            //}

            //foreach (var item in offs.Skip(1))
            //{
            //    var nfp2 = new NFP();

            //    nfp2.Points = item.Points.Select(z => new Vector2d(z.X, z.Y)).ToArray();
            //    ph.Childrens.Add(nfp2);

            //}

            //List<DraftPoint> newp = new List<DraftPoint>();
            //for (int i = 0; i < ph.Points.Length; i++)
            //{
            //    newp.Add(new DraftPoint(Draft, ph.Points[i].X, ph.Points[i].Y));

            //}
            //Draft.Elements.AddRange(newp);
            //for (int i = 1; i <= ph.Points.Length; i++)
            //{
            //    Draft.AddElement(new DraftLine(newp[i - 1], newp[i % ph.Points.Length], Draft));
            //}

            ///*ph.OffsetX = ph2.OffsetX;
            //ph.OffsetY = ph2.OffsetY;
            //ph.Rotation = ph2.Rotation;
            //dataModel.AddItem(ph);*/
        }

        private void offsetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OffsetUI();
        }

        private void dummyAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var l = Draft.DraftLines.Where(z => selected.Contains(z.V0) && selected.Contains(z.V1)).OfType<DraftElement>().ToArray();
            l = l.Union(Draft.DraftEllipses.Where(z => selected.Contains(z.Center)).ToArray()).ToArray();
            if (l.Any()) Backup();
            foreach (var item in l)
            {
                item.Dummy = true;
            }
        }

        private void undummyAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var l = Draft.DraftLines.Where(z => selected.Contains(z.V0) && selected.Contains(z.V1)).OfType<DraftElement>().ToArray();
            l = l.Union(Draft.DraftEllipses.Where(z => selected.Contains(z.Center)).ToArray()).ToArray();
            if (l.Any()) Backup();
            foreach (var item in l)
            {
                item.Dummy = false;
            }
        }

        private void mergePointsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var points = selected.OfType<DraftPoint>().ToArray();
            if (points.Length == 0) return;


            var sx = points.Sum(z => z.X) / points.Length;
            var sy = points.Sum(z => z.Y) / points.Length;
            Backup();
            _draft.AddElement(new DraftPoint(_draft, sx, sy));

            var l = Draft.DraftLines.Where(z => selected.Contains(z.V0) && selected.Contains(z.V1)).OfType<DraftElement>().ToArray();


            for (int i = 0; i < points.Length; i++)
            {
                _draft.RemoveElement(points[i]);
            }
            //todo: add lines
        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            if (!Visible) return;
            RenderControl.Refresh();
            return;
        }
    }
    public static class Helpers
    {
        public static double ParseDouble(string v)
        {
            return double.Parse(v.Replace(",", "."), CultureInfo.InvariantCulture);
        }
        public static decimal ParseDecimal(string v)
        {
            return decimal.Parse(v.Replace(",", "."), CultureInfo.InvariantCulture);
        }

        
        public static PointF ToPointF(this Vector2d v)
        {
            return new PointF((float)v.X, (float)v.Y);
        }
        public static PointF Offset(this PointF v, float x, float y)
        {
            return new PointF((float)v.X + x, (float)v.Y + y);
        }
        public static Vector2d ToVector2d(this PointF v)
        {
            return new Vector2d(v.X, v.Y);
        }

        public static Vector3d ParseVector(string value)
        {
            Vector3d ret = new Vector3d();
            var spl = value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(ParseDouble).ToArray();
            for (int i = 0; i < 3; i++)
            {
                ret[i] = spl[i];
            }
            return ret;
        }
        public static Vector2d ParseVector2(string value)
        {
            Vector2d ret = new Vector2d();
            var spl = value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(ParseDouble).ToArray();
            for (int i = 0; i < 2; i++)
            {
                ret[i] = spl[i];
            }
            return ret;
        }
    }
    public interface IDraftConstraintHelper : IDraftHelper
    {
        DraftConstraint Constraint { get; }
        Vector2d SnapPoint { get; set; }


    }
    public class DraftLine : DraftElement
    {
        public readonly DraftPoint V0;
        public readonly DraftPoint V1;

        public DraftLine(XElement el, Draft parent) : base(el, parent)
        {
            var v0Id = int.Parse(el.Attribute("v0").Value);
            var v1Id = int.Parse(el.Attribute("v1").Value);
            Dummy = bool.Parse(el.Attribute("dummy").Value);
            V0 = parent.DraftPoints.First(z => z.Id == v0Id);
            V1 = parent.DraftPoints.First(z => z.Id == v1Id);
        }

        public DraftLine(DraftPoint v0, DraftPoint v1, Draft parent) : base(parent)
        {
            this.V0 = v0;
            this.V1 = v1;
        }

        public Vector2d Center => (V0.Location + V1.Location) / 2;
        public Vector2d Dir => (V1.Location - V0.Location).Normalized();
        public Vector2d Normal => new Vector2d(-Dir.Y, Dir.X);
        public double Length => (V1.Location - V0.Location).Length;
        public override void Store(TextWriter writer)
        {
            writer.WriteLine($"<line id=\"{Id}\" dummy=\"{Dummy}\" v0=\"{V0.Id}\" v1=\"{V1.Id}\" />");
        }

        public bool ContainsPoint(Vector2d proj)
        {
            return Math.Abs(((V0.Location - proj).Length + (V1.Location - proj).Length) - Length) < 1e-8f;
        }
    }
    public static class FactoryHelper
    {

        public static int NewId;
    }
    public class ConstraintSolverContext
    {
        public ConstraintSolverContext Parent;
        public List<ConstraintSolverContext> Childs = new List<ConstraintSolverContext>();
        public List<DraftPoint> FreezedPoints = new List<DraftPoint>();
        public List<TopologyDraftLineInfo> FreezedLinesDirs = new List<TopologyDraftLineInfo>();
    }
    
    public class DraftEllipse : DraftElement
    {
        public readonly DraftPoint Center;
        public double X { get => Center.Location.X; set => Center.SetLocation(new OpenTK.Vector2d(value, Center.Y)); }
        public double Y { get => Center.Location.Y; set => Center.SetLocation(new OpenTK.Vector2d(Center.X, value)); }
        decimal _radius { get; set; }
        public decimal Radius { get => _radius; set => _radius = value; }
        public decimal Diameter { get => 2 * _radius; set => _radius = value / 2; }
        public bool SpecificAngles { get; set; }
        public int Angles { get; set; }
        public DraftEllipse(DraftPoint center, decimal radius, Draft parent)
            : base(parent)
        {
            this.Center = center;
            this.Radius = radius;
        }
        public DraftEllipse(XElement elem, Draft parent)
          : base(elem, parent)
        {
            var c = Helpers.ParseVector2(elem.Attribute("center").Value);
            Center = new DraftPoint(parent, c.X, c.Y);
            Radius = Helpers.ParseDecimal(elem.Attribute("radius").Value);
            if (elem.Attribute("angles") != null)
                Angles = int.Parse(elem.Attribute("angles").Value);
            if (elem.Attribute("specificAngles") != null)
                SpecificAngles = bool.Parse(elem.Attribute("specificAngles").Value);
        }

        internal decimal CutLength()
        {
            return (2 * (decimal)Math.PI * Radius);
        }

        public override void Store(TextWriter writer)
        {
            writer.WriteLine($"<ellipse id=\"{Id}\" angles=\"{Angles}\" specificAngles=\"{SpecificAngles}\" center=\"{Center.X}; {Center.Y}\" radius=\"{Radius}\">");
            writer.WriteLine("</ellipse>");
        }

        public Vector2d[] GetPoints()
        {
            var step = 360f / Angles;
            List<Vector2d> pp = new List<Vector2d>();
            for (int i = 0; i < Angles; i++)
            {
                var ang = step * i;
                var radd = ang * Math.PI / 180f;
                var xx = Center.X + (double)Radius * Math.Cos(radd);
                var yy = Center.Y + (double)Radius * Math.Sin(radd);
                pp.Add(new Vector2d(xx, yy));
            }
            return pp.ToArray();
        }
    }
    public class TopologyDraftLineInfo
    {
        public DraftLine Line;
        public Vector2d Dir;
    }
    public class Line3D
    {
        public Vector3d Start;
        public Vector3d End;
        public Vector3d Dir
        {
            get
            {
                return (End - Start).Normalized();
            }
        }

        public bool IsPointOnLine(Vector3d pnt, float epsilon = 10e-6f)
        {
            float tolerance = 10e-6f;
            var d1 = pnt - Start;
            if (d1.Length < tolerance) return true;
            if ((End - Start).Length < tolerance) throw new Exception("degenerated 3d line");
            var crs = Vector3d.Cross(d1.Normalized(), (End - Start).Normalized());
            return Math.Abs(crs.Length) < epsilon;
        }
        public bool IsPointInsideSegment(Vector3d pnt, float epsilon = 10e-6f)
        {
            if (!IsPointOnLine(pnt, epsilon)) return false;
            var v0 = (End - Start).Normalized();
            var v1 = pnt - Start;
            var crs = Vector3d.Dot(v0, v1) / (End - Start).Length;
            return !(crs < 0 || crs > 1);
        }
        public bool IsSameLine(Line3D l)
        {
            return IsPointOnLine(l.Start) && IsPointOnLine(l.End);
        }

        public void Shift(Vector3d vector3)
        {
            Start += vector3;
            End += vector3;
        }
    }
    public interface ITool
    {

        void Update();
        void MouseDown(MouseEventArgs e);
        void MouseUp(MouseEventArgs e);
        void Draw();
        void Select();
        void Deselect();
    }
    public interface IDrawable
    {
        int Id { get; set; }
        IDrawable Parent { get; set; }
        List<IDrawable> Childs { get; }
        string Name { get; set; }
        bool Visible { get; set; }
        bool Frozen { get; set; }
        void Draw();
        bool Selected { get; set; }
        TransformationChain Matrix { get; }

        IDrawable[] GetAll(Predicate<IDrawable> p);
        void RemoveChild(IDrawable dd);
        void Store(TextWriter writer);
        int Z { get; set; }
    }
    public class PlaneHelper : AbstractDrawable, IEditFieldsContainer, ICommandsContainer
    {
        public PlaneHelper()
        {

        }

        public PlaneHelper(XElement elem)
        {
            if (elem.Attribute("name") != null)
            {
                Name = elem.Attribute("name").Value;
            }
            if (elem.Attribute("size") != null)
            {
                DrawSize = int.Parse(elem.Attribute("size").Value);
            }
            var pos = elem.Attribute("pos").Value.Split(';').Select(z => double.Parse(z.Replace(",", "."), CultureInfo.InvariantCulture)).ToArray();
            Position = new Vector3d(pos[0], pos[1], pos[2]);
            var normal = elem.Attribute("normal").Value.Split(';').Select(z => double.Parse(z.Replace(",", "."), CultureInfo.InvariantCulture)).ToArray();
            Normal = new Vector3d(normal[0], normal[1], normal[2]);
        }

        public Plane GetPlane()
        {
            return new Plane() { Normal = Normal, Position = Position };
        }

        [EditField]
        public Vector3d Position { get; set; }

        [EditField]
        public Vector3d Normal { get; set; }

        [EditField]
        public int DrawSize { get; set; } = 10;

        public override void Store(TextWriter writer)
        {
            writer.WriteLine($"<plane name=\"{Name}\" size=\"{DrawSize}\" pos=\"{Position.X};{Position.Y};{Position.Z}\" normal=\"{Normal.X};{Normal.Y};{Normal.Z}\"/>");
        }

        public Vector3d[] GetBasis()
        {
            Vector3d[] shifts = new[] { Vector3d.UnitX, Vector3d.UnitY, Vector3d.UnitZ };
            Vector3d axis1 = Vector3d.Zero;
            for (int i = 0; i < shifts.Length; i++)
            {
                var proj = ProjPoint(Position + shifts[i]);

                if (Vector3d.Distance(proj, Position) > 10e-6)
                {
                    axis1 = (proj - Position).Normalized();
                    break;
                }
            }
            var axis2 = Vector3d.Cross(Normal.Normalized(), axis1);

            return new[] { axis1, axis2 };
        }
        public Vector2d ProjectPointUV(Vector3d v)
        {
            var basis = GetBasis();
            return GetUVProjPoint(v, basis[0], basis[1]);
        }
        public Vector2d GetUVProjPoint(Vector3d point, Vector3d axis1, Vector3d axis2)
        {
            var p = GetProjPoint(point) - Position;
            var p1 = Vector3d.Dot(p, axis1);
            var p2 = Vector3d.Dot(p, axis2);
            return new Vector2d(p1, p2);
        }
        public Vector3d GetProjPoint(Vector3d point)
        {
            var v = point - Position;
            var nrm = Normal;
            var dist = Vector3d.Dot(v, nrm);
            var proj = point - dist * nrm;
            return proj;
        }
        public Vector3d ProjPoint(Vector3d point)
        {
            var nrm = Normal.Normalized();
            var v = point - Position;
            var dist = Vector3d.Dot(v, nrm);
            var proj = point - dist * nrm;
            return proj;
        }

        public Line3D Intersect(PlaneHelper ps)
        {
            Line3D ret = new Line3D();

            var dir = Vector3d.Cross(ps.Normal, Normal);


            var k1 = ps.GetKoefs();
            var k2 = GetKoefs();
            var a1 = k1[0];
            var b1 = k1[1];
            var c1 = k1[2];
            var d1 = k1[3];

            var a2 = k2[0];
            var b2 = k2[1];
            var c2 = k2[2];
            var d2 = k2[3];



            var res1 = det2(new[] { a1, a2 }, new[] { b1, b2 }, new[] { -d1, -d2 });
            var res2 = det2(new[] { a1, a2 }, new[] { c1, c2 }, new[] { -d1, -d2 });
            var res3 = det2(new[] { b1, b2 }, new[] { c1, c2 }, new[] { -d1, -d2 });

            List<Vector3d> vvv = new List<Vector3d>();

            if (res1 != null)
            {
                Vector3d v1 = new Vector3d((float)res1[0], (float)res1[1], 0);
                vvv.Add(v1);

            }

            if (res2 != null)
            {
                Vector3d v1 = new Vector3d((float)res2[0], 0, (float)res2[1]);
                vvv.Add(v1);
            }
            if (res3 != null)
            {
                Vector3d v1 = new Vector3d(0, (float)res3[0], (float)res3[1]);
                vvv.Add(v1);
            }

            var pnt = vvv.OrderBy(z => z.Length).First();


            var r1 = IsOnPlane(pnt);
            var r2 = IsOnPlane(pnt);

            ret.Start = pnt;
            ret.End = pnt + dir * 100;
            return ret;
        }
        public bool IsOnPlane(Vector3d orig, Vector3d normal, Vector3d check, double tolerance = 10e-6)
        {
            return (Math.Abs(Vector3d.Dot(orig - check, normal)) < tolerance);
        }
        public bool IsOnPlane(Vector3d v)
        {
            return IsOnPlane(Position, Normal, v);
        }
        double[] det2(double[] a, double[] b, double[] c)
        {
            var d = a[0] * b[1] - a[1] * b[0];
            if (d == 0) return null;
            var d1 = c[0] * b[1] - c[1] * b[0];
            var d2 = a[0] * c[1] - a[1] * c[0];
            var x = d1 / d;
            var y = d2 / d;
            return new[] { x, y };
        }

        public bool Fill { get; set; }

        public static List<ICommand> Commands = new List<ICommand>();
        ICommand[] ICommandsContainer.Commands => Commands.ToArray();

        public double[] GetKoefs()
        {
            double[] ret = new double[4];
            ret[0] = Normal.X;
            ret[1] = Normal.Y;
            ret[2] = Normal.Z;
            ret[3] = -(ret[0] * Position.X + Position.Y * ret[1] + Position.Z * ret[2]);

            return ret;
        }

        public override void Draw()
        {
            if (!Visible) return;
            
        }

        public IName[] GetObjects()
        {
            List<IName> ret = new List<IName>();
            var fld = GetType().GetProperties();
            for (int i = 0; i < fld.Length; i++)
            {

                var at = fld[i].GetCustomAttributes(typeof(EditFieldAttribute), true);
                if (at != null && at.Length > 0)
                {
                    if (fld[i].PropertyType == typeof(Vector3d))
                    {
                        //ret.Add(new VectorEditor(fld[i]) { Object = this });
                    }
                    if (fld[i].PropertyType == typeof(int))
                    {
                        ret.Add(new IntFieldEditor(fld[i]) { Object = this });
                    }
                }
            }
            return ret.ToArray();
        }
    }
    public class Token
    {
        public string Text;
        public object Tag;
    }
    public class LiteCadException : Exception
    {
        public LiteCadException(string str) : base(str) { }
    }

    public class ConstraintsException : Exception
    {
        public ConstraintsException(string msg) : base(msg) { }
    }
    public class XmlNameAttribute : Attribute
    {
        public string XmlName { get; set; }
    }
    
    public class ChangeCand
    {
        public DraftPoint Point;
        public Vector2d Position;
        public void Apply()
        {
            Point.SetLocation(Position);
        }

    }
    public interface IPropEditor
    {
        void Init(object o);
        object ReturnValue { get; }
    }
    public class VertexInfo
    {
        public Vector3d Position;
        public Vector3d Normal;
    }
    public class ParallelConstraintHelper : AbstractDrawable, IDraftConstraintHelper
    {
        public readonly ParallelConstraint constraint;
        public ParallelConstraintHelper(ParallelConstraint c)
        {
            constraint = c;
        }

        public Vector2d SnapPoint { get; set; }
        public DraftConstraint Constraint => constraint;

        public bool Enabled { get => constraint.Enabled; set => constraint.Enabled = value; }

        public Draft DraftParent => throw new System.NotImplementedException();

        public void Draw(IDrawingContext ctx)
        {
            var dp0 = constraint.Element1.Center;
            var dp1 = constraint.Element2.Center;
            var tr0 = ctx.Transform(dp0);
            var tr1 = ctx.Transform(dp1);
            var text = ctx.Transform((dp0 + dp1) / 2);

            ctx.DrawString("||", SystemFonts.DefaultFont, Brushes.Black, text);
            SnapPoint = (dp0 + dp1) / 2;
            AdjustableArrowCap bigArrow = new AdjustableArrowCap(5, 5);
            Pen p = new Pen(Color.Red, 1);
            p.CustomEndCap = bigArrow;
            p.CustomStartCap = bigArrow;


            //create bezier here
            ctx.DrawPolygon(p, new PointF[] { tr0, tr1 });
        }



        public override void Draw()
        {

        }
    }
    public abstract class AbstractDrawable : IDrawable
    {
        public static IMessageReporter MessageReporter;
        public AbstractDrawable()
        {
            Id = FactoryHelper.NewId++;
        }
        public bool Frozen { get; set; }

        public AbstractDrawable(XElement item)
        {
            if (item.Attribute("id") != null)
            {
                Id = int.Parse(item.Attribute("id").Value);
                FactoryHelper.NewId = Math.Max(FactoryHelper.NewId, Id + 1);
            }
            else
            {
                Id = FactoryHelper.NewId++;
            }
        }
        public string Name { get; set; }

        public abstract void Draw();

        public virtual void RemoveChild(IDrawable dd)
        {
            Childs.Remove(dd);
        }

        public virtual void Store(TextWriter writer)
        {

        }

        public virtual IDrawable[] GetAll(Predicate<IDrawable> p)
        {
            if (Childs.Count == 0)
                return new[] { this };
            return new[] { this }.Union(Childs.SelectMany(z => z.GetAll(p))).ToArray();
        }

        public bool Visible { get; set; } = true;
        public bool Selected { get; set; }

        public List<IDrawable> Childs { get; set; } = new List<IDrawable>();

        public IDrawable Parent { get; set; }
        public int Id { get; set; }

        protected TransformationChain _matrix = new TransformationChain();
        public TransformationChain Matrix { get => _matrix; set => _matrix = value; }
        public int Z { get; set; }
    }
    public class TransformationChain
    {
        public void StoreXml(TextWriter writer)
        {
            writer.WriteLine("<transformationChain>");
            foreach (var item in Items)
            {
                item.StoreXml(writer);
            }
            writer.WriteLine("</transformationChain>");
        }

        public void RestoreXml(XElement xElement)
        {
            Items.Clear();
            Type[] types = new[] {
                typeof(ScaleTransformChainItem),
                typeof(TranslateTransformChainItem),
                typeof(RotationTransformChainItem)
            };
            foreach (var item in xElement.Element("transformationChain").Elements())
            {
                var fr = types.First(z => (z.GetCustomAttributes(typeof(XmlNameAttribute), true).First() as XmlNameAttribute).XmlName == item.Name);
                var v = Activator.CreateInstance(fr) as TransformationChainItem;
                v.RestoreXml(item);
                Items.Add(v);
            }
        }

        public List<TransformationChainItem> Items = new List<TransformationChainItem>();
        public Matrix4d Calc()
        {
            var r = Matrix4d.Identity;
            foreach (var item in Items)
            {
                r *= item.Matrix();
            }
            return r;
        }

        public TransformationChain Clone()
        {
            TransformationChain ret = new TransformationChain();
            foreach (var item in Items)
            {
                ret.Items.Add(item.Clone());
            }
            return ret;
        }
    }
    public abstract class TransformationChainItem : IXmlStorable
    {
        public abstract Matrix4d Matrix();

        void IXmlStorable.RestoreXml(XElement elem)
        {
            RestoreXml(elem);
        }

        internal abstract void StoreXml(TextWriter writer);
        internal abstract void RestoreXml(XElement elem);

        void IXmlStorable.StoreXml(TextWriter writer)
        {
            StoreXml(writer);
        }

        internal abstract TransformationChainItem Clone();
    }
    public interface IXmlStorable
    {
        void StoreXml(TextWriter writer);
        void RestoreXml(XElement elem);

    }
    public class ClipperHelper
    {
        public static NFP clipperToSvg(IList<IntPoint> polygon, double clipperScale = 10000000)
        {
            List<Vector2d> ret = new List<Vector2d>();

            for (var i = 0; i < polygon.Count; i++)
            {
                ret.Add(new Vector2d(polygon[i].X / clipperScale, polygon[i].Y / clipperScale));
            }

            return new NFP() { Points = ret.ToArray() };
        }

        public static IntPoint[] ScaleUpPaths(NFP p, double scale = 10000000)
        {
            List<IntPoint> ret = new List<IntPoint>();

            for (int i = 0; i < p.Points.Count(); i++)
            {
                ret.Add(new ClipperLib.IntPoint(
                    (long)Math.Round((decimal)p.Points[i].X * (decimal)scale),
                    (long)Math.Round((decimal)p.Points[i].Y * (decimal)scale)
                ));

            }
            return ret.ToArray();
        }

        public static NFP[] offset(NFP polygon, double offset, JoinType jType = JoinType.jtMiter, double clipperScale = 10000000, double curveTolerance = 0.72, double miterLimit = 4)
        {
            var p = ScaleUpPaths(polygon, clipperScale).ToList();

            var co = new ClipperLib.ClipperOffset(miterLimit, curveTolerance * clipperScale);
            co.AddPath(p.ToList(), jType, ClipperLib.EndType.etClosedPolygon);

            var newpaths = new List<List<ClipperLib.IntPoint>>();
            co.Execute(ref newpaths, offset * clipperScale);

            var result = new List<NFP>();
            for (var i = 0; i < newpaths.Count; i++)
            {
                result.Add(clipperToSvg(newpaths[i]));
            }
            return result.ToArray();
        }
        public static IntPoint[][] nfpToClipperCoordinates(NFP nfp, double clipperScale = 10000000)
        {

            List<IntPoint[]> clipperNfp = new List<IntPoint[]>();

            // children first
            if (nfp.Childrens != null && nfp.Childrens.Count > 0)
            {
                for (var j = 0; j < nfp.Childrens.Count; j++)
                {
                    if (GeometryUtils.polygonArea(nfp.Childrens[j]) < 0)
                    {
                        nfp.Childrens[j].Reverse();
                    }
                    //var childNfp = SvgNest.toClipperCoordinates(nfp.children[j]);
                    var childNfp = ScaleUpPaths(nfp.Childrens[j], clipperScale);
                    clipperNfp.Add(childNfp);
                }
            }

            if (GeometryUtils.polygonArea(nfp) > 0)
            {
                nfp.Reverse();
            }


            //var outerNfp = SvgNest.toClipperCoordinates(nfp);

            // clipper js defines holes based on orientation

            var outerNfp = ScaleUpPaths(nfp, clipperScale);

            //var cleaned = ClipperLib.Clipper.CleanPolygon(outerNfp, 0.00001*config.clipperScale);

            clipperNfp.Add(outerNfp);
            //var area = Math.abs(ClipperLib.Clipper.Area(cleaned));

            return clipperNfp.ToArray();
        }
        public static IntPoint[][] ToClipperCoordinates(NFP[] nfp, double clipperScale = 10000000)
        {
            List<IntPoint[]> clipperNfp = new List<IntPoint[]>();
            for (var i = 0; i < nfp.Count(); i++)
            {
                var clip = nfpToClipperCoordinates(nfp[i], clipperScale);
                clipperNfp.AddRange(clip);
            }

            return clipperNfp.ToArray();
        }
        public static NFP toNestCoordinates(IntPoint[] polygon, double scale)
        {
            var clone = new List<Vector2d>();

            for (var i = 0; i < polygon.Count(); i++)
            {
                clone.Add(new Vector2d(
                     polygon[i].X / scale,
                             polygon[i].Y / scale
                        ));
            }
            return new NFP() { Points = clone.ToArray() };
        }
        public static NFP[] intersection(NFP polygon, NFP polygon1, double offset, JoinType jType = JoinType.jtMiter, double clipperScale = 10000000, double curveTolerance = 0.72, double miterLimit = 4)
        {
            var p = ToClipperCoordinates(new[] { polygon }, clipperScale).ToList();
            var p1 = ToClipperCoordinates(new[] { polygon1 }, clipperScale).ToList();

            Clipper clipper = new Clipper();
            clipper.AddPaths(p.Select(z => z.ToList()).ToList(), PolyType.ptClip, true);
            clipper.AddPaths(p1.Select(z => z.ToList()).ToList(), PolyType.ptSubject, true);

            List<List<IntPoint>> finalNfp = new List<List<IntPoint>>();
            if (clipper.Execute(ClipType.ctIntersection, finalNfp, PolyFillType.pftNonZero, PolyFillType.pftNonZero) && finalNfp != null && finalNfp.Count > 0)
            {
                return finalNfp.Select(z => toNestCoordinates(z.ToArray(), clipperScale)).ToArray();
            }
            return null;
        }
        public static NFP MinkowskiSum(NFP pattern, NFP path, bool useChilds = false, bool takeOnlyBiggestArea = true)
        {
            var ac = ScaleUpPaths(pattern);

            List<List<IntPoint>> solution = null;
            if (useChilds)
            {
                var bc = nfpToClipperCoordinates(path);
                for (var i = 0; i < bc.Length; i++)
                {
                    for (int j = 0; j < bc[i].Length; j++)
                    {
                        bc[i][j].X *= -1;
                        bc[i][j].Y *= -1;
                    }
                }

                solution = ClipperLib.Clipper.MinkowskiSum(new List<IntPoint>(ac), new List<List<IntPoint>>(bc.Select(z => z.ToList())), true);
            }
            else
            {
                var bc = ScaleUpPaths(path);
                for (var i = 0; i < bc.Length; i++)
                {
                    bc[i].X *= -1;
                    bc[i].Y *= -1;
                }
                solution = Clipper.MinkowskiSum(new List<IntPoint>(ac), new List<IntPoint>(bc), true);
            }
            NFP clipperNfp = null;

            double? largestArea = null;
            int largestIndex = -1;

            for (int i = 0; i < solution.Count(); i++)
            {
                var n = toNestCoordinates(solution[i].ToArray(), 10000000);
                var sarea = Math.Abs(GeometryUtils.polygonArea(n));
                if (largestArea == null || largestArea < sarea)
                {
                    clipperNfp = n;
                    largestArea = sarea;
                    largestIndex = i;
                }
            }
            if (!takeOnlyBiggestArea)
            {
                for (int j = 0; j < solution.Count; j++)
                {
                    if (j == largestIndex) continue;
                    var n = toNestCoordinates(solution[j].ToArray(), 10000000);
                    if (clipperNfp.Childrens == null)
                        clipperNfp.Childrens = new List<NFP>();
                    clipperNfp.Childrens.Add(n);
                }
            }

            for (var i = 0; i < clipperNfp.Length; i++)
            {
                clipperNfp.Points[i].X *= -1;
                clipperNfp.Points[i].Y *= -1;
                clipperNfp.Points[i].X += pattern[0].X;
                clipperNfp.Points[i].Y += pattern[0].Y;
            }
            var minx = clipperNfp.Points.Min(z => z.X);
            var miny = clipperNfp.Points.Min(z => z.Y);
            var minx2 = path.Points.Min(z => z.X);
            var miny2 = path.Points.Min(z => z.Y);

            var shiftx = minx2 - minx;
            var shifty = miny2 - miny;
            if (clipperNfp.Childrens != null)
                foreach (var nFP in clipperNfp.Childrens)
                {
                    for (int j = 0; j < nFP.Length; j++)
                    {

                        nFP.Points[j].X *= -1;
                        nFP.Points[j].Y *= -1;
                        nFP.Points[j].X += pattern[0].X;
                        nFP.Points[j].Y += pattern[0].Y;
                    }
                }

            return clipperNfp;
        }

    }
    public class NFP
    {
        public Vector2d[] Points = new Vector2d[] { };
        public List<NFP> Childrens = new List<NFP>();
        public NFP Parent;
        public Vector2d this[int ind]
        {
            get
            {
                return Points[ind];
            }
        }
        public void Shift(Vector2d vector)
        {
            for (int i = 0; i < Points.Length; i++)
            {
                Points[i].X += vector.X;
                Points[i].Y += vector.Y;
            }
        }
        public double SignedArea()
        {
            return GeometryUtils.signed_area(Points);
        }

        public int Length
        {
            get
            {
                return Points.Length;
            }
        }

        public void Reverse()
        {
            Points = Points.Reverse().ToArray();
        }
    }
    public interface IMessageReporter
    {
        void Warning(string text);
        void Error(string text);
        void Info(string text);
    }
    public interface IDraftHelper : IDrawable
    {

        Draft DraftParent { get; }
        bool Enabled { get; set; }

        void Draw(IDrawingContext ctx);

    }
    [XmlName(XmlName = "equalsConstraint")]
    public class EqualsConstraint : DraftConstraint, IXmlStorable
    {
        public DraftLine SourceLine;
        public DraftLine TargetLine;
        public EqualsConstraint(DraftLine target, DraftLine source, Draft parent) : base(parent)
        {
            SourceLine = source;
            TargetLine = target;
        }

        public EqualsConstraint(XElement el, Draft parent) : base(parent)
        {
            TargetLine = parent.Elements.OfType<DraftLine>().First(z => z.Id == int.Parse(el.Attribute("targetId").Value));
            SourceLine = parent.Elements.OfType<DraftLine>().First(z => z.Id == int.Parse(el.Attribute("sourceId").Value));
        }

        public override bool IsSatisfied(float eps = 1E-06F)
        {
            return Math.Abs(TargetLine.Length - SourceLine.Length) < eps;
        }

        ChangeCand[] GetCands()
        {
            List<ChangeCand> ret = new List<ChangeCand>();
            var dir = TargetLine.Dir;
            ret.Add(new ChangeCand() { Point = TargetLine.V0, Position = TargetLine.V1.Location + SourceLine.Length * (-dir) });
            ret.Add(new ChangeCand() { Point = TargetLine.V1, Position = TargetLine.V0.Location + SourceLine.Length * dir });
            return ret.Where(z => !z.Point.Frozen).ToArray();
        }

        public override void RandomUpdate(ConstraintSolverContext ctx)
        {
            var cc = GetCands();
            var ar = cc.OrderBy(z => GeometryUtils.Random.Next(100)).ToArray();
            var fr = ar.First();
            fr.Apply();
        }

        public bool IsSame(EqualsConstraint cc)
        {
            return cc.TargetLine == TargetLine && cc.SourceLine == SourceLine;
        }

        public override bool ContainsElement(DraftElement de)
        {
            return TargetLine == de || TargetLine.V0 == de || TargetLine.V1 == de || SourceLine == de || SourceLine.V0 == de || SourceLine.V1 == de;
        }

        internal override void Store(TextWriter writer)
        {
            writer.WriteLine($"<equalsConstraint targetId=\"{TargetLine.Id}\" sourceId=\"{SourceLine.Id}\"/>");
        }

        public void StoreXml(TextWriter writer)
        {
            Store(writer);
        }

        public void RestoreXml(XElement elem)
        {
            //   var targetId = int.Parse(elem.Attribute("targetId").Value);
            // Line = Line.Parent.Elements.OfType<DraftLine>().First(z => z.Id == targetId);
        }
    }
    public class CSPConstrEqualTwoVars : CSPConstrEqualExpression
    {
        public CSPConstrEqualTwoVars(CSPVar var1, CSPVar var2)
        {
            Var1 = var1;
            Var2 = var2;
            Expression = $"{var1.Name}={var2.Name}";
            Vars = new[] { var1, var2 };
        }
        public CSPVar Var1;
        public CSPVar Var2;
    }
    [XmlName(XmlName = "pointPositionConstraint")]
    public class PointPositionConstraint : DraftConstraint
    {
        public readonly DraftPoint Point;

        Vector2d _location;
        public Vector2d Location
        {
            get => _location; set
            {
                BeforeChanged?.Invoke();
                _location = value;
                Parent.RecalcConstraints();
            }
        }

        public double X
        {
            get => _location.X; set
            {
                _location.X = value;
                Parent.RecalcConstraints();
            }
        }
        public double Y
        {
            get => _location.Y; set
            {
                _location.Y = value;
                Parent.RecalcConstraints();
            }
        }
        public PointPositionConstraint(XElement el, Draft parent) : base(parent)
        {
            if (el.Attribute("id") != null)
                Id = int.Parse(el.Attribute("id").Value);

            Point = parent.Elements.OfType<DraftPoint>().First(z => z.Id == int.Parse(el.Attribute("pointId").Value));
            X = Helpers.ParseDouble(el.Attribute("x").Value);
            Y = Helpers.ParseDouble(el.Attribute("y").Value);
        }

        public PointPositionConstraint(DraftPoint draftPoint1, Draft parent) : base(parent)
        {
            this.Point = draftPoint1;
        }

        public override bool IsSatisfied(float eps = 1e-6f)
        {
            return (Point.Location - Location).Length < eps;
        }

        internal void Update()
        {
            //Point.SetLocation(Location);
            var top = Point.Parent.Constraints.OfType<TopologyConstraint>().FirstOrDefault();
            var dir = Location - Point.Location;
            if (top != null)
            {
                //whole draft translate
                var d = Point.Parent;
                foreach (var item in d.DraftPoints)
                {
                    item.SetLocation(item.Location + dir);
                }
            }
            else
                Point.SetLocation(Location);
        }

        public override void RandomUpdate(ConstraintSolverContext ctx)
        {
            Update();
        }

        public bool IsSame(PointPositionConstraint cc)
        {
            return cc.Point == Point;
        }

        public override bool ContainsElement(DraftElement de)
        {
            return Point == de;
        }

        internal override void Store(TextWriter writer)
        {
            writer.WriteLine($"<pointPositionConstraint id=\"{Id}\" pointId=\"{Point.Id}\" x=\"{X}\" y=\"{Y}\"/>");
        }
    }
    [XmlName(XmlName = "linearConstraintHelper")]
    public class LinearConstraintHelper : AbstractDrawable, IDraftConstraintHelper
    {
        public readonly LinearConstraint constraint;
        public LinearConstraintHelper(Draft parent, LinearConstraint c)
        {
            DraftParent = parent;
            constraint = c;
        }
        public LinearConstraintHelper(XElement el, Draft draft)
        {
            DraftParent = draft;
            var cid = int.Parse(el.Attribute("constrId").Value);
            constraint = draft.Constraints.OfType<LinearConstraint>().First(z => z.Id == cid);
            Shift = int.Parse(el.Attribute("shift").Value);
        }
        public decimal Length { get => constraint.Length; set => constraint.Length = value; }
        public Vector2d SnapPoint { get; set; }
        public DraftConstraint Constraint => constraint;
        public int Shift { get; set; } = 10;
        public bool Enabled { get => constraint.Enabled; set => constraint.Enabled = value; }

        public Draft DraftParent { get; private set; }

        public override void Store(TextWriter writer)
        {
            writer.WriteLine($"<linearConstraintHelper constrId=\"{constraint.Id}\" shift=\"{Shift}\" enabled=\"{Enabled}\" snapPoint=\"{SnapPoint.X};{SnapPoint.Y}\"/>");
        }
        public static SKPath RoundedRect(SKRect bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            SKRect arc = new SKRect(bounds.Location.X, bounds.Location.Y, bounds.Location.X + size.Width, bounds.Location.Y + size.Height);
            SKPath path = new SKPath();


            if (radius == 0)
            {
                path.AddRect(bounds);
                return path;
            }

            // top left arc  
            path.AddArc(arc, 180, 90);

            // top right arc  
            arc.Left = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // bottom right arc  
            arc.Top = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // bottom left arc 
            arc.Left = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.Close();
            return path;
        }

        public void Draw(IDrawingContext ctx)
        {
            var editor = ctx.Tag as IDraftEditor;
            var elems = new[] { constraint.Element1, constraint.Element2 };
            AdjustableArrowCap bigArrow = new AdjustableArrowCap(5, 5);
            var hovered = editor.nearest == this;
            Pen p = new Pen(hovered ? Color.Red : Color.Blue, 1);
            p.CustomEndCap = bigArrow;
            p.CustomStartCap = bigArrow;
            if (constraint.Element1 is DraftPoint dp0 && constraint.Element2 is DraftPoint dp1)
            {
                //get perpencdicular
                var diff = (dp1.Location - dp0.Location).Normalized();
                var perp = new Vector2d(-diff.Y, diff.X);
                var tr0 = ctx.Transform(dp0.Location + perp * Shift);
                var tr1 = ctx.Transform(dp1.Location + perp * Shift);
                var tr2 = ctx.Transform(dp0.Location);
                var tr3 = ctx.Transform(dp1.Location);
                var shiftX = 0;
                var text = (dp0.Location + perp * Shift + dp1.Location + perp * Shift) / 2 + perp;
                var trt = ctx.Transform(text);
                trt = new PointF(trt.X + shiftX, trt.Y);
                var ms = ctx.MeasureString(constraint.Length.ToString(), SystemFonts.DefaultFont);

                var fontBrush = Brushes.Black;
                if (hovered)
                    fontBrush = Brushes.Red;

                if (!constraint.IsSatisfied())
                {
                    var rect = new SKRect(trt.X, trt.Y, trt.X + ms.Width, trt.Y + ms.Height);
                    rect.Inflate(1.3f, 1.3f);
                    var rr = new SKRoundRect(rect, 5);
                    ctx.FillRoundRectangle(Brushes.Red, rr);
                    ctx.DrawRoundRectangle(Pens.Black, rr);
                    fontBrush = Brushes.White;
                }

                ctx.DrawString(constraint.Length.ToString(), SystemFonts.DefaultFont, fontBrush, trt);

                SnapPoint = text;

                //ctx.DrawLine(p, tr0, tr1);
                ctx.DrawArrowedLine(p, tr0, tr1, 5);
                ctx.SetPen(hovered ? Pens.Red : Pens.Blue);
                ctx.DrawLine(tr0, tr2);
                ctx.DrawLine(tr1, tr3);
                if (hovered)
                {
                    ctx.FillCircle(Brushes.Red, tr2.X, tr2.Y, 5);
                    ctx.FillCircle(Brushes.Red, tr3.X, tr3.Y, 5);
                }
            }
            if (elems.Any(z => z is DraftLine) && elems.Any(z => z is DraftPoint))
            {
                var dp = elems.OfType<DraftPoint>().First();
                var dl = elems.OfType<DraftLine>().First();
                var pp = GeometryUtils.GetProjPoint(dp.Location, dl.V0.Location, dl.Dir);

                var diff = (dp.Location - pp).Normalized();
                var perp = new Vector2d(-diff.Y, diff.X);
                var tr0 = ctx.Transform(dp.Location + perp * Shift);
                var tr1 = ctx.Transform(pp + perp * Shift);
                var text = (dp.Location + perp * Shift + pp + perp * Shift) / 2 + perp;
                var trt = ctx.Transform(text);
                ctx.DrawString(constraint.Length.ToString(), SystemFonts.DefaultFont, Brushes.Black, trt);
                SnapPoint = text;
                //get proj of point to line
                //var diff = (pp - dp.Location).Length;
                ctx.SetPen(p);
                ctx.DrawLine(tr0, tr1);
                var tr2 = ctx.Transform(dp.Location);
                var tr3 = ctx.Transform(pp);
                ctx.SetPen(Pens.Red);
                ctx.DrawLine(tr0, tr2);
                ctx.DrawLine(tr1, tr3);
            }
        }

        public override void Draw()
        {

        }
    }
    public interface IEditFieldsContainer
    {
        IName[] GetObjects();
    }
    public class EditFieldAttribute : Attribute
    {

    }
    public interface ICommand
    {
        string Name { get; }
        Action<IDrawable, object> Process { get; }
    }
    public interface ICommandsContainer
    {
        ICommand[] Commands { get; }
    }
    public interface IName
    {
        string Name { get; set; }
    }
    public class VerticalConstraintHelper : AbstractDrawable, IDraftConstraintHelper
    {
        public readonly VerticalConstraint constraint;
        public VerticalConstraintHelper(VerticalConstraint c)
        {
            constraint = c;
        }

        public Vector2d SnapPoint { get; set; }
        public DraftConstraint Constraint => constraint;

        public bool Enabled { get => constraint.Enabled; set => constraint.Enabled = value; }

        public Draft DraftParent { get; private set; }

        public void Draw(IDrawingContext ctx)
        {
            var dp0 = constraint.Line.Center;
            var perp = new Vector2d(-constraint.Line.Dir.Y, constraint.Line.Dir.X);

            SnapPoint = (dp0);
            var tr0 = ctx.Transform(dp0 + perp * 15 / ctx.zoom);

            var gap = 10;
            //create bezier here
            ctx.FillCircle(Brushes.Green, tr0.X, tr0.Y, gap);
            ctx.SetPen(new Pen(Brushes.Orange, 3));
            ctx.DrawLine(tr0.X, tr0.Y + 5, tr0.X, tr0.Y - 5);
        }

        public override void Draw()
        {

        }
    }
    public interface IEditor
    {
        IDrawable[] Parts { get; }
        ITool CurrentTool { get; }
        event Action<ITool> ToolChanged;
        IntersectInfo Pick { get; }
        void ObjectSelect(object nearest);
        void ResetTool();
        void Backup();
        void Undo();
    }
    public class IntersectInfo
    {
        public double Distance;
        public TriangleInfo Target;
        public IMeshNodesContainer Model;
        public Vector3d Point { get; set; }
        public object Parent;
    }
    public interface IMeshNodesContainer
    {
        MeshNode[] Nodes { get; }
    }
    public class MeshNode
    {
        public bool Visible { get; set; } = true;
        //public BRepFace Parent;
        public List<TriangleInfo> Triangles = new List<TriangleInfo>();

        public bool Contains(TriangleInfo tr)
        {
            return Triangles.Any(z => z.IsSame(tr));
        }

        public virtual void SwitchNormal()
        {
            //if (!(Parent.Surface is BRepPlane pl)) return;

            foreach (var item in Triangles)
            {
                foreach (var vv in item.Vertices)
                {
                    vv.Normal *= -1;
                }
            }
        }

        public MeshNode RestoreXml(XElement mesh)
        {
            MeshNode ret = new MeshNode();
            foreach (var tr in mesh.Elements())
            {
                TriangleInfo tt = new TriangleInfo();
                tt.RestoreXml(tr);
                ret.Triangles.Add(tt);
            }
            return ret;
        }

        public void StoreXml(TextWriter writer)
        {
            writer.WriteLine("<mesh>");
            foreach (var item in Triangles)
            {
                item.StoreXml(writer);
            }
            writer.WriteLine("</mesh>");
        }

        public bool Contains(TriangleInfo target, Matrix4d mtr1)
        {
            return Triangles.Any(z => z.Multiply(mtr1).IsSame(target));
        }
    }
    public class PerpendicularConstraintTool : AbstractDraftTool
    {
        public PerpendicularConstraintTool(IDraftEditor ee) : base(ee)
        {
        }


        public override void Deselect()
        {

        }

        public override void Draw()
        {

        }

        public override void MouseDown(MouseEventArgs e)
        {

        }

        public override void MouseUp(MouseEventArgs e)
        {

        }

        public override void Select()
        {


        }

        public override void Update()
        {

        }
    }
    public class PerpendicularConstraint : DraftConstraint
    {
        public DraftLine Element1;
        public DraftLine Element2;
        public DraftPoint CommonPoint;
        public PerpendicularConstraint(DraftLine draftPoint1, DraftLine draftPoint2, Draft parent) : base(parent)
        {
            var ar1 = new[] { draftPoint2.V0, draftPoint2.V1 };
            var ar2 = new[] { draftPoint1.V0, draftPoint1.V1 };
            if (ar1.Intersect(ar2).Count() != 1) throw new ArgumentException();
            CommonPoint = ar1.Intersect(ar2).First();
            this.Element1 = draftPoint1;
            this.Element2 = draftPoint2;
        }

        public override bool IsSatisfied(float eps = 1e-6f)
        {
            var dp0 = Element1 as DraftLine;
            var dp1 = Element2 as DraftLine;

            return Math.Abs(Vector2d.Dot(dp0.Dir, dp1.Dir)) <= eps;
        }

        internal void Update()
        {
            var dp0 = Element1 as DraftLine;
            var dp1 = Element2 as DraftLine;
            /*var diff = (dp1.Location - dp0.Location).Normalized();
            dp1.Location = dp0.Location + diff * (double)Length;*/
        }
        public override void RandomUpdate(ConstraintSolverContext ctx)
        {
            var dp0 = Element1 as DraftLine;
            var dp1 = Element2 as DraftLine;
            if (dp0.Frozen && dp1.Frozen)
            {
                throw new ConstraintsException("double frozen");
            }
            var ar = new[] { dp0, dp1 }.OrderBy(z => GeometryUtils.Random.Next(100)).ToArray();
            dp0 = ar[0];
            dp1 = ar[1];
            if (dp1.Frozen || (dp1.V0 == CommonPoint && dp1.V1.Frozen) || (dp1.V1 == CommonPoint && dp1.V0.Frozen))
            {
                var temp = dp1;
                dp1 = dp0;
                dp0 = temp;
            }

            //generate all valid candidates first. then random select
            //not frozen points to move
            var mp = new[] { dp1.V0, dp1.V1, dp0.V1, dp0.V0 }.Distinct().Where(z => !z.Frozen).ToArray();

            if (!CommonPoint.Frozen)
            {
                //intersect
            }
            else
            if (dp1.V0 == CommonPoint)
            {
                var diff = dp1.Dir * dp1.Length;
                var projectV = new Vector2d(-dp0.Dir.Y, dp0.Dir.X);
                var cand1 = CommonPoint.Location + projectV * dp1.Length;
                var cand2 = CommonPoint.Location - projectV * dp1.Length;
                if ((cand1 - dp1.V1.Location).Length < (cand2 - dp1.V1.Location).Length)
                {
                    dp1.V1.SetLocation(cand1);
                }
                else
                {
                    dp1.V1.SetLocation(cand2);
                }
            }
            else
            {
                var diff = dp1.Dir * dp1.Length;
                var projectV = new Vector2d(-dp0.Dir.Y, dp0.Dir.X);
                //dp1.V0.SetLocation(CommonPoint.Location + projectV * dp1.Length);
                var cand1 = CommonPoint.Location + projectV * dp1.Length;
                var cand2 = CommonPoint.Location - projectV * dp1.Length;
                if ((cand1 - dp1.V0.Location).Length < (cand2 - dp1.V0.Location).Length)
                {
                    dp1.V0.SetLocation(cand1);
                }
                else
                {
                    dp1.V0.SetLocation(cand2);
                }
            }
            /* var diff = (dp1.Location - dp0.Location).Normalized();
             dp1.Location = dp0.Location + diff * (double)Length;*/
        }
        public bool IsSame(PerpendicularConstraint cc)
        {
            return new[] { Element2, Element1 }.Except(new[] { cc.Element1, cc.Element2 }).Count() == 0;
        }

        public override bool ContainsElement(DraftElement de)
        {
            return Element1 == de || Element2 == de;
        }

        internal override void Store(TextWriter writer)
        {
            writer.WriteLine($"<perpendicularConstraint p0=\"{Element1.Id}\" p1=\"{Element2.Id}\"/>");
        }
    }
    public class SelectionTool : AbstractTool
    {
        public SelectionTool(IEditor editor) : base(editor)
        {

        }

        public override void Deselect()
        {
            //Form1.Form.ViewManager.Detach();
        }

        public override void Draw()
        {

        }

        public override void MouseDown(MouseEventArgs e)
        {

        }

        public override void MouseUp(MouseEventArgs e)
        {

        }

        public override void Select()
        {
            //Form1.Form.ViewManager.Attach(Form1.Form.evwrapper, Form1.Form.camera1);

        }

        public override void Update()
        {

        }
    }
    public class PerpendicularConstraintHelper : AbstractDrawable, IDraftConstraintHelper
    {
        public readonly PerpendicularConstraint constraint;
        public PerpendicularConstraintHelper(PerpendicularConstraint c)
        {
            constraint = c;
        }

        public Vector2d SnapPoint { get; set; }
        public DraftConstraint Constraint => constraint;

        public bool Enabled { get => constraint.Enabled; set => constraint.Enabled = value; }

        public Draft DraftParent => throw new System.NotImplementedException();

        public void Draw(IDrawingContext ctx)
        {
            var dp0 = constraint.Element1.Center;
            var dp1 = constraint.Element2.Center;
            var tr0 = ctx.Transform(dp0);
            var tr1 = ctx.Transform(dp1);
            var text = ctx.Transform((dp0 + dp1) / 2);

            ctx.DrawString("P-|", SystemFonts.DefaultFont, Brushes.Black, text);
            SnapPoint = (dp0 + dp1) / 2;
            AdjustableArrowCap bigArrow = new AdjustableArrowCap(5, 5);
            Pen p = new Pen(Color.Red, 1);
            p.CustomEndCap = bigArrow;
            p.CustomStartCap = bigArrow;


            //create bezier here
            ctx.DrawPolygon(p, new PointF[] { tr0, tr1 });
        }

        public override void Draw()
        {

        }
    }
    public abstract class AbstractTool : ITool
    {
        protected IEditor Editor;
        public AbstractTool(IEditor editor)
        {
            Editor = editor;
        }

        public abstract void Deselect();

        public abstract void Draw();

        public abstract void MouseDown(MouseEventArgs e);

        public abstract void MouseUp(MouseEventArgs e);

        public abstract void Select();

        public abstract void Update();

    }
    public static class DebugHelpers
    {

        public static Action<string> Error;
        public static Action<Exception> Exception;
        public static Action<string> Warning;
        public static Action<bool, float> Progress;
        public static void ToBitmap(Contour[] cntrs, Vector2d[][] triangls, float mult = 1, bool withTriang = false)
        {
            if (!Debugger.IsAttached) return;


            var maxx = cntrs.SelectMany(z => z.Elements).Max(z => Math.Max(z.Start.X, z.End.X));
            var minx = cntrs.SelectMany(z => z.Elements).Min(z => Math.Min(z.Start.X, z.End.X));
            var maxy = cntrs.SelectMany(z => z.Elements).Max(z => Math.Max(z.Start.Y, z.End.Y));
            var miny = cntrs.SelectMany(z => z.Elements).Min(z => Math.Min(z.Start.Y, z.End.Y));
            var dx = (float)(maxx - minx);
            var dy = (float)(maxy - miny);
            var mdx = Math.Max(dx, dy);
            Bitmap bmp = new Bitmap((int)(mdx * mult), (int)(mdx * mult));
            var gr = Graphics.FromImage(bmp);
            gr.Clear(Color.White);

            foreach (var item in triangls)
            {
                GraphicsPath gp = new GraphicsPath();
                gp.AddPolygon(item.Select(z => new PointF((float)((z.X - minx) / mdx * (bmp.Width - 1)),
                    (float)((z.Y - miny) / mdx * (bmp.Height - 1)))).ToArray());
                gr.FillPath(Brushes.LightBlue, gp);
                if (withTriang)
                {
                    gr.DrawPath(Pens.Black, gp);

                }
            }

            foreach (var cntr in cntrs)
            {
                foreach (var cc in cntr.Elements)
                {
                    var x1 = (float)(cc.Start.X - minx);
                    x1 = (x1 / mdx) * (bmp.Width - 1);
                    var y1 = (float)(cc.Start.Y - miny);
                    y1 = (y1 / mdx) * (bmp.Height - 1);
                    var x2 = (float)(cc.End.X - minx);
                    x2 = (x2 / mdx) * (bmp.Width - 1);
                    var y2 = (float)(cc.End.Y - miny);
                    y2 = (y2 / mdx) * (bmp.Height - 1);

                    gr.DrawLine(Pens.Black, x1, y1, x2, y2);
                }
            }

            ExecuteSTA(() => Clipboard.SetImage(bmp));
        }

        public static void ExecuteSTA(Action act)
        {
            if (!Debugger.IsAttached) return;
            Thread thread = new Thread(() => { act(); });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        public static bool DebugBitmapExportAllowed = false;

        public static void ToBitmap(Contour[] cntrs, float mult = 1)
        {
            if (!DebugBitmapExportAllowed) return;
            if (!Debugger.IsAttached) return;


            var maxx = cntrs.SelectMany(z => z.Elements).Max(z => Math.Max(z.Start.X, z.End.X));
            var minx = cntrs.SelectMany(z => z.Elements).Min(z => Math.Min(z.Start.X, z.End.X));
            var maxy = cntrs.SelectMany(z => z.Elements).Max(z => Math.Max(z.Start.Y, z.End.Y));
            var miny = cntrs.SelectMany(z => z.Elements).Min(z => Math.Min(z.Start.Y, z.End.Y));
            var dx = (float)(maxx - minx);
            var dy = (float)(maxy - miny);
            var mdx = Math.Max(dx, dy);
            Bitmap bmp = new Bitmap((int)(mdx * mult), (int)(mdx * mult));
            var gr = Graphics.FromImage(bmp);
            gr.Clear(Color.White);

            foreach (var cntr in cntrs)
            {
                foreach (var cc in cntr.Elements)
                {
                    var x1 = (float)(cc.Start.X - minx);
                    x1 = (x1 / mdx) * (bmp.Width - 1);
                    var y1 = (float)(cc.Start.Y - miny);
                    y1 = (y1 / mdx) * (bmp.Height - 1);
                    var x2 = (float)(cc.End.X - minx);
                    x2 = (x2 / mdx) * (bmp.Width - 1);
                    var y2 = (float)(cc.End.Y - miny);
                    y2 = (y2 / mdx) * (bmp.Height - 1);

                    gr.DrawLine(Pens.Black, x1, y1, x2, y2);
                }
            }

            ExecuteSTA(() => Clipboard.SetImage(bmp));
        }
    }
    public class IntFieldEditor : IName
    {
        public IntFieldEditor(PropertyInfo f)
        {
            Field = f;
            Name = f.Name;
        }
        public string Name { get; set; }
        public object Object;
        public PropertyInfo Field;
        public int Value
        {
            get
            {
                return ((int)Field.GetValue(Object));
            }
            set
            {
                Field.SetValue(Object, value);
            }
        }

    }
    public class CSPConstrEqualVarValue : CSPConstrEqualExpression
    {
        public CSPConstrEqualVarValue(CSPVar var, double val)
        {
            Var1 = var;
            Value = val;
            Expression = $"{Var1.Name}={val}";
            Vars = new[] { Var1 };
        }
        public CSPVar Var1;
        public double Value;
    }
    public class Segment
    {
        public override string ToString()
        {
            return "Start: " + Start + "; End: " + End;
        }
        public Vector2d Start;
        public Vector2d End;
        public double Length()
        {
            return (End - Start).Length;
        }
    }
    public class Plane
    {
        public Plane()
        {

        }

        public Vector3d Position { get; set; }


        public Vector3d Normal { get; set; }




        public Vector3d[] GetBasis()
        {
            Vector3d[] shifts = new[] { Vector3d.UnitX, Vector3d.UnitY, Vector3d.UnitZ };
            Vector3d axis1 = Vector3d.Zero;
            for (int i = 0; i < shifts.Length; i++)
            {
                var proj = ProjPoint(Position + shifts[i]);

                if (Vector3d.Distance(proj, Position) > 10e-6)
                {
                    axis1 = (proj - Position).Normalized();
                    break;
                }
            }
            var axis2 = Vector3d.Cross(Normal.Normalized(), axis1);

            return new[] { axis1, axis2 };
        }
        public Vector2d ProjectPointUV(Vector3d v)
        {
            var basis = GetBasis();
            return GetUVProjPoint(v, basis[0], basis[1]);
        }
        public Vector2d GetUVProjPoint(Vector3d point, Vector3d axis1, Vector3d axis2)
        {
            var p = GetProjPoint(point) - Position;
            var p1 = Vector3d.Dot(p, axis1);
            var p2 = Vector3d.Dot(p, axis2);
            return new Vector2d(p1, p2);
        }
        public Vector3d GetProjPoint(Vector3d point)
        {
            var v = point - Position;
            var nrm = Normal;
            var dist = Vector3d.Dot(v, nrm);
            var proj = point - dist * nrm;
            return proj;
        }
        public Vector3d ProjPoint(Vector3d point)
        {
            var nrm = Normal.Normalized();
            var v = point - Position;
            var dist = Vector3d.Dot(v, nrm);
            var proj = point - dist * nrm;
            return proj;
        }

        public Line3D Intersect(Plane ps)
        {
            Line3D ret = new Line3D();

            var dir = Vector3d.Cross(ps.Normal, Normal);


            var k1 = ps.GetKoefs();
            var k2 = GetKoefs();
            var a1 = k1[0];
            var b1 = k1[1];
            var c1 = k1[2];
            var d1 = k1[3];

            var a2 = k2[0];
            var b2 = k2[1];
            var c2 = k2[2];
            var d2 = k2[3];



            var res1 = det2(new[] { a1, a2 }, new[] { b1, b2 }, new[] { -d1, -d2 });
            var res2 = det2(new[] { a1, a2 }, new[] { c1, c2 }, new[] { -d1, -d2 });
            var res3 = det2(new[] { b1, b2 }, new[] { c1, c2 }, new[] { -d1, -d2 });

            List<Vector3d> vvv = new List<Vector3d>();

            if (res1 != null)
            {
                Vector3d v1 = new Vector3d((float)res1[0], (float)res1[1], 0);
                vvv.Add(v1);

            }

            if (res2 != null)
            {
                Vector3d v1 = new Vector3d((float)res2[0], 0, (float)res2[1]);
                vvv.Add(v1);
            }
            if (res3 != null)
            {
                Vector3d v1 = new Vector3d(0, (float)res3[0], (float)res3[1]);
                vvv.Add(v1);
            }

            var pnt = vvv.OrderBy(z => z.Length).First();


            var r1 = IsOnPlane(pnt);
            var r2 = IsOnPlane(pnt);

            ret.Start = pnt;
            ret.End = pnt + dir * 100;
            return ret;
        }
        public bool IsOnPlane(Vector3d orig, Vector3d normal, Vector3d check, double tolerance = 10e-6)
        {
            return (Math.Abs(Vector3d.Dot(orig - check, normal)) < tolerance);
        }
        public bool IsOnPlane(Vector3d v)
        {
            return IsOnPlane(Position, Normal, v);
        }
        double[] det2(double[] a, double[] b, double[] c)
        {
            var d = a[0] * b[1] - a[1] * b[0];
            if (d == 0) return null;
            var d1 = c[0] * b[1] - c[1] * b[0];
            var d2 = a[0] * c[1] - a[1] * c[0];
            var x = d1 / d;
            var y = d2 / d;
            return new[] { x, y };
        }




        public double[] GetKoefs()
        {
            double[] ret = new double[4];
            ret[0] = Normal.X;
            ret[1] = Normal.Y;
            ret[2] = Normal.Z;
            ret[3] = -(ret[0] * Position.X + Position.Y * ret[1] + Position.Z * ret[2]);

            return ret;
        }
    }
    [XmlName(XmlName = "scale")]
    public class ScaleTransformChainItem : TransformationChainItem
    {
        public Vector3d Vector;
        public override Matrix4d Matrix()
        {
            return Matrix4d.Scale(Vector);
        }

        internal override TransformationChainItem Clone()
        {
            return new ScaleTransformChainItem() { Vector = Vector };
        }

        internal override void RestoreXml(XElement elem)
        {
            Vector = Helpers.ParseVector(elem.Attribute("vec").Value);
        }

        internal override void StoreXml(TextWriter writer)
        {
            writer.WriteLine($"<scale vec=\"{Vector.X};{Vector.Y};{Vector.Z}\"/>");

        }
    }
    [XmlName(XmlName = "translate")]
    public class TranslateTransformChainItem : TransformationChainItem
    {
        public Vector3d Vector;
        public override Matrix4d Matrix()
        {
            return Matrix4d.CreateTranslation(Vector);
        }

        internal override TransformationChainItem Clone()
        {
            return new TranslateTransformChainItem() { Vector = Vector };
        }

        internal override void RestoreXml(XElement elem)
        {
            Vector = Helpers.ParseVector(elem.Attribute("vec").Value);
        }

        internal override void StoreXml(TextWriter writer)
        {
            writer.WriteLine($"<translate vec=\"{Vector.X};{Vector.Y};{Vector.Z}\"/>");
        }
    }
    [XmlName(XmlName = "rotate")]
    public class RotationTransformChainItem : TransformationChainItem
    {
        public Vector3d Axis = Vector3d.UnitZ;
        public double Angle { get; set; }
        public override Matrix4d Matrix()
        {
            return Matrix4d.Rotate(Axis, Angle * Math.PI / 180);
        }

        internal override TransformationChainItem Clone()
        {
            return new RotationTransformChainItem() { Axis = Axis, Angle = Angle };
        }

        internal override void RestoreXml(XElement elem)
        {
            Axis = Helpers.ParseVector(elem.Attribute("axis").Value);
            Angle = Helpers.ParseDouble(elem.Attribute("angle").Value);
        }

        internal override void StoreXml(TextWriter writer)
        {
            writer.WriteLine($"<rotate axis=\"{Axis.X};{Axis.Y};{Axis.Z}\" angle=\"{Angle}\"/>");

        }
    }
}
