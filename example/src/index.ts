import { BrowserCSharp } from "browser-csharp";

let output: HTMLOListElement;
let input: HTMLInputElement;

const history = new Array<string>();
let historyIndex = -1;
function moveHistory(direction: -1 | 1): string {
	historyIndex = Math.max(-1, Math.min(historyIndex + direction, history.length - 1));

	if (historyIndex === -1) {
		return "";
	}
	else {
		return history[historyIndex];
	}
}

function init(): void {
	output = document.getElementById("output") as HTMLOListElement;
	input = document.getElementById("input") as HTMLInputElement;
	input.addEventListener("keydown", onInputKey, false);
}

async function onInputKey(e: KeyboardEvent): Promise<void> {
	function arrowValue(key: string): -1 | 0 | 1 {
		if (key === "ArrowUp") {
			return 1;
		}
		else if (key === "ArrowDown") {
			return -1;
		}
		else {
			return 0;
		}
	}

	if (e.key === "Enter" && (input.value != null && input.value.length > 0)) {
		const code = input.value;
		log(code, "in");
		historyIndex = -1;

		input.value = "";
		const ogPlaceholder = input.placeholder;
		input.placeholder = "Executing...";
		input.disabled = true;

		await runCode(code);
		
		input.placeholder = ogPlaceholder;
		input.disabled = false;
		input.focus();
	}
	else {
		const direction = arrowValue(e.key);
		if (direction !== 0) {
			input.value = moveHistory(direction);
			input.selectionStart = input.selectionEnd = input.value.length;
			e.preventDefault();
		}
		else {
			historyIndex = -1;
		}
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

	if (type === "in" && history[0] !== content) {
		history.unshift(content);
	}
}

window.addEventListener("load", init, false);

BrowserCSharp.OnReady(success => {
	if (success) {
		input.placeholder = "Write C# code and press enter";
		input.disabled = false;
	}
	else {
		input.placeholder = "Failed to load runtime dependencies";
	}
});