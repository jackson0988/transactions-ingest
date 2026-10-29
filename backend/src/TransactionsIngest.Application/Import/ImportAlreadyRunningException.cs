namespace TransactionsIngest.Application.Import;

public sealed class ImportAlreadyRunningException()
    : InvalidOperationException("An import is already running. Try again when it has finished.");
