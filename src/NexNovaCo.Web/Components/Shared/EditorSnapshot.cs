namespace NexNovaCo.Web.Components.Shared;

// UI-only value snapshot. Capture only after a successful load/save, never after validation or write failure.
public sealed class EditorSnapshot
{
    private string[] _saved = [];
    public void Capture(string[] values) => _saved = [.. values];
    public bool HasChanges(string[] values) => !_saved.SequenceEqual(values, StringComparer.Ordinal);
}
