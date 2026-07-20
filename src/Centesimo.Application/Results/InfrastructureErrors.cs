namespace Centesimo.Application;

public static class InfrastructureErrors
{
    public static readonly Error PersistenceFailure = new(
        "Infrastructure.PersistenceFailure",
        "Non è stato possibile salvare i dati.");

    public static readonly Error Unexpected = new(
        "Infrastructure.Unexpected",
        "Si è verificato un errore imprevisto.");
}
