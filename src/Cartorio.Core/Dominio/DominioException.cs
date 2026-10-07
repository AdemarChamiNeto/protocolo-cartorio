namespace Cartorio.Core.Dominio;

/// <summary>Regra de negócio violada. A mensagem é mostrada ao usuário como está.</summary>
public sealed class DominioException(string mensagem) : Exception(mensagem);
