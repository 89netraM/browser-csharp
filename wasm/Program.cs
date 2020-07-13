using System;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace BrowserCSharp
{
	public class Program
	{
		public static void Main(string[] args)
		{
			WebAssemblyHostBuilder.CreateDefault(args);
			Console.WriteLine("Hello from C#");
		}
	}
}