const del = require("del");
const { series, src, dest } = require("gulp");
const { exec } = require("child_process");

const wasmProj = "./wasm/BrowserCSharp.csproj";
const wasmSrc = "./wasm/bin/Release/netstandard2.1/publish/wwwroot/_framework"
const wasmDest = "./out/_framework";

const wasmClean = () => {
	return del(wasmDest, { force: true });
};
exports["wasm-clean"] = wasmClean;

const wasmMove = () => {
	return src("**/*", { cwd: wasmSrc })
		.pipe(dest(wasmDest));
};
exports["wasm-move"] = wasmMove;

const wasmBuild = series(
	wasmClean,
	cb => {
		exec(
			`dotnet publish -c Release ${wasmProj}`,
			(err, stdout, stderr) => {
				console.log(stdout);
				console.error(stderr);
				cb(err);
			}
		);
	},
	wasmMove
);
exports["wasm-build"] = wasmBuild;