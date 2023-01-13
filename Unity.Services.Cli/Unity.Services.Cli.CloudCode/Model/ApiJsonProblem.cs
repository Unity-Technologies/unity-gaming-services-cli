namespace Unity.Services.Cli.CloudCode.Model;

[Serializable]
class ApiJsonProblem
{
    public string? Type { get; set; }
    public string? Title { get; set; }
    public int Status { get; set; }
    public string? Detail { get; set; }
    public int Code { get; set; }
    public IList<ApiJsonProblemError>? Errors { get; set; }
}

[Serializable]
class ApiJsonProblemError
{
    public string? Field { get; set; }
    public IList<string>? Messages { get; set; }
}
