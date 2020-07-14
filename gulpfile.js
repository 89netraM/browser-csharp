const del = require("del");
const { series, src, dest, parallel } = require("gulp");
const { exec } = require("child_process");
const path = require("path");
const { createProject } = require("gulp-typescript");
const merge = require("merge2");

const wasmProj = "./wasm/BrowserCSharp.csproj";
const wasmSrc = "./wasm/bin/Release/netstandard2.1/publish/wwwroot/_framework"
const wasmDest = "./out/_framework";

const tsConfig = "./tsconfig.json"
const tsDest = "./out/"
const tsTypings = "./typings";

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

const tsClean = parallel(
	() => del(path.join(tsDest, "*.js"), { force: true }),
	() => del(tsTypings, { force: true })
);
exports["ts-clean"] = tsClean;

const tsBuild = series(
	tsClean,
	() => {
		const tsProj = createProject(tsConfig);
		const tsResult = tsProj.src().pipe(tsProj());

		return merge(
			tsResult.js.pipe(dest(tsDest)),
			tsResult.dts.pipe(dest(tsTypings))
		);
	}
);
exports["ts-build"] = tsBuild;