namespace oyasumi.Utilities
{
    public interface IMultiKeyDictionary
    {
        void Add(object primaryRaw, object secondaryRaw, object valueRaw);
        object ValueAt(int index);
        int Count { get; }
    }
}
