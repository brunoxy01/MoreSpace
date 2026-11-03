using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
namespace MoreSpace {
static class AppTheme {
 public static bool Dark;
 public static Color Background {get{return Dark?Color.FromArgb(22,27,33):Color.White;}}
 public static Color Surface {get{return Dark?Color.FromArgb(30,37,44):Color.FromArgb(246,248,247);}}
 public static Color Text {get{return Dark?Color.FromArgb(229,237,240):Color.FromArgb(29,37,48);}}
 public static Color Accent {get{return Dark?Color.FromArgb(113,222,181):Color.FromArgb(33,105,86);}}
 public static Color Selected {get{return Dark?Color.FromArgb(38,73,64):Color.FromArgb(219,236,227);}}
 public static bool SystemDark(){try{using(var k=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))return k!=null&&Convert.ToInt32(k.GetValue("AppsUseLightTheme",1))==0;}catch{return false;}}
 [DllImport("user32.dll")] static extern bool SystemParametersInfo(uint action,uint param,out bool value,uint flags);
 public static bool Animate {get{bool enabled;return !SystemInformation.HighContrast&&SystemParametersInfo(0x1042,0,out enabled,0)&&enabled;}}
 [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd,int attr,ref int value,int size);
 public static void Apply(Control c){
  c.BackColor=c is Button||c is ListView||c is TextBoxBase||c is ComboBox?Surface:Background;c.ForeColor=Text;
  var button=c as RoundedButton;if(button!=null){button.ForeColor=Accent;button.FlatAppearance.BorderColor=Dark?Color.FromArgb(66,82,79):Color.FromArgb(220,230,224);}
  var combo=c as ComboBox;if(combo!=null&&combo.DrawMode!=DrawMode.OwnerDrawFixed){combo.DrawMode=DrawMode.OwnerDrawFixed;combo.DrawItem+=delegate(object sender,DrawItemEventArgs e){using(var brush=new SolidBrush(AppTheme.Surface))e.Graphics.FillRectangle(brush,e.Bounds);if(e.Index>=0)TextRenderer.DrawText(e.Graphics,combo.GetItemText(combo.Items[e.Index]),combo.Font,e.Bounds,AppTheme.Text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);e.DrawFocusRectangle();};}
  var link=c as LinkLabel;if(link!=null){link.LinkColor=Accent;link.ActiveLinkColor=Accent;link.VisitedLinkColor=Accent;}
  foreach(Control child in c.Controls)Apply(child);
  var form=c as Form;if(form!=null&&form.IsHandleCreated){try{int value=Dark?1:0;DwmSetWindowAttribute(form.Handle,20,ref value,4);}catch{}}
  c.Invalidate();
 }
}
partial class MainWindow {
 ComboBox themeChoice;LinkLabel support;Panel preferences;readonly Timer themeTimer=new Timer{Interval=1500};
 readonly HashSet<Form> themedForms=new HashSet<Form>();
 static string ThemePath {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MaisEspaco","appearance.txt");}}
 void SetupAppearance(){
  preferences=new Panel{Height=140,Dock=DockStyle.Bottom,Padding=new Padding(10)};
  var label=new Label{Text="Aparência",AutoSize=true,Location=new Point(12,4)};
  themeChoice=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Location=new Point(12,28),Width=126,Anchor=AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Top};
  themeChoice.Items.AddRange(new object[]{"Sistema","Claro","Escuro"});int choice=0;
  if(!demoMode)try{int.TryParse(File.ReadAllText(ThemePath),out choice);}catch{}
  themeChoice.SelectedIndex=Math.Max(0,Math.Min(2,choice));
  AddLanguagePicker();support=new LinkLabel{Text="suport us",AutoSize=true,Location=new Point(12,114)};support.LinkClicked+=delegate{OpenSettings("https://github.com/brunoxy01/MoreSpace");};
  preferences.Controls.AddRange(new Control[]{label,themeChoice,support});side.Controls.Add(preferences);preferences.BringToFront();
  themeChoice.SelectedIndexChanged+=delegate{ApplyAppearance();if(!demoMode)try{Directory.CreateDirectory(Path.GetDirectoryName(ThemePath));File.WriteAllText(ThemePath,themeChoice.SelectedIndex.ToString());}catch(Exception ex){status.Text="Não foi possível salvar a aparência: "+ex.Message;}};
  themeTimer.Tick+=delegate{bool dark=themeChoice.SelectedIndex==2||(themeChoice.SelectedIndex==0&&AppTheme.SystemDark());if(dark!=AppTheme.Dark)ApplyAppearance();};
  Application.Idle+=ThemeOpenForms;Disposed+=delegate{themeTimer.Dispose();Application.Idle-=ThemeOpenForms;themedForms.Clear();};
  ApplyAppearance();themeTimer.Start();AdaptLayout();
 }
 void ThemeOpenForms(object sender,EventArgs e){foreach(Form form in Application.OpenForms)if(!form.IsDisposed&&themedForms.Add(form)){if(form==this)ApplyAppearance();else AppTheme.Apply(form);form.Disposed+=delegate{themedForms.Remove(form);};}}
 void ApplyAppearance(){AppTheme.Dark=themeChoice.SelectedIndex==2||(themeChoice.SelectedIndex==0&&AppTheme.SystemDark());foreach(Form form in Application.OpenForms)AppTheme.Apply(form);AppTheme.Apply(this);side.BackColor=AppTheme.Surface;brand.ForeColor=AppTheme.Accent;brand.BackColor=AppTheme.Surface;navigation.BackColor=AppTheme.Surface;UpdateNavigation();RenderCards();}
 internal void PreviewTheme(int choice){themeChoice.SelectedIndex=choice;ApplyAppearance();}
 internal int CheckAppearance(){foreach(int choice in new[]{2,1,0}){PreviewTheme(choice);Application.DoEvents();bool dark=choice==2||(choice==0&&AppTheme.SystemDark());if(AppTheme.Dark!=dark||report.BackColor!=AppTheme.Surface||pie.BackColor!=AppTheme.Background)return 161;if(support.Text!="suport us"||navigation.Bottom>preferences.Top)return 162;using(var dialog=new ChartFileDialog(Path.GetTempPath(),new List<Entry>())){dialog.Show(this);Application.DoEvents();ThemeOpenForms(this,EventArgs.Empty);if(dialog.BackColor!=AppTheme.Background)return 163;dialog.Close();}}return 0;}
}
}
