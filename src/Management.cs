using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace MoreSpace {
class QuietTabs:TabControl {
 public QuietTabs(){Multiline=true;SizeMode=TabSizeMode.Fixed;ItemSize=new Size(1,1);Appearance=TabAppearance.FlatButtons;}
 protected override void WndProc(ref Message m){if(m.Msg==0x1328){m.Result=(IntPtr)1;return;}base.WndProc(ref m);}
}
partial class MainWindow {
 FlowLayoutPanel navigation; List<Button> navButtons=new List<Button>();
 ListView startup=FilesView(false),apps=FilesView(false); TextBox processSearch=new TextBox(),appSearch=new TextBox();
 Label startupSummary=new Label(),appSummary=new Label(); TextBox processDetail=new TextBox();
 ComboBox processOrder=new ComboBox(),appFilter=new ComboBox(); List<AppEntry> appEntries=new List<AppEntry>();
 bool demoMode,processRefreshing;
 Dictionary<int,Sample> samples=new Dictionary<int,Sample>(); bool loadingStartup; Button applyStartup,refreshStartup,loadApps,removeApp;
 class Sample {public DateTime Start,At;public double Total;}
 class ProcessRef {public int Id;public DateTime Start;public string Path;}
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool IsProcessCritical(IntPtr process,out bool critical);
 static bool Critical(Process p){try{bool critical;return !IsProcessCritical(p.Handle,out critical)||critical;}catch{return true;}}
 public void PreviewPage(int n){tabs.SelectedIndex=n;if(tabs.SelectedTab.Text=="Inicialização"&&!demoMode)LoadStartup();if(tabs.SelectedTab.Text=="Aplicativos"){appEntries=demoMode?DemoApps():AppManager.Read();RenderApps();}PopulateDemo();}
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,string l);
 TableLayoutPanel shell;Panel side;Label brand;Control workspace;
 void BuildShell(Control main){workspace=main;shell=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,BackColor=Color.White};shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,200));shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));Controls.Add(shell);side=new Panel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(246,248,247)};shell.Controls.Add(side,0,0);shell.Controls.Add(main,1,0);brand=new Label{Text="+Espaço",Font=new Font("Segoe UI",25,FontStyle.Bold),ForeColor=blue,AutoSize=true,Location=new Point(20,26)};side.Controls.Add(brand);navigation=new FlowLayoutPanel{FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Location=new Point(8,110),Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right};side.Controls.Add(navigation);Resize+=delegate{AdaptLayout();}; }
 void BuildNavigation(){foreach(TabPage page in tabs.TabPages){int index=tabs.TabPages.IndexOf(page);var b=new RoundedButton{Text=page.Text,Width=174,Height=40,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(14,0,0,0),FlatStyle=FlatStyle.Flat,Margin=new Padding(0,0,0,5)};b.FlatAppearance.BorderSize=0;b.Click+=delegate{tabs.SelectedIndex=index;};navigation.Controls.Add(b);navButtons.Add(b);}tabs.SelectedIndexChanged+=delegate{UpdateNavigation();SlidePage();if(!demoMode&&!busy&&!tempVisited&&tabs.SelectedTab.Text=="Limpeza"){tempVisited=true;var ignore=Scan(true);}EnsureLargeScan();if(!demoMode&&tabs.SelectedTab.Text=="Inicialização" && startup.Items.Count==0)LoadStartup();};UpdateNavigation();}
 void UpdateNavigation(){for(int i=0;i<navButtons.Count;i++){navButtons[i].BackColor=i==tabs.SelectedIndex?AppTheme.Selected:AppTheme.Surface;navButtons[i].ForeColor=i==tabs.SelectedIndex?AppTheme.Accent:AppTheme.Text;navButtons[i].Font=new Font("Segoe UI",10,i==tabs.SelectedIndex?FontStyle.Bold:FontStyle.Regular);}}
 void StyleLists(Control root){foreach(Control c in root.Controls){var l=c as ListView;if(l!=null){l.BorderStyle=BorderStyle.None;l.BackColor=Color.FromArgb(251,252,251);l.ForeColor=navy;l.HideSelection=false;l.ShowItemToolTips=true;var spacing=new ImageList{ImageSize=new Size(1,28)};l.SmallImageList=spacing;}StyleLists(c);}SendMessage(processSearch.Handle,0x1501,(IntPtr)1,"Buscar processo…");SendMessage(appSearch.Handle,0x1501,(IntPtr)1,"Buscar aplicativo…");}
 void SetupProcesses(TableLayoutPanel rp,FlowLayoutPanel bar){
  processSearch.Width=175;processSearch.Margin=new Padding(4,8,8,0);bar.Controls.Add(processSearch);processSearch.TextChanged+=delegate{RefreshProcesses();};new ToolTip().SetToolTip(processSearch,"Buscar nome, PID ou caminho");
  processOrder.DropDownStyle=ComboBoxStyle.DropDownList;processOrder.Width=110;processOrder.Items.AddRange(new object[]{"Mais RAM","Mais CPU","Nome"});processOrder.SelectedIndex=0;processOrder.Margin=new Padding(0,8,0,0);bar.Controls.Add(processOrder);processOrder.SelectedIndexChanged+=delegate{RefreshProcesses();};
  processes.Dock=DockStyle.Fill;processes.View=View.Details;processes.FullRowSelect=true;processes.MultiSelect=false;processes.Columns.Add("Processo",180);processes.Columns.Add("PID",65);processes.Columns.Add("CPU",75);processes.Columns.Add("RAM",110);processes.Columns.Add("Sessão / acesso",145);processes.Columns.Add("Executável",430);
  processes.SelectedIndexChanged+=delegate{if(processes.SelectedItems.Count>0)processDetail.Text=processes.SelectedItems[0].ToolTipText;};
  rp.Controls.Add(processes,0,2);processDetail.Dock=DockStyle.Fill;processDetail.ReadOnly=true;processDetail.BorderStyle=BorderStyle.None;processDetail.BackColor=Color.White;processDetail.Multiline=true;rp.Controls.Add(processDetail,0,3);
 }
 async void RefreshProcesses(){
  if(processRefreshing||demoMode||IsDisposed)return;processRefreshing=true;
  try{string processQuery=processSearch.Text.Trim();
  int selected=processes.SelectedItems.Count>0?((ProcessRef)processes.SelectedItems[0].Tag).Id:-1;var rows=new List<Tuple<long,double,ListViewItem>>();var next=new Dictionary<int,Sample>();DateTime now=DateTime.UtcNow;int mySession=Process.GetCurrentProcess().SessionId;
  await Task.Run(()=>{foreach(var p in Process.GetProcesses())using(p)try{
   int id=p.Id;string name=p.ProcessName,path="Acesso restrito",session="Restrito";DateTime start=DateTime.MinValue;long memory=0;double cpu=-1;
   try{memory=p.WorkingSet64;}catch{}try{start=p.StartTime;double total=p.TotalProcessorTime.TotalMilliseconds;Sample old;if(samples.TryGetValue(id,out old)&&old.Start==start&&(now-old.At).TotalMilliseconds>=500)cpu=Math.Max(0,Math.Min(100,(total-old.Total)/(now-old.At).TotalMilliseconds/Environment.ProcessorCount*100));next[id]=new Sample{Start=start,At=now,Total=total};}catch{}
   try{session=p.SessionId==mySession?"Sua sessão":"Sistema / outra sessão";}catch{}try{path=p.MainModule.FileName;}catch{}
   string filter=processQuery;if(filter.Length>0 && (name+" "+id+" "+path).IndexOf(filter,StringComparison.CurrentCultureIgnoreCase)<0)continue;
   var row=new ListViewItem(new[]{name,id.ToString(),cpu<0?"—":cpu.ToString("N1")+"%",Engine.Bytes(memory),session,path});row.Tag=new ProcessRef{Id=id,Start=start,Path=path};row.ToolTipText=name+" • PID "+id+"\r\n"+path;rows.Add(Tuple.Create(memory,cpu,row));
  }catch{} });
  if(IsDisposed||Disposing)return;
  samples=next;IEnumerable<Tuple<long,double,ListViewItem>> sorted=rows.OrderByDescending(x=>x.Item1);if(processOrder.SelectedIndex==1)sorted=rows.OrderByDescending(x=>x.Item2);if(processOrder.SelectedIndex==2)sorted=rows.OrderBy(x=>x.Item3.Text);
  int top=processes.TopItem==null?-1:((ProcessRef)processes.TopItem.Tag).Id;
  processes.BeginUpdate();try{processes.Items.Clear();foreach(var row in sorted){processes.Items.Add(row.Item3);if(((ProcessRef)row.Item3.Tag).Id==selected)row.Item3.Selected=true;}if(top>=0){var item=processes.Items.Cast<ListViewItem>().FirstOrDefault(i=>((ProcessRef)i.Tag).Id==top);if(item!=null)processes.TopItem=item;}}finally{processes.EndUpdate();}
  }catch(Exception ex){if(!IsDisposed)status.Text="Atualização de processos indisponível: "+ex.Message;}finally{processRefreshing=false;}
 }
 void SetupStartup(){
  var page=Page("Inicialização","Marcado = habilitado no próximo login. Desmarque para desativar e clique em Aplicar. Serviços, tarefas e inicialização de apps Store ficam nas configurações do Windows.");
  var bar=new FlowLayoutPanel{Dock=DockStyle.Fill};refreshStartup=Btn("Atualizar",delegate{if(StartupDirty() && MessageBox.Show(this,"Descartar as alterações ainda não aplicadas?","Inicialização",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;LoadStartup();});bar.Controls.Add(refreshStartup);applyStartup=Btn("Aplicar alterações",delegate{ApplyStartup();});bar.Controls.Add(applyStartup);bar.Controls.Add(Btn("Configurações do Windows",delegate{OpenSettings("ms-settings:startupapps");}));page.Controls.Add(bar,0,1);
  startup.CheckBoxes=true;startup.Columns.Clear();startup.Columns.Add("Programa",230);startup.Columns.Add("Estado",200);startup.Columns.Add("Origem",180);startup.Columns.Add("Comando / caminho",600);page.Controls.Add(startup,0,2);startupSummary.Dock=DockStyle.Fill;page.Controls.Add(startupSummary,0,3);
  startup.ItemCheck+=delegate(object sender,ItemCheckEventArgs e){if(loadingStartup)return;var entry=(StartupEntry)startup.Items[e.Index].Tag;if(!entry.Known || busy){e.NewValue=e.CurrentValue;return;}BeginInvoke((Action)UpdateStartupSummary);};
 }
 bool StartupDirty(){return startup.Items.Cast<ListViewItem>().Any(i=>i.Checked!=((StartupEntry)i.Tag).Enabled);}
 void UpdateStartupSummary(){int count=startup.Items.Cast<ListViewItem>().Count(i=>i.Checked!=((StartupEntry)i.Tag).Enabled);startupSummary.Text=startup.Items.Count+" entradas • "+count+" alterações pendentes. Itens de todos os usuários podem exigir administrador.";}
 void LoadStartup(){loadingStartup=true;startup.BeginUpdate();try{startup.Items.Clear();var errors=new List<string>();foreach(var e in StartupManager.Read(errors)){var row=new ListViewItem(new[]{e.Name,e.Known?(e.Enabled?"Habilitado":"Desabilitado"):"Gerenciar no Windows",e.Scope+" • "+e.Group,e.Command});row.Tag=e;row.Checked=e.Enabled;row.ToolTipText=e.Command;if(!e.Known)row.ForeColor=Color.Gray;startup.Items.Add(row);}foreach(var err in errors)Log(err);UpdateStartupSummary();if(errors.Count>0)startupSummary.Text+=" "+errors.Count+" falhas de acesso; consulte Atividade.";}catch(Exception ex){Log(ex.Message);startupSummary.Text=ex.Message;}finally{startup.EndUpdate();loadingStartup=false;}}
 void ApplyStartup(){if(busy)return;var changes=startup.Items.Cast<ListViewItem>().Where(i=>i.Checked!=((StartupEntry)i.Tag).Enabled).ToList();if(changes.Count==0)return;
  string summary=String.Join("\r\n",changes.Select(i=>(i.Checked?"Habilitar: ":"Desabilitar: ")+i.Text));if(MessageBox.Show(this,summary+"\r\n\r\nAplicar para os próximos logins? Não encerra os programas abertos.","Revisar inicialização",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
  int ok=0;foreach(var row in changes)try{StartupManager.Apply((StartupEntry)row.Tag,row.Checked);ok++;Log("INICIALIZAÇÃO "+(row.Checked?"HABILITADA ":"DESABILITADA ")+row.Text);}catch(Exception ex){Log("FALHA INICIALIZAÇÃO "+row.Text+": "+ex.Message);}LoadStartup();MessageBox.Show(this,ok+" de "+changes.Count+" alterações aplicadas e verificadas.\r\n"+(ok<changes.Count?"Consulte Atividade. Para entradas protegidas, use Configurações do Windows.":"Você pode reverter marcando ou desmarcando novamente."),"Inicialização");
 }
 void OpenSettings(string uri){try{Process.Start(new ProcessStartInfo(uri){UseShellExecute=true});}catch(Exception ex){MessageBox.Show(this,ex.Message);}}
}
class StartupEntry {public string Name,Command,Group,Scope,ApprovalPath,SourcePath;public bool Machine,Enabled,Known,Folder;public byte[] Original;}
static class StartupManager {
 const string Approval=@"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\";
 public static bool Known(byte[] bytes){return bytes==null || (bytes.Length==12 && (bytes[0]==2||bytes[0]==3||bytes[0]==6||bytes[0]==7) && bytes[1]==0 && bytes[2]==0 && bytes[3]==0);}
 public static bool Enabled(byte[] bytes){return bytes==null || bytes[0]==2||bytes[0]==6;}
 public static byte[] State(bool enabled){var b=new byte[12];b[0]=(byte)(enabled?2:3);if(!enabled)Array.Copy(BitConverter.GetBytes(DateTime.UtcNow.ToFileTimeUtc()),0,b,4,8);return b;}
 static bool Same(byte[] a,byte[] b){return a==null?b==null:b!=null&&a.SequenceEqual(b);}
 static RegistryKey Hive(bool machine){return RegistryKey.OpenBaseKey(machine?RegistryHive.LocalMachine:RegistryHive.CurrentUser,RegistryView.Registry64);}
 public static List<StartupEntry> Read(List<string> errors){var list=new List<StartupEntry>();foreach(bool machine in new[]{false,true})using(var hive=Hive(machine)){
  foreach(string group in new[]{"Run","Run32","StartupFolder"})try{
   string source=group=="Run32"?@"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run":@"Software\Microsoft\Windows\CurrentVersion\Run";
   using(var approval=hive.OpenSubKey(Approval+group)){
    if(group=="StartupFolder"){string dir=Environment.GetFolderPath(machine?Environment.SpecialFolder.CommonStartup:Environment.SpecialFolder.Startup);if(Directory.Exists(dir))foreach(string f in Directory.GetFiles(dir)){if((File.GetAttributes(f)&(FileAttributes.Hidden|FileAttributes.System))!=0)continue;Add(list,hive,approval,machine,group,f,System.IO.Path.GetFileName(f),f,true);}}
    else using(var key=hive.OpenSubKey(source)){if(key!=null)foreach(string name in key.GetValueNames()){object value=key.GetValue(name,null,RegistryValueOptions.DoNotExpandEnvironmentNames);if(value is string)Add(list,hive,approval,machine,group,source,name,(string)value,false);}}
   }
  }catch(Exception ex){errors.Add("INICIALIZAÇÃO "+group+": "+ex.Message);}
 }return list.OrderBy(x=>x.Name).ToList();}
 static void Add(List<StartupEntry> list,RegistryKey hive,RegistryKey approval,bool machine,string group,string source,string name,string command,bool folder){object raw=approval==null?null:approval.GetValue(name);var bytes=raw as byte[];bool known=(raw==null||bytes!=null)&&Known(bytes);list.Add(new StartupEntry{Name=name,Command=command,Group=group,Scope=machine?"Todos os usuários":"Sua conta",Machine=machine,Folder=folder,SourcePath=source,ApprovalPath=Approval+group,Original=bytes,Known=known,Enabled=known&&Enabled(bytes)});}
 public static void Apply(StartupEntry e,bool enabled){if(!e.Known)throw new Exception("Estado não reconhecido; use o Windows.");using(var hive=Hive(e.Machine)){
  if(e.Folder){if(!File.Exists(e.SourcePath))throw new Exception("Entrada removida; atualize a lista.");}else using(var source=hive.OpenSubKey(e.SourcePath)){if(source==null||!Object.Equals(source.GetValue(e.Name,null,RegistryValueOptions.DoNotExpandEnvironmentNames),e.Command))throw new Exception("Entrada alterada; atualize a lista.");}
  using(var key=hive.CreateSubKey(e.ApprovalPath)){var raw=key.GetValue(e.Name);if((raw!=null&&!(raw is byte[]))||!Same(raw as byte[],e.Original))throw new Exception("O Windows alterou esse estado. Atualize a lista.");
   string dir=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MaisEspaco","Backups");Directory.CreateDirectory(dir);File.WriteAllText(System.IO.Path.Combine(dir,DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff")+"-"+Guid.NewGuid().ToString("N")+".json"),new JavaScriptSerializer().Serialize(new{Machine=e.Machine,Key=e.ApprovalPath,Name=e.Name,Previous=e.Original==null?null:Convert.ToBase64String(e.Original),Enabled=enabled}),Encoding.UTF8);
   byte[] state=State(enabled);key.SetValue(e.Name,state,RegistryValueKind.Binary);key.Flush();if(!Same(key.GetValue(e.Name) as byte[],state))throw new Exception("O estado gravado não foi confirmado.");
  }
 }}
}
class AppEntry {public string Title{get;set;}public string Name{get;set;}public string FullName{get;set;}public string Publisher{get;set;}public string Version{get;set;}public bool Protected{get;set;}}
static class AppManager {
 const string Guard="$p.IsFramework -or $p.IsResourcePackage -or $p.NonRemovable -or $null -eq $p.NonRemovable -or $p.Name -match '(?i)(WindowsStore|StorePurchaseApp|DesktopAppInstaller|SecHealthUI|ShellExperienceHost|StartMenuExperienceHost|AAD.BrokerPlugin|AccountsControl|CloudExperienceHost|Client.CBS|Client.Core|WindowsAppRuntime|VCLibs|NET.Native|UI.Xaml|Win32WebViewHost|Windows.Search)'";
 public static string Quote(string s){return "'"+s.Replace("'","''")+"'";}
 public static string Run(string script){var start=new ProcessStartInfo(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),@"WindowsPowerShell\v1.0\powershell.exe"),"-NoLogo -NoProfile -NonInteractive -EncodedCommand "+Convert.ToBase64String(Encoding.Unicode.GetBytes("$ErrorActionPreference='Stop'; [Console]::OutputEncoding=[System.Text.Encoding]::UTF8; "+script))){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};using(var p=Process.Start(start)){var output=p.StandardOutput.ReadToEndAsync();var error=p.StandardError.ReadToEndAsync();p.WaitForExit();Task.WaitAll(output,error);if(p.ExitCode!=0)throw new Exception(error.Result.Length>0?error.Result:"O Windows recusou a operação.");return output.Result.Trim('\uFEFF','\r','\n',' ');}}
 public static List<AppEntry> Read(){string json=Run("$names=@{}; try {Get-StartApps | ForEach-Object {if($_.AppID -like '*!*'){$family=$_.AppID.Split('!')[0]; if(-not $names.ContainsKey($family)){$names[$family]=$_.Name}}}} catch {}; $result=@(Get-AppxPackage | ForEach-Object {$p=$_; $title=$names[$p.PackageFamilyName]; if(-not $title){$title=$p.Name}; [pscustomobject]@{Title=$title;Name=$p.Name;FullName=$p.PackageFullName;Publisher=$p.Publisher;Version=$p.Version.ToString();Protected=[bool]("+Guard+")}}); ConvertTo-Json -InputObject $result -Depth 3 -Compress");return new JavaScriptSerializer{MaxJsonLength=16*1024*1024}.Deserialize<List<AppEntry>>(json)??new List<AppEntry>();}
 public static void Remove(AppEntry app){string exact=Quote(app.FullName);Run("$p=Get-AppxPackage | Where-Object {$_.PackageFullName -eq "+exact+"}; if(@($p).Count -ne 1){throw 'O pacote mudou ou já foi removido. Atualize a lista.'}; if("+Guard+"){throw 'Este componente é protegido.'}; Remove-AppxPackage -Package $p.PackageFullName -ErrorAction Stop; if(Get-AppxPackage | Where-Object {$_.PackageFullName -eq "+exact+"}){throw 'O pacote ainda aparece instalado.'}; 'OK'");}
}
}
