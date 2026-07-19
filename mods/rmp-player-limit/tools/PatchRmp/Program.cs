using Mono.Cecil;
using Mono.Cecil.Cil;

// Patches Rain156 RemoveMultiplayerPlayerLimit (workshop) for current STS2:
// - StringHelper.GetDeterministicHashCode: UInt64 -> Int32
// - Rng..ctor(UInt64) -> Rng..ctor(UInt32, Int32 counter=0)
//
// Usage:
//   PatchRmp <original.dll> <sts2.dll> <output.dll>

if (args.Length < 3)
{
    Console.Error.WriteLine("Usage: PatchRmp <original.dll> <sts2.dll> <output.dll>");
    return 1;
}

string srcPath = args[0];
string sts2Path = args[1];
string outPath = args[2];

var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetDirectoryName(sts2Path)!);
resolver.AddSearchDirectory(Path.GetDirectoryName(srcPath)!);

using var sts2 = ModuleDefinition.ReadModule(sts2Path, new ReaderParameters { AssemblyResolver = resolver });
var stringHelper = sts2.Types.First(t => t.FullName == "MegaCrit.Sts2.Core.Helpers.StringHelper");
var hashInt = stringHelper.Methods.First(m =>
    m.Name == "GetDeterministicHashCode"
    && m.Parameters.Count == 1
    && m.Parameters[0].ParameterType.FullName == "System.String");

var rngType = sts2.Types.First(t => t.FullName == "MegaCrit.Sts2.Core.Random.Rng");
var rngCtor = rngType.Methods.First(m =>
    m.IsConstructor
    && m.Parameters.Count >= 1
    && m.Parameters[0].ParameterType.FullName == "System.UInt32");

Console.WriteLine($"hash  : {hashInt.FullName}");
Console.WriteLine($"rng   : {rngCtor.FullName}");

using var rmp = ModuleDefinition.ReadModule(srcPath, new ReaderParameters
{
    AssemblyResolver = resolver,
    InMemory = true,
});

var importedHash = rmp.ImportReference(hashInt);
var importedRngCtor = rmp.ImportReference(rngCtor);
bool rngNeedsCounter = importedRngCtor.Parameters.Count >= 2;

int hashFixes = 0;
int rngFixes = 0;

foreach (var type in rmp.Types.SelectMany(Flatten))
{
    foreach (var method in type.Methods.Where(m => m.HasBody))
    {
        var body = method.Body;
        var il = body.GetILProcessor();
        // Snapshot — we mutate as we go.
        for (int i = 0; i < body.Instructions.Count; i++)
        {
            var instr = body.Instructions[i];
            if (instr.Operand is not MethodReference mr)
                continue;

            if (mr.Name == "GetDeterministicHashCode" && mr.DeclaringType.Name == "StringHelper")
            {
                instr.Operand = importedHash;
                hashFixes++;

                // Original IL is typically: call hash; newobj Rng(ulong)
                // or: call hash; conv.u4; conv.u8; newobj Rng(ulong)
                // Normalize to: call hash; conv.u4; [ldc.i4.0]; newobj Rng(uint[, int])
                Instruction? cursor = instr.Next;
                // Strip widening/narrowing junk until Newobj Rng or end of short window.
                while (cursor != null
                       && cursor.OpCode != OpCodes.Newobj
                       && (cursor.OpCode == OpCodes.Conv_U4
                           || cursor.OpCode == OpCodes.Conv_U8
                           || cursor.OpCode == OpCodes.Conv_I4
                           || cursor.OpCode == OpCodes.Conv_I8
                           || cursor.OpCode == OpCodes.Conv_Ovf_U4
                           || cursor.OpCode == OpCodes.Conv_Ovf_U8))
                {
                    var remove = cursor;
                    cursor = cursor.Next;
                    il.Remove(remove);
                }

                // Ensure conv.u4 after hash (int -> uint).
                il.InsertAfter(instr, il.Create(OpCodes.Conv_U4));
                var afterConv = instr.Next!;

                if (cursor != null
                    && cursor.OpCode == OpCodes.Newobj
                    && cursor.Operand is MethodReference ctor
                    && ctor.DeclaringType.Name == "Rng")
                {
                    cursor.Operand = importedRngCtor;
                    if (rngNeedsCounter)
                        il.InsertBefore(cursor, il.Create(OpCodes.Ldc_I4_0));
                    rngFixes++;
                }
                else
                {
                    Console.WriteLine($"WARN: no Rng newobj after hash in {method.FullName}");
                }

                Console.WriteLine($"patched hash in {method.Name}");
            }
            else if (mr.Name == ".ctor"
                     && mr.DeclaringType.Name == "Rng"
                     && mr.Parameters.Count == 1
                     && mr.Parameters[0].ParameterType.FullName == "System.UInt64")
            {
                // Catch any remaining ulong Rng ctors.
                instr.Operand = importedRngCtor;
                il.InsertBefore(instr, il.Create(OpCodes.Conv_U4));
                if (rngNeedsCounter)
                    il.InsertBefore(instr, il.Create(OpCodes.Ldc_I4_0));
                rngFixes++;
                Console.WriteLine($"patched lone Rng ctor in {method.Name}");
            }
        }
    }
}

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
rmp.Write(outPath);
Console.WriteLine($"OK hashFixes={hashFixes} rngFixes={rngFixes} -> {outPath}");
return hashFixes > 0 ? 0 : 2;

static IEnumerable<TypeDefinition> Flatten(TypeDefinition t)
{
    yield return t;
    foreach (var n in t.NestedTypes.SelectMany(Flatten))
        yield return n;
}
