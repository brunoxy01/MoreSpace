using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MoreSpace {
/// <summary>Native keyboard-accessible button with a short hover/press animation.</summary>
class RoundedButton:Button {
 public bool MultilineText;
 readonly Timer animation=new Timer{Interval=15};
 float hover,press; bool inside,down;
 public RoundedButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;animation.Tick+=Animate;}
 public override Size GetPreferredSize(Size proposedSize){var text=TextRenderer.MeasureText(Text,Font,Size.Empty,TextFormatFlags.NoPadding|TextFormatFlags.SingleLine);return new Size(text.Width+Padding.Horizontal+28,Math.Max(38,text.Height+Padding.Vertical+12));}
 static float Ease(float value,float target){float next=value+(target-value)*0.28f;return Math.Abs(next-target)<0.015f?target:next;}
 void Animate(object sender,EventArgs e){hover=Ease(hover,inside&&Enabled?1:0);press=Ease(press,down&&Enabled?1:0);Invalidate();if(hover==(inside&&Enabled?1:0)&&press==(down&&Enabled?1:0))animation.Stop();}
 void Start(){if(!AppTheme.Animate){hover=inside&&Enabled?1:0;press=down&&Enabled?1:0;Invalidate();return;}if(IsHandleCreated&&!IsDisposed)animation.Start();}
 protected override void OnMouseEnter(EventArgs e){inside=true;Start();base.OnMouseEnter(e);}
 protected override void OnMouseLeave(EventArgs e){inside=false;down=false;Start();base.OnMouseLeave(e);}
 protected override void OnMouseDown(MouseEventArgs e){if(e.Button==MouseButtons.Left){down=true;Start();}base.OnMouseDown(e);}
 protected override void OnMouseUp(MouseEventArgs e){down=false;Start();base.OnMouseUp(e);}
 protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space){down=true;Start();}base.OnKeyDown(e);}
 protected override void OnKeyUp(KeyEventArgs e){down=false;Start();base.OnKeyUp(e);}
 protected override void OnEnabledChanged(EventArgs e){down=false;inside=false;Start();base.OnEnabledChanged(e);}
 static Color Blend(Color a,Color b,float amount){return Color.FromArgb((int)(a.R+(b.R-a.R)*amount),(int)(a.G+(b.G-a.G)*amount),(int)(a.B+(b.B-a.B)*amount));}
 internal static GraphicsPath Shape(RectangleF rect,float radius){var path=new GraphicsPath();float d=radius*2;path.AddArc(rect.X,rect.Y,d,d,180,90);path.AddArc(rect.Right-d,rect.Y,d,d,270,90);path.AddArc(rect.Right-d,rect.Bottom-d,d,d,0,90);path.AddArc(rect.X,rect.Bottom-d,d,d,90,90);path.CloseFigure();return path;}
 protected override void OnPaint(PaintEventArgs e){
  e.Graphics.Clear(Parent==null?Color.White:Parent.BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
  float inset=press;var rect=new RectangleF(0.75f+inset,0.75f+inset,Width-1.5f-2*inset,Height-1.5f-2*inset);
  Color fill=Blend(BackColor,AppTheme.Selected,hover*0.5f+press*0.3f);if(!Enabled)fill=AppTheme.Surface;
  using(var shape=Shape(rect,10))using(var brush=new SolidBrush(fill)){e.Graphics.FillPath(brush,shape);if(FlatAppearance.BorderSize>0||hover>0)using(var pen=new Pen(Blend(FlatAppearance.BorderColor,AppTheme.Accent,hover),1+hover))e.Graphics.DrawPath(pen,shape);}
  var area=new Rectangle(Padding.Left+8,(int)press,Math.Max(1,Width-Padding.Horizontal-16),Height-2);
  TextFormatFlags flags=TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|(MultilineText?TextFormatFlags.WordBreak:TextFormatFlags.SingleLine);
  flags|=TextAlign==ContentAlignment.MiddleLeft?TextFormatFlags.Left:TextFormatFlags.HorizontalCenter;
  if(!ShowKeyboardCues)flags|=TextFormatFlags.HidePrefix;
  TextRenderer.DrawText(e.Graphics,Text,Font,area,Enabled?ForeColor:Color.Gray,flags);
  if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(5,5,Width-10,Height-10),ForeColor,fill);
 }
 protected override void Dispose(bool disposing){if(disposing)animation.Dispose();base.Dispose(disposing);}
}
class SmoothListView:ListView {protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);FitHeader();}protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);BeginInvoke((Action)FitHeader);}void FitHeader(){if(IsDisposed||Columns.Count==0||View!=View.Details)return;int available=ClientSize.Width;for(int i=0;i<Columns.Count-1;i++)available-=Columns[i].Width;if(available>Columns[Columns.Count-1].Width)Columns[Columns.Count-1].Width=available;}public SmoothListView(){OwnerDraw=true;DrawColumnHeader+=delegate(object sender,DrawListViewColumnHeaderEventArgs e){using(var brush=new SolidBrush(AppTheme.Surface))e.Graphics.FillRectangle(brush,e.Bounds);TextRenderer.DrawText(e.Graphics,e.Header.Text,Font,e.Bounds,AppTheme.Text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);};DrawItem+=delegate(object sender,DrawListViewItemEventArgs e){e.DrawDefault=!AppTheme.Dark;};DrawSubItem+=delegate(object sender,DrawListViewSubItemEventArgs e){if(!AppTheme.Dark){e.DrawDefault=true;return;}Color bg=e.Item.Selected?AppTheme.Selected:AppTheme.Surface;using(var brush=new SolidBrush(bg))e.Graphics.FillRectangle(brush,e.Bounds);Color fg=e.SubItem.ForeColor;bool blocked=e.Item.ForeColor==Color.Gray;if(blocked)fg=Color.Gray;else if(fg.GetBrightness()<0.55f)fg=AppTheme.Text;var bounds=e.Bounds;if(CheckBoxes&&e.ColumnIndex==0){ControlPaint.DrawCheckBox(e.Graphics,new Rectangle(bounds.Left+3,bounds.Top+6,13,13),blocked?ButtonState.Inactive:e.Item.Checked?ButtonState.Checked:ButtonState.Normal);bounds.X+=20;bounds.Width-=20;}TextRenderer.DrawText(e.Graphics,e.SubItem.Text,Font,bounds,fg,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);};DoubleBuffered=true;SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}}
}
