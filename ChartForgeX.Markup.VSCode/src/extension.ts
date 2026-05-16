import * as vscode from 'vscode';
import { spawn } from 'child_process';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';

const allowedCliArtifactNames = new Set([
  'chartforgex.markup.cli',
  'chartforgex.markup.cli.csproj',
  'chartforgex.markup.cli.dll',
  'chartforgex.markup.cli.exe'
]);

type CliResult = {
  stdout: string;
  stderr: string;
  code: number | null;
  spawnError?: boolean;
};

type CliCommand = {
  command: string;
  args: string[];
  fallbacks?: CliCommand[];
};

type ExportFormat = 'svg' | 'png' | 'html';

let diagnostics: vscode.DiagnosticCollection;
const validationTimers = new Map<string, ReturnType<typeof setTimeout>>();
const previewTimers = new Map<string, ReturnType<typeof setTimeout>>();
const previewPanels = new Map<string, vscode.WebviewPanel>();

export function activate(context: vscode.ExtensionContext): void {
  diagnostics = vscode.languages.createDiagnosticCollection('chartforgex-markup');
  context.subscriptions.push(diagnostics);

  context.subscriptions.push(vscode.commands.registerCommand('chartforgexMarkup.preview', (resource?: vscode.Uri) => previewActiveDocument(context, resource)));
  context.subscriptions.push(vscode.commands.registerCommand('chartforgexMarkup.validate', (resource?: vscode.Uri) => validateActiveDocument(context, true, resource)));
  context.subscriptions.push(vscode.commands.registerCommand('chartforgexMarkup.exportSvg', (resource?: vscode.Uri) => exportActiveDocument(context, 'svg', resource)));
  context.subscriptions.push(vscode.commands.registerCommand('chartforgexMarkup.exportPng', (resource?: vscode.Uri) => exportActiveDocument(context, 'png', resource)));
  context.subscriptions.push(vscode.commands.registerCommand('chartforgexMarkup.exportHtml', (resource?: vscode.Uri) => exportActiveDocument(context, 'html', resource)));
  context.subscriptions.push(vscode.commands.registerCommand('chartforgexMarkup.emitCSharp', (resource?: vscode.Uri) => emitCSharp(context, resource)));
  context.subscriptions.push(vscode.commands.registerCommand('chartforgexMarkup.emitCSharpToFile', (resource?: vscode.Uri) => emitCSharpToFile(context, resource)));
  context.subscriptions.push(vscode.commands.registerCommand('chartforgexMarkup.openOutputFolder', (resource?: vscode.Uri) => openOutputFolder(resource)));

  context.subscriptions.push(vscode.workspace.onDidOpenTextDocument((document) => scheduleValidation(context, document)));
  context.subscriptions.push(vscode.workspace.onDidSaveTextDocument((document) => scheduleValidation(context, document, 0)));
  context.subscriptions.push(vscode.workspace.onDidChangeTextDocument((event) => {
    scheduleValidation(context, event.document);
    schedulePreviewRefresh(context, event.document);
  }));
  context.subscriptions.push(vscode.workspace.onDidSaveTextDocument((document) => schedulePreviewRefresh(context, document, 0)));

  for (const document of vscode.workspace.textDocuments) {
    scheduleValidation(context, document, 0);
  }
}

export function deactivate(): void {
  diagnostics?.dispose();
  validationTimers.forEach((timer) => clearTimeout(timer));
  previewTimers.forEach((timer) => clearTimeout(timer));
  validationTimers.clear();
  previewTimers.clear();
  previewPanels.clear();
}

async function previewActiveDocument(context: vscode.ExtensionContext, resource?: vscode.Uri): Promise<void> {
  const document = await activeMarkupDocument(resource);
  if (!document) {
    return;
  }

  const key = document.uri.toString();
  let panel = previewPanels.get(key);
  if (!panel) {
    panel = vscode.window.createWebviewPanel(
      'chartforgexMarkupPreview',
      `ChartForgeX Preview: ${path.basename(document.fileName)}`,
      vscode.ViewColumn.Beside,
      {
        enableScripts: false,
        retainContextWhenHidden: true,
        localResourceRoots: [vscode.Uri.file(path.dirname(document.fileName))]
      }
    );
    panel.onDidDispose(() => {
      previewPanels.delete(key);
      const timer = previewTimers.get(key);
      if (timer) clearTimeout(timer);
      previewTimers.delete(key);
    });
    previewPanels.set(key, panel);
  }

  await refreshPreview(context, document, panel);
}

async function refreshPreview(context: vscode.ExtensionContext, document: vscode.TextDocument, panel: vscode.WebviewPanel): Promise<void> {
  const requestVersion = document.version;
  if (!isChartForgeXMarkup(document)) {
    panel.webview.html = renderMessage('No chartforgex topology block was found in this document.');
    return;
  }

  const result = await runCli(context, document, 'preview');
  if (document.version !== requestVersion) {
    return;
  }

  if (result.code !== 0) {
    panel.webview.html = renderMessage(escapeHtml(result.stderr || result.stdout || 'Preview failed.'));
    return;
  }

  panel.webview.html = result.stdout;
}

async function validateActiveDocument(context: vscode.ExtensionContext, showStatus: boolean, resource?: vscode.Uri): Promise<boolean> {
  const document = await activeMarkupDocument(resource);
  if (!document) {
    return false;
  }

  const requestVersion = document.version;
  if (!isChartForgeXMarkup(document)) {
    diagnostics.delete(document.uri);
    return false;
  }

  const result = await runCli(context, document, 'validate');
  if (document.version !== requestVersion) {
    return false;
  }

  applyDiagnostics(document, result);
  if (showStatus) {
    if (result.code === 0) {
      vscode.window.showInformationMessage('ChartForgeX markup is valid.');
    } else {
      vscode.window.showErrorMessage('ChartForgeX markup validation failed.');
    }
  }

  return result.code === 0;
}

async function exportActiveDocument(context: vscode.ExtensionContext, format: ExportFormat, resource?: vscode.Uri): Promise<void> {
  const document = await activeMarkupDocument(resource);
  if (!document) {
    return;
  }

  const defaultUri = vscode.Uri.file(defaultOutputPath(document, format));
  const target = await vscode.window.showSaveDialog({
    defaultUri,
    filters: { [format.toUpperCase()]: [format] }
  });
  if (!target) {
    return;
  }

  const result = await runCli(context, document, 'export', ['--output', target.fsPath]);
  if (result.code === 0) {
    vscode.window.showInformationMessage(`ChartForgeX markup exported to ${target.fsPath}.`);
  } else {
    vscode.window.showErrorMessage(result.stderr || result.stdout || 'ChartForgeX export failed.');
  }
}

async function emitCSharp(context: vscode.ExtensionContext, resource?: vscode.Uri): Promise<void> {
  const document = await activeMarkupDocument(resource);
  if (!document) {
    return;
  }

  const result = await runCli(context, document, 'emit', ['--target', 'csharp']);
  if (result.code !== 0) {
    vscode.window.showErrorMessage(result.stderr || result.stdout || 'C# generation failed.');
    return;
  }

  const generated = await vscode.workspace.openTextDocument({ language: 'csharp', content: result.stdout });
  await vscode.window.showTextDocument(generated, vscode.ViewColumn.Beside);
}

async function emitCSharpToFile(context: vscode.ExtensionContext, resource?: vscode.Uri): Promise<void> {
  const document = await activeMarkupDocument(resource);
  if (!document) {
    return;
  }

  const target = await vscode.window.showSaveDialog({
    defaultUri: vscode.Uri.file(defaultOutputPath(document, 'cs')),
    filters: { 'C#': ['cs'] }
  });
  if (!target) {
    return;
  }

  const result = await runCli(context, document, 'emit', ['--target', 'csharp', '--output', target.fsPath]);
  if (result.code === 0) {
    vscode.window.showInformationMessage(`ChartForgeX C# generated at ${target.fsPath}.`);
  } else {
    vscode.window.showErrorMessage(result.stderr || result.stdout || 'C# generation failed.');
  }
}

async function openOutputFolder(resource?: vscode.Uri): Promise<void> {
  const document = await activeMarkupDocument(resource);
  if (!document) {
    return;
  }

  const folder = outputDirectory(document);
  fs.mkdirSync(folder, { recursive: true });
  await vscode.commands.executeCommand('revealFileInOS', vscode.Uri.file(folder));
}

function scheduleValidation(context: vscode.ExtensionContext, document: vscode.TextDocument, delay?: number): void {
  if (!couldBeMarkupDocument(document)) {
    return;
  }

  const key = document.uri.toString();
  const existing = validationTimers.get(key);
  if (existing) clearTimeout(existing);
  const wait = delay ?? vscode.workspace.getConfiguration('chartforgexMarkup').get<number>('validateDebounceMs', 650);
  validationTimers.set(key, setTimeout(() => {
    validationTimers.delete(key);
    void validateActiveDocument(context, false, document.uri);
  }, wait));
}

function schedulePreviewRefresh(context: vscode.ExtensionContext, document: vscode.TextDocument, delay?: number): void {
  if (!vscode.workspace.getConfiguration('chartforgexMarkup').get<boolean>('previewAutoRefresh', true)) {
    return;
  }

  const panel = previewPanels.get(document.uri.toString());
  if (!panel) {
    return;
  }

  const key = document.uri.toString();
  const existing = previewTimers.get(key);
  if (existing) clearTimeout(existing);
  const wait = delay ?? vscode.workspace.getConfiguration('chartforgexMarkup').get<number>('previewDebounceMs', 350);
  previewTimers.set(key, setTimeout(() => {
    previewTimers.delete(key);
    void refreshPreview(context, document, panel);
  }, wait));
}

async function activeMarkupDocument(resource?: vscode.Uri): Promise<vscode.TextDocument | undefined> {
  if (resource) {
    return vscode.workspace.openTextDocument(resource);
  }

  const document = vscode.window.activeTextEditor?.document;
  if (!document) {
    vscode.window.showWarningMessage('Open a ChartForgeX markup document first.');
    return undefined;
  }

  return document;
}

function couldBeMarkupDocument(document: vscode.TextDocument): boolean {
  return document.languageId === 'chartforgex-markup' ||
    document.languageId === 'markdown' ||
    document.fileName.endsWith('.cfx.md') ||
    document.fileName.endsWith('.chartforgex.md');
}

function isChartForgeXMarkup(document: vscode.TextDocument): boolean {
  if (document.languageId === 'chartforgex-markup' || document.fileName.endsWith('.cfx.md') || document.fileName.endsWith('.chartforgex.md')) {
    return true;
  }

  return /```+\s*(chartforgex|cfx)[\s-]+topology\b/i.test(document.getText()) ||
    /~~~+\s*(chartforgex|cfx)[\s-]+topology\b/i.test(document.getText());
}

async function runCli(context: vscode.ExtensionContext, document: vscode.TextDocument, command: string, extraArgs: string[] = []): Promise<CliResult> {
  let cli: CliCommand;
  try {
    cli = resolveCli(context);
  } catch (error) {
    return { stdout: '', stderr: error instanceof Error ? error.message : String(error), code: 1 };
  }

  const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'chartforgex-markup-'));
  const tempFile = path.join(tempRoot, tempDocumentName(document));
  fs.writeFileSync(tempFile, document.getText(), 'utf8');
  try {
    const args = [...cli.args, command, tempFile, ...extraArgs];
    let result = await spawnProcess(cli.command, args, documentWorkingDirectory(document, tempRoot));
    for (const fallback of cli.fallbacks ?? []) {
      if (!result.spawnError) break;
      const fallbackArgs = [...fallback.args, command, tempFile, ...extraArgs];
      result = await spawnProcess(fallback.command, fallbackArgs, documentWorkingDirectory(document, tempRoot));
    }

    return result;
  } finally {
    fs.rmSync(tempRoot, { recursive: true, force: true });
  }
}

function resolveCli(context: vscode.ExtensionContext): CliCommand {
  const configured = vscode.workspace.getConfiguration('chartforgexMarkup').get<string>('cliPath', '').trim();
  const candidates = configured ? [configured] : [
    ...bundledCliPaths(context),
    sourceCliProjectPath(context)
  ];

  const resolved: CliCommand[] = [];
  for (const candidate of candidates) {
    if (!candidate || !fs.existsSync(candidate)) continue;
    const name = path.basename(candidate).toLowerCase();
    if (!configured && !allowedCliArtifactNames.has(name)) continue;
    if (candidate.endsWith('.csproj')) {
      resolved.push({ command: 'dotnet', args: ['run', '--project', candidate, '-c', 'Release', '--'] });
    } else if (candidate.endsWith('.dll')) {
      resolved.push({ command: 'dotnet', args: [candidate] });
    } else {
      resolved.push({ command: candidate, args: [] });
    }
  }

  if (resolved.length > 0) {
    const [primary, ...fallbacks] = resolved;
    return { ...primary, fallbacks };
  }

  if (configured) {
    throw new Error(`Configured chartforgexMarkup.cliPath was not found or is unsupported: ${configured}`);
  }

  throw new Error('ChartForgeX.Markup.Cli was not found. Set chartforgexMarkup.cliPath or package the extension with the bundled CLI.');
}

function bundledCliPaths(context: vscode.ExtensionContext): string[] {
  const root = path.join(context.extensionPath, 'tools', 'ChartForgeX.Markup.Cli');
  const rid = runtimeIdentifier();
  const executable = process.platform === 'win32' ? 'ChartForgeX.Markup.Cli.exe' : 'ChartForgeX.Markup.Cli';
  const ridExecutable = path.join(root, rid, executable);
  const portableDll = path.join(root, 'ChartForgeX.Markup.Cli.dll');
  return [ridExecutable, portableDll];
}

function sourceCliProjectPath(context: vscode.ExtensionContext): string {
  return path.resolve(context.extensionPath, '..', 'ChartForgeX.Markup.Cli', 'ChartForgeX.Markup.Cli.csproj');
}

function runtimeIdentifier(): string {
  const arch = process.arch === 'arm64' ? 'arm64' : 'x64';
  if (process.platform === 'win32') return `win-${arch}`;
  if (process.platform === 'darwin') return `osx-${arch}`;
  return `linux-${arch}`;
}

function spawnProcess(command: string, args: string[], cwd: string): Promise<CliResult> {
  return new Promise((resolve) => {
    const child = spawn(command, args, { cwd, shell: false });
    let stdout = '';
    let stderr = '';
    child.stdout.on('data', (chunk: Buffer) => { stdout += chunk.toString(); });
    child.stderr.on('data', (chunk: Buffer) => { stderr += chunk.toString(); });
    child.on('error', (error) => resolve({ stdout, stderr: error.message, code: 1, spawnError: true }));
    child.on('close', (code) => resolve({ stdout, stderr, code }));
  });
}

function tempDocumentName(document: vscode.TextDocument): string {
  const sourceName = document.uri.scheme === 'file' ? path.basename(document.fileName) : 'untitled.cfx.md';
  return /\.(cfx|chartforgex)\.md$/i.test(sourceName) || /\.md$/i.test(sourceName) ? sourceName : `${sourceName}.cfx.md`;
}

function documentWorkingDirectory(document: vscode.TextDocument, fallback: string): string {
  if (document.uri.scheme === 'file') {
    return path.dirname(document.fileName);
  }

  const workspaceFolder = vscode.workspace.workspaceFolders?.[0]?.uri;
  if (workspaceFolder?.scheme === 'file') {
    return workspaceFolder.fsPath;
  }

  return fallback;
}

function applyDiagnostics(document: vscode.TextDocument, result: CliResult): void {
  const items = parseDiagnostics(document, result);
  if (result.code === 0) {
    if (items.length === 0) {
      diagnostics.delete(document.uri);
    } else {
      diagnostics.set(document.uri, items);
    }
    return;
  }

  if (items.length === 0) {
    items.push(new vscode.Diagnostic(new vscode.Range(0, 0, 0, 1), result.stderr || result.stdout || 'ChartForgeX validation failed.', vscode.DiagnosticSeverity.Error));
  }

  diagnostics.set(document.uri, items);
}

function parseDiagnostics(document: vscode.TextDocument, result: CliResult): vscode.Diagnostic[] {
  const all = `${result.stderr}\n${result.stdout}`.split(/\r?\n/g);
  const items: vscode.Diagnostic[] = [];
  for (const line of all) {
    const match = /^(error|warning)(?:\((\d+)\))?(?:\s+[\w.-]+)?:\s*(.+)$/i.exec(line.trim());
    if (!match) continue;
    const lineNumber = Math.max(0, Number.parseInt(match[2] ?? '1', 10) - 1);
    const safeLine = Math.min(lineNumber, Math.max(0, document.lineCount - 1));
    const range = document.lineAt(safeLine).range;
    const severity = match[1].toLowerCase() === 'error' ? vscode.DiagnosticSeverity.Error : vscode.DiagnosticSeverity.Warning;
    items.push(new vscode.Diagnostic(range, match[3], severity));
  }

  return items;
}

function outputDirectory(document: vscode.TextDocument): string {
  const config = vscode.workspace.getConfiguration('chartforgexMarkup');
  const mode = config.get<string>('outputDirectoryMode', 'generatedSubfolder');
  if (mode === 'sourceDirectory') return path.dirname(document.fileName);
  const folder = config.get<string>('outputSubfolderName', 'generated').trim() || 'generated';
  return path.join(path.dirname(document.fileName), folder);
}

function defaultOutputPath(document: vscode.TextDocument, extension: string): string {
  const base = path.basename(document.fileName).replace(/(\.chartforgex|\.cfx)?\.md$/i, '');
  return path.join(outputDirectory(document), `${base}.${extension}`);
}

function renderMessage(message: string): string {
  return `<!doctype html><html><body style="font-family:Segoe UI,sans-serif;padding:24px;"><pre>${message}</pre></body></html>`;
}

function escapeHtml(value: string): string {
  return value.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}
