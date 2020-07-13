using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace BrowserCSharp
{
	readonly struct CompilationResult
	{
		public Assembly Assembly { get; }
		public Compilation Compilation { get; }
		public IEnumerable<Diagnostic> Errors { get; }
		public bool Success { get; }

		public CompilationResult(Assembly assembly, Compilation compilation)
		{
			Assembly = assembly;
			Compilation = compilation;
			Errors = null;
			Success = true;
		}
		public CompilationResult(IEnumerable<Diagnostic> errors)
		{
			Assembly = null;
			Compilation = null;
			Errors = errors;
			Success = false;
		}
	}
}