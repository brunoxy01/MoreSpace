using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
namespace MoreSpace {
static class PerformanceChecks {
 // Previous scan's access pattern, retained only for the isolated benchmark.
 static long Legacy(string path){long size=0;var pending=new Stack<string>();pending.Push(path);while(pending.Count>0){var dir=pending.Pop();if((File.GetAttributes(dir)&FileAttributes.ReparsePoint)!=0)continue;foreach(var sub in Directory.EnumerateDirectories(dir))pending.Push(sub);foreach(var file in Directory.EnumerateFiles(dir)){var info=new FileInfo(file);if((info.Attributes&FileAttributes.ReparsePoint)==0)size+=info.Length;}}return size;}
 public static int Run(){string root=Path.Combine(Path.GetTempPath(),"MoreSpace-Perf-"+Guid.NewGuid().ToString("N"));try{
  Directory.CreateDirectory(root);for(int i=0;i<30;i++){var dir=Path.Combine(root,i.ToString(),"nested");Directory.CreateDirectory(dir);for(int j=0;j<100;j++)File.WriteAllBytes(Path.Combine(dir,j+".bin"),new byte[128]);}
  var watch=Stopwatch.StartNew();long expected=Legacy(root);double legacy=watch.Elapsed.TotalMilliseconds;watch.Restart();var index=ScanIndex.Build(root,CancellationToken.None,n=>{});double indexed=watch.Elapsed.TotalMilliseconds;if(index.Get(root).Items.Sum(i=>i.Size)!=expected||index.FilesRead!=3000)return 71;
  watch.Restart();for(int i=0;i<300;i++){var child=index.Get(Path.Combine(root,(i%30).ToString()));if(child.Items.Sum(x=>x.Size)!=12800)return 72;}double cached=watch.Elapsed.TotalMilliseconds;
  File.Delete(Path.Combine(root,"0","nested","0.bin"));if(index.Get(root).Items.Sum(i=>i.Size)!=expected)return 73;var fresh=ScanIndex.Build(root,CancellationToken.None,n=>{});if(fresh.Get(root).Items.Sum(i=>i.Size)!=expected-128)return 74;
  var canceled=new CancellationTokenSource();canceled.Cancel();try{ScanIndex.Build(root,canceled.Token,n=>{});return 75;}catch(OperationCanceledException){}
  string ledger=Path.Combine(root,"ledger");var store=new SavingsStore(ledger);store.Add(new SavingEvent{Kind="Temporários",Count=2,LogicalBytes=500,FreeDelta=-20,Utc=DateTime.UtcNow});store.Add(new SavingEvent{Kind="Aplicativos",Count=1,LogicalBytes=null,FreeDelta=1000,Utc=DateTime.UtcNow});var loaded=new SavingsStore(ledger);if(loaded.LogicalTotal!=500||loaded.Events.Count!=2||loaded.Events[0].FreeDelta==null)return 76;
  if(SavingsStore.Delta(new Dictionary<string,long>{{"C",100}},new Dictionary<string,long>{{"C",80}})!=-20)return 77;if(SavingsStore.Delta(new Dictionary<string,long>{{"C",100}},new Dictionary<string,long>())!=null)return 78;
  File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"benchmark.txt"),"Synthetic fixture: 3,000 files, 60 directories. Sequential warm-cache-sensitive timings; not a full-drive guarantee.\r\nPrevious access pattern: "+legacy.ToString("F2")+" ms\r\nIndexed first scan: "+indexed.ToString("F2")+" ms\r\n300 cached directory navigations: "+cached.ToString("F2")+" ms\r\nCache invalidation, cancellation and persistent history: passed.");return 0;
 }catch{return 79;}finally{if(Directory.Exists(root))Directory.Delete(root,true);}}
}
}
