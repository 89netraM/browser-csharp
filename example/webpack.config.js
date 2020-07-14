const path = require("path");
const CopyPlugin = require("copy-webpack-plugin");
const HtmlPlugin = require("html-webpack-plugin");

module.exports = {
	entry: path.resolve(__dirname, "src/index.ts"),
	mode: "development",
	module: {
		rules: [
			{
				test: /\.ts$/,
				use: "ts-loader"
			}
		]
	},
	resolve: {
		extensions: [ ".ts", ".js" ]
	},
	plugins: [
		new CopyPlugin({
			patterns: [
				{
					// The important bits. The _framework folder needs to be
					// included as a static asset.
					from: path.resolve(__dirname, "node_modules/browser-csharp/out/_framework"),
					to: "_framework"
				}
			]
		}),
		new HtmlPlugin({
			template: path.resolve(__dirname, "index.html"),
			title: "Browser C# Example",
			base: "/"
		})
	],
	output: {
		filename: "main.js",
		path: path.resolve(__dirname, "dist")
	},
	devServer: {
		contentBase: path.resolve(__dirname, "dist"),
		port: 9090
	}
};