using System;
using System.Linq;
using System.Text.Json;

namespace BrutGui
{
    public class MRU
    {
        private string[] _mru;
        private readonly int _size;
        private int _count;

        public MRU(int size, string json = null)
        {
            _size = size;
            _mru = new string[_size];
            _count = 0;

            if (json != null)
            {
                string[] temp = JsonSerializer.Deserialize<string[]>(json);
                for (int i = temp.Count() - 1; i >= 0; i-- )
                {
                    Add(temp[i]);
                }
            }
        }

        public MRU(string json)
        {

        }

        public void Add(string item)
        {
            bool found = false;
            for (int i = 0; i < _count; i++)
            {
                if (_mru[i] == item)
                {
                    found = true;
                }
                if (found)
                {
                    if (i == _count - 1)
                    {
                        _mru[i] = item;
                    } else
                    {
                        _mru[i] = _mru[i + 1];
                    }
                }
            }
            if (!found)
            {
                _mru[_count++] = item;
            }
        }

        public int Size()
        {
            return _size;
        }

        public int Count()
        {
            return _count;
        }

        public void Clear()
        {
            _mru = new string[_size];
            _count = 0;
        }

        public string[] ToArray()
        {
            string[] array = new string[_count];
            int j = 0;
            for (int i = _count - 1; i >= 0; i--, j++)
            {
                array[j] = _mru[i];
            }
            return array;
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(ToArray());
        }
    }
}
