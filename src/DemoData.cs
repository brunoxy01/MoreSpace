using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
namespace MoreSpace {
partial class MainWindow {
 void PopulateDemo(){if(!demoMode)return;switch(tabs.SelectedTab.Text){case "Duplicados":duplicateGroups=new List<DuplicateGroup>();for(int i=0;i<3;i++){string name=new[]{"backup-projeto.zip","video-ferias.mp4","instalador.iso"}[i];long size=new[]{2400L,1800L,900L}[i]*1024*1024;duplicateGroups.Add(new DuplicateGroup{Hash="DEMONSTRAÇÃO",Files=new List<Entry>{new Entry{Path=@"C:\Demo\Originais\"+name,Size=size},new Entry{Path=@"C:\Demo\Downloads\"+name,Size=size}}});}RenderDuplicates();break;
 case "Lixeira":RefreshBins();break;
 case "Caches":cacheAreas.Items.Clear();foreach(var name in new[]{"Chrome • Default • Cache","VS Code • Cache","npm • cache","Relatórios de falha"})cacheAreas.Items.Add(new ListViewItem(new[]{name,"Disponível para análise",@"C:\Demo\AppData\Cache"}));break;
 case "Processos":processes.Items.Clear();processes.Items.Add(new ListViewItem(new[]{"Editor","1200","2,1%","420 MB","Sua sessão",@"C:\Demo\Editor.exe"}));processes.Items.Add(new ListViewItem(new[]{"Navegador","1450","3,2%","380 MB","Sua sessão",@"C:\Demo\Browser.exe"}));break;
 case "Arquivos grandes":large.Items.Clear();large.Items.Add(new ListViewItem(new[]{"backup-projeto.zip","8,2 GB","01/08/2025",@"C:\Demo\Downloads\backup-projeto.zip"}));large.Items.Add(new ListViewItem(new[]{"video-ferias.mp4","3,5 GB","15/01/2025",@"C:\Demo\Videos\video-ferias.mp4"}));largeSummary.Text="Demonstração • maior para o menor • marque os arquivos para revisar.";break;
 case "Atualizações":updatesOutput.Text="Demonstração — nenhum comando foi executado.\r\n\r\nNome                 ID                      Instalado   Disponível\r\nPowerToys            Microsoft.PowerToys       0.90        0.91\r\nEditor               Exemplo.Editor           1.0         1.1";break;
 case "Discos externos":RefreshRemovable();break;}}
}
}
