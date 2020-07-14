export interface ScriptResult {
	/**
	 * Contains the optional output of the script.
	 */
	readonly result: any;
	/**
	 * Contains the text of the standard output from running the script.
	 */
	readonly stdOut: string;
	/**
	 * Contains any errors that happened while trying to execute the script.
	 */
	readonly stdErr: string;
}