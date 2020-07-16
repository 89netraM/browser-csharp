# Browser C#

## Example

For an example of what this package can do, look in the [`example`](./example) folder for the code and [here](https://åsberg.net/browser-csharp/) for a live version.

## Development

This package requires that .NET Core is installed for development. Download the
SDK here: [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download).

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