namespace GameVM.Compiler.Application;

public record CompileRequest(string SourceCode, string Language, string OutputFile);
