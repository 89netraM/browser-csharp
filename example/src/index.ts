import { BrowserCSharp } from "browser-csharp";

let output: HTMLOListElement;
let input: HTMLInputElement;

function init(): void {
	output = document.getElementById("output") as HTMLOListElement;
	input = document.getElementById("input") as HTMLInputElement;
	input.addEventListener("keypress", onInputKey, false);
}

async function onInputKey(e: KeyboardEvent): Promise<void> {
	if (e.key === "Enter" && (input.value != null && input.value.length > 0)) {
		const code = input.value;
		log(code, "in");

		input.value = "";
		const ogPlaceholder = input.placeholder;
		input.placeholder = "Executing...";
		input.disabled = true;

		await runCode(code);
		
		input.placeholder = ogPlaceholder;
		input.disabled = false;
		input.focus();
	}
}

async function runCode(code: string): Promise<void> {
	const result = await BrowserCSharp.ExecuteScript(code, "contextId");
	if (result.stdErr == null) {
		if (result.stdOut != null) {
			result.stdOut.split("\n").forEach(o => log(o));
		}
		if (result.result != null) {
			log(result.result, "out");
		}
	}
	else {
		result.stdErr.split("\n").forEach(o => log(o, "error"));
	}
}

function log(content: string, type?: "in" | "out" | "error"): void {
	const li = document.createElement("li");
	if (type != null) {
		li.classList.add(type);
	}
	li.innerText = content;
	output.appendChild(li);
	li.scrollIntoView();
}

window.addEventListener("load", init, false);

BrowserCSharp.OnReady(success => {
	if (success) {
		input.placeholder = "Write C# code and press enter";
	}
	else {
		input.placeholder = "Failed to load runtime dependencies";
	}
});