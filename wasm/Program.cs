using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.JSInterop;

namespace BrowserCSharp
{
	public static class Program
	{
		private const string frameworkBinUri = "_framework/_bin";
		private static readonly IImmutableSet<string> references = ImmutableHashSet.Create(
			"mscorlib",
			"netstandard",
			"System",
			"System.Core"
		);
		private static readonly IEnumerable<string> defaultUsings = new[]
		{
			"System",
			"System.Linq"
		};

		private static Task<PortableExecutableReference[]> loadedReferences;

		private static IJSRuntime jsRuntime;

		private static IDictionary<string, ScriptContext> previousCompilations = new Dictionary<string, ScriptContext>();

		public static void Main()
		{
			WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault();
			WebAssemblyHost host = builder.Build();
			jsRuntime = (IJSRuntime)host.Services.GetService(typeof(IJSRuntime));

			loadedReferences = GetReferences(builder.HostEnvironment.BaseAddress);
			loadedReferences.GetAwaiter().OnCompleted(notifyJS);

			static void notifyJS()
			{
				if (loadedReferences.IsCompletedSuccessfully)
				{
					jsRuntime.InvokeVoidAsync("BrowserCSharp.loaded");
				}
				else
				{
					jsRuntime.InvokeVoidAsync("BrowserCSharp.failed");
				}
			}
		}

		private static Task<PortableExecutableReference[]> GetReferences(string baseUri)
		{
			static PortableExecutableReference toReference(Task<Stream> completedTask)
			{
				if (completedTask.IsCompletedSuccessfully)
				{
					return MetadataReference.CreateFromStream(completedTask.Result);
				}
				else
				{
					throw new Exception("Could not load a reference required for runtime compilation.", completedTask.Exception);
				}
			}

			Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
			HttpClient client = new HttpClient();
			client.BaseAddress = new Uri(baseUri);

			IDictionary<string, Task<PortableExecutableReference>> foundReferences = new Dictionary<string, Task<PortableExecutableReference>>();

			foreach (Assembly assembly in loadedAssemblies)
			{
				string name = assembly.GetName().Name;
				if (references.Contains(name) && !foundReferences.ContainsKey(name))
				{
					Task<PortableExecutableReference> task = client.GetStreamAsync(Path.Join(frameworkBinUri, assembly.Location)).ContinueWith(toReference);
					foundReferences.Add(name, task);
				}
			}

			if (references.All(foundReferences.ContainsKey))
			{
				Task<PortableExecutableReference[]> allTask = Task.WhenAll(foundReferences.Values);
				allTask.GetAwaiter().OnCompleted(client.Dispose);
				return allTask;
			}
			else
			{
				client.Dispose();
				return Task.FromException<PortableExecutableReference[]>(
					new Exception("Could not find all required references for runtime compilation. " +
						$"Missing references: {String.Join(", ", references.Except(foundReferences.Keys))}")
				);
			}
		}

		[JSInvokable]
		public static async Task<ExecutionResult> ExecuteScript(string code)
		{
			CompilationResult compilationResult = await CompileScript(code).ConfigureAwait(false);

			if (compilationResult.Success)
			{
				return await RunScript(compilationResult.Assembly, compilationResult.Compilation).ConfigureAwait(false);
			}
			else
			{
				return new ExecutionResult(null, null, String.Join('\n', compilationResult.Errors.Select(x => x.GetMessage())));
			}
		}

		[JSInvokable]
		public static async Task<ExecutionResult> ExecuteScriptInContext(string code, string contextId)
		{
			ScriptContext context = await Task.Run(() => previousCompilations.TryGetValue(contextId, out ScriptContext c) ? c : ScriptContext.Empty).ConfigureAwait(false);
			CompilationResult compilationResult = await CompileScript(code, context).ConfigureAwait(false);

			if (compilationResult.Success)
			{
				context = context.AddCompilation(compilationResult.Compilation);
				previousCompilations[contextId] = context;
				return await RunScript(compilationResult.Assembly, compilationResult.Compilation, context.States).ConfigureAwait(false);
			}
			else
			{
				return new ExecutionResult(null, null, String.Join('\n', compilationResult.Errors.Select(x => x.GetMessage())));
			}
		}

		private static async Task<CompilationResult> CompileScript(string code, ScriptContext? context = null)
		{
			CSharpCompilation compilation = CSharpCompilation.CreateScriptCompilation(
				Path.GetRandomFileName(),
				CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithKind(SourceCodeKind.Script).WithLanguageVersion(LanguageVersion.Preview)),
				await loadedReferences.ConfigureAwait(false),
				new CSharpCompilationOptions(outputKind: OutputKind.DynamicallyLinkedLibrary, usings: defaultUsings),
				context?.Compilation
			);

			IEnumerable<Diagnostic> parsingErrors = compilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error);
			if (parsingErrors.Any())
			{
				return new CompilationResult(parsingErrors);
			}
			else
			{
				using MemoryStream ms = new MemoryStream();
				EmitResult result = compilation.Emit(ms);

				if (result.Success)
				{
					return new CompilationResult(Assembly.Load(ms.ToArray()), compilation);
				}
				else
				{
					return new CompilationResult(result.Diagnostics);
				}
			}
		}

		private static Task<ExecutionResult> RunScript(Assembly assembly, Compilation compilation)
		{
			return RunScript(assembly, compilation, new object[] { null, null });
		}
		private static async Task<ExecutionResult> RunScript(Assembly assembly, Compilation compilation, object[] states)
		{
			IMethodSymbol entryPoint = compilation.GetEntryPoint(CancellationToken.None);
			Type type = assembly.GetType($"{entryPoint.ContainingNamespace.MetadataName}.{entryPoint.ContainingType.MetadataName}"); ;
			MethodInfo entryPointMethod = type.GetMethod(entryPoint.MetadataName);

			TextWriter ogOut = Console.Out;
			using StringWriter sw = new StringWriter();
			Console.SetOut(sw);

			Func<object[], Task<object>> submission = (Func<object[], Task<object>>)entryPointMethod.CreateDelegate(typeof(Func<object[], Task<object>>));
			object result = await submission.Invoke(states).ConfigureAwait(false);

			Console.SetOut(ogOut);

			string stdOut = sw.ToString();
			return new ExecutionResult(result, stdOut.Length > 0 ? stdOut : null, null);
		}
	}
}