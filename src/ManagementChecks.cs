using System;
using System.IO;
using System.Linq;
using Microsoft.Win32;
namespace MoreSpace {
static class ManagementChecks {
 public static int Run(){
  string root=@"Software\MaisEspaco\Tests\"+Guid.NewGuid().ToString("N");
  try{
   if(!StartupManager.Known(null)||!StartupManager.Enabled(null))return 11;
   if(StartupManager.Known(new byte[]{2}))return 12;
   if(StartupManager.Known(new byte[12]))return 13;
   foreach(byte state in new byte[]{2,3,6,7}){var data=new byte[12];data[0]=state;if(!StartupManager.Known(data)||StartupManager.Enabled(data)!=(state==2||state==6))return 14;}
   if(AppManager.Quote("a'b")!="'a''b'")return 15;
   using(var hive=RegistryKey.OpenBaseKey(RegistryHive.CurrentUser,RegistryView.Registry64)){
    using(var source=hive.CreateSubKey(root+@"\Source"))source.SetValue("FakeEntry","nonexistent-test-app.exe",RegistryValueKind.String);
    var e=new StartupEntry{Name="FakeEntry",Command="nonexistent-test-app.exe",Known=true,Enabled=true,Machine=false,SourcePath=root+@"\Source",ApprovalPath=root+@"\Approval"};
    StartupManager.Apply(e,false);using(var approval=hive.OpenSubKey(e.ApprovalPath)){e.Original=(byte[])approval.GetValue(e.Name);if(StartupManager.Enabled(e.Original))return 16;}
    StartupManager.Apply(e,true);using(var approval=hive.OpenSubKey(e.ApprovalPath)){var state=(byte[])approval.GetValue(e.Name);if(!StartupManager.Enabled(state))return 17;}
    bool rejected=false;try{StartupManager.Apply(e,false);}catch{rejected=true;}if(!rejected)return 18;
    using(var approval=hive.OpenSubKey(e.ApprovalPath))e.Original=(byte[])approval.GetValue(e.Name);
    using(var source=hive.OpenSubKey(e.SourcePath,true))source.SetValue(e.Name,"changed.exe");rejected=false;try{StartupManager.Apply(e,false);}catch{rejected=true;}if(!rejected)return 19;
   }
   var apps=AppManager.Read();if(apps.Any(a=>String.IsNullOrEmpty(a.FullName)||String.IsNullOrEmpty(a.Name)))return 20;
   if(apps.Any(a=>(a.Name.Contains("WindowsStore")||a.Name.Contains("DesktopAppInstaller"))&&!a.Protected))return 21;
   return 0;
  }catch(Exception ex){File.WriteAllText(Path.Combine(Path.GetTempPath(),"MaisEspaco-test-error.txt"),ex.ToString());return 99;}
  finally{using(var hive=RegistryKey.OpenBaseKey(RegistryHive.CurrentUser,RegistryView.Registry64)){hive.DeleteSubKeyTree(root,false);}}
 }
}
}
