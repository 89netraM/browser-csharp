# Browser C#

Executes C# snippets in the browser.

## Installation

Install using `npm`:
```
npm install browser-csharp
```

The WASM files for .NET **must** be included as static assets in the correct
way:

1. The folder `./node_modules/browser-csharp/out/_framework` **must** be
   included as `./_framework` relative to the `<base/>` of your HTML file.
2. You **must** include a reference to the WASM .NET entry file:  
   `<script src="_framework/blazor.webassembly.js"></script>`

After doing this you can include the client library in JS/TS with a regular
`require`/`import`.

## Usage

Include a reference to the library:
```TypeScript
const BrowserCSharp = require("browser-csharp").BrowserCSharp; // JS
import { BrowserCSharp } from "browser-csharp";                // TS
```

Wait for the WASM library to initiate:
```TypeScript
BrowserCSharp.OnReady(success => { /* Callback */ });
```

Run a code snippet:
```TypeScript
BrowserCSharp.ExecuteScript(`Console.WriteLine("Hello from C#");`)
.then(response => {
	response.stdErr // String with errors (parsing and compilation)
	response.stdOut // String with C# output (runtime)
	response.result // The return value of the the expression
});
```

With an extra argument (a string) the code snippet can be run in the context of
previous code snippets, that was run with the same context id.  
This is good for creating REPLs.
```TypeScript
BrowserCSharp.ExecuteScript(codeSnippet, contextId)
```
The return is the same, a `Promise` that resolves with an object of the same
shape.

## Example

For an example of what this package can do, look in the [`./example`](https://github.com/89netraM/browser-csharp/example/) folder for the code and [here](https://åsberg.net/browser-csharp/) for a live demo.

## Development

This package requires that .NET Core is installed for development. [Download the
SDK here](https://dotnet.microsoft.com/download).

To test the wasm part during development run `dotnet run` in the wasm directory
to start a dev server. It will only include the wasm part, so use the following
snippet to invoke the code.

```JavaScript
DotNet.invokeMethodAsync(
	"BrowserCSharp", "ExecuteScript",
	`String.Concat("apa".Select(c=>(char)(c+1)))`
).then(console.log)
```

There's not much to test with the TypeScript part, but you can build it alone
with `npx gulp ts-build`.

Building everything at once can be done with `npx gulp ts-build wasm-build`, or
`npm run prepack` 🤷‍♀️.