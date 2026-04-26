using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace EraserMod.Injector;

internal static class Program
{
    private const string MarkerName = "EraserMod_Bootstrap_Loader";

    private static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("usage: EraserMod.Injector <gameDataDir>");
            Console.WriteLine("  example: \"C:\\Program Files (x86)\\Steam\\steamapps\\common\\Slay the Spire 2\\data_sts2_windows_x86_64\"");
            return 1;
        }

        var dataDir = Path.GetFullPath(args[0]);
        var sts2 = Path.Combine(dataDir, "sts2.dll");
        var backup = Path.Combine(dataDir, "sts2.dll.orig");
        var modDll = Path.Combine(dataDir, "EraserMod.dll");

        if (!File.Exists(sts2))
        {
            Console.Error.WriteLine($"sts2.dll not found at {sts2}");
            return 2;
        }
        if (!File.Exists(modDll))
        {
            Console.Error.WriteLine($"EraserMod.dll not found at {modDll} — copy it there first");
            return 3;
        }

        if (!File.Exists(backup))
        {
            File.Copy(sts2, backup);
            Console.WriteLine($"Backed up to {backup}");
        }
        else
        {
            Console.WriteLine("Backup exists; using backup as input (idempotent re-inject).");
        }

        ModuleDefMD module;
        using (var stream = File.OpenRead(backup))
        {
            module = ModuleDefMD.Load(stream);
        }

        try
        {
            var loaderType = module.Types.FirstOrDefault(t => t.Name == MarkerName);
            if (loaderType != null)
            {
                Console.WriteLine("Loader already injected; replacing.");
                module.Types.Remove(loaderType);
            }

            InjectLoader(module);

            // Use original metadata flags to avoid token shifts.
            var writerOpts = new ModuleWriterOptions(module);
            writerOpts.MetadataOptions.Flags |= MetadataFlags.PreserveAll;

            var tmp = sts2 + ".tmp";
            module.Write(tmp, writerOpts);
            module.Dispose();

            File.Delete(sts2);
            File.Move(tmp, sts2);
            Console.WriteLine($"Patched {sts2}");
        }
        catch
        {
            module.Dispose();
            throw;
        }

        return 0;
    }

    private static void InjectLoader(ModuleDefMD module)
    {
        var corLib = module.CorLibTypes;

        // ---- import refs ----
        var assemblyTr = new TypeRefUser(module, "System.Reflection", "Assembly", corLib.AssemblyRef);
        var typeTr = new TypeRefUser(module, "System", "Type", corLib.AssemblyRef);
        var methodInfoTr = new TypeRefUser(module, "System.Reflection", "MethodInfo", corLib.AssemblyRef);
        var methodBaseTr = new TypeRefUser(module, "System.Reflection", "MethodBase", corLib.AssemblyRef);
        var pathTr = new TypeRefUser(module, "System.IO", "Path", corLib.AssemblyRef);
        var runtimeLoaderRef = module.GetAssemblyRefs()
            .FirstOrDefault(r => r.Name == "System.Runtime.Loader")
            ?? throw new InvalidOperationException("System.Runtime.Loader assembly reference not found");
        var alcTr = new TypeRefUser(module, "System.Runtime.Loader", "AssemblyLoadContext", runtimeLoaderRef);

        var assemblyGetType = new MemberRefUser(module, "GetType",
            MethodSig.CreateInstance(new ClassSig(typeTr), corLib.String), assemblyTr);

        var typeGetMethod = new MemberRefUser(module, "GetMethod",
            MethodSig.CreateInstance(new ClassSig(methodInfoTr), corLib.String), typeTr);

        var assemblyGetExecutingAssembly = new MemberRefUser(module, "GetExecutingAssembly",
            MethodSig.CreateStatic(new ClassSig(assemblyTr)), assemblyTr);

        var assemblyGetLocation = new MemberRefUser(module, "get_Location",
            MethodSig.CreateInstance(corLib.String), assemblyTr);

        var pathGetDirectoryName = new MemberRefUser(module, "GetDirectoryName",
            MethodSig.CreateStatic(corLib.String, corLib.String), pathTr);

        var pathCombine2 = new MemberRefUser(module, "Combine",
            MethodSig.CreateStatic(corLib.String, corLib.String, corLib.String), pathTr);

        // MethodBase.Invoke(object, object[])
        var methodBaseInvoke = new MemberRefUser(module, "Invoke",
            MethodSig.CreateInstance(corLib.Object, corLib.Object,
                new SZArraySig(corLib.Object)), methodBaseTr);

        // AssemblyLoadContext.GetLoadContext(Assembly) -> AssemblyLoadContext  (static)
        var alcGetLoadContext = new MemberRefUser(module, "GetLoadContext",
            MethodSig.CreateStatic(new ClassSig(alcTr), new ClassSig(assemblyTr)), alcTr);

        // AssemblyLoadContext.LoadFromAssemblyPath(string) -> Assembly  (instance)
        var alcLoadFromAssemblyPath = new MemberRefUser(module, "LoadFromAssemblyPath",
            MethodSig.CreateInstance(new ClassSig(assemblyTr), corLib.String), alcTr);

        // ---- create loader type with static Load() ----
        var loader = new TypeDefUser(MarkerName,
            new TypeRefUser(module, "System", "Object", corLib.AssemblyRef));
        loader.Attributes = TypeAttributes.NotPublic | TypeAttributes.AutoLayout
            | TypeAttributes.AnsiClass | TypeAttributes.Abstract | TypeAttributes.Sealed;
        module.Types.Add(loader);

        var loadMethod = new MethodDefUser("Load",
            MethodSig.CreateStatic(corLib.Void),
            MethodImplAttributes.IL | MethodImplAttributes.Managed,
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig);
        loader.Methods.Add(loadMethod);

        var body = new CilBody();
        loadMethod.Body = body;

        // try {
        //   Assembly self = Assembly.GetExecutingAssembly();
        //   string dir = Path.GetDirectoryName(self.Location);
        //   string mod = Path.Combine(dir, "EraserMod.dll");
        //   AssemblyLoadContext alc = AssemblyLoadContext.GetLoadContext(self);
        //   Assembly a = alc.LoadFromAssemblyPath(mod);
        //   a.GetType("EraserMod.Bootstrap").GetMethod("Init").Invoke(null, null);
        // } catch { }

        var instrs = body.Instructions;
        var leaveTarget = OpCodes.Nop.ToInstruction();

        // alc = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())
        instrs.Add(OpCodes.Call.ToInstruction(assemblyGetExecutingAssembly));
        instrs.Add(OpCodes.Call.ToInstruction(alcGetLoadContext));
        // path = Path.Combine(Path.GetDirectoryName(self.Location), "EraserMod.dll")
        instrs.Add(OpCodes.Call.ToInstruction(assemblyGetExecutingAssembly));
        instrs.Add(OpCodes.Callvirt.ToInstruction(assemblyGetLocation));
        instrs.Add(OpCodes.Call.ToInstruction(pathGetDirectoryName));
        instrs.Add(OpCodes.Ldstr.ToInstruction("EraserMod.dll"));
        instrs.Add(OpCodes.Call.ToInstruction(pathCombine2));
        // a = alc.LoadFromAssemblyPath(path)
        instrs.Add(OpCodes.Callvirt.ToInstruction(alcLoadFromAssemblyPath));
        // a.GetType("EraserMod.Bootstrap").GetMethod("Init").Invoke(null, null)
        instrs.Add(OpCodes.Ldstr.ToInstruction("EraserMod.Bootstrap"));
        instrs.Add(OpCodes.Callvirt.ToInstruction(assemblyGetType));
        instrs.Add(OpCodes.Ldstr.ToInstruction("Init"));
        instrs.Add(OpCodes.Callvirt.ToInstruction(typeGetMethod));
        instrs.Add(OpCodes.Ldnull.ToInstruction());
        instrs.Add(OpCodes.Ldnull.ToInstruction());
        instrs.Add(OpCodes.Callvirt.ToInstruction(methodBaseInvoke));
        instrs.Add(OpCodes.Pop.ToInstruction());
        var leave = OpCodes.Leave_S.ToInstruction(leaveTarget);
        instrs.Add(leave);

        // catch handler
        var catchStart = OpCodes.Pop.ToInstruction();
        instrs.Add(catchStart);
        var leave2 = OpCodes.Leave_S.ToInstruction(leaveTarget);
        instrs.Add(leave2);

        instrs.Add(leaveTarget);
        instrs.Add(OpCodes.Ret.ToInstruction());

        var ehExceptionType = new TypeRefUser(module, "System", "Exception", corLib.AssemblyRef);
        body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
        {
            TryStart = instrs[0],
            TryEnd = catchStart,
            HandlerStart = catchStart,
            HandlerEnd = leaveTarget,
            CatchType = ehExceptionType,
        });

        // ---- inject call into <Module> .cctor ----
        var globalType = module.GlobalType;
        var cctor = globalType.FindOrCreateStaticConstructor();
        var cctorInstrs = cctor.Body.Instructions;

        // Insert call to loader at the very start.
        cctorInstrs.Insert(0, OpCodes.Call.ToInstruction(loadMethod));
    }
}
