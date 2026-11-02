using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace MoreSpace {
partial class MainWindow {
 NotifyIcon tray;Timer schedule;DateTime nextScan;bool scheduled;int scheduleMinutes=60;
 void SetupTray(){if(demoMode)return;var menu=new ContextMenuStrip();menu.Items.Add("Abrir +Espaço",null,delegate{Show();WindowState=FormWindowState.Normal;Activate();});menu.Items.Add("Processos",null,delegate{Show();WindowState=FormWindowState.Normal;tabs.SelectedTab=tabs.TabPages.Cast<TabPage>().First(p=>p.Text=="Processos");});menu.Items.Add("Sair",null,delegate{Close();});tray=new NotifyIcon{Text="+Espaço • clique para abrir",Icon=Icon,Visible=true,ContextMenuStrip=menu};tray.DoubleClick+=delegate{Show();WindowState=FormWindowState.Normal;Activate();};schedule=new Timer{Interval=60000};schedule.Tick+=async delegate{if(!scheduled||busy||DateTime.Now<nextScan)return;nextScan=DateTime.Now.AddMinutes(scheduleMinutes);await Scan(true);tray.BalloonTipTitle="+Espaço • análise concluída";tray.BalloonTipText=temps.Items.Count+" temporários candidatos. Abra o app para revisar. Nada foi apagado.";tray.ShowBalloonTip(5000);};schedule.Start();Disposed+=delegate{schedule.Dispose();tray.Visible=false;tray.Dispose();menu.Dispose();};}
 void AddScheduleControls(FlowLayoutPanel panel){var enabled=new CheckBox{Text="Analisar temporários periodicamente (somente com app aberto)",AutoSize=true};var interval=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=180};interval.Items.AddRange(new object[]{"A cada 1 hora","A cada 6 horas","A cada 24 horas"});interval.SelectedIndex=0;enabled.CheckedChanged+=delegate{scheduled=enabled.Checked;nextScan=DateTime.Now.AddMinutes(scheduleMinutes);};interval.SelectedIndexChanged+=delegate{scheduleMinutes=new[]{60,360,1440}[interval.SelectedIndex];nextScan=DateTime.Now.AddMinutes(scheduleMinutes);};panel.Controls.Add(enabled);panel.Controls.Add(interval);panel.Controls.Add(new Label{Text="Vem desligado. A opção vale para esta sessão; só analisa e notifica, nunca exclui.",AutoSize=true});}
}
}
