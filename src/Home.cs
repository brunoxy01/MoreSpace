using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MoreSpace {
class SpaceItem {public string Name,Path;public long Size,Files;public bool Directory;public int Skipped;}
class SpaceResult {public string Path;public List<SpaceItem> Items=new List<SpaceItem>();public int Skipped;}
static class SpaceScanner {
 public static SpaceResult Read(string path,CancellationToken token,Action<long> progress){return ScanIndex.Build(path,token,progress).Get(path);}

}
class PieSector {public string Name;public long Value;public Color Color;public SpaceItem Item;}
class SpacePie:Control {
 public List<PieSector> Sectors=new List<PieSector>();public Action<PieSector> Pick;public Action<PieSector,Point> ContextPick;readonly ToolTip tip=new ToolTip();int hovered=-1;RectangleF circle;
 public SpacePie(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);BackColor=Color.White;}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;float size=Math.Max(1,Math.Min(Width-32,Height-16));circle=new RectangleF((Width-size)/2,(Height-size)/2,size,size);double total=Sectors.Sum(s=>(double)s.Value);if(total<=0){TextRenderer.DrawText(e.Graphics,"Sem dados de tamanho",Font,ClientRectangle,Color.Gray,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);return;}float angle=-90;
  for(int i=0;i<Sectors.Count;i++){var sector=Sectors[i];float sweep=(float)(sector.Value/total*360);if(sweep<=0)continue;Color color=i==hovered?ControlPaint.Light(sector.Color,0.2f):sector.Color;using(var brush=new SolidBrush(color))e.Graphics.FillPie(brush,circle.X,circle.Y,circle.Width,circle.Height,angle,sweep);using(var pen=new Pen(Color.White,2))e.Graphics.DrawPie(pen,circle.X,circle.Y,circle.Width,circle.Height,angle,sweep);
   if(sweep>=35){double radians=(angle+sweep/2)*Math.PI/180;var center=new Point((int)(circle.X+size/2+Math.Cos(radians)*size*0.31),(int)(circle.Y+size/2+Math.Sin(radians)*size*0.31));var label=new Rectangle(center.X-35,center.Y-14,70,28);TextRenderer.DrawText(e.Graphics,(sector.Value/total*100).ToString("N0")+"%",Font,label,Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}
   angle+=sweep;
  }
 }
 int Hit(Point point){double x=point.X-(circle.X+circle.Width/2),y=point.Y-(circle.Y+circle.Height/2);if(x*x+y*y>circle.Width*circle.Width/4)return -1;double angle=(Math.Atan2(y,x)*180/Math.PI+450)%360,total=Sectors.Sum(s=>(double)s.Value),end=0;if(total<=0)return -1;for(int i=0;i<Sectors.Count;i++){end+=Sectors[i].Value/total*360;if(angle<end)return i;}return -1;}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);int hit=Hit(e.Location);if(hit==hovered)return;hovered=hit;Cursor=hit>=0&&Sectors[hit].Item!=null&&Sectors[hit].Item.Directory?Cursors.Hand:Cursors.Default;tip.SetToolTip(this,hit<0?"":Sectors[hit].Name+" • "+Engine.Bytes(Sectors[hit].Value)+(Sectors[hit].Item!=null&&Sectors[hit].Item.Directory?"\r\nClique para entrar na pasta":""));Invalidate();}
 protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hovered=-1;Invalidate();}
 protected override void OnMouseClick(MouseEventArgs e){base.OnMouseClick(e);int hit=Hit(e.Location);if(e.Button==MouseButtons.Left&&hit>=0&&Pick!=null)Pick(Sectors[hit]);if(e.Button==MouseButtons.Right&&hit>=0&&ContextPick!=null)ContextPick(Sectors[hit],e.Location);}
 protected override void Dispose(bool disposing){if(disposing)tip.Dispose();base.Dispose(disposing);}
}
partial class MainWindow {
 ComboBox volumes=new ComboBox();Label homeTitle=new Label(),homeNote=new Label();TextBox location=new TextBox();SpacePie pie=new SpacePie();ListView distribution=new SmoothListView();
 ScanIndex spaceIndex;
 Button exploreHome,upHome,resetHome,cancelHome,refreshHome,chooseHome;string currentFolder;bool loadingVolumes;SpaceResult currentSpace;
 readonly Color[] chartColors={Color.FromArgb(33,105,86),Color.FromArgb(62,159,130),Color.FromArgb(49,113,171),Color.FromArgb(120,108,172),Color.FromArgb(198,134,60),Color.FromArgb(176,86,110),Color.FromArgb(72,142,156),Color.FromArgb(126,144,73)};
 class Volume {public string Root,Label,Format;public long Total,Free;public override string ToString(){return Root+"  "+Label+" • "+Engine.Bytes(Total);}}
 void SetupHome(){
  var page=new TabPage("Home"){BackColor=Color.White};tabs.TabPages.Add(page);var grid=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=5,ColumnCount=1,Padding=new Padding(0,12,0,0)};grid.RowStyles.Add(new RowStyle(SizeType.Absolute,40));grid.RowStyles.Add(new RowStyle(SizeType.Absolute,52));grid.RowStyles.Add(new RowStyle(SizeType.Absolute,36));grid.RowStyles.Add(new RowStyle(SizeType.Percent,100));grid.RowStyles.Add(new RowStyle(SizeType.Absolute,56));page.Controls.Add(grid);
  homeTitle.Text="Partições e ocupação";homeTitle.Font=new Font("Segoe UI",16,FontStyle.Bold);homeTitle.Dock=DockStyle.Fill;grid.Controls.Add(homeTitle,0,0);
  var bar=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,AutoScroll=true};volumes.DropDownStyle=ComboBoxStyle.DropDownList;volumes.Width=230;volumes.Margin=new Padding(0,9,8,0);volumes.AccessibleName="Partição";bar.Controls.Add(volumes);
  refreshHome=Btn("Atualizar",async delegate{if(currentFolder!=null)await Explore(currentFolder,true);else LoadVolumes();});bar.Controls.Add(refreshHome);exploreHome=Btn("Explorar pastas",async delegate{var volume=volumes.SelectedItem as Volume;if(volume!=null)await Explore(volume.Root);});bar.Controls.Add(exploreHome);
  chooseHome=Btn("Escolher pasta",async delegate{if(busy)return;using(var dialog=new FolderBrowserDialog{Description="Escolha uma pasta para detalhar sua ocupação",ShowNewFolderButton=false}){if(dialog.ShowDialog(this)==DialogResult.OK)await Explore(dialog.SelectedPath);}});bar.Controls.Add(chooseHome);
  upHome=Btn("Subir nível",async delegate{if(currentFolder!=null){var parent=new DirectoryInfo(currentFolder).Parent;if(parent!=null)await Explore(parent.FullName);else ShowVolume();}});upHome.Enabled=false;bar.Controls.Add(upHome);
  resetHome=Btn("Partição",delegate{ShowVolume();});bar.Controls.Add(resetHome);cancelHome=Btn("Cancelar",delegate{if(cts!=null)cts.Cancel();});cancelHome.Enabled=false;bar.Controls.Add(cancelHome);grid.Controls.Add(bar,0,1);
  location.Dock=DockStyle.Fill;location.BorderStyle=BorderStyle.None;location.ReadOnly=true;location.BackColor=Color.White;location.ForeColor=Color.DimGray;grid.Controls.Add(location,0,2);
  var content=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};homeContent=content;content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,42));content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,58));pie.Dock=DockStyle.Fill;pie.Font=new Font("Segoe UI",12,FontStyle.Bold);content.Controls.Add(pie,0,0);
  distribution.Dock=DockStyle.Fill;distribution.View=View.Details;distribution.FullRowSelect=true;distribution.MultiSelect=false;distribution.Columns.Add("Pasta / espaço",245);distribution.Columns.Add("Tamanho",105);distribution.Columns.Add("Parcela",80);distribution.Columns.Add("Leitura",130);content.Controls.Add(distribution,1,0);grid.Controls.Add(content,0,3);homeNote.Dock=DockStyle.Fill;homeNote.ForeColor=Color.DimGray;grid.Controls.Add(homeNote,0,4);
  volumes.SelectedIndexChanged+=delegate{if(!loadingVolumes&&!busy)ShowVolume();};distribution.DoubleClick+=async delegate{if(distribution.SelectedItems.Count>0){var item=distribution.SelectedItems[0].Tag as SpaceItem;if(item!=null){if(item.Directory)await Explore(item.Path);else await ReviewChartFiles(item.Path);}}};pie.Pick=async sector=>{if(sector.Item!=null){if(sector.Item.Directory)await Explore(sector.Item.Path);else await ReviewChartFiles(sector.Item.Path);}};SetupChartActions();LoadVolumes();
 }
 void LoadVolumes(){if(busy)return;loadingVolumes=true;string previous=(volumes.SelectedItem as Volume)==null?null:((Volume)volumes.SelectedItem).Root;volumes.Items.Clear();if(demoMode){volumes.Items.Add(new Volume{Root="C:\\",Label="Windows",Format="NTFS",Total=512L*1024*1024*1024,Free=128L*1024*1024*1024});volumes.Items.Add(new Volume{Root="D:\\",Label="Dados",Format="NTFS",Total=1024L*1024*1024*1024,Free=640L*1024*1024*1024});}else foreach(var d in DriveInfo.GetDrives())try{if(d.IsReady&&(d.DriveType==DriveType.Fixed||d.DriveType==DriveType.Removable))volumes.Items.Add(new Volume{Root=d.RootDirectory.FullName,Label=d.VolumeLabel,Format=d.DriveFormat,Total=d.TotalSize,Free=d.TotalFreeSpace});}catch{}
  if(volumes.Items.Count>0){int found=volumes.Items.Cast<Volume>().ToList().FindIndex(v=>v.Root==previous);volumes.SelectedIndex=found>=0?found:0;}loadingVolumes=false;ShowVolume();
 }
 void ShowVolume(){if(busy)return;var v=volumes.SelectedItem as Volume;currentFolder=null;currentSpace=null;upHome.Enabled=false;pie.Sectors.Clear();distribution.Items.Clear();if(v==null){location.Text="Nenhuma partição montada disponível.";homeNote.Text="Somente volumes montados e acessíveis são exibidos.";pie.Invalidate();return;}
  homeTitle.Text="Partição "+v.Root+" • "+(String.IsNullOrWhiteSpace(v.Label)?"Sem rótulo":v.Label);location.Text=v.Root+"  |  "+v.Format+"  |  Capacidade: "+Engine.Bytes(v.Total);long used=Math.Max(0,v.Total-v.Free);
  pie.Sectors.Add(new PieSector{Name="Usado",Value=used,Color=chartColors[0],Item=new SpaceItem{Name="Usado",Path=v.Root,Directory=true}});pie.Sectors.Add(new PieSector{Name="Livre",Value=v.Free,Color=chartColors[1]});foreach(var sector in pie.Sectors){var row=new ListViewItem(new[]{sector.Name,Engine.Bytes(sector.Value),v.Total>0?(sector.Value/(double)v.Total*100).ToString("N1")+"%":"—","Volume"});row.Tag=sector.Item;row.ForeColor=sector.Color;distribution.Items.Add(row);}homeNote.Text="Selecione uma partição acima. Clique na fatia usada ou em Explorar pastas para detalhar a ocupação; a leitura de um disco inteiro pode demorar.\r\nPartições sem letra/ponto de montagem acessível não aparecem nesta lista.";pie.Invalidate();
 }
 async Task Explore(string path,bool force=false){if(busy||demoMode)return; if(!force&&spaceIndex!=null&&spaceIndex.Contains(path)){currentFolder=path;currentSpace=spaceIndex.Get(path);RenderSpace();status.Text="Leitura instantânea do índice • capturado às "+spaceIndex.CapturedUtc.ToLocalTime().ToString("HH:mm:ss")+" • Atualizar refaz a análise.";return;}Busy(true);cancel.Enabled=false;cts=new CancellationTokenSource();SetHomeBusy(true);status.Text="Calculando tamanhos em "+path+"…";
  try{var index=await Task.Run(()=>ScanIndex.Build(path,cts.Token,count=>BeginInvoke((Action)(()=>status.Text=count.ToString("N0")+" arquivos lidos • "+path))));spaceIndex=index;var result=index.Get(path);currentFolder=result.Path;currentSpace=result;RenderSpace();status.Text="Análise em "+index.Seconds.ToString("N1")+" s • "+index.FilesRead.ToString("N0")+" arquivos • navegação por subpastas sem releitura.";Log("OCUPAÇÃO "+path+" | "+result.Skipped+" ignorados");}
  catch(OperationCanceledException){status.Text="Leitura cancelada. A visualização anterior foi preservada.";}catch(Exception ex){Log("FALHA OCUPAÇÃO "+path+": "+ex.Message);MessageBox.Show(this,ex.Message,"Não foi possível analisar a pasta");}
  finally{cts.Dispose();cts=null;Busy(false);SetHomeBusy(false);}
 }
 void SetHomeBusy(bool value){volumes.Enabled=refreshHome.Enabled=exploreHome.Enabled=chooseHome.Enabled=resetHome.Enabled=!value;upHome.Enabled=!value&&currentFolder!=null;cancelHome.Enabled=value;}
 void RenderSpace(){var result=currentSpace;if(result==null)return;homeTitle.Text="Ocupação por pasta";location.Text=result.Path+(spaceIndex==null?"":"  • Índice de "+spaceIndex.CapturedUtc.ToLocalTime().ToString("HH:mm:ss")+" — Atualizar para reler");upHome.Enabled=true;pie.Sectors.Clear();distribution.BeginUpdate();distribution.Items.Clear();double total=result.Items.Sum(i=>(double)i.Size);int index=0;
  foreach(var item in result.Items){Color color=chartColors[Math.Min(index,7)];var row=new ListViewItem(new[]{item.Name,Engine.Bytes(item.Size),total>0?(item.Size/total*100).ToString("N1")+"%":"—",item.Skipped>0?"Parcial • "+item.Skipped:"Concluída"});row.Tag=item;row.ToolTipText=item.Path;row.ForeColor=color;distribution.Items.Add(row);if(index<7)pie.Sectors.Add(new PieSector{Name=item.Name,Value=item.Size,Color=color,Item=item});index++;}
  if(result.Items.Count>7)pie.Sectors.Add(new PieSector{Name="Outros — veja a lista",Value=result.Items.Skip(7).Sum(i=>i.Size),Color=chartColors[7]});distribution.EndUpdate();homeNote.Text="Clique em uma fatia de pasta ou dê duplo clique na lista para descer um nível. Botão direito: ver arquivos / excluir.\r\nTamanhos lógicos; links e itens sem acesso são ignorados ("+result.Skipped+"). O total pode diferir do uso físico da partição.";pie.Invalidate();
 }
 public int CheckHomeUi(){tabs.SelectedIndex=0;Application.DoEvents();if(pie.Sectors.Count!=2||volumes.Items.Count!=2)return 51;if(pie.Sectors.Sum(s=>s.Value)!=512L*1024*1024*1024)return 52;volumes.SelectedIndex=1;Application.DoEvents();if(pie.Sectors.Sum(s=>s.Value)!=1024L*1024*1024*1024)return 53;currentSpace=new SpaceResult{Path="D:\\Exemplo",Items=new List<SpaceItem>{new SpaceItem{Name="Fotos",Path="D:\\Exemplo\\Fotos",Directory=true,Size=300},new SpaceItem{Name="Documentos",Path="D:\\Exemplo\\Documentos",Directory=true,Size=100}}};RenderSpace();if(pie.Sectors.Count!=2||distribution.Items.Count!=2)return 54;ShowVolume();return 0;}
}
static class SpaceChecks {
 public static int Run(){string root=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"MoreSpace-SpaceTest-"+Guid.NewGuid().ToString("N"));try{Directory.CreateDirectory(System.IO.Path.Combine(root,"a","b"));File.WriteAllBytes(System.IO.Path.Combine(root,"root.bin"),new byte[10]);File.WriteAllBytes(System.IO.Path.Combine(root,"a","file.bin"),new byte[20]);File.WriteAllBytes(System.IO.Path.Combine(root,"a","b","nested.bin"),new byte[30]);var result=SpaceScanner.Read(root,CancellationToken.None,n=>{});if(result.Items.Sum(i=>i.Size)!=60||result.Items.Count!=2)return 61;var child=result.Items.First(i=>i.Directory);if(child.Size!=50)return 62;var deeper=SpaceScanner.Read(child.Path,CancellationToken.None,n=>{});if(deeper.Items.Sum(i=>i.Size)!=50||deeper.Items.Count!=2)return 63;var cancel=new CancellationTokenSource();cancel.Cancel();try{SpaceScanner.Read(root,cancel.Token,n=>{});return 64;}catch(OperationCanceledException){}return 0;}catch{return 69;}finally{if(Directory.Exists(root))Directory.Delete(root,true);}}
}
}
