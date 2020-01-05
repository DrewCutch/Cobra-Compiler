using System.Collections.Generic;

namespace CobraCompiler.Parse
{
    class ListNibbler<T>
    {
        private readonly IReadOnlyList<T> _list;
        private int _position;

        public ListNibbler(IReadOnlyList<T> list)
        {
            _list = list;
            _position = 0;
        }

        public T Pop()
        {
            if (HasNext())
                return _list[_position++];

            return default;
        }

        public T Peek(int lookAhead = 0)
        {
            if (HasNext(lookAhead))
                return _list[_position + lookAhead];

            return default;
        }

        public T Previous(int lookBehind = 1)
        {
            if (HasPrevious(lookBehind))
                return _list[_position - lookBehind];

            return default;
        }

        public bool HasPrevious(int lookBehind = 1)
        {
            return (_position - lookBehind) >= 0;
        }

        public bool HasNext(int lookAhead = 0)
        {
            return (_position + lookAhead) < _list.Count;
        }
    }
}
