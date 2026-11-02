using System;
using System.IO;
using System.Linq;
using System.Threading;
namespace MoreSpace {
static class FeatureChecks {
 public static int Run(){string root=Path.Combine(Path.GetTempPath(),"MoreSpace-Features-"+Guid.NewGuid().ToString("N"));try{Directory.CreateDirectory(root);File.WriteAllBytes(Path.Combine(root,"original.bin"),new byte[]{1,2,3,4});File.WriteAllBytes(Path.Combine(root,"copy.bin"),new byte[]{1,2,3,4});File.WriteAllBytes(Path.Combine(root,"different.bin"),new byte[]{1,2,3,5});Directory.CreateDirectory(Path.Combine(root,".git"));File.WriteAllBytes(Path.Combine(root,".git","ignored.bin"),new byte[]{1,2,3,4});var groups=DuplicateScanner.Scan(root,CancellationToken.None,s=>{},1);if(groups.Count!=1||groups[0].Files.Count!=2)return 161;DuplicateScanner.Revalidate(groups[0],groups[0].Files[1]);bool blocked=false;try{DuplicateScanner.Revalidate(groups[0],groups[0].Files[0]);}catch(IOException){blocked=true;}if(!blocked)return 162;File.WriteAllBytes(groups[0].Files[0].Path,new byte[]{4,3,2,1});blocked=false;try{DuplicateScanner.Revalidate(groups[0],groups[0].Files[1]);}catch(IOException){blocked=true;}if(!blocked)return 163;
  File.WriteAllText(Path.Combine(root,".DS_Store"),"metadata");File.WriteAllText(Path.Combine(root,"._keep"),"metadata");var metadata=DiskMetadata.Scan(root,CancellationToken.None);if(metadata.Count!=1||Path.GetFileName(metadata[0].Path)!=".DS_Store")return 164;FileRemoval.Delete(metadata[0].Path,true);if(File.Exists(metadata[0].Path))return 165;var cancel=new CancellationTokenSource();cancel.Cancel();try{DuplicateScanner.Scan(root,cancel.Token,s=>{},1);return 166;}catch(OperationCanceledException){}return 0;
 }catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"features-error.txt"),ex.ToString());return 169;}finally{if(Directory.Exists(root))Directory.Delete(root,true);}}
}
}
