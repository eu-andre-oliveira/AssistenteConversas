namespace AssistenteConversas.Models;

// Critério enviado pelo assistente para localizar leads existentes.
public sealed record LeadSearchCriteria(string? Nome, string? Empresa);

// Candidato retornado pela busca. Score vai de 0 a 1; quanto maior, mais parecido.
public sealed record LeadCandidate(
    Guid Id,
    string Nome,
    string? Empresa,
    string? Cargo,
    string Status,
    string Temperatura,
    DateOnly? ProximoFollowUp,
    double Score);
