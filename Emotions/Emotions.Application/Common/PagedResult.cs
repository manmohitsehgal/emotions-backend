namespace Emotions.Application.Common;

public class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public int? NextPage => Items.Count < PageSize ? null : Page + 1;
}