using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Windows.Forms;
namespace MoreSpace {
class SavingEvent {public DateTime Utc{get;set;}public string Kind{get;set;}public int Count{get;set;}public long? LogicalBytes{get;set;}public long? FreeDelta{get;set;}}
class SavingsStore {
 readonly string directory;public List<SavingEvent> Events=new List<SavingEvent>();public int Unreadable;
 public SavingsStore(string path){directory=path;if(!Directory.Exists(path))return;foreach(string file in Directory.GetFiles(path,"*.json"))try{var item=new JavaScriptSerializer().Deserialize<SavingEvent>(File.ReadAllText(file));if(item==null||item.Count<0||item.LogicalBytes<0)throw new IOException();Events.Add(item);}catch{Unreadable++;}}
 public void Add(SavingEvent item){Directory.CreateDirectory(directory);string name=Path.Combine(directory,Guid.NewGuid().ToString("N"));File.WriteAllText(name+".tmp",new JavaScriptSerializer().Serialize(item));File.Move(name+".tmp",name+".json");Events.Add(item);}
 public long LogicalTotal{get{return Events.Sum(e=>e.LogicalBytes??0);}}
 public static Dictionary<string,long> Snapshot(){var result=new Dictionary<string,long>(StringComparer.OrdinalIgnoreCase);foreach(var drive in DriveInfo.GetDrives())try{if(drive.DriveType==DriveType.Fixed&&drive.IsReady)result[drive.Name]=drive.TotalFreeSpace;}catch{}return result;}
 public static long? Delta(Dictionary<string,long> before,Dictionary<string,long> after){if(before.Count==0||before.Keys.Any(k=>!after.ContainsKey(k)))return null;return before.Sum(kv=>after[kv.Key]-kv.Value);}
}
partial class MainWindow {
 SavingsStore savings;Label savingsLabel=new Label();
 void SetupSavings(TableLayoutPanel layout){var row=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,64));row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,36));row.Controls.Add(overview,0,0);savingsLabel.Dock=DockStyle.Fill;savingsLabel.Padding=new Padding(12,8,0,0);savingsLabel.BackColor=Color.FromArgb(238,246,242);savingsLabel.ForeColor=blue;savingsLabel.Font=new Font("Segoe UI",9);savingsLabel.Cursor=Cursors.Hand;savingsLabel.Click+=delegate{ShowSavings();};row.Controls.Add(savingsLabel,1,0);layout.Controls.Add(row,0,1);
  try{savings=new SavingsStore(demoMode?Path.Combine(Path.GetTempPath(),"MoreSpace-Demo-NotUsed"):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MaisEspaco","Historico"));if(demoMode)savings.Events.Clear();}catch(Exception ex){savingsLabel.Text="Histórico indisponível: "+ex.Message;return;}UpdateSavings();
 }
 void UpdateSavings(){if(savings!=null)savingsLabel.Text="ESPAÇO LIBERADO • "+Engine.Bytes(savings.LogicalTotal)+"\r\nTemporários removidos (tamanho lógico)\r\nClique para histórico e variação no disco";}
 void RecordSavings(string kind,int count,long? logical,Dictionary<string,long> before){spaceIndex=null;long? delta=SavingsStore.Delta(before,SavingsStore.Snapshot());if(savings==null){Log("Histórico indisponível: resultado não persistido.");return;}try{savings.Add(new SavingEvent{Utc=DateTime.UtcNow,Kind=kind,Count=count,LogicalBytes=logical,FreeDelta=delta});UpdateSavings();Log("ESPAÇO: "+kind+" | lógico: "+(logical.HasValue?Engine.Bytes(logical.Value):"não medido")+" | variação livre: "+Signed(delta));}catch(Exception ex){Log("FALHA AO SALVAR HISTÓRICO: "+ex.Message);MessageBox.Show(this,"A operação terminou, mas o histórico não foi salvo: "+ex.Message,"Histórico");}}
 static string Signed(long? value){return !value.HasValue?"Indisponível":(value.Value>=0?"+":"−")+Engine.Bytes(Math.Abs(value.Value));}
 void ShowSavings(){using(var dialog=new Form{Text="Espaço liberado • Histórico",Size=new Size(900,520),StartPosition=FormStartPosition.CenterParent,Font=Font,BackColor=Color.White}){var grid=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,ColumnCount=1,Padding=new Padding(18)};grid.RowStyles.Add(new RowStyle(SizeType.Absolute,90));grid.RowStyles.Add(new RowStyle(SizeType.Percent,100));dialog.Controls.Add(grid);grid.Controls.Add(new Label{Dock=DockStyle.Fill,Text="O total soma apenas temporários excluídos pelo +Espaço a partir da versão 2.2.\r\nA variação do espaço livre é medida antes/depois nos discos fixos e inclui atividade de outros programas.\r\nRemoções de apps registram essa variação, sem estimar o tamanho dos pacotes."+(savings!=null&&savings.Unreadable>0?"\r\n"+savings.Unreadable+" registros não puderam ser lidos.":"")},0,0);var list=new SmoothListView{Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true};list.Columns.Add("Quando",150);list.Columns.Add("Operação",150);list.Columns.Add("Itens",60);list.Columns.Add("Tamanho lógico",160);list.Columns.Add("Variação livre no disco",200);if(savings!=null)foreach(var e in savings.Events.OrderByDescending(e=>e.Utc))list.Items.Add(new ListViewItem(new[]{e.Utc.ToLocalTime().ToString("g"),e.Kind,e.Count.ToString(),e.LogicalBytes.HasValue?Engine.Bytes(e.LogicalBytes.Value):"Não medido",Signed(e.FreeDelta)}));grid.Controls.Add(list,0,1);dialog.ShowDialog(this);}}
}
}
