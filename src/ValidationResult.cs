namespace Philiprehberger.JsonPatch;

/// <summary>
/// Represents the result of validating a patch document against a JSON document.
/// </summary>
public class ValidationResult
{
    private readonly List<ValidationError> _errors;

    /// <summary>
    /// Initializes a new <see cref="ValidationResult"/> with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors encountered, if any.</param>
    internal ValidationResult(List<ValidationError> errors)
    {
        _errors = errors;
    }

    /// <summary>
    /// Gets a value indicating whether the patch can be cleanly applied without errors.
    /// </summary>
    public bool IsValid => _errors.Count == 0;

    /// <summary>
    /// Gets the list of validation errors encountered during pre-checking.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors => _errors.AsReadOnly();
}
