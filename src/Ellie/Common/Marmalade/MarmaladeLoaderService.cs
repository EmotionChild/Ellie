using Discord.Commands.Builders;
using Microsoft.Extensions.DependencyInjection;
using Ellie.Common.ModuleBehaviors;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Ellie.Marmalade;

// ReSharper disable RedundantAssignment
public sealed class MarmaladeLoaderService : IMarmaladeLoaderService, IReadyExecutor, IEService
{
    private readonly CommandService _cmdService;
    private readonly IServiceProvider _botServices;
    private readonly IBehaviorHandler _behHandler;
    private readonly IPubSub _pubSub;
    private readonly IMarmaladeConfigService _marmaladeConfig;
    
    private readonly ConcurrentDictionary<string, ResolvedMarmalade> _resolved = new();
#pragma warning disable IDE0090 // Use 'new(...)'
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
#pragma warning restore IDE0090 // Use 'new(...)'

    private readonly TypedKey<string> _loadKey = new("marmalade:load");
    private readonly TypedKey<string> _unloadKey = new("marmalade:unload");
    
    private readonly TypedKey<bool> _stringsReload = new("marmalade:reload_strings");

    private const string BASE_DIR = "data/marmalade";

    public MarmaladeLoaderService(CommandService cmdService,
        IServiceProvider botServices,
        IBehaviorHandler behHandler,
        IPubSub pubSub,
        IMarmaladeConfigService marmaladeConfig)
    {
        _cmdService = cmdService;
        _botServices = botServices;
        _behHandler = behHandler;
        _pubSub = pubSub;
        _marmaladeConfig = marmaladeConfig;
        
        // has to be done this way to support this feature on sharded bots
        _pubSub.Sub(_loadKey, async name => await InternalLoadAsync(name));
        _pubSub.Sub(_unloadKey, async name => await InternalUnloadAsync(name));

        _pubSub.Sub(_stringsReload, async _ => await ReloadStringsInternal());
    }

    public IReadOnlyCollection<string> GetAllMarmalades()
    {
        if (!Directory.Exists(BASE_DIR))
            return Array.Empty<string>();

        return Directory.GetDirectories(BASE_DIR)
                        .Select(x => Path.GetRelativePath(BASE_DIR, x))
                        .ToArray();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public IReadOnlyCollection<MarmaladeStats> GetLoadedMarmalades(CultureInfo? culture)
    {
        var toReturn = new List<MarmaladeStats>(_resolved.Count);
        foreach (var (name, resolvedData) in _resolved)
        {
            var canaries = new List<CanaryStats>(resolvedData.CanaryInfos.Count);

            foreach (var canaryInfos in resolvedData.CanaryInfos.Concat(resolvedData.CanaryInfos.SelectMany(x => x.Subcanaries)))
            {
                var commands = new List<CanaryCommandStats>();

                foreach (var command in canaryInfos.Commands)
                {
                    commands.Add(new CanaryCommandStats(command.Aliases.First()));
                }
                
                canaries.Add(new CanaryStats(canaryInfos.Name, commands));
            }

            toReturn.Add(new MarmaladeStats(name, resolvedData.Strings.GetDescription(culture), canaries));
        }
        return toReturn;
    }

    public async Task OnReadyAsync()
    {
        foreach (var name in _marmaladeConfig.GetLoadedMarmalades())
        {
            var result = await InternalLoadAsync(name);
            if(result != MarmaladeLoadResult.Success)
                Log.Warning("Unable to load '{MarmaladeName}' marmalade", name);
            else 
                Log.Warning("Loaded marmalade '{MarmaladeName}'", name);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<MarmaladeLoadResult> LoadMarmaladeAsync(string marmaladeName)
    {
        // try loading on this shard first to see if it works
        var res = await InternalLoadAsync(marmaladeName);
        if (res == MarmaladeLoadResult.Success)
        {
            // if it does publish it so that other shards can load the marmalade too
            // this method will be ran twice on this shard but it doesn't matter as 
            // the second attempt will be ignored
            await _pubSub.Pub(_loadKey, marmaladeName);
        }

        return res;
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<MarmaladeUnloadResult> UnloadMarmaladeAsync(string marmaladeName)
    {
        var res = await InternalUnloadAsync(marmaladeName);
        if (res == MarmaladeUnloadResult.Success)
        {
            await _pubSub.Pub(_unloadKey, marmaladeName);
        }

        return res;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string[] GetCommandExampleArgs(string marmaladeName, string commandName, CultureInfo culture)
    {
        if (!_resolved.TryGetValue(marmaladeName, out var data))
            return Array.Empty<string>();

        return data.Strings.GetCommandStrings(commandName, culture).Args
               ?? data.CanaryInfos
                      .SelectMany(x => x.Commands)
                      .FirstOrDefault(x => x.Aliases.Any(alias
                          => alias.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)))
                      ?.OptionalStrings
                      .Args
               ?? new[] { string.Empty };
    }

    public Task ReloadStrings()
        => _pubSub.Pub(_stringsReload, true);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ReloadStringsSync()
    {
        foreach (var resolved in _resolved.Values)
        {
            resolved.Strings.Reload();
        }
    }
    
    private async Task ReloadStringsInternal()
    {
        await _lock.WaitAsync();
        try
        {
            ReloadStringsSync();
        }
        finally
        {
            _lock.Release();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string GetCommandDescription(string marmaladeName, string commandName, CultureInfo culture)
    {
        if (!_resolved.TryGetValue(marmaladeName, out var data))
            return string.Empty;

        return data.Strings.GetCommandStrings(commandName, culture).Desc
               ?? data.CanaryInfos
                      .SelectMany(x => x.Commands)
                      .FirstOrDefault(x => x.Aliases.Any(alias
                          => alias.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)))
                      ?.OptionalStrings
                      .Desc
               ?? string.Empty;
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private async ValueTask<MarmaladeLoadResult> InternalLoadAsync(string name)
    {
        if (_resolved.ContainsKey(name))
            return MarmaladeLoadResult.AlreadyLoaded;
        
        var safeName = Uri.EscapeDataString(name);
        name = name.ToLowerInvariant();

        await _lock.WaitAsync();
        try
        {
            if (LoadAssemblyInternal(safeName,
                    out var ctx,
                    out var canaryData,
                    out var services,
                    out var strings,
                    out var typeReaders))
            {
                var moduleInfos = new List<ModuleInfo>();

                LoadTypeReadersInternal(typeReaders);

                foreach (var point in canaryData)
                {
                    try
                    {
                        // initialize canary and subcanaries
                        await point.Instance.InitializeAsync();
                        foreach (var sub in point.Subcanaries)
                        {
                            await sub.Instance.InitializeAsync();
                        }

                        var module = await LoadModuleInternalAsync(name, point, strings, services);
                        moduleInfos.Add(module);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex,
                            "Error loading canaries {CanaryName}",
                            point.Name);
                    }
                }

                var execs = GetExecsInternal(canaryData, strings, services);
                await _behHandler.AddRangeAsync(execs);

                _resolved[name] = new(LoadContext: ctx,
                    ModuleInfos: moduleInfos.ToImmutableArray(),
                    CanaryInfos: canaryData.ToImmutableArray(),
                    strings,
                    typeReaders,
                    execs)
                {
                    Services = services
                };

                
                services = null;
                _marmaladeConfig.AddLoadedMarmalade(safeName);
                return MarmaladeLoadResult.Success;
            }

            return MarmaladeLoadResult.Empty;
        }
        catch (Exception ex) when (ex is FileNotFoundException or BadImageFormatException)
        {
            return MarmaladeLoadResult.NotFound;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred loading a marmalade");
            return MarmaladeLoadResult.UnknownError;
        }
        finally
        {
            _lock.Release();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private IReadOnlyCollection<ICustomBehavior> GetExecsInternal(IReadOnlyCollection<CanaryInfo> canaryData, IMarmaladeStrings strings, IServiceProvider services)
    {
        var behs = new List<ICustomBehavior>();
        foreach (var canary in canaryData)
        {
            behs.Add(new BehaviorAdapter(new(canary.Instance), strings, services));

            foreach (var sub in canary.Subcanaries)
            {
                behs.Add(new BehaviorAdapter(new(sub.Instance), strings, services));
            }
        }

        
        return behs;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void LoadTypeReadersInternal(Dictionary<Type, TypeReader> typeReaders)
    {
        var notAddedTypeReaders = new List<Type>();
        foreach (var (type, typeReader) in typeReaders)
        {
            // if type reader for this type already exists, it will not be replaced
            if (_cmdService.TypeReaders.Contains(type))
            {
                notAddedTypeReaders.Add(type);
                continue;
            }
    
            _cmdService.AddTypeReader(type, typeReader);
        }
    
        // remove the ones that were not added
        // to prevent them from being unloaded later
        // as they didn't come from this marmalade
        foreach (var toRemove in notAddedTypeReaders)
        {
            typeReaders.Remove(toRemove);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool LoadAssemblyInternal(
        string safeName,
        [NotNullWhen(true)] out WeakReference<MarmaladeAssemblyLoadContext>? ctxWr,
        [NotNullWhen(true)] out IReadOnlyCollection<CanaryInfo>? canaryData,
        out IServiceProvider services,
        out IMarmaladeStrings strings,
        out Dictionary<Type, TypeReader> typeReaders)
    {
        ctxWr = null;
        canaryData = null;
        
        var path = $"{BASE_DIR}/{safeName}/{safeName}.dll";
        strings = MarmaladeStrings.CreateDefault($"{BASE_DIR}/{safeName}");
        var ctx = new MarmaladeAssemblyLoadContext(Path.GetDirectoryName(path)!);
        var a = ctx.LoadFromAssemblyPath(Path.GetFullPath(path));
        var sis = LoadCanariesFromAssembly(a, out services);
        typeReaders = LoadTypeReadersFromAssembly(a, strings, services);

        if (sis.Count == 0)
        {
            return false;
        }

        ctxWr = new(ctx);
        canaryData = sis;
        
        return true;
    }

    private static readonly Type _paramParserType = typeof(ParamParser<>);
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private Dictionary<Type, TypeReader> LoadTypeReadersFromAssembly(
        Assembly assembly,
        IMarmaladeStrings strings,
        IServiceProvider services)
    {
        var paramParsers = assembly.GetExportedTypes()
                .Where(x => x.IsClass
                            && !x.IsAbstract
                            && x.BaseType is not null
                            && x.BaseType.IsGenericType
                            && x.BaseType.GetGenericTypeDefinition() == _paramParserType);

        var typeReaders = new Dictionary<Type, TypeReader>();
        foreach (var parserType in paramParsers)
        {
            var parserObj = ActivatorUtilities.CreateInstance(services, parserType);

            var targetType = parserType.BaseType!.GetGenericArguments()[0];
            var typeReaderInstance = (TypeReader)Activator.CreateInstance(
                typeof(ParamParserAdapter<>).MakeGenericType(targetType),
                args: new[] { parserObj, strings, services })!;
            
            typeReaders.Add(targetType, typeReaderInstance);
        }

        return typeReaders;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private async Task<ModuleInfo> LoadModuleInternalAsync(string marmaladeName, CanaryInfo canaryInfo, IMarmaladeStrings strings, IServiceProvider services)
    {
        var module = await _cmdService.CreateModuleAsync(canaryInfo.Instance.Prefix,
            CreateModuleFactory(marmaladeName, canaryInfo, strings, services));
        
        return module;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Action<ModuleBuilder> CreateModuleFactory(
        string marmaladeName,
        CanaryInfo canaryInfo,
        IMarmaladeStrings strings,
        IServiceProvider marmaladeServices)
        => mb =>
        {
            var m = mb.WithName(canaryInfo.Name);

            foreach (var cmd in canaryInfo.Commands)
            {
                m.AddCommand(cmd.Aliases.First(),
                    CreateCallback(cmd.ContextType,
                        new(canaryInfo),
                        new(cmd),
                        new(marmaladeServices),
                        strings),
                    CreateCommandFactory(marmaladeName, cmd));
            }

            foreach (var subInfo in canaryInfo.Subcanaries)
                m.AddModule(subInfo.Instance.Prefix, CreateModuleFactory(marmaladeName, subInfo, strings, marmaladeServices));
        };

#pragma warning disable IDE0090 // Use 'new(...)'
    private static readonly RequireContextAttribute _reqGuild = new RequireContextAttribute(ContextType.Guild);
#pragma warning restore IDE0090 // Use 'new(...)'
#pragma warning disable IDE0090 // Use 'new(...)'
    private static readonly RequireContextAttribute _reqDm = new RequireContextAttribute(ContextType.DM);
#pragma warning restore IDE0090 // Use 'new(...)'
    private Action<CommandBuilder> CreateCommandFactory(string marmaladeName, CanaryCommandData cmd)
        => (cb) =>
        {
            cb.AddAliases(cmd.Aliases.Skip(1).ToArray());

            if (cmd.ContextType == CommandContextType.Guild)
                cb.AddPrecondition(_reqGuild);
            else if (cmd.ContextType == CommandContextType.Dm)
                cb.AddPrecondition(_reqDm);

            cb.WithPriority(cmd.Priority);
            
            // using summary to save method name
            // method name is used to retrieve desc/usages
            cb.WithRemarks($"marmalade///{marmaladeName}");
            cb.WithSummary(cmd.MethodInfo.Name.ToLowerInvariant());
            
            foreach (var param in cmd.Parameters)
            {
                cb.AddParameter(param.Name, param.Type, CreateParamFactory(param));
            }
        };

    private Action<ParameterBuilder> CreateParamFactory(ParamData paramData)
        => (pb) =>
        {
            pb.WithIsMultiple(paramData.IsParams)
              .WithIsOptional(paramData.IsOptional)
              .WithIsRemainder(paramData.IsLeftover);
        };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Func<ICommandContext, object[], IServiceProvider, CommandInfo, Task> CreateCallback(
        CommandContextType contextType,
        WeakReference<CanaryInfo> canaryDataWr,
        WeakReference<CanaryCommandData> canaryCommandDataWr,
        WeakReference<IServiceProvider> marmaladeServicesWr,
        IMarmaladeStrings strings)
        => async (context, parameters, svcs, _) =>
        {
            if (!canaryCommandDataWr.TryGetTarget(out var cmdData)
                || !canaryDataWr.TryGetTarget(out var canaryData)
                || !marmaladeServicesWr.TryGetTarget(out var marmaladeServices))
            {
                Log.Warning("Attempted to run an unloaded canary's command");
                return;
            }
                
            var paramObjs = ParamObjs(contextType, cmdData, parameters, context, svcs, marmaladeServices, strings);

            try
            {
                var methodInfo = cmdData.MethodInfo;
                if (methodInfo.ReturnType == typeof(Task)
                    || (methodInfo.ReturnType.IsGenericType
                        && methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)))
                {
                    await (Task)methodInfo.Invoke(canaryData.Instance, paramObjs)!;
                }
                else if (methodInfo.ReturnType == typeof(ValueTask))
                {
                    await ((ValueTask)methodInfo.Invoke(canaryData.Instance, paramObjs)!).AsTask();
                }
                else // if (methodInfo.ReturnType == typeof(void))
                {
                    methodInfo.Invoke(canaryData.Instance, paramObjs);
                }
            }
            finally
            {
                paramObjs = null;
                cmdData = null;
                
                canaryData = null;
                marmaladeServices = null;
            }
        };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object[] ParamObjs(
        CommandContextType contextType,
        CanaryCommandData cmdData,
        object[] parameters,
        ICommandContext context,
        IServiceProvider svcs,
        IServiceProvider svcProvider,
        IMarmaladeStrings strings)
    {
        var extraParams = contextType == CommandContextType.Unspecified ? 0 : 1;
        extraParams += cmdData.InjectedParams.Count;

        var paramObjs = new object[parameters.Length + extraParams];

        var startAt = 0;
        if (contextType != CommandContextType.Unspecified)
        {
            paramObjs[0] = ContextAdapterFactory.CreateNew(context, strings, svcs);

            startAt = 1;
        }

        for (var i = 0; i < cmdData.InjectedParams.Count; i++)
        {
            var svc = svcProvider.GetService(cmdData.InjectedParams[i]);
            if (svc is null)
            {
                throw new ArgumentException($"Cannot inject a service of type {cmdData.InjectedParams[i]}");
            }

            paramObjs[i + startAt] = svc;

            svc = null;
        }

        startAt += cmdData.InjectedParams.Count;

        for (var i = 0; i < parameters.Length; i++)
            paramObjs[startAt + i] = parameters[i];
        
        return paramObjs;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private async Task<MarmaladeUnloadResult> InternalUnloadAsync(string name)
    {
        name = name.ToLowerInvariant();
        if (!_resolved.Remove(name, out var lsi))
            return MarmaladeUnloadResult.NotLoaded;

        await _lock.WaitAsync();
        try
        {
            UnloadTypeReaders(lsi.TypeReaders);

            foreach (var mi in lsi.ModuleInfos)
            {
                await _cmdService.RemoveModuleAsync(mi);
            }

            await _behHandler.RemoveRangeAsync(lsi.Execs);
            
            await DisposeCanaryInstances(lsi);

            var lc = lsi.LoadContext;

            // removing this line will prevent assembly from being unloaded quickly
            // as this local variable will be held for a long time potentially
            // due to how async works
            lsi.Services = null!;
            lsi = null;
            
            _marmaladeConfig.RemoveLoadedMarmalade(name);
            return UnloadInternal(lc)
                ? MarmaladeUnloadResult.Success
                : MarmaladeUnloadResult.PossiblyUnable;
        }
        finally
        {
            _lock.Release();
        }
    }

    private void UnloadTypeReaders(Dictionary<Type, TypeReader> valueTypeReaders)
    {
        foreach (var tr in valueTypeReaders)
        {
            _cmdService.TryRemoveTypeReader(tr.Key, false, out _);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private async Task DisposeCanaryInstances(ResolvedMarmalade marmalade)
    {
        foreach (var si in marmalade.CanaryInfos)
        {
            try
            {
                await si.Instance.DisposeAsync();
                foreach (var sub in si.Subcanaries)
                {
                    await sub.Instance.DisposeAsync();
                }  
            }
            catch (Exception ex)
            {
                Log.Warning(ex,
                    "Failed cleanup of Canary {CanaryName}. This marmalade might not unload correctly",
                    si.Instance.Name);
            }
        }

        // marmaladee = null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool UnloadInternal(WeakReference<MarmaladeAssemblyLoadContext> lsi)
    {
        UnloadContext(lsi);
        GcCleanup();

        return !lsi.TryGetTarget(out _);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void UnloadContext(WeakReference<MarmaladeAssemblyLoadContext> lsiLoadContext)
    {
        if(lsiLoadContext.TryGetTarget(out var ctx))
            ctx.Unload();
    }
    
    private void GcCleanup()
    {
        // cleanup
        for (var i = 0; i < 10; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCComplete();
            GC.Collect();
        }
    }

    private static readonly Type _canaryType = typeof(Canary);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private IServiceProvider LoadMarmaladeServicesInternal(Assembly a)
        => new ServiceCollection()
           .Scan(x => x.FromAssemblies(a)
                       .AddClasses(static x => x.WithAttribute<svcAttribute>(x => x.Lifetime == Lifetime.Transient))
                       .AsSelfWithInterfaces()
                       .WithTransientLifetime()
                       .AddClasses(static x => x.WithAttribute<svcAttribute>(x => x.Lifetime == Lifetime.Singleton))
                       .AsSelfWithInterfaces()
                       .WithSingletonLifetime())
           .BuildServiceProvider();


    [MethodImpl(MethodImplOptions.NoInlining)]
    public IReadOnlyCollection<CanaryInfo> LoadCanariesFromAssembly(Assembly a, out IServiceProvider services)
    {
        var marmaladeServices = LoadMarmaladeServicesInternal(a);
        services = new MarmaladeServiceProvider(_botServices, marmaladeServices);
        
        // find all types in teh assembly
        var types = a.GetExportedTypes();
        // canary is always a public non abstract class
        var classes = types.Where(static x => x.IsClass
                                              && (x.IsNestedPublic || x.IsPublic)
                                              && !x.IsAbstract
                                              && x.BaseType == _canaryType
                                              && (x.DeclaringType is null || x.DeclaringType.IsAssignableTo(_canaryType)))
                           .ToList();

        var topModules = new Dictionary<Type, CanaryInfo>();
        
        foreach (var cl in classes)
        {
            if (cl.DeclaringType is not null)
                continue;
            
            // get module data, and add it to the topModules dictionary
            var module = GetModuleData(cl, services);
            topModules.Add(cl, module);
        }

        foreach (var c in classes)
        {
            if (c.DeclaringType is not Type dt)
                continue;

            // if there is no top level module which this module is a child of
            // just print a warning and skip it
            if (!topModules.TryGetValue(dt, out var parentData))
            {
                Log.Warning("Can't load submodule {SubName} because parent module {Name} does not exist",
                    c.Name,
                    dt.Name);
                continue;
            }
            
            GetModuleData(c, services, parentData);
        }

        return topModules.Values.ToArray();
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private CanaryInfo GetModuleData(Type type, IServiceProvider services, CanaryInfo? parentData = null)
    {
        var filters = type.GetCustomAttributes<FilterAttribute>(true)
                          .ToArray();
        
        var instance = (Canary)ActivatorUtilities.CreateInstance(services, type);
        
        var module = new CanaryInfo(instance.Name,
            parentData,
            instance,
            GetCommands(instance, type),
            filters);

        if (parentData is not null)
            parentData.Subcanaries.Add(module);

        return module;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private IReadOnlyCollection<CanaryCommandData> GetCommands(Canary instance, Type type)
    {
        var methodInfos = type
                          .GetMethods(BindingFlags.Instance
                                      | BindingFlags.DeclaredOnly
                                      | BindingFlags.Public)
                          .Where(static x =>
                          {
                              if(x.GetCustomAttribute<cmdAttribute>(true) is null)
                                  return false;

                              if (x.ReturnType.IsGenericType)
                              {
                                  var genericType = x.ReturnType.GetGenericTypeDefinition();
                                  if (genericType == typeof(Task<>))
                                      return true;
                              
                                  // if (genericType == typeof(ValueTask<>))
                                  //     return true;

                                  Log.Warning("Method {MethodName} has an invalid return type: {ReturnType}",
                                      x.Name,
                                      x.ReturnType);
                                  
                                  return false;
                              }

                              var succ = x.ReturnType == typeof(Task)
                                         || x.ReturnType == typeof(ValueTask)
                                         || x.ReturnType == typeof(void);

                              if (!succ)
                              {
                                  Log.Warning("Method {MethodName} has an invalid return type: {ReturnType}",
                                      x.Name,
                                      x.ReturnType);
                              }

                              return succ;
                          });
        
        
        var cmds = new List<CanaryCommandData>();
        foreach (var method in methodInfos)
        {
            var filters = method.GetCustomAttributes<FilterAttribute>().ToArray();
            var prio = method.GetCustomAttribute<prioAttribute>()?.Priority ?? 0;

            var paramInfos = method.GetParameters();
            var cmdParams = new List<ParamData>();
            var diParams = new List<Type>();
            var cmdContext = CommandContextType.Unspecified;
            var canInject = false;
            for (var paramCounter = 0; paramCounter < paramInfos.Length; paramCounter++)
            {
                var pi = paramInfos[paramCounter];

                var paramName = pi.Name ?? "unnamed";
                var isContext = paramCounter == 0 && pi.ParameterType.IsAssignableTo(typeof(AnyContext));

                var leftoverAttribute = pi.GetCustomAttribute<leftoverAttribute>(true);
                var hasDefaultValue = pi.HasDefaultValue;
                var isLeftover = leftoverAttribute != null;
                var isParams = pi.GetCustomAttribute<ParamArrayAttribute>() is not null;
                var paramType = pi.ParameterType;
                var isInjected = pi.GetCustomAttribute<injectAttribute>(true) is not null;

                if (isContext)
                {
                    if (hasDefaultValue || leftoverAttribute != null || isParams)
                        throw new ArgumentException("IContext parameter cannot be optional, leftover, constant or params. " + GetErrorPath(method, pi));

                    if (paramCounter != 0)
                        throw new ArgumentException($"IContext parameter has to be first. {GetErrorPath(method, pi)}");

                    canInject = true;
                    
                    if (paramType.IsAssignableTo(typeof(GuildContext)))
                        cmdContext = CommandContextType.Guild;
                    else if (paramType.IsAssignableTo(typeof(DmContext)))
                        cmdContext = CommandContextType.Dm;
                    else
                        cmdContext = CommandContextType.Any;
                    
                    continue;
                }

                if (isInjected)
                {
                    if (!canInject && paramCounter != 0)
                        throw new ArgumentException($"Parameters marked as [Injected] have to come after IContext");

                    canInject = true;
                    
                    diParams.Add(paramType);
                    continue;
                }

                canInject = false;

                if (isParams)
                {
                    if (hasDefaultValue)
                        throw new NotSupportedException("Params can't have const values at the moment. "
                                                        + GetErrorPath(method, pi));
                    // if it's params, it means it's an array, and i only need a parser for the actual type,
                    // as the parser will run on each array element, it can't be null
                    paramType = paramType.GetElementType()!;
                }

                // leftover can only be the last parameter.
                if (isLeftover && paramCounter != paramInfos.Length - 1)
                {
                    var path = GetErrorPath(method, pi);
                    Log.Error("Only one parameter can be marked [Leftover] and it has to be the last one. {Path} ",
                        path);
                    throw new ArgumentException("Leftover attribute error.");
                }

                cmdParams.Add(new ParamData(paramType, paramName, hasDefaultValue, isLeftover, isParams));
            }


            var cmdAttribute = method.GetCustomAttribute<cmdAttribute>()!; 
            var aliases = cmdAttribute.Aliases;
            if (aliases.Length == 0)
                aliases = new[] { method.Name.ToLowerInvariant() };
            
            cmds.Add(new(
                aliases,
                method,
                instance,
                filters,
                cmdContext,
                diParams,
                cmdParams,
                new(cmdAttribute.desc, cmdAttribute.args),
                prio
            ));
        }

        return cmds;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private string GetErrorPath(MethodInfo m, System.Reflection.ParameterInfo pi)
        => $@"Module: {m.DeclaringType?.Name} 
Command: {m.Name}
ParamName: {pi.Name}
ParamType: {pi.ParameterType.Name}";
}

public enum MarmaladeLoadResult
{
    Success,
    NotFound,
    AlreadyLoaded,
    Empty,
    UnknownError,
}

public enum MarmaladeUnloadResult
{
    Success,
    NotLoaded,
    PossiblyUnable,
    NotFound,
}