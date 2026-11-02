using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
namespace MoreSpace {
static class LongPathChecks {
 public static int Run(){string root=Path.Combine(Path.GetTempPath(),"MoreSpace-Long-"+Guid.NewGuid().ToString("N"));try{string deep=root;for(int i=0;i<9;i++)deep=Path.Combine(deep,new string('a',28)+i);Directory.CreateDirectory(deep);string file=Path.Combine(deep,"example.bin");File.WriteAllBytes(file,new byte[128]);if(file.Length<=260)return 101;var index=ScanIndex.Build(root,CancellationToken.None,n=>{});if(index.Get(root).Items.Sum(i=>i.Size)!=128)return 102;var entries=ChartFileRules.Read(deep);if(entries.Count!=1)return 103;ChartFileRules.Validate(entries[0],deep);var result=LargeScanner.Read(new[]{root},CancellationToken.None,p=>{},100);if(result.Count!=1)return 104;var errors=new List<string>();Engine.Scan(new[]{root},false,CancellationToken.None,(n,p)=>{},errors);if(errors.Count!=0)return 105;return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"long-path-error.txt"),ex.ToString());return 109;}finally{if(Directory.Exists(root))Directory.Delete(root,true);}}
}
}
