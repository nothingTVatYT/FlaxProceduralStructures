using System.Collections.Generic;

namespace ProceduralStructures;

public class CircularReadonlyList<T> : List<T>
{
    List<T> _data;
    bool _reversedAccess;
    public int IndexOffset;

    ///<summary>This creates a read-only view on the list which will not be changed</summary>
    public CircularReadonlyList(List<T> orig)
    {
        _data = orig;
    }

    public CircularReadonlyList(params T[] t)
    {
        _data = new List<T>(t);
    }

    public new T this[int index]
    {
        get
        {
            var clippedIndex = index % _data.Count;
            if (clippedIndex < 0) clippedIndex += _data.Count;
            var realIndex = (_data.Count + IndexOffset + clippedIndex * (_reversedAccess ? -1 : 1)) % _data.Count;
            return _data[realIndex];
        }
    }

    public new int Count => _data.Count;

    /// <summary>This just changes the access order, it won't change the underlying list</summary>
    public new void Reverse()
    {
        _reversedAccess = !_reversedAccess;
    }

    /// <summary>Shift and rotate the items so that the new index 0 points to the previous 1 and so on</summary>
    public void Shift()
    {
        if (!_reversedAccess) IndexOffset++;
        else IndexOffset--;
        if (IndexOffset < 0) IndexOffset += _data.Count;
    }
}