using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MoreSpace {
static class Program {
 [STAThread] static void Main(string[] args) {
  AppContext.SetSwitch("Switch.System.IO.UseLegacyPathHandling",false);
  AppContext.SetSwitch("Switch.System.IO.BlockLongPaths",false);
  if(args.Contains("--features-test")) { Environment.Exit(FeatureChecks.Run()); return; }
  if(args.Contains("--long-path-test")) { Environment.Exit(LongPathChecks.Run()); return; }
  if(args.Contains("--large-test")) { Environment.Exit(LargeChecks.Run()); return; }
  if(args.Contains("--chart-file-test")) { Environment.Exit(ChartFileChecks.Run()); return; }
  if(args.Contains("--performance-test")) { Environment.Exit(PerformanceChecks.Run()); return; }
  if(args.Contains("--space-test")) { Environment.Exit(SpaceChecks.Run()); return; }
  if(args.Contains("--self-test")) { Environment.Exit(Checks.Run()); return; }
  if(args.Contains("--management-test")) { Environment.Exit(ManagementChecks.Run()); return; }
  Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
  if(args.Contains("--ui-test")) { Environment.Exit(UiChecks.Run()); return; }
  if(args.Length>=2 && (args[0]=="--preview" || args[0]=="--preview-demo")) {using(var form=new MainWindow(args[0]=="--preview-demo")){form.Show();if(args.Length>2)form.PreviewPage(Int32.Parse(args[2]));if(args.Length>4)form.Size=new Size(Int32.Parse(args[3]),Int32.Parse(args[4]));if(args.Length>5)form.PreviewTheme(Int32.Parse(args[5]));Application.DoEvents();using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(0,0,form.Width,form.Height));bitmap.Save(args[1]);}form.Close();}return;}
  Application.Run(new MainWindow());
 }
}
class Entry { public string Path; public long Size; public DateTime Modified; public string Category; }
static class Engine {
 public static string[] Roots() { return new[]{System.IO.Path.GetTempPath(),System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Temp")}.Select(x=>System.IO.Path.GetFullPath(x).TrimEnd('\\')).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(); }
 public static bool Under(string path,string root) { return System.IO.Path.GetFullPath(path).StartsWith(System.IO.Path.GetFullPath(root).TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase); }
 public static bool NoLinks(string path) {
  string current=System.IO.Path.GetFullPath(path);
  while(!String.IsNullOrEmpty(current)) { if((File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0) return false; current=System.IO.Path.GetDirectoryName(current); } return true;
 }
 public static bool Allowed(Entry e) {
  if(!Roots().Any(r=>Under(e.Path,r)) || !NoLinks(e.Path)) return false;
  var f=new FileInfo(e.Path); return f.Exists && f.Length==e.Size && f.LastWriteTimeUtc==e.Modified && f.LastWriteTimeUtc<DateTime.UtcNow.AddDays(-7) && f.LastAccessTimeUtc<DateTime.UtcNow.AddDays(-7);
 }
 public static List<Entry> Scan(string[] roots,bool temp,CancellationToken token,Action<int,string> progress,List<string> errors,bool showRecent=false) {
  var result=new List<Entry>(); var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase); int count=0;
  foreach(string root in roots) {
   if(!NoLinks(root))continue;var pending=new Stack<string>(); pending.Push(root);
   while(pending.Count>0) {
    token.ThrowIfCancellationRequested(); string dir=pending.Pop();
    try {
     if((File.GetAttributes(dir)&FileAttributes.ReparsePoint)!=0 || !seen.Add(System.IO.Path.GetFullPath(dir))) continue;
     foreach(var info in new DirectoryInfo(dir).EnumerateFileSystemInfos()) {
      string path=info.FullName;
      token.ThrowIfCancellationRequested();
      try { if((info.Attributes&FileAttributes.ReparsePoint)!=0)continue;if((info.Attributes&FileAttributes.Directory)!=0){pending.Push(path);continue;}var f=(FileInfo)info;
       count++; if(count%250==0) progress(count,dir);
       bool recent=temp&&(f.LastWriteTimeUtc>=DateTime.UtcNow.AddDays(-7)||f.LastAccessTimeUtc>=DateTime.UtcNow.AddDays(-7));if(temp ? showRecent || f.LastWriteTimeUtc<DateTime.UtcNow.AddDays(-7) && f.LastAccessTimeUtc<DateTime.UtcNow.AddDays(-7) : f.Length>=100L*1024*1024)
        result.Add(new Entry{Path=path,Size=f.Length,Modified=f.LastWriteTimeUtc,Category=recent?"Recente — protegido":temp?"Temporário antigo — revisar":"Arquivo grande — revisar"});
      } catch(Exception ex) { if(!(ex is IOException || ex is UnauthorizedAccessException)) throw; errors.Add(path+": "+ex.Message); }
     }
    } catch(Exception ex) { if(!(ex is IOException || ex is UnauthorizedAccessException)) throw; errors.Add(dir+": "+ex.Message); }
   }
  } return result.OrderByDescending(x=>x.Size).ToList();
 }
 public static string Bytes(long n) { string[] units={"B","KB","MB","GB","TB"}; double d=n; int i=0; while(d>=1024 && i<4){d/=1024;i++;} return d.ToString("N1")+" "+units[i]; }
}
partial class MainWindow:Form {
 [StructLayout(LayoutKind.Sequential)] class MEMORY { public uint length=(uint)Marshal.SizeOf(typeof(MEMORY)); public uint load; public ulong total,available,pageTotal,pageAvailable,virtualTotal,virtualAvailable,extended; }
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool GlobalMemoryStatusEx([In,Out] MEMORY m);
 readonly Color navy=Color.FromArgb(29,37,48),blue=Color.FromArgb(33,105,86);
 Label overview=new Label(),status=new Label(),selection=new Label(),ramInfo=new Label();
 ListView temps=FilesView(true),large=FilesView(false),processes=new SmoothListView();
 TextBox report=new TextBox(); TabControl tabs=new QuietTabs();
 Button scanTemp,scanLarge,clean,cancel; CancellationTokenSource cts; bool busy,tempVisited; string reportPath;
 List<string> logs=new List<string>(); System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();
 public MainWindow(bool demo=false) {
  demoMode=demo;
  Text="+Espaço 3.3.0"; DoubleBuffered=true; Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath); Size=new Size(1360,860); MinimumSize=new Size(820,650); StartPosition=FormStartPosition.CenterScreen;
  Font=new Font("Segoe UI",10); ForeColor=navy; BackColor=Color.White;
  var layout=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=4,ColumnCount=1,Padding=new Padding(24,16,24,12)};
  layout.RowStyles.Add(new RowStyle(SizeType.Absolute,70));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,90));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,42)); BuildShell(layout);
  var header=new Panel{Dock=DockStyle.Fill,BackColor=Color.White}; header.Controls.Add(new Label{Text="Seu PC, mais leve.",ForeColor=navy,Font=new Font("Segoe UI",23,FontStyle.Bold),AutoSize=true,Location=new Point(0,0)});
  header.Controls.Add(new Label{Text="Espaço, desempenho e controle em um só lugar.",ForeColor=Color.DimGray,AutoSize=true,Location=new Point(3,44)});layout.Controls.Add(header,0,0);
  overview.Dock=DockStyle.Fill;overview.Padding=new Padding(5,14,0,0);SetupSavings(layout);
  tabs.Dock=DockStyle.Fill;layout.Controls.Add(tabs,0,2);status.Dock=DockStyle.Fill;status.TextAlign=ContentAlignment.MiddleLeft;status.AutoEllipsis=true;layout.Controls.Add(status,0,3);
  SetupHome();
  var tp=Page("Limpeza","Temporários recentes aparecem em cinza para consulta. Só é possível selecionar arquivos sem uso há mais de 7 dias.");
  var bar=new FlowLayoutPanel{Dock=DockStyle.Fill}; scanTemp=Btn("Analisar temporários",async delegate{await Scan(true);}); bar.Controls.Add(scanTemp);
  bar.Controls.Add(Btn("Selecionar todos",delegate{foreach(ListViewItem i in temps.Items)i.Checked=true;}));bar.Controls.Add(Btn("Desmarcar",delegate{foreach(ListViewItem i in temps.Items)i.Checked=false;}));
  clean=Btn("Excluir selecionados",async delegate{await Clean();});bar.Controls.Add(clean);cancel=Btn("Cancelar análise",delegate{if(cts!=null)cts.Cancel();});cancel.Enabled=false;bar.Controls.Add(cancel);
  temps.Columns.Add("Situação",180);temps.ItemCheck+=delegate(object sender,ItemCheckEventArgs e){var file=temps.Items[e.Index].Tag as Entry;if(file!=null&&file.Category.StartsWith("Recente"))e.NewValue=CheckState.Unchecked;};tp.Controls.Add(bar,0,1);tp.Controls.Add(temps,0,2);selection.Dock=DockStyle.Fill;selection.Text="Nenhum arquivo selecionado.";tp.Controls.Add(selection,0,3); temps.ItemChecked+=delegate{BeginInvoke((Action)UpdateSelection);};temps.DoubleClick+=delegate{Reveal(temps);};
  SetupLargeFiles();
  var rp=Page("Processos","Consumo ao vivo a cada 3 segundos. CPU relativa ao total de núcleos; a primeira leitura precisa de uma segunda amostra.");
  var rb=new FlowLayoutPanel{Dock=DockStyle.Fill};rb.Controls.Add(Btn("Atualizar processos",delegate{RefreshProcesses();}));rb.Controls.Add(Btn("Solicitar fechamento",delegate{CloseProcess(false);}));rb.Controls.Add(Btn("Forçar encerramento…",delegate{CloseProcess(true);}));rp.Controls.Add(rb,0,1);
  SetupProcesses(rp,rb);
  SetupStartup(); SetupApps();
  SetupDuplicates();SetupRecycle();SetupUpdates();SetupMaintenance();SetupAdvancedCleanup();SetupLeftovers();SetupExternalDisks();
  var logpage=new TabPage("Atividade");report.Multiline=true;report.ReadOnly=true;report.ScrollBars=ScrollBars.Both;report.WordWrap=false;report.Dock=DockStyle.Fill;logpage.Controls.Add(report);tabs.TabPages.Add(logpage);
  timer.Interval=3000;timer.Tick+=delegate{RefreshOverview();EnsureLargeScan();if(tabs.SelectedTab.Text=="Processos" && !busy)RefreshProcesses();};if(!demoMode)timer.Start();RefreshOverview();if(!demoMode)RefreshProcesses();
  BuildNavigation();SetupTray(); StyleLists(this);ConfigureResponsive(this);AdaptLayout();SetupAppearance(); status.Text="Pronto. Escolha uma área no menu para começar.";
  FormClosing+=delegate(object sender,FormClosingEventArgs e){if(busy){e.Cancel=true;MessageBox.Show(this,"Aguarde a operação terminar. Uma análise pode ser cancelada pelo botão Cancelar análise.");}else if(StartupDirty() && MessageBox.Show(this,"Há alterações de inicialização não aplicadas. Sair e descartar essas alterações?","+Espaço",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)e.Cancel=true;};
 }
 static ListView FilesView(bool check) {var v=new SmoothListView{Dock=DockStyle.Fill,View=View.Details,CheckBoxes=check,FullRowSelect=true,HideSelection=false};v.Columns.Add("Arquivo",230);v.Columns.Add("Tamanho",110);v.Columns.Add("Modificado",150);v.Columns.Add("Caminho completo",650);return v;}
 TableLayoutPanel Page(string title,string description) {var p=new TabPage(title){BackColor=Color.White};var t=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=4,ColumnCount=1,Padding=new Padding(0,14,0,0)};t.RowStyles.Add(new RowStyle(SizeType.Absolute,58));t.RowStyles.Add(new RowStyle(SizeType.Absolute,52));t.RowStyles.Add(new RowStyle(SizeType.Percent,100));t.RowStyles.Add(new RowStyle(SizeType.Absolute,40));t.Controls.Add(new Label{Text=description,Dock=DockStyle.Fill},0,0);p.Controls.Add(t);tabs.TabPages.Add(p);return t;}
 Button Btn(string text,EventHandler action) {var b=new RoundedButton{Text=text,AutoSize=true,Height=36,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(240,245,242),ForeColor=blue,Padding=new Padding(7,4,7,4),Margin=new Padding(0,2,8,2)};b.FlatAppearance.BorderSize=1;b.FlatAppearance.BorderColor=Color.FromArgb(221,230,224);b.Click+=action;return b;}
 void Busy(bool value) {busy=value;scanTemp.Enabled=scanLarge.Enabled=clean.Enabled=!value;cancel.Enabled=value;volumes.Enabled=refreshHome.Enabled=exploreHome.Enabled=chooseHome.Enabled=resetHome.Enabled=!value;upHome.Enabled=!value&&currentFolder!=null;if(!value)cancelHome.Enabled=false;}
 async Task Scan(bool temp) {
  if(busy)return;string[] roots;
  if(temp) roots=Engine.Roots(); else {using(var dialog=new FolderBrowserDialog{Description="Escolha a pasta para procurar arquivos grandes",ShowNewFolderButton=false}) {if(dialog.ShowDialog(this)!=DialogResult.OK)return;roots=new[]{dialog.SelectedPath};}}
  Busy(true);cts=new CancellationTokenSource();var errors=new List<string>();status.Text="Analisando… Pastas com links são ignoradas.";
  try {var entries=await Task.Run(()=>Engine.Scan(roots,temp,cts.Token,(count,path)=>BeginInvoke((Action)(()=>status.Text="Analisados "+count.ToString("N0")+" arquivos • "+path)),errors,true));
   var view=temp?temps:large;view.BeginUpdate();view.Items.Clear();foreach(var e in entries) {var i=new ListViewItem(new[]{System.IO.Path.GetFileName(e.Path),Engine.Bytes(e.Size),e.Modified.ToLocalTime().ToString("g"),e.Path});i.Tag=e;if(temp){i.SubItems.Add(e.Category);if(e.Category.StartsWith("Recente"))i.ForeColor=Color.Gray;}view.Items.Add(i);}view.EndUpdate();
   status.Text=entries.Count+" encontrados ("+entries.Count(x=>x.Category.StartsWith("Recente"))+" recentes protegidos) • "+Engine.Bytes(entries.Sum(x=>x.Size))+" • "+errors.Count+" itens/pastas sem acesso.";
   Log("ANÁLISE "+String.Join("; ",roots)+"\r\n"+status.Text);foreach(string error in errors)Log("IGNORADO "+error);UpdateSelection();
  } catch(OperationCanceledException){status.Text="Análise cancelada. Resultados anteriores preservados.";Log(status.Text);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Falha na análise");Log(ex.ToString());}
  finally{cts.Dispose();cts=null;Busy(false);}
 }
 void UpdateSelection() {long size=0;foreach(ListViewItem i in temps.CheckedItems)size+=((Entry)i.Tag).Size;selection.Text=temps.CheckedItems.Count+" selecionados • "+Engine.Bytes(size)+" potenciais (tamanho lógico)";}
 async Task Clean() {
  if(busy)return;var entries=temps.CheckedItems.Cast<ListViewItem>().Select(i=>(Entry)i.Tag).ToArray();if(entries.Length==0){MessageBox.Show(this,"Selecione os temporários que deseja excluir.");return;}
  if(MessageBox.Show(this,"Excluir permanentemente "+entries.Length+" temporários selecionados ("+Engine.Bytes(entries.Sum(x=>x.Size))+")?\r\n\r\nNão serão enviados à Lixeira. Feche os aplicativos que possam usar esses arquivos. A lista mostra os caminhos completos.","Confirmar limpeza",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
  Busy(true);cancel.Enabled=false;status.Text="Limpando arquivos selecionados…";var before=SavingsStore.Snapshot();var removed=new HashSet<string>();var messages=new List<string>();long bytes=0;
  try {await Task.Run(()=>{foreach(var e in entries)try {if(!Engine.Allowed(e)){messages.Add("PRESERVADO (alterado, recente ou fora das regras) "+e.Path);continue;}File.Delete(e.Path);removed.Add(e.Path);bytes+=e.Size;messages.Add("EXCLUÍDO "+Engine.Bytes(e.Size)+" | "+e.Path);}catch(Exception ex){messages.Add("NÃO EXCLUÍDO "+e.Path+" | "+ex.Message);}});
   if(removed.Count>0)RecordSavings("Temporários",removed.Count,bytes,before);
   foreach(ListViewItem i in temps.Items.Cast<ListViewItem>().ToArray())if(removed.Contains(((Entry)i.Tag).Path))temps.Items.Remove(i);
   foreach(var m in messages)Log(m);status.Text=removed.Count+" arquivos excluídos • "+Engine.Bytes(bytes)+" lógicos • "+(entries.Length-removed.Count)+" preservados/falhas.";Log(status.Text);RefreshOverview();UpdateSelection();MessageBox.Show(this,status.Text+"\r\n\r\nO espaço livre real aparece no painel superior. Compressão e outros programas podem afetar a variação.","Resultado da limpeza");
  }finally{Busy(false);}
 }
 void Reveal(ListView view) {if(view.SelectedItems.Count==0)return;var e=(Entry)view.SelectedItems[0].Tag;if(File.Exists(e.Path))Process.Start("explorer.exe","/select,\""+e.Path+"\"");else MessageBox.Show(this,"O arquivo não existe mais.");}
 void RefreshOverview() {if(demoMode){overview.Text="C:  128 GB livres / 512 GB\r\nRAM: 6,4 GB em uso / 16 GB • 9,6 GB disponível";return;}var disks=new List<string>();foreach(var d in DriveInfo.GetDrives())try{if(d.IsReady && d.DriveType==DriveType.Fixed)disks.Add(d.Name+"  "+Engine.Bytes(d.AvailableFreeSpace)+" livres / "+Engine.Bytes(d.TotalSize));}catch{}var m=new MEMORY();string ram=GlobalMemoryStatusEx(m)?"RAM: "+Engine.Bytes((long)(m.total-m.available))+" em uso / "+Engine.Bytes((long)m.total)+" • "+Engine.Bytes((long)m.available)+" disponível":"RAM indisponível";overview.Text=String.Join("     |     ",disks)+"\r\n"+ram;ramInfo.Text=ram+" • Atualização automática nesta tela.";}
 void CloseProcess(bool force) {if(processes.SelectedItems.Count==0)return;var row=processes.SelectedItems[0];var id=(ProcessRef)row.Tag;string name=row.Text;
  if(MessageBox.Show(this,(force?"Forçar o encerramento":"Solicitar fechamento")+" de "+name+" (PID "+id.Id+")?\r\n"+(force?"Dados não salvos podem ser perdidos.":"O aplicativo poderá pedir para salvar documentos."),"Confirmar",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
  try{using(var p=Process.GetProcessById(id.Id)){if(p.SessionId!=Process.GetCurrentProcess().SessionId || p.Id==Process.GetCurrentProcess().Id || Critical(p))throw new Exception("Este processo é protegido ou pertence a outra sessão.");if(p.StartTime!=id.Start)throw new Exception("O processo original já terminou.");string[] blocked={"explorer","dwm","csrss","winlogon","wininit","services","lsass","smss","svchost","sihost","fontdrvhost"};if(blocked.Contains(p.ProcessName.ToLowerInvariant()))throw new Exception("Processo do Windows protegido pelo aplicativo.");if(force){p.Kill();Log("ENCERRAMENTO SOLICITADO "+name+" PID "+id.Id);}else if(!p.CloseMainWindow())throw new Exception("Este processo não tem janela que aceite fechamento. Você pode usar o aplicativo original ou revisar a opção de forçar encerramento.");else Log("FECHAMENTO SOLICITADO "+name+" PID "+id.Id);}status.Text="Pedido enviado. Atualize a lista em alguns segundos para verificar.";}catch(Exception ex){MessageBox.Show(this,ex.Message,"Processo preservado ou indisponível");}RefreshOverview();
 }
 void Log(string message) {string line=DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+"  "+message;logs.Add(line);report.AppendText(line+Environment.NewLine);try{if(reportPath==null){var dir=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MaisEspaco","Relatorios");Directory.CreateDirectory(dir);reportPath=System.IO.Path.Combine(dir,DateTime.Now.ToString("yyyyMMdd-HHmmss")+".txt");}File.AppendAllText(reportPath,line+Environment.NewLine,Encoding.UTF8);}catch{status.Text="Não foi possível salvar o relatório em disco; os detalhes continuam na aba Relatório.";}}
}
static class Checks {
 public static int Run() {string root=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"EspacoClaro-Test-"+Guid.NewGuid());try{Directory.CreateDirectory(root);string old=System.IO.Path.Combine(root,"antigo.tmp"),fresh=System.IO.Path.Combine(root,"recente.tmp");File.WriteAllText(old,"teste");File.WriteAllText(fresh,"teste");File.SetLastWriteTimeUtc(old,DateTime.UtcNow.AddDays(-10));File.SetLastAccessTimeUtc(old,DateTime.UtcNow.AddDays(-10));var errors=new List<string>();var found=Engine.Scan(new[]{root},true,CancellationToken.None,(n,p)=>{},errors);if(found.Count!=1 || found[0].Path!=old || !Engine.Allowed(found[0]))return 1;if(Engine.Under(root+"-outside\\a",root))return 2;File.AppendAllText(old,"mudou");if(Engine.Allowed(found[0]))return 3;if(Engine.Scan(new[]{root},false,CancellationToken.None,(n,p)=>{},errors).Count!=0)return 4;var c=new CancellationTokenSource();c.Cancel();try{Engine.Scan(new[]{root},true,c.Token,(n,p)=>{},errors);return 5;}catch(OperationCanceledException){}return 0;}catch{return 10;}finally{if(Directory.Exists(root))Directory.Delete(root,true);}}
}
}
