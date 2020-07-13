using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.CodeAnalysis;

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
	}
}