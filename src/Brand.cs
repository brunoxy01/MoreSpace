using System;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using System.Drawing;
namespace MoreSpace {
partial class MainWindow {
 WebBrowser cards;
 bool english;
 Timer slide=new Timer{Interval=15};
 TabPage moving;
 static string Asset(string name){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(name))using(var reader=new StreamReader(stream))return reader.ReadToEnd();}
 Control BuildCards(){
 cards=new WebBrowser{Dock=DockStyle.Fill,AllowWebBrowserDrop=false,IsWebBrowserContextMenuEnabled=false,WebBrowserShortcutsEnabled=false};
 var panel=new Panel{Dock=DockStyle.Fill};panel.Controls.Add(overview);panel.Controls.Add(cards);cards.Visible=false;
 cards.Navigating+=delegate(object sender,WebBrowserNavigatingEventArgs e){if(e.Url.Host=="drive.invalid"){e.Cancel=true;string root=e.Url.AbsolutePath.TrimStart('/')+@":\";if(drivePicker.Items.Contains(root)&&tabs.SelectedTab.Text=="Arquivos grandes")drivePicker.SelectedItem=root;}};cards.DocumentCompleted+=delegate{RenderCards();};overview.TextChanged+=delegate{RenderCards();};
 cards.DocumentText=Asset("cards.html").Replace("REACT_JS",Asset("react.js")).Replace("DOM_JS",Asset("react-dom.js"));return panel;}
 void RenderCards(){if(cards==null||cards.Document==null)return;try{cards.Document.InvokeScript("update",new object[]{overview.Text,AppTheme.Dark,AppTheme.Animate,english,drivePicker.SelectedItem as string});cards.Visible=true;cards.BringToFront();}catch(System.Runtime.InteropServices.COMException){cards.Visible=false;}}
 void SlidePage(){if(moving!=null)moving.Padding=Padding.Empty;slide.Stop();if(!AppTheme.Animate)return;moving=tabs.SelectedTab;moving.Padding=new Padding(16,0,0,0);slide.Tick-=SlideTick;slide.Tick+=SlideTick;slide.Start();}
 void SlideTick(object sender,EventArgs e){int x=Math.Max(0,moving.Padding.Left-2);moving.Padding=new Padding(x,0,0,0);if(x==0)slide.Stop();}
 void AddLanguagePicker(){preferences.Controls.Add(new Label{Text="Idioma / Language",AutoSize=true,Location=new Point(12,55)});var picker=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Location=new Point(12,77),Width=126,AccessibleName="Idioma / Language"};picker.Items.AddRange(new object[]{"Português (BR)","English"});string path=Path.ChangeExtension(ThemePath,"lang");english=!demoMode&&File.Exists(path)&&File.ReadAllText(path)=="en";picker.SelectedIndex=english?1:0;preferences.Controls.Add(picker);ApplyLanguage();picker.SelectedIndexChanged+=delegate{english=picker.SelectedIndex==1;ApplyLanguage();if(!demoMode){Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,english?"en":"pt-BR");}};Disposed+=delegate{slide.Dispose();};}
 void ApplyLanguage(){TranslateControls(this);if(themeChoice!=null){var choices=english?new[]{"System","Light","Dark"}:new[]{"Sistema","Claro","Escuro"};for(int i=0;i<choices.Length;i++)themeChoice.Items[i]=choices[i];}string[] labels={"Home","Cleanup","Large files","Processes","Startup","Applications","Duplicates","Recycle bin","Updates","Review","Caches","App data","External drives","Activity"};for(int i=0;i<navButtons.Count&&i<labels.Length;i++)navButtons[i].Text=english?labels[i]:tabs.TabPages[i].Text;RenderCards();}
}}
