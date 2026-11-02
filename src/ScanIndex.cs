using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
namespace MoreSpace {
// Keep directory summaries, not individual files. Navigation reuses one snapshot.
class ScanIndex {
 class Node {public string Path;public List<Node> Children=new List<Node>();public long Own,OwnFiles,Total,Files;public int OwnSkipped,Skipped;}
 readonly Dictionary<string,Node> nodes=new Dictionary<string,Node>(StringComparer.OrdinalIgnoreCase);
 public DateTime CapturedUtc;public long FilesRead;public double Seconds;
 static string Key(string path){return System.IO.Path.GetFullPath(path).TrimEnd('\\');}
 public bool Contains(string path){return nodes.ContainsKey(Key(path));}
 public SpaceResult Get(string path){Node n;if(!nodes.TryGetValue(Key(path),out n))return null;var result=new SpaceResult{Path=n.Path,Skipped=n.Skipped};if(n.OwnFiles>0||n.OwnSkipped>0)result.Items.Add(new SpaceItem{Name="Arquivos nesta pasta",Path=n.Path,Size=n.Own,Files=n.OwnFiles,Skipped=n.OwnSkipped});foreach(var c in n.Children)result.Items.Add(new SpaceItem{Name=new DirectoryInfo(c.Path).Name,Path=c.Path,Directory=true,Size=c.Total,Files=c.Files,Skipped=c.Skipped});result.Items=result.Items.OrderByDescending(i=>i.Size).ToList();return result;}
 public static ScanIndex Build(string path,CancellationToken token,Action<long> progress){token.ThrowIfCancellationRequested();path=System.IO.Path.GetFullPath(path);if(!Engine.NoLinks(path))throw new IOException("Pastas com links não são percorridas.");var watch=Stopwatch.StartNew();var index=new ScanIndex();var root=new Node{Path=path};var pending=new Stack<Node>();var ordered=new List<Node>();pending.Push(root);long lastUpdate=0;
  while(pending.Count>0){token.ThrowIfCancellationRequested();var node=pending.Pop();index.nodes.Add(Key(node.Path),node);ordered.Add(node);try{
   foreach(var info in new DirectoryInfo(node.Path).EnumerateFileSystemInfos()){token.ThrowIfCancellationRequested();try{var attrs=info.Attributes;if((attrs&FileAttributes.ReparsePoint)!=0){node.OwnSkipped++;continue;}if((attrs&FileAttributes.Directory)!=0){var child=new Node{Path=info.FullName};node.Children.Add(child);pending.Push(child);}else{node.Own+=((FileInfo)info).Length;node.OwnFiles++;index.FilesRead++;}
    if(watch.ElapsedMilliseconds-lastUpdate>=200){lastUpdate=watch.ElapsedMilliseconds;progress(index.FilesRead);}
   }catch(UnauthorizedAccessException){node.OwnSkipped++;}catch(IOException){node.OwnSkipped++;}}
  }catch(UnauthorizedAccessException){if(node==root)throw;node.OwnSkipped++;}catch(IOException){if(node==root)throw;node.OwnSkipped++;}}
  for(int i=ordered.Count-1;i>=0;i--){token.ThrowIfCancellationRequested();var node=ordered[i];node.Total=node.Own;node.Files=node.OwnFiles;node.Skipped=node.OwnSkipped;foreach(var child in node.Children){node.Total+=child.Total;node.Files+=child.Files;node.Skipped+=child.Skipped;}}
  index.Seconds=watch.Elapsed.TotalSeconds;index.CapturedUtc=DateTime.UtcNow;return index;
 }
}
}
