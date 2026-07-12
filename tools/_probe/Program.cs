using System.Reflection;
using System.Runtime.Loader;
var path = @"C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll";
var dir = Path.GetDirectoryName(path)!;
var ctx = new AssemblyLoadContext("sts2", true);
ctx.Resolving += (c, n) => {
  var p = Path.Combine(dir, n.Name + ".dll");
  return File.Exists(p) ? c.LoadFromAssemblyPath(p) : null;
};
var asm = ctx.LoadFromAssemblyPath(path);
var t = asm.GetTypes().First(x => x.Name == "AbstractModel");
foreach (var m in t.GetMethods(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly)
    .Where(m => m.Name.Contains("TryModify")))
{
  Console.WriteLine(m.Name);
  Console.WriteLine("  virtual=" + m.IsVirtual + " abstract=" + m.IsAbstract);
  foreach (var p in m.GetParameters())
    Console.WriteLine($"  {p.ParameterType} name={p.Name} IsByRef={p.ParameterType.IsByRef} IsOut={p.IsOut}");
  Console.WriteLine("  ret " + m.ReturnType);
}
// also CardModel TryModifyEnergy
var ct = asm.GetTypes().First(x => x.Name == "CardModel");
foreach (var m in ct.GetMethods(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly)
    .Where(m => m.Name.Contains("TryModify") || m.Name.Contains("EnergyCost")))
{
  Console.WriteLine("CardModel." + m.Name);
  foreach (var p in m.GetParameters())
    Console.WriteLine($"  {p.ParameterType} {p.Name} byref={p.ParameterType.IsByRef} out={p.IsOut}");
}
