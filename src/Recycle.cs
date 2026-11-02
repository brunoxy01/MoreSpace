using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
namespace MoreSpace {
static class FileRemoval {
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct Operation {public IntPtr hwnd;public uint operation;[MarshalAs(UnmanagedType.LPWStr)]public string from;[MarshalAs(UnmanagedType.LPWStr)]public string to;public ushort flags;[MarshalAs(UnmanagedType.Bool)]public bool aborted;public IntPtr mappings;[MarshalAs(UnmanagedType.LPWStr)]public string title;}
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)]static extern int SHFileOperation(ref Operation operation);
 [StructLayout(LayoutKind.Sequential)]public struct BinInfo {public int Size;public long Bytes,Items;}
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)]static extern int SHQueryRecycleBin(string root,ref BinInfo info);
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)]static extern int SHEmptyRecycleBin(IntPtr owner,string root,uint flags);
 public static BinInfo Query(string root){var info=new BinInfo{Size=Marshal.SizeOf(typeof(BinInfo))};int code=SHQueryRecycleBin(root,ref info);if(code!=0)throw new IOException("A Lixeira desta unidade está indisponível ("+code+").");return info;}
 public static void Empty(IntPtr owner,string root){int result=SHEmptyRecycleBin(owner,root,0);if(result!=0)throw new IOException("Esvaziamento cancelado ou indisponível ("+result+").");}
 public static void Delete(string path,bool permanent){if(permanent){File.Delete(path);return;}if(path.Length>=260)throw new IOException("A API da Lixeira não aceita este caminho longo. Use o Explorer ou revise a opção de exclusão permanente.");Exception error=null;var thread=new Thread(()=>{try{var op=new Operation{operation=3,from=Path.GetFullPath(path)+"\0\0",flags=0x40|0x4000|0x10|0x2000,title="+Espaço — Lixeira"};int result=SHFileOperation(ref op);if(result!=0||op.aborted)throw new IOException("Operação cancelada ou recusada pela Lixeira ("+result+").");if(File.Exists(path))throw new IOException("O arquivo ainda está na origem.");}catch(Exception ex){error=ex;}});thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();if(error!=null)throw error;}
}
partial class MainWindow {
 ListView binList=new SmoothListView();Label binNote=new Label();
 void SetupRecycle(){var page=Page("Lixeira","Arquivos enviados à Lixeira ainda ocupam espaço. Abra para restaurar itens; esvaziar remove definitivamente e pede confirmação do Windows.");var bar=new FlowLayoutPanel{Dock=DockStyle.Fill};bar.Controls.Add(Btn("Atualizar",delegate{RefreshBins();}));bar.Controls.Add(Btn("Abrir Lixeira",delegate{Process.Start("explorer.exe","shell:RecycleBinFolder");}));bar.Controls.Add(Btn("Esvaziar unidade…",delegate{if(busy||binList.SelectedItems.Count==0)return;string root=(string)binList.SelectedItems[0].Tag;if(MessageBox.Show(this,"Esvaziar permanentemente a Lixeira de "+root+"?","Confirmar",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;var before=SavingsStore.Snapshot();try{FileRemoval.Empty(Handle,root);RecordSavings("Lixeira esvaziada",0,null,before);RefreshBins();RefreshOverview();}catch(Exception ex){MessageBox.Show(this,ex.Message);}}));page.Controls.Add(bar,0,1);binList.Dock=DockStyle.Fill;binList.View=View.Details;binList.FullRowSelect=true;binList.MultiSelect=false;binList.Columns.Add("Unidade",180);binList.Columns.Add("Itens",160);binList.Columns.Add("Ocupação",180);page.Controls.Add(binList,0,2);binNote.Dock=DockStyle.Fill;binNote.Text="Clique em Atualizar para consultar o Windows. Nenhum item é apagado automaticamente.";page.Controls.Add(binNote,0,3);}
 void RefreshBins(){if(demoMode){binList.Items.Clear();binList.Items.Add(new ListViewItem(new[]{"C:\\ • Windows","42","2,8 GB"}));return;}binList.Items.Clear();foreach(var d in DriveInfo.GetDrives())try{if(d.IsReady&&(d.DriveType==DriveType.Fixed||d.DriveType==DriveType.Removable)){var info=FileRemoval.Query(d.Name);var row=new ListViewItem(new[]{d.Name,info.Items.ToString("N0"),Engine.Bytes(info.Bytes)});row.Tag=d.Name;binList.Items.Add(row);}}catch(Exception ex){Log("LIXEIRA "+d.Name+": "+ex.Message);}binNote.Text="Consulta concluída. Em discos sem Lixeira, use o Explorer para verificar o comportamento do Windows.";}
}
}
