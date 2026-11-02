using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MoreSpace {
/// <summary>Selections survive filtering, but never allow protected packages.</summary>
class AppSelection {
 readonly HashSet<string> selected=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 public int Count{get{return selected.Count;}}
 public bool Contains(AppEntry app){return selected.Contains(app.FullName);}
 public void Set(AppEntry app,bool value){if(value&&!app.Protected)selected.Add(app.FullName);else selected.Remove(app.FullName);}
 public void Clear(){selected.Clear();}
 public void Reconcile(IEnumerable<AppEntry> apps){selected.IntersectWith(apps.Where(a=>!a.Protected).Select(a=>a.FullName));}
 public List<AppEntry> Resolve(IEnumerable<AppEntry> apps){return apps.Where(a=>!a.Protected&&Contains(a)).ToList();}
}
partial class MainWindow {
 readonly AppSelection chosenApps=new AppSelection();
 CheckBox selectVisible=new CheckBox(); Button clearApps,stopApps;
 bool renderingApps,removingApps,stopRequested;
 IEnumerable<AppEntry> VisibleApps(){return appEntries.Where(a=>(a.Title+" "+a.Name+" "+a.Publisher).IndexOf(appSearch.Text.Trim(),StringComparison.CurrentCultureIgnoreCase)>=0 && (appFilter.SelectedIndex!=1||(a.Publisher??"").IndexOf("Microsoft Corporation",StringComparison.OrdinalIgnoreCase)>=0) && (appFilter.SelectedIndex!=2||!a.Protected)).OrderBy(a=>a.Protected).ThenBy(a=>a.Title);}
 void SetupApps(){
  var page=Page("Aplicativos","Marque os aplicativos que deseja remover. Você revisa a lista antes de confirmar. Componentes protegidos não podem ser selecionados.");
  page.RowStyles[1].Height=96;
  var toolbar=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,ColumnCount=1,Margin=Padding.Empty};toolbar.RowStyles.Add(new RowStyle(SizeType.Absolute,46));toolbar.RowStyles.Add(new RowStyle(SizeType.Absolute,46));
  var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Margin=Padding.Empty};
  selectVisible.Text="Selecionar visíveis";selectVisible.AutoSize=true;selectVisible.Margin=new Padding(4,12,18,0);selectVisible.AccessibleName="Selecionar todos os aplicativos removíveis visíveis";actions.Controls.Add(selectVisible);
  loadApps=Btn("Carregar aplicativos",async delegate{await LoadApps();});removeApp=Btn("Desinstalar selecionados…",async delegate{await RemoveApps();});
  actions.Controls.Add(loadApps);actions.Controls.Add(removeApp);actions.Controls.Add(Btn("Programas tradicionais",delegate{OpenSettings("ms-settings:appsfeatures");}));
  var filters=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Margin=Padding.Empty};
  appSearch.Width=230;appSearch.Margin=new Padding(0,8,10,0);appSearch.AccessibleName="Buscar aplicativo";filters.Controls.Add(appSearch);appSearch.TextChanged+=delegate{RenderApps();};
  appFilter.DropDownStyle=ComboBoxStyle.DropDownList;appFilter.Width=155;appFilter.Margin=new Padding(0,8,10,0);appFilter.Items.AddRange(new object[]{"Todos os apps","Apps Microsoft","Removíveis"});appFilter.SelectedIndex=0;appFilter.AccessibleName="Filtrar aplicativos";appFilter.SelectedIndexChanged+=delegate{RenderApps();};filters.Controls.Add(appFilter);
  clearApps=Btn("Limpar seleção",delegate{chosenApps.Clear();RenderApps();});filters.Controls.Add(clearApps);
  stopApps=Btn("Parar após o atual",delegate{stopRequested=true;stopApps.Enabled=false;status.Text="A remoção atual será concluída; os próximos apps serão preservados.";});stopApps.Visible=false;filters.Controls.Add(stopApps);
  toolbar.Controls.Add(actions,0,0);toolbar.Controls.Add(filters,0,1);page.Controls.Add(toolbar,0,1);
  apps.CheckBoxes=true;apps.MultiSelect=true;apps.Columns.Clear();apps.Columns.Add("Aplicativo",275);apps.Columns.Add("Remoção",160);apps.Columns.Add("Versão",120);apps.Columns.Add("Publicador",440);page.Controls.Add(apps,0,2);appSummary.Dock=DockStyle.Fill;page.Controls.Add(appSummary,0,3);
  selectVisible.CheckStateChanged+=delegate{if(renderingApps||removingApps)return;bool check=selectVisible.CheckState==CheckState.Checked;foreach(var app in VisibleApps())chosenApps.Set(app,check);RenderApps();};
  apps.ItemCheck+=delegate(object sender,ItemCheckEventArgs e){if(renderingApps)return;var app=(AppEntry)apps.Items[e.Index].Tag;if(removingApps||app.Protected){e.NewValue=e.CurrentValue;return;}chosenApps.Set(app,e.NewValue==CheckState.Checked);BeginInvoke((Action)UpdateAppSelection);};
  UpdateAppSelection();
 }
 void UpdateAppSelection(){
  var visible=VisibleApps().Where(a=>!a.Protected).ToList();int visibleChosen=visible.Count(a=>chosenApps.Contains(a));
  renderingApps=true;try{selectVisible.CheckState=visibleChosen==0?CheckState.Unchecked:visibleChosen==visible.Count?CheckState.Checked:CheckState.Indeterminate;}finally{renderingApps=false;}
  selectVisible.Enabled=!removingApps&&visible.Count>0;removeApp.Enabled=!busy&&chosenApps.Count>0;clearApps.Enabled=!removingApps&&chosenApps.Count>0;
  appSummary.Text=apps.Items.Count+" exibidos • "+chosenApps.Count+" selecionados"+(chosenApps.Count>visibleChosen?" ("+(chosenApps.Count-visibleChosen)+" fora do filtro)":"")+" • Passe o mouse para ver o pacote exato.";
 }
 async Task LoadApps(){if(busy)return;Busy(true);cancel.Enabled=false;loadApps.Enabled=removeApp.Enabled=false;status.Text="Consultando aplicativos instalados…";try{appEntries=await Task.Run(()=>AppManager.Read());chosenApps.Reconcile(appEntries);RenderApps();status.Text=appEntries.Count+" pacotes encontrados. Marque os aplicativos que deseja revisar.";}catch(Exception ex){Log(ex.Message);MessageBox.Show(this,ex.Message,"Não foi possível carregar aplicativos");}finally{Busy(false);loadApps.Enabled=true;UpdateAppSelection();}}
 void RenderApps(){if(apps.Columns.Count==0)return;renderingApps=true;apps.BeginUpdate();try{apps.Items.Clear();foreach(var app in VisibleApps()){var row=new ListViewItem(new[]{app.Title??app.Name,app.Protected?"Protegido":"Pode ser removido",app.Version,app.Publisher});row.Tag=app;row.ToolTipText=app.FullName;row.Checked=chosenApps.Contains(app);if(app.Protected)row.ForeColor=Color.Gray;apps.Items.Add(row);}}finally{apps.EndUpdate();renderingApps=false;}UpdateAppSelection();}
 async Task RemoveApps(){
  if(busy)return;var selected=chosenApps.Resolve(appEntries);if(selected.Count==0)return;
  using(var confirm=new AppRemovalDialog(selected)){if(confirm.ShowDialog(this)!=DialogResult.OK)return;}
  Busy(true);cancel.Enabled=false;removingApps=true;stopRequested=false;loadApps.Enabled=removeApp.Enabled=selectVisible.Enabled=clearApps.Enabled=apps.Enabled=appSearch.Enabled=appFilter.Enabled=false;stopApps.Visible=stopApps.Enabled=true;
  var before=SavingsStore.Snapshot();int removed=0,failed=0,attempted=0;
  try{
   foreach(var app in selected){if(stopRequested)break;attempted++;status.Text="Removendo "+attempted+" de "+selected.Count+": "+(app.Title??app.Name)+"…";
    try{await Task.Run(()=>AppManager.Remove(app));Log("APP REMOVIDO E VERIFICADO "+app.FullName);appEntries.Remove(app);chosenApps.Set(app,false);removed++;}
    catch(Exception ex){failed++;Log("FALHA REMOÇÃO "+app.FullName+": "+ex.Message);}
   }
   if(attempted>0)RecordSavings("Aplicativos",removed,null,before);
   RenderApps();RefreshOverview();string result=removed+" removidos • "+failed+" falhas • "+(selected.Count-attempted)+" não iniciados.";Log(result);status.Text=result;
   MessageBox.Show(this,result+"\r\n\r\nFalhas e itens não iniciados permanecem selecionados. Consulte Atividade para detalhes.","Resultado da desinstalação",MessageBoxButtons.OK,failed>0?MessageBoxIcon.Warning:MessageBoxIcon.Information);
  }finally{removingApps=false;Busy(false);loadApps.Enabled=apps.Enabled=appSearch.Enabled=appFilter.Enabled=true;stopApps.Visible=false;UpdateAppSelection();}
 }
 static List<AppEntry> DemoApps(){return new List<AppEntry>{
  new AppEntry{Title="Calculadora",Name="Microsoft.WindowsCalculator",FullName="Microsoft.WindowsCalculator_demo",Publisher="Microsoft Corporation",Version="11.0"},
  new AppEntry{Title="Clipchamp",Name="Clipchamp.Clipchamp",FullName="Clipchamp_demo",Publisher="Microsoft Corporation",Version="4.0"},
  new AppEntry{Title="Clima",Name="Microsoft.BingWeather",FullName="Microsoft.BingWeather_demo",Publisher="Microsoft Corporation",Version="4.0"},
  new AppEntry{Title="Fotos",Name="Microsoft.Windows.Photos",FullName="Microsoft.Windows.Photos_demo",Publisher="Microsoft Corporation",Version="2026.1"},
  new AppEntry{Title="Microsoft Store",Name="Microsoft.WindowsStore",FullName="Microsoft.WindowsStore_demo",Publisher="Microsoft Corporation",Version="22601.1",Protected=true},
  new AppEntry{Title="Segurança do Windows",Name="Microsoft.SecHealthUI",FullName="Microsoft.SecHealthUI_demo",Publisher="Microsoft Corporation",Version="1000.1",Protected=true}
 };}
}
class AppRemovalDialog:Form {
 public AppRemovalDialog(List<AppEntry> selected){Text="Revisar desinstalação";Size=new Size(760,520);MinimumSize=new Size(640,420);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);BackColor=Color.White;MinimizeBox=MaximizeBox=false;
  var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(22),RowCount=4,ColumnCount=1};layout.RowStyles.Add(new RowStyle(SizeType.Absolute,42));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,68));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,46));Controls.Add(layout);
  layout.Controls.Add(new Label{Text="Remover "+selected.Count+" aplicativo(s) da sua conta?",Font=new Font("Segoe UI",15,FontStyle.Bold),Dock=DockStyle.Fill},0,0);
  var list=new SmoothListView{Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true};list.Columns.Add("Aplicativo",210);list.Columns.Add("Pacote exato",540);foreach(var app in selected)list.Items.Add(new ListViewItem(new[]{app.Title??app.Name,app.FullName}));layout.Controls.Add(list,0,1);
  layout.Controls.Add(new Label{Text="A lista inclui seleções fora do filtro atual. Dados locais dos apps podem ser perdidos. Reinstalar depende da disponibilidade na Store ou do instalador original.",Dock=DockStyle.Fill,Padding=new Padding(0,12,0,0)},0,2);
  var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};var no=new RoundedButton{Text="Cancelar",Width=115,Height=38,DialogResult=DialogResult.Cancel,BackColor=Color.FromArgb(240,244,242)};var yes=new RoundedButton{Text="Confirmar remoção",Width=185,Height=38,DialogResult=DialogResult.OK,BackColor=Color.FromArgb(33,105,86),ForeColor=Color.White};actions.Controls.Add(no);actions.Controls.Add(yes);layout.Controls.Add(actions,0,3);CancelButton=no;AcceptButton=no;Shown+=delegate{no.Focus();};
 }
}
}
