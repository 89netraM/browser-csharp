import { ScriptResult } from "./ScriptResult";

declare namespace DotNet {
	function invokeMethodAsync(namespace: string, method: string, ...args: Array<any>): Promise<any>;
}

export namespace BrowserCSharp {
	const namespaceName = "BrowserCSharp";
	const executeName = "ExecuteScript";
	const executeInContextName = "ExecuteScriptInContext";

	/**
	 * Executes the provided C# code.
	 * @param code C# code.
	 */
	export function ExecuteScript(code: string): Promise<ScriptResult>;
	/**
	 * Executes the provided C# code in the context of previous executions
	 * with the same `contextId`.
	 * If no previous executions have taken place, a new context will be
	 * created for the provided `contextId`.
	 *
	 * Useful for creating REPLs.
	 *
	 * @param code      C# code.
	 * @param contextId A string id for the context.
	 */
	export function ExecuteScript(code: string, contextId: string): Promise<ScriptResult>;
	export function ExecuteScript(code: string, contextId?: string): Promise<ScriptResult> {
		if (contextId != null) {
			return DotNet.invokeMethodAsync(namespaceName, executeInContextName, code, contextId);
		}
		else {
			return DotNet.invokeMethodAsync(namespaceName, executeName, code);
		}
	}
}

export { ScriptResult };