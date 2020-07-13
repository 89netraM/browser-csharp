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
			"System"
		);
		private static readonly IEnumerable<string> defaultUsings = new[]
		{
			"System"
		};

		private static Task<PortableExecutableReference[]> loadedReferences;

		public static void Main()
		{
			WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault();
			loadedReferences = GetReferences(builder.HostEnvironment.BaseAddress);
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
			CompilationResult compilationResult = await CompileScript(code);

			if (compilationResult.Success)
			{
				return await RunScript(compilationResult.Assembly, compilationResult.Compilation);
			}
			else
			{
				return new ExecutionResult(null, null, String.Join('\n', compilationResult.Errors.Select(x => x.GetMessage())));
			}
		}

		private static async Task<CompilationResult> CompileScript(string code)
		{
			CSharpCompilation compilation = CSharpCompilation.CreateScriptCompilation(
				Path.GetRandomFileName(),
				CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithKind(SourceCodeKind.Script).WithLanguageVersion(LanguageVersion.Preview)),
				await loadedReferences,
				new CSharpCompilationOptions(outputKind: OutputKind.DynamicallyLinkedLibrary, usings: defaultUsings)
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

		private static async Task<ExecutionResult> RunScript(Assembly assembly, Compilation compilation)
		{
			IMethodSymbol entryPoint = compilation.GetEntryPoint(CancellationToken.None);
			Type type = assembly.GetType($"{entryPoint.ContainingNamespace.MetadataName}.{entryPoint.ContainingType.MetadataName}"); ;
			MethodInfo entryPointMethod = type.GetMethod(entryPoint.MetadataName);

			TextWriter ogOut = Console.Out;
			using StringWriter sw = new StringWriter();
			Console.SetOut(sw);

			Func<object[], Task<object>> submission = (Func<object[], Task<object>>)entryPointMethod.CreateDelegate(typeof(Func<object[], Task<object>>));
			object result = await submission.Invoke(new object[] { null, null });

			Console.SetOut(ogOut);

			string stdOut = sw.ToString();
			return new ExecutionResult(result, stdOut.Length > 0 ? stdOut : null, null);
		}
	}
}