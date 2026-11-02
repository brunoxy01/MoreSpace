using System;
using System.Linq;
using System.Windows.Forms;

namespace MoreSpace {
static class UiChecks {
 internal static bool ContainsText(Control root,string text){return root.Text==text||root.Controls.Cast<Control>().Any(c=>ContainsText(c,text));}
 public static int Run(){try{using(var files=new ChartFileDialog(System.IO.Path.GetTempPath(),new System.Collections.Generic.List<Entry>())){files.Show();Application.DoEvents();files.Close();}using(var form=new MainWindow(true)){form.Show();Application.DoEvents();int result=form.CheckSelectionUi();if(result==0)result=form.CheckChartMenu();form.Close();if(!form.ChartMenuDisposed)return 115;return result;}}catch{return 90;}}
}
partial class MainWindow {
 public int CheckSelectionUi(){
  int home=CheckHomeUi();if(home!=0)return home; PreviewPage(5);Application.DoEvents();
  if(UiChecks.ContainsText(this,"CUIDE DO SEU WINDOWS"))return 30;
  if(!apps.CheckBoxes||apps.Items.Count!=6)return 31;
  var protectedRow=apps.Items.Cast<ListViewItem>().First(i=>((AppEntry)i.Tag).Protected);protectedRow.Checked=true;Application.DoEvents();if(protectedRow.Checked||chosenApps.Count!=0)return 32;
  selectVisible.Checked=true;Application.DoEvents();if(chosenApps.Count!=4||apps.CheckedItems.Count!=4)return 33;
  appSearch.Text="Clima";Application.DoEvents();if(apps.Items.Count!=1||chosenApps.Count!=4||!appSummary.Text.Contains("3 fora do filtro"))return 34;
  selectVisible.Checked=false;Application.DoEvents();if(chosenApps.Count!=3||apps.CheckedItems.Count!=0)return 35;
  appSearch.Clear();Application.DoEvents();if(apps.CheckedItems.Count!=3)return 36;
  clearApps.PerformClick();Application.DoEvents();if(chosenApps.Count!=0||removeApp.Enabled)return 37;
  Size=MinimumSize;Application.DoEvents();if(removeApp.Right>removeApp.Parent.ClientSize.Width)return 40;
  var selector=new AppSelection();var data=DemoApps();selector.Set(data[0],true);selector.Set(data[4],true);if(selector.Count!=1)return 38;selector.Reconcile(data.Skip(1));if(selector.Count!=0)return 39;
  foreach(TabPage page in tabs.TabPages){tabs.SelectedTab=page;Application.DoEvents();}
  return 0;
 }
}
}
